using TMPro;
using UnityEngine;

public class TalentTierHeader : MonoBehaviour
{
    [SerializeField] private TMP_Text headerText;

    public void Setup(int tier)
    {
        int requiredPoints =
            Mathf.Max(0, (tier - 1) * 3);

        headerText.text = $"Tier {tier} - Unlock: {requiredPoints} points";
    }
}
