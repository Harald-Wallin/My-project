using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Generellt valfönster för ett InteractionTarget som
/// erbjuder fler än ett interaction-alternativ.
///
/// Fönstret är presentation-only:
/// - visar tillgängliga options,
/// - rapporterar spelarens val,
/// - känner inte till Vendor/Favour/Tribute/etc.
/// </summary>
[DisallowMultipleComponent]
public sealed class InteractionSelectionWindow :
    MonoBehaviour,
    IUIWindow
{
    [Header("Root")]

    [SerializeField]
    private GameObject windowRoot;

    [SerializeField]
    private RectTransform windowPanel;

    [Header("Header")]

    [SerializeField]
    private TMP_Text titleText;

    [SerializeField]
    private Button closeButton;

    [Header("Options")]

    [SerializeField]
    private Transform optionContainer;

    [SerializeField]
    private InteractionOptionButton
        optionButtonPrefab;

    private readonly List<
        InteractionOptionButton>
        spawnedButtons =
            new();

    private InteractionTarget currentTarget;

    public static InteractionSelectionWindow
        Instance
    {
        get;
        private set;
    }

    public bool IsOpen =>
        windowRoot != null &&
        windowRoot.activeSelf;

    private void Awake()
    {
        if (Instance != null &&
            Instance != this)
        {
            Debug.LogWarning(
                "Flera InteractionSelectionWindow hittades. " +
                "Den nya komponenten stängs av.",
                this
            );

            enabled = false;
            return;
        }

        Instance =
            this;

        if (closeButton != null)
        {
            closeButton.onClick.AddListener(
                Close
            );
        }

        SetOpen(
            false
        );
    }

    private void OnDestroy()
    {
        if (closeButton != null)
        {
            closeButton.onClick.RemoveListener(
                Close
            );
        }

        if (Instance == this)
        {
            Instance =
                null;
        }
    }

    /// <summary>
    /// Öppnar fönstret för ett target och en färdig lista
    /// av konkreta interaction-options.
    /// </summary>
    public void Open(
        InteractionTarget target,
        IReadOnlyList<IInteractionOption> options)
    {
        if (target == null ||
            options == null ||
            options.Count == 0)
        {
            return;
        }

        currentTarget =
            target;

        ClearButtons();

        RefreshTitle();

        for (int i = 0;
             i < options.Count;
             i++)
        {
            IInteractionOption option =
                options[i];

            if (option == null)
                continue;

            CreateButton(
                option
            );
        }

        if (spawnedButtons.Count == 0)
        {
            currentTarget =
                null;

            return;
        }

        SetOpen(
            true
        );

        RegisterAsInteractionWindow();

        RefreshLayout();
    }

    public void Close()
    {
        ClearButtons();

        currentTarget =
            null;

        SetOpen(
            false
        );

        GlobalUIManager.Instance?
            .ClearInteractionWindow(
                this
            );
    }

    private void CreateButton(
        IInteractionOption option)
    {
        if (optionButtonPrefab == null ||
            optionContainer == null)
        {
            return;
        }

        InteractionOptionButton button =
            Instantiate(
                optionButtonPrefab,
                optionContainer
            );

        button.Bind(
            option,
            HandleOptionClicked
        );

        spawnedButtons.Add(
            button
        );
    }

    private void HandleOptionClicked(
        IInteractionOption option)
    {
        if (option == null)
            return;

        InteractionManager manager =
            InteractionManager.Instance;

        if (manager == null)
            return;

        /*
         * Selection-fönstret ska försvinna innan nästa
         * gameplay-fönster öppnas.
         *
         * Vi använder dock inte Close() här eftersom den
         * skulle kunna påverka interaction-window-
         * registreringen precis innan nästa window tar över.
         */
        ClearButtons();

        currentTarget =
            null;

        SetOpen(
            false
        );

        GlobalUIManager.Instance?
            .ClearInteractionWindow(
                this
            );

        bool executed =
            manager.ExecuteOption(
                option
            );

        if (!executed)
        {
            manager.ClearCurrentInteraction();
        }
    }

    private void RefreshTitle()
    {
        if (titleText == null)
            return;

        if (currentTarget == null ||
            currentTarget.InteractionOwner == null)
        {
            titleText.text =
                "Interaction";

            return;
        }

        EntityIdentity identity =
            EntityTargetUtility.GetIdentity(
                currentTarget.InteractionOwner
            );

        if (identity != null &&
            !string.IsNullOrWhiteSpace(
                identity.DisplayName))
        {
            titleText.text =
                identity.DisplayName;

            return;
        }

        titleText.text =
            currentTarget.InteractionOwner.name;
    }

    private void RegisterAsInteractionWindow()
    {
        if (currentTarget == null)
            return;

        GlobalUIManager.Instance?
            .RegisterInteractionWindow(
                this,
                currentTarget.InteractionTransform,
                currentTarget.WindowCloseDistance
            );
    }

    private void ClearButtons()
    {
        for (int i =
                 spawnedButtons.Count - 1;
             i >= 0;
             i--)
        {
            InteractionOptionButton button =
                spawnedButtons[i];

            if (button == null)
                continue;

            button.gameObject.SetActive(
                false
            );

            Destroy(
                button.gameObject
            );
        }

        spawnedButtons.Clear();
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
