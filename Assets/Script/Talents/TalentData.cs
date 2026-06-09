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

    [Header("Ward")]
    public bool unlocksWardSystem;

    [Header("Tier")]
    public int tier = 1;

    [Header("Requirements")]
    public TalentRequirement[] requirements;

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

        // Ability unlock
        // Ability unlock / unlocked
        if (unlockedAbility != null)
        {
            if (currentPoints == 0)
            {
                data.stats.Add(
                    $"<color=#66FFAA>Unlocks: {unlockedAbility.abilityName}</color>"
                );
            }
            else
            {
                data.stats.Add(
                    $"<color=#66FFAA>Unlocked: {unlockedAbility.abilityName}</color>"
                );
            }

            data.stats.Add("");

            TooltipData abilityTooltip =
                unlockedAbility.GetTooltipData(caster);

            if (!string.IsNullOrEmpty(
                abilityTooltip.description))
            {
                data.stats.Add(
                    abilityTooltip.description
                );
            }

            foreach (string line in abilityTooltip.stats)
            {
                data.stats.Add(line);
            }
        }

        // NEXT RANK
        if (currentPoints < maxPoints)
        {
            if (currentPoints > 0)
                data.stats.Add("");

            data.stats.Add("<color=yellow>Next Rank:</color>");

            foreach (var effect in effects)
            {
                if (effect is StatModifierEffect stat)
                {
                    float value =
                        stat.value *
                        (currentPoints + 1);

                    string text =
                        stat.type == ModifierType.Flat
                        ? $"+{value:0} {stat.stat}"
                        : $"+{value * 100f:0}% {stat.stat}";

                    data.stats.Add(text);
                }
            }

        }

        if (requirements != null && requirements.Length > 0)
        {
            data.stats.Add("");
            data.stats.Add(
                "<color=yellow>Requirements:</color>"
            );

            foreach (var req in requirements)
            {
                TalentRuntime runtime =
                    TalentManager.Instance.talents
                    .Find(t => t.data == req.talent);

                bool fulfilled =
                    runtime != null &&
                    runtime.currentPoints >= req.requiredPoints;

                string color =
                    fulfilled
                    ? "#66FF66"
                    : "#FF6666";

                int current =
                    runtime != null
                    ? runtime.currentPoints
                    : 0;

                data.stats.Add(
                    $"<color={color}>{req.talent.talentName} ({current}/{req.requiredPoints})</color>"
                );
            }
        }

        // ❗ viktigt
        data.showFooter = false;

        return data;
    }
}
