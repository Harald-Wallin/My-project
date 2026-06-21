using UnityEngine;
using UnityEngine.EventSystems;

public class AgressiveMobAI : MonoBehaviour
{
    // --- Sprites ---
    private HumanoidVisualController visualController;

    [Header("Refs")]
    public Transform player;
    protected HumanoidEquipment npcEquipment;
    private PlayerStats subscribedPlayer;

    [Header("Leash")]
    [SerializeField] protected float maxDistanceFromSpawn = 8f;
    [SerializeField] float stopDistance = 0.8f;

    [Header("Attack")]
    private AbilityController abilityController;
    [SerializeField] float attackRange = 0.9f;

    [Header("Ability Delay")]
    [SerializeField] private float abilityDelayAfterAggro = 3f;

    private float abilityLockTimer;

    [Header("Movement")]
    public float moveSpeed = 2.5f;
    private SpriteRenderer sr;
    private Rigidbody2D rb;

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
    [SerializeField] float wanderRadius = 3f;
    [SerializeField] float wanderMoveTime = 2f;
    [SerializeField] float wanderPauseTime = 2f;
    [SerializeField] float wanderSpeedMultiplier = 0.5f;

    [Header("Patrol")]
    [SerializeField]
    protected bool canPatrol = false;

    [SerializeField]
    protected PatrolPath patrolPath;

    [SerializeField]
    protected float patrolSpeedMultiplier = 0.75f;
    private BaseAttackController baseAttackController;

    [Header("Flee")]
    [SerializeField]
    protected float fleeDistance = 12f; //Första panic-jumpen

    [SerializeField]
    protected float safeDistanceFromThreat = 18f; //När NPC slutar springa

    [SerializeField]
    protected float resumeFleeDistance = 12f; //Om spelaren kommer närmre än detta > fly igen

    [SerializeField]
    protected float maxHoldDistanceFromSpawn = 50f; //Förhindrar oändlig flykt


    protected Vector3 fleeTargetPosition;
    protected CharacterStats fleeSource;
    private Vector2 wanderTarget;
    private float wanderTimer;
    private bool isWandering;
    private bool isPausing;
    private float aggroDisableTimer;
    private bool wasMovingLastFrame;

    //patrol
    private int patrolIndex = 0;
    private bool patrolForward = true;
    private float patrolWaitTimer = 0f;
    private bool waitingAtPatrolNode = false;
    private Vector3 interruptedPatrolPosition;
    private bool returnToPatrolAfterCombat;

    public bool IsInCombat => currentState == AIState.Aggro;
    protected AIState currentState = AIState.Idle;
    public AIState CurrentState => currentState;
    public CharacterStats selfStats;

    public Vector2 CurrentFacingDirection { get; private set; } = Vector2.down;

    [Header("Re-aggro")]
    [SerializeField] private float reaggroCooldown = 3f;
    private float lastReturnTime = -999f;

    void Awake()
    {
        visualController = GetComponentInChildren<HumanoidVisualController>();
        sr = GetComponentInChildren<SpriteRenderer>();
        rb = GetComponent<Rigidbody2D>();
        baseAttackController = GetComponent<BaseAttackController>();
        npcEquipment = GetComponent<HumanoidEquipment>();

        spawnPosition = transform.position;

        abilityController = GetComponent<AbilityController>();

        wanderTimer = Random.Range(1f, wanderPauseTime);
        isPausing = true;

        selfStats = GetComponent<CharacterStats>();
        currentAggroRange = aggroRange;

        if (selfStats != null)
        {
            selfStats.OnDamagedBy += HandleDamaged;
        }
    }

