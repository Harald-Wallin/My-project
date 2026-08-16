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
    [Tooltip(
        "Om exakt delad högsta contribution fortfarande ska " +
        "ge spelaren reward-credit."
    )]
    private bool allowTiedTopContribution =
        true;

    [Header("Visuals")]

    public GameObject floatingExpTextPrefab;

    [Header("World")]

    public bool unlockStartZoneWolf;

    // =========================================================
    // EXPERIENCE
    // =========================================================

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

    // =========================================================
    // DEATH RESULT
    // =========================================================

    public void GiveRewards(
        CharacterDefeatedResult result)
    {
        if (result == null ||
            result.Victim == null)
        {
            return;
        }

        CharacterStats victim =
            result.Victim;

        PlayerStats player =
            PlayerReference.Player;

        bool playerHasCredit =
            player != null &&
            result.IsTopDamageContributor(
                player,
                allowTiedTopContribution
            );

        /*
         * Corpse skapas ALLTID här.
         *
         * Detta är viktigt:
         *
         * tidigare skapades corpse från NPCBehavior innan
         * contribution-resultatet ens hade utvärderats.
         *
         * Nu känner corpse-generationen till reward ownership.
         */
        SpawnCorpse(
            victim.transform.position,
            victim,
            playerHasCredit
        );

        if (!playerHasCredit ||
            player == null)
        {
            return;
        }

        GiveRewardsInternal(
            victim,
            player
        );
    }

    /// <summary>
    /// Legacy-ingång.
    ///
    /// Behålls tills alla äldre callers är migrerade.
    /// Den delar endast ut progression och skapar INTE corpse.
    /// </summary>
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

    // =========================================================
    // REWARD PAYOUT
    // =========================================================

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

    // =========================================================
    // EXPERIENCE FEEDBACK
    // =========================================================

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
                amount +
                " EXP";
        }
    }

    // =========================================================
    // LOOT GENERATION
    // =========================================================

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

    // =========================================================
    // CORPSE
    // =========================================================

    public GameObject SpawnCorpse(
        Vector3 position,
        CharacterStats owner,
        bool generatePlayerLoot)
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
            /*
             * Corpse får bara player-loot om spelaren faktiskt
             * vann contribution.
             */
            if (generatePlayerLoot)
            {
                GenerateLoot(
                    loot
                );
            }
            else
            {
                loot.items.Clear();

                loot.SetCoins(
                    0
                );
            }
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

    /// <summary>
    /// Legacy-overload.
    /// </summary>
    public GameObject SpawnCorpse(
        Vector3 position,
        CharacterStats owner)
    {
        return SpawnCorpse(
            position,
            owner,
            true
        );
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