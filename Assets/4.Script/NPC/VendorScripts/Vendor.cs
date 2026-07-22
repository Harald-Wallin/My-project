using System.Collections.Generic;
using UnityEngine;
using static UnityEngine.EventSystems.EventTrigger;

public class Vendor : MonoBehaviour, IInteractable
{
    [System.Serializable]
    public class BuybackEntry
    {
        public ItemData item;
        public int quantity;
        public int pricePaid;
        public float expiryTime;
    }

    [Header("Buyback Settings")]
    [SerializeField] private int maxBuybackSlots = 5;
    [SerializeField] private float buybackLifetime = 1200f;

    private List<VendorItemRuntime> runtimeItems = new List<VendorItemRuntime>();
    private List<BuybackEntry> buybackItems = new List<BuybackEntry>();

    public List<VendorItemRuntime> GetItems()
    {
        return runtimeItems;
    }
    public List<BuybackEntry> GetBuybackItems()
    {
        return buybackItems;
    }

    [Header("Vendor Settings")]
    [SerializeField] private Faction faction;

    [Header("Items For Sale")]
   
    [SerializeField] private List<VendorItem> itemsForSale = new List<VendorItem>();

    public VendorUI vendorUI;


    void Start()
    {
        InitializeRuntimeInventory();
    }
    void Update()
    {
        RemoveExpiredBuybacks();
        UpdateRestocks();
    }

    void RemoveExpiredBuybacks()
    {
        for (int i = buybackItems.Count - 1; i >= 0; i--)
        {
            if (Time.time >= buybackItems[i].expiryTime)
            {
                buybackItems.RemoveAt(i);
            }
        }
    }

    void InitializeRuntimeInventory()
    {
        runtimeItems.Clear();

        foreach (var item in itemsForSale)
        {
            runtimeItems.Add(new VendorItemRuntime(item));
        }
    }

    public void Interact(PlayerStats player)
    {
        PlayerReputationManager rep =
            FindFirstObjectByType<PlayerReputationManager>();

        if (!CanTrade(rep))
        {
            //Debug.Log("Vendor refuses to trade.");
            return;
        }

        OpenVendor();
    }

    public void OpenVendor()
    {
        //Debug.Log($"Vendor.OpenVendor: opening vendor UI for '{name}'");

        // Use explicit vendorUI reference if set (useful for testing),
        // otherwise fall back to the global VendorUI instance in scene.
        if (vendorUI != null)
        {
            vendorUI.Open(this);
            //Debug.Log("Vendor.OpenVendor: used vendorUI reference on Vendor component.");
            return;
        }

        if (VendorUI.Instance != null)
        {
            VendorUI.Instance.Open(this);
            //Debug.Log("Vendor.OpenVendor: used VendorUI.Instance singleton.");
            return;
        }

        //Debug.LogWarning("Vendor.OpenVendor: No VendorUI assigned and no VendorUI.Instance found in scene.");
    }

    public bool CanTrade(PlayerReputationManager repManager)
    {
        if (repManager == null)
            return false;

        NPCReactionController reaction = GetComponent<NPCReactionController>();

        if (reaction != null &&
            reaction.IsTemporarilyHostile)
        {
            return false;
        }

        if (IsTemporarilyHostile())
            return false;

        if (faction == null)
            return true;

        return repManager.GetReputationState(faction)
               >= ReputationState.Indifferent;
    }

    public bool IsTemporarilyHostile()
    {
        NPCReactionController reaction = GetComponent<NPCReactionController>();

        return reaction != null &&
               reaction.IsTemporarilyHostile;
    }

    public void BuyItem(VendorItemRuntime entry)
    {
        if (entry == null || entry.data == null || entry.data.item == null)
            return;

        PlayerStats player = PlayerReference.Player;

        // Kontrollera level-krav på VendorItem (VendorItem.requiredLevel)
        if (entry.data.useLevelRequirement && entry.data.requiredLevel > player.level)
        {
            Debug.Log("Level too low to buy this item.");
            return;
        }

        // Kontrollera reputation-krav om item kräver det
        if (entry.data.useReputationRequirement)
        {
            PlayerReputationManager repManager = FindFirstObjectByType<PlayerReputationManager>();

            if (repManager == null || faction == null)
            {
                Debug.Log("Cannot determine reputation for this vendor. Purchase denied.");
                return;
            }

            var state = repManager.GetReputationState(faction);
            if (state < entry.data.requiredReputation)
            {
                Debug.Log("Your reputation is too low to buy this item.");
                return;
            }
        }

        // 🛑 STOPPA KÖP OM STOCK = 0
        if (entry.data.stockType == VendorStockType.Limited && entry.currentStock <= 0)
        {
            Debug.Log("Item out of stock.");
            return;
        }

        int price = GetModifiedPrice(entry.data);

        if (price < 0)
        {
            Debug.Log("Vendor refuses to trade with you.");
            return;
        }

        if (!PlayerCurrency.Instance.TrySpendCoins(price))
        {
            Debug.Log("Not enough coins.");
            return;
        }

        bool added = Inventory.Instance.AddItem(entry.data.item, 1);

        if (!added)
        {
            Debug.Log("Inventory full.");
            PlayerCurrency.Instance.AddCoins(price);
            return;
        }

        // 🔻 Minska lager om item inte är infinite
        if (entry.data.stockType == VendorStockType.Limited)
        {
            entry.currentStock--;

            if (entry.currentStock <= 0)
            {
                entry.currentStock = 0;

                if (entry.data.restockTime > 0)
                    entry.restockTimer = entry.data.restockTime;
            }
        }

        Debug.Log($"Bought {entry.data.item.itemName} for {price} coins.");
    }

