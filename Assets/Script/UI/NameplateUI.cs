using TMPro;
using Unity.VisualScripting.Antlr3.Runtime.Misc;
using UnityEngine;
using UnityEngine.UI;

public class NameplateUI : MonoBehaviour
{
    [Header("Refs")]
    public CharacterStats target;
    public Image healthFill;
    public TMP_Text hpText;
    public TMP_Text nameText;
    public TMP_Text levelText;

    private PlayerStats player;
    private PlayerReputationManager repManager;

    [Header("Buff UI")]
    [SerializeField] private Transform buffContainer;
    [SerializeField] private GameObject buffSlotPrefab;

    void Awake()
    {
        if (target == null)
            target = GetComponentInParent<CharacterStats>();

        player = PlayerReference.Player;
        repManager = FindFirstObjectByType<PlayerReputationManager>();
    }

    void Start()
    {
        if (target == null) return;

        nameText.text = target.displayName; // temporärt

        // TEST BUFFS
        //AddTestBuff(null, 10f);
        //AddTestBuff(null, 5f);

        UpdateHealth();
        UpdateLevelText();
    }

    void Update()
    {
        if (target == null) return;

        UpdateHealth();
        UpdateLevelText();
    }

    void UpdateHealth()
    {
        if (target.maxHP <= 0)
            return;

        float percent = (float)target.currentHP / target.maxHP;
        percent = Mathf.Clamp01(percent);

        healthFill.rectTransform.localScale =
            new Vector3(percent, 1f, 1f);

        // Color health fill according to reputation with target's faction
        Color fillColor = Color.white;

        NPCReactionController reaction =
    target.GetComponent<NPCReactionController>();

        bool hostile =
            target.IsHostileToPlayer(player);

        bool temporarilyHostile =
            reaction != null &&
            reaction.IsTemporarilyHostile;

        if (hostile || temporarilyHostile)
        {
            fillColor =
                ReputationColorUtility.GetColor(1);
        }
        else
        {
            if (repManager != null && target.faction != null)
            {
                var rep =
                    repManager.GetReputation(target.faction);

                int level =
                    rep != null
                    ? rep.level
                    : 3;

                fillColor =
                    ReputationColorUtility.GetColor(level);
            }
        }

        if (healthFill != null)
            healthFill.color = fillColor;

        if (hpText != null)
            hpText.text = $"{target.currentHP} / {target.maxHP}";
    }

    void UpdateLevelText()
    {
        if (levelText == null || player == null)
            return;

        int levelDiff = GetLevel(target) - player.level;

        levelText.text = GetLevel(target).ToString();
        levelText.color = GetDifficultyColor(levelDiff);
    }

    int GetLevel(CharacterStats stats)
    {
        // För mobs (Enemy)
        Enemy enemy = stats.GetComponent<Enemy>();
        if (enemy != null)
            return enemy.monsterLevel;

        // För player
        PlayerStats playerStats = stats.GetComponent<PlayerStats>();
        if (playerStats != null)
            return playerStats.level;

        // fallback
        return 1;
    }

    Color GetDifficultyColor(int diff)
    {
        if (diff >= 5)
            return Hex("#D51512"); // röd
        else if (diff >= 3)
            return Hex("#D55912"); // orange
        else if (diff >= -2)
            return Hex("#F5F207"); // gul
        else if (diff >= -5)
            return Hex("#4D9927"); // grön
        else
            return Hex("#9E9E9E"); // grå
    }

    Color Hex(string hex)
    {
        ColorUtility.TryParseHtmlString(hex, out Color color);
        return color;
    }

    public void SetCorpseMode()
    {
        if (hpText != null)
            hpText.text = "Corpse";

        if (healthFill != null)
            healthFill.rectTransform.localScale =
                new Vector3(0f, 1f, 1f);

        enabled = false;
    }

    public void AddBuff(ActiveBuff buff)
    {
        if (buffContainer == null || buffSlotPrefab == null)
            return;

        GameObject slot = Instantiate(buffSlotPrefab, buffContainer);

        BuffSlotUI buffUI = slot.GetComponent<BuffSlotUI>();
        if (buffUI != null)
        {
            BuffSystem buffSystem = target.GetComponent<BuffSystem>();
            buffUI.Setup(buff, buffSystem);
        }

        SortBuffs();
    }

    void SortBuffs()
    {
        var slots = buffContainer.GetComponentsInChildren<BuffSlotUI>();

        System.Array.Sort(slots, (a, b) =>
            a.GetRemainingTime().CompareTo(b.GetRemainingTime()));

        for (int i = 0; i < slots.Length; i++)
        {
            slots[i].transform.SetSiblingIndex(i);
        }
    }
}