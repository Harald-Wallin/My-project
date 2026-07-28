using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class ReputationReward
{
    public Faction faction;
    public int reputation;
}

public class DeathReward :
    MonoBehaviour
{
    [Header("Experience")]

    public bool useDynamicExperience = true;
    public int experience;

    [Header("Reputation")]

    public List<ReputationReward>
        reputationRewards =
            new();

    [Header("Loot")]

    public GameObject corpsePrefab;

    public List<LootTable>
        lootTables =
            new();

    public int minLootRolls;
    public int maxLootRolls = 3;

    [Header("Credit")]

    [SerializeField]
    [Range(0f, 1f)]
    private float minimumPlayerDamageShare =
        0.5f;

    [Header("Visuals")]

    public GameObject floatingExpTextPrefab;

    [Header("World")]

    public bool unlockStartZoneWolf;

    public int GetExperience(
        int targetLevel,
        int playerLevel)
    {
        if (useDynamicExperience)
        {
            return ExperienceCalculator
                .CalculateExp(
                    targetLevel,
                    playerLevel
                );
        }

        return experience;
    }

    public void GiveRewards(
        CharacterDefeatedResult result)
    {
        if (result == null ||
            result.Victim == null)
        {
            return;
        }

        PlayerStats player =
            PlayerReference.Player;

        if (player == null)
            return;

        if (!result.HasMinimumDamageShare(
                player,
                minimumPlayerDamageShare))
        {
            return;
        }

        GiveRewardsInternal(
            result.Victim,
            player
        );
    }

    public void GiveRewards(
        CharacterStats victim,
        CharacterStats killer)
    {
        PlayerStats player =
            killer as PlayerStats;

        if (player == null)
            return;

        GiveRewardsInternal(
            victim,
            player
        );
    }

    private void GiveRewardsInternal(
        CharacterStats victim,
        PlayerStats player)
    {
        if (victim == null ||
            player == null)
        {
            return;
        }

        int exp =
            GetExperience(
                victim.level,
                player.level
            );

        if (exp > 0)
        {
            player.GainExp(
                exp
            );

            ShowFloatingExperience(
                victim,
                exp
            );
        }

        PlayerReputationManager reputation =
            player.GetComponent<
                PlayerReputationManager>();

        if (reputation != null)
        {
            foreach (ReputationReward reward
                     in reputationRewards)
            {
                if (reward == null ||
                    reward.faction == null ||
                    reward.reputation == 0)
                {
                    continue;
                }

                reputation.AddReputation(
                    reward.faction,
                    reward.reputation
                );
            }
        }

        if (player.murderMode &&
            !victim.IsHostileToPlayer(
                player) &&
            victim.faction != null &&
            reputation != null)
        {
            reputation.AddReputation(
                victim.faction,
                -victim.reputationLossOnDeath
            );
        }
    }

    private void ShowFloatingExperience(
        CharacterStats victim,
        int amount)
    {
        if (floatingExpTextPrefab == null ||
            victim == null ||
            amount <= 0)
        {
            return;
        }

        GameObject text =
            Instantiate(
                floatingExpTextPrefab,
                victim.transform.position +
                Vector3.up * 1.5f,
                Quaternion.identity
            );

        TMPro.TMP_Text tmp =
            text.GetComponentInChildren<
                TMPro.TMP_Text>();

        if (tmp != null)
        {
            tmp.text =
                amount + " EXP";
        }
    }

    public void GenerateLoot(
        LootContainer container)
    {
        if (container == null)
            return;

        container.items.Clear();
        container.SetCoins(
            0
        );

        if (lootTables == null ||
            lootTables.Count == 0)
        {
            return;
        }

        LootGenerationResult result =
            LootGenerator.GenerateLootResult(
                lootTables,
                minLootRolls,
                maxLootRolls
            );

        container.items.AddRange(
            result.Items
        );

        container.SetCoins(
            result.Coins
        );
    }

    public GameObject SpawnCorpse(
        Vector3 position,
        CharacterStats owner)
    {
        if (corpsePrefab == null)
            return null;

        GameObject corpse =
            Instantiate(
                corpsePrefab,
                position,
                Quaternion.identity
            );

        LootContainer loot =
            corpse.GetComponent<
                LootContainer>();

        if (loot != null)
        {
            GenerateLoot(
                loot
            );
        }

        CharacterStats corpseStats =
            corpse.GetComponent<
                CharacterStats>();

        if (corpseStats != null)
        {
            corpseStats.faction =
                null;
        }

        if (owner != null)
        {
            MoveNameplateToCorpse(
                owner,
                corpse
            );
        }

        return corpse;
    }

    private static void MoveNameplateToCorpse(
        CharacterStats owner,
        GameObject corpse)
    {
        if (owner == null ||
            corpse == null)
        {
            return;
        }

        Transform nameplate =
            owner.transform.Find(
                "Nameplate"
            );

        if (nameplate == null)
            return;

        nameplate.SetParent(
            corpse.transform,
            true
        );

        NameplateUI ui =
            nameplate.GetComponentInChildren<
                NameplateUI>();

        if (ui != null)
        {
            ui.SetCorpseMode();
        }
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        minLootRolls =
            Mathf.Max(
                0,
                minLootRolls
            );

        maxLootRolls =
            Mathf.Max(
                minLootRolls,
                maxLootRolls
            );
    }
#endif
}