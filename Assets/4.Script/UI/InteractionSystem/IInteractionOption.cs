/// <summary>
/// Representerar ett konkret interaktionsalternativ
/// som spelaren kan välja genom ett InteractionTarget.
/// </summary>
public interface IInteractionOption
{
    /// <summary>
    /// Den semantiska typen av interaktion.
    /// Används av presentationslagret och UI:t.
    /// </summary>
    InteractionCategory Category
    {
        get;
    }

    /// <summary>
    /// Skapar presentationen som visas för spelaren.
    ///
    /// Text kan vara statisk, automatiskt genererad
    /// eller hämtad från underliggande gameplay-data.
    /// Status får förändras medan ett valfönster är öppet.
    /// </summary>
    InteractionPresentation GetPresentation();

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