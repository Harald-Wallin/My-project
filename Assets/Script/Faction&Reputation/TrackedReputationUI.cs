using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class TrackedReputationUI : MonoBehaviour
{
    public PlayerReputationManager reputationManager;

    public GameObject contentRoot;   // Dra in child-objektet här
    public TextMeshProUGUI factionNameText;
    public TextMeshProUGUI tierText;
    public Slider reputationSlider;
    public TMP_Text reputationText;

    void Start()
    {
        reputationManager = FindFirstObjectByType<PlayerReputationManager>();
        reputationManager.OnReputationChanged += Refresh;

        Refresh(reputationManager.GetTrackedFaction());
    }

    void OnDestroy()
    {
        if (reputationManager != null)
            reputationManager.OnReputationChanged -= Refresh;
    }

    public void Refresh(FactionReputationData data)
    {
        var tracked = reputationManager.GetTrackedFaction();

        if (tracked == null || tracked.faction == null)
        {
            contentRoot.SetActive(false);
            return;
        }

        contentRoot.SetActive(true);

        factionNameText.text = tracked.faction.factionName;

        tierText.text =
            reputationManager.levelDefinition.GetTierName(tracked.level);

        reputationSlider.maxValue =
            reputationManager.levelDefinition.GetXPRequired(tracked.level);

        reputationSlider.value = tracked.currentXP;
        Color color = ReputationColorUtility.GetColor(tracked.level);
        reputationSlider.fillRect.GetComponent<Image>().color = color;

        if (reputationText != null)
            reputationText.text = $"{tracked.currentXP}/{reputationSlider.maxValue}";
    }
}