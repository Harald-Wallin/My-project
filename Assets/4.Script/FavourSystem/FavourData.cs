using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(
    menuName =
        "RPG/Favours/Favour"
)]
public sealed class FavourData :
    ScriptableObject
{
    [Header("Identity")]

    [SerializeField]
    [Tooltip(
        "Permanent ID för save/load. " +
        "Ändra inte efter release."
    )]
    private string id;

    [SerializeField]
    private string displayName;

    [TextArea(3, 8)]
    [SerializeField]
    private string description;

    [SerializeField]
    private FavourType category =
        FavourType.General;

    [Header("Flow")]

    [SerializeField]
    private FavourActivationPolicy
        activationPolicy =
            FavourActivationPolicy
                .ExplicitAccept;

    [SerializeField]
    private FavourCompletionPolicy
        completionPolicy =
            FavourCompletionPolicy
                .ReturnToGiver;

    // =========================================================
    // OBJECTIVES
    // =========================================================

    [Header("Objectives")]

    [SerializeField]
    private List<FavourObjectiveData>
        objectives =
            new();

    // =========================================================
    // DIALOGUE
    // =========================================================

    [Header("Dialogue")]

    [SerializeField]
    private FavourDialogueSet dialogueSet;

    // =========================================================
    // REWARDS
    // =========================================================

    [Header("Rewards")]

    [SerializeField]
    [Min(0)]
    private int experienceReward;

    [SerializeField]
    [Min(0)]
    private int currencyReward;

    [SerializeField]
    private List<FavourReputationReward>
        reputationRewards =
            new();

    [SerializeField]
    private List<FavourItemReward>
        itemRewards =
            new();

    [SerializeField]
    private List<FavourAbilityReward>
        abilityRewards =
            new();

    [SerializeField]
    [Tooltip(
        "Varje grupp kräver att spelaren väljer det angivna " +
        "antalet rewards innan favouren kan lämnas in."
    )]
    private List<FavourRewardChoiceGroup>
        rewardChoiceGroups =
            new();

    // =========================================================
    // REQUIREMENTS
    // =========================================================

    [Header("Requirements")]

    [SerializeField]
    [Min(0)]
    [Tooltip(
        "0 innebär att favouren saknar level requirement."
    )]
    private int minimumLevel;

    [SerializeField]
    [Min(0)]
    [Tooltip(
        "0 innebär att favouren saknar currency requirement. " +
        "Valutan förbrukas inte."
    )]
    private int minimumCurrency;

    [SerializeField]
    private List<FavourReputationRequirement>
        reputationRequirements =
            new();

    [SerializeField]
    private List<FavourItemRequirement>
        itemRequirements =
            new();

    [SerializeField]
    [Tooltip(
        "Samtliga angivna favours måste vara completed."
    )]
    private List<FavourData>
        requiredCompletedFavours =
            new();

    [SerializeField]
    [Tooltip(
        "Favouren är otillgänglig om någon angiven favour " +
        "redan är completed."
    )]
    private List<FavourData>
        forbiddenCompletedFavours =
            new();

    // =========================================================
    // FAILURE / REPEATABILITY / FOLLOW-UPS
    // =========================================================

    [Header("Failure")]

    [SerializeField]
    private FavourFailureSettings
        failureSettings =
            new();

    [Header("Repeatability")]

    [SerializeField]
    private FavourRepeatSettings
        repeatSettings =
            new();

    [Header("Follow-ups")]

    [SerializeField]
    [Tooltip(
        "Endast UX-hjälp. Requirements avgör fortfarande " +
        "om follow-up-favouren är tillgänglig."
    )]
    private List<FavourData> followUps =
        new();

    // =========================================================
    // IDENTITY API
    // =========================================================

    public string Id =>
        id;

    public string DisplayName =>
        string.IsNullOrWhiteSpace(
            displayName
        )
            ? name
            : displayName;

    public string Description =>
        description;

    public FavourType Category =>
        category;

    // =========================================================
    // FLOW API
    // =========================================================

    public FavourActivationPolicy
        ActivationPolicy =>
            activationPolicy;

    public FavourCompletionPolicy
        CompletionPolicy =>
            completionPolicy;

    // =========================================================
    // OBJECTIVE / DIALOGUE API
    // =========================================================

    public IReadOnlyList<
        FavourObjectiveData>
        Objectives =>
            objectives;

    public FavourDialogueSet DialogueSet =>
        dialogueSet;

    // =========================================================
    // REWARD API
    // =========================================================

    public int ExperienceReward =>
        Mathf.Max(
            0,
            experienceReward
        );

    public int CurrencyReward =>
        Mathf.Max(
            0,
            currencyReward
        );

    public IReadOnlyList<
        FavourReputationReward>
        ReputationRewards =>
            reputationRewards;

    public IReadOnlyList<
        FavourItemReward>
        ItemRewards =>
            itemRewards;

    public IReadOnlyList<
        FavourAbilityReward>
        AbilityRewards =>
            abilityRewards;

    public IReadOnlyList<
        FavourRewardChoiceGroup>
        RewardChoiceGroups =>
            rewardChoiceGroups;

    public bool HasRewardChoices =>
        rewardChoiceGroups != null &&
        rewardChoiceGroups.Count > 0;

    // =========================================================
    // REQUIREMENT API
    // =========================================================

    public int MinimumLevel =>
        Mathf.Max(
            0,
            minimumLevel
        );

    public int MinimumCurrency =>
        Mathf.Max(
            0,
            minimumCurrency
        );

    public IReadOnlyList<
        FavourReputationRequirement>
        ReputationRequirements =>
            reputationRequirements;

    public IReadOnlyList<
        FavourItemRequirement>
        ItemRequirements =>
            itemRequirements;

    public IReadOnlyList<FavourData>
        RequiredCompletedFavours =>
            requiredCompletedFavours;

    public IReadOnlyList<FavourData>
        ForbiddenCompletedFavours =>
            forbiddenCompletedFavours;

    // =========================================================
    // OTHER API
    // =========================================================

    public FavourFailureSettings
        FailureSettings =>
            failureSettings;

    public FavourRepeatSettings
        RepeatSettings =>
            repeatSettings;

    public IReadOnlyList<FavourData>
        FollowUps =>
            followUps;

#if UNITY_EDITOR
    private void OnValidate()
    {
        id =
            id?.Trim();

        experienceReward =
            Mathf.Max(
                0,
                experienceReward
            );

        currencyReward =
            Mathf.Max(
                0,
                currencyReward
            );

        minimumLevel =
            Mathf.Max(
                0,
                minimumLevel
            );

        minimumCurrency =
            Mathf.Max(
                0,
                minimumCurrency
            );

        objectives ??=
            new List<FavourObjectiveData>();

        reputationRewards ??=
            new List<
                FavourReputationReward>();

        itemRewards ??=
            new List<FavourItemReward>();

        abilityRewards ??=
            new List<
                FavourAbilityReward>();

        rewardChoiceGroups ??=
            new List<
                FavourRewardChoiceGroup>();

        reputationRequirements ??=
            new List<
                FavourReputationRequirement>();

        itemRequirements ??=
            new List<
                FavourItemRequirement>();

        requiredCompletedFavours ??=
            new List<FavourData>();

        forbiddenCompletedFavours ??=
            new List<FavourData>();

        followUps ??=
            new List<FavourData>();


        foreach (FavourRewardChoiceGroup
                 group
                 in rewardChoiceGroups)
        {
            group?.Normalize();
        }

        if (string.IsNullOrWhiteSpace(
                id))
        {
            Debug.LogWarning(
                $"FavourData '{name}' saknar permanent ID.",
                this
            );
        }

        if (objectives.Count == 0)
        {
            Debug.LogWarning(
                $"FavourData '{name}' saknar objectives.",
                this
            );
        }

        ValidateRewardEntries();
        ValidateRequirementEntries();
    }

    private void ValidateRewardEntries()
    {
        foreach (FavourReputationReward reward
                 in reputationRewards)
        {
            if (reward != null &&
                reward.Faction == null)
            {
                Debug.LogWarning(
                    $"FavourData '{name}' har en reputation " +
                    $"reward utan Faction.",
                    this
                );
            }
        }

        foreach (FavourItemReward reward
                 in itemRewards)
        {
            if (reward != null &&
                reward.Item == null)
            {
                Debug.LogWarning(
                    $"FavourData '{name}' har en item reward " +
                    $"utan ItemData.",
                    this
                );
            }
        }

        foreach (FavourAbilityReward reward
                 in abilityRewards)
        {
            if (reward != null &&
                reward.Ability == null)
            {
                Debug.LogWarning(
                    $"FavourData '{name}' har en ability reward " +
                    $"utan AbilityData.",
                    this
                );
            }
        }
    }

    private void ValidateRequirementEntries()
    {
        foreach (FavourReputationRequirement
                 requirement
                 in reputationRequirements)
        {
            if (requirement != null &&
                requirement.Faction == null)
            {
                Debug.LogWarning(
                    $"FavourData '{name}' har ett reputation " +
                    $"requirement utan Faction.",
                    this
                );
            }
        }

        foreach (FavourItemRequirement requirement
                 in itemRequirements)
        {
            if (requirement != null &&
                requirement.Item == null)
            {
                Debug.LogWarning(
                    $"FavourData '{name}' har ett item " +
                    $"requirement utan ItemData.",
                    this
                );
            }
        }

        foreach (FavourData required
                 in requiredCompletedFavours)
        {
            if (required == this)
            {
                Debug.LogWarning(
                    $"FavourData '{name}' kräver sig själv.",
                    this
                );
            }
        }

        foreach (FavourData forbidden
                 in forbiddenCompletedFavours)
        {
            if (forbidden == this)
            {
                Debug.LogWarning(
                    $"FavourData '{name}' förbjuder sig själv.",
                    this
                );
            }
        }
    }
#endif
}