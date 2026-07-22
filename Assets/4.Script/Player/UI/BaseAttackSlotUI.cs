using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class BaseAttackSlotUI :
    MonoBehaviour,
    IDropHandler,
    IPointerEnterHandler,
    IPointerExitHandler,
    ICombatSlot
{
    [SerializeField] private Image icon;
    [SerializeField] private Image cooldownOverlay;
    [SerializeField] private TMP_Text cooldownText;

    private BaseAttackController controller;
    private PlayerBaseAttackCollection collection;

    private AbilityData attack;

    public void Initialize(
        BaseAttackController controller,
        PlayerBaseAttackCollection collection
    )
    {
        this.controller = controller;
        this.collection = collection;

        Refresh();
    }

    void Update()
    {
        UpdateCooldown();
    }

    void UpdateCooldown()
    {
        if (attack == null)
        {
            cooldownOverlay.fillAmount = 0f;
            cooldownText.text = "";
            return;
        }

        float normalized =
            controller.GetCooldownNormalized();

        cooldownOverlay.fillAmount = normalized;

        if (normalized > 0f)
        {
            float speed =
                controller.GetComponent<CharacterStats>()
                    .GetStat(StatType.AttackSpeed);

            float max =
                1f / Mathf.Max(speed, 0.01f);

            cooldownText.text =
                Mathf.CeilToInt(
                    normalized * max
                ).ToString();
        }
        else
        {
            cooldownText.text = "";
        }
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

        if (!dragged.ability.IsBaseAttack)
            return;

        if (!collection.EquipAttack(
                dragged.ability))
        {
            return;
        }

        dragged.wasDroppedOnSlot = true;

        Refresh();
    }

    void Refresh()
    {
        attack =
            collection.GetEquippedAttack();

        if (attack == null)
        {
            icon.sprite = null;
            icon.color =
                new Color(1f, 1f, 1f, 0.2f);

            return;
        }

        icon.sprite = attack.icon;
        icon.color = Color.white;
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (attack == null)
            return;

        ItemTooltip.Instance.Show(
            attack,
            icon.rectTransform,
            PlayerReference.Player,
            ItemTooltip.TooltipAnchorMode.TopRight
        );
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        ItemTooltip.Instance.Hide();
    }

    public void ClearSlot()
    {
        collection.EquipAttack(null);
        Refresh();
    }
}
