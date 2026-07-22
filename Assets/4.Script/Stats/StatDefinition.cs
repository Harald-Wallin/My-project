using UnityEngine;

[CreateAssetMenu(menuName = "Stats/Stat Definition")]
public class StatDefinition : ScriptableObject
{
    [Header("Identity")]
    public StatType stat;

    public string displayName;

    [Header("Display")]
    public StatDisplayFormat displayFormat =
    StatDisplayFormat.Number;

    [Header("Behavior")]
    public StatKind kind;

    [Tooltip(
        "Default value used when the stat is first added. " +
        "For derived stats, this is also the value before scaling is applied."
    )]
    public float defaultValue;

    [Header("Inspector")]
    public bool editable = true;

    public bool visible = true;

    public string DisplayName =>
        string.IsNullOrWhiteSpace(displayName)
            ? stat.ToString()
            : displayName;
}

public enum StatKind
{
    Primary,
    Derived
}
