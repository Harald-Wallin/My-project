using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(NPCNavigationAgent))]
public class NPCMovement : MonoBehaviour
{
    // =========================================================
    // REFERENCES
    // =========================================================

    protected CharacterStats stats;
    protected HumanoidEquipment equipment;
    protected CharacterActionController actionController;

    private HumanoidVisualController visualController;

    private Rigidbody2D rb;
    private NPCNavigationAgent navigationAgent;

    // =========================================================
    // MOVEMENT
    // =========================================================

    [Header("Movement")]

    [SerializeField]
    [Min(0.01f)]
    [Tooltip(
        "Hur nära destinationen NPC:n behöver komma innan " +
        "den betraktas som nådd."
    )]
    private float stopDistance =
        0.8f;

    [SerializeField]
    [Min(0f)]
    [Tooltip(
        "Liten säkerhetsmarginal vid Rigidbody-casts mot World."
    )]
    private float skinWidth =
        0.02f;

    public float DefaultStopDistance =>
        stopDistance;

    // =========================================================
    // LOCAL SEPARATION
    // =========================================================

    [Header("Local Separation")]

    [SerializeField]
    [Min(0f)]
    [Tooltip(
        "Radie där andra karaktärer får påverka lokal separation."
    )]
    private float separationRadius =
        1.2f;

    [SerializeField]
    [Range(0f, 2f)]
    [Tooltip(
        "Hur starkt NPC:n försöker undvika att stå ovanpå " +
        "andra karaktärer."
    )]
    private float separationWeight =
        0.45f;

    private static int SeparationLayers =>
    LayerMask.GetMask(
        "NPC",
        "Player"
    );

    private readonly Collider2D[]
        separationBuffer =
            new Collider2D[16];

    [Header("Character Avoidance")]

    [SerializeField]
    [Min(0f)]
    [Tooltip(
    "Hur långt framför NPC:n dynamiska karaktärer " +
    "börjar påverka avoidance."
)]
    private float characterAvoidanceProbeDistance =
    1.4f;

    [SerializeField]
    [Min(0f)]
    [Tooltip(
        "Hur brett området framför NPC:n är där andra " +
        "karaktärer betraktas som blockerande."
    )]
    private float characterAvoidanceWidth =
        0.65f;

    [SerializeField]
    [Range(0f, 1f)]
    [Tooltip(
        "Hur starkt sidostyrningen får påverka navigationen."
    )]
    private float characterAvoidanceStrength =
        0.8f;

    // =========================================================
    // FLEE
    // =========================================================

    private bool isFleeing;

    private CharacterStats fleeSource;

    private float fleeDistance;
    private float safeDistance;

    private Vector2 fleeStartPosition;
    private Vector2 fleeTargetPosition;

    private float fleeSpeedMultiplier =
        1f;

    // =========================================================
    // WANDER
    // =========================================================

    [Header("Wander Settings")]

    [SerializeField]
    private float wanderRadius =
        3f;

    [SerializeField]
    private float wanderMoveTime =
        2f;

    [SerializeField]
    private float wanderPauseTime =
        2f;

    [SerializeField]
    private float wanderSpeedMultiplier =
        0.5f;

    private Vector2 wanderTarget;

    private float wanderTimer;

    private bool isWandering;
    private bool isPausing;

    // =========================================================
    // PATROL
    // =========================================================

    [Header("Patrol Settings")]

    [SerializeField]
    protected float patrolSpeedMultiplier =
        0.75f;

    private int patrolIndex;

    private bool patrolForward =
        true;

    private float patrolWaitTimer;

    private bool waitingAtPatrolNode;

    [SerializeField]
    [Min(0f)]
    [Tooltip(
    "Om patrol-noden är blockerad av en annan karaktär " +
    "och NPC:n redan är nära noden får noden räknas som nådd."
)]
    private float blockedPatrolNodeAcceptanceDistance =
    2f;

