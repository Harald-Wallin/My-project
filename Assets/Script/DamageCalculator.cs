using UnityEditor.Experimental.GraphView;
using UnityEngine;
using static UnityEngine.GraphicsBuffer;

public struct DamageResult
{
    public int damage;

    public bool isCrit;
    public bool isMiss;
    public bool isEvaded;

    public bool isBlocked;
    public int blockedAmount;
}

public static class DamageCalculator
{
    public static DamageResult CalculateDamage(
    int baseDamage,
    float critChance,
    float critMultiplier,
    CharacterStats attacker,
    CharacterStats defender
)
    {
        // 1. Basdamage (kommer från PlayerStats / EnemyStats)
        float damage = baseDamage;

        // 2. Liten variation (±20%)
        float variation = Random.Range(0.8f, 1.2f);
        damage *= variation;


        // MISS CHECK
        if (!CombatResolver.RollHit(attacker, defender))
        {
            return new DamageResult { damage = 0, isMiss = true };
        }

        // EVADE CHECK
        if (CombatResolver.RollDodge(attacker, defender))
        {
            return new DamageResult { damage = 0, isEvaded = true };
        }

        // 3. Crit?
        bool isCrit = Random.value < critChance;

        if (isCrit)
        {
            damage *= critMultiplier;
        }

        float armor = defender.GetStat(StatType.Armor);
        damage *= 100f / (100f + armor);

        bool blocked = false;
        int blockedAmount = 0;

        float blockChance = defender.GetStat(StatType.BlockChance);

        //Debug.Log( $"BlockChance: {blockChance} | BlockValue: {defender.GetStat(StatType.BlockValue)}");

        if (Random.value < blockChance)
        {
            blocked = true;

            blockedAmount = Mathf.RoundToInt(defender.GetStat(StatType.BlockValue));

            damage -= blockedAmount;

            damage = Mathf.Max(1, damage);
        }

        return new DamageResult
        {
            damage = Mathf.Max(1, Mathf.FloorToInt(damage)),
            isCrit = isCrit,

            isBlocked = blocked,
            blockedAmount = blockedAmount
        };
    }

}





