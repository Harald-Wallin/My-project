using UnityEngine;
using UnityEngine.EventSystems;

[RequireComponent(typeof(CharacterStats))]
[RequireComponent(typeof(CharacterActionController))]
[RequireComponent(typeof(PlayerBaseAttackCollection))]
public sealed class BaseAttackController :
    MonoBehaviour
{
    [Header("Indicator")]

    [SerializeField]
    private Transform attackIndicator;

    [SerializeField]
    private Transform attackOrigin;

    [SerializeField]
    [Min(0f)]
    private float indicatorDistance = 0.6f;

    private CharacterStats stats;
    private CharacterActionController actionController;
    private PlayerBaseAttackCollection collection;

    public AbilityData CurrentAttack =>
        collection != null
            ? collection.GetEquippedAttack()
            : null;

    public bool IsReady
    {
        get
        {
            AbilityData attack =
                CurrentAttack;

            if (attack == null ||
                actionController == null)
            {
                return false;
            }

            return actionController
                       .GetCooldownRemaining(
                           attack
                       ) <= 0f;
        }
    }

    public bool IsOnCooldown =>
        !IsReady;

    public float CurrentAttackRange
    {
        get
        {
            AbilityData attack =
                CurrentAttack;

            if (attack == null ||
                attack.TargetingSettings == null)
            {
                return 1f;
            }

            return attack
                .TargetingSettings
                .Range;
        }
    }

    public Vector2 CurrentDirection
    {
        get;
        private set;
    } = Vector2.right;

    private void Awake()
    {
        stats =
            GetComponent<CharacterStats>();

        actionController =
            GetComponent<
                CharacterActionController
            >();

        collection =
            GetComponent<
                PlayerBaseAttackCollection
            >();
    }

    private void OnValidate()
    {
        indicatorDistance =
            Mathf.Max(
                0f,
                indicatorDistance
            );
    }

    private void Update()
    {
        if (!(stats is PlayerStats))
            return;

        UpdateDirection();
        UpdateIndicator();
        HandleInput();
    }

    private void HandleInput()
    {
        if (!Input.GetMouseButtonDown(0))
            return;

        if (EventSystem.current != null &&
            EventSystem.current
                .IsPointerOverGameObject())
        {
            return;
        }

        TryAttack();
    }

    /// <summary>
    /// Startar den utrustade base attacken genom det gemensamma
    /// actionsystemet.
    ///
    /// TargetResolver ansvarar för range, form, relationer,
    /// line of sight och val av targets.
    /// </summary>
    public bool TryAttack()
    {
        AbilityData attack =
            CurrentAttack;

        if (!CanUseAttack(attack))
            return false;

        Vector2 aimPoint =
            GetAimPoint();

        return actionController
            .TryStartAction(
                attack,
                aimPoint,
                null,
                CurrentDirection
            );
    }

    /// <summary>
    /// NPC/AI-väg eller annan kod som vill utföra base attacken
    /// mot ett uttryckligt target.
    /// </summary>
    public bool TryAttackTarget(
        CharacterStats target)
    {
        AbilityData attack =
            CurrentAttack;

        if (!CanUseAttack(attack))
            return false;

        if (target == null)
            return false;

        return actionController
            .TryStartAction(
                attack,
                target
            );
    }

    private bool CanUseAttack(
        AbilityData attack)
    {
        if (attack == null)
            return false;

        if (!attack.IsBaseAttack)
        {
            Debug.LogWarning(
                $"Ability '{attack.abilityName}' ligger i " +
                $"base attack-slotten men är inte markerad " +
                $"som BaseAttack.",
                this
            );

            return false;
        }

        if (!attack.UsesActionSettings)
        {
            Debug.LogWarning(
                $"Base attack '{attack.abilityName}' måste ha " +
                $"'Use Action Settings' aktiverat.",
                this
            );

            return false;
        }

        if (stats == null ||
            !stats.CanAct())
        {
            return false;
        }

        return actionController != null;
    }

    public float GetCooldownNormalized()
    {
        AbilityData attack =
            CurrentAttack;

        if (attack == null ||
            actionController == null)
        {
            return 0f;
        }

        float remaining =
            actionController
                .GetCooldownRemaining(
                    attack
                );

        float maximum =
            actionController
                .GetMaxCooldown(
                    attack
                );

        if (remaining <= 0f ||
            maximum <= 0f)
        {
            return 0f;
        }

        return Mathf.Clamp01(
            remaining / maximum
        );
    }

    /// <summary>
    /// 0 när attacken precis har använts.
    /// 1 när attacken är helt redo.
    /// </summary>
    public float GetReadinessNormalized()
    {
        return 1f -
               GetCooldownNormalized();
    }

    public Vector2 GetAimDirection()
    {
        if (stats is PlayerStats)
        {
            Camera mainCamera =
                Camera.main;

            if (mainCamera == null)
                return CurrentDirection;

            Vector2 mouseWorld =
                mainCamera.ScreenToWorldPoint(
                    Input.mousePosition
                );

            Vector2 direction =
                mouseWorld -
                (Vector2)transform.position;

            if (direction.sqrMagnitude <=
                0.0001f)
            {
                return CurrentDirection;
            }

            return direction.normalized;
        }

        NPCBehavior ai =
            GetComponent<NPCBehavior>();

        if (ai != null &&
            ai.CurrentTarget != null)
        {
            Vector2 direction =
                (Vector2)ai
                    .CurrentTarget
                    .transform
                    .position -
                (Vector2)transform.position;

            if (direction.sqrMagnitude >
                0.0001f)
            {
                return direction.normalized;
            }
        }

        return CurrentDirection;
    }

    private Vector2 GetAimPoint()
    {
        if (stats is PlayerStats)
        {
            Camera mainCamera =
                Camera.main;

            if (mainCamera != null)
            {
                return mainCamera
                    .ScreenToWorldPoint(
                        Input.mousePosition
                    );
            }
        }

        NPCBehavior ai =
            GetComponent<NPCBehavior>();

        if (ai != null &&
            ai.CurrentTarget != null)
        {
            return ai
                .CurrentTarget
                .transform
                .position;
        }

        float distance =
            Mathf.Max(
                CurrentAttackRange,
                0.01f
            );

        return (Vector2)transform.position +
               CurrentDirection * distance;
    }

    private void UpdateDirection()
    {
        Vector2 direction =
            GetAimDirection();

        if (direction.sqrMagnitude <=
            0.0001f)
        {
            return;
        }

        CurrentDirection =
            direction.normalized;
    }

    private void UpdateIndicator()
    {
        if (attackIndicator == null ||
            attackOrigin == null)
        {
            return;
        }

        Vector2 direction =
            CurrentDirection;

        attackIndicator.position =
            attackOrigin.position +
            (Vector3)(
                direction *
                indicatorDistance
            );

        float angle =
            Mathf.Atan2(
                direction.y,
                direction.x
            ) * Mathf.Rad2Deg;

        attackIndicator.rotation =
            Quaternion.Euler(
                0f,
                0f,
                angle - 90f
            );
    }
}