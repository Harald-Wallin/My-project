using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class ReputationReward
{
    public Faction faction;
    public int reputation;
}

    public class DeathReward : MonoBehaviour
{
    [Header("Experience")]
    public bool useDynamicExperience = true;
    public int experience = 0;

    [Header("Reputation")]
    public List<ReputationReward> reputationRewards = new();

    [Header("Loot")]
    public GameObject corpsePrefab;
    public List<LootTable> lootTables = new();

    public int minLootRolls = 0;
    public int maxLootRolls = 3;

    [Header("Visuals")]
    public GameObject floatingExpTextPrefab;

    [Header("World")]
    public bool unlockStartZoneWolf;

    public int GetExperience(int targetLevel, int playerLevel)
    {
        if (useDynamicExperience)
            return ExperienceCalculator.CalculateExp(targetLevel, playerLevel);

        return experience;
    }



    public void GenerateLoot(LootContainer container)
    {
        if (container == null)
            return;

        if (lootTables == null || lootTables.Count == 0)
            return;

        container.items.Clear();

        container.items.AddRange(
            LootGenerator.GenerateLoot(
                lootTables,
                minLootRolls,
                maxLootRolls
            ));
    }

    public GameObject SpawnCorpse(Vector3 position, CharacterStats owner)
    {
        if (corpsePrefab == null)
            return null;

        GameObject corpse =
            Instantiate(corpsePrefab, position, Quaternion.identity);

        LootContainer loot =
            corpse.GetComponent<LootContainer>();

        if (loot != null)
        {
            GenerateLoot(loot);
        }

        CharacterStats corpseStats =
            corpse.GetComponent<CharacterStats>();

        if (corpseStats != null)
        {
            corpseStats.faction = null;
        }

        // Flytta över Nameplate
        Transform nameplate = owner.transform.Find("Nameplate");

        if (nameplate != null)
        {
            nameplate.SetParent(corpse.transform, true);

            NameplateUI ui =
                nameplate.GetComponentInChildren<NameplateUI>();

            if (ui != null)
                ui.SetCorpseMode();
        }

        return corpse;
    }

    public void GiveRewards(CharacterStats victim, CharacterStats killer)
    {
        PlayerStats player = killer as PlayerStats;

        if (player == null)
            return;

        // Experience
        int exp = GetExperience(victim.level, player.level);

        if (exp > 0)
        {
            player.GainExp(exp);

            if (floatingExpTextPrefab != null)
            {
                GameObject text =
                    Instantiate(
                        floatingExpTextPrefab,
                        victim.transform.position + Vector3.up * 1.5f,
                        Quaternion.identity);

                TMPro.TMP_Text tmp =
                    text.GetComponentInChildren<TMPro.TMP_Text>();

                if (tmp != null)
                    tmp.text = exp + " EXP";
            }
        }

        //Reputation
        PlayerReputationManager rep = player.GetComponent<PlayerReputationManager>();

        if (rep != null)
        {
            foreach (var reward in reputationRewards)
            {
                if (reward.faction == null)
                    continue;

                if (reward.reputation == 0)
                    continue;

                rep.AddReputation(reward.faction,reward.reputation);
            }
        }

        // Murder penalty
        if (player.murderMode &&
            !victim.IsHostileToPlayer(player) &&
            victim.faction != null)
        {
            //PlayerReputationManager rep = player.GetComponent<PlayerReputationManager>();

            if (rep != null)
            {
                rep.AddReputation(
                    victim.faction,
                    -victim.reputationLossOnDeath);
            }
        }
    }
}
