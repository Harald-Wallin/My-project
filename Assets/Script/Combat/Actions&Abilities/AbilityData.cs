using UnityEngine;

public enum AbilityType
{
    Melee,
    Spell,
    Buff,
    Curse
}

[CreateAssetMenu(menuName = "RPG/Ability")]
public class AbilityData : ScriptableObject, ITooltipProvider
{
    [Header("Basic")]
    public string abilityName;

    [TextArea]
    public string description;

    public Sprite icon;

    [Header("Type")]
    public AbilityType types;

    [Header("Tags")]
    public AbilityTag[] tags;

    // =========================================================
    // NEW ACTION SYSTEM
    // =========================================================

    [Header("New Action System")]

    [SerializeField]
    [Tooltip(
        "När detta är aktiverat använder den nya action-pipelinen " +
        "Targeting, Timing och Execution Settings.\n\n" +
        "Lämna avstängd på befintliga abilities tills de migreras."
    )]
    private bool useActionSettings;

    [SerializeField]
    private AbilityTargetingSettings targetingSettings =
        new();

    [SerializeField]
    private AbilityTimingSettings timingSettings =
        new();

    [SerializeField]
    private AbilityExecutionSettings executionSettings =
        new();

    // =========================================================
    // LEGACY CONFIGURATION
    // =========================================================

    [Header("Legacy Timing")]
    [Tooltip(
        "Används av det gamla AbilityController-systemet och som " +
        "fallback tills Use Action Settings aktiveras."
    )]
    public float cooldown = 5f;

    public float globalCooldown = 0.8f;
    public float castTime;

    [Header("Costs")]
    public int wardCost;

    [Header("Combat Rules")]
    public bool alwaysHits;
    public bool canCrit = true;
    public bool canMiss = true;
    public bool isSelfCast;
    public bool requiresHitCheck;
    public bool entersCombatState = true;

    [Header("Effects")]
    public AbilityEffect[] effects;

    // =========================================================
    // ACTION SYSTEM API
    // =========================================================

    /// <summary>
    /// Om denna ability har migrerats till den nya
    /// action-konfigurationen.
    /// </summary>
    public bool UsesActionSettings =>
        useActionSettings;

    public AbilityTargetingSettings TargetingSettings =>
        targetingSettings;

    public AbilityTimingSettings TimingSettings =>
        timingSettings;

    public AbilityExecutionSettings ExecutionSettings =>
        executionSettings;

    /// <summary>
    /// Cooldown som den aktiva runtime-pipelinen ska använda.
    ///
    /// Innan migration används det gamla cooldown-fältet.
    /// Efter migration används ExecutionSettings.
    /// </summary>
    public float EffectiveCooldown =>
        useActionSettings
            ? executionSettings.Cooldown
            : Mathf.Max(0f, cooldown);

    /// <summary>
    /// Global cooldown för denna ability.
    ///
    /// Noll betyder att abilityn inte ska starta någon GCD.
    /// </summary>
    public float EffectiveGlobalCooldown
    {
        get
        {
            if (!useActionSettings)
                return Mathf.Max(0f, globalCooldown);

            if (!executionSettings.TriggersGlobalCooldown)
                return 0f;

            if (executionSettings.UsesGlobalCooldownOverride)
            {
                return executionSettings
                    .GlobalCooldownOverride;
            }

            // Controllern använder sitt standardvärde.
            return 0f;
        }
    }

    /// <summary>
    /// Casttid som äldre system kan läsa under migrationen.
    ///
    /// För Channel och Charge kommer den nya controllern senare
    /// använda respektive timingvärden direkt.
    /// </summary>
    public float EffectiveCastTime
    {
        get
        {
            if (!useActionSettings)
                return Mathf.Max(0f, castTime);

            if (timingSettings.TimingType !=
                ActionTimingType.Cast)
            {
                return 0f;
            }

            return timingSettings.CastDuration;
        }
    }

    /// <summary>
    /// Kompatibilitetsvärde för kod som behöver avgöra om
    /// abilityn riktar sig direkt mot castern.
    /// </summary>
    public bool EffectiveIsSelfCast =>
        useActionSettings
            ? targetingSettings.TargetingMode ==
              TargetingMode.Self
            : isSelfCast;

    /// <summary>
    /// En ability behöver targeting-confirmation när dess
    /// activation mode är Confirmed.
    /// </summary>
    public bool RequiresConfirmation =>
        useActionSettings &&
        executionSettings.ActivationMode ==
        ActionActivationMode.Confirmed;

    /// <summary>
    /// Om abilityn ska starta direkt när den aktiveras.
    /// </summary>
    public bool ActivatesImmediately =>
        !useActionSettings ||
        executionSettings.ActivationMode ==
        ActionActivationMode.Immediate;