    protected virtual void Start()
    {
        if (player == null)
            player = PlayerReference.Player?.transform;

        Vector2 initialDir = Vector2.down;
        CurrentFacingDirection = initialDir;

        npcEquipment?.UpdateVisualDirection(initialDir);

        HumanoidVisualController visual = GetComponentInChildren<HumanoidVisualController>();

        if (visual != null)
            visual.UpdateSkinDirection(initialDir);

        if (canPatrol && patrolPath != null && patrolPath.points.Count > 0)
        {
            currentState = AIState.Patrolling;
        }
        else if (canWander)
        {
            currentState = AIState.Wandering;
        }
        else
        {
            currentState = AIState.Idle;
        }
    }

    void FixedUpdate()
    {
        if (aggroDisableTimer > 0f)
        {
            aggroDisableTimer -= Time.fixedDeltaTime;
        }

        if (
            currentTargetStats != null &&
            currentTargetStats.currentHP <= 0 &&
            !isReturning
            )
        {
            ReturnToSpawn();
        }

        if (abilityLockTimer > 0f)
        {
            abilityLockTimer -= Time.fixedDeltaTime;
        }

        if (player == null)
        {
            if (isReturning)
            {
                HandleMovement();
                HandleIdleAnimation();
                HandlePatrolReturn();
            }

            return;
        }

        float distanceFromSpawn = Vector2.Distance(rb.position, spawnPosition);

        HandleLeash(distanceFromSpawn);
        HandleAggroDetection();
        HandleMovement();
        HandleIdleAnimation();
        HandleFleeState();
        HandleHoldingState();
        HandleAttack();
    }

    protected virtual void EnterIdleState()
    {
        isAggro = false;
        isReturning = false;

        currentTargetStats = null;

        currentState = AIState.Idle;
    }

    protected virtual void EnterWanderState()
    {
        isAggro = false;
        isReturning = false;

        currentTargetStats = null;

        currentState = AIState.Wandering;
    }

    protected virtual void EnterAggroState(CharacterStats target)
    {
        //Debug.Log($"{name} ENTER AGGRO -> {target.name}");

        if (target == null)
            return;

        //Sparar eventuell patrullstatus
        if (currentState == AIState.Patrolling)
        {
            interruptedPatrolPosition =
                transform.position;

            returnToPatrolAfterCombat = true;
        }

        currentTargetStats = target;
        player = target.transform;

        isAggro = true;
        isReturning = false;

        currentState = AIState.Aggro;

        abilityLockTimer = abilityDelayAfterAggro;

        PlayerStats ps = target as PlayerStats;

        if (ps != null)
        {
            SubscribeToPlayerDeath(ps);
        }
    }

    protected virtual void EnterReturnState()
    {
        isAggro = false;
        isReturning = true;

        currentTargetStats = null;

        currentState = AIState.Returning;

        lastReturnTime = Time.time;

        if (subscribedPlayer != null)
        {
            subscribedPlayer.OnDied -= HandleTargetDied;
            subscribedPlayer = null;
        }
    }

    protected virtual void EnterFleeState(CharacterStats threat)
    {
        if (threat == null)
            return;

        fleeSource = threat;

        isAggro = false;
        isReturning = false;

        currentTargetStats = null;

        currentState = AIState.Fleeing;
    }

    protected virtual void EnterHoldingState()
    {
        isAggro = false;
        isReturning = false;

        currentTargetStats = null;

        currentState = AIState.Holding;

        rb.linearVelocity = Vector2.zero;
    }

    void HandleLeash(float distanceFromSpawn)
    {
        if (currentState == AIState.Fleeing)
            return;

        if (!isReturning && isAggro && distanceFromSpawn > maxDistanceFromSpawn)
        {
            EnterReturnState();
            GetComponent<Enemy>()?.ResetHealth();
        }

        if (isReturning && distanceFromSpawn <= stopDistance)
        {
            if (canWander)
                EnterWanderState();
            else
                EnterIdleState();
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

        NPCReactionController reaction =
            GetComponent<NPCReactionController>();

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

            //if (target.faction == selfStats.faction)
            //   continue;
            if (target == selfStats)
                continue;

            if (!ShouldAggro(target))
                continue;

            // Debug.Log($"{name} sees {target.name} | same faction = {selfStats.faction == target.faction}");
            // Debug.Log($"{name} ShouldAggro({target.name}) = {ShouldAggro(target)}");

            if (ShouldAggro(target))
            {
                EnterAggroState(target);
                break;
            }
        }
    }

