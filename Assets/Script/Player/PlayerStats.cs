using UnityEngine;
using System;

public class PlayerStats : CharacterStats
{

    public int level = 1;
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
    

    // Hemlig playercondition kodsamling att implementera senare
    //public bool hasKilledStartZoneWolf = false;
    //player.hasKilledStartZoneWolf = true;
    //if (!player.hasKilledStartZoneWolf)
    //{
        // Unlock special dialog / quest / item
    //}

    void ApplyLevelUpStats()
    {
        strength += 1;

        float oldMaxHP = GetStat(StatType.MaxHP);
        maxHP += 4;

        float newMaxHP = GetStat(StatType.MaxHP);
        float gainedHP= newMaxHP - oldMaxHP;
        currentHP += Mathf.RoundToInt(gainedHP);
        currentHP = Mathf.Clamp(currentHP, 0, Mathf.RoundToInt(newMaxHP));

        TalentManager.Instance.availablePoints++;

        TalentNotificationManager.Instance
            ?.NotifyNewTalentPoints();

        RaiseHealthChanged();
        //Debug.Log($"Level up bonuses → STR: {strength}, HP: {currentHP}/{maxHP}");

        if (confettiPrefab != null)
        {
            Vector3 spawnPos = (effectPoint != null) ? effectPoint.position : (transform.position + Vector3.up * 1.5f);
            GameObject vfx = Instantiate(confettiPrefab, spawnPos, Quaternion.identity);
            Destroy(vfx, confettiDuration);
        }
        else
        {
            Debug.LogWarning("Confetti prefab inte tilldelad i inspector");
        }
    }


    public void GainExp(int amount)
    {
        currentExp += amount;

        while (currentExp >= expToNextLevel)
        {
            currentExp -= expToNextLevel;
            level++;

            ApplyLevelUpStats();
            AnnouncementSpawner.Instance?.QueueAnnouncement(AnnouncementSpawner.Instance.Database.levelUp,$"Hail!\n You reached\nLevel {level}");
            //AnnouncementManager.Instance?.Show("Hail!\n" + $"LEVEL {level}", Color.yellow, 120, 3f);

            expToNextLevel = Mathf.RoundToInt(expToNextLevel * 1.12f);

            //Debug.Log($"CONGRATULATIONS, you are now lvl {level}");
            OnLevelChanged?.Invoke();

        }

        OnExpChanged?.Invoke();
    }

    protected override void Awake()
    {
        base.Awake();
        regenTimer = regenInterval;
    }

    //DEBUG TEST- H tar skada, J healar
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.H))
        {
            TakeDamage(new DamageResult { damage = 5 }, this);
            Debug.Log("Player took damage");
        }

        if (Input.GetKeyDown(KeyCode.J))
        {
            Heal(5);
            Debug.Log("Player healed");
        }

        // Passive regeneration: restore approx regenPercentPerTick of max HP every regenInterval seconds
        int actualMaxHP = Mathf.RoundToInt(GetStat(StatType.MaxHP));

        if (actualMaxHP > 0 &&
            currentHP > 0 &&
            currentHP < actualMaxHP)
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
    }

    public void RaiseHealthChangedExternally()
    {
        RaiseHealthChanged();
    }

}


