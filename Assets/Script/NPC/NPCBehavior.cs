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
    private BaseAttackController baseAttackController;

    [Header("Flee")]
    [SerializeField] protected float fleeDistance = 12f; //Första panic-jumpen
    [SerializeField] protected float safeDistanceFromThreat = 18f; //När NPC slutar springa
    [SerializeField] protected float resumeFleeDistance = 12f; //Om spelaren kommer närmre än detta > fly igen
    [SerializeField] protected float maxHoldDistanceFromSpawn = 50f; //Förhindrar oändlig flykt
    protected Vector3 fleeTargetPosition;
    protected CharacterStats fleeSource;

    [Header("Death & Respawn")]
    [SerializeField] private GameObject corpsePrefab;
    [SerializeField] private MobSpawner spawner;
    private bool isDead = false;

    private bool handledDeath;

    private float aggroDisableTimer;
    //private bool wasMovingLastFrame;

    private bool wasPatrollingBeforeCombat;

    public bool IsInCombat => currentState == AIState.Aggro;
    protected AIState currentState = AIState.Idle;
    public AIState CurrentState => currentState;
    public CharacterStats selfStats;

    //public Vector2 CurrentFacingDirection { get; private set; } = Vector2.down;

    [Header("Re-aggro")]
    [SerializeField] private float reaggroCooldown = 3f;
    private float lastReturnTime = -999f;

    void Awake()
    {
        movement = GetComponent<NPCMovement>();
        baseAttackController = GetComponent<BaseAttackController>();

        spawnPosition = transform.position;

        abilityController = GetComponent<AbilityController>();

        selfStats = GetComponent<CharacterStats>();

        if (selfStats != null)
        {
            selfStats.OnDamagedBy += HandleDamaged;
            selfStats.OnDied += HandleDeath;
        }

        currentAggroRange = aggroRange;
    }

    protected virtual void Start()
    {
        if (player == null)
            player = PlayerReference.Player?.transform;

        movement.SetFacing(Vector2.down);

        if (canPatrol && patrolPath != null && patrolPath.points.Count > 0)
        {
            EnterPatrolState();
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

    public void SetPatrolPath(PatrolPath path)
    {
        patrolPath = path;

        if (canPatrol &&
            patrolPath != null &&
            patrolPath.points.Count > 0)
        {
            EnterPatrolState();
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

        float distanceFromSpawn = Vector2.Distance( transform.position, spawnPosition);

        HandleLeash(distanceFromSpawn);
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
                movement.BeginPatrol();
                movement.SetMovementMode(NPCMovement.NPCMovementMode.Patrol);
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
    protected virtual void EnterPatrolState()
    {
        SetupPatrol();

        ChangeState(AIState.Patrolling);
    }

    void UpdateAggroState()
    {
        if (currentTargetStats == null)
        {
            ReturnToSpawn();
            return;
        }

        if (currentTargetStats.currentHP <= 0)
        {
            ReturnToSpawn();
            return;
        }

        float distance = Vector2.Distance(
        transform.position,
        currentTargetStats.transform.position
        );

        if (distance > baseAttackController.CurrentAttackRange * 0.9f)
        {
            movement.UpdateAggroMovement(
                currentTargetStats,
                baseAttackController.CurrentAttackRange
            );
        }
        else
        {
            movement.Stop();
        }

        HandleAttack();
    }

    void SetupAggro(CharacterStats target)
    {
        currentTargetStats = target;

        player = target.transform;

        isAggro = true;

        isReturning = false;

        abilityLockTimer = abilityDelayAfterAggro;
    }

    protected virtual void EnterAggroState(CharacterStats target)
    {
        if (target == null)
        {
            return;
        }

        if (currentState == AIState.Aggro)
        {
            currentTargetStats = target;
            return;
        }

        //Sparar eventuell patrullstatus
        wasPatrollingBeforeCombat = currentState == AIState.Patrolling;

        ChangeState(AIState.Aggro);

        SetupAggro(target);

        PlayerStats ps = target as PlayerStats;

        if (ps != null)
        {
            SubscribeToPlayerDeath(ps);
        }
    }

    void UpdateReturnState()
    {
        movement.UpdateReturnMovement(spawnPosition);

        float distance =
            Vector2.Distance(
                transform.position,
                spawnPosition);

        if (distance <= movement.DefaultStopDistance)
        {
            if (canPatrol)
            {
                EnterPatrolState();
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

    void HandleLeash(float distanceFromSpawn)
    {
        if (canPatrol)
            return;

        if (currentState == AIState.Fleeing)
            return;

        if (!isReturning &&
            isAggro &&
            distanceFromSpawn > maxDistanceFromSpawn)
        {
            EnterReturnState();

            selfStats?.ResetHealth();
        }
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

    void HandleAttack()
    {
        if (!selfStats.CanAct())
            return;

        if (!isAggro || isReturning || currentTargetStats == null)
            return;

        if (currentState == AIState.Fleeing ||
            currentState == AIState.Holding)
        {
            return;
        }

        float distance =  Vector2.Distance(transform.position, currentTargetStats.transform.position);

        if (distance <= baseAttackController.CurrentAttackRange)
        {
            bool usedAbility = TryUseAbility();

            if (!usedAbility && baseAttackController != null)
            {
                baseAttackController.TryAttackTarget(currentTargetStats);
            }
        }
    }

    bool TryUseAbility()
    {
        if (abilityLockTimer > 0f)
            return false;

        if (abilityController == null)
            return false;

        var abilities = abilityController.GetEquippedAbilities();

        if (abilities == null || abilities.Length == 0)
            return false;

        foreach (var ability in abilities)
        {
            if (ability == null)
                continue;

            float cd = abilityController.GetCooldownRemaining(ability);

            if (cd > 0f)
                continue;

            float distance = Vector2.Distance(
                transform.position,
                currentTargetStats.transform.position
            );

            if (distance > baseAttackController.CurrentAttackRange)
                continue;

            abilityController.TryUseAbility(ability,currentTargetStats);

            return true;
        }

        return false;
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
        fleeSource = null;

        if (wasPatrollingBeforeCombat)
{
    wasPatrollingBeforeCombat = false;

    EnterPatrolState();

    return;
}

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

    void HandleTargetDied(CharacterStats deadTarget)
    {
        aggroDisableTimer = 2f;

        ReturnToSpawn();
    }

    void HandleDeath(CharacterStats deadCharacter)
    {
        if (isDead)
            return;

        isDead = true;

        DisableBehaviour();

        SpawnCorpse();

        if (spawner != null)
        {
            spawner.OnMobDied();
        }
    }

    void DisableBehaviour()
    {
        enabled = false;

        if (baseAttackController != null)
            baseAttackController.enabled = false;

        movement.Stop();
    }

    void SpawnCorpse()
    {
        if (corpsePrefab == null)
            return;

        GameObject corpse = Instantiate(
            corpsePrefab,
            transform.position,
            Quaternion.identity);

        CharacterStats corpseStats = corpse.GetComponent<CharacterStats>();

        LootContainer loot = corpse.GetComponent<LootContainer>();

        if (loot != null &&
            selfStats.deathReward != null)
        {
            selfStats.deathReward.GenerateLoot(loot);
        }

        if (corpseStats != null)
        {
            corpseStats.faction = null;
        }

        Transform nameplate =
            transform.Find("Nameplate");

        if (nameplate != null)
        {
            nameplate.SetParent(corpse.transform, true);

            NameplateUI ui =
                nameplate.GetComponentInChildren<NameplateUI>();

            if (ui != null)
            {
                ui.SetCorpseMode();
            }
        }
    }
}