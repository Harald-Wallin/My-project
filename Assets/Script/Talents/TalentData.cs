using UnityEngine;

[CreateAssetMenu(menuName = "RPG/Talent")]
public class TalentData : ScriptableObject
{
    public string talentName;
    [TextArea] public string description;
    public Sprite icon;

    [Header("Scaling")]
    public int maxPoints = 5;

    [Header("Effects")]
    public AbilityEffect[] effects;

    [Header("Unlocks")]
    public AbilityData unlockedAbility;

    public BaseAttackData unlockedBaseAttack;

    public TooltipData GetTooltipData(CharacterStats caster, int currentPoints)
    {
        TooltipData data = new TooltipData();

        data.title = talentName;
        data.description = description;

        // ✅ CURRENT (bara om > 0)
        if (currentPoints > 0)
        {

            foreach (var effect in effects)
            {
                if (effect is StatModifierEffect stat)
                {
                    float value = stat.value * currentPoints;

                    string text = stat.type == ModifierType.Flat
                        ? $"+{value:0} {stat.stat}"
                        : $"+{value * 100f:0}% {stat.stat}";

                    data.stats.Add(text);
                }
            }
        }

        // ✅ NEXT RANK
        if (currentPoints < maxPoints)
        {
            if (currentPoints > 0)
                data.stats.Add(""); // spacing bara om vi har current

            data.stats.Add("\n<color=yellow>Next Rank:</color>");

            foreach (var effect in effects)
            {
                if (effect is StatModifierEffect stat)
                {
                    float value = stat.value * (currentPoints + 1);

                    string text = stat.type == ModifierType.Flat
                        ? $"+{value:0} {stat.stat}"
                        : $"+{value * 100f:0}% {stat.stat}";

                    data.stats.Add(text);
                }
            }
        }

        // ❗ viktigt
        data.showFooter = false;

        return data;
    }
}
