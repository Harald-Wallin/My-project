using System;
using UnityEngine;

public sealed class KillObjectiveRuntime :
    FavourObjectiveRuntime
{
    private readonly KillObjectiveData
        killData;

    private int currentKills;

    public KillObjectiveRuntime(
        KillObjectiveData data,
        FavourRuntime favour)
        : base(
            data,
            favour)
    {
        killData =
            data;
    }

    public override bool IsComplete =>
        currentKills >=
        RequiredProgress;

    public override int CurrentProgress =>
        currentKills;

    public override int RequiredProgress =>
        killData != null
            ? killData.RequiredKills
            : 1;

    protected override void OnCharacterDefeated(
        CharacterDefeatedResult result)
    {
        if (killData == null ||
            killData.Creature == null)
        {
            return;
        }

        if (!IsMatchingCreature(
                result.Creature,
                killData.Creature))
        {
            return;
        }

        PlayerStats player =
            Favour?.Manager?.Player;

        if (player == null)
            return;

        if (!result.HasMinimumDamageShare(
                player,
                killData.MinimumDamageShare))
        {
            return;
        }

        currentKills =
            Mathf.Min(
                currentKills + 1,
                RequiredProgress
            );

        RaiseProgressChanged();
    }

    private static bool IsMatchingCreature(
        CreatureDefinition defeated,
        CreatureDefinition required)
    {
        if (defeated == null ||
            required == null)
        {
            return false;
        }

        if (defeated == required)
            return true;

        if (string.IsNullOrWhiteSpace(
                defeated.Id) ||
            string.IsNullOrWhiteSpace(
                required.Id))
        {
            return false;
        }

        return string.Equals(
            defeated.Id,
            required.Id,
            StringComparison.Ordinal
        );
    }

    public override void ResetProgress()
    {
        currentKills = 0;

        RaiseProgressChanged();
    }
}
