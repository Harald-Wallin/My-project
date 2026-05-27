using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class TalentSlotUI : MonoBehaviour,
    IPointerEnterHandler,
    IPointerExitHandler,
    IPointerClickHandler
{
    [SerializeField] Image icon;
    [SerializeField] TMP_Text pointsText;

    private TalentRuntime talent;

    public void Setup(TalentRuntime talent)
    {
        this.talent = talent;

        if (talent == null || talent.data == null)
        {
            Debug.LogError("Talent runtime is NULL");
            return;
        }

        icon.sprite = talent.data.icon;

        Refresh();
    }

    void Refresh()
    {
        pointsText.text = $"{talent.currentPoints}/{talent.data.maxPoints}";
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (TalentManager.Instance.TrySpendPoint(talent))
        {
            Refresh();
            ItemTooltip.Instance.ShowTalent(
                talent.data,
                talent.currentPoints,
                icon.rectTransform
            );
        }

    }

    public void OnPointerEnter(PointerEventData eventData)
    {

        ItemTooltip.Instance.ShowTalent(
            talent.data,
            talent.currentPoints,
            icon.rectTransform
        );
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        ItemTooltip.Instance.Hide();
    }
}
