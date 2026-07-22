using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Gemensam hit- och dodge-resolution för actionsystemet.
///
/// Samma resolver används av:
/// - omedelbara melee- och spell-attacker
/// - projektiler
/// - framtida traps
/// - framtida delayed impacts
/// - framtida secondary effects
///
/// Ett target får aldrig göra fler än ett hit/dodge-slag per
/// faktisk träffhändelse.
/// </summary>
public static class AbilityHitResolver
{
    public static List<AbilityTargetHitResult>
        ResolveActionTargets(
            ActionExecutionContext action)
    {
        List<AbilityTargetHitResult> results =
            new();

        if (action == null)
            return results;

        IReadOnlyList<CharacterStats> targets =
            action.AffectedCharacters;

        bool shouldResolveImmediately =
            action.Ability.ExecutionSettings == null ||
            action.Ability
                .ExecutionSettings
                .ResolvesTargetHitImmediately;

        for (int i = 0;
             i < targets.Count;
             i++)
        {
            CharacterStats target =
                targets[i];

            if (target == null)
                continue;

            AbilityTargetHitResult result =
                ResolveTarget(
                    action,
                    target.gameObject,
                    target,
                    i,
                    shouldResolveImmediately
                );

            results.Add(result);
        }

        return results;
    }

    public static AbilityTargetHitResult ResolveTarget(
        ActionExecutionContext action,
        GameObject targetObject,
        CharacterStats target,
        int targetIndex = 0,
        bool resolveHitNow = true)
    {
        if (action == null ||
            target == null)
        {
            return null;
        }

        targetObject =
            targetObject != null
                ? TargetUtility.ResolveCharacterTarget(
                    targetObject
                )
                : target.gameObject;

        AbilityTargetHitOutcome outcome =
            ResolveOutcome(
                action,
                target,
                resolveHitNow
            );

        return new AbilityTargetHitResult(
            targetObject,
            target,
            targetIndex,
            outcome
        );
    }

    public static AbilityTargetHitOutcome ResolveOutcome(
        ActionExecutionContext action,
        CharacterStats target,
        bool resolveHitNow = true)
    {
        if (action == null ||
            action.Ability == null ||
            action.Caster == null ||
            target == null)
        {
            return AbilityTargetHitOutcome.Miss;
        }

        if (!resolveHitNow)
        {
            return AbilityTargetHitOutcome.NotRolled;
        }

        AbilityData ability =
            action.Ability;

        bool shouldRollHit =
            ability.requiresHitCheck &&
            ability.canMiss &&
            !ability.alwaysHits;

        if (!shouldRollHit)
        {
            return AbilityTargetHitOutcome.NotRolled;
        }

        if (!CombatResolver.RollHit(
                action.Caster,
                target))
        {
            return AbilityTargetHitOutcome.Miss;
        }

        if (CombatResolver.RollDodge(
                action.Caster,
                target))
        {
            return AbilityTargetHitOutcome.Evaded;
        }

        return AbilityTargetHitOutcome.Hit;
    }
}
