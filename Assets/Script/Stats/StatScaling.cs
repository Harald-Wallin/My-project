using UnityEngine;

public static class StatScaling
{
    public static void ApplyDerivedStats(CharacterStats stats)
    {
        ApplyStrength(stats);
        ApplyArmor(stats);
        ApplySwiftness(stats);
    }

    static void ApplyStrength(CharacterStats stats)
    {
        float strength =
            stats.GetBaseStatValue(StatType.Strength);

        stats.SetBaseStat(
            StatType.BaseMeleeDamage,
            2f + strength * 0.30f
        );
    }

    static void ApplyArmor(CharacterStats stats)
    {
        float armor =
            stats.GetBaseStatValue(StatType.Armor);

        stats.SetBaseStat(
            StatType.DamageReduction,
            armor * 0.15f
        );

        stats.SetBaseStat(
            StatType.MovementSpeed,
            Mathf.Max(
                0.5f,
                stats.GetBaseStatValue(StatType.MovementSpeed)
                - armor * 0.01f
            )
        );
    }

    static void ApplySwiftness(CharacterStats stats)
    {
        float swiftness =
            stats.GetBaseStatValue(StatType.Swiftness);

        stats.SetBaseStat(
            StatType.MovementSpeed,
            stats.GetBaseStatValue(StatType.MovementSpeed)
            + swiftness * 0.02f
        );

        stats.SetBaseStat(
            StatType.AttackSpeed,
            stats.GetBaseStatValue(StatType.AttackSpeed)
            + swiftness * 0.01f
        );

        stats.SetBaseStat(
            StatType.CritChance,
            stats.GetBaseStatValue(StatType.CritChance)
            + swiftness * 0.0003f
        );
    }
}
