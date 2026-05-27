using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;


public class Enemy : CharacterStats
{
    [Header("General")]
    public string enemyName = "Enemy";
    public int monsterLevel = 1;
    private bool isDead = false;


    [Header("Loot")]
    public GameObject corpsePrefab;
    public List<LootTable> lootTables = new List<LootTable>();



    [Header("UI")]
    [SerializeField] private TMP_Text levelText;

    public int CurrentHP => currentHP;

    [Header("Rewards")]
    public GameObject floatingExpTextPrefab;

    [Header("Respawn")]
    public MobSpawner spawner;

    [Header("Reputation Reward")]
    public Faction reputationRewardFaction;
    public int reputationRewardAmount = 0;

    [Header("World Flags")]
    public bool isStartZoneWolf = false;


    protected override void Awake()
    {
        base.Awake();

        if (levelText != null)
            levelText.text = $"{monsterLevel}";
    }


    public void SetLevel(int level)
    {
        monsterLevel = level;

        // Exempel på enkel scaling
        maxHP = 20 + level * 5;
        strength = 3 + level * 2;

        currentHP = maxHP;

        // Uppdatera leveltext direkt
        if (levelText != null)
            levelText.text = $"{monsterLevel}";
    }


    protected override void Die(CharacterStats killer)
    {
        if (isDead) return;
        isDead = true;

        // Stop AI
        AgressiveMobAI ai = GetComponent<AgressiveMobAI>();
        if (ai != null)
            ai.enabled = false;

        BaseAttackController attack = GetComponent<BaseAttackController>();

        if (attack != null)
            attack.enabled = false;

        // Give EXP
        int exp = 0;

        PlayerStats player = killer as PlayerStats;

        if (player != null)
        {
            exp = ExperienceCalculator.CalculateExp(monsterLevel, player.level);
            player.GainExp(exp);

            if (exp > 0)
                ShowExpText(exp);
        }

        // Give Reputation Reward
        if (player != null && reputationRewardFaction != null && reputationRewardAmount != 0)
        {
            PlayerReputationManager repManager =
                player.GetComponent<PlayerReputationManager>();

            if (repManager != null)
            {
                repManager.AddReputation(
                    reputationRewardFaction,
                    reputationRewardAmount
                );
            }
        }

        // Reputation penalty
        if (player != null && player.murderMode && player.IsFriendlyTo(this))
        {
            PlayerReputationManager repManager =
                player.GetComponent<PlayerReputationManager>();

            if (repManager != null)
                repManager.AddReputation(faction, -350);
        }

        // Spawn corpse
        SpawnCorpse();

        // Respawn
        if (spawner != null)
            spawner.OnMobDied();

        HandleDeathCleanup();

        Rigidbody2D rb = GetComponent<Rigidbody2D>();

        if (rb != null)
        {
            rb.linearVelocity = Vector2.zero;
        }

        // Destroy enemy completely
        base.Die(killer);
    }

    void SpawnCorpse()
    {
        //Debug.Log("SpawnCorpse() körs");

        if (corpsePrefab == null) return;

        GameObject corpse = Instantiate(
            corpsePrefab,
            transform.position,
            Quaternion.identity
        );

        CharacterStats corpseStats = corpse.GetComponent<CharacterStats>();
        if (corpseStats != null)
        {
            corpseStats.faction = null;
        }

        // 🔁 Flytta nameplate till corpse
        Transform nameplate = transform.Find("Nameplate");
        if (nameplate != null)
        {
            nameplate.SetParent(corpse.transform, true);

            // If nameplate UI exists, set it to corpse mode so it shows "Corpse" and stops updating
            var nameplateUI = nameplate.GetComponentInChildren<NameplateUI>();
            if (nameplateUI != null)
            {
                nameplateUI.SetCorpseMode();
            }
        }

        // 🎒 Loot
        LootContainer loot = corpse.GetComponent<LootContainer>();

        if (loot == null)
        {
            Debug.LogError("Corpse saknar LootContainer!");
        }
        else
        {
            Debug.Log("LootContainer hittad på corpse.");
        }

        if (loot != null && lootTables != null)
        {
            int dropSlots = Random.Range(0, 4); // 0–3 olika items

            for (int i = 0; i < dropSlots; i++)
            {
                List<ItemData> drop =
                    LootGenerator.GenerateSingleDrop(lootTables);

                loot.items.AddRange(drop);
            }

        }

    }

    public void ResetHealth()
    {
        currentHP = maxHP;
    }

    void ShowExpText(int exp)
    {
        GameObject textObj = Instantiate(
            floatingExpTextPrefab,
            transform.position + Vector3.up * 1.5f,
            Quaternion.identity
        );

        textObj.GetComponentInChildren<TMPro.TMP_Text>().text =exp + " EXP";
    }
}
