using System.Collections.Generic;
using UnityEngine;

public sealed class LootGenerationResult
{
    private readonly List<ItemData>
        items =
            new();

    public List<ItemData> Items =>
        items;

    public int Coins
    {
        get;
        private set;
    }

    public void AddItem(
        ItemData item,
        int amount)
    {
        if (item == null ||
            amount <= 0)
        {
            return;
        }

        for (int i = 0;
             i < amount;
             i++)
        {
            items.Add(
                item
            );
        }
    }

    public void AddCoins(
        int amount)
    {
        if (amount <= 0)
            return;

        Coins +=
            amount;
    }
}

public static class LootGenerator
{
    public static LootGenerationResult
        GenerateLootResult(
            List<LootTable> tables,
            int minRolls,
            int maxRolls)
    {
        LootGenerationResult result =
            new LootGenerationResult();

        if (tables == null ||
            tables.Count == 0)
        {
            return result;
        }

        int safeMinimum =
            Mathf.Max(
                0,
                minRolls
            );

        int safeMaximum =
            Mathf.Max(
                safeMinimum,
                maxRolls
            );

        int rolls =
            Random.Range(
                safeMinimum,
                safeMaximum + 1
            );

        for (int i = 0;
             i < rolls;
             i++)
        {
            foreach (LootTable table
                     in tables)
            {
                RollTable(
                    table,
                    result
                );
            }
        }

        return result;
    }

    /*
     * Behålls för kompatibilitet med kod som endast förväntar
     * sig vanliga items.
     */
    public static List<ItemData> GenerateLoot(
        List<LootTable> tables,
        int minRolls,
        int maxRolls)
    {
        return GenerateLootResult(
            tables,
            minRolls,
            maxRolls
        ).Items;
    }

    public static LootGenerationResult
        GenerateSingleDropResult(
            List<LootTable> tables)
    {
        LootGenerationResult result =
            new LootGenerationResult();

        if (tables == null ||
            tables.Count == 0)
        {
            return result;
        }

        LootTable chosenTable =
            tables[
                Random.Range(
                    0,
                    tables.Count
                )
            ];

        RollTable(
            chosenTable,
            result
        );

        return result;
    }

    /*
     * Behålls för kompatibilitet.
     */
    public static List<ItemData>
        GenerateSingleDrop(
            List<LootTable> tables)
    {
        return GenerateSingleDropResult(
            tables
        ).Items;
    }

    private static void RollTable(
        LootTable table,
        LootGenerationResult result)
    {
        if (table == null ||
            result == null)
        {
            return;
        }

        if (table.mode ==
            LootTableMode.SingleDrop)
        {
            RollSingleDrop(
                table,
                result
            );
        }
        else
        {
            RollMultiDrop(
                table,
                result
            );
        }
    }

    private static void RollSingleDrop(
        LootTable table,
        LootGenerationResult result)
    {
        if (table?.entries == null)
            return;

        foreach (LootEntry entry
                 in table.entries)
        {
            if (!CanRollEntry(
                    entry))
            {
                continue;
            }

            if (Random.value >
                entry.DropChance)
            {
                continue;
            }

            AddRolledEntry(
                entry,
                result
            );

            /*
             * SingleDrop tillåter högst en lyckad entry
             * per table och roll.
             */
            return;
        }
    }

    private static void RollMultiDrop(
        LootTable table,
        LootGenerationResult result)
    {
        if (table?.entries == null)
            return;

        foreach (LootEntry entry
                 in table.entries)
        {
            if (!CanRollEntry(
                    entry))
            {
                continue;
            }

            if (Random.value >
                entry.DropChance)
            {
                continue;
            }

            AddRolledEntry(
                entry,
                result
            );
        }
    }

    private static bool CanRollEntry(
        LootEntry entry)
    {
        if (entry == null ||
            !entry.IsValid)
        {
            return false;
        }

        if (entry.Type !=
            LootEntryType.Item)
        {
            return true;
        }

        ItemData item =
            entry.Item;

        return item != null &&
               item.CanDropForPlayer(
                   PlayerFavourManager.Instance
               );
    }

    private static void AddRolledEntry(
        LootEntry entry,
        LootGenerationResult result)
    {
        int amount =
            Random.Range(
                entry.MinQuantity,
                entry.MaxQuantity + 1
            );

        switch (entry.Type)
        {
            case LootEntryType.Item:

                result.AddItem(
                    entry.Item,
                    amount
                );

                break;

            case LootEntryType.Coins:

                result.AddCoins(
                    amount
                );

                break;
        }
    }
}