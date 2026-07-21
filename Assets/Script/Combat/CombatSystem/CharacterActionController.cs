using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Auktoritativ runtime-controller för det nya actionsystemet.
///
/// Ansvar:
/// - starta actions
/// - hantera targeting-preview
/// - bekräfta eller avbryta
/// - hantera cast och recovery
/// - betala kostnader
/// - starta cooldowns
/// - exekvera den migrerade abilityn
///
/// Själva targetinglogiken delegeras till TargetResolver.
/// Själva effect-execution använder tillfälligt AbilityData.Use().
/// </summary>
[RequireComponent(typeof(CharacterStats))]
public sealed class CharacterActionController :
    MonoBehaviour
{
    [Header("Cooldown")]

    [SerializeField]
    [Min(0f)]
    private float defaultGlobalCooldown = 0.8f;

    [Header("Targeting")]

    [SerializeField]
    [Min(1)]
    private int targetingBufferSize = 128;

    private readonly Dictionary<AbilityData, float>
        cooldownTimers =
            new();

    private CharacterStats stats;
    private CharacterStateController stateController;
    private WardSystem wardSystem;

    private TargetResolver targetResolver;

    private ActionContext currentContext;
    private ActionRequest currentRequest;

    private float globalCooldownTimer;
    private float phaseTimer;
    private float phaseDuration;

    public ActionContext CurrentContext =>
        currentContext;

    public ActionPhase CurrentPhase =>
        currentContext != null
            ? currentContext.Phase
            : ActionPhase.Idle;

    public bool HasActiveAction =>
        currentContext != null;

    public bool IsPreviewing =>
        CurrentPhase == ActionPhase.Preview;

    public bool IsCasting =>
        CurrentPhase == ActionPhase.Casting;

    public bool IsExecuting =>
        CurrentPhase == ActionPhase.Executing;

    public bool IsRecovering =>
        CurrentPhase == ActionPhase.Recovery;

    public event Action<ActionContext>
        OnActionStarted;

    public event Action<ActionContext>
        OnPreviewStarted;

    public event Action<ActionContext>
        OnTargetingUpdated;

    public event Action<ActionContext, ActionPhase>
        OnPhaseChanged;

    public event Action<ActionContext>
        OnActionExecuted;

    public event Action<ActionContext>
        OnActionCancelled;

    public event Action<ActionContext>
        OnActionCompleted;

    private void Awake()
    {
        stats =
            GetComponent<CharacterStats>();

        stateController =
            GetComponent<CharacterStateController>();

        wardSystem =
            GetComponent<WardSystem>();

        CreateTargetResolver();
    }

    private void OnValidate()
    {
        defaultGlobalCooldown =
            Mathf.Max(
                0f,
                defaultGlobalCooldown
            );

        targetingBufferSize =
            Mathf.Max(
                1,
                targetingBufferSize
            );
    }

    private void Update()
    {
        UpdateCooldowns();
        UpdateActiveAction();
    }

    private void CreateTargetResolver()
    {
        targetResolver =
            new TargetResolver(
                targetingBufferSize
            );
    }

    // =========================================================
    // ACTION ACTIVATION
    // =========================================================

    /// <summary>
    /// Försöker starta en migrerad ability.
    ///
    /// För Confirmed-actions startas Preview.
    /// För Immediate-actions går actionen direkt vidare till
    /// timing/execution om targetingen är giltig.
    /// </summary>
    public bool TryStartAction(
        AbilityData ability,
        Vector2 requestedAimPoint,
        GameObject explicitTarget = null,
        Vector2 requestedDirection = default)
    {
        if (!CanStartAction(ability))
            return false;

        ActionRequest request =
            new ActionRequest(
                stats,
                ability,
                requestedAimPoint,
                explicitTarget,
                requestedDirection
            );

        TargetingResult targeting =
            targetResolver.Resolve(request);

        ActionContext context =
            new ActionContext(request);

        context.UpdateTargeting(targeting);

        currentRequest = request;
        currentContext = context;

        OnActionStarted?.Invoke(
            currentContext
        );

        if (ability.RequiresConfirmation)
        {
            SetPhase(
                ActionPhase.Preview
            );

            OnPreviewStarted?.Invoke(
                currentContext
            );

            return true;
        }

        if (!targeting.IsValid)
        {
            FailCurrentAction();
            return false;
        }

        BeginTimingOrExecution();

        return true;
    }

    /// <summary>
    /// Bekvämlighetsmetod för actions som riktas mot ett
    /// uttryckligt target.
    /// </summary>
    public bool TryStartAction(
        AbilityData ability,
        CharacterStats explicitTarget)
    {
        GameObject targetObject =
            explicitTarget != null
                ? explicitTarget.gameObject
                : null;

        Vector2 aimPoint =
            explicitTarget != null
                ? TargetUtility.GetTargetPosition(
                    explicitTarget.gameObject
                )
                : (Vector2)transform.position;

        return TryStartAction(
            ability,
            aimPoint,
            targetObject
        );
    }

    /// <summary>
    /// Bekvämlighetsmetod för self-cast eller abilities som inte
    /// behöver extern aim-data.
    /// </summary>
    public bool TryStartAction(
        AbilityData ability)
    {
        return TryStartAction(
            ability,
            transform.position,
            null,
            Vector2.down
        );
    }

    private bool CanStartAction(
        AbilityData ability)
    {
        if (ability == null)
            return false;

        if (!ability.UsesActionSettings)
            return false;

        if (HasActiveAction)
            return false;

        if (stats == null ||
            !stats.CanAct())
        {
            return false;
        }

        if (stateController != null &&
            !stateController.CanUseAbilities)
        {
            return false;
        }

        if (globalCooldownTimer > 0f)
        {
            ShowAbilityOnCooldown();
            return false;
        }

        if (cooldownTimers.ContainsKey(
                ability))
        {
            ShowAbilityOnCooldown();
            return false;
        }

        AbilityTimingSettings timing =
            ability.TimingSettings;

        if (timing == null)
            return false;

        switch (timing.TimingType)
        {
            case ActionTimingType.Instant:
            case ActionTimingType.Cast:
                return true;

            case ActionTimingType.Channel:
            case ActionTimingType.Charge:
                Debug.LogWarning(
                    $"Ability '{ability.abilityName}' använder " +
                    $"{timing.TimingType}, vilket ännu inte stöds " +
                    $"av CharacterActionController.",
                    this
                );

                return false;

            default:
                return false;
        }
    }

    // =========================================================
    // PREVIEW
    // =========================================================

    /// <summary>
    /// Uppdaterar den aktiva previewns aim och targetingresultat.
    ///
    /// ActionContext behåller den ursprungliga activation-requesten,
    /// medan currentRequest representerar previewns senaste input.
    /// </summary>
    public bool UpdatePreview(
        Vector2 requestedAimPoint,
        GameObject explicitTarget = null,
        Vector2 requestedDirection = default)
    {
        if (!IsPreviewing ||
            currentContext == null ||
            currentContext.Ability == null)
        {
            return false;
        }

        ActionRequest updatedRequest =
            new ActionRequest(
                stats,
                currentContext.Ability,
                requestedAimPoint,
                explicitTarget,
                requestedDirection
            );

        TargetingResult targeting =
            targetResolver.Resolve(
                updatedRequest
            );

        currentRequest =
            updatedRequest;

        currentContext.UpdateTargeting(
            targeting
        );

        OnTargetingUpdated?.Invoke(
            currentContext
        );

        return targeting.IsValid;
    }

    /// <summary>
    /// Bekräftar den aktiva targeting-previewn.
    ///
    /// Targetingen resolve:as en sista gång så att execution inte
    /// använder ett gammalt previewresultat.
    /// </summary>
    public bool ConfirmCurrentAction()
    {
        if (!IsPreviewing ||
            currentContext == null ||
            currentRequest == null)
        {
            return false;
        }

        TargetingResult targeting =
            targetResolver.Resolve(
                currentRequest
            );

        currentContext.UpdateTargeting(
            targeting
        );

        OnTargetingUpdated?.Invoke(
            currentContext
        );

        if (!targeting.IsValid)
            return false;

        BeginTimingOrExecution();

        return true;
    }

    // =========================================================
    // TIMING
    // =========================================================

    private void BeginTimingOrExecution()
    {
        if (currentContext == null ||
            currentContext.Ability == null)
        {
            FailCurrentAction();
            return;
        }

        AbilityTimingSettings timing =
            currentContext.Ability.TimingSettings;

        switch (timing.TimingType)
        {
            case ActionTimingType.Instant:
                ExecuteCurrentAction();
                break;

            case ActionTimingType.Cast:
                BeginCast(
                    timing.CastDuration
                );
                break;

            case ActionTimingType.Channel:
            case ActionTimingType.Charge:
                Debug.LogWarning(
                    $"Timingtypen {timing.TimingType} stöds " +
                    $"ännu inte.",
                    this
                );

                FailCurrentAction();
                break;
        }
    }

    private void BeginCast(
        float duration)
    {
        float safeDuration =
            Mathf.Max(
                0f,
                duration
            );

        if (safeDuration <= 0f)
        {
            ExecuteCurrentAction();
            return;
        }

        phaseTimer = 0f;
        phaseDuration = safeDuration;

        currentContext.NormalizedProgress = 0f;

        SetPhase(
            ActionPhase.Casting
        );
    }

    private void UpdateActiveAction()
    {
        if (currentContext == null)
            return;

        switch (currentContext.Phase)
        {
            case ActionPhase.Casting:
                UpdateCasting();
                break;

            case ActionPhase.Recovery:
                UpdateRecovery();
                break;
        }
    }

    private void UpdateCasting()
    {
        if (!CanContinueActiveAction())
        {
            InterruptCurrentAction();
            return;
        }

        phaseTimer += Time.deltaTime;

        currentContext.NormalizedProgress =
            GetNormalizedPhaseProgress();

        if (phaseTimer < phaseDuration)
            return;

        currentContext.NormalizedProgress = 1f;

        ExecuteCurrentAction();
    }

    private void UpdateRecovery()
    {
        phaseTimer += Time.deltaTime;

        currentContext.NormalizedProgress =
            GetNormalizedPhaseProgress();

        if (phaseTimer < phaseDuration)
            return;

        currentContext.NormalizedProgress = 1f;

        CompleteCurrentAction();
    }

    private float GetNormalizedPhaseProgress()
    {
        if (phaseDuration <= 0f)
            return 1f;

        return Mathf.Clamp01(
            phaseTimer / phaseDuration
        );
    }

    private bool CanContinueActiveAction()
    {
        if (stats == null ||
            !stats.CanAct())
        {
            return false;
        }

        if (stateController != null &&
            !stateController.CanUseAbilities)
        {
            return false;
        }

        return true;
    }

    // =========================================================
    // EXECUTION
    // =========================================================

    private void ExecuteCurrentAction()
    {
        if (currentContext == null ||
            currentRequest == null)
        {
            FailCurrentAction();
            return;
        }

        TargetingResult finalTargeting =
            targetResolver.Resolve(
                currentRequest
            );

        currentContext.UpdateTargeting(
            finalTargeting
        );

        OnTargetingUpdated?.Invoke(
            currentContext
        );

        if (!finalTargeting.IsValid)
        {
            FailCurrentAction();
            return;
        }

        if (!TryPayWardCost(
                currentContext.Ability))
        {
            FailCurrentAction();
            return;
        }

        SetPhase(
            ActionPhase.Executing
        );

        currentContext.NormalizedProgress = 1f;

        ExecuteLegacyEffects(
            currentContext
        );

        currentContext.MarkExecuted();

        StartCooldowns(
            currentContext.Ability
        );

        OnActionExecuted?.Invoke(
            currentContext
        );

        BeginRecovery();
    }

    /// <summary>
    /// Tillfällig execution-brygga.
    ///
    /// Varje påverkat CharacterStats-target skickas genom den
    /// befintliga AbilityData.Use()-vägen.
    ///
    /// När det nya effectsystemet byggs ersätts endast denna metod.
    /// Resten av actionlivscykeln kan ligga kvar.
    /// </summary>
    private static void ExecuteLegacyEffects(
        ActionContext context)
    {
        if (context == null ||
            context.Ability == null ||
            context.Caster == null)
        {
            return;
        }

        IReadOnlyList<GameObject> targets =
            context.AffectedTargets;

        for (int i = 0;
             i < targets.Count;
             i++)
        {
            GameObject targetObject =
                targets[i];

            CharacterStats targetStats =
                TargetUtility.GetCharacterStats(
                    targetObject
                );

            if (targetStats == null)
                continue;

            context.Ability.Use(
                context.Caster,
                targetStats
            );
        }
    }

    private bool TryPayWardCost(
        AbilityData ability)
    {
        if (ability == null)
            return false;

        if (ability.wardCost <= 0)
            return true;

        if (wardSystem == null)
        {
            wardSystem =
                GetComponent<WardSystem>();
        }

        if (wardSystem == null)
            return false;

        if (wardSystem.TrySpendWard(
                ability.wardCost))
        {
            return true;
        }

        NotificationSpawner.Instance?.Show(
            NotificationSpawner
                .Instance
                .Database
                .notEnoughWard
        );

        return false;
    }

    // =========================================================
    // RECOVERY
    // =========================================================

    private void BeginRecovery()
    {
        if (currentContext == null ||
            currentContext.Ability == null)
        {
            CompleteCurrentAction();
            return;
        }

        float duration =
            currentContext
                .Ability
                .TimingSettings
                .RecoveryDuration;

        if (duration <= 0f)
        {
            CompleteCurrentAction();
            return;
        }

        phaseTimer = 0f;
        phaseDuration = duration;

        currentContext.NormalizedProgress = 0f;

        SetPhase(
            ActionPhase.Recovery
        );
    }

    // =========================================================
    // CANCELLATION
    // =========================================================

    public bool CancelCurrentAction()
    {
        if (currentContext == null)
            return false;

        if (!CanCancelCurrentPhase())
            return false;

        ActionContext cancelledContext =
            currentContext;

        SetPhase(
            ActionPhase.Idle
        );

        ClearCurrentAction();

        OnActionCancelled?.Invoke(
            cancelledContext
        );

        return true;
    }

    private void InterruptCurrentAction()
    {
        if (currentContext == null)
            return;

        AbilityExecutionSettings execution =
            currentContext
                .Ability
                ?.ExecutionSettings;

        if (execution != null &&
            !execution.CanBeInterrupted)
        {
            return;
        }

        ActionContext cancelledContext =
            currentContext;

        SetPhase(
            ActionPhase.Idle
        );

        ClearCurrentAction();

        OnActionCancelled?.Invoke(
            cancelledContext
        );
    }

    private bool CanCancelCurrentPhase()
    {
        if (currentContext == null)
            return false;

        switch (currentContext.Phase)
        {
            case ActionPhase.Preview:
                return true;

            case ActionPhase.Casting:
                AbilityExecutionSettings execution =
                    currentContext
                        .Ability
                        ?.ExecutionSettings;

                return execution == null ||
                       execution.CanBeCancelled;

            case ActionPhase.Executing:
            case ActionPhase.Recovery:
            case ActionPhase.Idle:
                return false;

            default:
                return false;
        }
    }

    private void FailCurrentAction()
    {
        if (currentContext == null)
            return;

        ActionContext failedContext =
            currentContext;

        if (currentContext.Phase !=
            ActionPhase.Idle)
        {
            SetPhase(
                ActionPhase.Idle
            );
        }

        ClearCurrentAction();

        OnActionCancelled?.Invoke(
            failedContext
        );
    }

    // =========================================================
    // COMPLETION
    // =========================================================

    private void CompleteCurrentAction()
    {
        if (currentContext == null)
            return;

        ActionContext completedContext =
            currentContext;

        SetPhase(
            ActionPhase.Idle
        );

        ClearCurrentAction();

        OnActionCompleted?.Invoke(
            completedContext
        );
    }

    private void ClearCurrentAction()
    {
        currentContext = null;
        currentRequest = null;

        phaseTimer = 0f;
        phaseDuration = 0f;
    }

    private void SetPhase(
        ActionPhase phase)
    {
        if (currentContext == null)
            return;

        if (currentContext.Phase == phase)
            return;

        currentContext.Phase = phase;

        OnPhaseChanged?.Invoke(
            currentContext,
            phase
        );
    }

    // =========================================================
    // COOLDOWNS
    // =========================================================

    private void UpdateCooldowns()
    {
        if (globalCooldownTimer > 0f)
        {
            globalCooldownTimer =
                Mathf.Max(
                    0f,
                    globalCooldownTimer -
                    Time.deltaTime
                );
        }

        if (cooldownTimers.Count == 0)
            return;

        AbilityData[] abilities =
            new AbilityData[
                cooldownTimers.Count
            ];

        cooldownTimers.Keys.CopyTo(
            abilities,
            0
        );

        for (int i = 0;
             i < abilities.Length;
             i++)
        {
            AbilityData ability =
                abilities[i];

            float remaining =
                cooldownTimers[ability] -
                Time.deltaTime;

            if (remaining <= 0f)
            {
                cooldownTimers.Remove(
                    ability
                );

                continue;
            }

            cooldownTimers[ability] =
                remaining;
        }
    }

    private void StartCooldowns(
        AbilityData ability)
    {
        if (ability == null)
            return;

        float cooldown =
            ability.EffectiveCooldown;

        if (cooldown > 0f)
        {
            cooldownTimers[ability] =
                cooldown;
        }

        AbilityExecutionSettings execution =
            ability.ExecutionSettings;

        if (execution == null ||
            !execution.TriggersGlobalCooldown)
        {
            return;
        }

        float globalCooldown =
            execution.UsesGlobalCooldownOverride
                ? execution.GlobalCooldownOverride
                : defaultGlobalCooldown;

        if (globalCooldown <= 0f)
            return;

        globalCooldownTimer =
            globalCooldown;
    }

    public float GetCooldownRemaining(
        AbilityData ability)
    {
        if (ability == null)
            return 0f;

        float abilityCooldown = 0f;

        if (cooldownTimers.TryGetValue(
                ability,
                out float remaining))
        {
            abilityCooldown =
                Mathf.Max(
                    0f,
                    remaining
                );
        }

        if (abilityCooldown > 0f)
            return abilityCooldown;

        return Mathf.Max(
            0f,
            globalCooldownTimer
        );
    }

    public float GetMaxCooldown(
        AbilityData ability)
    {
        if (ability == null)
            return 0f;

        if (cooldownTimers.ContainsKey(
                ability))
        {
            return ability.EffectiveCooldown;
        }

        AbilityExecutionSettings execution =
            ability.ExecutionSettings;

        if (execution != null &&
            execution.TriggersGlobalCooldown)
        {
            return
                execution.UsesGlobalCooldownOverride
                    ? execution.GlobalCooldownOverride
                    : defaultGlobalCooldown;
        }

        return 0f;
    }

    private static void ShowAbilityOnCooldown()
    {
        NotificationSpawner.Instance?.Show(
            NotificationSpawner
                .Instance
                .Database
                .abilityOnCooldown
        );
    }
}
