using System.Collections;
using TMPro;
using Unity.VisualScripting.Antlr3.Runtime.Misc;
using UnityEngine;
using UnityEngine.UI;

public class PlayerHealthUI : MonoBehaviour
{
    public PlayerStats player;
    public Slider healthSlider;
    public TMP_Text hpText;

    [Header("Visibility")]
    public CanvasGroup canvasGroup;
    public float hideDelay = 4f;

    Coroutine hideRoutine;

    void Start()
    {
        if (player == null)
            player = PlayerReference.Player;

        player.OnHealthChanged += OnHealthChanged;

        healthSlider.maxValue = player.GetStat(StatType.MaxHP);

        canvasGroup.alpha = 0f; // start osynlig
    }


    void OnDestroy()
    {
        if (player != null)
            player.OnHealthChanged -= OnHealthChanged;
    }


    void OnHealthChanged()
    {

        UpdateHealth();
        Show();

        if (player.currentHP >= player.GetStat(StatType.MaxHP))
        {
            if (hideRoutine != null)
                StopCoroutine(hideRoutine);

            hideRoutine = StartCoroutine(HideAfterDelay());
        }
    }

    void UpdateHealth()
    {
        healthSlider.maxValue = player.GetStat(StatType.MaxHP);
        healthSlider.value = player.currentHP;

        if (hpText != null)
        {
            hpText.text =
            $"{Mathf.CeilToInt(player.currentHP)} / {Mathf.CeilToInt(player.GetStat(StatType.MaxHP))}"; 
        }
    }

    void Show()
    {
        canvasGroup.alpha = 1f;
    }

    IEnumerator HideAfterDelay()
    {
        yield return new WaitForSeconds(hideDelay);
        canvasGroup.alpha = 0f;
    }
}



