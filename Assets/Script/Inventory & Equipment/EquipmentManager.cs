using System.Collections.Generic;
using UnityEngine;

public class EquipmentManager : MonoBehaviour
{
    public static EquipmentManager Instance;
    public List<EquipmentSlotUI> equipmentSlots = new List<EquipmentSlotUI>();

    [SerializeField] private Inventory inventory;
    [SerializeField] private PlayerStats playerStats;
    [SerializeField] private HumanoidEquipment humanoidEquipment;



    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        FindEquipmentSlots();
    }

    private void FindEquipmentSlots()
    {
        equipmentSlots.Clear();

        EquipmentSlotUI[] slots =
        FindObjectsByType<EquipmentSlotUI>(FindObjectsInactive.Include, FindObjectsSortMode.None);


        foreach (EquipmentSlotUI slot in slots)
        {
            equipmentSlots.Add(slot);
        }
    }

    private EquipmentSlotUI GetSlotForItem(ItemType itemType)
    {
        foreach (EquipmentSlotUI slot in equipmentSlots)
        {
            if (slot.slotType == itemType)
            {
                return slot;
            }
        }

        return null;
    }

    public void TryEquipItem(ItemData item, int fromSlotIndex)
    {

        if (!item.MeetsRequirements(playerStats))
        {
            Debug.Log("Requirements not met.");
            return;
        }

        if (item == null || !item.equippable)
            return;

        EquipmentSlotUI targetSlot = GetSlotForItem(item.itemType);
        if (targetSlot == null)
            return;

        ItemData existingItem = targetSlot.GetEquippedItem();

        // 🔁 OM SLOT HAR ITEM → SWAP
        if (existingItem != null)
        {
            //Debug.Log($"EquipmentManager.TryEquipItem: swapping '{existingItem?.itemName}' into inventory slot {fromSlotIndex} and equipping '{item?.itemName}'");

            // Ta bort stats från gamla
            RemoveStats(existingItem);

            // Ersätt inventory-slotten direkt
            inventory.slots[fromSlotIndex].item = existingItem;
            inventory.slots[fromSlotIndex].amount = 1;
        }
        else
        {
            //Debug.Log($"EquipmentManager.TryEquipItem: equipping '{item?.itemName}' from inventory slot {fromSlotIndex} (empty target slot)");

            // Om tom → ta bort från inventory
            inventory.RemoveItemAt(fromSlotIndex, 1);
        }

        // Notify inventory UI about the change so UI updates immediately
        inventory.NotifyChanged();

        // ➕ Stats in för nya
        ApplyStats(item);

        // Sätt i equipment slot
        targetSlot.SetItem(item);
        humanoidEquipment.Equip(item);

        WardSystem ward = playerStats.GetComponent<WardSystem>();

        if (ward != null)
        {
            ward.RefreshShieldState();
        }

        // Debug: dump inventory slot after operation
        if (fromSlotIndex >= 0 && fromSlotIndex < inventory.slots.Count)
        {
            var slot = inventory.slots[fromSlotIndex];
            string name = slot.IsEmpty() ? "(empty)" : slot.item.itemName;
            //Debug.Log($"EquipmentManager.TryEquipItem: inventory slot {fromSlotIndex} now contains: {name} (amount {slot.amount})");
        }
    }



    private void ApplyStats(ItemData item)
    {
        if (item.strengthBonus != 0)
            playerStats.AddModifier(new StatModifier(StatType.Strength, item.strengthBonus, ModifierType.Flat, item, ModifierSourceType.Equipment));

        if (item.armorBonus != 0)
            playerStats.AddModifier(new StatModifier(StatType.Armor, item.armorBonus, ModifierType.Flat, item, ModifierSourceType.Equipment));

        if (item.healthBonus != 0)
            playerStats.AddModifier(new StatModifier(StatType.MaxHP, item.healthBonus, ModifierType.Flat, item , ModifierSourceType.Equipment));

        if (item.damageBonus != 0)
            playerStats.AddModifier(new StatModifier(StatType.WeaponDamage, item.damageBonus, ModifierType.Flat, item, ModifierSourceType.Equipment));

        if (item.blockChanceBonus != 0)
            playerStats.AddModifier(new StatModifier(StatType.BlockChance,item.blockChanceBonus,ModifierType.Flat,item,ModifierSourceType.Equipment));

        if (item.blockValueBonus != 0)
            playerStats.AddModifier(new StatModifier(StatType.BlockValue,item.blockValueBonus,ModifierType.Flat,item,ModifierSourceType.Equipment));
    }

    public void RemoveStats(ItemData item)
    {
        playerStats.RemoveModifiersFromSource(item);
    }

    public void Unequip(EquipmentSlotUI slot)
    {
        ItemData item = slot.GetEquippedItem();
        if (item == null)
            return;

        // ➖ STATS UT
        RemoveStats(item);
        humanoidEquipment.Unequip(item);

        //visualEquipment.Unequip(item);
        slot.ClearSlot();

        WardSystem ward = playerStats.GetComponent<WardSystem>();

        if (ward != null)
        {
            ward.RefreshShieldState();
        }

        // 📦 TILLBAKA TILL INVENTORY
        inventory.AddItem(item, 1);
    }
}


