using UnityEngine;

[CreateAssetMenu(menuName = "Stats/Stat Definition")]
public class StatDefinition : ScriptableObject
{
    public StatType stat;

    public string displayName;

    public StatCategory category;

    public bool editable = true;

    public bool visible = true;
}

public enum StatCategory
{
    Primary,
    Survival,
    Combat,
    Mobility,
    Defense,
    Magic,
    Misc
}
