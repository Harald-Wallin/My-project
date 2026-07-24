using UnityEngine;

[CreateAssetMenu(
    menuName =
        "RPG/Favours/Objectives/Collect Objective"
)]
public sealed class CollectObjectiveData :
    FavourObjectiveData
{
    [Header("Required Item")]

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

    public override FavourObjectiveRuntime
        CreateRuntime(
            FavourRuntime favour)
    {
        return new CollectObjectiveRuntime(
            this,
            favour
        );
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
                $"CollectObjective '{name}' saknar ItemData.",
                this
            );
        }
        else if (string.IsNullOrWhiteSpace(
                     item.Id))
        {
            Debug.LogWarning(
                $"CollectObjective '{name}' refererar till " +
                $"ItemData '{item.name}' som saknar permanent ID.",
                this
            );
        }
    }
#endif
}