    void HandleMovement()
    {
        if (currentState == AIState.Returning)
        {
            MoveTowards(spawnPosition);
        }
        else if (currentState == AIState.Aggro && currentTargetStats != null)
        {
            MoveTowards(currentTargetStats.transform.position);
        }
        else if (currentState == AIState.Wandering)
        {
            WanderLogic();
        }
        else if (currentState == AIState.Patrolling)
        {
            PatrolLogic();
        }
        else if (currentState == AIState.PatrolReturning)
        {
            MoveTowards(interruptedPatrolPosition);
        }
    }

    void HandlePatrolReturn()
    {
        if (currentState != AIState.PatrolReturning)
            return;

        float distance =
            Vector2.Distance(
                transform.position,
                interruptedPatrolPosition
            );

        if (distance <= stopDistance)
        {
            returnToPatrolAfterCombat = false;

            currentState = AIState.Patrolling;
        }
    }

    void HandleFleeState()
    {
        if (currentState != AIState.Fleeing)
            return;

        if (fleeSource == null)
        {
            EnterHoldingState();
            return;
        }

        float distanceToThreat =
            Vector2.Distance(
                transform.position,
                fleeSource.transform.position
            );

        if (distanceToThreat >= safeDistanceFromThreat)
        {
            EnterHoldingState();
            return;
        }

        Vector2 awayDirection =
            (
                (Vector2)transform.position -
                (Vector2)fleeSource.transform.position
            ).normalized;

        fleeTargetPosition =
            transform.position +
            (Vector3)(awayDirection * fleeDistance);

        MoveTowards(fleeTargetPosition);
    }

    void HandleHoldingState()
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

        float distance = Vector2.Distance(
            rb.position,
            currentTargetStats.transform.position
        );

        if (distance <= attackRange)
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

            if (distance > attackRange)
                continue;

            abilityController.TryUseAbility(ability,currentTargetStats);

