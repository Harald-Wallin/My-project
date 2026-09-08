using UnityEngine;

/// <summary>
/// Generell interaction-option för enkla världsföremål.
///
/// Exempel:
/// - benrester
/// - gravstenar
/// - altaren
/// - böcker
/// - statyer
/// - mystiska objekt
///
/// Själva interaktionssystemet ansvarar för att skicka
/// InteractionEvents.InteractionCommitted innan Interact()
/// körs. Favour-systemets InteractObjective kan därför
/// reagera på objektets EntityIdentity utan att denna
/// komponent behöver känna till favours.
/// </summary>
[DisallowMultipleComponent]
public sealed class WorldInteractable :
    MonoBehaviour,
    IInteractionOption
{
    [Header("Interaction")]

    [SerializeField]
    [Tooltip(
        "Namnet på interaktionen. " +
        "Används senare om objektet har flera interaction-options.")]
    private string interactionName =
        "Inspect";

    [SerializeField]
    [Tooltip(
        "Om avstängd kan objektet inte interageras med.")]
    private bool interactionEnabled =
        true;

    public string InteractionName
    {
        get
        {
            if (!string.IsNullOrWhiteSpace(
                    interactionName))
            {
                return interactionName;
            }

            return "Interact";
        }
    }

    public bool CanInteract(
        in InteractionContext context)
    {
        return
            interactionEnabled &&
            context.IsValid;
    }

    public void Interact(
        in InteractionContext context)
    {
        /*
         * Avsiktligt tom.
         *
         * InteractionManager har redan validerat interaktionen
         * och InteractionEvents.InteractionCommitted används av
         * exempelvis InteractObjectiveRuntime.
         *
         * Den här komponentens uppgift är därför att göra ett
         * vanligt världsföremål till ett giltigt
         * IInteractionOption.
         */
    }

    public void SetInteractionEnabled(
        bool enabled)
    {
        interactionEnabled =
            enabled;
    }
}