    [SerializeField]
    [Min(0f)]
    [Tooltip(
        "Radie runt patrol-noden där levande karaktärer " +
        "betraktas som att de blockerar noden."
    )]
    private float patrolNodeBlockRadius =
        0.9f;

    // =========================================================
    // STATE
    // =========================================================

    public Vector2 CurrentFacingDirection
    {
        get;
        private set;
    } = Vector2.down;

    public Vector3 SpawnPosition
    {
        get;
        private set;
    }

    public enum NPCMovementMode
    {
        Default,
        Aggressive,
        Patrol,
        Wander,
        Flee
    }

    private NPCMovementMode movementMode =
        NPCMovementMode.Default;

    // =========================================================
    // UNITY
    // =========================================================

    private void Awake()
    {
        rb =
            GetComponent<Rigidbody2D>();

        stats =
            GetComponent<CharacterStats>();

        visualController =
            GetComponentInChildren<
                HumanoidVisualController>();

        equipment =
            GetComponent<HumanoidEquipment>();

        actionController =
            GetComponent<
                CharacterActionController>();

        navigationAgent =
            GetComponent<
                NPCNavigationAgent>();

        SpawnPosition =
            transform.position;
    }

    private void OnValidate()
    {
        stopDistance =
            Mathf.Max(
                0.01f,
                stopDistance
            );

        skinWidth =
            Mathf.Max(
                0f,
                skinWidth
            );

        separationRadius =
            Mathf.Max(
                0f,
                separationRadius
            );

        separationWeight =
            Mathf.Max(
                0f,
                separationWeight
            );

        wanderRadius =
            Mathf.Max(
                0f,
                wanderRadius
            );

        wanderMoveTime =
            Mathf.Max(
                0f,
                wanderMoveTime
            );

        wanderPauseTime =
            Mathf.Max(
                0f,
                wanderPauseTime
            );

        wanderSpeedMultiplier =
            Mathf.Max(
                0f,
                wanderSpeedMultiplier
            );

        patrolSpeedMultiplier =
            Mathf.Max(
                0f,
                patrolSpeedMultiplier
            );

        characterAvoidanceProbeDistance =
             Mathf.Max(
                0f,
                characterAvoidanceProbeDistance
            );

        characterAvoidanceWidth =
            Mathf.Max(
                0f,
                characterAvoidanceWidth
            );

        characterAvoidanceStrength =
            Mathf.Clamp01(
                characterAvoidanceStrength
            );

        blockedPatrolNodeAcceptanceDistance =
            Mathf.Max(
                0f,
             blockedPatrolNodeAcceptanceDistance
            );

        patrolNodeBlockRadius =
            Mathf.Max(
                0f,
                patrolNodeBlockRadius
            );
    }

    // =========================================================
    // MODE / FACING
    // =========================================================

    public void SetMovementMode(
        NPCMovementMode mode)
    {
        movementMode =
            mode;
    }

    public void SetFacing(
        Vector2 direction)
    {
        if (direction.sqrMagnitude <=
            0.001f)
        {
            return;
        }

        CurrentFacingDirection =
            direction.normalized;

        visualController?.SetFacing(
            CurrentFacingDirection
        );

        equipment?.UpdateVisualDirection(
            CurrentFacingDirection
        );
    }

    // =========================================================
    // CORE MOVEMENT
    // =========================================================

