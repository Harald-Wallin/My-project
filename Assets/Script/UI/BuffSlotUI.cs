using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.EventSystems;

public class BuffSlotUI : MonoBehaviour,
    IPointerEnterHandler,
    IPointerExitHandler
{
    [Header("UI")]
    [SerializeField] private Image icon;

    [Header("Player Buff UI")]
    [SerializeField] private TMP_Text stackText;
    [SerializeField] private TMP_Text timerText;

    private ActiveBuff buff;
    private BuffSystem ownerBuffSystem;

    public void Setup(ActiveBuff buff, BuffSystem owner)
    {
        Debug.Log($"BuffSlot Setup: {buff.Name} on {gameObject.name}");

        this.buff = buff;
        ownerBuffSystem = owner;

        icon.sprite = buff.Icon;

        RefreshUI();
    }

    void Update()
    {
        if (buff == null)
            return;

        if (ownerBuffSystem != null &&
        !ownerBuffSystem.HasBuff(buff))
        {
            Destroy(gameObject);
            return;
        }

        // 🔥 remove slot when buff expires
        if (buff.IsFinished)
        {
            Destroy(gameObject);
            return;
        }

        RefreshUI();

        HandlePulseEffect();
    }

    void RefreshUI()
    {
        // =========================
        // TIMER TEXT
        // =========================

        if (timerText != null)
        {
            timerText.text = FormatDuration(buff.RemainingTime);
        }

        // =========================
        // STACK TEXT
        // =========================

        if (stackText != null)
        {
            if (buff.stacks > 1)
            {
                stackText.gameObject.SetActive(true);
                stackText.text = buff.stacks.ToString();
            }
            else
            {
                stackText.gameObject.SetActive(false);
            }
        }
    }

    void HandlePulseEffect()
    {
        // 🔥 Don't pulse permanent buffs
        if (float.IsInfinity(buff.duration))
        {
            icon.transform.localScale = Vector3.one;
            return;
        }

        // 🔥 pulse final 3 seconds
        if (buff.RemainingTime <= 3f)
        {
            float pulse =
                Mathf.Sin(Time.time * 8f) * 0.15f + 1f;

            icon.transform.localScale =
                Vector3.one * pulse;
        }
        else
        {
            icon.transform.localScale = Vector3.one;
        }
    }

    string FormatDuration(float seconds)
    {
        // 🔥 permanent effects
        if (float.IsInfinity(seconds))
        {
            return "∞";
        }

        // 2h+
        if (seconds >= 7200f)
        {
            int hours =
                Mathf.FloorToInt(seconds / 3600f);

            return $"{hours}h";
        }

        // 1h+
        if (seconds >= 3600f)
        {
            int hours =
                Mathf.FloorToInt(seconds / 3600f);

            int minutes =
                Mathf.FloorToInt((seconds % 3600f) / 60f);

            return $"{hours}h {minutes}m";
        }

        // 1m+
        if (seconds >= 60f)
        {
            int minutes =
                Mathf.FloorToInt(seconds / 60f);

            return $"{minutes}m";
        }

        // seconds
        return $"{Mathf.CeilToInt(seconds)}s";
    }

    public float GetRemainingTime()
    {
        if (buff == null)
            return 0f;

        return buff.RemainingTime;
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (buff == null)
            return;

        var provider = new BuffTooltipProvider(buff);

       /* ItemTooltip.Instance.Show(
        new BuffTooltipProvider(buff),
        icon.rectTransform,
        PlayerReference.Player,
        ItemTooltip.TooltipAnchorMode.BottomRight
        ); */

        //Denna nya instance placerar tooltip korrekt men verkar istället 
        //göra så att debuffs inte visas alls på spelaren (De appliceras verkar det som,
        //men syns inte i UI't)
        ItemTooltip.Instance.Show(
        new BuffTooltipProvider(buff),
        icon.rectTransform,
        PlayerReference.Player,
        ItemTooltip.TooltipAnchorMode.FixedBottomLeft
        );

    }

    public void OnPointerExit(PointerEventData eventData)
    {
        ItemTooltip.Instance.Hide();
    }
}