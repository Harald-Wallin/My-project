using TMPro;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public sealed class PlayerHealthUI :
    MonoBehaviour
{
    [Header("References")]

    [SerializeField]
    private PlayerStats player;

    [SerializeField]
    private Image healthFill;

    [SerializeField]
    private TMP_Text hpText;


    private void Awake()
    {
        ConfigureFillImage(
            healthFill
        );
    }


    private void Start()
    {
        if (player == null)
        {
            player =
                PlayerReference.Player;
        }

        if (player == null)
        {
            Debug.LogWarning(
                $"{nameof(PlayerHealthUI)} kunde inte hitta spelaren.",
                this
            );

            return;
        }

        player.OnHealthChanged -=
            HandleHealthChanged;

        player.OnHealthChanged +=
            HandleHealthChanged;

        Refresh();
    }


    private void OnDestroy()
    {
        if (player == null)
            return;

        player.OnHealthChanged -=
            HandleHealthChanged;
    }


    private void HandleHealthChanged()
    {
        Refresh();
    }


    private void Refresh()
    {
        if (player == null)
            return;

        float maximumHealth =
            Mathf.Max(
                0f,
                player.GetStat(
                    StatType.MaxHP
                )
            );

        if (healthFill != null)
        {
            healthFill.fillAmount =
                maximumHealth > 0f
                    ? Mathf.Clamp01(
                        player.currentHP /
                        maximumHealth
                    )
                    : 0f;
        }

        if (hpText != null)
        {
            hpText.text =
                $"{Mathf.CeilToInt(player.currentHP)} / " +
                $"{Mathf.CeilToInt(maximumHealth)}";
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