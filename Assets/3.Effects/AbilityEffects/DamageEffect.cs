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

    public int CalculateBaseDamage(
        CharacterStats caster,
        AbilityData ability = null,
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

        if (ability != null &&
            ability.ChargeSettings != null)
        {
            float chargeMultiplier =
                ability.ChargeSettings
                    .GetDamageMultiplier(
                        chargeProgress
                    );

            damage *=
                chargeMultiplier;
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
                context.Ability,
                context.ChargeProgress
            );

        if (damage <= 0)
            return;

        DamageSourceContext source =
            context.DamageSource;

        if (dealsRawDamage)
        {
            CombatResolver.DealRawDamage(
                source,
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
            source
        );
    }

    public override string GetTooltipText(
        CharacterStats caster)
    {
        /*
         * Tooltipen visar full-charge damage.
         *
         * AbilityEffect känner inte på egen hand till vilken
         * AbilityData som äger effekten, så chargeintervallet
         * kan senare presenteras av AbilityData-tooltipen.
         */
        int damage =
            CalculateBaseDamage(
                caster,
                null,
                1f
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
    }
#endif
}