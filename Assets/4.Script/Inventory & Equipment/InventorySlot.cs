
[System.Serializable]
public class InventorySlot
{
    public ItemData item;
    public int amount;

    public bool IsEmpty()
    {
        return item == null || amount <= 0;
    }

    public void SetItem(ItemData newItem, int newAmount = 1)
    {
        item = newItem;
        amount = newAmount;
    }

    public void AddAmount(int value)
    {
        amount += value;
    }

    public void Clear()
    {
        item = null;
        amount = 0;
    }
}

