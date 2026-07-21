using UnityEngine;

[CreateAssetMenu(menuName = "RPG/Effects/Injury")]
public class InjuryEffect : AbilityEffect
{
    public StatType stat;
    public float value; // negativ
    public ModifierType type;

    public float duration = 240f; // 4 min default

    public override void Apply(CharacterStats caster, CharacterStats target)
    {
        // handled by BuffSystem
    }

    public override string GetTooltipText(CharacterStats caster)
    {
        if (type == ModifierType.Percent)
        {
            float percent = value * 100f;

            return $"{stat} {percent:0}%";
        }

        return $"{stat} {value}";
    }
}
