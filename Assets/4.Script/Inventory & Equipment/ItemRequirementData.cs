using UnityEngine;

[CreateAssetMenu(
    menuName =
        "RPG/Favours/Requirements/Item Requirement"
)]
public sealed class ItemRequirementData :
    FavourRequirementData
{
    [SerializeField]
    private ItemData item;

    [SerializeField]
    [Min(1)]
    private int requiredAmount = 1;

    public ItemData Item =>
        item;

    public int RequiredAmount =>
        Mathf.Max(
            1,
            requiredAmount
        );

    public override bool IsMet(
        PlayerFavourManager manager)
    {
        if (manager == null ||
            item == null)
        {
            return false;
        }

        Inventory inventory =
            manager.PlayerInventory;

        if (inventory == null)
        {
            inventory =
                Inventory.Instance;
        }

        return inventory != null &&
               inventory.GetItemCount(
                   item
               ) >= RequiredAmount;
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        requiredAmount =
            Mathf.Max(
                1,
                requiredAmount
            );

        if (item == null)
        {
            Debug.LogWarning(
                $"ItemRequirement '{name}' saknar ItemData.",
                this
            );
        }
    }
#endif
}