    /// <summary>
    /// Gemensam movement-ingång för all NPC-rörelse.
    ///
    /// Destination:
    /// AI-systemets önskade world-position.
    ///
    /// NPCNavigationAgent avgör sedan om NPC:n kan gå direkt
    /// eller om A* behövs.
    /// </summary>
    public bool MoveTowards(
        Vector3 target,
        float speedMultiplier = 1f,
        float customStopDistance = -1f)
    {
        if (rb == null ||
            stats == null)
        {
            return false;
        }

        if (!stats.IsAlive ||
            stats.IsStunned)
        {
            StopVisualMovement();

            return false;
        }

        float stopDist =
            customStopDistance >= 0f
                ? customStopDistance
                : stopDistance;

        Vector2 targetPosition =
            target;

        float targetDistance =
            Vector2.Distance(
                rb.position,
                targetPosition
            );

        if (targetDistance <=
            stopDist)
        {
            StopVisualMovement();

            return false;
        }

        float actionMovementMultiplier =
            actionController != null
                ? actionController
                    .CurrentMovementMultiplier
                : 1f;

        if (actionMovementMultiplier <=
            0.0001f)
        {
            StopVisualMovement();

            return false;
        }

        float moveSpeed =
            stats.GetStat(
                StatType.MovementSpeed
            );

        moveSpeed *=
            actionMovementMultiplier;

        moveSpeed *=
            Mathf.Max(
                0f,
                speedMultiplier
            );

        if (moveSpeed <=
            0.0001f)
        {
            StopVisualMovement();

            return false;
        }

        // -----------------------------------------------------
        // NAVIGATION DIRECTION
        // -----------------------------------------------------

        Vector2 navigationDirection = Vector2.zero;

        bool hasNavigationDirection =
            navigationAgent != null &&
            navigationAgent
                .TryGetMovementDirection(
                    targetPosition,
                    out navigationDirection
                );

        if (!hasNavigationDirection)
        {
            if (navigationAgent != null)
            {
                Debug.Log(
                $"[NAV DEBUG] {name} NO DIRECTION | " +
                $"pos={rb.position} | " +
                $"target={targetPosition} | " +
                $"distance={targetDistance:F2} | " +
                $"hasDestination={navigationAgent.HasDestination} | " +
                $"hasPath={navigationAgent.HasPath}",
                this);

                StopVisualMovement();

                return false;
            }

            Vector2 directDirection =
                targetPosition -
                rb.position;

            if (directDirection.sqrMagnitude <=
                0.0001f)
            {
                StopVisualMovement();

                return false;
            }

            navigationDirection =
                directDirection.normalized;
        }

        // -----------------------------------------------------
        // LOCAL CHARACTER SEPARATION
        // -----------------------------------------------------

        Vector2 movementDirection =
    navigationDirection.normalized;

        movementDirection =
            ApplyCharacterAvoidance(
                movementDirection
            );

        movementDirection =
            ApplyLocalSeparation(
                movementDirection
            );

        if (movementDirection.sqrMagnitude <=
            0.0001f)
        {
            movementDirection =
                navigationDirection.normalized;
        }
        else
        {
            movementDirection.Normalize();
        }

        if (navigationAgent != null &&
            navigationAgent.CurrentRegion != null)
        {
            float probeDistance =
                Mathf.Max(
                    0.4f,
                    moveSpeed *
                    Time.fixedDeltaTime *
                    3f
                );

            Vector2 probeEnd =
                rb.position +
                movementDirection *
                probeDistance;

            bool separationDirectionClear =
                navigationAgent
                    .CurrentRegion
                    .IsDirectPathClear(
                        rb.position,
                        probeEnd
                    );

            if (!separationDirectionClear)
            {
                movementDirection =
                    navigationDirection.normalized;
            }
        }

        // -----------------------------------------------------
        // PHYSICAL MOVEMENT
        // -----------------------------------------------------

        Vector2 desiredMove =
            movementDirection *
            moveSpeed *
            Time.fixedDeltaTime;

        Vector2 safeMove =
            ResolveWorldCollision(
            desiredMove
            );

        if (safeMove.sqrMagnitude <=
            0.0000001f)
        {
            /*
             * Navigationen trodde att NPC:n hade en användbar
             * färdriktning, men den riktiga Rigidbody-collidern
             * kunde inte röra sig.
             *
             * Rapportera detta tillbaka till NavigationAgent så att
             * Direct Movement tillfälligt överges och A* tvingas fram.
             */
            navigationAgent
                ?.NotifyPhysicalMovementBlocked(
                    targetPosition
                );

            StopVisualMovement();

            return false;
        }

        rb.MovePosition(
            rb.position +
            safeMove
        );

        SetFacing(
            safeMove.normalized
        );

        visualController?.SetMoving(
            true
        );

        return true;
    }

