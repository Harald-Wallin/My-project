using System;
using System.Collections.Generic;
using UnityEngine;

public class CharacterStats : MonoBehaviour
{
    [Header("Faction")]
    public Faction faction;

    [Header("Crime")]
    public int reputationLossOnHit = 20;
    public int reputationLossOnDeath = 100;

    [Header("Identity")]
    public string displayName = "NPC";

    [Header("Level")]
    public int level = 1;

    [Header("Base Stats")]
    [SerializeField]
    private List<BaseStatEntry> baseStatEntries = new();
    private readonly Dictionary<StatType, float> baseStats =new();
    public int maxHP;
    public int currentHP;
    //public int CurrentHP => currentHP; SKA DENNA VARA KVAR? ELLER ERSÄTTA den övre currentHP??
    private int baseStrength;
    public int strength;
    [Tooltip("Base damage without any weapon equipped.")]
    public int baseMeleeDamage = 2;

    [Tooltip("Derived combat stat. Normally calculated from Strength.")]
    public int attackPower = 0;
    public float swiftness = 0f;
    public int armor;
    public float movementSpeed = 2.5f;
    public float attackSpeed = 1f;//attack per sekund

    [Header("Advanced Stats")]
    public bool IsStunned => stunCount > 0;
    private int stunCount = 0;
    public int weaponDamage;
    public float hitChance = 0.85f;
    public float evasion = 0.08f;
    public float blockChance = 0f;
    public int blockValue = 0;

    private List<StatModifier> modifiers = new List<StatModifier>();
    private CharacterStateController stateController;

    [Header("Combat")]
    public float critChance = 0.3f;
    public float critMultiplier = 2f;

    public event Action OnHealthChanged;
    public event Action OnStatsChanged;
    public event Action<CharacterStats> OnDamagedBy;
    public event Action<CharacterStats> OnDied;

    [Header("Death Rewards")]
    public DeathReward deathReward;

    [Header("VFX")]
    public Transform effectPoint;



    protected virtual void Awake()
    {
        currentHP = maxHP;
        baseStrength = strength;
        InitializeBaseStats();
        stateController = GetComponent<CharacterStateController>();
        deathReward = GetComponent<DeathReward>();
    }

    public virtual void ResetHealth()
    {
        currentHP = GetMaxHP();
        RaiseHealthChanged();
    }

    protected void RaiseDied(CharacterStats deadCharacter)
    {
        OnDied?.Invoke(deadCharacter);
    }

    public void RaiseHealthChanged()
    {
        OnHealthChanged?.Invoke();
    }


    public int TakeDamage(DamageResult result, CharacterStats attacker)
    {
        if (result.isMiss)
        {
            FloatingTextSpawner.Instance?.SpawnCustomText(transform.position, "Miss", false);
            OnDamagedBy?.Invoke(attacker);

            return 0;
        }

        if (result.isEvaded)
        {
            FloatingTextSpawner.Instance?.SpawnCustomText(transform.position, "Evade", false);
            OnDamagedBy?.Invoke(attacker);

            return 0;
        }

        int finalDamage = result.damage;

        if (result.isBlocked)
        {
            PlayerStats player = GetComponent<PlayerStats>();

            if (player != null)
            {
                WardSystem ward = GetComponent<WardSystem>();

                if (ward != null)
                {
                    ward.AddWard(1);
                }
            }
        }

        currentHP -= finalDamage;

        RaiseHealthChanged();

        stateController?.NotifyCombatActivity();

        if (attacker != null)
        {
            CharacterStateController attackerState =
                attacker.GetComponent<CharacterStateController>();

            attackerState?.NotifyCombatActivity();
        }

        OnDamagedBy?.Invoke(attacker);

        DamageReaction reaction = GetComponentInChildren<DamageReaction>();

        if (reaction != null && attacker != null)
        {
            reaction.PlayReaction(
                attacker.transform.position
            );
        }

        // Crime BEFORE death
        CrimeManager.HandleAttackCrime(attacker, this);

        // NPC reaction BEFORE death
        if (stateController != null)
        {
            stateController.EnterCombat();
        }

        OnDamaged(attacker);

        string damageText = $"-{finalDamage}";

        if (result.isBlocked)
        {
            damageText +=
                $" <color=white>({result.blockedAmount} Block)</color>";
        }

        bool attackerIsPlayer = attacker is PlayerStats;

        FloatingTextSpawner.Instance?.SpawnDamageText(
            transform.position,
            damageText,
            result.isCrit,
            !attackerIsPlayer
        );

        if (currentHP <= 0)
        {
            currentHP = 0;
            Die(attacker);
        }

        return finalDamage;
    }

