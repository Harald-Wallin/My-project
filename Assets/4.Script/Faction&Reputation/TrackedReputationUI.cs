using TMPro;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public sealed class TrackedReputationUI :
    MonoBehaviour
{
    [Header("References")]

    [SerializeField]
    private PlayerReputationManager
        reputationManager;

    [SerializeField]
    private Image reputationFill;

    [SerializeField]
    private TextMeshProUGUI factionNameText;

    [SerializeField]
    private TextMeshProUGUI tierText;

    [SerializeField]
    private TMP_Text reputationText;


    [Header("Visibility")]

    [Tooltip(
        "Dynamiska reputation-objekt som ska döljas när ingen faction trackas. " +
        "Lägg exempelvis ReputationFill, FactionNameText, TierText och ReputationText här.")]
    [SerializeField]
    private GameObject[] trackedContentObjects;


    private void Awake()
    {
        ConfigureFillImage(
            reputationFill
        );
    }


    private void Start()
    {
        if (reputationManager == null)
        {
            reputationManager =
                FindFirstObjectByType<
                    PlayerReputationManager>();
        }

        if (reputationManager == null)
        {
            Debug.LogWarning(
                $"{nameof(TrackedReputationUI)} kunde inte hitta " +
                $"{nameof(PlayerReputationManager)}.",
                this
            );

            SetContentVisible(
                false
            );

            return;
        }

        reputationManager
            .OnReputationChanged -=
            Refresh;

        reputationManager
            .OnReputationChanged +=
            Refresh;

        Refresh(
            reputationManager
                .GetTrackedFaction()
        );
    }


    private void OnDestroy()
    {
        if (reputationManager == null)
            return;

        reputationManager
            .OnReputationChanged -=
            Refresh;
    }


    public void Refresh(
        FactionReputationData data)
    {
        if (reputationManager == null)
            return;

        var tracked =
            reputationManager
                .GetTrackedFaction();

        if (tracked == null ||
            tracked.faction == null)
        {
            SetContentVisible(
                false
            );

            return;
        }

        SetContentVisible(
            true
        );


        if (factionNameText != null)
        {
            factionNameText.text =
                tracked.faction.factionName;
        }


        if (tierText != null)
        {
            tierText.text =
                reputationManager
                    .levelDefinition
                    .GetTierName(
                        tracked.level
                    );
        }


        float maximum =
            reputationManager
                .levelDefinition
                .GetXPRequired(
                    tracked.level
                );


        if (reputationFill != null)
        {
            reputationFill.fillAmount =
                maximum > 0f
                    ? Mathf.Clamp01(
                        tracked.currentXP /
                        maximum
                    )
                    : 0f;

            reputationFill.color =
                ReputationColorUtility
                    .GetColor(
                        tracked.level
                    );
        }


        if (reputationText != null)
        {
            reputationText.text =
                $"{tracked.currentXP}/{maximum}";
        }
    }


    private void SetContentVisible(
        bool visible)
    {
        if (trackedContentObjects == null)
            return;

        for (int i = 0;
             i < trackedContentObjects.Length;
             i++)
        {
            GameObject content =
                trackedContentObjects[i];

            if (content != null)
            {
                content.SetActive(
                    visible
                );
            }
        }
    }


    private static void ConfigureFillImage(
        Image image)
    {
        if (image == null)
            return;

        image.type =
            Image.Type.Filled;

        image.fillMethod =
            Image.FillMethod.Horizontal;

        image.fillOrigin =
            (int)Image.OriginHorizontal.Left;
    }
}