    // =========================================================
    // LEGACY EXECUTION
    // =========================================================

    /// <summary>
    /// Befintlig execution lämnas intakt under migrationen.
    ///
    /// Den nya action-pipelinen kommer senare skapa ett
    /// ActionContext och exekvera effects genom en separat
    /// authoritative execution-väg.
    /// </summary>
    public virtual void Use(
        CharacterStats caster,
        CharacterStats target)
    {
        if (caster == null || target == null)
            return;

        if (requiresHitCheck)
        {
            if (!CombatResolver.RollHit(caster, target))
            {
                DamageResult missResult =
                    new DamageResult
                    {
                        isMiss = true
                    };

                target.TakeDamage(
                    missResult,
                    caster
                );

                return;
            }

            if (CombatResolver.RollDodge(caster, target))
            {
                DamageResult evadeResult =
                    new DamageResult
                    {
                        isEvaded = true
                    };

                target.TakeDamage(
                    evadeResult,
                    caster
                );

                return;
            }
        }

        if (entersCombatState)
        {
            CharacterStateController casterState =
                caster.GetComponent<
                    CharacterStateController
                >();

            casterState?.NotifyCombatActivity();

            CharacterStateController targetState =
                target.GetComponent<
                    CharacterStateController
                >();

            targetState?.NotifyCombatActivity();
        }

        if (effects == null)
            return;

        foreach (AbilityEffect effect in effects)
        {
            if (effect == null)
                continue;

            effect.Apply(
                caster,
                target
            );
        }
    }

    // =========================================================
    // TOOLTIP
    // =========================================================

    public virtual TooltipData GetTooltipData(
        CharacterStats caster)
    {
        TooltipData data =
            new TooltipData
            {
                title = abilityName,
                subtitle = types.ToString(),
                description = description
            };

        if (wardCost > 0)
        {
            data.stats.Add(
                $"<color=#7FD9FF>" +
                $"{wardCost} Wards" +
                $"</color>"
            );
        }

        if (effects != null)
        {
            foreach (AbilityEffect effect in effects)
            {
                if (effect == null)
                    continue;

                string text =
                    effect.GetTooltipText(caster);

                if (!string.IsNullOrEmpty(text))
                {
                    data.stats.Add(
                        $"<color=#FF5555>" +
                        $"{text}" +
                        $"</color>"
                    );
                }
            }
        }

        AddTimingTooltip(data);
        AddCooldownTooltip(data);

        return data;
    }

    private void AddTimingTooltip(
        TooltipData data)
    {
        if (!useActionSettings)
        {
            if (castTime > 0f)
            {
                data.stats.Add(
                    $"<color=white>" +
                    $"Cast Time: {castTime:0.0}s" +
                    $"</color>"
                );
            }

            return;
        }

        switch (timingSettings.TimingType)
        {
            case ActionTimingType.Instant:
                break;

            case ActionTimingType.Cast:
                if (timingSettings.CastDuration > 0f)
                {
                    data.stats.Add(
                        $"<color=white>" +
                        $"Cast Time: " +
                        $"{timingSettings.CastDuration:0.0}s" +
                        $"</color>"
                    );
                }

                break;

            case ActionTimingType.Channel:
                if (timingSettings.ChannelDuration > 0f)
                {
                    data.stats.Add(
                        $"<color=white>" +
                        $"Channel Time: " +
                        $"{timingSettings.ChannelDuration:0.0}s" +
                        $"</color>"
                    );
                }

                break;

            case ActionTimingType.Charge:
                if (timingSettings.MaximumChargeDuration > 0f)
                {
                    data.stats.Add(
                        $"<color=white>" +
                        $"Maximum Charge: " +
                        $"{timingSettings.MaximumChargeDuration:0.0}s" +
                        $"</color>"
                    );
                }

                break;
        }
    }

    private void AddCooldownTooltip(
        TooltipData data)
    {
        float displayedCooldown =
            EffectiveCooldown;

        if (displayedCooldown <= 0f)
            return;

        data.stats.Add(
            $"<color=white>" +
            $"Cooldown: {displayedCooldown:0.#}s" +
            $"</color>"
        );
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        cooldown =
            Mathf.Max(0f, cooldown);

        globalCooldown =
            Mathf.Max(0f, globalCooldown);

        castTime =
            Mathf.Max(0f, castTime);

        wardCost =
            Mathf.Max(0, wardCost);

        targetingSettings ??=
            new AbilityTargetingSettings();

        timingSettings ??=
            new AbilityTimingSettings();

        executionSettings ??=
            new AbilityExecutionSettings();
    }
#endif
}