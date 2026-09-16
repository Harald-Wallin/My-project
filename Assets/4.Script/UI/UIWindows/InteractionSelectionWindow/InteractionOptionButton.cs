using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Visuell knapp för exakt ett IInteractionOption.
///
/// Komponenten känner inte till Vendor, Favour,
/// Tribute eller andra gameplay-system.
/// Den visar endast optionens presentation och
/// rapporterar klicket tillbaka till sitt fönster.
/// </summary>
[DisallowMultipleComponent]
public sealed class InteractionOptionButton :
    MonoBehaviour
{
    [Header("References")]

    [SerializeField]
    private Button button;

    [SerializeField]
    private TMP_Text categoryText;

    [SerializeField]
    private TMP_Text mainText;

    [SerializeField]
    private TMP_Text statusText;

    private IInteractionOption option;

    private Action<IInteractionOption>
        clickedCallback;

    public IInteractionOption Option =>
        option;

    private void Awake()
    {
        if (button == null)
        {
            button =
                GetComponent<Button>();
        }

        if (button != null)
        {
            button.onClick.AddListener(
                HandleClicked
            );
        }
    }

    private void OnDestroy()
    {
        if (button != null)
        {
            button.onClick.RemoveListener(
                HandleClicked
            );
        }
    }

    /// <summary>
    /// Binder knappen till ett konkret interaction-alternativ.
    /// </summary>
    public void Bind(
        IInteractionOption interactionOption,
        Action<IInteractionOption> onClicked)
    {
        option =
            interactionOption;

        clickedCallback =
            onClicked;

        RefreshPresentation();

        if (button != null)
        {
            button.interactable =
                option != null;
        }
    }

    /// <summary>
    /// Hämtar presentationen på nytt.
    ///
    /// Detta låter dynamisk status, exempelvis en
    /// Favour-cooldown, uppdateras utan att knappen
    /// behöver skapas om.
    /// </summary>
    public void RefreshPresentation()
    {
        if (option == null)
        {
            ClearPresentation();
            return;
        }

        InteractionPresentation presentation =
            option.GetPresentation();

        if (categoryText != null)
        {
            string category =
                GetCategoryLabel(
                    presentation.Category
                );

            bool hasCategory =
                !string.IsNullOrWhiteSpace(
                    category
                );

            categoryText.text =
                hasCategory
                    ? $"[{category}]:"
                    : string.Empty;

            categoryText.gameObject.SetActive(
                hasCategory
            );
        }

        if (mainText != null)
        {
            mainText.text =
                presentation.Text;
        }

        if (statusText != null)
        {
            bool hasStatus =
                !string.IsNullOrWhiteSpace(
                    presentation.Status
                );

            statusText.text =
                hasStatus
                    ? presentation.Status
                    : string.Empty;

            statusText.gameObject.SetActive(
                hasStatus
            );
        }
    }

    private static string GetCategoryLabel(
        InteractionCategory category)
    {
        switch (category)
        {
            case InteractionCategory.Favour:
                return "Favour";

            case InteractionCategory.Vendor:
                return "Vendor";

            case InteractionCategory.Tribute:
                return "Tribute";

            case InteractionCategory.Dialogue:
                return "Dialogue";

            case InteractionCategory.Trainer:
                return "Trainer";

            case InteractionCategory.Bank:
                return "Bank";

            case InteractionCategory.Crafting:
                return "Crafting";

            /*
             * Generiska world interactions behöver inte
             * visa en mekanisk "[Other]"-prefix.
             */
            case InteractionCategory.Other:
            case InteractionCategory.None:
            default:
                return string.Empty;
        }
    }

    private void HandleClicked()
    {
        if (option == null)
            return;

        clickedCallback?.Invoke(
            option
        );
    }

    private void ClearPresentation()
    {
        if (categoryText != null)
        {
            categoryText.text =
                string.Empty;

            categoryText.gameObject.SetActive(
                false
            );
        }

        if (mainText != null)
        {
            mainText.text =
                string.Empty;
        }

        if (statusText != null)
        {
            statusText.text =
                string.Empty;

            statusText.gameObject.SetActive(
                false
            );
        }
    }
}
