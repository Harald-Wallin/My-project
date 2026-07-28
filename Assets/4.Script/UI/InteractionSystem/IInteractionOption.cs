/// <summary>
/// Implementeras av komponenter som erbjuder en interaktion
/// genom ett InteractionTarget.
///
/// Exempel:
/// Vendor, FavourGiver, ReputationDonationNPC,
/// LootContainer och Harvestable.
/// </summary>
public interface IInteractionOption
{
    /// <summary>
    /// Texten som visas i interaktionsmenyn.
    /// </summary>
    string InteractionName { get; }

    /// <summary>
    /// Avgör om alternativet för tillfället är tillgängligt.
    /// </summary>
    bool CanInteract(
        in InteractionContext context);

    /// <summary>
    /// Utför interaktionen.
    /// </summary>
    void Interact(
        in InteractionContext context);
}