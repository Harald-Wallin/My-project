using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(
    menuName = "RPG/Rewards/Death Reward",
    fileName = "DeathReward")]
public class DeathRewardData : ScriptableObject
{
    [Header("Experience")]
    public bool useDynamicExperience = true;
    public int experience = 0;

    [Header("Reputation")]
    public Faction reputationFaction;
    public int reputation = 0;

    [Header("Loot")]
    public GameObject corpsePrefab;
    public List<LootTable> lootTables = new();

    public int minLootRolls = 0;
    public int maxLootRolls = 3;

    [Header("Visuals")]
    public GameObject floatingExpTextPrefab;

    [Header("World")]
    public bool unlockStartZoneWolf;

    //[Header("Future")]
    //public List<WorldFlag> unlockFlags;
    //public List<GameEventData> triggerEvents;

    public int GetExperience(int targetLevel, int playerLevel)
    {
        if (useDynamicExperience)
            return ExperienceCalculator.CalculateExp(targetLevel, playerLevel);

        return experience;
    }

    public void FillLootContainer(LootContainer container)
    {
        if (container == null)
            return;

        if (lootTables == null || lootTables.Count == 0)
            return;

        int rolls = Random.Range(minLootRolls, maxLootRolls + 1);

        for (int i = 0; i < rolls; i++)
        {
            List<ItemData> drop =
                LootGenerator.GenerateSingleDrop(lootTables);

            container.items.AddRange(drop);
        }
    }

    public void GenerateLoot(LootContainer container)
    {
        if (container == null)
            return;

        if (lootTables == null || lootTables.Count == 0)
            return;

        int rolls = Random.Range(minLootRolls, maxLootRolls + 1);

        for (int i = 0; i < rolls; i++)
        {
            List<ItemData> drop =
                LootGenerator.GenerateSingleDrop(lootTables);

            container.items.AddRange(drop);
        }
    }

}
