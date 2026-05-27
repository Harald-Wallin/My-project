using System.Collections.Generic;
using UnityEngine;

public enum LootTableMode
{
    SingleDrop,   // gear, rare stuff (WoW-style)
    MultiDrop     // trash, reagents, vendor loot
}

[CreateAssetMenu(menuName = "Loot/Loot Table")]
public class LootTable : ScriptableObject
{
    public LootTableMode mode = LootTableMode.SingleDrop;

    public List<LootEntry> entries = new List<LootEntry>();
}