    private Vector2 ApplyCharacterAvoidance(
    Vector2 navigationDirection)
    {
        if (rb == null ||
            navigationDirection.sqrMagnitude <=
                0.0001f ||
            characterAvoidanceProbeDistance <=
                0f ||
            characterAvoidanceStrength <=
                0f)
        {
            return navigationDirection;
        }

        navigationDirection.Normalize();

        Vector2 origin =
            rb.position;

        int hitCount =
            Physics2D.OverlapCircleNonAlloc(
                origin,
                characterAvoidanceProbeDistance,
                separationBuffer,
                SeparationLayers
            );

        if (hitCount <= 0)
            return navigationDirection;

        CharacterStats blockingCharacter =
            null;

        Vector2 blockerPosition =
            Vector2.zero;

        float bestForwardDistance =
            float.PositiveInfinity;

        for (int i = 0;
             i < hitCount;
             i++)
        {
            Collider2D hit =
                separationBuffer[i];

            if (hit == null)
                continue;

            CharacterStats other =
                hit.GetComponentInParent<
                    CharacterStats>();

            if (other == null ||
                other == stats ||
                !other.IsAlive)
            {
                continue;
            }

            Vector2 closestPoint =
                hit.ClosestPoint(
                    origin
                );

            Vector2 toOther =
                closestPoint -
                origin;

            if (toOther.sqrMagnitude <=
                0.0001f)
            {
                toOther =
                    (Vector2)other
                        .transform
                        .position -
                    origin;
            }

            float forwardDistance =
                Vector2.Dot(
                    toOther,
                    navigationDirection
                );

            if (forwardDistance <=
                    0f ||
                forwardDistance >
                    characterAvoidanceProbeDistance)
            {
                continue;
            }

            Vector2 lateral =
                toOther -
                navigationDirection *
                forwardDistance;

            float lateralDistance =
                lateral.magnitude;

            if (lateralDistance >
                characterAvoidanceWidth)
            {
                continue;
            }

            if (forwardDistance >=
                bestForwardDistance)
            {
                continue;
            }

            bestForwardDistance =
                forwardDistance;

            blockingCharacter =
                other;

            blockerPosition =
                closestPoint;
        }

        if (blockingCharacter == null)
            return navigationDirection;

        Vector2 leftDirection =
            new Vector2(
                -navigationDirection.y,
                navigationDirection.x
            );

        Vector2 rightDirection =
            -leftDirection;

        Vector2 toBlocker =
            blockerPosition -
            origin;

        float blockerSide =
            Vector2.Dot(
                toBlocker,
                leftDirection
            );

        Vector2 preferredSide =
            blockerSide >= 0f
                ? rightDirection
                : leftDirection;

        Vector2 alternateSide =
            -preferredSide;

        Vector2 preferredSteering =
            (
                navigationDirection +
                preferredSide *
                characterAvoidanceStrength
            ).normalized;

        Vector2 alternateSteering =
            (
                navigationDirection +
                alternateSide *
                characterAvoidanceStrength
            ).normalized;

        bool preferredClear =
            IsShortMovementDirectionClear(
                preferredSteering
            );

        if (preferredClear)
        {
            return preferredSteering;
        }

        bool alternateClear =
            IsShortMovementDirectionClear(
                alternateSteering
            );

        if (alternateClear)
        {
            return alternateSteering;
        }
        return navigationDirection;
    }

