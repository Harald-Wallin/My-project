using UnityEngine;

[CreateAssetMenu(
    menuName = "RPG/Effects/Damage"
)]
public sealed class DamageEffect :
    AbilityEffect
{
    [Header("Damage")]

    [SerializeField]
    [Min(0)]
    private int flatDamage;

    [SerializeField]
    private DamageScalingEntry[] scaling;

    [Header("Rules")]

    [SerializeField]
    [Tooltip(
        "Ignorerar armor, block, crit och normal " +
        "damageberäkning."
    )]
    private bool dealsRawDamage;

    [SerializeField]
    [Tooltip(
        "Om charge-progress ska multiplicera skadan. " +
        "Vid full charge används multiplier 1."
    )]
    private bool scalesWithCharge;

    [SerializeField]
    [Min(0f)]
    private float minimumChargeMultiplier = 0.25f;

    public int CalculateBaseDamage(
        CharacterStats caster,
        float chargeProgress = 1f)
    {
        float damage =
            Mathf.Max(
                0,
                flatDamage
            );

        if (scaling != null)
        {
            for (int i = 0;
                 i < scaling.Length;
                 i++)
            {
                DamageScalingEntry entry =
                    scaling[i];

                if (entry == null)
                    continue;

                damage +=
                    entry.Evaluate(
                        caster
                    );
            }
        }

        if (scalesWithCharge)
        {
            float chargeMultiplier =
                Mathf.Lerp(
                    Mathf.Max(
                        0f,
                        minimumChargeMultiplier
                    ),
                    1f,
                    Mathf.Clamp01(
                        chargeProgress
                    )
                );

            damage *= chargeMultiplier;
        }

        return Mathf.Max(
            0,
            Mathf.RoundToInt(
                damage
            )
        );
    }

    public override void Execute(
        AbilityEffectExecutionContext context)
    {
        if (context == null ||
            context.Caster == null ||
            context.Target == null)
        {
            return;
        }

        if (!context.TargetWasSuccessful)
            return;

        int damage =
            CalculateBaseDamage(
                context.Caster,
                context.ChargeProgress
            );

        if (damage <= 0)
            return;

        if (dealsRawDamage)
        {
            CombatResolver.DealRawDamage(
                context.Caster,
                context.Target,
                damage
            );

            return;
        }

        DamageResult result =
            CombatResolver
                .ResolveDamageAfterSuccessfulHit(
                    context.Caster,
                    context.Target,
                    damage,
                    context.Ability
                );

        context.Target.TakeDamage(
            result,
            context.Caster
        );
    }

    public override string GetTooltipText(
        CharacterStats caster)
    {
        int damage =
            CalculateBaseDamage(
                caster
            );

        return dealsRawDamage
            ? $"Deals {damage} raw damage"
            : $"Deals approximately {damage} damage";
    }

#if UNITY_EDITOR
    protected override void OnValidate()
    {
        base.OnValidate();

        flatDamage =
            Mathf.Max(
                0,
                flatDamage
            );

        minimumChargeMultiplier =
            Mathf.Max(
                0f,
                minimumChargeMultiplier
            );
    }
#endif
}