            return true;
        }

        return false;
    }


    protected virtual void HandleDamaged(CharacterStats attacker)
    {
        //Debug.Log($"{name} AgressiveMobAI.HandleDamaged triggered.");

        // NPCReactionController styr reaktionen istället
    }

    protected virtual bool ShouldAggro(CharacterStats potentialTarget)
    {
        if (potentialTarget == null)
            return false;

        return CombatTargeting.CanAttack(
            selfStats,
            potentialTarget
        );
    }

    void MoveTowards(Vector3 target, float speedMultiplier = 1f)
    {
        if (selfStats.IsStunned)
            return;

        float moveSpeedStat = selfStats.GetStat(StatType.MovementSpeed);

        if (moveSpeedStat <= 0f)
            return;

        Vector2 toTarget = (Vector2)target - rb.position;
        float distance = toTarget.magnitude;

        if (distance <= stopDistance)
            return;

        Vector2 direction = toTarget.normalized;

        Vector2 desiredMove =
        direction * moveSpeed * speedMultiplier * moveSpeedStat * Time.fixedDeltaTime;

        ContactFilter2D filter = new ContactFilter2D();
        filter.useLayerMask = true;
        filter.layerMask = LayerMask.GetMask("Blocked", "Enemy", "Player");
        filter.useTriggers = false;

        RaycastHit2D[] hits = new RaycastHit2D[1];

        int hitCount = rb.Cast(
            direction,
            filter,
            hits,
            desiredMove.magnitude + 0.02f
        );

        if (hitCount == 0)
        {
            rb.MovePosition(rb.position + desiredMove);
        }

        if (npcEquipment != null)
            npcEquipment.UpdateVisualDirection(direction);

        if (visualController != null)
        {
            visualController.UpdateSkinDirection(direction);
        }

        CurrentFacingDirection = direction;
        UpdateVisualAnimation(true);
        wasMovingLastFrame = true;
    }

    void HandleIdleAnimation()
    {
        bool shouldBeMoving =
            currentState == AIState.Aggro ||
            currentState == AIState.Returning ||
            currentState == AIState.Patrolling ||
            currentState == AIState.PatrolReturning ||
            isWandering;

        if (!shouldBeMoving && wasMovingLastFrame)
        {
            UpdateVisualAnimation(false);
            wasMovingLastFrame = false;
        }
    }

    void WanderLogic()
    {
        wanderTimer -= Time.fixedDeltaTime;

        if (isPausing)
        {
            if (wanderTimer <= 0f)
            {
                isPausing = false;
                isWandering = true;

                Vector2 randomDirection = Random.insideUnitCircle.normalized;
                float randomDistance = Random.Range(0.5f, wanderRadius);

                wanderTarget = (Vector2)spawnPosition + randomDirection * randomDistance;
                wanderTimer = wanderMoveTime;
            }

            return;
        }

        if (isWandering)
        {
            MoveTowards(wanderTarget, wanderSpeedMultiplier);

            if (wanderTimer <= 0f ||
                Vector2.Distance(rb.position, wanderTarget) <= stopDistance)
            {
                isWandering = false;
                isPausing = true;
                wanderTimer = Random.Range(1f, wanderPauseTime);
            }
        }
    }

    public bool IsAggroOnPlayer()
    {
        if (currentState != AIState.Aggro)
            return false;

        return currentTargetStats is PlayerStats;
    }

    public void ForceAggro(CharacterStats target)
    {

        //Debug.Log($"{name} FORCE AGGRO -> {target.name}");

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

        if (returnToPatrolAfterCombat)
        {
            currentState =
                AIState.PatrolReturning;

            return;
        }

        EnterReturnState();
    }

    void OnDestroy()
    {
        if (selfStats != null)
        {
            selfStats.OnDamagedBy -= HandleDamaged;
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

    void PatrolLogic()
    {
        if (patrolPath == null)
            return;

        if (patrolPath.points.Count == 0)
            return;

        PatrolPoint point = patrolPath.points[patrolIndex];

        if (point == null)
            return;

        if (waitingAtPatrolNode)
        {
            patrolWaitTimer -= Time.fixedDeltaTime;

            if (patrolWaitTimer <= 0f)
            {
                waitingAtPatrolNode = false;
                AdvancePatrolPoint();
            }

            return;
        }

        MoveTowards(
            point.transform.position,
            patrolSpeedMultiplier
        );

        float distance =
            Vector2.Distance(
                transform.position,
                point.transform.position
            );

        if (distance <= stopDistance)
        {
            waitingAtPatrolNode = true;
            patrolWaitTimer = point.waitTime;
        }
    }

    void AdvancePatrolPoint()
    {
        if (patrolPath.patrolMode ==
            PatrolPath.PatrolMode.Loop)
        {
            patrolIndex++;

            if (patrolIndex >= patrolPath.points.Count)
                patrolIndex = 0;

            return;
        }

        if (patrolForward)
        {
            patrolIndex++;

            if (patrolIndex >= patrolPath.points.Count)
            {
                patrolIndex =
                    patrolPath.points.Count - 2;

                patrolForward = false;
            }
        }
        else
        {
            patrolIndex--;

            if (patrolIndex < 0)
            {
                patrolIndex = 1;
                patrolForward = true;
            }
        }
    }

    void UpdateVisualAnimation(bool moving)
    {
        if (visualController == null)
            return;

        if (moving)
        {
            visualController.SetAnimationState(
                HumanoidAnimationState.Walk
            );
        }
        else
        {
            visualController.SetAnimationState(
                HumanoidAnimationState.Idle
            );
        }

        visualController.UpdateSkinDirection(
            CurrentFacingDirection
        );
    }
}