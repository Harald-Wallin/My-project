using System.Collections.Generic;
using UnityEngine;

public sealed class LootUI :
    MonoBehaviour
{
    public static LootUI Instance
    {
        get;
        private set;
    }

    [Header("Content")]

    [SerializeField]
    private GameObject lootItemRowPrefab;

    [SerializeField]
    private GameObject lootWindow;

    [SerializeField]
    private Transform contentParent;

    [SerializeField]
    private TMPro.TMP_Text titleText;

    [Header("Layout")]

    [SerializeField]
    private RectTransform lootWindowRect;

    [SerializeField]
    private RectTransform contentRect;

    [SerializeField]
    private int paddingTop = 6;

    [SerializeField]
    private int paddingBottom = 6;

    [SerializeField]
    private float maxHeight;

    [SerializeField]
    private float titleHeight = 40f;

    private LootContainer currentContainer;
    private string currentTitle;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(
                gameObject
            );

            return;
        }

        gameObject.SetActive(
            false
        );
    }

    public void Show(
        LootContainer container,
        string title)
    {
        if (container == null)
        {
            Close();
            return;
        }

        currentContainer =
            container;

        currentTitle =
            title;

        if (titleText != null)
        {
            titleText.text =
                title;
        }

        ClearRows();

        BuildCoinRow(
            container
        );

        BuildItemRows(
            container
        );

        gameObject.SetActive(
            true
        );
    }

    private void BuildCoinRow(
        LootContainer container)
    {
        if (container.CoinAmount <= 0 ||
            lootItemRowPrefab == null ||
            contentParent == null)
        {
            return;
        }

        PlayerCurrency playerCurrency =
            PlayerCurrency.Instance;

        CurrencyData currency =
            playerCurrency != null
                ? playerCurrency
                    .CurrencyDefinition
                : null;

        if (currency == null)
        {
            Debug.LogWarning(
                "Loot innehåller coins men PlayerCurrency " +
                "saknar CurrencyData.",
                this
            );

            return;
        }

        GameObject row =
            Instantiate(
                lootItemRowPrefab,
                contentParent
            );

        LootItemRow lootRow =
            row.GetComponent<
                LootItemRow>();

        if (lootRow == null)
        {
            Debug.LogError(
                "Loot row-prefabben saknar LootItemRow.",
                row
            );

            Destroy(
                row
            );

            return;
        }

        lootRow.SetupCoins(
            currency,
            container,
            this
        );
    }

    private void BuildItemRows(
        LootContainer container)
    {
        if (container.items == null ||
            lootItemRowPrefab == null ||
            contentParent == null)
        {
            return;
        }

        List<ItemData> shownItems =
            new List<ItemData>();

        foreach (ItemData item
                 in container.items)
        {
            if (item == null ||
                ContainsMatchingItem(
                    shownItems,
                    item))
            {
                continue;
            }

            shownItems.Add(
                item
            );

            GameObject row =
                Instantiate(
                    lootItemRowPrefab,
                    contentParent
                );

            LootItemRow lootRow =
                row.GetComponent<
                    LootItemRow>();

            if (lootRow == null)
            {
                Debug.LogError(
                    "Loot row-prefabben saknar LootItemRow.",
                    row
                );

                Destroy(
                    row
                );

                continue;
            }

            lootRow.SetupItem(
                item,
                container,
                this
            );
        }
    }

    private static bool ContainsMatchingItem(
        IReadOnlyList<ItemData> items,
        ItemData candidate)
    {
        if (items == null ||
            candidate == null)
        {
            return false;
        }

        foreach (ItemData item
                 in items)
        {
            if (Inventory.ItemsMatch(
                    item,
                    candidate))
            {
                return true;
            }
        }

        return false;
    }

    private void ClearRows()
    {
        if (contentParent == null)
            return;

        foreach (Transform child
                 in contentParent)
        {
            Destroy(
                child.gameObject
            );
        }
    }

    public void Refresh()
    {
        if (currentContainer == null ||
            !currentContainer.HasLoot)
        {
            Close();
            return;
        }

        Show(
            currentContainer,
            currentTitle
        );
    }

    public void Close()
    {
        currentContainer =
            null;

        currentTitle =
            string.Empty;

        ItemTooltip.Instance?.Hide();

        gameObject.SetActive(
            false
        );
    }
}