    private bool IsShortMovementDirectionClear(
    Vector2 direction)
    {
        if (direction.sqrMagnitude <=
            0.0001f)
        {
            return false;
        }

        direction.Normalize();

        float probeDistance =
            Mathf.Max(
                0.35f,
                characterAvoidanceProbeDistance *
                0.6f
            );

        Vector2 probeEnd =
            rb.position +
            direction *
            probeDistance;

        /*
         * Statisk World-clearance.
         */
        if (navigationAgent != null &&
            navigationAgent.CurrentRegion != null &&
            !navigationAgent
                .CurrentRegion
                .IsDirectPathClear(
                    rb.position,
                    probeEnd
                ))
        {
            return false;
        }

        return true;
    }

    // =========================================================
    // WORLD COLLISION SAFETY
    // =========================================================

    /// <summary>
    /// A* planerar runt World.
    ///
    /// Den här casten är endast sista fysisk säkerhetskontroll
    /// så att Rigidbody aldrig råkar klippa genom geometri.
    ///
    /// Den väljer INTE vägen.
    /// </summary>
    private Vector2 ResolveWorldCollision(
    Vector2 desiredMove)
    {
        if (desiredMove.sqrMagnitude <=
            0.0000001f)
        {
            return Vector2.zero;
        }

        ContactFilter2D filter =
            new ContactFilter2D
            {
                useLayerMask = true,

                layerMask =
                    LayerMask.GetMask(
                        "World"
                    ),

                useTriggers = false
            };

        RaycastHit2D[] hits =
            new RaycastHit2D[1];

        float distance =
            desiredMove.magnitude;

        int hitCount =
            rb.Cast(
                desiredMove.normalized,
                filter,
                hits,
                distance +
                skinWidth
            );

        if (hitCount == 0)
        {
            return desiredMove;
        }

        RaycastHit2D hit =
            hits[0];

        Vector2 hitNormal =
            hit.normal;

        float normalMovement =
            Vector2.Dot(
                desiredMove,
                hitNormal
            );

        if (normalMovement >= 0f)
        {
            return desiredMove;
        }

        Vector2 slideMove =
            desiredMove -
            hitNormal *
            normalMovement;

        if (slideMove.sqrMagnitude <=
            0.0000001f)
        {
            return Vector2.zero;
        }

        int slideHitCount =
            rb.Cast(
                slideMove.normalized,
                filter,
                hits,
                slideMove.magnitude +
                skinWidth
            );

        if (slideHitCount <= 0)
        {
            return slideMove;
        }

        return Vector2.zero;
    }

    private bool IsPatrolNodeBlockedByCharacter(
    Vector2 nodePosition)
    {
        if (patrolNodeBlockRadius <= 0f)
            return false;

        int hitCount =
            Physics2D.OverlapCircleNonAlloc(
                nodePosition,
                patrolNodeBlockRadius,
                separationBuffer,
                SeparationLayers
            );

        for (int i = 0;
             i < hitCount;
             i++)
        {
            Collider2D hit =
                separationBuffer[i];

            if (hit == null)
                continue;

            CharacterStats other =
                hit.GetComponentInParent<
                    CharacterStats>();

            if (other == null ||
                other == stats ||
                !other.IsAlive)
            {
                continue;
            }

            return true;
        }

        return false;
    }

    // =========================================================
    // LOCAL SEPARATION
    // =========================================================

    /// <summary>
    /// A* undviker statisk World-geometri.
    ///
    /// Separation hanterar dynamiska karaktärer lokalt.
    /// Den får endast påverka färdriktningen lite och får
    /// därför aldrig ersätta navigationens riktning.
    /// </summary>


