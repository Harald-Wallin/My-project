using System.Collections.Generic;
using UnityEngine;
using System;

public class Inventory : MonoBehaviour
{
    public static Inventory Instance;

    [Header("Settings")]
    [SerializeField] private int size = 9;

    [Header("Slots")]
    public List<InventorySlot> slots = new List<InventorySlot>();


    //TEST
    [SerializeField] private ItemData debugTestItemOne;
    [SerializeField] private ItemData debugTestItemTwo;
    [SerializeField] private ItemData debugTestItemThree;
    [SerializeField] private ItemData debugTestItemFour;
    [SerializeField] private ItemData debugTestItemFive;
    [SerializeField] private ItemData debugTestItemSix;
    [SerializeField] private KeyCode debugAddItemKey = KeyCode.L;


    public event Action OnInventoryChanged;

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

        Initialize();
    }



    private void Initialize()
    {
        slots.Clear();

        for (int i = 0; i < size; i++)
        {
            slots.Add(new InventorySlot());
        }
    }

    public void NotifyChanged()
    {
        //Debug.Log("Inventory changed!");

        OnInventoryChanged?.Invoke();
    }


    public bool AddItem(ItemData item, int amount = 1)
    {
        // 1. Försök stacka
        if (item.stackable)
        {
            foreach (var slot in slots)
            {
                if (!slot.IsEmpty() && slot.item == item && slot.amount < item.maxStack)
                {
                    slot.amount += amount;
                    NotifyChanged();

                    return true;
                }
            }
        }

        // 2. Lägg i tom slot
        foreach (var slot in slots)
        {
            if (slot.IsEmpty())
            {
                slot.item = item;
                slot.amount = amount;
                NotifyChanged();
                return true;
            }
        }

        // 3. Inventory fullt
        NotificationManager.Instance?.Show(NotificationManager.Instance.Database.inventoryFull);
        return false;
    }

    public void RemoveItem(ItemData item, int amount = 1)
    {
        foreach (var slot in slots)
        {
            if (!slot.IsEmpty() && slot.item == item)
            {
                slot.amount -= amount;

                if (slot.amount <= 0)
                {
                    slot.item = null;
                    slot.amount = 0;
                }

                NotifyChanged();
                return;
            }
        }
    }

    public void RemoveItemAt(int slotIndex, int amount = 1)
    {
        if (slotIndex < 0 || slotIndex >= slots.Count)
            return;

        InventorySlot slot = slots[slotIndex];

        if (slot.IsEmpty())
            return;

        slot.amount -= amount;

        if (slot.amount <= 0)
        {
            slot.item = null;
            slot.amount = 0;
        }

        NotifyChanged();
    }

    //Testfunktion
    private void Update()
    {
        if (Input.GetKeyDown(debugAddItemKey))
        {
            //Debug.Log("Debug: Add item key pressed");
            AddItem(debugTestItemOne, 1);
            AddItem(debugTestItemTwo, 1);
            AddItem(debugTestItemThree, 1);
            AddItem(debugTestItemFour, 1);
            AddItem(debugTestItemFive, 1);
            AddItem(debugTestItemSix, 1);
        }
    }

    public void SwapSlots(int indexA, int indexB)
    {
        if (indexA < 0 || indexA >= slots.Count) return;
        if (indexB < 0 || indexB >= slots.Count) return;

        if (indexA == indexB) return;

        InventorySlot temp = slots[indexA];
        slots[indexA] = slots[indexB];
        slots[indexB] = temp;

        NotifyChanged();
    }

    public int GetItemCount(ItemData item)
    {
        if (item == null)
            return 0;

        int total = 0;

        foreach (var slot in slots)
        {
            if (!slot.IsEmpty() && slot.item == item)
            {
                total += slot.amount;
            }
        }

        return total;
    }

    public bool RemoveItemAmount(ItemData item, int amount)
    {
        if (item == null || amount <= 0)
            return false;

        int remaining = amount;

        foreach (var slot in slots)
        {
            if (slot.IsEmpty())
                continue;

            if (slot.item != item)
                continue;

            int remove = Mathf.Min(slot.amount, remaining);

            slot.amount -= remove;
            remaining -= remove;

            if (slot.amount <= 0)
            {
                slot.Clear();
            }

            if (remaining <= 0)
            {
                NotifyChanged();
                return true;
            }
        }

        NotifyChanged();

        return remaining <= 0;
    }


}

