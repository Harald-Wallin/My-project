using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public sealed class FavourWindow :
    MonoBehaviour, IUIWindow
{
    private readonly List<
        FavourObjectiveRowUI>
        objectiveRows =
            new();

    private readonly List<
        FavourRewardEntryUI>
        fixedRewardViews =
            new();

    private readonly List<GameObject>
    rewardChoiceObjects =
        new();

    private readonly List<FavourRewardEntryUI>
        rewardChoiceEntries =
            new();

    [Header("Root")]

    [SerializeField]
    private GameObject windowRoot;

    [SerializeField]
    private RectTransform windowPanel;

    [Header("Header")]

    [SerializeField]
    private TMP_Text statusText;

    [SerializeField]
    private TMP_Text favourNameText;

    [SerializeField]
    private TMP_Text giverNameText;

    [SerializeField]
    private TMP_Text dialogueText;

    [Header("Objectives")]

    [SerializeField]
    private Transform objectiveContainer;

    [SerializeField]
    private FavourObjectiveRowUI
        objectiveRowPrefab;

    [Header("Rewards")]

    [SerializeField]
    private GameObject fixedRewardsSection;

    [SerializeField]
    private Transform fixedRewardContainer;

    [SerializeField]
    private FavourRewardEntryUI
        fixedRewardPrefab;

    [SerializeField]
    private GameObject rewardChoiceSection;

    [SerializeField]
    private Transform rewardChoiceContent;

    [SerializeField]
    private TMP_Text rewardChoiceTitlePrefab;

    [SerializeField]
    private RectTransform rewardChoiceRowPrefab;

    [SerializeField]
    private FavourRewardEntryUI
        rewardChoiceEntryPrefab;

    [SerializeField]
    private TMP_Text experienceRewardText;

    [SerializeField]
    private TMP_Text reputationRewardText;

    [Header("Buttons")]

    [SerializeField]
    private Button acceptButton;

    [SerializeField]
    private Button completeButton;

    [SerializeField]
    private Button closeButton;

    [Header("Main Sections")]

    [SerializeField]
    private GameObject objectivesSection;

    [SerializeField]
    private GameObject rewardsSection;

    public static FavourWindow Instance
    {
        get;
        private set;
    }

    public bool IsOpen =>
        windowRoot != null &&
        windowRoot.activeSelf;

    public FavourRuntime CurrentRuntime
    {
        get;
        private set;
    }

    public FavourGiver CurrentGiver
    {
        get;
        private set;
    }

    private void Awake()
    {
        if (Instance != null &&
            Instance != this)
        {
            Debug.LogWarning(
                "Flera FavourWindow hittades. " +
                "Den nya komponenten stängs av.",
                this
            );

            enabled = false;
            return;
        }

        Instance = this;

        ConfigureButtons();

        SetOpen(
            false
        );
    }

    private void OnDestroy()
    {
        UnsubscribeFromRuntime();

        if (acceptButton != null)
        {
            acceptButton.onClick
                .RemoveListener(
                    HandleAcceptClicked
                );
        }

        if (completeButton != null)
        {
            completeButton.onClick
                .RemoveListener(
                    HandleCompleteClicked
                );
        }

        if (closeButton != null)
        {
            closeButton.onClick
                .RemoveListener(
                    Close
                );
        }

        if (Instance == this)
        {
            Instance = null;
        }
    }

    private void ConfigureButtons()
    {
        if (acceptButton != null)
        {
            acceptButton.onClick
                .AddListener(
                    HandleAcceptClicked
                );
        }

        if (completeButton != null)
        {
            completeButton.onClick
                .AddListener(
                    HandleCompleteClicked
                );
        }

        if (closeButton != null)
        {
            closeButton.onClick
                .AddListener(
                    Close
                );
        }
    }

    public void Open(FavourGiver giver)
    {
        Open(
            giver,
            (InteractionTarget)null);
    }

    public void Open(
        FavourGiver giver,
        InteractionTarget interactionTarget)
    {
        if (giver == null)
            return;

        List<FavourRuntime> visibleFavours =
            giver.GetVisibleFavours();

        if (visibleFavours == null ||
            visibleFavours.Count == 0)
        {
            Debug.Log(
                $"{giver.GiverName} har inga synliga favours.",
                giver);

            return;
        }

        Open(
            giver,
            visibleFavours[0],
            interactionTarget);
    }

    public void Open(
        FavourGiver giver,
        FavourRuntime runtime)
    {
        Open(
            giver,
            runtime,
            null);
    }

    public void Open(
        FavourGiver giver,
        FavourRuntime runtime,
        InteractionTarget interactionTarget)
    {
        if (giver == null ||
            runtime == null)
        {
            return;
        }

        UnsubscribeFromRuntime();

        CurrentGiver = giver;
        CurrentRuntime = runtime;

        SubscribeToRuntime();

        SetOpen(true);

        if (interactionTarget != null)
        {
            GlobalUIManager.Instance?
                .RegisterInteractionWindow(
                    this,
                    interactionTarget.InteractionTransform,
                    interactionTarget.WindowCloseDistance);
        }

        RebuildAll();
    }

    private void RegisterAsInteractionWindow(
    FavourGiver giver)
    {
        if (giver == null)
            return;

        InteractionTarget target =
            giver.GetComponentInChildren<
                InteractionTarget>();

        if (target == null)
        {
            Debug.LogWarning(
                $"FavourGiver '{giver.name}' saknar " +
                "InteractionTarget i sin hierarchy.",
                giver);

            return;
        }

        GlobalUIManager.Instance?
            .RegisterInteractionWindow(
                this,
                target.InteractionTransform,
                target.WindowCloseDistance);
    }

    public void Close()
    {
        UnsubscribeFromRuntime();

        CurrentRuntime = null;
        CurrentGiver = null;

        ClearDynamicContent();
        ClearRewardChoices();

        SetOpen(false);

        GlobalUIManager.Instance?.ClearInteractionWindow(this);
    }



    private void SubscribeToRuntime()
    {
        if (CurrentRuntime == null)
            return;

        CurrentRuntime.StateChanged +=
            HandleRuntimeStateChanged;

        CurrentRuntime.ProgressChanged +=
            HandleRuntimeProgressChanged;

        CurrentRuntime
            .RewardSelectionChanged +=
            HandleRewardSelectionChanged;
    }

    private void UnsubscribeFromRuntime()
    {
        if (CurrentRuntime == null)
            return;

        CurrentRuntime.StateChanged -=
            HandleRuntimeStateChanged;

        CurrentRuntime.ProgressChanged -=
            HandleRuntimeProgressChanged;

        CurrentRuntime
            .RewardSelectionChanged -=
            HandleRewardSelectionChanged;
    }

    private void HandleRuntimeStateChanged(
    FavourRuntime runtime)
    {
        if (runtime != CurrentRuntime)
            return;

        /*
         * State kan ändra hela presentationen.
         * Completed ska exempelvis ta bort objectives och rewards.
         */
        RebuildAll();
    }

    private void RefreshLayout()
    {
        Canvas.ForceUpdateCanvases();

        if (windowPanel != null)
        {
            LayoutRebuilder
                .ForceRebuildLayoutImmediate(
                    windowPanel
                );
        }
    }

    private void HandleRuntimeProgressChanged(
    FavourRuntime runtime)
    {
        if (runtime != CurrentRuntime)
            return;

        RebuildObjectives();
        RefreshRewardChoices();
    }

    private void HandleRewardSelectionChanged(
    FavourRuntime runtime)
    {
        if (runtime != CurrentRuntime)
            return;

        RefreshRewardChoices();
    }

    private void RebuildAll()
    {
        RefreshStaticText();

        bool completed =
            CurrentRuntime != null &&
            CurrentRuntime.State ==
            FavourState.Completed;

        if (completed)
        {
            ApplyCompletedPresentation();
            RefreshButtons();
            RefreshLayout();

            return;
        }

        ApplyStandardPresentation();

        RebuildObjectives();
        RebuildRewards();

        RefreshButtons();
        RefreshLayout();
    }

    private void ApplyCompletedPresentation()
    {
        /*
         * Rensa först de instansierade objekten så att de inte
         * ligger kvar osynligt och återanvänds av misstag.
         */
        ClearObjectiveRows();
        ClearFixedRewards();
        ClearRewardChoices();

        if (objectivesSection != null)
        {
            objectivesSection.SetActive(
                false
            );
        }

        if (rewardsSection != null)
        {
            rewardsSection.SetActive(
                false
            );
        }

        if (statusText != null)
        {
            statusText.text =
                string.Empty;

            statusText.gameObject.SetActive(
                false
            );
        }

        /*
         * Close ska alltid finnas i det färdiga läget.
         */
        if (closeButton != null)
        {
            closeButton.gameObject.SetActive(
                true
            );

            closeButton.interactable =
                true;
        }
    }

    private void ApplyStandardPresentation()
    {
        if (objectivesSection != null)
        {
            objectivesSection.SetActive(
                true
            );
        }

        if (rewardsSection != null)
        {
            rewardsSection.SetActive(
                true
            );
        }

        if (closeButton != null)
        {
            closeButton.gameObject.SetActive(
                true
            );

            closeButton.interactable =
                true;
        }
    }

    private void RefreshStaticText()
    {
        if (CurrentRuntime == null)
            return;

        if (favourNameText != null)
        {
            favourNameText.text =
                CurrentRuntime.DisplayName;
        }

        if (giverNameText != null)
        {
            giverNameText.text =
                CurrentGiver != null
                    ? CurrentGiver.GiverName
                    : string.Empty;
        }

        if (dialogueText != null)
        {
            string dialogue =
                CurrentRuntime.CurrentDialogue;

            if (string.IsNullOrWhiteSpace(
                    dialogue))
            {
                dialogue =
                    CurrentRuntime.Description;
            }

            dialogueText.text =
                dialogue;
        }

        RefreshStatusText();
    }

    private void RefreshStatusText()
    {
        if (statusText == null ||
            CurrentRuntime == null)
        {
            return;
        }

        switch (CurrentRuntime.State)
        {
            case FavourState.Failed:
                statusText.text =
                    "This favour has failed.";

                statusText.gameObject
                    .SetActive(
                        true
                    );

                break;

            case FavourState.Cooldown:
                statusText.text =
                    "This favour is currently on cooldown.";

                statusText.gameObject
                    .SetActive(
                        true
                    );

                break;

            default:
                statusText.text =
                    string.Empty;

                statusText.gameObject
                    .SetActive(
                        false
                    );

                break;
        }
    }

    private void RebuildObjectives()
    {
        ClearObjectiveRows();

        if (CurrentRuntime == null ||
            objectiveContainer == null ||
            objectiveRowPrefab == null)
        {
            return;
        }

        foreach (FavourObjectiveRuntime objective
                 in CurrentRuntime.Objectives)
        {
            if (objective == null)
                continue;

            FavourObjectiveRowUI row =
                Instantiate(
                    objectiveRowPrefab,
                    objectiveContainer
                );

            row.Bind(
                objective
            );

            objectiveRows.Add(
                row
            );
        }

        if (CurrentRuntime
            .ShouldShowReturnToGiverObjective)
        {
            FavourObjectiveRowUI returnRow =
                Instantiate(
                    objectiveRowPrefab,
                    objectiveContainer
                );

            returnRow.BindReturnToGiver(
                CurrentGiver != null
                    ? CurrentGiver.GiverName
                    : string.Empty,
                CurrentRuntime.State ==
                    FavourState.ReadyToTurnIn
            );

            objectiveRows.Add(
                returnRow
            );
        }
    }

    private void ClearRewardChoices()
    {
        foreach (FavourRewardEntryUI entry
                 in rewardChoiceEntries)
        {
            if (entry != null)
            {
                entry.gameObject.SetActive(
                    false
                );
            }
        }

        rewardChoiceEntries.Clear();

        foreach (GameObject choiceObject
                 in rewardChoiceObjects)
        {
            if (choiceObject == null)
                continue;

            /*
             * Inaktivering tar bort objektet från layouten direkt.
             * Destroy sker vid slutet av framen.
             */
            choiceObject.SetActive(
                false
            );

            Destroy(
                choiceObject
            );
        }

        rewardChoiceObjects.Clear();

        if (rewardChoiceSection != null)
        {
            rewardChoiceSection.SetActive(
                false
            );
        }
    }

    private void RebuildRewards()
    {
        ClearFixedRewards();
        ClearRewardChoices();

        if (CurrentRuntime == null)
        {
            SetRewardSectionsVisible(
                false,
                false
            );

            RefreshExperienceAndCurrency();
            RefreshReputationRewards();

            return;
        }

        BuildFixedItemRewards();
        BuildFixedAbilityRewards();
        BuildFixedCurrencyReward();
        BuildRewardChoices();

        bool hasFixedRewards =
            fixedRewardViews.Count > 0;

        bool hasRewardChoices =
            rewardChoiceEntries.Count > 0;

        SetRewardSectionsVisible(
            hasFixedRewards,
            hasRewardChoices
        );

        RefreshExperienceAndCurrency();
        RefreshReputationRewards();
    }

    private void SetRewardSectionsVisible(
    bool showFixedRewards,
    bool showRewardChoices)
    {
        if (fixedRewardsSection != null)
        {
            fixedRewardsSection.SetActive(
                showFixedRewards
            );
        }

        if (rewardChoiceSection != null)
        {
            rewardChoiceSection.SetActive(
                showRewardChoices
            );
        }
    }

    private void BuildFixedItemRewards()
    {
        if (CurrentRuntime == null ||
            fixedRewardContainer == null ||
            fixedRewardPrefab == null ||
            CurrentRuntime.ItemRewards == null)
        {
            return;
        }

        foreach (FavourItemReward reward
                 in CurrentRuntime.ItemRewards)
        {
            if (reward == null ||
                reward.Item == null ||
                reward.Amount <= 0)
            {
                continue;
            }

            FavourRewardEntryUI view =
                Instantiate(
                    fixedRewardPrefab,
                    fixedRewardContainer
                );

            view.BindFixedItem(
                reward
            );

            fixedRewardViews.Add(
                view
            );
        }
    }

    private void BuildFixedAbilityRewards()
    {
        if (CurrentRuntime == null ||
            fixedRewardContainer == null ||
            fixedRewardPrefab == null ||
            CurrentRuntime.AbilityRewards == null)
        {
            return;
        }

        foreach (FavourAbilityReward reward
                 in CurrentRuntime.AbilityRewards)
        {
            if (reward == null ||
                reward.Ability == null)
            {
                continue;
            }

            FavourRewardEntryUI view =
                Instantiate(
                    fixedRewardPrefab,
                    fixedRewardContainer
                );

            view.BindFixedAbility(
                reward
            );

            fixedRewardViews.Add(
                view
            );
        }
    }

    private void BuildFixedCurrencyReward()
    {
        if (CurrentRuntime == null ||
            CurrentRuntime.CurrencyReward <= 0 ||
            fixedRewardContainer == null ||
            fixedRewardPrefab == null)
        {
            return;
        }

        PlayerCurrency playerCurrency =
            CurrentRuntime.Manager != null
                ? CurrentRuntime.Manager
                    .PlayerCurrency
                : PlayerCurrency.Instance;

        CurrencyData currency =
            playerCurrency != null
                ? playerCurrency
                    .CurrencyDefinition
                : null;

        if (currency == null)
        {
            Debug.LogWarning(
                $"Kan inte visa currency reward för " +
                $"'{CurrentRuntime.DisplayName}': " +
                $"{nameof(CurrencyData)} saknas.",
                this
            );

            return;
        }

        FavourRewardEntryUI view =
            Instantiate(
                fixedRewardPrefab,
                fixedRewardContainer
            );

        view.BindFixedCurrency(
            currency,
            CurrentRuntime.CurrencyReward
        );

        fixedRewardViews.Add(
            view
        );
    }

    private void BuildRewardChoices()
    {
        if (CurrentRuntime == null ||
            rewardChoiceContent == null ||
            rewardChoiceTitlePrefab == null ||
            rewardChoiceRowPrefab == null ||
            rewardChoiceEntryPrefab == null)
        {
            return;
        }

        foreach (
            FavourRewardChoiceRuntime choiceGroup
            in CurrentRuntime.RewardChoices)
        {
            if (choiceGroup == null)
                continue;

            int validOptionCount =
                CountValidRewardOptions(
                    choiceGroup
                );

            if (validOptionCount == 0)
                continue;

            TMP_Text title =
                Instantiate(
                    rewardChoiceTitlePrefab,
                    rewardChoiceContent
                );

            title.text =
                BuildRewardChoiceTitle(
                    choiceGroup
                );

            rewardChoiceObjects.Add(
                title.gameObject
            );

            RectTransform optionRow =
                Instantiate(
                    rewardChoiceRowPrefab,
                    rewardChoiceContent
                );

            rewardChoiceObjects.Add(
                optionRow.gameObject
            );

            foreach (
                FavourRewardChoiceOptionRuntime option
                in choiceGroup.Options)
            {
                if (option == null ||
                    !option.IsValid)
                {
                    continue;
                }

                FavourRewardEntryUI entry =
                    Instantiate(
                        rewardChoiceEntryPrefab,
                        optionRow
                    );

                entry.BindChoice(
                    option
                );

                rewardChoiceEntries.Add(
                    entry
                );
            }
        }
    }

    private static string BuildRewardChoiceTitle(
    FavourRewardChoiceRuntime choiceGroup)
    {
        int amount =
            Mathf.Max(
                1,
                choiceGroup.RequiredSelections
            );

        return $"Choose {amount}:";
    }

    private static int CountValidRewardOptions(
    FavourRewardChoiceRuntime choiceGroup)
    {
        if (choiceGroup?.Options == null)
            return 0;

        int count = 0;

        foreach (
            FavourRewardChoiceOptionRuntime option
            in choiceGroup.Options)
        {
            if (option != null &&
                option.IsValid)
            {
                count++;
            }
        }

        return count;
    }

    private void RefreshExperienceAndCurrency()
    {
        if (experienceRewardText == null ||
            CurrentRuntime == null)
        {
            return;
        }

        int experience =
            CurrentRuntime.ExperienceReward;

        bool hasExperience =
            experience > 0;

        experienceRewardText.gameObject
            .SetActive(
                hasExperience
            );

        experienceRewardText.text =
            hasExperience
                ? $"Experience: {experience}"
                : string.Empty;
    }

    private void RefreshRewardChoices()
    {
        foreach (
            FavourRewardEntryUI entry
            in rewardChoiceEntries)
        {
            entry?.RefreshChoiceVisual();
        }

        RefreshButtons();
    }

    private void RefreshReputationRewards()
    {
        if (reputationRewardText == null ||
            CurrentRuntime == null)
        {
            return;
        }

        IReadOnlyList<
            FavourReputationReward>
            rewards =
                CurrentRuntime
                    .ReputationRewards;

        if (rewards == null ||
            rewards.Count == 0)
        {
            reputationRewardText.text =
                string.Empty;

            reputationRewardText.gameObject
                .SetActive(
                    false
                );

            return;
        }

        StringBuilder builder =
            new();

        foreach (FavourReputationReward reward
                 in rewards)
        {
            if (reward == null ||
                reward.Faction == null ||
                reward.Amount == 0)
            {
                continue;
            }

            if (builder.Length > 0)
            {
                builder.Append(
                    ", "
                );
            }

            string factionName =
                string.IsNullOrWhiteSpace(
                    reward.Faction.factionName)
                    ? reward.Faction.name
                    : reward.Faction
                        .factionName;

            builder.Append(
                reward.Amount > 0
                    ? "+"
                    : string.Empty
            );

            builder.Append(
                reward.Amount
            );

            builder.Append(
                " "
            );

            builder.Append(
                factionName
            );
        }

        bool hasText =
            builder.Length > 0;

        reputationRewardText.gameObject
            .SetActive(
                hasText
            );

        reputationRewardText.text =
            hasText
                ? builder.ToString()
                : string.Empty;
    }

    private void RefreshButtons()
    {
        if (CurrentRuntime == null)
            return;

        if (acceptButton != null)
        {
            bool visible =
                CurrentRuntime.State ==
                FavourState.Available;

            acceptButton.gameObject.SetActive(
                visible
            );

            acceptButton.interactable =
                visible &&
                CurrentRuntime.CanAccept;
        }

        if (completeButton != null)
        {
            bool visible =
                CurrentRuntime.State ==
                FavourState.ReadyToTurnIn;

            completeButton.gameObject.SetActive(
                visible
            );

            completeButton.interactable =
                visible &&
                CurrentRuntime.CanTurnIn;
        }

        if (closeButton != null)
        {
            closeButton.gameObject.SetActive(
                true
            );

            closeButton.interactable =
                true;
        }
    }

    private void HandleAcceptClicked()
    {
        if (CurrentRuntime == null ||
            CurrentGiver == null)
        {
            return;
        }

        FavourData favour =
            CurrentRuntime.Data;

        bool accepted =
            CurrentGiver.TryAccept(
                favour);

        if (accepted)
        {
            Close();
        }
    }

    private void HandleCompleteClicked()
    {
        if (CurrentRuntime == null ||
            CurrentGiver == null)
        {
            return;
        }

        FavourData favour =
            CurrentRuntime.Data;

        bool completed =
            CurrentGiver.TryTurnIn(
                favour);

        if (completed)
        {
            Close();
        }
    }

    private void ClearDynamicContent()
    {
        ClearObjectiveRows();
        ClearFixedRewards();
    }

    private void ClearObjectiveRows()
    {
        foreach (FavourObjectiveRowUI row
                 in objectiveRows)
        {
            if (row != null)
            {
                Destroy(
                    row.gameObject
                );
            }
        }

        objectiveRows.Clear();
    }

    private void ClearFixedRewards()
    {
        foreach (FavourRewardEntryUI view
                 in fixedRewardViews)
        {
            if (view == null)
                continue;

            view.gameObject.SetActive(
                false
            );

            Destroy(
                view.gameObject
            );
        }

        fixedRewardViews.Clear();

        if (fixedRewardsSection != null)
        {
            fixedRewardsSection.SetActive(
                false
            );
        }
    }

    private void SetOpen(
        bool open)
    {
        if (windowRoot != null)
        {
            windowRoot.SetActive(
                open
            );
        }
        else
        {
            gameObject.SetActive(
                open
            );
        }
    }
}