    private Vector2 ApplyLocalSeparation(
        Vector2 navigationDirection)
    {
        if (separationRadius <= 0f ||
            separationWeight <= 0f)
        {
            return navigationDirection;
        }

        int hitCount =
            Physics2D.OverlapCircleNonAlloc(
                rb.position,
                separationRadius,
                separationBuffer,
                SeparationLayers
            );

        if (hitCount <= 0)
            return navigationDirection;

        Vector2 separation =
            Vector2.zero;

        int validNeighbours =
            0;

        for (int i = 0;
             i < hitCount;
             i++)
        {
            Collider2D hit =
                separationBuffer[i];

            if (hit == null)
                continue;

            CharacterStats other =
                hit.GetComponentInParent<
                    CharacterStats>();

            if (other == null ||
                other == stats)
            {
                continue;
            }

            Vector2 closestPoint =
                hit.ClosestPoint(
                    rb.position
                );

            Vector2 away =
                rb.position -
                closestPoint;

            float distance =
                away.magnitude;

            if (distance <=
                0.01f)
            {
                away =
                    rb.position -
                    (Vector2)other
                        .transform
                        .position;

                distance =
                    away.magnitude;
            }

            if (distance <=
                0.01f)
            {
                continue;
            }

            float proximity =
                1f -
                Mathf.Clamp01(
                    distance /
                    separationRadius
                );

            /*
             * Kvadratisk falloff:
             *
             * långt bort = nästan ingen påverkan
             * väldigt nära = starkare push
             */
            float strength =
                proximity *
                proximity;

            separation +=
                away.normalized *
                strength;

            validNeighbours++;
        }

        if (validNeighbours <= 0 ||
            separation.sqrMagnitude <=
            0.0001f)
        {
            return navigationDirection;
        }

        separation.Normalize();

        Vector2 result =
            navigationDirection +
            separation *
            separationWeight;

        if (result.sqrMagnitude <=
            0.0001f)
        {
            return navigationDirection;
        }

        return result.normalized;
    }

    // =========================================================
    // WANDER
    // =========================================================

    public void BeginWander()
    {
        SetMovementMode(
            NPCMovementMode.Wander
        );

        isWandering =
            true;

        isPausing =
            false;

        GenerateNewWanderTarget();

        wanderTimer =
            wanderMoveTime;
    }

    public void UpdateWander(
        Vector3 spawnPosition)
    {
        wanderTimer -=
            Time.fixedDeltaTime;

        if (isPausing)
        {
            StopVisualMovement();

            if (wanderTimer <= 0f)
            {
                isPausing =
                    false;

                isWandering =
                    true;

                GenerateNewWanderTarget(
                    spawnPosition
                );

                wanderTimer =
                    wanderMoveTime;
            }

            return;
        }

        if (!isWandering)
            return;

        MoveTowards(
            wanderTarget,
            wanderSpeedMultiplier
        );

        float distance =
            Vector2.Distance(
                transform.position,
                wanderTarget
            );

        if (wanderTimer <= 0f ||
            distance <=
            DefaultStopDistance)
        {
            isWandering =
                false;

            isPausing =
                true;

            Stop();

            wanderTimer =
                Random.Range(
                    1f,
                    Mathf.Max(
                        1f,
                        wanderPauseTime
                    )
                );
        }
    }

    private void GenerateNewWanderTarget()
    {
        GenerateNewWanderTarget(
            SpawnPosition
        );
    }

    private void GenerateNewWanderTarget(
        Vector3 center)
    {
        Vector2 randomDirection =
            Random.insideUnitCircle;

        if (randomDirection.sqrMagnitude <=
            0.0001f)
        {
            randomDirection =
                Vector2.right;
        }

        randomDirection.Normalize();

        float randomDistance =
            Random.Range(
                0.5f,
                Mathf.Max(
                    0.5f,
                    wanderRadius
                )
            );

        wanderTarget =
            (Vector2)center +
            randomDirection *
            randomDistance;
    }

    public void EndWander()
    {
        isWandering =
            false;

        isPausing =
            false;

        navigationAgent
            ?.ClearDestination();
    }

    // =========================================================
    // FLEE
    // =========================================================

