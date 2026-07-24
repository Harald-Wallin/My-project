using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class NPCMovement : MonoBehaviour
{
    protected CharacterStats stats;
    protected HumanoidEquipment equipment;

    private HumanoidVisualController visualController;
    protected HumanoidEquipment npcEquipment;

    private Rigidbody2D rb;

    [Header("Object Avoidance")]

    [Tooltip("How close to the target the NPC stops before it stops moving.")]
        [SerializeField] private float stopDistance = 0.8f;
    [Tooltip("How far ahead the NPC looks for obstacles.")]
        [SerializeField] private float avoidanceProbeDistance = 1.5f;
    [Tooltip("The angle used when the NPC searches for alternative paths around an obstacle.")]
        [SerializeField] private float avoidanceAngle = 35f;
    [Tooltip("How long the NPC remembers a previously chosen avoidance direction.")]
        [SerializeField] private float avoidanceMemoryDuration = 1.5f;
    [Tooltip("How long the NPC must be stuck before it tries to find a new path.")]
        [SerializeField] private float stuckCheckTime = 0.5f;
    [Tooltip("The minimum movement required for the NPC not to be considered stuck.")]
        [SerializeField] private float stuckMovementThreshold = 0.1f;
    [Tooltip("How far to the side a temporary avoidance target is placed.")]
        [SerializeField] private float avoidanceTargetDistance = 2f;
    [Tooltip("How strongly the NPC prioritizes moving towards its target.")]
        [SerializeField] private float targetWeight = 1.0f;
    [Tooltip("How strongly the NPC prioritizes avoiding walls and other obstacles.")]
        [SerializeField] private float obstacleWeight = 2.0f;
    [Tooltip("How strongly the NPC prioritizes maintaining distance from other NPCs.")]
        [SerializeField] private float separationWeight = 1.2f;
    [Tooltip("Radius within which other NPCs affect separation.")]
        [SerializeField] private float separationRadius = 1.2f;
    [Tooltip("How long a detected obstacle continues to affect steering.")]
        [SerializeField] private float obstacleMemoryDuration = 0.8f;
    [SerializeField] private LayerMask avoidanceLayers;

    public float DefaultStopDistance => stopDistance;
    private Vector2 rememberedAvoidanceDirection;
    private float obstacleMemoryTimer;
    private bool hasObstacleMemory;

    private float stuckTimer;
    private Vector2 lastStuckPosition;

    private bool hasTemporaryAvoidanceTarget;
    private Vector3 temporaryAvoidanceTarget;

    private bool wasMovingLastFrame;

    [Header("Wander Settings")]
    [SerializeField] float wanderRadius = 3f;
    [SerializeField] float wanderMoveTime = 2f;
    [SerializeField] float wanderPauseTime = 2f;
    [SerializeField] float wanderSpeedMultiplier = 0.5f;
    private Vector2 wanderTarget;
    private float wanderTimer;
    private bool isWandering;
    private bool isPausing;

    //[Header("Flee Settings")]
    private bool isFleeing;
    private CharacterStats fleeSource;
    private float fleeDistance;
    private float safeDistance;

    [Header("Patrol Settings")]
    [SerializeField] protected float patrolSpeedMultiplier = 0.75f;
    private int patrolIndex = 0;
    private bool patrolForward = true;
    private float patrolWaitTimer = 0f;
    private bool waitingAtPatrolNode = false;

    public Vector2 CurrentFacingDirection { get; private set; }
        = Vector2.down;

    public Vector3 SpawnPosition { get; private set; }

    public enum NPCMovementMode
    {
        Default,
        Aggressive,
        Patrol,
        Wander,
        Flee
    }

    private NPCMovementMode movementMode = NPCMovementMode.Default;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();

        stats = GetComponent<CharacterStats>();

        visualController =
            GetComponentInChildren<HumanoidVisualController>();

        equipment =
            GetComponent<HumanoidEquipment>();

        lastStuckPosition =
            rb.position;

        SpawnPosition = transform.position;
    }

    public void SetMovementMode(NPCMovementMode mode)
    {
        movementMode = mode;
    }

    public void SetFacing(Vector2 direction)
    {
        if (direction.sqrMagnitude <= 0.001f)
            return;

        CurrentFacingDirection = direction;

        visualController?.SetFacing(CurrentFacingDirection);
    }


    public bool MoveTowards(Vector3 target, float speedMultiplier = 1f, float customStopDistance = -1f)
    {
        float stopDist = customStopDistance > 0f ? customStopDistance : stopDistance;

        if (hasTemporaryAvoidanceTarget)
        {
            target = temporaryAvoidanceTarget;

            float avoidanceDistance =
                Vector2.Distance(
                    rb.position,
                    temporaryAvoidanceTarget
                );

            if (avoidanceDistance <= stopDistance)
            {
                hasTemporaryAvoidanceTarget = false;
            }
        }

        if (stats.IsStunned)
            return false;

        float moveSpeed = stats.GetStat(StatType.MovementSpeed);

        if (moveSpeed <= 0f)
            return false;

        Vector2 toTarget = (Vector2)target - rb.position;
        float distance = toTarget.magnitude;

        if (distance <= stopDist)
        {
            visualController?.SetMoving(false);
            wasMovingLastFrame = false;
            return false;
        }

        Vector2 direction = GetSteeringDirection(toTarget.normalized);

        Vector2 desiredMove = direction * moveSpeed * speedMultiplier * Time.fixedDeltaTime;

        ContactFilter2D filter = new ContactFilter2D();
        filter.useLayerMask = true;
        filter.layerMask = LayerMask.GetMask(
        "World"
        );

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
            hasObstacleMemory = false;

            rb.MovePosition(rb.position + desiredMove);

            CurrentFacingDirection = direction;

            visualController?.SetFacing(CurrentFacingDirection);
            visualController?.SetMoving(true);

            wasMovingLastFrame = true;

            return true;
        }
        else
        {
            Vector2 hitNormal = hits[0].normal;

            float dot =
                Vector2.Dot(
                    desiredMove,
                    hitNormal);

            Vector2 slideMove =
                desiredMove -
                hitNormal * dot;

            if (slideMove.sqrMagnitude > 0.0001f)
            {
                RaycastHit2D[] slideHits =
                    new RaycastHit2D[1];

                int slideBlocked =
                    rb.Cast(
                        slideMove.normalized,
                        filter,
                        slideHits,
                        slideMove.magnitude + 0.02f);

                if (slideBlocked == 0)
                {
                    rb.MovePosition(
                        rb.position + slideMove);

                    Vector2 slideDir = slideMove.normalized;

                    CurrentFacingDirection = slideDir;
                    visualController?.SetFacing(CurrentFacingDirection);
                    visualController?.SetMoving(true);

                    wasMovingLastFrame = true;

                    return true;
                }
            }

        }

        CurrentFacingDirection = direction;
        visualController?.SetFacing(CurrentFacingDirection);

        CheckIfStuck(target);

        visualController?.SetMoving(false);
        wasMovingLastFrame = false;

        return false;
    }

    public void StartPatrol()
    {
        SetMovementMode(NPCMovementMode.Patrol);

        waitingAtPatrolNode = false;
        patrolWaitTimer = 0f;

        patrolForward = true;
        patrolIndex = 0;
    }

    public void ResumePatrol()
    {
        SetMovementMode(NPCMovementMode.Patrol);

        waitingAtPatrolNode = false;
        patrolWaitTimer = 0f;
    }

    public void EndPatrol()
    {
        waitingAtPatrolNode = false;

        patrolWaitTimer = 0f;
    }

    public void BeginWander()
    {
        SetMovementMode(NPCMovementMode.Wander);

        isWandering = true;
        isPausing = false;

        Vector2 randomDirection =
            Random.insideUnitCircle.normalized;

        float randomDistance =
            Random.Range(0.5f, wanderRadius);

        wanderTarget =
            (Vector2)SpawnPosition +
            randomDirection * randomDistance;

        wanderTimer = wanderMoveTime;
    }

    public void UpdateWander(Vector3 spawnPosition)
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

            if (wanderTimer <= 0f || Vector2.Distance(transform.position, wanderTarget) <= DefaultStopDistance)
            {
                isWandering = false;
                isPausing = true;
                wanderTimer = Random.Range(1f, wanderPauseTime);
            }
        }
    }

    public void EndWander()
    {
        isWandering = false;
        isPausing = false;
    }

    public void BeginFlee(
    CharacterStats source,
    float fleeDistance,
    float safeDistance)
    {
        if (source == null)
            return;

        fleeSource = source;

        this.fleeDistance = fleeDistance;
        this.safeDistance = safeDistance;

        isFleeing = true;

        SetMovementMode(NPCMovementMode.Flee);
    }

    public void EndFlee()
    {
        isFleeing = false;

        fleeSource = null;
    }

    public bool UpdateFlee()
    {
        if (!isFleeing)
            return true;

        if (fleeSource == null)
            return true;

        float distance =
            Vector2.Distance(
                transform.position,
                fleeSource.transform.position);

        if (distance >= safeDistance)
            return true;

        Vector2 fleeDirection =
            (
                (Vector2)transform.position -
                (Vector2)fleeSource.transform.position
            ).normalized;

        Vector3 fleeTarget =
            transform.position +
            (Vector3)(fleeDirection * fleeDistance);

        MoveTowards(fleeTarget);

        return false;
    }

    void CheckIfStuck(Vector3 finalTarget)
    {
        float movedDistance =
            Vector2.Distance(
                rb.position,
                lastStuckPosition
            );

        if (movedDistance > stuckMovementThreshold)
        {
            stuckTimer = 0f;
            lastStuckPosition = rb.position;
            return;
        }

        stuckTimer += Time.fixedDeltaTime;

        if (stuckTimer < stuckCheckTime)
            return;

        stuckTimer = 0f;
        lastStuckPosition = rb.position;

        GenerateTemporaryAvoidanceTarget(finalTarget);
    }

    private void GenerateTemporaryAvoidanceTarget(
    Vector3 finalTarget)
    {
        Vector2 toTarget =
            (Vector2)finalTarget -
            rb.position;

        if (toTarget.sqrMagnitude <
            0.0001f)
        {
            hasTemporaryAvoidanceTarget =
                false;

            return;
        }

        toTarget.Normalize();

        Vector2 left =
            new Vector2(
                -toTarget.y,
                toTarget.x
            );

        Vector2 right =
            new Vector2(
                toTarget.y,
                -toTarget.x
            );

        float leftClearance =
            GetDirectionClearance(
                left
            );

        float rightClearance =
            GetDirectionClearance(
                right
            );

        Vector2 chosenSide =
            leftClearance >= rightClearance
                ? left
                : right;

        temporaryAvoidanceTarget =
            rb.position +
            chosenSide *
            avoidanceTargetDistance +
            toTarget *
            (avoidanceTargetDistance * 0.35f);

        hasTemporaryAvoidanceTarget =
            true;
    }

    private float GetDirectionClearance(
    Vector2 direction)
    {
        RaycastHit2D hit =
            Physics2D.Raycast(
                rb.position,
                direction,
                avoidanceTargetDistance,
                avoidanceLayers
            );

        if (!hit)
        {
            return avoidanceTargetDistance;
        }

        if (hit.rigidbody == rb)
        {
            return avoidanceTargetDistance;
        }

        return hit.distance;
    }

    Vector2 GetSteeringDirection(Vector2 targetDirection)
    {
        if (hasObstacleMemory)
        {
            obstacleMemoryTimer -= Time.fixedDeltaTime;

            if (obstacleMemoryTimer <= 0f)
            {
                hasObstacleMemory = false;
            }
        }

        Vector2 steering = Vector2.zero;

        // Vikter beroende på AI-state
        float currentTargetWeight = targetWeight;
        float currentObstacleWeight = obstacleWeight;
        float currentSeparationWeight = separationWeight;

        switch (movementMode)
        {
            case NPCMovementMode.Aggressive:

                // Vill komma fram aggressivt.
                currentTargetWeight *= 2.0f;
                currentObstacleWeight *= 0.6f;
                currentSeparationWeight *= 0.65f;
                break;

            case NPCMovementMode.Patrol:

                currentObstacleWeight *= 1.0f;
                currentSeparationWeight *= 1.35f;
                break;

            case NPCMovementMode.Wander:

                currentObstacleWeight *= 1.1f;
                currentSeparationWeight *= 1.2f;
                break;

            case NPCMovementMode.Flee:

                currentObstacleWeight *= 1.6f;
                currentSeparationWeight *= 1.5f;
                break;

            case NPCMovementMode.Default:
            default:
                break;
        }

        // Målriktning
        steering += targetDirection * currentTargetWeight;


        // Obstacle Memory
        if (hasObstacleMemory)
        {
            steering += rememberedAvoidanceDirection * currentObstacleWeight;
        }

        // Nya hinder
        Vector2 obstacleForce =
            CalculateObstacleAvoidance(targetDirection);

        steering += obstacleForce * currentObstacleWeight;

        // Separation
        Vector2 separationForce =
            CalculateSeparationForce();

        steering += separationForce * currentSeparationWeight;

        //---------------------------------

        if (steering.sqrMagnitude < 0.001f)
            return targetDirection;

        return steering.normalized;
    }

    Vector2 CalculateObstacleAvoidance(Vector2 desiredDirection)
    {
        if (desiredDirection.sqrMagnitude < 0.001f)
            return Vector2.zero;

        desiredDirection.Normalize();

        Vector2 leftProbeDirection =
            RotateDirection(
                desiredDirection,
                avoidanceAngle
            );

        Vector2 rightProbeDirection =
            RotateDirection(
                desiredDirection,
                -avoidanceAngle
            );

        Vector2 avoidanceForce = Vector2.zero;

        avoidanceForce += CalculateProbeForce(
            desiredDirection,
            avoidanceProbeDistance,
            1.5f
        );

        avoidanceForce += CalculateProbeForce(
            leftProbeDirection,
            avoidanceProbeDistance * 0.85f,
            1f
        );

        avoidanceForce += CalculateProbeForce(
            rightProbeDirection,
            avoidanceProbeDistance * 0.85f,
            1f
        );

        if (avoidanceForce.sqrMagnitude < 0.001f)
            return Vector2.zero;

        Vector2 normalizedForce =
            avoidanceForce.normalized;

        RememberObstacleDirection(normalizedForce);

        return normalizedForce;
    }

    Vector2 CalculateProbeForce(Vector2 probeDirection,float probeDistance,float probeWeight)
    {
        RaycastHit2D hit =
            Physics2D.Raycast(
                rb.position,
                probeDirection,
                probeDistance,
                avoidanceLayers
            );

        if (!hit)
            return Vector2.zero;

        if (hit.rigidbody == rb)
            return Vector2.zero;

        float proximity =
            1f - Mathf.Clamp01(
                hit.distance / probeDistance
            );

        float strength =
            Mathf.Lerp(
                0.25f,
                1f,
                proximity
            );

        Vector2 awayFromObstacle =
            hit.normal;

        return awayFromObstacle *
               strength *
               probeWeight;
    }

    Vector2 RotateDirection(Vector2 direction,float angleDegrees)
    {
        float radians =
            angleDegrees * Mathf.Deg2Rad;

        float cos = Mathf.Cos(radians);
        float sin = Mathf.Sin(radians);

        return new Vector2(
            direction.x * cos -
            direction.y * sin,

            direction.x * sin +
            direction.y * cos
        ).normalized;
    }

    void RememberObstacleDirection(Vector2 direction)
    {
        rememberedAvoidanceDirection =
            direction.normalized;

        obstacleMemoryTimer =
            obstacleMemoryDuration;

        hasObstacleMemory = true;
    }

    private Vector2 CalculateSeparationForce()
    {
        Collider2D[] hits =
            Physics2D.OverlapCircleAll(
                rb.position,
                separationRadius,
                LayerMask.GetMask(
                    "NPC",
                    "HostileMob",
                    "Player"
                )
            );

        Vector2 totalForce =
            Vector2.zero;

        foreach (Collider2D hit in hits)
        {
            if (hit == null)
                continue;

            Rigidbody2D otherBody =
                hit.attachedRigidbody;

            if (otherBody == rb)
                continue;

            CharacterStats otherCharacter =
                hit.GetComponentInParent<
                    CharacterStats
                >();

            if (otherCharacter == stats)
                continue;

            Vector2 closestPoint =
                hit.ClosestPoint(
                    rb.position
                );

            Vector2 away =
                rb.position -
                closestPoint;

            float distance =
                away.magnitude;

            /*
             * Om NPC:n står exakt ovanpå colliderpunkten använder vi
             * skillnaden mellan transformpositionerna som reserv.
             */
            if (distance < 0.01f)
            {
                away =
                    rb.position -
                    (Vector2)hit.transform.position;

                distance =
                    away.magnitude;
            }

            if (distance < 0.01f)
                continue;

            float normalizedProximity =
                1f -
                Mathf.Clamp01(
                    distance /
                    separationRadius
                );

            float strength =
                normalizedProximity *
                normalizedProximity;

            totalForce +=
                away.normalized *
                strength;
        }

        return Vector2.ClampMagnitude(
            totalForce,
            1f
        );
    }

    public void UpdateAggroMovement(CharacterStats target, float attackRange)
    {
        if (target == null)
            return;

        MoveTowards(
            target.transform.position,
            1f,
            attackRange * 0.9f
        );
    }

    public void UpdateReturnMovement(Vector3 spawnPosition)
    {
        MoveTowards(spawnPosition);
    }
    public void UpdatePatrol(PatrolPath patrolPath)
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
            Stop();

            patrolWaitTimer -=
                Time.fixedDeltaTime;

            if (patrolWaitTimer <= 0f)
            {
                waitingAtPatrolNode = false;

                AdvancePatrolPoint(
                    patrolPath
                );
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

        if (distance <= DefaultStopDistance)
        {
            Stop();
            waitingAtPatrolNode = true;
            patrolWaitTimer = point.waitTime;
        }
    }

    void AdvancePatrolPoint(PatrolPath patrolPath)
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

    public void Stop()
    {
        rb.linearVelocity =
            Vector2.zero;

        hasTemporaryAvoidanceTarget =
            false;

        temporaryAvoidanceTarget =
            Vector3.zero;

        hasObstacleMemory =
            false;

        rememberedAvoidanceDirection =
            Vector2.zero;

        obstacleMemoryTimer =
            0f;

        stuckTimer =
            0f;

        lastStuckPosition =
            rb.position;

        visualController
            ?.SetMoving(
                false
            );

        wasMovingLastFrame =
            false;
    }
}