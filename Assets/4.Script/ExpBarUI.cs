using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ExpBarUI : MonoBehaviour
{
    public PlayerStats player;
    public Slider expSlider;
    public TMP_Text levelText;
    public TMP_Text expText;

    void Start()
    {
        if (player != null)
        {
            player.OnExpChanged += UpdateUI;
            player.OnLevelChanged += UpdateUI;
        }

        UpdateUI();
    }

    void UpdateUI()
    {
        if (player == null || expSlider == null)
            return;

        expSlider.maxValue = player.expToNextLevel;
        expSlider.value = player.currentExp;
        levelText.text = $"Lv {player.level}";

        if (expText != null)
            expText.text = $"{player.currentExp}/{player.expToNextLevel}";
    }
}

