using UnityEngine;

public static class CombatResolver
{
    /// <summary>
    /// Legacy/full damageväg som både slår hit och beräknar
    /// damage.
    /// </summary>
    public static int DealDamage(
        CharacterStats attacker,
        CharacterStats target,
        int baseDamage)
    {
        if (attacker == null ||
            target == null)
        {
            return 0;
        }

        DamageResult result =
            DamageCalculator.CalculateDamage(
                baseDamage,
                attacker.GetStat(
                    StatType.CritChance
                ),
                attacker.GetStat(
                    StatType.CritMultiplier
                ),
                attacker,
                target
            );

        return target.TakeDamage(
            result,
            attacker
        );
    }

    public static int DealRawDamage(
        CharacterStats attacker,
        CharacterStats target,
        int damage)
    {
        if (target == null)
            return 0;

        return target.TakeRawDamage(
            damage,
            attacker
        );
    }

    public static bool RollHit(
        CharacterStats attacker,
        CharacterStats target)
    {
        if (attacker == null ||
            target == null)
        {
            return false;
        }

        float hitChance =
            attacker.GetStat(
                StatType.HitChance
            );

        int levelDifference =
            GetLevel(target) -
            GetLevel(attacker);

        hitChance -=
            levelDifference * 0.07f;

        hitChance =
            Mathf.Clamp01(
                hitChance
            );

        return
            Random.value <=
            hitChance;
    }

    public static bool RollDodge(
        CharacterStats attacker,
        CharacterStats target)
    {
        if (attacker == null ||
            target == null)
        {
            return false;
        }

        float evasion =
            target.GetStat(
                StatType.Evasion
            );

        int levelDifference =
            GetLevel(target) -
            GetLevel(attacker);

        evasion +=
            levelDifference * 0.02f;

        evasion =
            Mathf.Clamp01(
                evasion
            );

        return
            Random.value <=
            evasion;
    }

    /// <summary>
    /// Damageberäkning för den nya effect-pipelinen.
    ///
    /// Hit och dodge måste redan ha avgjorts.
    /// </summary>
    public static DamageResult
        ResolveDamageAfterSuccessfulHit(
            CharacterStats attacker,
            CharacterStats target,
            int baseDamage,
            AbilityData ability)
    {
        if (attacker == null ||
            target == null)
        {
            return default;
        }

        bool canCrit =
            ability == null ||
            ability.canCrit;

        return DamageCalculator
            .CalculateDamageAfterSuccessfulHit(
                baseDamage,
                attacker.GetStat(
                    StatType.CritChance
                ),
                attacker.GetStat(
                    StatType.CritMultiplier
                ),
                attacker,
                target,
                canCrit
            );
    }

    /// <summary>
    /// Legacy-metod. Gör fortfarande både hit/dodge och damage.
    /// </summary>
    public static DamageResult ResolveEffectDamage(
        CharacterStats attacker,
        CharacterStats target,
        int baseDamage,
        AbilityData ability)
    {
        if (attacker == null ||
            target == null)
        {
            return default;
        }

        bool shouldRollHit =
            ability != null &&
            ability.canMiss &&
            !ability.alwaysHits;

        if (shouldRollHit)
        {
            if (!RollHit(
                    attacker,
                    target))
            {
                return new DamageResult
                {
                    isMiss = true
                };
            }

            if (RollDodge(
                    attacker,
                    target))
            {
                return new DamageResult
                {
                    isEvaded = true
                };
            }
        }

        return ResolveDamageAfterSuccessfulHit(
            attacker,
            target,
            baseDamage,
            ability
        );
    }

    /// <summary>
    /// Legacy-metod för äldre damagekod.
    /// </summary>
    public static DamageResult ResolveAbilityHit(
        CharacterStats attacker,
        CharacterStats target,
        int baseDamage)
    {
        if (attacker == null ||
            target == null)
        {
            return default;
        }

        return DamageCalculator.CalculateDamage(
            baseDamage,
            attacker.GetStat(
                StatType.CritChance
            ),
            attacker.GetStat(
                StatType.CritMultiplier
            ),
            attacker,
            target
        );
    }

    private static int GetLevel(
        CharacterStats stats)
    {
        return stats != null
            ? stats.level
            : 1;
    }
}