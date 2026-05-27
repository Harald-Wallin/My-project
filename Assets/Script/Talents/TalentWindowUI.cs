using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class TalentWindowUI : MonoBehaviour
{
    [SerializeField] private Transform tierContainer;
    [SerializeField] private GameObject tierPrefab;
    [SerializeField] private GameObject talentSlotPrefab;
    [SerializeField] private TMP_Text availablePointsText;

    [SerializeField] private List<TalentTier> tiers;
    [Header("Visibility")]
    [SerializeField] private CanvasGroup canvasGroup;

    void Start()
    {
        BuildUI();
        // Keep component active so Update() receives input, but hide visually if a CanvasGroup is assigned.
        if (canvasGroup != null)
        {
            canvasGroup.alpha = 0f;
            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = false;
        }
        else
        {
            // Fallback to disabling the GameObject (Update won't run while disabled)
            gameObject.SetActive(false);
        }
    }

    void Update()
    {

        if (availablePointsText != null && TalentManager.Instance != null)
        {
            availablePointsText.text =
                $"Talent points: {TalentManager.Instance.availablePoints}";
        }
    }

    void BuildUI()
    {
        foreach (Transform child in tierContainer)
            Destroy(child.gameObject);

        foreach (var tier in tiers)
        {
            GameObject tierGO = Instantiate(tierPrefab, tierContainer);

            foreach (var talentData in tier.talents)
            {
                var runtime = TalentManager.Instance.talents
                    .Find(t => t.data == talentData);

                var slotGO = Instantiate(talentSlotPrefab, tierGO.transform);

                slotGO.GetComponent<TalentSlotUI>().Setup(runtime);
            }
        }
    }

    public void Toggle()
    {
        if (canvasGroup != null)
        {
            bool isOpen = canvasGroup.alpha > 0f;
            canvasGroup.alpha = isOpen ? 0f : 1f;
            canvasGroup.interactable = !isOpen;
            canvasGroup.blocksRaycasts = !isOpen;
        }
        else
        {
            gameObject.SetActive(!gameObject.activeSelf);
        }
    }

    public void Open()
    {
        if (canvasGroup != null)
        {
            canvasGroup.alpha = 1f;
            canvasGroup.interactable = true;
            canvasGroup.blocksRaycasts = true;
        }
        else
            gameObject.SetActive(true);
    }

    public void Close()
    {
        if (canvasGroup != null)
        {
            canvasGroup.alpha = 0f;
            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = false;
        }
        else
            gameObject.SetActive(false);
    }
}