    protected virtual void OnDamaged(CharacterStats attacker)
    {
        // Bas-klass gör inget
    }

    public int GetAttackDamage()
    {
        float baseDamage = GetStat(StatType.BaseMeleeDamage);

        float weaponDamage = GetStat(StatType.WeaponDamage);

        return Mathf.Max(
            1,
            Mathf.RoundToInt(baseDamage + weaponDamage)
        );
    }

    public int GetMaxHP()
    {
        return Mathf.RoundToInt(GetStat(StatType.MaxHP));
    }

    public virtual void Heal(int amount)
    {
          currentHP  =Mathf.Clamp(currentHP + amount, 0, Mathf.RoundToInt(GetStat(StatType.MaxHP)));
        RaiseHealthChanged();
    }

    public int TakeRawDamage(int damage, CharacterStats attacker)
    {
        DamageResult result = new DamageResult
        {
            damage = damage,
            isCrit = false,
            isMiss = false,
            isEvaded = false
        };

        return TakeDamage(result, attacker);
    }

    // ---- STAT MODIFIERS ADD ----
    float GetBaseStat(StatType stat)
    {
        return GetBaseStatValue(stat);
    }

    public void AddStun()
    {
        stunCount++;
    }

    public void RemoveStun()
    {
        stunCount = Mathf.Max(0, stunCount - 1);
    }

    public bool CanAct()
    {
        return !IsStunned;
    }

    public float GetStat(StatType stat)
    {
        float baseValue = GetBaseStat(stat);

        float talentFlat = 0f;
        float talentPercent = 0f;

        float equipmentFlat = 0f;
        float equipmentPercent = 0f;

        float buffPercent = 0f;
        float oathPercent = 0f;

        float evasionPercent = 0f;

        foreach (var mod in modifiers)
        {
            if (mod.stat != stat) continue;

            switch (mod.sourceType)
            {
                case ModifierSourceType.Talent:
                    if (mod.type == ModifierType.Flat)
                        talentFlat += mod.value;
                    else
                        talentPercent += mod.value;
                    break;

                case ModifierSourceType.Equipment:
                    if (mod.type == ModifierType.Flat)
                        equipmentFlat += mod.value;
                    else
                        equipmentPercent += mod.value;
                    break;

                case ModifierSourceType.Buff:
                    if (mod.type == ModifierType.Percent)
                    {
                        if (mod.stat == StatType.Evasion)
                            evasionPercent += mod.value;
                        else
                            buffPercent += mod.value;
                    }
                    break;

                case ModifierSourceType.Oath:
                    if (mod.type == ModifierType.Percent)
                        oathPercent += mod.value;
                    break;
            }
        }

        // 🔥 Pipeline
        float talentBase = (baseValue + talentFlat) * (1f + talentPercent);

        float withEquipment = (talentBase + equipmentFlat) * (1f + equipmentPercent);

        float finalValue = withEquipment * (1f + buffPercent + oathPercent);

        return finalValue;
    }

    public void AddModifier(StatModifier mod)
    {
        float oldMaxHP = GetStat(StatType.MaxHP);

        modifiers.Add(mod);

        float newMaxHP = GetStat(StatType.MaxHP);

        // 🔥 Justera current HP proportionellt om maxHP ändras
        if (newMaxHP != oldMaxHP && oldMaxHP > 0)
        {
            float percent = (float)currentHP / oldMaxHP;
            currentHP = Mathf.RoundToInt(newMaxHP * percent);
            RaiseHealthChanged();
        }

        OnStatsChanged?.Invoke();
    }

