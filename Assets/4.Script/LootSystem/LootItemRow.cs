using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public sealed class LootItemRow :
    MonoBehaviour,
    IPointerEnterHandler,
    IPointerExitHandler
{
    [Header("Visuals")]

    [SerializeField]
    private TMP_Text itemNameText;

    [SerializeField]
    private Image iconImage;

    [SerializeField]
    private TMP_Text amountText;

    [SerializeField]
    private Image border;

    private ItemData item;
    private CurrencyData currency;

    private LootContainer sourceContainer;
    private LootUI lootUI;

    private Color originalBorderColor;

    private bool IsCurrency =>
        currency != null;

    public void SetupItem(
        ItemData newItem,
        LootContainer container,
        LootUI ui)
    {
        item = newItem;
        currency = null;

        sourceContainer =
            container;

        lootUI =
            ui;

        if (item == null ||
            sourceContainer == null)
        {
            gameObject.SetActive(
                false
            );

            return;
        }

        int quantity =
            CountItemQuantity();

        itemNameText.text =
            item.DisplayName;

        Color rarityColor =
            ItemRarityColors.GetColor(
                item.rarity
            );

        SetBorderColor(
            rarityColor
        );

        itemNameText.color =
            rarityColor;

        if (iconImage != null)
        {
            iconImage.sprite =
                item.icon;

            iconImage.enabled =
                item.icon != null;
        }

        SetAmountText(
            quantity
        );
    }

    public void SetupCoins(
        CurrencyData currencyData,
        LootContainer container,
        LootUI ui)
    {
        item = null;
        currency = currencyData;

        sourceContainer =
            container;

        lootUI =
            ui;

        if (currency == null ||
            sourceContainer == null ||
            sourceContainer.CoinAmount <= 0)
        {
            gameObject.SetActive(
                false
            );

            return;
        }

        itemNameText.text =
            currency.DisplayName;

        itemNameText.color =
            Color.white;

        SetBorderColor(
            Color.white
        );

        if (iconImage != null)
        {
            iconImage.sprite =
                currency.Icon;

            iconImage.enabled =
                currency.Icon != null;
        }

        SetAmountText(
            sourceContainer.CoinAmount
        );
    }

    public void TakeItem()
    {
        if (IsCurrency)
        {
            TakeCoins();
            return;
        }

        TakeInventoryItem();
    }

    private void TakeInventoryItem()
    {
        if (item == null ||
            sourceContainer == null ||
            Inventory.Instance == null)
        {
            return;
        }

        int quantity =
            CountItemQuantity();

        if (quantity <= 0)
            return;

        /*
         * Inventoryt modifieras först. Loot tas inte bort om
         * inventoryt är fullt.
         */
        bool added =
            Inventory.Instance.AddItem(
                item,
                quantity
            );

        if (!added)
            return;

        for (int i =
                 sourceContainer.items.Count - 1;
             i >= 0;
             i--)
        {
            if (Inventory.ItemsMatch(
                    sourceContainer.items[i],
                    item))
            {
                sourceContainer.items
                    .RemoveAt(
                        i
                    );
            }
        }

        FinishTakingLoot();
    }

    private void TakeCoins()
    {
        if (sourceContainer == null)
            return;

        PlayerCurrency playerCurrency =
            PlayerCurrency.Instance;

        if (playerCurrency == null)
        {
            Debug.LogError(
                "Kan inte loota coins: PlayerCurrency saknas.",
                this
            );

            return;
        }

        int amount =
            sourceContainer.CoinAmount;

        if (amount <= 0)
            return;

        if (!playerCurrency.AddCoins(
                amount))
        {
            return;
        }

        sourceContainer.SetCoins(
            0
        );

        FinishTakingLoot();
    }

    private void FinishTakingLoot()
    {
        ItemTooltip.Instance?.Hide();

        lootUI?.Refresh();

        LootableCorpse corpse =
            sourceContainer != null
                ? sourceContainer.GetComponent<
                    LootableCorpse>()
                : null;

        corpse?.RefreshVisuals();

        Destroy(
            gameObject
        );
    }

    private int CountItemQuantity()
    {
        if (item == null ||
            sourceContainer?.items == null)
        {
            return 0;
        }

        int quantity = 0;

        foreach (ItemData containedItem
                 in sourceContainer.items)
        {
            if (Inventory.ItemsMatch(
                    containedItem,
                    item))
            {
                quantity++;
            }
        }

        return quantity;
    }

    public void OnPointerEnter(
        PointerEventData eventData)
    {
        ITooltipProvider provider =
            IsCurrency
                ? currency
                : item;

        if (provider == null ||
            ItemTooltip.Instance == null ||
            iconImage == null)
        {
            return;
        }

        ItemTooltip.Instance.Show(
            provider,
            iconImage.rectTransform,
            PlayerReference.Player
        );

        if (border != null)
        {
            border.color =
                Color.Lerp(
                    originalBorderColor,
                    Color.white,
                    0.5f
                );
        }
    }

    public void OnPointerExit(
        PointerEventData eventData)
    {
        ItemTooltip.Instance?.Hide();

        if (border != null)
        {
            border.color =
                originalBorderColor;
        }
    }

    private void SetAmountText(
        int amount)
    {
        if (amountText == null)
            return;

        bool show =
            amount > 1;

        amountText.gameObject
            .SetActive(
                show
            );

        amountText.text =
            show
                ? amount.ToString()
                : string.Empty;
    }

    private void SetBorderColor(
        Color color)
    {
        if (border == null)
            return;

        border.enabled =
            true;

        border.color =
            color;

        originalBorderColor =
            color;
    }

    private void OnDisable()
    {
        ItemTooltip.Instance?.Hide();
    }
}