using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[DisallowMultipleComponent]
public sealed class BaseAttackBarUI :
    MonoBehaviour
{
    [Header("Slots")]

    [SerializeField]
    private BaseAttackSlotUI
        primarySlot;

    [SerializeField]
    private BaseAttackSlotUI
        secondarySlot;

    [Header("Controls")]

    [SerializeField]
    [Tooltip(
        "Knappen mellan slotarna som byter plats på " +
        "primary och secondary attack."
    )]
    private Button swapButton;

    [SerializeField]
    private KeyCode swapKey =
        KeyCode.Tab;

    [SerializeField]
    [Tooltip(
        "Förhindrar byte när spelaren skriver i ett TMP-inputfält."
    )]
    private bool blockSwapWhileTyping =
        true;

    private PlayerBaseAttackCollection
        collection;

    private BaseAttackController
        controller;

    private IEnumerator Start()
    {
        /*
         * PlayerReference kan initialiseras något senare
         * än UI-objekten.
         */
        yield return null;

        ResolveReferences();

        if (controller == null ||
            collection == null)
        {
            Debug.LogWarning(
                $"{nameof(BaseAttackBarUI)} kunde inte hitta " +
                $"{nameof(BaseAttackController)} eller " +
                $"{nameof(PlayerBaseAttackCollection)}.",
                this
            );

            yield break;
        }

        primarySlot?.Initialize(
            controller,
            collection,
            PlayerBaseAttackCollection
                .PrimarySlotIndex
        );

        secondarySlot?.Initialize(
            controller,
            collection,
            PlayerBaseAttackCollection
                .SecondarySlotIndex
        );

        ConfigureSwapButton();
    }

    private void Update()
    {
        if (collection == null)
            return;

        if (!Input.GetKeyDown(
                swapKey))
        {
            return;
        }

        if (blockSwapWhileTyping &&
            IsTypingInInputField())
        {
            return;
        }

        /*
         * TAB ska inte göra någonting medan användaren
         * modifierar textinmatning.
         *
         * Det blockeras däremot inte av att musen befinner
         * sig över vanligt UI.
         */
        SwapAttacks();
    }

    private void OnDestroy()
    {
        if (swapButton != null)
        {
            swapButton.onClick
                .RemoveListener(
                    HandleSwapClicked
                );
        }
    }

    private void ResolveReferences()
    {
        PlayerStats player =
            PlayerReference.Player;

        if (player == null)
            return;

        controller =
            player.GetComponent<
                BaseAttackController>();

        collection =
            player.GetComponent<
                PlayerBaseAttackCollection>();
    }

    private void ConfigureSwapButton()
    {
        if (swapButton == null)
            return;

        swapButton.onClick
            .RemoveListener(
                HandleSwapClicked
            );

        swapButton.onClick
            .AddListener(
                HandleSwapClicked
            );
    }

    private void HandleSwapClicked()
    {
        SwapAttacks();
    }

    private void SwapAttacks()
    {
        collection?.SwapAttacks();
    }

    private static bool IsTypingInInputField()
    {
        if (EventSystem.current == null)
            return false;

        GameObject selected =
            EventSystem.current
                .currentSelectedGameObject;

        if (selected == null)
            return false;

        return selected.GetComponent<
                   TMP_InputField>() != null;
    }
}