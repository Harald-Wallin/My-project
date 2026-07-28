using System.Collections.Generic;
using UnityEngine;

public sealed class LootContainer :
    MonoBehaviour
{
    [Header("Items")]

    public List<ItemData> items =
        new List<ItemData>();

    [Header("Currency")]

    [SerializeField]
    [Min(0)]
    private int coinAmount;

    public int CoinAmount =>
        Mathf.Max(
            0,
            coinAmount
        );

    public bool HasLoot =>
        (items != null &&
         items.Count > 0) ||
        CoinAmount > 0;

    public void SetCoins(
        int amount)
    {
        coinAmount =
            Mathf.Max(
                0,
                amount
            );
    }

    public void AddCoins(
        int amount)
    {
        if (amount <= 0)
            return;

        coinAmount +=
            amount;
    }

    public int TakeAllCoins()
    {
        int taken =
            CoinAmount;

        coinAmount = 0;

        return taken;
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        items ??=
            new List<ItemData>();

        coinAmount =
            Mathf.Max(
                0,
                coinAmount
            );
    }
#endif
}