    public void RemoveModifiersFromSource(object source)
    {
        float oldMaxHP = GetStat(StatType.MaxHP);

        modifiers.RemoveAll(m => m.source == source);

        float newMaxHP = GetStat(StatType.MaxHP);

        if (newMaxHP != oldMaxHP && oldMaxHP > 0)
        {
            float percent = (float)currentHP / oldMaxHP;
            currentHP = Mathf.RoundToInt(newMaxHP * percent);
            RaiseHealthChanged();
        }

        OnStatsChanged?.Invoke();
    }

    //-------------------DEATH-------------------
    protected virtual void Die(CharacterStats killer)
    {
        RaiseDied(this);
        //string killerName = killer != null ? killer.name : "Unknown";
        GiveDeathRewards(killer);
        HandleDeathCleanup();
        Destroy(gameObject);
    }

    protected virtual void HandleDeathCleanup()
    {
        if (!(this is PlayerStats))
        {
            Collider2D col = GetComponent<Collider2D>();

            if (col != null)
                col.enabled = false;
        }

        Rigidbody2D rb = GetComponent<Rigidbody2D>();

        if (rb != null)
            rb.linearVelocity = Vector2.zero;

        BuffSystem buffs = GetComponent<BuffSystem>();

        if (buffs != null)
        {
            buffs.RemoveDeathRemovableBuffs();
        }
    }

    //---------------REPUTATION-----------------
    public bool IsHostileTo(CharacterStats other)
    {
        //NPC kan inte hata sig själv <3
        if (other == this)
            return false;

        //NPC hatar aldrig sina egna faction-fränder
        if (faction == other.faction)
            return false;

        //NPC kan inte hata någon/något som inte finns
        if (other == null)
            return false;

        if (faction == null || other.faction == null)
            return false;

        ReputationState standing = faction.GetStanding(other.faction);

        if (standing == ReputationState.Hated)
            return true;

        PlayerStats player =
            other.GetComponent<PlayerStats>();

        if (player != null)
        {
            PlayerReputationManager rep =
                player.GetComponent<PlayerReputationManager>();

            if (rep != null)
            {
                ReputationState state =
                    rep.GetReputationState(faction);

                if (state == ReputationState.Hated)
                    return true;
            }
        }

        return false;
    }

    public bool IsFriendlyTo(CharacterStats other)
    {
        if (other == null)
            return false;

        if (faction == null || other.faction == null)
            return false;

        ReputationState standing = faction.GetStanding(other.faction);

        return standing != ReputationState.Hated &&
               standing != ReputationState.Indifferent;
    }

    public bool IsHostileToPlayer(PlayerStats player)
    {
        if (player == null)
            return false;

        // Permanent hostility
        PlayerReputationManager rep =
            player.GetComponent<PlayerReputationManager>();

        if (rep != null && faction != null)
        {
            ReputationState state =
                rep.GetReputationState(faction);

            if (state == ReputationState.Hated)
                return true;
        }

        // Temporary hostility
        NPCReactionController reaction =
            GetComponent<NPCReactionController>();

        if (reaction != null && reaction.IsTemporarilyHostile)
        {
            return true;
        }

        return false;
    }

    protected virtual void GiveDeathRewards(CharacterStats killer)
    {
        deathReward?.GiveRewards(this, killer);
    }

    public void EnterCombat()
    {
        stateController?.EnterCombat();
    }

    public bool IsInCombat()
    {
        return stateController != null && stateController.InCombat;
    }

    void InitializeBaseStats()
    {
        baseStats.Clear();

        foreach (var entry in baseStatEntries)
        {
            baseStats[entry.stat] = entry.value;
        }

        StatScaling.ApplyDerivedStats(this);
    }

    public void SetBaseStat(StatType stat, float value)
    {
        baseStats[stat] = value;
    }

    public float GetBaseStatValue(StatType stat)
    {
        if (baseStats.TryGetValue(stat, out float value))
            return value;

        return 0f;
    }
}

