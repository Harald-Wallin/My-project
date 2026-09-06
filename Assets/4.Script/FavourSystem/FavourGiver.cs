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
        EnsureMarker();

        RegisterBackgroundFavours();

        TrySubscribeToManager();

        RefreshMarker();
    }

    private void OnEnable()
    {
        EnsureMarker();
        TrySubscribeToManager();
    }

    private void OnDisable()
    {
        UnsubscribeFromManager();
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

        RefreshMarker();
    }

    [Header("Favour Marker")]

    [SerializeField]
    [Tooltip(
    "Marker-prefaben som automatiskt skapas under denna " +
    "FavourGiver om ingen FavourMarker redan finns.")]
    private FavourMarker markerPrefab;

    [SerializeField]
    [Tooltip(
        "Valfri parent för markern. Om tom används FavourGiver-rooten.")]
    private Transform markerParent;

    [SerializeField]
    private bool autoCreateMarker =
        true;

    [SerializeField]
    [Tooltip(
        "Om aktiverad visas Bronze även för konfigurerade " +
        "favours som ännu inte registrerats hos spelaren. " +
        "Bra för vanliga quest/favour-givers.")]
    private bool showUnregisteredFavours =
        true;

    private FavourMarker marker;

    private PlayerFavourManager
        subscribedManager;

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
    // FAVOUR MARKER
    // =========================================================

    private void EnsureMarker()
    {
        if (marker != null)
            return;

        marker =
            GetComponentInChildren<
                FavourMarker>(
                true
            );

        if (marker != null)
            return;

        if (!autoCreateMarker ||
            markerPrefab == null)
        {
            return;
        }

        Transform parent =
            markerParent != null
                ? markerParent
                : transform;

        marker =
            Instantiate(
                markerPrefab,
                parent
            );

        marker.transform.localPosition =
            Vector3.zero;

        marker.transform.localRotation =
            Quaternion.identity;
    }

    private void TrySubscribeToManager()
    {
        PlayerFavourManager manager =
            PlayerFavourManager.Instance;

        if (manager == null)
            return;

        if (subscribedManager ==
            manager)
        {
            return;
        }

        UnsubscribeFromManager();

        subscribedManager =
            manager;

        subscribedManager.FavourRegistered +=
            HandleMarkerFavourChanged;

        subscribedManager.FavourStateChanged +=
            HandleMarkerFavourChanged;

        subscribedManager.FavourProgressChanged +=
            HandleMarkerFavourChanged;
    }

    private void UnsubscribeFromManager()
    {
        if (subscribedManager == null)
            return;

        subscribedManager.FavourRegistered -=
            HandleMarkerFavourChanged;

        subscribedManager.FavourStateChanged -=
            HandleMarkerFavourChanged;

        subscribedManager.FavourProgressChanged -=
            HandleMarkerFavourChanged;

        subscribedManager =
            null;
    }

    private void HandleMarkerFavourChanged(
        FavourRuntime runtime)
    {
        if (runtime == null ||
            runtime.Data == null)
        {
            return;
        }

        if (!ContainsFavour(
                runtime.Data))
        {
            return;
        }

        RefreshMarker();
    }

    private void RefreshMarker()
    {
        EnsureMarker();

        if (marker == null)
            return;

        marker.SetState(
            GetMarkerState()
        );
    }

    private FavourMarkerVisualState
        GetMarkerState()
    {
        PlayerFavourManager manager =
            PlayerFavourManager.Instance;

        if (manager == null)
        {
            return
                FavourMarkerVisualState.Hidden;
        }

        FavourMarkerVisualState
            strongestState =
                FavourMarkerVisualState.Hidden;

        foreach (FavourData favour
                 in favours)
        {
            if (favour == null)
                continue;

            if (!manager.TryGetRuntime(
                    favour,
                    out FavourRuntime runtime))
            {
                if (showUnregisteredFavours)
                {
                    strongestState =
                        GetStrongerMarkerState(
                            strongestState,
                            FavourMarkerVisualState
                                .Bronze
                        );
                }

                continue;
            }

            if (runtime == null)
                continue;

            FavourMarkerVisualState state =
                GetMarkerStateForRuntime(
                    runtime
                );

            strongestState =
                GetStrongerMarkerState(
                    strongestState,
                    state
                );

            if (strongestState ==
                FavourMarkerVisualState.Gold)
            {
                return strongestState;
            }
        }

        return strongestState;
    }

    private static FavourMarkerVisualState
        GetMarkerStateForRuntime(
            FavourRuntime runtime)
    {
        if (runtime == null)
        {
            return
                FavourMarkerVisualState.Hidden;
        }

        switch (runtime.State)
        {
            case FavourState.Available:

                return
                    FavourMarkerVisualState
                        .Bronze;

            case FavourState.Active:

                return
                    FavourMarkerVisualState
                        .Silver;

            case FavourState.ReadyToTurnIn:

                return
                    FavourMarkerVisualState
                        .Gold;

            default:

                return
                    FavourMarkerVisualState
                        .Hidden;
        }
    }

    private static FavourMarkerVisualState
        GetStrongerMarkerState(
            FavourMarkerVisualState current,
            FavourMarkerVisualState candidate)
    {
        return GetMarkerPriority(candidate) >
               GetMarkerPriority(current)
            ? candidate
            : current;
    }

    private static int GetMarkerPriority(
        FavourMarkerVisualState state)
    {
        switch (state)
        {
            case FavourMarkerVisualState
                .Gold:

                return 3;

            case FavourMarkerVisualState
                .Silver:

                return 2;

            case FavourMarkerVisualState
                .Bronze:

                return 1;

            default:
                return 0;
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