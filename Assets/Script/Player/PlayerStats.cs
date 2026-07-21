using UnityEngine;
using System;

public class PlayerStats : CharacterStats
{
    public int currentExp = 0;
    public int expToNextLevel = 112;
    [Header("LevelUpConfetti")]
    [SerializeField] private GameObject confettiPrefab;
    [SerializeField] private float confettiDuration = 5f;


    public Action OnExpChanged;
    public Action OnLevelChanged;

    [Header("Crime")]
    public bool murderMode = false;


    [Header("Regen")]
    [Tooltip("Percent of max HP restored each tick (0.01 = 1%)")]
    [SerializeField] private float regenPercentPerTick = 0.01f;
    [Tooltip("Seconds between each regen tick")]
    [SerializeField] private float regenInterval = 3f;

    private float regenTimer;


    private void ApplyLevelUpStats()
    {
        float oldMaxHP = GetStat(StatType.MaxHP);

        SetBaseStat(StatType.Strength,GetBaseStatValue(StatType.Strength) + 1f);

        SetBaseStat(StatType.Health,GetBaseStatValue(StatType.Health) + 4f);

        float newMaxHP = GetStat(StatType.MaxHP);

        float gainedHP = newMaxHP - oldMaxHP;

        currentHP += Mathf.RoundToInt(gainedHP);

        currentHP = Mathf.Clamp(
            currentHP,
            0,
            Mathf.RoundToInt(newMaxHP)
        );

        if (TalentManager.Instance != null)
        {
            TalentManager.Instance.availablePoints++;
        }

        TalentNotificationManager.Instance
            ?.NotifyNewTalentPoints();

        
        RaiseHealthChanged();
        GetMaxHP();

        if (confettiPrefab != null)
        {
            Vector3 spawnPosition =
                effectPoint != null
                    ? effectPoint.position
                    : transform.position + Vector3.up * 1.5f;

            GameObject effect =
                Instantiate(
                    confettiPrefab,
                    spawnPosition,
                    Quaternion.identity
                );

            Destroy(
                effect,
                confettiDuration
            );
        }
        else
        {
            Debug.LogWarning(
                "Confetti prefab är inte tilldelad i PlayerStats.",
                this
            );
        }
    }


    public void GainExp(int amount)
    {
        currentExp += amount;

        while (currentExp >= expToNextLevel)
        {
            currentExp -= expToNextLevel;
            level++;
            base.level = level;

            ApplyLevelUpStats();
            AnnouncementSpawner.Instance?.QueueAnnouncement(AnnouncementSpawner.Instance.Database.levelUp,$"Hail!\n You reached\nLevel {level}");

            expToNextLevel = Mathf.RoundToInt(expToNextLevel * 1.12f);

            OnLevelChanged?.Invoke();

        }

        OnExpChanged?.Invoke();
    }

    protected override void Awake()
    {
        base.Awake();
        base.level = level;
        regenTimer = regenInterval;
    }

    //DEBUG TEST- H tar skada, J healar
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.H))
        {
            TakeDamage(new DamageResult { damage = 5 }, this);
        }

        if (Input.GetKeyDown(KeyCode.J))
        {
            Heal(5);
            //Debug.Log("Player healed");
        }

        // Passive regeneration: restore approx regenPercentPerTick of max HP every regenInterval seconds
        int actualMaxHP = Mathf.RoundToInt(GetStat(StatType.MaxHP));

        if (actualMaxHP > 0 && currentHP > 0 && currentHP < actualMaxHP)
        {
            regenTimer -= Time.deltaTime;

            while (regenTimer <= 0f)
            {
                int healAmount = Mathf.Max(1,Mathf.RoundToInt(actualMaxHP * regenPercentPerTick)
                );
                Heal(healAmount);
                regenTimer += regenInterval;
            }
        }
        else
        {
            // keep timer in range when at full health or dead
            regenTimer = regenInterval;
        }
    }

    protected override void Die(CharacterStats killer)
    {
        RaiseDied(this);

        HandleDeathCleanup();

        PlayerDeathManager deathManager =
            GetComponent<PlayerDeathManager>();

        if (deathManager != null)
        {
            PlayerRespawnManager respawn =
                GetComponent<PlayerRespawnManager>();

            deathManager.HandleDeath(
                this,
                respawn.GetRespawnPosition()
            );
        }
    }

    void Respawn()
    {
        // TODO: replace with runestone system later
        transform.position = Vector3.zero;

        currentHP = GetMaxHP();
        RaiseHealthChanged();
        ResetHealth();
    }

    public void RaiseHealthChangedExternally()
    {
        RaiseHealthChanged();
    }

}


