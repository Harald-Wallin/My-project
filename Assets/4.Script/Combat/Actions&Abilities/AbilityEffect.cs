using UnityEngine;

/// <summary>
/// Basklass för alla modulära ability-effekter.
///
/// Effekten beskriver både:
/// - när den ska exekveras
/// - vilket target den ska använda
/// - hur den skapar sin runtime-effekt
/// </summary>
public abstract class AbilityEffect :
    ScriptableObject
{
    [Header("Presentation")]

    public Sprite icon;

    [Header("Execution")]

    [SerializeField]
    private AbilityEffectExecutionTiming executionTiming =
        AbilityEffectExecutionTiming.Immediate;

    [SerializeField]
    private AbilityEffectTargetMode targetMode =
        AbilityEffectTargetMode.EachAffectedTarget;

    [SerializeField]
    [Tooltip(
        "Om pipelinen ska hoppa över effekten när inget " +
        "CharacterStats-target finns."
    )]
    private bool requiresCharacterTarget = true;

    [Header("Buff Rules")]

    public bool stackable;

    [Min(1)]
    public int maxStacks = 1;

    public bool refreshDurationOnStack = true;

    public bool removeOnDeath = true;

    public AbilityEffectExecutionTiming ExecutionTiming =>
        executionTiming;

    public AbilityEffectTargetMode TargetMode =>
        targetMode;

    public bool RequiresCharacterTarget =>
        requiresCharacterTarget;

    /// <summary>
    /// Ny context-baserad execution-ingång.
    /// </summary>
    public virtual void Execute(
        AbilityEffectExecutionContext context)
    {
        if (context == null)
            return;

        if (RequiresCharacterTarget &&
            context.Target == null)
        {
            return;
        }

        Apply(
            context.Caster,
            context.Target
        );
    }

    /// <summary>
    /// Legacy-ingång för ännu inte migrerade effekter.
    /// </summary>
    public virtual void Apply(
        CharacterStats caster,
        CharacterStats target)
    {
    }

    /// <summary>
    /// Factory för runtime-buffar.
    /// </summary>
    public virtual ActiveBuff CreateActiveBuff(
        CharacterStats source,
        CharacterStats target)
    {
        return null;
    }

    public virtual string GetTooltipText(
        CharacterStats caster)
    {
        return string.Empty;
    }

#if UNITY_EDITOR
    protected virtual void OnValidate()
    {
        maxStacks =
            Mathf.Max(
                1,
                maxStacks
            );
    }
#endif
}