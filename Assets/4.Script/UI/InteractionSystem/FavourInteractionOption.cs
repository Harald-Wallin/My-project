/// <summary>
/// Ett konkret interaction-alternativ för exakt en
/// FavourRuntime hos en specifik FavourGiver.
///
/// Objektet är endast ett tunt adapterlager mellan det
/// generella interaction-systemet och befintlig Favour-logik.
/// </summary>
public sealed class FavourInteractionOption :
    IInteractionOption
{
    private readonly FavourGiver giver;
    private readonly FavourRuntime runtime;

    public FavourInteractionOption(
        FavourGiver giver,
        FavourRuntime runtime)
    {
        this.giver =
            giver;

        this.runtime =
            runtime;
    }

    public InteractionCategory Category =>
        InteractionCategory.Favour;

    public InteractionPresentation
        GetPresentation()
    {
        string title =
            runtime?.Data != null
                ? runtime.Data.DisplayName
                : "Favour";

        return new InteractionPresentation(
            Category,
            title,
            GetStatusText()
        );
    }

    public bool CanInteract(
        in InteractionContext context)
    {
        if (!context.IsValid ||
            giver == null ||
            runtime == null ||
            runtime.Data == null)
        {
            return false;
        }

        /*
         * FavourGiver är fortsatt auktoritativ för
         * huruvida just denna runtime ska visas här.
         *
         * På så vis duplicerar vi inte regler för
         * giver, completion target, delivery,
         * escort eller annan Favour-logik.
         */
        return giver.TryGetVisibleRuntime(
            runtime.Data,
            out FavourRuntime visibleRuntime
        ) &&
        visibleRuntime == runtime;
    }

    public void Interact(
        in InteractionContext context)
    {
        if (!CanInteract(context))
            return;

        giver.OpenFavour(
            runtime,
            context.Target
        );
    }

    private string GetStatusText()
    {
        if (runtime == null)
            return string.Empty;

        switch (runtime.State)
        {
            case FavourState.Available:
                return "Available";

            case FavourState.Active:
                return "Active";

            case FavourState.ReadyToTurnIn:
                return "Ready to turn in";

            case FavourState.Completed:
                return "Completed";

            case FavourState.Failed:
                return "Failed";

            case FavourState.Cooldown:
                return "Retry";

            default:
                return string.Empty;
        }
    }
}
