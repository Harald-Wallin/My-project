using UnityEngine;

[System.Serializable]
public class LootEntry
{
    public ItemData item;
    [Range(0f, 1f)]
    public float dropChance = 1f;

    public int minQuantity = 1;
    public int maxQuantity = 1;
}