    public void BeginFlee(
        CharacterStats source,
        float fleeDistance,
        float safeDistance,
        float speedMultiplier = 1f)
    {
        if (source == null ||
            rb == null)
        {
            return;
        }

        fleeSource =
            source;

        this.fleeDistance =
            Mathf.Max(
                0.1f,
                fleeDistance
            );

        this.safeDistance =
            Mathf.Max(
                0f,
                safeDistance
            );

        fleeSpeedMultiplier =
            Mathf.Max(
                0f,
                speedMultiplier
            );

        fleeStartPosition =
            rb.position;

        Vector2 threatPosition =
            TargetUtility
                .GetTargetPosition(
                    source.gameObject
                );

        Vector2 fleeDirection =
            rb.position -
            threatPosition;

        if (fleeDirection.sqrMagnitude <=
            0.0001f)
        {
            fleeDirection =
                CurrentFacingDirection;

            if (fleeDirection.sqrMagnitude <=
                0.0001f)
            {
                fleeDirection =
                    Vector2.right;
            }
        }

        fleeDirection.Normalize();

        fleeTargetPosition =
            rb.position +
            fleeDirection *
            this.fleeDistance;

        isFleeing =
            true;

        navigationAgent
            ?.ForceRepath();

        SetMovementMode(
            NPCMovementMode.Flee
        );
    }

    public bool UpdateFlee()
    {
        if (!isFleeing)
            return true;

        float travelledDistance =
            Vector2.Distance(
                fleeStartPosition,
                rb.position
            );

        float destinationDistance =
            Vector2.Distance(
                rb.position,
                fleeTargetPosition
            );

        bool travelledEnough =
            travelledDistance >=
            fleeDistance *
            0.95f;

        bool reachedDestination =
            destinationDistance <=
            DefaultStopDistance;

        if (travelledEnough ||
            reachedDestination)
        {
            Stop();

            return true;
        }

        MoveTowards(
            fleeTargetPosition,
            fleeSpeedMultiplier,
            DefaultStopDistance
        );

        return false;
    }

    public void EndFlee()
    {
        isFleeing =
            false;

        fleeSource =
            null;

        fleeSpeedMultiplier =
            1f;

        fleeStartPosition =
            Vector2.zero;

        fleeTargetPosition =
            Vector2.zero;

        navigationAgent
            ?.ClearDestination();
    }

    // =========================================================
    // AGGRO
    // =========================================================

    public void UpdateAggroMovement(
     CharacterStats target,
     float attackRange,
     bool forceApproach = false,
     float customStopDistance = -1f)
    {
        if (target == null)
            return;

        Vector2 targetPosition =
            TargetUtility.GetTargetPosition(
                target.gameObject
            );

        float desiredStopDistance;

        if (forceApproach)
        {
            desiredStopDistance =
                0.05f;
        }
        else if (customStopDistance >= 0f)
        {
            desiredStopDistance =
                customStopDistance;
        }
        else
        {
            desiredStopDistance =
                Mathf.Max(
                    0f,
                    attackRange * 0.9f
                );
        }

        MoveTowards(
            targetPosition,
            1f,
            desiredStopDistance
        );
    }

    public void UpdateAggroReposition(
    CharacterStats target)
    {
        if (target == null)
            return;
        MoveTowards(
            target.transform.position,
            1f,
            0f
        );
    }

    // =========================================================
    // RETURN
    // =========================================================

    public void UpdateReturnMovement(
        Vector3 returnPosition)
    {
        MoveTowards(
            returnPosition
        );
    }

    // =========================================================
    // PATROL
    // =========================================================

    public void StartPatrol()
    {
        SetMovementMode(
            NPCMovementMode.Patrol
        );

        waitingAtPatrolNode =
            false;

        patrolWaitTimer =
            0f;

        patrolForward =
            true;

        patrolIndex =
            0;

        navigationAgent
            ?.ClearDestination();
    }

    public void ResumePatrol()
    {
        SetMovementMode(
            NPCMovementMode.Patrol
        );

        waitingAtPatrolNode =
            false;

        patrolWaitTimer =
            0f;

        navigationAgent
            ?.ClearDestination();
    }

