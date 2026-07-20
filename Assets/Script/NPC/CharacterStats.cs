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
    private List<StatEntry> stats = new();

    private readonly Dictionary<StatType, float> statLookup = new();

    [Header("Derived Stats")]
    [SerializeField]
    [Tooltip(
        "Automatically generated stats based on primary stats, " +
        "stat definitions and the scaling profile."
    )]
    private List<DerivedStat> derivedStats = new();

    private readonly Dictionary<StatType, float> derivedLookup = new();

    [SerializeField]
    private int _currentHP;

    public int currentHP
    {
        get => _currentHP;
        set => _currentHP = value;
    }

    private static StatScalingProfile cachedScalingProfile;

    [Header("Advanced Stats")]
    public bool IsStunned => stunCount > 0;

    private int stunCount;

    private readonly List<StatModifier> modifiers = new();

    private CharacterStateController stateController;

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
        stateController =
            GetComponent<CharacterStateController>();

        deathReward =
            GetComponent<DeathReward>();

        RefreshStats();

        currentHP = GetMaxHP();
    }

    /// <summary>
    /// Synchronizes serialized stat entries with StatDatabase,
    /// rebuilds the runtime lookup and recalculates derived stats.
    /// </summary>
    public void RefreshStats()
    {
        SynchronizeWithDatabase();
        InitializeBaseStatLookup();
        RecalculateDerivedStats();
    }

    /// <summary>
    /// Rebuilds the serialized lists from StatDatabase while
    /// preserving existing primary-stat values.
    /// </summary>
    public void SynchronizeWithDatabase()
    {
        StatDatabase database =
            StatDatabase.Instance;

        if (database == null)
            return;

        Dictionary<StatType, float> existingPrimaryValues = new();

        for (int i = 0; i < stats.Count; i++)
        {
            StatEntry entry = stats[i];

            if (entry == null)
                continue;

            if (!existingPrimaryValues.ContainsKey(entry.stat))
            {
                existingPrimaryValues.Add(
                    entry.stat,
                    entry.value
                );
            }
        }

        stats.Clear();

        HashSet<StatType> addedPrimaryStats = new();

        foreach (StatDefinition definition in database.Stats)
        {
            if (definition == null)
                continue;

            if (definition.kind != StatKind.Primary)
                continue;

            if (!addedPrimaryStats.Add(definition.stat))
                continue;

            float value =
                existingPrimaryValues.TryGetValue(
                    definition.stat,
                    out float existingValue)
                    ? existingValue
                    : definition.defaultValue;

            stats.Add(new StatEntry
            {
                stat = definition.stat,
                value = value
            });
        }

        derivedStats.Clear();

        HashSet<StatType> addedDerivedStats = new();

        foreach (StatDefinition definition in database.Stats)
        {
            if (definition == null)
                continue;

            if (definition.kind != StatKind.Derived)
                continue;

            if (!addedDerivedStats.Add(definition.stat))
                continue;

            derivedStats.Add(new DerivedStat
            {
                stat = definition.stat,
                value = definition.defaultValue
            });
        }
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

    public int TakeDamage(
        DamageResult result,
        CharacterStats attacker)
    {
        if (result.isMiss)
        {
            FloatingTextSpawner.Instance?.SpawnCustomText(
                transform.position,
                "Miss",
                false
            );

            OnDamagedBy?.Invoke(attacker);

            return 0;
        }

        if (result.isEvaded)
        {
            FloatingTextSpawner.Instance?.SpawnCustomText(
                transform.position,
                "Evade",
                false
            );

            OnDamagedBy?.Invoke(attacker);

            return 0;
        }

        int finalDamage = result.damage;

        if (result.isBlocked)
        {
            PlayerStats player =
                GetComponent<PlayerStats>();

            if (player != null)
            {
                WardSystem ward =
                    GetComponent<WardSystem>();

                ward?.AddWard(1);
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

        DamageReaction reaction =
            GetComponentInChildren<DamageReaction>();

        if (reaction != null &&
            attacker != null)
        {
            reaction.PlayReaction(
                attacker.transform.position
            );
        }

        CrimeManager.HandleAttackCrime(
            attacker,
            this
        );

        stateController?.EnterCombat();

        OnDamaged(attacker);

        string damageText = $"-{finalDamage}";

        if (result.isBlocked)
        {
            damageText +=
                $" <color=white>({result.blockedAmount} Block)</color>";
        }

        bool attackerIsPlayer =
            attacker is PlayerStats;

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

    protected virtual void OnDamaged(
        CharacterStats attacker)
    {
    }

    public int GetAttackDamage()
    {
        float baseDamage =
            GetStat(StatType.BaseMeleeDamage);

        float weaponDamage =
            GetStat(StatType.WeaponDamage);

        return Mathf.Max(
            1,
            Mathf.RoundToInt(
                baseDamage + weaponDamage
            )
        );
    }

    public int GetMaxHP()
    {
        return Mathf.RoundToInt(
            GetStat(StatType.MaxHP)
        );
    }

    public virtual void Heal(int amount)
    {
        currentHP = Mathf.Clamp(
            currentHP + amount,
            0,
            GetMaxHP()
        );

        RaiseHealthChanged();
    }

    public int TakeRawDamage(
        int damage,
        CharacterStats attacker)
    {
        DamageResult result = new DamageResult
        {
            damage = damage,
            isCrit = false,
            isMiss = false,
            isEvaded = false
        };

        return TakeDamage(
            result,
            attacker
        );
    }

    public void AddStun()
    {
        stunCount++;
    }

    public void RemoveStun()
    {
        stunCount =
            Mathf.Max(0, stunCount - 1);
    }

    public bool CanAct()
    {
        return !IsStunned;
    }

    public void RecalculateDerivedStats()
    {
        derivedLookup.Clear();

        StatDatabase database =
            StatDatabase.Instance;

        if (database == null)
            return;

        Dictionary<StatType, DerivedStat> derivedByType = new();

        for (int i = 0; i < derivedStats.Count; i++)
        {
            DerivedStat derived = derivedStats[i];

            if (derived == null)
                continue;

            StatDefinition definition =
                database.GetDefinition(derived.stat);

            float defaultValue =
                definition != null
                    ? definition.defaultValue
                    : 0f;

            derived.value = defaultValue;

            derivedByType[derived.stat] = derived;
            derivedLookup[derived.stat] = defaultValue;
        }

        StatScalingProfile profile =
            ScalingProfile;

        if (profile == null)
            return;

        foreach (StatScalingRule rule in profile.rules)
        {
            if (rule == null)
                continue;

            StatDefinition sourceDefinition =
                database.GetDefinition(rule.source);

            if (sourceDefinition == null ||
                sourceDefinition.kind != StatKind.Primary)
            {
                continue;
            }

            // Uses the fully modified primary-stat value.
            // Equipment or talents that increase Health can
            // therefore also increase MaxHP.
            float sourceValue =
                GetStat(rule.source);

            foreach (StatScalingOutput output in rule.outputs)
            {
                if (output == null)
                    continue;

                StatDefinition outputDefinition =
                    database.GetDefinition(output.stat);

                if (outputDefinition == null ||
                    outputDefinition.kind != StatKind.Derived)
                {
                    continue;
                }

                if (!derivedByType.TryGetValue(
                        output.stat,
                        out DerivedStat derived))
                {
                    continue;
                }

                derived.value +=
                    sourceValue * output.value;

                derivedLookup[output.stat] =
                    derived.value;
            }
        }
    }

    public float GetStat(
    StatType stat)
    {
        float value =
            GetRawStatValue(stat);

        float talentFlat = 0f;
        float talentPercent = 0f;

        float equipmentFlat = 0f;
        float equipmentPercent = 0f;

        float buffFlat = 0f;
        float buffPercent = 0f;

        float oathFlat = 0f;
        float oathPercent = 0f;

        foreach (StatModifier modifier
                 in modifiers)
        {
            if (modifier == null ||
                modifier.stat != stat)
            {
                continue;
            }

            switch (modifier.sourceType)
            {
                case ModifierSourceType.Talent:
                    AddModifierValue(
                        modifier,
                        ref talentFlat,
                        ref talentPercent
                    );
                    break;

                case ModifierSourceType.Equipment:
                    AddModifierValue(
                        modifier,
                        ref equipmentFlat,
                        ref equipmentPercent
                    );
                    break;

                case ModifierSourceType.Buff:
                    AddModifierValue(
                        modifier,
                        ref buffFlat,
                        ref buffPercent
                    );
                    break;

                case ModifierSourceType.Oath:
                    AddModifierValue(
                        modifier,
                        ref oathFlat,
                        ref oathPercent
                    );
                    break;
            }
        }

        float talentStage =
            (value + talentFlat) *
            (1f + talentPercent);

        float equipmentStage =
            (talentStage + equipmentFlat) *
            (1f + equipmentPercent);

        float buffStage =
            (equipmentStage + buffFlat) *
            (1f + buffPercent);

        float oathStage =
            (buffStage + oathFlat) *
            (1f + oathPercent);

        return oathStage;
    }

    private static void AddModifierValue(
        StatModifier modifier,
        ref float flat,
        ref float percent)
    {
        if (modifier.type ==
            ModifierType.Flat)
        {
            flat += modifier.value;
        }
        else
        {
            percent += modifier.value;
        }
    }

    public void AddModifier(StatModifier modifier)
    {
        if (modifier == null)
            return;

        float oldMaxHP =
            GetStat(StatType.MaxHP);

        modifiers.Add(modifier);

        RecalculateDerivedStats();

        float newMaxHP =
            GetStat(StatType.MaxHP);

        PreserveCurrentHealthPercentage(
            oldMaxHP,
            newMaxHP
        );

        OnStatsChanged?.Invoke();
    }

    public void RemoveModifiersFromSource(
        object source)
    {
        float oldMaxHP =
            GetStat(StatType.MaxHP);

        modifiers.RemoveAll(
            modifier => modifier.source == source
        );

        RecalculateDerivedStats();

        float newMaxHP =
            GetStat(StatType.MaxHP);

        PreserveCurrentHealthPercentage(
            oldMaxHP,
            newMaxHP
        );

        OnStatsChanged?.Invoke();
    }

    private void PreserveCurrentHealthPercentage(
        float oldMaxHP,
        float newMaxHP)
    {
        if (Mathf.Approximately(
                oldMaxHP,
                newMaxHP))
        {
            return;
        }

        if (oldMaxHP <= 0f)
        {
            currentHP = Mathf.Clamp(
                currentHP,
                0,
                Mathf.RoundToInt(newMaxHP)
            );

            RaiseHealthChanged();
            return;
        }

        float healthPercentage =
            currentHP / oldMaxHP;

        currentHP = Mathf.Clamp(
            Mathf.RoundToInt(
                newMaxHP * healthPercentage
            ),
            0,
            Mathf.RoundToInt(newMaxHP)
        );

        RaiseHealthChanged();
    }

    protected virtual void Die(
        CharacterStats killer)
    {
        RaiseDied(this);
        GiveDeathRewards(killer);
        HandleDeathCleanup();
        Destroy(gameObject);
    }

    protected virtual void HandleDeathCleanup()
    {
        if (!(this is PlayerStats))
        {
            Collider2D col =
                GetComponent<Collider2D>();

            if (col != null)
                col.enabled = false;
        }

        Rigidbody2D rb =
            GetComponent<Rigidbody2D>();

        if (rb != null)
            rb.linearVelocity = Vector2.zero;

        BuffSystem buffs =
            GetComponent<BuffSystem>();

        buffs?.RemoveDeathRemovableBuffs();
    }

    public bool IsHostileTo(
        CharacterStats other)
    {
        if (other == null)
            return false;

        if (other == this)
            return false;

        if (faction == null ||
            other.faction == null)
        {
            return false;
        }

        if (faction == other.faction)
            return false;

        ReputationState standing =
            faction.GetStanding(other.faction);

        if (standing == ReputationState.Hated)
            return true;

        PlayerStats player =
            other.GetComponent<PlayerStats>();

        if (player != null)
        {
            PlayerReputationManager reputation =
                player.GetComponent<PlayerReputationManager>();

            if (reputation != null)
            {
                ReputationState state =
                    reputation.GetReputationState(faction);

                if (state == ReputationState.Hated)
                    return true;
            }
        }

        return false;
    }

    public bool IsFriendlyTo(
        CharacterStats other)
    {
        if (other == null)
            return false;

        if (faction == null ||
            other.faction == null)
        {
            return false;
        }

        ReputationState standing =
            faction.GetStanding(other.faction);

        return standing != ReputationState.Hated &&
               standing != ReputationState.Indifferent;
    }

    public bool IsHostileToPlayer(
        PlayerStats player)
    {
        if (player == null)
            return false;

        PlayerReputationManager reputation =
            player.GetComponent<PlayerReputationManager>();

        if (reputation != null &&
            faction != null)
        {
            ReputationState state =
                reputation.GetReputationState(faction);

            if (state == ReputationState.Hated)
                return true;
        }

        NPCReactionController reaction =
            GetComponent<NPCReactionController>();

        return reaction != null &&
               reaction.IsTemporarilyHostile;
    }

    protected virtual void GiveDeathRewards(
        CharacterStats killer)
    {
        deathReward?.GiveRewards(
            this,
            killer
        );
    }

    public void EnterCombat()
    {
        stateController?.EnterCombat();
    }

    public bool IsInCombat()
    {
        return stateController != null &&
               stateController.InCombat;
    }

    private void InitializeBaseStatLookup()
    {
        statLookup.Clear();

        foreach (StatEntry entry in stats)
        {
            if (entry == null)
                continue;

            statLookup[entry.stat] =
                entry.value;
        }
    }

    public void SetBaseStat(
        StatType stat,
        float value)
    {
        StatDatabase database =
            StatDatabase.Instance;

        StatDefinition definition =
            database?.GetDefinition(stat);

        if (definition == null)
        {
            Debug.LogWarning(
                $"Kan inte sätta {stat}: ingen StatDefinition finns.",
                this
            );

            return;
        }

        if (definition.kind != StatKind.Primary)
        {
            Debug.LogWarning(
                $"Kan inte sätta {stat} som base stat eftersom " +
                $"definitionen är {definition.kind}.",
                this
            );

            return;
        }

        StatEntry entry =
            stats.Find(candidate =>
                candidate != null &&
                candidate.stat == stat);

        if (entry == null)
        {
            entry = new StatEntry
            {
                stat = stat
            };

            stats.Add(entry);
        }

        entry.value = value;
        statLookup[stat] = value;

        RecalculateDerivedStats();

        OnStatsChanged?.Invoke();
    }

    /// <summary>
    /// Returns the stored primary value or the calculated
    /// derived value before modifiers for that same stat.
    /// </summary>
    public float GetBaseStatValue(
        StatType stat)
    {
        return GetRawStatValue(stat);
    }

    private float GetRawStatValue(
        StatType stat)
    {
        if (statLookup.TryGetValue(
                stat,
                out float baseValue))
        {
            return baseValue;
        }

        if (derivedLookup.TryGetValue(
                stat,
                out float derivedValue))
        {
            return derivedValue;
        }

        return 0f;
    }

    private StatScalingProfile ScalingProfile
    {
        get
        {
            if (cachedScalingProfile == null)
            {
                cachedScalingProfile =
                    Resources.Load<StatScalingProfile>(
                        "Stats/StatScalingProfile"
                    );

                if (cachedScalingProfile == null)
                {
                    cachedScalingProfile =
                        Resources.Load<StatScalingProfile>(
                            "StatScalingProfile"
                        );
                }

                if (cachedScalingProfile == null)
                {
                    Debug.LogError(
                        "StatScalingProfile kunde inte hittas. " +
                        "Lägg asseten i Resources/Stats eller Resources.",
                        this
                    );
                }
            }

            return cachedScalingProfile;
        }
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        RefreshStats();
    }
#endif
}