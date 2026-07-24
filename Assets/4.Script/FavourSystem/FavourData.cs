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

    [Header("Requirements")]

    [SerializeField]
    private List<FavourRequirementData>
        requirements =
            new();

    [Header("Objectives")]

    [SerializeField]
    private List<FavourObjectiveData>
        objectives =
            new();

    [Header("Dialogue")]

    [SerializeField]
    private FavourDialogueSet dialogueSet;

    [Header("Rewards")]

    [SerializeField]
    private List<FavourRewardData>
        rewards =
            new();

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

    public FavourActivationPolicy
        ActivationPolicy =>
            activationPolicy;

    public FavourCompletionPolicy
        CompletionPolicy =>
            completionPolicy;

    public IReadOnlyList<
        FavourRequirementData>
        Requirements =>
            requirements;

    public IReadOnlyList<
        FavourObjectiveData>
        Objectives =>
            objectives;

    public FavourDialogueSet DialogueSet =>
        dialogueSet;

    public IReadOnlyList<
        FavourRewardData>
        Rewards =>
            rewards;

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

        if (string.IsNullOrWhiteSpace(id))
        {
            Debug.LogWarning(
                $"FavourData '{name}' saknar permanent ID.",
                this
            );
        }

        if (objectives == null ||
            objectives.Count == 0)
        {
            Debug.LogWarning(
                $"FavourData '{name}' saknar objectives.",
                this
            );
        }
    }
#endif
}
