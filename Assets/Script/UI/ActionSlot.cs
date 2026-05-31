using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.EventSystems;
using System.Collections;

public class ActionSlot : MonoBehaviour, IDropHandler, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler, ICombatSlot
{
    [SerializeField] private Image icon;
    [SerializeField] private Image cooldownOverlay;
    [SerializeField] private TMP_Text cooldownText;
    [SerializeField] private TMP_Text hotkeyText;
    [SerializeField] private RectTransform slotTransform;
    [SerializeField] private Image flashImage;

    private AbilityController abilityController;
    private PlayerAbilityCollection collection;
    public AbilityData ability;
    private int slotIndex;
    private bool isDragging = false;

    public void Initialize(AbilityController controller, AbilityData ability, int index)
    {

        //Debug.Log("Slot init: " + ability?.abilityName);
        collection = controller.GetComponent<PlayerAbilityCollection>();

        this.abilityController = controller;
        this.ability = ability;
        this.slotIndex = index;

        hotkeyText.text = (index + 1).ToString();

        if (ability != null)
        {
            icon.sprite = ability.icon;
            icon.enabled = true;

            var drag = icon.GetComponent<DraggableAbility>();
            if (drag != null)
                drag.ability = ability;
        }
        else
        {
            icon.color = new Color(1, 1, 1, 0.2f);

            var drag = icon.GetComponent<DraggableAbility>();
            if (drag != null)
                drag.ability = null;
        }
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (eventData.button != PointerEventData.InputButton.Left)
            return;

        PlayClickFeedback(); // 🔥 ALLTID

        if (abilityController != null && ability != null)
        {
            abilityController.TryUseAbility(ability);
        }
    }

    void Update()
    {
        if (abilityController == null || ability == null)
            return;

        if (Input.GetKeyDown(KeyCode.Alpha1 + slotIndex))
        {
            PlayClickFeedback();

            if (ability != null)
                abilityController.TryUseAbility(ability);
        }

        UpdateCooldownUI();
    }

    void UpdateCooldownUI()
    {
        float cd = abilityController.GetCooldownRemaining(ability);

        if (cd > 0f)
        {
            float max = abilityController.GetMaxCooldown(ability);

            cooldownOverlay.fillAmount = cd / max;
            cooldownText.text = Mathf.CeilToInt(cd).ToString();
        }
        else
        {
            cooldownOverlay.fillAmount = 0f;
            cooldownText.text = "";
        }
    }

    void PlayClickFeedback()
    {
        StopAllCoroutines();
        StartCoroutine(ClickFeedbackRoutine());
    }

    IEnumerator ClickFeedbackRoutine()
    {
        float t = 0f;
        float duration = 0.15f;

        Vector3 originalScale = slotTransform.localScale;
        Vector3 targetScale = originalScale * 1.05f;

        // Flash start
        flashImage.color = new Color(1, 1, 1, 0.6f);

        while (t < duration)
        {
            t += Time.deltaTime;
            float lerp = t / duration;

            slotTransform.localScale = Vector3.Lerp(targetScale, originalScale, lerp);
            flashImage.color = new Color(1, 1, 1, Mathf.Lerp(0.1f, 0f, lerp));

            yield return null;
        }

        slotTransform.localScale = originalScale;
        flashImage.color = new Color(1, 1, 1, 0);
    }

    public void OnDrop(PointerEventData eventData)
    {
        DraggableAbility dragged =
            eventData.pointerDrag?.GetComponent<DraggableAbility>();

        if (dragged == null)
            return;

        if (dragged.ability == null)
            return;

        // BLOCK BASE ATTACKS
        if (dragged.ability is BaseAttackData)
            return;

        dragged.wasDroppedOnSlot = true;

        AbilityData previous = ability;

        SetAbility(dragged.ability);

        // swap
        if (dragged.sourceSlot != null && dragged.sourceSlot != this)
        {
            if (dragged.sourceSlot is ActionSlot oldSlot)
            {
                oldSlot.SetAbility(previous);
            }
        }
    }

    public void SetAbility(AbilityData newAbility)
    {
        ability = newAbility;

        var drag = icon.GetComponent<DraggableAbility>();

        if (ability != null)
        {
            icon.sprite = ability.icon;
            icon.color = Color.white;

            if (drag != null)
                drag.ability = ability;
        }
        else
        {
            icon.sprite = null;
            icon.color = new Color(1, 1, 1, 0.2f);

            if (drag != null)
                drag.ability = null;
        }

        // 🔥 synka med AbilityController
        if (abilityController != null)
        {
            if (collection != null)
            {
                collection.SetEquippedAbility(
                    slotIndex,
                    ability
                );
            }
        }
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (ability == null) return;

        if (DraggableAbility.dragged) return;

        var player = PlayerReference.Player;

        ItemTooltip.Instance.Show(
        ability,
        slotTransform,
        player,
        ItemTooltip.TooltipAnchorMode.TopRight
        );
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        ItemTooltip.Instance.Hide();
    }

    public void ClearSlot()
    {
        SetAbility(null);
    }
}