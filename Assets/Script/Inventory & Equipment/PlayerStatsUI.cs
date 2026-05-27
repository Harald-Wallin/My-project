using UnityEngine;
using TMPro;

public class PlayerStatsUI : MonoBehaviour
{
    [SerializeField] private PlayerStats playerStats;

    [SerializeField] private TextMeshProUGUI strengthText;
    [SerializeField] private TextMeshProUGUI armorText;
    [SerializeField] private TextMeshProUGUI healthText;
    [SerializeField] private TextMeshProUGUI damageText;
    [SerializeField] private TextMeshProUGUI critText;
    [SerializeField] private TextMeshProUGUI swiftnessText;
    [SerializeField] private TextMeshProUGUI hitText;
    [SerializeField] private TextMeshProUGUI evasionText;

    private void OnEnable()
    {
        playerStats.OnStatsChanged += UpdateUI;
        playerStats.OnHealthChanged += UpdateUI;
        UpdateUI();
    }

    private void OnDisable()
    {
        playerStats.OnStatsChanged -= UpdateUI;
        playerStats.OnHealthChanged -= UpdateUI;
    }


    private void UpdateUI()
    {
        strengthText.text = $"Strength: {playerStats.GetStat(StatType.Strength):0}";
        swiftnessText.text = $"Swiftness: {playerStats.GetStat(StatType.Swiftness):0}";
        armorText.text = $"Armor: {playerStats.GetStat(StatType.Armor):0}";
        healthText.text = $"HP: {playerStats.currentHP} / {playerStats.GetStat(StatType.MaxHP):0}";
        damageText.text = $"Damage: {playerStats.GetAttackDamage()}";
        critText.text = $"Crit: {playerStats.GetStat(StatType.CritChance) * 100f:0.0}%";
        hitText.text = $"Hit: {playerStats.GetStat(StatType.HitChance) * 100:0.0}%";
        evasionText.text = $"Evasion: {playerStats.GetStat(StatType.Evasion) * 100:0.0}%";
    }

    public void Close()
    {
        gameObject.SetActive(false);
    }

}

