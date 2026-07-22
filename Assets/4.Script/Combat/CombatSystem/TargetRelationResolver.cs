using UnityEngine;

public static class TargetRelationResolver
{
    /// <summary>
    /// Beräknar den faktiska relationen från source till target.
    ///
    /// Murder mode behandlas inte här. Murder är ett undantag
    /// från targetingreglerna, inte en verklig relationsändring.
    /// </summary>
    public static TargetRelation Resolve(
        CharacterStats source,
        GameObject target)
    {
        if (source == null || target == null)
            return TargetRelation.None;

        CharacterStats targetStats =
            TargetUtility.GetCharacterStats(target);

        if (targetStats == null)
        {
            // Förstörbara och andra icke-karaktärsmål behandlas
            // tills vidare som neutrala.
            return TargetRelation.Neutral;
        }

        if (targetStats == source)
            return TargetRelation.Self;

        PlayerStats sourcePlayer =
            source as PlayerStats;

        if (sourcePlayer != null)
        {
            return ResolvePlayerToCharacter(
                sourcePlayer,
                targetStats
            );
        }

        PlayerStats targetPlayer =
            targetStats as PlayerStats;

        if (targetPlayer != null)
        {
            return ResolveCharacterToPlayer(
                source,
                targetPlayer
            );
        }

        return ResolveCharacterToCharacter(
            source,
            targetStats
        );
    }

    private static TargetRelation ResolvePlayerToCharacter(
        PlayerStats player,
        CharacterStats target)
    {
        if (target.IsHostileToPlayer(player))
            return TargetRelation.Hostile;

        if (target.faction != null &&
            FactionHostilitySystem.Instance != null &&
            FactionHostilitySystem.Instance
                .IsHostileToPlayer(
                    target.faction,
                    player
                ))
        {
            return TargetRelation.Hostile;
        }

        PlayerReputationManager reputation =
            player.GetComponent<PlayerReputationManager>();

        if (reputation != null &&
            target.faction != null)
        {
            ReputationState standing =
                reputation.GetReputationState(
                    target.faction
                );

            return ConvertStandingToRelation(
                standing
            );
        }

        if (player.faction != null &&
            player.faction == target.faction)
        {
            return TargetRelation.Friendly;
        }

        return ResolveFactionRelation(
            player,
            target
        );
    }

    private static TargetRelation ResolveCharacterToPlayer(
        CharacterStats source,
        PlayerStats player)
    {
        if (source.IsHostileToPlayer(player))
            return TargetRelation.Hostile;

        if (source.faction != null &&
            FactionHostilitySystem.Instance != null &&
            FactionHostilitySystem.Instance
                .IsHostileToPlayer(
                    source.faction,
                    player
                ))
        {
            return TargetRelation.Hostile;
        }

        if (source.faction != null &&
            source.faction == player.faction)
        {
            return TargetRelation.Friendly;
        }

        if (source.IsFriendlyTo(player))
            return TargetRelation.Friendly;

        if (source.IsHostileTo(player))
            return TargetRelation.Hostile;

        return TargetRelation.Neutral;
    }

    private static TargetRelation ResolveCharacterToCharacter(
        CharacterStats source,
        CharacterStats target)
    {
        if (source.faction != null &&
            source.faction == target.faction)
        {
            return TargetRelation.Friendly;
        }

        if (source.IsHostileTo(target))
            return TargetRelation.Hostile;

        if (source.IsFriendlyTo(target))
            return TargetRelation.Friendly;

        return TargetRelation.Neutral;
    }

    private static TargetRelation ResolveFactionRelation(
        CharacterStats source,
        CharacterStats target)
    {
        if (source.faction == null ||
            target.faction == null)
        {
            return TargetRelation.Neutral;
        }

        if (source.faction == target.faction)
            return TargetRelation.Friendly;

        ReputationState standing =
            source.faction.GetStanding(
                target.faction
            );

        return ConvertStandingToRelation(
            standing
        );
    }

    private static TargetRelation ConvertStandingToRelation(
        ReputationState standing)
    {
        switch (standing)
        {
            case ReputationState.Hated:
                return TargetRelation.Hostile;

            case ReputationState.Indifferent:
                return TargetRelation.Neutral;

            default:
                return TargetRelation.Friendly;
        }
    }
}
