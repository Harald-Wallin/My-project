using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public sealed class BaseAttackSlotUI :
    MonoBehaviour,
    IDropHandler,
    IPointerEnterHandler,
    IPointerExitHandler,
    IPointerClickHandler,
    ICombatSlot
{
    [Header("Slot")]

    [SerializeField]
    [Range(
        0,
        PlayerBaseAttackCollection
            .SlotCount - 1)]
    private int slotIndex;

    [Header("Visuals")]

    [SerializeField]
    private Image icon;

    [SerializeField]
    private Image cooldownOverlay;

    [SerializeField]
    private TMP_Text cooldownText;

    [SerializeField]
    [Tooltip(
        "Overlay som markerar primary-slotten. " +
        "Primary-slotten är alltid den aktiva attacken."
    )]
    private Image activeIndicator;

    private BaseAttackController
        baseAttackController;

    private CharacterActionController
        actionController;

    private PlayerBaseAttackCollection
        collection;

    private AbilityData attack;

    public int SlotIndex =>
        slotIndex;

    public AbilityData Attack =>
        attack;

    public void Initialize(
        BaseAttackController controller,
        PlayerBaseAttackCollection
            attackCollection,
        int index)
    {
        Unsubscribe();

        baseAttackController =
            controller;

        collection =
            attackCollection;

        slotIndex =
            Mathf.Clamp(
                index,
                0,
                PlayerBaseAttackCollection
                    .SlotCount - 1
            );

        if (baseAttackController != null)
        {
            actionController =
                baseAttackController
                    .GetComponent<
                        CharacterActionController>();
        }

        Subscribe();
        Refresh();
    }

    private void OnEnable()
    {
        Subscribe();
        Refresh();
    }

    private void OnDisable()
    {
        Unsubscribe();

        ItemTooltip.Instance?.Hide();
    }

    private void OnDestroy()
    {
        Unsubscribe();
    }

    private void Update()
    {
        UpdateCooldown();
    }

    private void Subscribe()
    {
        if (collection == null)
            return;

        collection.OnAttackSlotChanged -=
            HandleAttackSlotChanged;

        collection.OnActiveAttackChanged -=
            HandleActiveAttackChanged;

        collection.OnAttackSlotChanged +=
            HandleAttackSlotChanged;

        collection.OnActiveAttackChanged +=
            HandleActiveAttackChanged;
    }

    private void Unsubscribe()
    {
        if (collection == null)
            return;

        collection.OnAttackSlotChanged -=
            HandleAttackSlotChanged;

        collection.OnActiveAttackChanged -=
            HandleActiveAttackChanged;
    }

    private void HandleAttackSlotChanged(
        int changedSlotIndex,
        AbilityData changedAttack)
    {
        if (changedSlotIndex !=
            slotIndex)
        {
            return;
        }

        Refresh();
    }

    private void HandleActiveAttackChanged(
        int activeSlotIndex,
        AbilityData activeAttack)
    {
        /*
         * Primary är alltid aktiv, men eventet kan användas
         * för att garantera att markeringen är korrekt.
         */
        RefreshActiveIndicator();
    }

    private void UpdateCooldown()
    {
        if (cooldownOverlay == null ||
            cooldownText == null)
        {
            return;
        }

        if (attack == null ||
            actionController == null)
        {
            ClearCooldown();
            return;
        }

        float remaining =
            actionController
                .GetCooldownRemaining(
                    attack
                );

        float maximum =
            actionController
                .GetMaxCooldown(
                    attack
                );

        if (remaining <= 0f ||
            maximum <= 0f)
        {
            ClearCooldown();
            return;
        }

        cooldownOverlay.fillAmount =
            Mathf.Clamp01(
                remaining /
                maximum
            );

        cooldownText.text =
            Mathf.CeilToInt(
                remaining
            ).ToString();
    }

    private void ClearCooldown()
    {
        if (cooldownOverlay != null)
        {
            cooldownOverlay.fillAmount =
                0f;
        }

        if (cooldownText != null)
        {
            cooldownText.text =
                string.Empty;
        }
    }

    public void OnDrop(
        PointerEventData eventData)
    {
        if (collection == null)
            return;

        DraggableAbility dragged =
            eventData.pointerDrag
                ?.GetComponent<
                    DraggableAbility>();

        if (dragged == null ||
            dragged.ability == null)
        {
            return;
        }

        if (!dragged.ability.IsBaseAttack)
            return;

        bool equipped =
            collection.EquipAttack(
                slotIndex,
                dragged.ability
            );

        if (!equipped)
            return;

        /*
         * Förhindrar att DraggableAbility tömmer sin
         * ursprungsslot efter en lyckad drop.
         */
        dragged.wasDroppedOnSlot =
            true;
    }

    public void OnPointerClick(
        PointerEventData eventData)
    {
        if (eventData.button !=
            PointerEventData
                .InputButton.Left)
        {
            return;
        }

        if (collection == null)
            return;

        /*
         * Primary är redan aktiv.
         */
        if (slotIndex ==
            PlayerBaseAttackCollection
                .PrimarySlotIndex)
        {
            return;
        }

        /*
         * Klick på secondary gör att dess attack kommer
         * fram till primary-positionen.
         */
        collection.SwapAttacks();
    }

    private void Refresh()
    {
        attack =
            collection != null
                ? collection.GetAttack(
                    slotIndex
                )
                : null;

        RefreshIcon();
        RefreshActiveIndicator();
        ClearCooldown();
    }

    private void RefreshIcon()
    {
        if (icon == null)
            return;

        DraggableAbility draggable =
            icon.GetComponent<
                DraggableAbility>();

        if (attack == null)
        {
            icon.sprite =
                null;

            icon.enabled =
                true;

            icon.color =
                new Color(
                    1f,
                    1f,
                    1f,
                    0.2f
                );

            if (draggable != null)
            {
                draggable.ability =
                    null;
            }

            return;
        }

        icon.sprite =
            attack.icon;

        icon.enabled =
            true;

        icon.color =
            Color.white;

        if (draggable != null)
        {
            draggable.ability =
                attack;
        }
    }

    private void RefreshActiveIndicator()
    {
        if (activeIndicator == null)
            return;

        /*
         * Den visuellt främre primary-slotten är alltid aktiv.
         */
        activeIndicator.enabled =
            slotIndex ==
            PlayerBaseAttackCollection
                .PrimarySlotIndex;
    }

    public void OnPointerEnter(
        PointerEventData eventData)
    {
        if (attack == null ||
            DraggableAbility.dragged ||
            ItemTooltip.Instance == null ||
            icon == null)
        {
            return;
        }

        ItemTooltip.Instance.Show(
            attack,
            icon.rectTransform,
            PlayerReference.Player,
            ItemTooltip
                .TooltipAnchorMode
                .TopRight
        );
    }

    public void OnPointerExit(
        PointerEventData eventData)
    {
        ItemTooltip.Instance?.Hide();
    }

    public void ClearSlot()
    {
        collection?.ClearAttack(
            slotIndex
        );
    }
}