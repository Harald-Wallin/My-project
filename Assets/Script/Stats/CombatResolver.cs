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
        Enemy enemy = stats.GetComponent<Enemy>();

        if (enemy != null)
            return enemy.monsterLevel;

        PlayerStats player =
            stats.GetComponent<PlayerStats>();

        if (player != null)
            return player.level;

        return 1;
    }
}