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

        TrySubscribeToManager();

        RefreshMarker();
    }

    private void OnEnable()
    {
        TrySubscribeToManager();
    }

    private void OnDisable()
    {
        UnsubscribeFromManager();
    }


    // ========================================================
    // IDENTITY
    // =======================================================

    private EntityIdentity entityIdentity;

    public EntityIdentity EntityIdentity
    {
        get
        {
            if (entityIdentity == null)
            {
                entityIdentity =
                    GetComponent<
                        EntityIdentity>();
            }

            return entityIdentity;
        }
    }

    public string EntityId =>
        EntityIdentity != null
            ? EntityIdentity.Id
            : string.Empty;

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

        PlayerFavourManager manager =
            PlayerFavourManager.Instance;

        if (manager == null)
            return false;

        return
            HasLocalInteraction(
                manager
            ) ||
            HasRelevantRuntime(
                manager
            );
    }

    private bool HasLocalInteraction(
    PlayerFavourManager manager)
    {
        if (manager == null)
            return false;

        foreach (FavourData favour
                 in favours)
        {
            if (favour == null)
                continue;

            /*
             * Oregistrerad favour måste kunna interageras med
             * så att ExplicitAccept/DiscoverOnInteraction kan
             * registrera den.
             */
            if (!manager.TryGetRuntime(
                    favour,
                    out FavourRuntime runtime))
            {
                return true;
            }

            if (runtime != null &&
                ShouldShowRuntimeHere(
                    runtime))
            {
                return true;
            }
        }

        return false;
    }

    private bool HasRelevantRuntime(
    PlayerFavourManager manager)
    {
        if (manager == null ||
            string.IsNullOrWhiteSpace(
                EntityId))
        {
            return false;
        }

        foreach (FavourRuntime runtime
                 in manager.Runtimes)
        {
            if (runtime == null)
                continue;

            /*
             * Ready favour som ska lämnas in här.
             */
            if (runtime.State ==
                    FavourState.ReadyToTurnIn &&
                IsCompletionTargetFor(
                    runtime))
            {
                return true;
            }

            /*
             * Optional Completed-dialogue hos completion target.
             */
            if (runtime.State ==
                    FavourState.Completed &&
                IsCompletionTargetFor(
                    runtime) &&
                !string.IsNullOrWhiteSpace(
                    GetDialogueFor(
                        runtime)))
            {
                return true;
            }

            /*
             * Ett vanligt InteractObjective får också göra denna
             * entity interagerbar medan objective't är aktivt.
             */
            if (runtime.State !=
                FavourState.Active)
            {
                continue;
            }

            foreach (FavourObjectiveRuntime objective
                     in runtime.Objectives)
            {
                if (objective is
                        InteractObjectiveRuntime interact &&
                    interact.RequiresTarget(
                        EntityId))
                {
                    return true;
                }
            }
        }

        return false;
    }

    public bool IsCompletionTargetFor(
    FavourRuntime runtime)
    {
        if (runtime == null ||
            runtime.Data == null)
        {
            return false;
        }

        switch (runtime.CompletionPolicy)
        {
            case FavourCompletionPolicy
                .ReturnToGiver:

                return ContainsFavour(
                    runtime.Data
                );

            case FavourCompletionPolicy
                .CompleteAtTarget:

            case FavourCompletionPolicy
                .CompleteAtWorldObject:

                return EntityTargetUtility
                    .Matches(
                        EntityIdentity,
                        runtime
                            .CompletionTargetId
                    );

            default:

                return false;
        }
    }

    public bool CanTurnIn(
    FavourData favour)
    {
        if (favour == null)
            return false;

        PlayerFavourManager manager =
            PlayerFavourManager.Instance;

        if (manager == null ||
            !manager.TryGetRuntime(
                favour,
                out FavourRuntime runtime))
        {
            return false;
        }

        return runtime != null &&
               runtime.State ==
                   FavourState.ReadyToTurnIn &&
               IsCompletionTargetFor(
                   runtime
               );
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
        {
            RefreshMarkerAnchor();
            return;
        }

        marker =
            GetComponentInChildren<
                FavourMarker>(
                    true
                );

        if (marker != null)
        {
            RefreshMarkerAnchor();
            return;
        }

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

        RefreshMarkerAnchor();
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
        RefreshMarker();
    }

    private void RefreshMarker()
    {
        FavourMarkerVisualState state =
            GetMarkerState();

        /*
         * Skapa aldrig en marker enbart för att hålla den Hidden.
         */
        if (state ==
                FavourMarkerVisualState.Hidden &&
            marker == null)
        {
            return;
        }

        EnsureMarker();

        if (marker == null)
            return;

        marker.SetState(
            state
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

        /*
         * Favours som denna entity själv erbjuder.
         */
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

            switch (runtime.State)
            {
                case FavourState.Available:

                    strongestState =
                        GetStrongerMarkerState(
                            strongestState,
                            FavourMarkerVisualState
                                .Bronze
                        );

                    break;

                case FavourState.Active:

                    strongestState =
                        GetStrongerMarkerState(
                            strongestState,
                            FavourMarkerVisualState
                                .Silver
                        );

                    break;

                case FavourState.ReadyToTurnIn:

                    /*
                     * GOLD visas endast där favourn faktiskt
                     * kan lämnas in.
                     */
                    if (IsCompletionTargetFor(
                            runtime))
                    {
                        return
                            FavourMarkerVisualState
                                .Gold;
                    }

                    break;
            }
        }

        /*
         * Denna entity kan vara completion target för en favour
         * den INTE själv äger.
         *
         * Exempel:
         * Umfrin gav favourn, Fanarik är recipient.
         */
        foreach (FavourRuntime runtime
                 in manager.Runtimes)
        {
            if (runtime == null ||
                runtime.State !=
                    FavourState.ReadyToTurnIn)
            {
                continue;
            }

            if (IsCompletionTargetFor(
                    runtime))
            {
                return
                    FavourMarkerVisualState
                        .Gold;
            }
        }

        return strongestState;
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

    private void RefreshMarkerAnchor()
    {
        if (marker == null)
            return;

        InteractionTarget target =
            GetComponentInChildren<
                InteractionTarget>(
                    true
                );

        if (target == null ||
            target.InteractionCollider == null)
        {
            return;
        }

        marker.AnchorToColliderBottom(
            target.InteractionCollider
        );
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
        if (!CanTurnIn(
                favour))
        {
            return false;
        }

        PlayerFavourManager manager =
            PlayerFavourManager.Instance;

        return manager != null &&
               manager.TryTurnIn(
                   favour
               );
    }

    public List<FavourRuntime>
    GetVisibleFavours()
    {
        List<FavourRuntime> result =
            new();

        HashSet<string> addedIds =
            new(
                System.StringComparer.Ordinal
            );

        PlayerFavourManager manager =
            PlayerFavourManager.Instance;

        if (manager == null)
            return result;

        /*
         * Favours som denna entity själv erbjuder.
         */
        foreach (FavourData favour
                 in favours)
        {
            if (favour == null)
                continue;

            if (!manager.TryGetRuntime(
                    favour,
                    out FavourRuntime runtime))
            {
                continue;
            }

            if (!ShouldShowRuntimeHere(
                    runtime))
            {
                continue;
            }

            if (addedIds.Add(
                    runtime.Id))
            {
                result.Add(
                    runtime
                );
            }
        }

        /*
         * Favours som denna entity är target/recipient för,
         * utan att själv behöva äga FavourData i Inspector.
         */
        foreach (FavourRuntime runtime
                 in manager.Runtimes)
        {
            if (runtime == null ||
                addedIds.Contains(
                    runtime.Id))
            {
                continue;
            }

            if (!ShouldShowRuntimeHere(
                    runtime))
            {
                continue;
            }

            if (addedIds.Add(
                    runtime.Id))
            {
                result.Add(
                    runtime
                );
            }
        }

        return result;
    }

    private bool ShouldShowRuntimeHere(
    FavourRuntime runtime)
    {
        if (runtime == null ||
            runtime.Data == null ||
            runtime.State ==
                FavourState.Unavailable)
        {
            return false;
        }

        bool localGiver =
            ContainsFavour(
                runtime.Data
            );

        bool completionTarget =
            IsCompletionTargetFor(
                runtime
            );

        if (runtime.State ==
            FavourState.ReadyToTurnIn)
        {
            return completionTarget;
        }

        if (runtime.State ==
            FavourState.Completed)
        {
            string dialogue =
                GetDialogueFor(
                    runtime
                );

            return !string.IsNullOrWhiteSpace(
                dialogue
            );
        }

        /*
         * Offer och Active visas hos den ursprungliga givaren.
         */
        return localGiver;
    }

    public bool TryGetVisibleRuntime(
    FavourData favour,
    out FavourRuntime runtime)
    {
        runtime = null;

        if (favour == null)
            return false;

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

        if (!ShouldShowRuntimeHere(
                runtime))
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


    // =========================================================
    // Dialogue
    // =========================================================
    public string GetDialogueFor(
    FavourRuntime runtime)
    {
        if (runtime?.Data == null)
            return string.Empty;

        bool localGiver =
            ContainsFavour(
                runtime.Data
            );

        bool completionTarget =
            IsCompletionTargetFor(
                runtime
            );

        /*
         * Ett separat completion target äger framför allt
         * ReadyToTurnIn / Completed-dialogen.
         */
        if (completionTarget &&
            runtime.Data
                .CompletionDialogueSet != null)
        {
            if (runtime.State ==
                    FavourState.ReadyToTurnIn ||
                runtime.State ==
                    FavourState.Completed)
            {
                string completionDialogue =
                    runtime.Data
                        .CompletionDialogueSet
                        .GetDialogue(
                            runtime.State
                        );

                if (!string.IsNullOrWhiteSpace(
                        completionDialogue))
                {
                    return completionDialogue;
                }
            }
        }

        if (localGiver &&
            runtime.Data.DialogueSet != null)
        {
            return runtime.Data
                .DialogueSet
                .GetDialogue(
                    runtime.State
                );
        }

        if (completionTarget &&
            runtime.Data
                .CompletionDialogueSet != null)
        {
            return runtime.Data
                .CompletionDialogueSet
                .GetDialogue(
                    runtime.State
                );
        }

        return string.Empty;
    }
}