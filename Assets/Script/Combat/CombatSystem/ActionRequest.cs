using UnityEngine;

public sealed class ActionRequest
{
    /// <summary>
    /// Den karaktär eller entitet som försöker utföra actionen.
    /// </summary>
    public CharacterStats Caster { get; }

    /// <summary>
    /// Ability/action-data som ska aktiveras.
    /// Under migrationsperioden använder vi befintlig AbilityData.
    /// </summary>
    public AbilityData Ability { get; }

    /// <summary>
    /// Den råa position som input eller AI siktade mot.
    /// Positionen har ännu inte begränsats av range eller LoS.
    /// </summary>
    public Vector2 RequestedAimPoint { get; }

    /// <summary>
    /// Ett uttryckligen valt mål, om actionen använder ett sådant.
    /// Kan vara en NPC, spelaren, en tunna eller ett annat objekt.
    /// </summary>
    public GameObject ExplicitTarget { get; }

    /// <summary>
    /// En valfri föreslagen riktning från input eller AI.
    /// TargetingResolver får korrigera riktningen.
    /// </summary>
    public Vector2 RequestedDirection { get; }

    public bool HasExplicitTarget =>
        ExplicitTarget != null;

    public bool HasRequestedDirection =>
        RequestedDirection.sqrMagnitude > 0.0001f;

    public ActionRequest(
        CharacterStats caster,
        AbilityData ability,
        Vector2 requestedAimPoint,
        GameObject explicitTarget = null,
        Vector2 requestedDirection = default)
    {
        Caster = caster;
        Ability = ability;
        RequestedAimPoint = requestedAimPoint;
        ExplicitTarget = explicitTarget;

        RequestedDirection =
            requestedDirection.sqrMagnitude > 0.0001f
                ? requestedDirection.normalized
                : Vector2.zero;
    }
}