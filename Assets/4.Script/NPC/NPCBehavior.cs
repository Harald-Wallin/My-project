using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class NPCBehavior : MonoBehaviour
{
    [Header("Refs")]
    public Transform player;
    private PlayerStats subscribedPlayer;

    [Header("Leash")]
    [SerializeField] protected float maxDistanceFromSpawn = 8f;

    [Header("Attack")]
    private AbilityController abilityController;
    private CharacterActionController actionController;
    private BaseAttackController baseAttackController;

    [Header("Ability Delay")]
    [SerializeField] private float abilityDelayAfterAggro = 3f;
    private float abilityLockTimer;

    [Header("Movement")]
    protected NPCMovement movement;
    [SerializeField]
    float stopDistanceDefault = 1f;

    [Header("Aggro")]
    [SerializeField]
    protected bool canAggro = true;
    public float aggroRange = 4f;
    private float currentAggroRange;
    protected bool isAggro;
    protected bool isReturning;
    protected Vector3 spawnPosition;
    protected CharacterStats currentTargetStats;
    public bool HasTarget => currentTargetStats != null;
    public CharacterStats CurrentTarget => currentTargetStats;
    [SerializeField]
    [Tooltip(
    "Collider-lager som används när NPC:n söker efter hot. " +
    "Ska normalt inkludera Hitbox.")]

    private LayerMask threatDetectionLayers;

    [Header("Wander")]
    [SerializeField] protected bool canWander = true;

    [Header("Patrol")]
    [SerializeField] protected bool canPatrol = false;
    [SerializeField] protected PatrolPath patrolPath;

    [Header("Flee")]
    private float activeFleeSpeedMultiplier = 1f;
    [SerializeField] protected float fleeDistance = 12f; //Första panic-jumpen
    [SerializeField] protected float safeDistanceFromThreat = 18f; //När NPC slutar springa
    [SerializeField] protected float resumeFleeDistance = 12f; //Om spelaren kommer närmre än detta > fly igen
    [SerializeField] protected float maxHoldDistanceFromSpawn = 50f; //Förhindrar oändlig flykt
    protected Vector3 fleeTargetPosition;
    protected CharacterStats fleeSource;

 //Low-health retreat är INTE samma sak som vanlig Flee.
 //Den är en tillfällig ompositionering inom samma encounter.
    private bool lowHealthRetreatActive;
    private CharacterStats lowHealthRetreatThreat;
    private Vector3 lowHealthRetreatStartPosition;

    [Header("Death & Respawn")]
    private bool isDead = false;

    private float aggroDisableTimer;
    //private bool wasMovingLastFrame;

    private bool wasPatrollingBeforeCombat;
    private bool restartPatrolOnNextEnter;
    private Vector3 combatAnchorPosition;
    private Vector3 encounterReturnPosition;

    private bool hasCombatAnchor;

    public bool IsInCombat => currentState == AIState.Aggro;
    protected AIState currentState = AIState.Idle;
    public AIState CurrentState => currentState;
    public CharacterStats selfStats;

    private BuffSystem buffSystem;
    private NPCReactionController reactionController;

    private bool encounterResetInProgress;

    public bool IsEncounterResetting =>
        encounterResetInProgress ||
        currentState == AIState.Returning;

    [Header("Re-aggro")]
    [SerializeField] private float reaggroCooldown = 0f;
    private float lastReturnTime = -999f;

    void Awake()
    {
        movement =
            GetComponent<NPCMovement>();

        baseAttackController =
            GetComponent<BaseAttackController>();

        abilityController =
            GetComponent<AbilityController>();

        actionController =
            GetComponent<CharacterActionController>();

        selfStats =
            GetComponent<CharacterStats>();

        buffSystem =
            GetComponent<BuffSystem>();

        reactionController =
            GetComponent<NPCReactionController>();

        spawnPosition =
            transform.position;

        combatAnchorPosition =
            spawnPosition;

        encounterReturnPosition =
            spawnPosition;

        if (selfStats != null)
        {
            /*
             * NPCReactionController hanterar den generella
             * Aggro/Flee/None-reaktionen.
             *
             * Denna hook används av specialiserade AI-klasser,
             * exempelvis HumanoidAI och GuardAI.
             */
            selfStats.OnDamagedBy +=
                HandleDamaged;

            selfStats.OnDied +=
                HandleDeath;
        }

        currentAggroRange =
            aggroRange;
    }

    /// <summary>
    /// Utökningspunkt för specialiserade NPC-typer.
    ///
    /// Den generella damage-reaktionen styrs av
    /// NPCReactionController. Den här metoden ska därför inte
    /// starta vanlig Aggro/Flee-logik i basklassen.
    /// </summary>
    protected virtual void HandleDamaged(
        CharacterStats attacker)
    {
    }

    protected virtual void Start()
    {
        if (player == null)
            player = PlayerReference.Player?.transform;

        movement.SetFacing(Vector2.down);

        if (canPatrol && patrolPath != null && patrolPath.points.Count > 0)
        {
            EnterPatrolState(true);
        }
        else if (canWander)
        {
            EnterWanderState();
        }
        else
        {
            EnterIdleState();
        }
    }

    private MobSpawner spawner;

    public void SetSpawner(MobSpawner newSpawner)
    {
        spawner = newSpawner;
    }

    public void SetPatrolPath(PatrolPath path)
    {
        patrolPath = path;

        if (canPatrol &&
            patrolPath != null &&
            patrolPath.points.Count > 0)
        {
            EnterPatrolState(true);
        }
    }

    void UpdateTimers()
    {
        if (aggroDisableTimer > 0f)
            aggroDisableTimer -= Time.fixedDeltaTime;

        if (abilityLockTimer > 0f)
            abilityLockTimer -= Time.fixedDeltaTime;
    }

    void FixedUpdate()
    {
        if (player == null)
        {
            player = PlayerReference.Player?.transform;
        }

        UpdateTimers();

        HandleLeash();
        UpdateCurrentState();
    }

    void UpdateCurrentState()
    {
        switch (currentState)
        {
            case AIState.Idle:
                UpdateIdleState();
                break;

            case AIState.Wandering:
                UpdateWanderState();
                break;

            case AIState.Patrolling:
                UpdatePatrolState();
                break;

            case AIState.Aggro:
                UpdateAggroState();
                break;

            case AIState.Returning:
                UpdateReturnState();
                break;

            case AIState.Fleeing:
                UpdateFleeState();
                break;

            case AIState.Holding:
                UpdateHoldingState();
                break;
        }
    }

    void ChangeState(AIState newState)
    {
        if (currentState == newState)
            return;

        ExitCurrentState();

        currentState = newState;

        EnterCurrentState();
    }

    void EnterCurrentState()
    {
        switch (currentState)
        {
            case AIState.Idle:
                movement.EndWander();
                movement.EndPatrol();
                movement.EndFlee();
                movement.SetMovementMode(NPCMovement.NPCMovementMode.Default);
                break;

            case AIState.Wandering:
                movement.BeginWander();
                movement.SetMovementMode(NPCMovement.NPCMovementMode.Wander);
                break;

            case AIState.Patrolling:
                if (restartPatrolOnNextEnter)
                {
                    movement.StartPatrol();
                }
                else
                {
                    movement.ResumePatrol();
                }

                restartPatrolOnNextEnter = false;
                break;

            case AIState.Aggro:
                movement.EndWander();
                movement.EndPatrol();
                movement.EndFlee();
                movement.SetMovementMode(NPCMovement.NPCMovementMode.Aggressive);
                break;

            case AIState.Returning:
                movement.EndWander();
                movement.EndPatrol();
                movement.EndFlee();
                movement.SetMovementMode(NPCMovement.NPCMovementMode.Default);
                break;

            case AIState.Fleeing:
                movement.SetMovementMode(NPCMovement.NPCMovementMode.Flee);
                break;

            case AIState.Holding:
                movement.EndFlee();
                movement.Stop();
                break;
        }
    }

    void ExitCurrentState()
    {
        switch (currentState)
        {
            case AIState.Wandering:
                movement.EndWander();
                break;

            case AIState.Patrolling:
                movement.EndPatrol();
                break;

            case AIState.Fleeing:
                movement.EndFlee();
                break;
        }
    }

    void SetupIdle()
    {
        isAggro = false;
        isReturning = false;

        currentTargetStats = null;
    }

    void SetupWander()
    {
        isAggro = false;
        isReturning = false;

        currentTargetStats = null;
    }

    void SetupPatrol()
    {
        isAggro = false;
        isReturning = false;

        currentTargetStats = null;
    }

    void SetupReturn()
    {
        isAggro = false;

        isReturning = true;

        currentTargetStats = null;

        lastReturnTime = Time.time;

        if (subscribedPlayer != null)
        {
            subscribedPlayer.OnDied -= HandleTargetDied;
            subscribedPlayer = null;
        }
    }

    void SetupHolding()
    {
        isAggro = false;
        isReturning = false;

        currentTargetStats = null;
    }

    private void SetupFlee(
    CharacterStats threat,
    float speedMultiplier)
    {
        if (threat == null)
            return;

        fleeSource =
            threat;

        isAggro =
            false;

        isReturning =
            false;

        currentTargetStats =
            null;

        activeFleeSpeedMultiplier =
            Mathf.Max(
                0f,
                speedMultiplier
            );

        movement.BeginFlee(
            threat,
            fleeDistance,
            safeDistanceFromThreat,
            activeFleeSpeedMultiplier
        );
    }


    void UpdateIdleState()
    {
        HandleAggroDetection();
    }

    protected virtual void EnterIdleState()
    {
        SetupIdle();
        ChangeState(AIState.Idle);
    }

    void UpdateWanderState()
    {
        movement.UpdateWander(spawnPosition);
        HandleAggroDetection();
    }
    protected virtual void EnterWanderState()
    {
        SetupWander();

        ChangeState(AIState.Wandering);
    }

    void UpdatePatrolState()
    {
        movement.UpdatePatrol(patrolPath);
        HandleAggroDetection();
    }
    protected virtual void EnterPatrolState(bool restartPatrol = false)
    {
        SetupPatrol();

        restartPatrolOnNextEnter = restartPatrol;

        if (currentState == AIState.Patrolling)
        {
            if (restartPatrol)
            {
                movement.StartPatrol();
            }
            else
            {
                movement.ResumePatrol();
            }

            restartPatrolOnNextEnter = false;
            return;
        }

        ChangeState(AIState.Patrolling);
    }

    void UpdateAggroState()
    {
        // =====================================================
        // TARGET VALIDATION
        // =====================================================

        if (currentTargetStats == null ||
            !currentTargetStats.IsAlive)
        {
            BeginEncounterResetAndReturn();

            return;
        }

        if (actionController == null)
        {
            movement.Stop();

            return;
        }

        // =====================================================
        // ACTIVE ACTION
        // =====================================================

        /*
         * En action pågår redan.
         *
         * NPC:n ska stå still under den actionfas som
         * CharacterActionController just nu styr.
         *
         * VIKTIGT:
         * navigationen får INTE raderas här.
         *
         * När actionen är färdig kan NPC:n fortsätta på sin
         * gamla path eller repatha mot targetets nya position.
         */
        if (actionController.HasActiveAction)
        {
            movement.HoldPosition();

            FaceCurrentTarget();

            return;
        }

        // =====================================================
        // DISTANCE
        // =====================================================

        float distance =
            Vector2.Distance(
                transform.position,
                currentTargetStats.transform.position
            );

        AbilityData desiredAction =
            SelectDesiredAction(
                distance
            );

        // =====================================================
        // NO CURRENTLY AVAILABLE ACTION
        // =====================================================

        /*
         * Detta kan exempelvis ske medan base attack ligger
         * på cooldown.
         *
         * NPC:n ska fortfarande bibehålla sitt combat state
         * och sin navigation.
         */
        if (desiredAction == null)
        {
            float fallbackRange =
                baseAttackController != null
                    ? baseAttackController
                        .CurrentAttackRange
                    : movement.DefaultStopDistance;

            /*
             * Är target faktiskt för långt bort fortsätter vi
             * approach.
             */
            if (distance >
                fallbackRange * 0.9f)
            {
                movement.UpdateAggroMovement(
                    currentTargetStats,
                    fallbackRange,
                    forceApproach: false
                );

                return;
            }

            /*
             * Vi står ungefär där vi vill vara medan vi väntar
             * på nästa tillgängliga action.
             *
             * Behåll navigationen.
             */
            movement.HoldPosition();

            FaceCurrentTarget();

            return;
        }

        // =====================================================
        // TARGETING SETTINGS
        // =====================================================

        AbilityTargetingSettings targeting =
            desiredAction.TargetingSettings;

        if (targeting == null)
        {
            movement.HoldPosition();

            return;
        }

        // =====================================================
        // SELF-TARGETED ACTION
        // =====================================================

        if (targeting.TargetingMode ==
            TargetingMode.Self)
        {
            movement.HoldPosition();

            TryStartNPCAction(
                desiredAction
            );

            return;
        }

        // =====================================================
        // APPROACH RANGE
        // =====================================================

        float desiredRange =
            Mathf.Max(
                targeting.Range,
                movement.DefaultStopDistance
            );

        /*
         * Target befinner sig utanför abilityns räckvidd.
         *
         * Här behövs ingen targeting-query ännu.
         * Vi vet redan att NPC:n måste närmare.
         */
        if (distance >
            desiredRange * 0.9f)
        {
            movement.UpdateAggroMovement(
                currentTargetStats,
                desiredRange,
                forceApproach: false
            );

            return;
        }

        // =====================================================
        // MINIMUM RANGE
        // =====================================================

        /*
         * Target ligger för nära den önskade abilityn.
         *
         * För närvarande försöker vi fallback-base-attack.
         *
         * Ranged repositioning kan läggas till separat senare.
         */
        if (distance <
            targeting.MinimumRange)
        {
            movement.HoldPosition();

            FaceCurrentTarget();

            HandleAttack(
                desiredAction,
                distance
            );

            return;
        }

        // =====================================================
        // CAN THE ACTION ACTUALLY HIT FROM HERE?
        // =====================================================

        /*
         * RANGE är inte samma sak som en giltig combat-position.
         *
         * Exempel:
         *
         * WOLF
         *   |
         * TREE
         *   |
         * PLAYER
         *
         * Vargen kan matematiskt vara inom 2 meter,
         * men trädet gör attacken ogiltig.
         *
         * Abilityns egna targetingregler avgör detta.
         */
        TargetingResult targetingResult =
            actionController
                .EvaluateTargeting(
                    desiredAction,
                    currentTargetStats
                );

        bool canAttackFromCurrentPosition =
            targetingResult != null &&
            targetingResult.IsValid;

        // =====================================================
        // INVALID COMBAT POSITION
        // =====================================================

        if (!canAttackFromCurrentPosition)
        {
            /*
             * Detta är den viktiga combat-navigation-regeln:
             *
             * NPC:n HAR redan aggro.
             *
             * Därför ska förlorad LoS INTE få den att glömma
             * target eller stå still.
             *
             * Den fortsätter istället pathfinding mot target
             * tills en användbar combat-position hittas.
             *
             * 0.05-ish stop distance används genom forceApproach
             * eftersom vanlig attack-range annars skulle stoppa
             * NPC:n på fel sida av ett hinder.
             */
            movement.UpdateAggroMovement(
                currentTargetStats,
                desiredRange,
                forceApproach: true
            );

            return;
        }

        // =====================================================
        // VALID COMBAT POSITION
        // =====================================================

        movement.HoldPosition();

        FaceCurrentTarget();

        bool attackStarted =
            HandleAttack(
                desiredAction,
                distance
            );

        if (attackStarted)
        {
            return;
        }

        /*
         * Targetingen var giltig men actionen kunde inte startas.
         *
         * Exempel:
         * - någon kort runtime-lock
         * - resource/cost
         * - timing/state
         *
         * NPC:n får stå kvar utan att dess navigation förstörs.
         */
    }

    void SetupAggro(
    CharacterStats target)
    {
        encounterResetInProgress = false;

        currentTargetStats = target;

        player = target.transform;

        isAggro = true;
        isReturning = false;

        abilityLockTimer =
            abilityDelayAfterAggro;
    }

    protected virtual void EnterAggroState(
    CharacterStats target)
    {
        if (target == null)
            return;

        if (IsEncounterResetting)
            return;

        if (currentState == AIState.Aggro)
        {
            currentTargetStats =
                target;

            return;
        }

        wasPatrollingBeforeCombat =
            currentState ==
            AIState.Patrolling;

        /*
         * Patrullerande NPC:er leashar från den plats där
         * striden började, inte från sin ursprungliga spawnpunkt.
         */
        combatAnchorPosition =
            wasPatrollingBeforeCombat
                ? transform.position
                : spawnPosition;

        encounterReturnPosition =
            combatAnchorPosition;

        hasCombatAnchor = true;

        ChangeState(
            AIState.Aggro
        );

        SetupAggro(
            target
        );

        PlayerStats playerTarget =
            target as PlayerStats;

        if (playerTarget != null)
        {
            SubscribeToPlayerDeath(
                playerTarget
            );
        }
    }

    private void UpdateReturnState()
    {
        movement.UpdateReturnMovement(
            encounterReturnPosition
        );

        float distance =
            Vector2.Distance(
                transform.position,
                encounterReturnPosition
            );

        if (distance >
            movement.DefaultStopDistance)
        {
            return;
        }

        movement.Stop();

        CompleteEncounterReset();

        bool shouldResumePatrol =
                    wasPatrollingBeforeCombat &&
            canPatrol &&
            patrolPath != null &&
            patrolPath.points.Count > 0;

        wasPatrollingBeforeCombat = false;

        if (shouldResumePatrol)
        {
            EnterPatrolState(
                false
            );

            return;
        }

        if (canWander)
        {
            EnterWanderState();
            return;
        }

        EnterIdleState();
    }

    protected virtual void EnterReturnState()
    {
        SetupReturn();

        ChangeState(AIState.Returning);
    }

    protected virtual void EnterFleeState(
    CharacterStats threat,
    float speedMultiplier = 1f)
    {
        if (threat == null)
            return;

        SetupFlee(
            threat,
            speedMultiplier
        );

        ChangeState(
            AIState.Fleeing
        );
    }

    protected virtual void EnterHoldingState()
    {
        SetupHolding();

        ChangeState(AIState.Holding);
    }

    private void HandleLeash()
    {
        if (currentState != AIState.Aggro)
            return;

        if (IsEncounterResetting)
            return;

        Vector3 leashOrigin =
            hasCombatAnchor
                ? combatAnchorPosition
                : spawnPosition;

        float distanceFromLeashOrigin =
            Vector2.Distance(
                transform.position,
                leashOrigin
            );

        if (distanceFromLeashOrigin <=
            maxDistanceFromSpawn)
        {
            return;
        }

        encounterReturnPosition = spawnPosition;

        BeginEncounterResetAndReturn();
    }

    private void BeginEncounterResetAndReturn()
    {
        if (encounterResetInProgress)
            return;

        bool wasInEncounter =
            currentState == AIState.Aggro ||
            currentState == AIState.Fleeing ||
            currentState == AIState.Holding;

        if (!wasInEncounter)
            return;

        encounterResetInProgress =
            true;

        /*
         * Avsluta aktiva actions omedelbart.
         *
         * HP, encounter-buffs och reaction-memory återställs däremot
         * först när NPC:n faktiskt har nått sin return-position.
         */
        actionController
            ?.ResetRuntimeState();

        abilityController
            ?.ResetRuntimeState();

        movement?.Stop();

        lowHealthRetreatActive =
            false;

        lowHealthRetreatThreat =
            null;

        lowHealthRetreatStartPosition =
            Vector3.zero;

        fleeSource =
            null;

        currentTargetStats =
            null;

        activeFleeSpeedMultiplier =
            1f;

        if (subscribedPlayer != null)
        {
            subscribedPlayer.OnDied -=
                HandleTargetDied;

            subscribedPlayer =
                null;
        }

        aggroDisableTimer =
            Mathf.Max(
                aggroDisableTimer,
                reaggroCooldown
            );

        lastReturnTime =
            Time.time;

        isAggro =
            false;

        isReturning =
            true;

        /*
         * Behåll encounterReturnPosition som sattes när encounter
         * startade.
         *
         * En patrullerande NPC går därmed tillbaka till sin
         * encounter-anchor. En stationär NPC använder spawnpunkten.
         */
        EnterReturnState();
    }

    private void CompleteEncounterReset()
    {
        /*
         * Detta är den riktiga fulla encounter-resetten.
         *
         * Den körs först när NPC:n faktiskt har återvänt.
         */
        buffSystem
            ?.RemoveEncounterResetBuffs();

        selfStats
            ?.ResetEncounterState();

        reactionController
            ?.ResetEncounterState();

        fleeSource =
            null;

        currentTargetStats =
            null;

        lowHealthRetreatActive =
            false;

        lowHealthRetreatThreat =
            null;

        lowHealthRetreatStartPosition =
            Vector3.zero;

        activeFleeSpeedMultiplier =
            1f;

        isAggro =
            false;

        isReturning =
            false;

        hasCombatAnchor =
            false;

        encounterResetInProgress =
            false;
    }

    private void HandleAggroDetection()
    {
        if (!canAggro)
            return;

        if (aggroDisableTimer > 0f)
            return;

        if (Time.time - lastReturnTime <
            reaggroCooldown)
        {
            return;
        }

        if (IsEncounterResetting ||
            isReturning)
        {
            return;
        }

        if (currentState == AIState.Aggro ||
            currentState == AIState.Fleeing ||
            currentState == AIState.Holding)
        {
            return;
        }

        if (reactionController == null)
        {
            reactionController =
                GetComponent<
                    NPCReactionController>();
        }

        if (reactionController == null ||
            reactionController.ReactionType ==
            NPCReactionType.None)
        {
            return;
        }



        /*
         * Vi söker alla colliders och resolve:ar därefter deras
         * CharacterStats från parent-hierarkin.
         *
         * Det gör proximity detection oberoende av om karaktärens
         * collider ligger på NPC, HostileMob, Hitbox eller ett
         * annat child-layer.
         */
        Collider2D[] hits =
            Physics2D.OverlapCircleAll(
                transform.position,
                currentAggroRange
            );


        HashSet<CharacterStats>
            checkedCharacters =
                new();

        for (int i = 0;
             i < hits.Length;
             i++)
        {
            Collider2D hit =
                hits[i];

            if (hit == null)
                continue;

            CharacterStats threat =
                hit.GetComponentInParent<
                    CharacterStats>();

            if (threat == null ||
                threat == selfStats)
            {
                continue;
            }

            /*
             * En karaktär kan ha flera colliders.
             * Den ska bara valideras en gång per scan.
             */
            if (!checkedCharacters.Add(
                    threat))
            {
                continue;
            }

            if (!threat.IsAlive)
                continue;

            if (!LineOfSightUtility
                    .HasLineOfSight(
                        transform.position,
                        threat.transform.position))
            {
                continue;
            }

            bool reacted =
                reactionController
                    .TryReactToProximityThreat(
                        threat
                    );

            if (reacted)
                return;
        }
    }

    private void UpdateFleeState()
    {
        if (currentState !=
            AIState.Fleeing)
        {
            return;
        }

        bool fleeFinished =
            movement.UpdateFlee();

        if (!fleeFinished)
            return;

        movement.EndFlee();

        /*
         * LOW HEALTH RETREAT
         *
         * Detta är bara en taktisk ompositionering.
         * NPC:n ska INTE:
         *
         * - resetta HP
         * - resetta cooldowns
         * - lämna encounter
         * - återvända till spawn
         */
        if (lowHealthRetreatActive)
        {
            CompleteLowHealthRetreat();

            return;
        }

        /*
         * Vanlig Flee-reaction behåller det gamla beteendet.
         */
        EnterHoldingState();
    }

    private void CompleteLowHealthRetreat()
    {
        CharacterStats threat =
            lowHealthRetreatThreat;

        lowHealthRetreatActive =
            false;

        lowHealthRetreatThreat =
            null;

        fleeSource =
            null;

        activeFleeSpeedMultiplier =
            1f;

        /*
         * Om target dog medan NPC:n retirerade finns inget encounter
         * kvar att återgå till.
         *
         * Då går NPC:n tillbaka till ORIGINAL spawn och gör en riktig
         * encounter-reset där.
         */
        if (threat == null ||
            !threat.IsAlive)
        {
            encounterReturnPosition =
                spawnPosition;

            BeginEncounterResetAndReturn();

            return;
        }

        /*
         * Återgå till samma encounter utan att skapa ett nytt
         * combat-anchor.
         *
         * combatAnchorPosition är redan retreatens startpunkt.
         */
        ResumeAggroAfterLowHealthRetreat(
            threat
        );
    }

    private void ResumeAggroAfterLowHealthRetreat(
    CharacterStats threat)
    {
        if (threat == null ||
            !threat.IsAlive)
        {
            encounterReturnPosition =
                spawnPosition;

            BeginEncounterResetAndReturn();

            return;
        }

        currentTargetStats =
            threat;

        player =
            threat.transform;

        isAggro =
            true;

        isReturning =
            false;

        /*
         * VIKTIGT:
         *
         * Vi anropar INTE EnterAggroState här.
         *
         * EnterAggroState skapar normalt ett nytt encounter-anchor.
         * Efter retreat ska ankaret däremot fortsätta vara platsen
         * där retreaten började.
         */
        ChangeState(
            AIState.Aggro
        );

        PlayerStats playerTarget =
            threat as PlayerStats;

        if (playerTarget != null)
        {
            SubscribeToPlayerDeath(
                playerTarget
            );
        }
    }

    void UpdateHoldingState()
    {
        if (currentState != AIState.Holding)
            return;

        if (fleeSource == null)
            return;

        float distanceToThreat =
            Vector2.Distance(
                transform.position,
                fleeSource.transform.position
            );

        if (distanceToThreat <= resumeFleeDistance)
        {
            EnterFleeState(
                fleeSource,
                activeFleeSpeedMultiplier
            );
            return;
        }

        float distanceFromSpawn =
            Vector2.Distance(
                transform.position,
                spawnPosition
            );

        if (distanceFromSpawn >= maxHoldDistanceFromSpawn)
        {
            ReturnToSpawn();
        }

        NPCReactionController reaction = GetComponent<NPCReactionController>();

        if (reaction == null)
            return;

        bool factionStillAlerted =
            FactionAwarenessSystem.Instance != null &&
            FactionAwarenessSystem.Instance
                .IsFactionAlerted(reaction.Faction);

        if (!factionStillAlerted)
        {
            ReturnToSpawn();
        }
    }

    private bool HandleAttack(
    AbilityData desiredAction,
    float distanceToTarget)
    {
        if (desiredAction == null)
            return false;

        if (actionController == null)
            return false;

        if (selfStats == null ||
            !selfStats.CanAct())
        {
            return false;
        }

        if (!isAggro ||
            isReturning ||
            currentTargetStats == null)
        {
            return false;
        }

        if (currentState ==
                AIState.Fleeing ||
            currentState ==
                AIState.Holding)
        {
            return false;
        }

        AbilityTargetingSettings targeting =
            desiredAction.TargetingSettings;

        if (targeting == null)
            return false;

        bool isSelfTargeted =
            targeting.TargetingMode ==
            TargetingMode.Self;

        if (!isSelfTargeted)
        {
            if (distanceToTarget >
                targeting.Range)
            {
                return false;
            }

            if (distanceToTarget <
                targeting.MinimumRange)
            {
                return TryFallbackBaseAttack(
                    distanceToTarget
                );
            }
        }

        bool started =
            TryStartNPCAction(
                desiredAction
            );

        if (started)
            return true;

        /*
         * Abilityn gick inte att använda från nuvarande position.
         *
         * Exempel:
         * - World blockerar Line of Sight
         * - targetingen är spatialt ogiltig
         * - abilityn kan inte påverka target härifrån
         *
         * NPCBehavior får då fortsätta navigationen istället för
         * att fastna i "inom range = stå still".
         */
        if (!desiredAction.IsBaseAttack)
        {
            return TryFallbackBaseAttack(
                distanceToTarget
            );
        }

        return false;
    }

    bool TryStartNPCAction(
    AbilityData ability)
    {
        if (ability == null)
            return false;

        if (actionController == null)
            return false;

        if (actionController.HasActiveAction)
            return false;

        bool started;

        if (ability.TargetingSettings != null &&
            ability.TargetingSettings.TargetingMode ==
            TargetingMode.Self)
        {
            started =
                actionController.TryStartAction(
                    ability
                );
        }
        else
        {
            if (currentTargetStats == null)
                return false;

            started =
                actionController.TryStartAction(
                    ability,
                    currentTargetStats
                );
        }

        if (!started)
            return false;

        /*
         * Spelaren bekräftar en Confirmed-action genom input.
         * NPC:n har ingen sådan input och bekräftar därför sin
         * targeting automatiskt.
         */
        if (actionController.IsPreviewing)
        {
            bool confirmed =
                actionController
                    .ConfirmCurrentAction();

            if (!confirmed)
            {
                actionController
                    .CancelCurrentAction();

                return false;
            }
        }

        return true;
    }

    bool TryFallbackBaseAttack(
    float distanceToTarget)
    {
        if (baseAttackController == null ||
            actionController == null)
        {
            return false;
        }

        AbilityData baseAttack =
            baseAttackController.CurrentAttack;

        if (!CanSelectBaseAttack(
                baseAttack))
        {
            return false;
        }

        AbilityTargetingSettings targeting =
            baseAttack.TargetingSettings;

        if (targeting == null)
            return false;

        if (distanceToTarget >
            targeting.Range)
        {
            return false;
        }

        if (distanceToTarget <
            targeting.MinimumRange)
        {
            return false;
        }

        return TryStartNPCAction(
            baseAttack
        );
    }

    AbilityData SelectDesiredAction(
    float distanceToTarget)
    {
        if (actionController == null)
            return null;

        /*
         * NPC-abilities är låsta en kort stund efter aggro.
         * Under den tiden kan NPC:n fortfarande använda sin
         * base attack.
         */
        if (abilityLockTimer <= 0f &&
            abilityController != null)
        {
            AbilityData[] abilities =
                abilityController
                    .GetEquippedAbilities();

            if (abilities != null)
            {
                foreach (AbilityData ability in abilities)
                {
                    if (!CanSelectAbility(
                            ability,
                            distanceToTarget))
                    {
                        continue;
                    }

                    return ability;
                }
            }
        }

        if (baseAttackController == null)
            return null;

        AbilityData baseAttack =
            baseAttackController.CurrentAttack;

        if (!CanSelectBaseAttack(
                baseAttack))
        {
            return null;
        }

        return baseAttack;
    }

    bool CanSelectBaseAttack(
    AbilityData attack)
    {
        if (attack == null)
            return false;

        if (!attack.IsBaseAttack)
            return false;

        if (!attack.UsesActionSettings)
            return false;

        if (attack.TargetingSettings == null)
            return false;

        return actionController
                   .GetCooldownRemaining(
                       attack
                   ) <= 0f;
    }

    bool CanSelectAbility(
    AbilityData ability,
    float distanceToTarget)
    {
        if (ability == null)
            return false;

        /*
         * Base attacks ägs av base attack-slotten och ska inte även
         * ligga bland NPC:ns vanliga equipped abilities.
         */
        if (ability.IsBaseAttack)
            return false;

        /*
         * NPC-migreringen använder endast abilities som har flyttats
         * till det nya actionsystemet.
         */
        if (!ability.UsesActionSettings)
            return false;

        if (ability.TargetingSettings == null)
            return false;

        if (ability.TimingSettings == null)
            return false;

        if (actionController
                .GetCooldownRemaining(
                    ability
                ) > 0f)
        {
            return false;
        }

        AbilityTargetingSettings targeting =
            ability.TargetingSettings;

        /*
         * Self-actions påverkas inte av avståndet till fienden.
         */
        if (targeting.TargetingMode ==
            TargetingMode.Self)
        {
            return true;
        }

        /*
         * NPCMovement kan i den här första versionen gå närmare ett
         * mål, men har ännu inget system för att backa till minimum
         * range.
         *
         * Därför väljer vi inte en ability när NPC:n redan står
         * innanför dess minimum range.
         */
        if (distanceToTarget <
            targeting.MinimumRange)
        {
            return false;
        }

        return true;
    }

    void FaceCurrentTarget()
    {
        if (movement == null ||
            currentTargetStats == null)
        {
            return;
        }

        Vector2 direction =
            (Vector2)currentTargetStats
                .transform
                .position -
            (Vector2)transform.position;

        if (direction.sqrMagnitude <= 0.0001f)
            return;

        movement.SetFacing(
            direction.normalized
        );
    }

    protected virtual bool ShouldAggro(CharacterStats potentialTarget)
    {
        if (potentialTarget == null)
            return false;

        if (potentialTarget == selfStats)
            return false;

        // Central regel:
        // Avgör först om detta överhuvudtaget är ett giltigt mål.
        if (!CombatTargeting.CanAttack(selfStats, potentialTarget))
            return false;

        // Därefter krävs fri sikt.
        if (!LineOfSightUtility.HasLineOfSight(
                transform.position,
                potentialTarget.transform.position))
            return false;

        return true;
    }

    public bool IsAggroOnPlayer()
    {
        if (currentState != AIState.Aggro)
            return false;

        return currentTargetStats is PlayerStats;
    }

    public void ForceAggro(CharacterStats target)
    {
        if (target == null)
            return;

        EnterAggroState(target);
    }

    public void StartFleeing(
    CharacterStats threat,
    float speedMultiplier = 1f)
    {
        if (threat == null)
            return;

        EnterFleeState(
            threat,
            speedMultiplier
        );
    }

    /// <summary>
    /// Startar en taktisk low-health retreat.
    ///
    /// NPC:n flyr en låst sträcka bort från threat men lämnar
    /// INTE sitt encounter.
    ///
    /// Punkten där retreaten började blir därefter encounterts
    /// nya leash-center.
    /// </summary>
    public void StartLowHealthRetreat(
        CharacterStats threat,
        float retreatDistance,
        float speedMultiplier)
    {
        if (threat == null ||
            movement == null)
        {
            return;
        }

        if (!threat.IsAlive)
            return;

        /*
         * Avbryt eventuell attack/cast som pågår när NPC:n
         * bestämmer sig för att retirera.
         *
         * Vi använder CancelCurrentAction i stället för
         * ResetRuntimeState eftersom cooldowns INTE ska
         * återställas av en taktisk retreat.
         */
        if (actionController != null &&
            actionController.HasActiveAction)
        {
            actionController
                .CancelCurrentAction();
        }

        lowHealthRetreatActive =
            true;

        lowHealthRetreatThreat =
            threat;

        /*
         * DENNA punkt blir encounterts nya leash-center.
         */
        lowHealthRetreatStartPosition =
            transform.position;

        combatAnchorPosition =
            lowHealthRetreatStartPosition;

        hasCombatAnchor =
            true;

        /*
         * Efter en low-health retreat ska en riktig encounter-reset
         * alltid ta NPC:n tillbaka till dess ursprungliga spawn.
         */
        encounterReturnPosition =
            spawnPosition;

        fleeSource =
            threat;

        activeFleeSpeedMultiplier =
            Mathf.Max(
                0f,
                speedMultiplier
            );

        
         //NPC:n slåss inte medan retreat-rörelsen pågår.
   
        isAggro =
            false;

        isReturning =
            false;

        currentTargetStats =
            null;

        movement.BeginFlee(
            threat,
            Mathf.Max(
                0.1f,
                retreatDistance
            ),
            0f,
            activeFleeSpeedMultiplier
        );

        ChangeState(
            AIState.Fleeing
        );
    }

    public void ResetAggro()
    {
        fleeSource =
            null;

        activeFleeSpeedMultiplier =
            1f;

        isAggro =
            false;

        isReturning =
            false;

        currentTargetStats =
            null;

        if (subscribedPlayer != null)
        {
            subscribedPlayer.OnDied -=
                HandleTargetDied;

            subscribedPlayer =
                null;
        }

        if (canWander)
        {
            EnterWanderState();
        }
        else
        {
            EnterIdleState();
        }
    }

    public void ReturnToSpawn()
    {
        if (currentState == AIState.Aggro ||
            currentState == AIState.Fleeing ||
            currentState == AIState.Holding)
        {
            BeginEncounterResetAndReturn();
            return;
        }

        if (currentState == AIState.Returning)
            return;

        wasPatrollingBeforeCombat = false;
        hasCombatAnchor = false;

        encounterReturnPosition =
            spawnPosition;

        encounterResetInProgress = true;

        movement?.Stop();

        EnterReturnState();
    }

    void OnDestroy()
    {
        if (selfStats != null)
        {
            selfStats.OnDamagedBy -=
                HandleDamaged;

            selfStats.OnDied -=
                HandleDeath;
        }

        if (subscribedPlayer != null)
        {
            subscribedPlayer.OnDied -= HandleTargetDied;
        }
    }

    void SubscribeToPlayerDeath(PlayerStats playerStats)
    {
        if (playerStats == null)
            return;

        if (subscribedPlayer == playerStats)
            return;

        if (subscribedPlayer != null)
        {
            subscribedPlayer.OnDied -= HandleTargetDied;
        }

        subscribedPlayer = playerStats;
        subscribedPlayer.OnDied += HandleTargetDied;
    }

    private void HandleTargetDied(
    CharacterStats deadTarget)
    {
        encounterReturnPosition =
            spawnPosition;

        BeginEncounterResetAndReturn();
    }

    void HandleDeath(CharacterStats deadCharacter)
    {
        if (isDead)
            return;

        isDead = true;

        DisableBehaviour();

        if (selfStats.deathReward != null)
        {
            selfStats.deathReward.SpawnCorpse(
                transform.position,
                selfStats);
        }

        spawner?.OnMobDied();
    }

    void DisableBehaviour()
    {
        enabled = false;

        actionController?.CancelCurrentAction();

        if (baseAttackController != null)
        {
            baseAttackController.enabled =
                false;
        }

        movement?.Stop();
    }
}