using System;
using System.Collections.Generic;
using UnityEngine;

public sealed class ActionContext
{
    /// <summary>
    /// Unikt ID för just denna runtime-action.
    ///
    /// Två casts av samma AbilityData får olika ID.
    /// </summary>
    public Guid ActionId { get; }

    public CharacterStats Caster { get; }

    public AbilityData Ability { get; }

    /// <summary>
    /// Den ursprungliga request som startade actionen.
    /// </summary>
    public ActionRequest Request { get; }

    /// <summary>
    /// Det senaste auktoritativa targeting-resultatet.
    /// Under preview kan detta ersättas när musen flyttas.
    /// </summary>
    public TargetingResult Targeting { get; private set; }

    /// <summary>
    /// Actionens nuvarande runtimefas.
    /// </summary>
    public ActionPhase Phase { get; internal set; }

    /// <summary>
    /// Normaliserad timing-progress från 0 till 1.
    ///
    /// UI avgör själv om exempelvis en channel-bar
    /// ska visas som 1 till 0.
    /// </summary>
    public float NormalizedProgress { get; internal set; }

    /// <summary>
    /// Unity-tiden när actionen började.
    /// </summary>
    public float StartedAt { get; internal set; }

    /// <summary>
    /// Unity-tiden när execution faktiskt skedde.
    /// Är -1 tills actionen har exekverats.
    /// </summary>
    public float ExecutedAt { get; internal set; }

    /// <summary>
    /// Snapshot-värde för framtida charge-actions.
    /// Alltid mellan 0 och 1.
    /// </summary>
    public float ChargeProgress { get; internal set; }

    public GameObject PrimaryTarget =>
        Targeting?.PrimaryTarget;

    public Vector2 Origin =>
        Targeting != null
            ? Targeting.Origin
            : Caster != null
                ? Caster.transform.position
                : Vector2.zero;

    public Vector2 TargetPoint =>
        Targeting?.TargetPoint ?? Origin;

    public Vector2 AimDirection =>
        Targeting != null
            ? Targeting.Direction
            : Vector2.down;

    public IReadOnlyList<GameObject> AffectedTargets =>
        Targeting != null
            ? Targeting.AffectedTargets
            : EmptyTargets;

    public bool HasValidTargeting =>
        Targeting != null &&
        Targeting.IsValid;

    private static readonly IReadOnlyList<GameObject>
        EmptyTargets =
            Array.Empty<GameObject>();

    public ActionContext(
        ActionRequest request)
    {
        Request =
            request ??
            throw new ArgumentNullException(
                nameof(request)
            );

        ActionId = Guid.NewGuid();

        Caster = request.Caster;
        Ability = request.Ability;

        Phase = ActionPhase.Idle;

        NormalizedProgress = 0f;
        ChargeProgress = 0f;

        StartedAt = Time.time;
        ExecutedAt = -1f;
    }

    public void UpdateTargeting(
        TargetingResult targetingResult)
    {
        Targeting =
            targetingResult ??
            throw new ArgumentNullException(
                nameof(targetingResult)
            );
    }

    internal void MarkExecuted()
    {
        ExecutedAt = Time.time;
    }
}
