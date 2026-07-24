using UnityEngine;

public abstract class FavourObjectiveData :
    ScriptableObject
{
    [Header("Presentation")]

    [SerializeField]
    private string displayName;

    [TextArea(2, 5)]
    [SerializeField]
    private string description;

    public string DisplayName =>
        string.IsNullOrWhiteSpace(
            displayName
        )
            ? name
            : displayName;

    public string Description =>
        description;

    public abstract FavourObjectiveRuntime
        CreateRuntime(
            FavourRuntime favour);
}
