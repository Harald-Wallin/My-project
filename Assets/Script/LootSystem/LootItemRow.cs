using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class LootItemRow : MonoBehaviour,
    IPointerEnterHandler,
    IPointerExitHandler
{
    [SerializeField] TMP_Text itemNameText;
    [SerializeField] Image iconImage;
    [SerializeField] TMP_Text amountText;
    [SerializeField] private Image border;

    //GLOW
    Color originalBorderColor;

    ItemData item;
    LootContainer sourceContainer;
    LootUI lootUI;


    public void Setup(ItemData newItem, LootContainer container, LootUI ui)
    {
        item = newItem;
        sourceContainer = container;
        lootUI = ui;

        int quantity = sourceContainer.items.FindAll(i => i == item).Count;

        itemNameText.text = item.itemName;
        border.color = ItemRarityColors.GetColor(item.rarity);
        border.enabled = true;

        if (amountText != null)
        {
            amountText.text = quantity > 1 ? quantity.ToString() : "";
        }

        itemNameText.color = ItemRarityColors.GetColor(item.rarity);

        if (iconImage != null)
            iconImage.sprite = item.icon;

        //GLOW
        border.color = ItemRarityColors.GetColor(item.rarity);
        originalBorderColor = border.color;
    }

    public void TakeItem()
    {
        int quantity = 0;

        // Hitta alla items av samma typ i LootContainer
        for (int i = sourceContainer.items.Count - 1; i >= 0; i--)
        {
            if (sourceContainer.items[i] == item)
            {
                quantity++;
                sourceContainer.items.RemoveAt(i);
            }
        }

        if (quantity == 0)
            return;

        // Lägg hela stacken i inventory
        bool added = Inventory.Instance.AddItem(item, quantity);

        if (!added)
        {
            Debug.Log("Inventory is full!");
            return;
        }

        ItemTooltip.Instance.Hide();
        lootUI.Refresh();

        LootableCorpse corpse = sourceContainer.GetComponent<LootableCorpse>();

        if (corpse != null)
        {
            corpse.RefreshVisuals();
        }
        Destroy(gameObject);
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (sourceContainer != null && item != null)
        {
            var player = PlayerReference.Player;

            ItemTooltip.Instance.Show(
                item,
                iconImage.rectTransform,
                player
            );

            //GLOW
            border.color = Color.Lerp(originalBorderColor, Color.white, 0.5f);
        }
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        ItemTooltip.Instance.Hide();

        //GLOW
        border.color = originalBorderColor;
    }

}


