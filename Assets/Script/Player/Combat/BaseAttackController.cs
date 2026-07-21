using UnityEngine;
using UnityEngine.EventSystems;
using System;

public class BaseAttackController : MonoBehaviour
{
    [Header("Indicator")]
    [SerializeField] private Transform attackIndicator;
    [SerializeField] private Transform attackOrigin;
    [SerializeField] private float indicatorDistance = 0.6f;

    private CharacterStats stats;

    private float cooldownTimer;

    public bool IsReady => cooldownTimer <= 0f;
    public bool IsOnCooldown => cooldownTimer > 0f;

    public BaseAttackData CurrentAttack =>
    collection != null
        ? collection.GetEquippedAttack()
        : null;

    public float CurrentAttackRange =>
        CurrentAttack != null
            ? CurrentAttack.range
            : 1f;

    public Vector2 CurrentDirection { get; private set; }
    private PlayerBaseAttackCollection collection;

    void Awake()
    {
        stats = GetComponent<CharacterStats>();
        collection = GetComponent<PlayerBaseAttackCollection>();
    }

    void Update()
    {
        TickCooldown();

        if (stats is PlayerStats)
        {
            UpdateDirection();

            UpdateIndicator();

            HandleInput();
        }
    }


    void TickCooldown()
    {
        if (cooldownTimer > 0f)
        {
            cooldownTimer -= Time.deltaTime;
        }
    }

    void HandleInput()
    {
        if (!Input.GetMouseButtonDown(0))
            return;

        if (EventSystem.current != null &&
            EventSystem.current.IsPointerOverGameObject())
            return;

        TryAttack();
    }

    public void TryAttack()
    {
        BaseAttackData attack = collection.GetEquippedAttack();

        if (attack == null)
            return;

        if (!stats.CanAct())
            return;

        if (!IsReady)
            return;

        CharacterStats target = FindTarget();

        if (target == null)
            return;

        if (!CombatTargeting.CanAttack(stats, target))
            return;

        attack.Use(stats, target);

        StartCooldown();
    }

    public bool TryAttackTarget(CharacterStats target)
    {
        BaseAttackData attack = collection.GetEquippedAttack();

        if (attack == null)
        return false;

        if (!stats.CanAct())
        return false;

        if (!IsReady)
        return false;

        if (target == null)
        {
            return false;
        }

        bool canAttack = CombatTargeting.CanAttack(stats, target);


        if (!canAttack)
        {
            return false;
        }

        float distance = Vector2.Distance(transform.position, target.transform.position);

        if (distance > attack.range)
        {
            return false;
        }

        attack.Use(stats, target);

        StartCooldown();

        return true;
    }

    void StartCooldown()
    {
        float attackSpeed =
            stats.GetStat(StatType.AttackSpeed);

        if (attackSpeed <= 0f)
            attackSpeed = 1f;

        cooldownTimer = 1f / attackSpeed;
    }

    CharacterStats FindTarget()
    {
        BaseAttackData attack = collection.GetEquippedAttack();

        if (attack == null)
            return null;

        Collider2D[] hits =
            Physics2D.OverlapCircleAll(
                transform.position,
                attack.range,
                LayerMask.GetMask("Hitbox")
            );

        Vector2 forward = GetAimDirection();

        float threshold =
            Mathf.Cos(
                attack.arcAngle * 0.5f * Mathf.Deg2Rad
            );

        foreach (var hit in hits)
        {
            CombatHitbox hitbox =
                hit.GetComponent<CombatHitbox>();

            if (hitbox == null)
                continue;

            CharacterStats target = hitbox.Owner;

            if (target == null)
                continue;

            if (target == stats)
                continue;

            if (!CombatTargeting.CanAttack(stats, target))
                continue;

            // 🔥 NU använder vi HITBOX-position
            Vector2 dirToTarget =
                ((Vector2)hit.transform.position -
                (Vector2)transform.position).normalized;

            float dot =
                Vector2.Dot(forward, dirToTarget);

            if (dot >= threshold)
            {
                return target;
            }
        }

        return null;
    }

    public float GetCooldownNormalized()
    {
        float attackSpeed =
            stats.GetStat(StatType.AttackSpeed);

        if (attackSpeed <= 0f)
            return 0f;

        float max = 1f / attackSpeed;

        return cooldownTimer / max;
    }

    public Vector2 GetAimDirection()
    {
        // PLAYER
        if (stats is PlayerStats)
        {
            Vector2 mouseWorld =
                Camera.main.ScreenToWorldPoint(
                    Input.mousePosition
                );

            return (
                mouseWorld -
                (Vector2)transform.position
            ).normalized;
        }

        // NPC
        NPCBehavior ai =
            GetComponent<NPCBehavior>();

        if (ai != null && ai.CurrentTarget != null)
        {
            return (
                (Vector2)ai.CurrentTarget.transform.position -
                (Vector2)transform.position
            ).normalized;
        }

        return Vector2.right;
    }

    /// <summary>
    /// 0 när attacken precis har använts.
    /// 1 när attacken är helt redo.
    /// </summary>
    public float GetReadinessNormalized()
    {
        if (IsReady)
            return 1f;

        float attackSpeed =
            stats != null
                ? stats.GetStat(
                    StatType.AttackSpeed
                )
                : 0f;

        if (attackSpeed <= 0f)
            attackSpeed = 1f;

        float cooldownDuration =
            1f / attackSpeed;

        if (cooldownDuration <= 0f)
            return 1f;

        float remainingNormalized =
            Mathf.Clamp01(
                cooldownTimer /
                cooldownDuration
            );

        return 1f -
               remainingNormalized;
    }

    void UpdateIndicator()
    {
        if (attackIndicator == null || attackOrigin == null)
            return;

        Vector3 mouseWorld =
            Camera.main.ScreenToWorldPoint(Input.mousePosition);

        mouseWorld.z = 0f;

        Vector2 direction =
            (mouseWorld - attackOrigin.position).normalized;

        // POSITION
        attackIndicator.position =
            attackOrigin.position +
            (Vector3)(direction * indicatorDistance);

        // ROTATION
        float angle =
            Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;

        attackIndicator.rotation =
            Quaternion.Euler(0f, 0f, angle - 90f);
    }

    void UpdateDirection()
    {
        Vector2 direction =
            GetAimDirection();

        if (direction.sqrMagnitude <= 0.0001f)
            return;

        CurrentDirection =
            direction.normalized;
    }
}
