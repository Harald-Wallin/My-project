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

    [Header("Wander")]
    [SerializeField] protected bool canWander = true;

    [Header("Patrol")]
    [SerializeField] protected bool canPatrol = false;
    [SerializeField] protected PatrolPath patrolPath;

    [Header("Flee")]
    [SerializeField] protected float fleeDistance = 12f; //Första panic-jumpen
    [SerializeField] protected float safeDistanceFromThreat = 18f; //När NPC slutar springa
    [SerializeField] protected float resumeFleeDistance = 12f; //Om spelaren kommer närmre än detta > fly igen
    [SerializeField] protected float maxHoldDistanceFromSpawn = 50f; //Förhindrar oändlig flykt
    protected Vector3 fleeTargetPosition;
    protected CharacterStats fleeSource;

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
            selfStats.OnDamagedBy += HandleDamaged;
            selfStats.OnDied += HandleDeath;
        }

        currentAggroRange =
            aggroRange;
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

    void SetupFlee(CharacterStats threat)
    {
        fleeSource = threat;
        isAggro = false;
        isReturning = false;

        currentTargetStats = null;

        movement.BeginFlee(
            threat,
            fleeDistance,
            safeDistanceFromThreat);
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
        if (currentTargetStats == null || currentTargetStats.currentHP <= 0)
        {
            BeginEncounterResetAndReturn();
            return;
        }

        if (actionController == null)
        {
            movement.Stop();
            return;
        }

        /*
         * NPC:n startar inte en ny förflyttning eller action medan
         * en action redan castas, exekveras eller återhämtar sig.
         *
         * Detta förhindrar att AI:n försöker starta samma ability
         * varje FixedUpdate.
         */
        if (actionController.HasActiveAction)
        {
            movement.Stop();
            FaceCurrentTarget();
            return;
        }

        float distance =
            Vector2.Distance(
                transform.position,
                currentTargetStats.transform.position
            );

        AbilityData desiredAction =
            SelectDesiredAction(
                distance
            );

        if (desiredAction == null)
        {
            float fallbackRange =
                baseAttackController != null
                    ? baseAttackController
                        .CurrentAttackRange
                    : movement.DefaultStopDistance;

            if (distance > fallbackRange * 0.9f)
            {
                movement.UpdateAggroMovement(
                    currentTargetStats,
                    fallbackRange
                );
            }
            else
            {
                movement.Stop();
                FaceCurrentTarget();
            }

            return;
        }

        AbilityTargetingSettings targeting =
            desiredAction.TargetingSettings;

        if (targeting == null)
        {
            movement.Stop();
            return;
        }

        bool isSelfTargeted =
            targeting.TargetingMode ==
            TargetingMode.Self;

        if (isSelfTargeted)
        {
            movement.Stop();

            TryStartNPCAction(
                desiredAction
            );

            return;
        }

        float desiredRange =
            Mathf.Max(
                targeting.Range,
                movement.DefaultStopDistance
            );

        if (distance > desiredRange * 0.9f)
        {
            movement.UpdateAggroMovement(
                currentTargetStats,
                desiredRange
            );

            return;
        }

        movement.Stop();
        FaceCurrentTarget();

        HandleAttack(
            desiredAction,
            distance
        );
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

        encounterResetInProgress = false;
        hasCombatAnchor = false;

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

    protected virtual void EnterFleeState(CharacterStats threat)
    {
        if (threat == null)
            return;

        SetupFlee(threat);

        ChangeState(AIState.Fleeing);
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

        encounterResetInProgress = true;

        bool shouldResumePatrol =
            wasPatrollingBeforeCombat &&
            canPatrol &&
            patrolPath != null &&
            patrolPath.points.Count > 0;

        actionController
            ?.ResetRuntimeState();

        abilityController
            ?.ResetRuntimeState();

        buffSystem
            ?.RemoveEncounterResetBuffs();

        selfStats
            ?.ResetEncounterState();

        reactionController
            ?.ResetEncounterState();

        movement?.Stop();

        fleeSource = null;
        currentTargetStats = null;

        if (subscribedPlayer != null)
        {
            subscribedPlayer.OnDied -=
                HandleTargetDied;

            subscribedPlayer = null;
        }

        /*
         * Hindrar NPC:n från att omedelbart kedje-aggra ett nytt mål
         * på nästa FixedUpdate.
         */
        aggroDisableTimer =
            Mathf.Max(
                aggroDisableTimer,
                reaggroCooldown
            );

        lastReturnTime =
            Time.time;

        isAggro = false;
        isReturning = false;

        encounterResetInProgress = false;
        hasCombatAnchor = false;

        if (shouldResumePatrol)
        {
            wasPatrollingBeforeCombat = false;

            EnterPatrolState(
                false
            );

            return;
        }

        wasPatrollingBeforeCombat = false;

        encounterResetInProgress = true;

        encounterReturnPosition =
            spawnPosition;

        EnterReturnState();
    }

    void HandleAggroDetection()
    {
        if (!canAggro)
            return;

        if (aggroDisableTimer > 0f)
            return;

        if (Time.time - lastReturnTime < reaggroCooldown)
            return;

        if (isAggro)
            return;

        NPCReactionController reaction = GetComponent<NPCReactionController>();

        bool isTemporaryHostile =
            reaction != null &&
            reaction.IsTemporarilyHostile;

        if (isReturning)
            return;

        Collider2D[] hits = Physics2D.OverlapCircleAll(
        transform.position,
        currentAggroRange,
        LayerMask.GetMask(
            "Player",
            "NPC",
            "HostileMob"
        ));

        foreach (var hit in hits)
        {
            CharacterStats target =hit.GetComponentInParent<CharacterStats>();

            if (target == null)
                continue;

            if (target == selfStats)
                continue;

            if (target == selfStats)
                continue;

            if (!ShouldAggro(target))
                continue;

            if (ShouldAggro(target))
            {
                EnterAggroState(target);
                break;
            }
        }
    }

    void UpdateFleeState()
    {
        if (currentState != AIState.Fleeing)
            return;

        if (movement.UpdateFlee())
        {
            movement.EndFlee();
            EnterHoldingState();
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
            EnterFleeState(fleeSource);
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

    void HandleAttack(
    AbilityData desiredAction,
    float distanceToTarget)
    {
        if (desiredAction == null)
            return;

        if (actionController == null)
            return;

        if (selfStats == null ||
            !selfStats.CanAct())
        {
            return;
        }

        if (!isAggro ||
            isReturning ||
            currentTargetStats == null)
        {
            return;
        }

        if (currentState == AIState.Fleeing ||
            currentState == AIState.Holding)
        {
            return;
        }

        AbilityTargetingSettings targeting =
            desiredAction.TargetingSettings;

        if (targeting == null)
            return;

        bool isSelfTargeted =
            targeting.TargetingMode ==
            TargetingMode.Self;

        if (!isSelfTargeted)
        {
            if (distanceToTarget >
                targeting.Range)
            {
                return;
            }

            if (distanceToTarget <
                targeting.MinimumRange)
            {
                TryFallbackBaseAttack(
                    distanceToTarget
                );

                return;
            }
        }

        bool started =
            TryStartNPCAction(
                desiredAction
            );

        if (!started &&
            !desiredAction.IsBaseAttack)
        {
            TryFallbackBaseAttack(
                distanceToTarget
            );
        }
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

    protected virtual void HandleDamaged(CharacterStats attacker)
    {
        // NPCReactionController styr reaktionen istället
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

    public void StartFleeing(CharacterStats threat)
    {
        if (threat == null)
            return;

        EnterFleeState(threat);
    }

    public void ResetAggro()
    {
        fleeSource = null;

        isAggro = false;
        isReturning = false;
        currentTargetStats = null;

        if (subscribedPlayer != null)
        {
            subscribedPlayer.OnDied -= HandleTargetDied;
            subscribedPlayer = null;
        }

        if (canWander)
            EnterWanderState();
        else
            EnterIdleState();
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
            selfStats.OnDamagedBy -= HandleDamaged;
            selfStats.OnDied -= HandleDeath;
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