    public void EndPatrol()
    {
        waitingAtPatrolNode =
            false;

        patrolWaitTimer =
            0f;

        navigationAgent
            ?.ClearDestination();
    }

    public void UpdatePatrol(
        PatrolPath patrolPath)
    {
        if (patrolPath == null ||
            patrolPath.points == null ||
            patrolPath.points.Count == 0)
        {
            return;
        }

        patrolIndex =
            Mathf.Clamp(
                patrolIndex,
                0,
                patrolPath.points.Count - 1
            );

        PatrolPoint point =
            patrolPath.points[
                patrolIndex
            ];

        if (point == null)
            return;

        if (waitingAtPatrolNode)
        {
            StopVisualMovement();

            patrolWaitTimer -=
                Time.fixedDeltaTime;

            if (patrolWaitTimer <= 0f)
            {
                waitingAtPatrolNode =
                    false;

                AdvancePatrolPoint(
                    patrolPath
                );
            }

            return;
        }

        Vector2 nodePosition =
    point.transform.position;

        float distance =
            Vector2.Distance(
                transform.position,
                nodePosition
            );

        bool reachedNormally =
            distance <=
            DefaultStopDistance;

        bool blockedNearNode =
            distance <=
                blockedPatrolNodeAcceptanceDistance &&
            IsPatrolNodeBlockedByCharacter(
                nodePosition
            );

        if (reachedNormally ||
            blockedNearNode)
        {
            Stop();

            if (point.waitTime <= 0f)
            {
                AdvancePatrolPoint(
                    patrolPath
                );

                return;
            }

            waitingAtPatrolNode =
                true;

            patrolWaitTimer =
                point.waitTime;

            return;
        }

        MoveTowards(
            nodePosition,
            patrolSpeedMultiplier
        );
    }

    private void AdvancePatrolPoint(
        PatrolPath patrolPath)
    {
        if (patrolPath == null ||
            patrolPath.points == null ||
            patrolPath.points.Count <= 1)
        {
            patrolIndex =
                0;

            return;
        }

        if (patrolPath.patrolMode ==
            PatrolPath.PatrolMode.Loop)
        {
            patrolIndex++;

            if (patrolIndex >=
                patrolPath.points.Count)
            {
                patrolIndex =
                    0;
            }

            return;
        }

        if (patrolForward)
        {
            patrolIndex++;

            if (patrolIndex >=
                patrolPath.points.Count)
            {
                patrolIndex =
                    patrolPath.points.Count -
                    2;

                patrolForward =
                    false;
            }

            return;
        }

        patrolIndex--;

        if (patrolIndex < 0)
        {
            patrolIndex =
                1;

            patrolForward =
                true;
        }
    }

    // =========================================================
    // MOVEMENT STOP / HOLD
    // =========================================================

    /// <summary>
    /// Pausar den fysiska rörelsen utan att kasta bort
    /// NPC:ns navigation.
    ///
    /// Används exempelvis:
    /// - under attack/cast/recovery
    /// - medan NPC:n väntar på cooldown
    /// - när NPC:n tillfälligt står i en giltig combat-position
    ///
    /// Den aktuella A*-pathen och destinationen behålls.
    /// </summary>
    public void HoldPosition()
    {
        if (rb != null)
        {
            rb.linearVelocity =
                Vector2.zero;
        }

        StopVisualMovement();
    }

    /// <summary>
    /// Full movement-reset.
    ///
    /// NPC:n stannar och den nuvarande navigationen kastas bort.
    ///
    /// Används när:
    /// - AI-state faktiskt avslutas
    /// - NPC:n börjar return/reset
    /// - patrol/flee/navigation ska avbrytas helt
    /// </summary>
    public void Stop()
    {
        HoldPosition();

        navigationAgent
            ?.ClearDestination();
    }

    private void StopVisualMovement()
    {
        visualController
            ?.SetMoving(
                false
            );
    }
}