    public void TryRightClickInteract(PlayerStats player)
    {
        if (!CanTrade(FindFirstObjectByType<PlayerReputationManager>()))
            return;

        float dist = Vector3.Distance(transform.position, player.transform.position);

        if (dist > 3f)
            return;

        OpenVendor();
    }

    public int GetModifiedPrice(VendorItem entry)
    {

        int basePrice = entry.vendorPrice;

        PlayerReputationManager rep =
            FindFirstObjectByType<PlayerReputationManager>();

        if (rep == null || faction == null)
            return basePrice;

        ReputationState state = rep.GetReputationState(faction);

        float modifier = 1f;

        switch (state)
        {
            case ReputationState.Hated:
                return -1;

            case ReputationState.Untrusted:
                modifier = 1.15f;
                break;

            case ReputationState.Indifferent:
                modifier = 1f;
                break;

            case ReputationState.Favoured:
                modifier = 0.97f;
                break;

            case ReputationState.Renowned:
                modifier = 0.94f;
                break;

            case ReputationState.Praised:
                modifier = 0.91f;
                break;

            case ReputationState.Revered:
                modifier = 0.88f;
                break;
        }

        return Mathf.RoundToInt(basePrice * modifier);
    }

    public void BuyFromPlayer(ItemData item, int quantity)
    {
        if (item == null)
            return;

        int totalPrice = item.SellPrice * quantity;

        PlayerCurrency.Instance.AddCoins(totalPrice);

        AddBuybackItem(item, quantity, totalPrice);
    }

    void AddBuybackItem(ItemData item, int quantity, int price)
    {
        PlayerReputationManager rep =
            FindFirstObjectByType<PlayerReputationManager>();

        if (rep == null || faction == null)
            return;

        if (rep.GetReputationState(faction) < ReputationState.Favoured)
            return;

        int remaining = quantity;

        // 🔹 STACKABLE ITEMS
        if (item.stackable)
        {
            // Försök fylla befintliga stacks
            foreach (var entry in buybackItems)
            {
                if (entry.item == item && entry.quantity < item.maxStack)
                {
                    int space = item.maxStack - entry.quantity;
                    int add = Mathf.Min(space, remaining);

                    entry.quantity += add;

                    // proportionell price
                    int pricePerItem = price / quantity;
                    entry.pricePaid += pricePerItem * add;

                    entry.expiryTime = Time.time + buybackLifetime;

                    remaining -= add;

                    if (remaining <= 0)
                        return;
                }
            }

            // Skapa nya stacks om det finns kvar
            while (remaining > 0)
            {
                int add = Mathf.Min(item.maxStack, remaining);

                int pricePerItem = price / quantity;

                BuybackEntry newEntry = new BuybackEntry
                {
                    item = item,
                    quantity = add,
                    pricePaid = pricePerItem * add,
                    expiryTime = Time.time + buybackLifetime
                };

                buybackItems.Insert(0, newEntry);

                remaining -= add;
            }
        }
        else
        {
            // 🔹 NON-STACKABLE ITEMS
            for (int i = 0; i < quantity; i++)
            {
                BuybackEntry newEntry = new BuybackEntry
                {
                    item = item,
                    quantity = 1,
                    pricePaid = price / quantity,
                    expiryTime = Time.time + buybackLifetime
                };

                buybackItems.Insert(0, newEntry);
            }
        }

        // 🔻 Trim list
        while (buybackItems.Count > maxBuybackSlots)
        {
            buybackItems.RemoveAt(buybackItems.Count - 1);
        }
    }

    void UpdateRestocks()
    {
        foreach (var item in runtimeItems)
        {
            if (item.data.stockType == VendorStockType.Infinite)
                continue;

            if (item.currentStock > 0)
                continue;

            if (item.data.restockTime <= 0)
                continue;

            item.restockTimer -= Time.deltaTime;

            if (item.restockTimer <= 0f)
            {
                item.currentStock = item.data.maxStock;
                item.restockTimer = 0f;

                if (VendorUI.Instance != null && VendorUI.Instance.ActiveVendor == this)
                {
                    VendorUI.Instance.RefreshVendorUI();
                }
            }
        }
    }
    public Faction GetFaction()
    {
        return faction;
    }
}