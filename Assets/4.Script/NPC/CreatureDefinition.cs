using UnityEngine;

[CreateAssetMenu(
    menuName = "RPG/Characters/Creature Definition"
)]
public sealed class CreatureDefinition :
    ScriptableObject
{
    [Header("Identity")]

    [SerializeField]
    [Tooltip(
        "Permanent ID som används av Favours och save/load. " +
        "Ändra inte efter att spelet har släppts."
    )]
    private string id;

    [SerializeField]
    private string displayName;

    [Header("Presentation")]

    [SerializeField]
    private Sprite icon;

    public string Id => id;

    public string DisplayName =>
        string.IsNullOrWhiteSpace(displayName)
            ? name
            : displayName;

    public Sprite Icon => icon;

#if UNITY_EDITOR
    private void OnValidate()
    {
        id = id?.Trim();

        if (string.IsNullOrWhiteSpace(id))
        {
            Debug.LogWarning(
                $"CreatureDefinition '{name}' saknar permanent ID.",
                this
            );
        }
    }
#endif
}
