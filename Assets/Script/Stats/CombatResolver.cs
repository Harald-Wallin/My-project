using UnityEngine;

public static class CombatResolver
{
    public static int DealDamage(
        CharacterStats attacker,
        CharacterStats target,
        int baseDamage
    )
    {
        if (attacker == null || target == null)
            return 0;

        DamageResult result =
            DamageCalculator.CalculateDamage(
                baseDamage,
                attacker.GetStat(StatType.CritChance),
                attacker.GetStat(StatType.CritMultiplier),
                attacker,
                target
            );

        return target.TakeDamage(result, attacker);
    }

    public static int DealRawDamage(
        CharacterStats attacker,
        CharacterStats target,
        int damage
    )
    {
        if (target == null)
            return 0;

        return target.TakeRawDamage(
            damage,
            attacker
        );
    }

    // =========================
    // HIT ROLL
    // =========================

    public static bool RollHit(
        CharacterStats attacker,
        CharacterStats target
    )
    {
        float hitChance =
            attacker.GetStat(StatType.HitChance);

        int levelDiff =
            GetLevel(target) - GetLevel(attacker);

        hitChance -= levelDiff * 0.07f;

        hitChance = Mathf.Clamp01(hitChance);

        return Random.value <= hitChance;
    }

    // =========================
    // DODGE ROLL
    // =========================

    public static bool RollDodge(
        CharacterStats attacker,
        CharacterStats target
    )
    {
        float evasion =
            target.GetStat(StatType.Evasion);

        int levelDiff =
            GetLevel(target) - GetLevel(attacker);

        evasion += levelDiff * 0.02f;

        evasion = Mathf.Clamp01(evasion);

        return Random.value <= evasion;
    }

    static int GetLevel(CharacterStats stats)
    {
        if (stats == null)
            return 1;

        return stats.level;
    }

    public static DamageResult ResolveEffectDamage(
    CharacterStats attacker,
    CharacterStats target,
    int baseDamage,
    AbilityData ability
)
    {
        DamageResult result = new DamageResult();

        if (attacker == null || target == null)
            return result;

        bool shouldRollHit =
            ability != null &&
            ability.canMiss &&
            !ability.alwaysHits;

        // MISS
        if (shouldRollHit)
        {
            if (!RollHit(attacker, target))
            {
                result.isMiss = true;
                return result;
            }

            // EVADE
            if (RollDodge(attacker, target))
            {
                result.isEvaded = true;
                return result;
            }
        }

        float damage = baseDamage;

        // VARIATION
        damage *= Random.Range(0.8f, 1.2f);

        bool isCrit = false;

        bool canCrit =
            ability == null || ability.canCrit;

        if (canCrit)
        {
            float critChance =
                attacker.GetStat(StatType.CritChance);

            float critMultiplier =
                attacker.GetStat(StatType.CritMultiplier);

            isCrit = Random.value < critChance;

            if (isCrit)
            {
                damage *= critMultiplier;
            }
        }

        // ARMOR
        float armor =
            target.GetStat(StatType.Armor);

        damage *= 100f / (100f + armor);

        result.damage =
            Mathf.Max(1, Mathf.FloorToInt(damage));

        result.isCrit = isCrit;

        return result;
    }

    public static DamageResult ResolveAbilityHit(
    CharacterStats attacker,
    CharacterStats target,
    int baseDamage
)
    {
        return DamageCalculator.CalculateDamage(
            baseDamage,
            attacker.GetStat(StatType.CritChance),
            attacker.GetStat(StatType.CritMultiplier),
            attacker,
            target
        );
    }
}