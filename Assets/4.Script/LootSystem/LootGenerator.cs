using System.Collections.Generic;
using UnityEngine;

public static class LootGenerator
{
    public static List<ItemData> GenerateLoot(
        List<LootTable> tables,
        int minRolls,
        int maxRolls)
    {
        List<ItemData> result = new List<ItemData>();

        int rolls = Random.Range(minRolls, maxRolls + 1);

        for (int i = 0; i < rolls; i++)
        {
            foreach (LootTable table in tables)
            {
                RollTable(table, result);
            }
        }

        return result;
    }

    static void RollTable(LootTable table, List<ItemData> result)
    {
        if (table == null)
            return;

        if (table.mode == LootTableMode.SingleDrop)
        {
            RollSingleDrop(table, result);
        }
        else
        {
            RollMultiDrop(table, result);
        }
    }

    static void RollSingleDrop(LootTable table, List<ItemData> result)
    {
        foreach (LootEntry entry in table.entries)
        {
            if (entry.item == null)
                continue;

            if (Random.value <= entry.dropChance)
            {
                int amount = Random.Range(
                    entry.minQuantity,
                    entry.maxQuantity + 1
                );

                for (int i = 0; i < amount; i++)
                {
                    result.Add(entry.item);
                }

                return; // max 1 drop per table per roll
            }
        }
    }

    public static List<ItemData> GenerateSingleDrop(List<LootTable> tables)
    {
        List<ItemData> result = new List<ItemData>();

        if (tables == null || tables.Count == 0)
            return result;

        // 1. Välj EN loot-table slumpmässigt
        LootTable chosenTable = tables[Random.Range(0, tables.Count)];

        if (chosenTable == null)
            return result;

        // 2. Rulla den tabellen
        if (chosenTable.mode == LootTableMode.SingleDrop)
        {
            RollSingleDrop(chosenTable, result);
        }
        else
        {
            RollMultiDrop(chosenTable, result);
        }

        return result;
    }


    static void RollMultiDrop(LootTable table, List<ItemData> result)
    {
        foreach (LootEntry entry in table.entries)
        {
            if (entry.item == null)
                continue;

            if (Random.value <= entry.dropChance)
            {
                int amount = Random.Range(
                    entry.minQuantity,
                    entry.maxQuantity + 1
                );

                for (int i = 0; i < amount; i++)
                {
                    result.Add(entry.item);
                }
            }
        }
    }
}


