using System;
using System.Collections.Generic;

public sealed class InteractObjectiveRuntime :
    FavourObjectiveRuntime
{
    private readonly InteractObjectiveData
        interactData;

    private readonly HashSet<string>
        completedTargetIds =
            new(
                StringComparer.Ordinal
            );

    public InteractObjectiveRuntime(
        InteractObjectiveData data,
        FavourRuntime favour)
        : base(
            data,
            favour)
    {
        interactData =
            data;
    }

    public override bool IsComplete =>
        RequiredProgress > 0 &&
        CurrentProgress >=
        RequiredProgress;

    public override int CurrentProgress =>
        completedTargetIds.Count;

    public override int RequiredProgress =>
        interactData != null
            ? interactData
                .RequiredInteractions
            : 0;

    protected override void OnActivated()
    {
        InteractionEvents
            .InteractionCommitted +=
            HandleInteractionCommitted;
    }

    protected override void OnDeactivated()
    {
        InteractionEvents
            .InteractionCommitted -=
            HandleInteractionCommitted;
    }

    public bool RequiresTarget(
        string entityId)
    {
        return interactData != null &&
               interactData.ContainsTarget(
                   entityId
               );
    }

    private void HandleInteractionCommitted(
        InteractionContext context)
    {
        if (!IsActive ||
            IsComplete ||
            interactData == null ||
            context.Target == null)
        {
            return;
        }

        EntityIdentity identity =
            context.Target.Identity;

        if (identity == null ||
            string.IsNullOrWhiteSpace(
                identity.Id))
        {
            return;
        }

        if (!interactData.ContainsTarget(
                identity.Id))
        {
            return;
        }

        if (!completedTargetIds.Add(
                identity.Id))
        {
            return;
        }

        RaiseProgressChanged();
    }

    public override void ResetProgress()
    {
        if (completedTargetIds.Count == 0)
            return;

        completedTargetIds.Clear();

        RaiseProgressChanged();
    }
}