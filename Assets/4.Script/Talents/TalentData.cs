using UnityEngine;

[CreateAssetMenu(menuName = "RPG/Talent")]
public class TalentData : ScriptableObject
{
    public string talentName;

    [TextArea]
    public string description;

    public Sprite icon;

    [Header("Scaling")]
    public int maxPoints = 5;

    [Header("Effects")]
    public AbilityEffect[] effects;

    [Header("Unlocks")]
    public AbilityData unlockedAbility;

    [Tooltip(
        "En AbilityData vars Usage Type är BaseAttack."
    )]
    public AbilityData unlockedBaseAttack;

    [Header("Ward")]
    public bool unlocksWardSystem;

    [Header("Tier")]
    public int tier = 1;

    [Header("Requirements")]
    public TalentRequirement[] requirements;

    public TooltipData GetTooltipData(
        CharacterStats caster,
        int currentPoints)
    {
        TooltipData data =
            new TooltipData
            {
                title = talentName,
                description = description
            };

        // CURRENT
        if (currentPoints > 0 &&
            effects != null)
        {
            foreach (AbilityEffect effect in effects)
            {
                if (effect is not StatModifierEffect stat)
                    continue;

                float value =
                    stat.value *
                    currentPoints;

                data.stats.Add(
                    StatFormatting.FormatModifier(
                        stat.stat,
                        stat.type,
                        value
                    )
                );
            }
        }

        AddAbilityUnlockTooltip(
            data,
            caster,
            currentPoints
        );

        AddBaseAttackUnlockTooltip(
            data,
            caster,
            currentPoints
        );

        // NEXT RANK
        if (currentPoints < maxPoints)
        {
            if (currentPoints > 0)
            {
                data.stats.Add("");
            }

            data.stats.Add(
                "<color=yellow>Next Rank:</color>"
            );

            if (effects != null)
            {
                foreach (AbilityEffect effect in effects)
                {
                    if (effect is not StatModifierEffect stat)
                        continue;

                    float value =
                        stat.value *
                        (currentPoints + 1);

                    data.stats.Add(
                        StatFormatting.FormatModifier(
                            stat.stat,
                            stat.type,
                            value
                        )
                    );
                }
            }
        }

        AddRequirementsTooltip(
            data
        );

        data.showFooter = false;

        return data;
    }

    private void AddAbilityUnlockTooltip(
        TooltipData data,
        CharacterStats caster,
        int currentPoints)
    {
        if (unlockedAbility == null)
            return;

        AddUnlockedEntryTooltip(
            data,
            unlockedAbility,
            caster,
            currentPoints
        );
    }

    private void AddBaseAttackUnlockTooltip(
        TooltipData data,
        CharacterStats caster,
        int currentPoints)
    {
        if (unlockedBaseAttack == null)
            return;

        AddUnlockedEntryTooltip(
            data,
            unlockedBaseAttack,
            caster,
            currentPoints
        );
    }

    private static void AddUnlockedEntryTooltip(
        TooltipData data,
        AbilityData ability,
        CharacterStats caster,
        int currentPoints)
    {
        string prefix =
            currentPoints == 0
                ? "Unlocks"
                : "Unlocked";

        data.stats.Add(
            $"<color=#66FFAA>" +
            $"{prefix}: {ability.abilityName}" +
            $"</color>"
        );

        data.stats.Add("");

        TooltipData abilityTooltip =
            ability.GetTooltipData(
                caster
            );

        if (!string.IsNullOrEmpty(
                abilityTooltip.description))
        {
            data.stats.Add(
                abilityTooltip.description
            );
        }

        foreach (string line in
                 abilityTooltip.stats)
        {
            data.stats.Add(
                line
            );
        }
    }

    private void AddRequirementsTooltip(
        TooltipData data)
    {
        if (requirements == null ||
            requirements.Length == 0)
        {
            return;
        }

        data.stats.Add("");

        data.stats.Add(
            "<color=yellow>Requirements:</color>"
        );

        foreach (TalentRequirement requirement in
                 requirements)
        {
            if (requirement == null ||
                requirement.talent == null)
            {
                continue;
            }

            TalentRuntime runtime =
                TalentManager.Instance != null
                    ? TalentManager.Instance
                        .talents
                        .Find(
                            talent =>
                                talent.data ==
                                requirement.talent
                        )
                    : null;

            bool fulfilled =
                runtime != null &&
                runtime.currentPoints >=
                requirement.requiredPoints;

            string color =
                fulfilled
                    ? "#66FF66"
                    : "#FF6666";

            int current =
                runtime != null
                    ? runtime.currentPoints
                    : 0;

            data.stats.Add(
                $"<color={color}>" +
                $"{requirement.talent.talentName} " +
                $"({current}/" +
                $"{requirement.requiredPoints})" +
                $"</color>"
            );
        }
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        maxPoints =
            Mathf.Max(
                1,
                maxPoints
            );

        tier =
            Mathf.Max(
                1,
                tier
            );

        if (unlockedAbility != null &&
            unlockedAbility.IsBaseAttack)
        {
            Debug.LogWarning(
                $"Talent '{talentName}' har en base attack i " +
                $"{nameof(unlockedAbility)}. Flytta den till " +
                $"{nameof(unlockedBaseAttack)}.",
                this
            );
        }

        if (unlockedBaseAttack != null &&
            !unlockedBaseAttack.IsBaseAttack)
        {
            Debug.LogWarning(
                $"Talent '{talentName}' har " +
                $"'{unlockedBaseAttack.abilityName}' i " +
                $"{nameof(unlockedBaseAttack)}, men abilityns " +
                $"Usage Type är inte BaseAttack.",
                this
            );
        }
    }
#endif
}