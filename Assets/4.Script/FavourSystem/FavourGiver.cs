using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Äger en uppsättning favours och exponerar dem genom det
/// gemensamma interaktionssystemet.
/// </summary>
public sealed class FavourGiver :
    MonoBehaviour,
    IInteractionOption
{
    [Header("Identity")]

    [SerializeField]
    [Tooltip(
        "Valfritt presentationsnamn. Om tomt används " +
        "CharacterStats.DisplayName eller GameObject-namnet.")]
    private string giverName;

    [Header("Favours")]

    [SerializeField]
    private List<FavourData> favours =
        new();

    [Header("Interaction")]

    [SerializeField]
    [Tooltip(
        "Registrerar ExplicitAccept-favours när spelaren " +
        "interagerar med objektet.")]
    private bool registerOnInteraction = true;

    /// <summary>
    /// Texten som senare kan visas i ett valfönster när ett
    /// objekt erbjuder flera interaktioner.
    /// </summary>
    public string InteractionName =>
        "Favours";

    public string GiverName
    {
        get
        {
            if (!string.IsNullOrWhiteSpace(
                    giverName))
            {
                return giverName;
            }

            CharacterStats character =
                GetComponent<CharacterStats>();

            if (character != null)
            {
                return character.DisplayName;
            }

            return gameObject.name;
        }
    }

    public IReadOnlyList<FavourData> Favours =>
        favours;

    private void Start()
    {
        RegisterBackgroundFavours();
    }

    // =========================================================
    // INTERACTION
    // =========================================================

    /// <summary>
    /// Kontrollerar om favour-givaren för närvarande kan användas.
    ///
    /// Vi kontrollerar konfigurerade favours i stället för endast
    /// redan synliga runtimes, eftersom ExplicitAccept- och
    /// DiscoverOnInteraction-favours kan registreras först när
    /// interaktionen sker.
    /// </summary>
    public bool CanInteract(
        in InteractionContext context)
    {
        if (!context.IsValid)
            return false;

        if (context.Target == null)
            return false;

        if (PlayerFavourManager.Instance == null)
            return false;

        return HasConfiguredFavour();
    }

    /// <summary>
    /// Registrerar eller upptäcker relevanta favours och öppnar
    /// sedan favour-fönstret.
    /// </summary>
    public void Interact(
        in InteractionContext context)
    {
        if (!CanInteract(context))
            return;

        PlayerFavourManager manager =
            PlayerFavourManager.Instance;

        if (manager == null)
        {
            Debug.LogWarning(
                $"'{name}' försökte öppna favours, men spelaren " +
                "saknar PlayerFavourManager.",
                this);

            return;
        }

        RegisterInteractionFavours(
            manager);

        FavourWindow window =
            FavourWindow.Instance;

        if (window == null)
        {
            Debug.LogWarning(
                $"'{name}' försökte öppna FavourWindow, men " +
                "inget aktivt FavourWindow hittades.",
                this);

            return;
        }

        window.Open(
            this,
            context.Target);
    }

    // =========================================================
    // REGISTRATION
    // =========================================================

    /// <summary>
    /// Registrerar favours som ska följas redan innan spelaren
    /// har upptäckt eller interagerat med givaren.
    /// </summary>
    private void RegisterBackgroundFavours()
    {
        PlayerFavourManager manager =
            PlayerFavourManager.Instance;

        if (manager == null)
            return;

        foreach (FavourData favour in favours)
        {
            if (favour == null)
                continue;

            if (favour.ActivationPolicy !=
                FavourActivationPolicy.TrackBeforeDiscovery)
            {
                continue;
            }

            FavourRuntime runtime =
                manager.RegisterFavour(
                    favour);

            if (runtime != null &&
                runtime.State ==
                FavourState.Available)
            {
                runtime.TryActivate();
            }
        }
    }

    /// <summary>
    /// Applicerar varje favours aktiveringspolicy när spelaren
    /// interagerar med givaren.
    /// </summary>
    private void RegisterInteractionFavours(
        PlayerFavourManager manager)
    {
        if (manager == null)
            return;

        foreach (FavourData favour in favours)
        {
            if (favour == null)
                continue;

            switch (favour.ActivationPolicy)
            {
                case FavourActivationPolicy.ExplicitAccept:
                    RegisterExplicitAcceptFavour(
                        manager,
                        favour);

                    break;

                case FavourActivationPolicy.DiscoverOnInteraction:
                    RegisterDiscoveredFavour(
                        manager,
                        favour);

                    break;

                case FavourActivationPolicy.TrackBeforeDiscovery:
                    manager.RegisterFavour(
                        favour);

                    break;
            }
        }
    }

    private void RegisterExplicitAcceptFavour(
        PlayerFavourManager manager,
        FavourData favour)
    {
        if (!registerOnInteraction)
            return;

        manager.RegisterFavour(
            favour);
    }

    private static void RegisterDiscoveredFavour(
        PlayerFavourManager manager,
        FavourData favour)
    {
        FavourRuntime runtime =
            manager.RegisterFavour(
                favour);

        if (runtime != null &&
            runtime.State ==
            FavourState.Available)
        {
            runtime.TryActivate();
        }
    }

    // =========================================================
    // FAVOUR ACCESS
    // =========================================================

    public bool TryAccept(
        FavourData favour)
    {
        if (!ContainsFavour(
                favour))
        {
            return false;
        }

        PlayerFavourManager manager =
            PlayerFavourManager.Instance;

        return manager != null &&
               manager.TryAccept(
                   favour);
    }

    public bool TryTurnIn(
        FavourData favour)
    {
        if (!ContainsFavour(
                favour))
        {
            return false;
        }

        PlayerFavourManager manager =
            PlayerFavourManager.Instance;

        return manager != null &&
               manager.TryTurnIn(
                   favour);
    }

    public List<FavourRuntime> GetVisibleFavours()
    {
        List<FavourRuntime> result =
            new();

        PlayerFavourManager manager =
            PlayerFavourManager.Instance;

        if (manager == null)
            return result;

        foreach (FavourData favour in favours)
        {
            if (favour == null)
                continue;

            if (!manager.TryGetRuntime(
                    favour,
                    out FavourRuntime runtime))
            {
                continue;
            }

            if (runtime == null ||
                runtime.State ==
                FavourState.Unavailable)
            {
                continue;
            }

            result.Add(
                runtime);
        }

        return result;
    }

    public bool TryGetVisibleRuntime(
        FavourData favour,
        out FavourRuntime runtime)
    {
        runtime = null;

        if (!ContainsFavour(
                favour))
        {
            return false;
        }

        PlayerFavourManager manager =
            PlayerFavourManager.Instance;

        if (manager == null ||
            !manager.TryGetRuntime(
                favour,
                out runtime))
        {
            runtime = null;
            return false;
        }

        if (runtime == null ||
            runtime.State ==
            FavourState.Unavailable)
        {
            runtime = null;
            return false;
        }

        return true;
    }

    private bool ContainsFavour(
        FavourData favour)
    {
        return favour != null &&
               favours.Contains(
                   favour);
    }

    private bool HasConfiguredFavour()
    {
        foreach (FavourData favour in favours)
        {
            if (favour != null)
                return true;
        }

        return false;
    }
}