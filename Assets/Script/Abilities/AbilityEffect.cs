using UnityEngine;

public abstract class AbilityEffect : ScriptableObject, ITooltipProvider
{
    public Sprite icon;

    [Header("Stacking")]
    public bool stackable = false;
    public int maxStacks = 1;
    public bool refreshDurationOnStack = true;

    [Header("Lifecycle")]
    public bool removeOnDeath = true;

    public abstract void Apply(CharacterStats caster, CharacterStats target);

    // 🔥 NYTT
    public virtual string GetTooltipText(CharacterStats caster)
    {
        return "";
    }

    public virtual TooltipData GetTooltipData(CharacterStats caster)
    {
        TooltipData data = new TooltipData();

        data.title = name;
        data.description = GetTooltipText(caster);

        return data;
    }
}
