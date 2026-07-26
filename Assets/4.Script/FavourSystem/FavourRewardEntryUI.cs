using TMPro;
using UnityEngine;
using UnityEngine.UI;

public sealed class FavourRewardEntryUI :
    MonoBehaviour
{
    [Header("Visuals")]

    [SerializeField]
    private Image selectionFrame;

    [SerializeField]
    private Image iconImage;

    [SerializeField]
    private TMP_Text stackText;

    [Header("Interaction")]

    [SerializeField]
    private Button button;

    [Header("Selection Frame")]

    [SerializeField]
    [Range(0f, 1f)]
    private float unselectedFrameAlpha = 0.1f;

    [SerializeField]
    [Range(0f, 1f)]
    private float selectedFrameAlpha = 1f;

    private FavourRewardChoiceOptionRuntime
        choiceOption;

    // =========================================================
    // FIXED REWARDS
    // =========================================================

    public void BindFixedItem(
        FavourItemReward reward)
    {
        ClearChoiceBinding();

        if (reward == null ||
            reward.Item == null)
        {
            gameObject.SetActive(
                false
            );

            return;
        }

        gameObject.SetActive(
            true
        );

        SetIcon(
            reward.Item.icon
        );

        SetStackAmount(
            reward.Amount
        );

        SetSelectionFrame(
            false
        );

        SetFixedRewardInteraction();
    }

    public void BindFixedAbility(
        FavourAbilityReward reward)
    {
        ClearChoiceBinding();

        if (reward == null ||
            reward.Ability == null)
        {
            gameObject.SetActive(
                false
            );

            return;
        }

        gameObject.SetActive(
            true
        );

        SetIcon(
            reward.Ability.icon
        );

        /*
         * Abilities har ingen stackstorlek.
         */
        SetStackAmount(
            1
        );

        SetSelectionFrame(
            false
        );

        SetFixedRewardInteraction();
    }

    // =========================================================
    // CHOICE REWARDS
    // =========================================================

    public void BindChoice(
        FavourRewardChoiceOptionRuntime option)
    {
        ClearChoiceBinding();

        choiceOption = option;

        if (choiceOption == null ||
            !choiceOption.IsValid)
        {
            gameObject.SetActive(
                false
            );

            return;
        }

        gameObject.SetActive(
            true
        );

        SetIcon(
            choiceOption.Icon
        );

        if (choiceOption.Type ==
            FavourRewardChoiceType.Item)
        {
            SetStackAmount(
                choiceOption.ItemAmount
            );
        }
        else
        {
            SetStackAmount(
                1
            );
        }

        if (button != null)
        {
            button.onClick.RemoveAllListeners();

            button.onClick.AddListener(
                HandleChoiceClicked
            );
        }

        RefreshChoiceVisual();
    }

    public void RefreshChoiceVisual()
    {
        if (choiceOption == null)
            return;

        SetSelectionFrame(
            choiceOption.IsSelected
        );

        if (button != null)
        {
            /*
             * Ett redan valt alternativ måste fortfarande
             * kunna klickas för att avmarkeras.
             */
            button.interactable =
                choiceOption.IsSelected
                    ? choiceOption.CanDeselect
                    : choiceOption.CanSelect;
        }
    }

    private void HandleChoiceClicked()
    {
        if (choiceOption == null)
            return;

        choiceOption.Toggle();

        /*
         * Detta ger omedelbar respons på den klickade ikonen.
         *
         * FavourRuntime-eventet gör därefter att hela gruppen
         * uppdateras, så även övriga alternativ får rätt
         * interactable-status.
         */
        RefreshChoiceVisual();
    }

    // =========================================================
    // VISUAL HELPERS
    // =========================================================

    private void SetIcon(
        Sprite sprite)
    {
        if (iconImage == null)
            return;

        iconImage.sprite =
            sprite;

        iconImage.enabled =
            sprite != null;
    }

    private void SetStackAmount(
        int amount)
    {
        if (stackText == null)
            return;

        bool showStack =
            amount > 1;

        stackText.gameObject.SetActive(
            showStack
        );

        stackText.text =
            showStack
                ? amount.ToString()
                : string.Empty;
    }

    private void SetSelectionFrame(
        bool selected)
    {
        if (selectionFrame == null)
            return;

        Color color =
            selectionFrame.color;

        color.a =
            selected
                ? selectedFrameAlpha
                : unselectedFrameAlpha;

        selectionFrame.color =
            color;
    }

    // =========================================================
    // INTERACTION HELPERS
    // =========================================================

    private void SetFixedRewardInteraction()
    {
        if (button == null)
            return;

        button.onClick.RemoveAllListeners();

        /*
         * Fasta rewards visas bara.
         * De ska inte kunna väljas eller avmarkeras.
         */
        button.interactable =
            false;
    }

    private void ClearChoiceBinding()
    {
        choiceOption = null;

        if (button != null)
        {
            button.onClick.RemoveAllListeners();
        }
    }

    private void OnDestroy()
    {
        if (button != null)
        {
            button.onClick.RemoveAllListeners();
        }
    }
}