using UnityEngine;

public enum AbilityType
{
    Melee,
    Spell,
    Buff,
    Curse
}

[CreateAssetMenu(menuName = "RPG/Ability")]
public class AbilityData : ScriptableObject, ITooltipProvider
{
    [Header("Basic")]
    public string abilityName;
    [TextArea] public string description;
    public Sprite icon;

    [Header("Type")]
    public AbilityType type;
    public bool isSelfCast = false;

    [Header("Timing")]
    public float cooldown = 5f;
    public float globalCooldown = 0.8f;
    public float castTime = 0f;

    [Header("Effects")]
    public AbilityEffect[] effects;

    public virtual void Use(CharacterStats caster, CharacterStats target)
    {
        if (target == null) return;

        foreach (var effect in effects)
        {
            effect.Apply(caster, target);
        }
    }

    public virtual TooltipData  GetTooltipData(CharacterStats caster)
    {
        TooltipData data = new TooltipData();

        data.title = abilityName;
        data.subtitle = type.ToString();
        data.description = description;

        foreach (var effect in effects)
        {
            string text = effect.GetTooltipText(caster);

            if (!string.IsNullOrEmpty(text))
                data.stats.Add($"<color=#FF5555>{text}</color>");
        }

        if (castTime > 0)
            data.stats.Add($"<color=white>Cast Time: {castTime:0.0}s</color>");
        if (cooldown > 0)
            data.stats.Add(
            $"<color=white>Cooldown: {cooldown:0}s</color>"
);


        return data;
    }
}
