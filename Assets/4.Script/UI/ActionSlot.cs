using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class ActionSlot :
    MonoBehaviour,
    IDropHandler,
    IPointerEnterHandler,
    IPointerExitHandler,
    IPointerClickHandler,
    ICombatSlot
{
    [SerializeField]
    private Image icon;

    [SerializeField]
    private Image cooldownOverlay;

    [SerializeField]
    private TMP_Text cooldownText;

    [SerializeField]
    private TMP_Text hotkeyText;

    [SerializeField]
    private RectTransform slotTransform;

    [SerializeField]
    private Image flashImage;

    private AbilityController abilityController;
    private CharacterActionController actionController;
    private PlayerActionInput playerActionInput;
    private PlayerAbilityCollection collection;

    public AbilityData ability;

    private int slotIndex;

    public void Initialize(
        AbilityController controller,
        AbilityData ability,
        int index)
    {
        abilityController =
            controller;

        collection =
            controller.GetComponent<
                PlayerAbilityCollection
            >();

        playerActionInput =
            controller.GetComponent<
                PlayerActionInput
            >();

        actionController =
            controller.GetComponent<
                CharacterActionController
            >();

        this.ability = ability;
        slotIndex = index;

        hotkeyText.text =
            (index + 1).ToString();

        RefreshVisual();
    }

    private void Update()
    {
        if (ability == null)
        {
            ClearCooldownUI();
            return;
        }

        if (Input.GetKeyDown(
                KeyCode.Alpha1 +
                slotIndex))
        {
            PlayClickFeedback();
            TryActivateAbility();
        }

        UpdateCooldownUI();
    }

    private bool TryActivateAbility()
    {
        if (ability == null)
            return false;

        if (ability.IsBaseAttack)
            return false;

        if (ability.UsesActionSettings)
        {
            if (playerActionInput == null)
                return false;

            return playerActionInput
                .TryStartAbility(
                    ability
                );
        }

        if (abilityController == null)
            return false;

        return abilityController
            .TryUseAbility(
                ability
            );
    }

    public void OnPointerClick(
        PointerEventData eventData)
    {
        if (eventData.button !=
            PointerEventData.InputButton.Left)
        {
            return;
        }

        PlayClickFeedback();
        TryActivateAbility();
    }

    private void UpdateCooldownUI()
    {
        float remaining;
        float maximum;

        if (ability.UsesActionSettings)
        {
            if (actionController == null)
            {
                ClearCooldownUI();
                return;
            }

            remaining =
                actionController
                    .GetCooldownRemaining(
                        ability
                    );

            maximum =
                actionController
                    .GetMaxCooldown(
                        ability
                    );
        }
        else
        {
            if (abilityController == null)
            {
                ClearCooldownUI();
                return;
            }

            remaining =
                abilityController
                    .GetCooldownRemaining(
                        ability
                    );

            maximum =
                abilityController
                    .GetMaxCooldown(
                        ability
                    );
        }

        if (remaining <= 0f ||
            maximum <= 0f)
        {
            ClearCooldownUI();
            return;
        }

        cooldownOverlay.fillAmount =
            Mathf.Clamp01(
                remaining / maximum
            );

        cooldownText.text =
            Mathf.CeilToInt(
                remaining
            ).ToString();
    }

    private void ClearCooldownUI()
    {
        cooldownOverlay.fillAmount = 0f;
        cooldownText.text = "";
    }

    private void PlayClickFeedback()
    {
        StopAllCoroutines();

        StartCoroutine(
            ClickFeedbackRoutine()
        );
    }

    private IEnumerator ClickFeedbackRoutine()
    {
        float elapsed = 0f;
        const float duration = 0.15f;

        Vector3 originalScale =
            slotTransform.localScale;

        Vector3 targetScale =
            originalScale * 1.05f;

        flashImage.color =
            new Color(
                1f,
                1f,
                1f,
                0.6f
            );

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;

            float progress =
                Mathf.Clamp01(
                    elapsed / duration
                );

            slotTransform.localScale =
                Vector3.Lerp(
                    targetScale,
                    originalScale,
                    progress
                );

            flashImage.color =
                new Color(
                    1f,
                    1f,
                    1f,
                    Mathf.Lerp(
                        0.1f,
                        0f,
                        progress
                    )
                );

            yield return null;
        }

        slotTransform.localScale =
            originalScale;

        flashImage.color =
            new Color(
                1f,
                1f,
                1f,
                0f
            );
    }

    public void OnDrop(
        PointerEventData eventData)
    {
        DraggableAbility dragged =
            eventData.pointerDrag
                ?.GetComponent<
                    DraggableAbility
                >();

        if (dragged == null ||
            dragged.ability == null)
        {
            return;
        }

        if (dragged.ability.IsBaseAttack)
            return;

        dragged.wasDroppedOnSlot = true;

        AbilityData previous =
            ability;

        SetAbility(
            dragged.ability
        );

        if (dragged.sourceSlot != null &&
            dragged.sourceSlot != this &&
            dragged.sourceSlot is ActionSlot oldSlot)
        {
            oldSlot.SetAbility(
                previous
            );
        }
    }

    public void SetAbility(
        AbilityData newAbility)
    {
        if (newAbility != null &&
            newAbility.IsBaseAttack)
        {
            return;
        }

        ability =
            newAbility;

        RefreshVisual();

        if (collection != null)
        {
            collection.SetEquippedAbility(
                slotIndex,
                ability
            );
        }
    }

    private void RefreshVisual()
    {
        DraggableAbility drag =
            icon.GetComponent<
                DraggableAbility
            >();

        if (ability != null)
        {
            icon.sprite =
                ability.icon;

            icon.enabled =
                true;

            icon.color =
                Color.white;

            if (drag != null)
            {
                drag.ability =
                    ability;
            }

            return;
        }

        icon.sprite = null;
        icon.enabled = true;

        icon.color =
            new Color(
                1f,
                1f,
                1f,
                0.2f
            );

        if (drag != null)
        {
            drag.ability = null;
        }
    }

    public void OnPointerEnter(
        PointerEventData eventData)
    {
        if (ability == null ||
            DraggableAbility.dragged)
        {
            return;
        }

        ItemTooltip.Instance.Show(
            ability,
            slotTransform,
            PlayerReference.Player,
            ItemTooltip
                .TooltipAnchorMode
                .TopRight
        );
    }

    public void OnPointerExit(
        PointerEventData eventData)
    {
        ItemTooltip.Instance.Hide();
    }

    public void ClearSlot()
    {
        SetAbility(
            null
        );
    }
}