using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class BaseAttackEntryUI :
    MonoBehaviour,
    IPointerEnterHandler,
    IPointerExitHandler
{
    [SerializeField] private Image icon;
    [SerializeField] private TMP_Text nameText;

    private BaseAttackData attack;

    public void Setup(BaseAttackData attack)
    {
        this.attack = attack;

        icon.sprite = attack.icon;
        nameText.text = attack.abilityName;

        DraggableAbility drag = icon.GetComponent<DraggableAbility>();

        if (drag != null)
        {
            drag.ability = attack;
        }
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
}
