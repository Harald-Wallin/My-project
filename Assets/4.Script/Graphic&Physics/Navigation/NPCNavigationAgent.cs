using UnityEngine;

/// <summary>
/// Runtime-navigation för en NPC.
///
/// Navigation sker i två nivåer:
///
/// WORLD NAVIGATION:
/// NavigationRegion -> Portal -> NavigationRegion.
///
/// LOCAL NAVIGATION:
/// A* inne i den region NPC:n för närvarande befinner sig i.
///
/// NPCMovement behöver aldrig känna till skillnaden.
/// Den frågar endast efter en movement-direction.
///
/// Ansvar:
/// - hålla reda på aktuell NavigationRegion
/// - planera world-route mellan regions
/// - välja nästa portal
/// - begära lokal A* till nästa navigationmål
/// - korsa portals
/// - följa smoothade waypoints
/// - repatha när navigationen förändras
/// - recovera från fysisk blockage
///
/// Ansvarar INTE för:
/// - Rigidbody2D movement
/// - movement speed
/// - animation
/// - AI states
/// - combat decisions
/// </summary>
[RequireComponent(typeof(NPCMovement))]
public sealed class NPCNavigationAgent :
    MonoBehaviour
{
    // =========================================================
    // REGION
    // =========================================================

    [Header("Region")]

    [SerializeField]
    private NavigationRegion navigationRegion;

    // =========================================================
    // LOCAL PATHFINDING
    // =========================================================

    [Header("Local Pathfinding")]

    [SerializeField]
    [Min(0.05f)]
    private float waypointReachDistance =
        0.35f;

    [SerializeField]
    [Min(0.05f)]
    [Tooltip(
        "Minsta tid mellan lokala A*-sökningar."
    )]
    private float repathInterval =
        0.25f;

    [SerializeField]
    [Min(0f)]
    [Tooltip(
        "Hur långt det lokala navigationmålet måste ha flyttats " +
        "innan den befintliga pathen betraktas som gammal."
    )]
    private float targetMoveThreshold =
        0.75f;

    [SerializeField]
    [Tooltip(
        "Om agenten får använda direkt movement när ingen " +
        "lokal A*-path redan är aktiv."
    )]
    private bool preferDirectMovement =
        true;

    // =========================================================
    // WORLD NAVIGATION
    // =========================================================

    [Header("World Navigation")]

    [SerializeField]
    [Min(0.05f)]
    [Tooltip(
        "Hur nära portalens säkra punkt NPC:n behöver komma " +
        "innan själva regionövergången börjar."
    )]
    private float portalReachDistance =
        0.35f;

    /*
     * High-level path:
     *
     * Region A
     *   ↓
     * Portal 0
     *   ↓
     * Region B
     *   ↓
     * Portal 1
     *   ↓
     * Region C
     */
    private NavigationWorldPath
        currentWorldPath;

    /*
     * Portalindex motsvarar vilken transition NPC:n står inför.
     *
     * Om NPC:n befinner sig i Regions[0]
     * används Portals[0].
     *
     * När NPC:n kommer till Regions[1]
     * används Portals[1].
     */
    private int worldPortalIndex;

    private NavigationRegion
        worldDestinationRegion;

    // =========================================================
    // STUCK RECOVERY
    // =========================================================

    [Header("Stuck Recovery")]

    [SerializeField]
    [Min(0.1f)]
    [Tooltip(
        "Hur länge NPC:n får försöka följa navigationen utan " +
        "att göra tydlig progress innan en ny lokal path begärs."
    )]
    private float stuckCheckDuration =
        0.75f;

    [SerializeField]
    [Min(0.001f)]
    [Tooltip(
        "Minsta positionsförändring som räknas som progress."
    )]
    private float minimumProgressDistance =
        0.12f;

    [SerializeField]
    [Min(0.1f)]
    [Tooltip(
        "Hur länge Direct Movement stängs av efter att den " +
        "fysiska Rigidbody-rörelsen har blockerats av World."
    )]
    private float physicalBlockPathDuration =
        1.5f;

    private Vector2 progressCheckPosition;

    private float progressTimer;

    private float forcedPathTimer;

    // =========================================================
    // DEBUG
    // =========================================================

    [Header("Debug")]

    [SerializeField]
    private bool drawCurrentPath =
        true;

    [SerializeField]
    private bool drawRawPath;

    [SerializeField]
    private bool drawCurrentWaypoint =
        true;

    [SerializeField]
    private bool drawWorldRoute =
        true;

    [SerializeField]
    private bool navigationDebugLogs;

    // =========================================================
    // LOCAL PATH RUNTIME
    // =========================================================

    private NavigationPath currentPath;

    private int waypointIndex;

    /*
     * Den slutgiltiga destination AI:n har bett om.
     *
     * Exempel:
     * spelarens position.
     */
    private Vector2 requestedDestination;

    /*
     * Destinationen som den nuvarande LOKALA A*-pathen
     * beräknades för.
     *
     * Detta kan vara:
     * - final target
     * - portal PointA
     * - portal PointB
     */
    private Vector2 pathDestination;

    private float repathTimer;

    private bool hasDestination;

    private bool pathRequestPending;

    private int pathRequestVersion;

    // =========================================================
    // PUBLIC
    // =========================================================

    public bool HasDestination =>
        hasDestination;

    public bool HasPath =>
        currentPath != null &&
        currentPath.IsValid;

    public bool HasWorldPath =>
        currentWorldPath != null &&
        currentWorldPath.IsValid;

    public bool IsCrossRegionNavigating =>
        HasWorldPath &&
        currentWorldPath.RequiresRegionTransition;

    public NavigationRegion CurrentRegion =>
        navigationRegion;

    public NavigationRegion DestinationRegion =>
        worldDestinationRegion;

    public Vector2 Destination =>
        requestedDestination;

    // =========================================================
    // UNITY
    // =========================================================

    private void Awake()
    {
        progressCheckPosition =
            transform.position;

        ResolveNavigationRegion();
    }

    private void Update()
    {
        if (repathTimer > 0f)
        {
            repathTimer =
                Mathf.Max(
                    0f,
                    repathTimer -
                    Time.deltaTime
                );
        }

        if (forcedPathTimer > 0f)
        {
            forcedPathTimer =
                Mathf.Max(
                    0f,
                    forcedPathTimer -
                    Time.deltaTime
                );
        }
    }

    private void OnValidate()
    {
        waypointReachDistance =
            Mathf.Max(
                0.05f,
                waypointReachDistance
            );

        repathInterval =
            Mathf.Max(
                0.05f,
                repathInterval
            );

        targetMoveThreshold =
            Mathf.Max(
                0f,
                targetMoveThreshold
            );

        portalReachDistance =
            Mathf.Max(
                0.05f,
                portalReachDistance
            );

        stuckCheckDuration =
            Mathf.Max(
                0.1f,
                stuckCheckDuration
            );

        minimumProgressDistance =
            Mathf.Max(
                0.001f,
                minimumProgressDistance
            );

        physicalBlockPathDuration =
            Mathf.Max(
                0.1f,
                physicalBlockPathDuration
            );
    }

    // =========================================================
    // MOVEMENT DIRECTION
    // =========================================================

    /// <summary>
    /// Huvudingången för NPCMovement.
    ///
    /// destination är alltid AI:ns SLUTGILTIGA destination.
    ///
    /// Metoden avgör själv om NPC:n:
    /// - kan gå direkt
    /// - behöver lokal A*
    /// - behöver gå till en portal
    /// - behöver korsa regiongränsen
    /// </summary>
    public bool TryGetMovementDirection(
        Vector2 destination,
        out Vector2 direction)
    {
        direction =
            Vector2.zero;

        requestedDestination =
            destination;

        hasDestination =
            true;

        Vector2 currentPosition =
            transform.position;

        if (!ResolveNavigationRegion())
        {
            /*
             * Utanför NavigationWorld.
             *
             * Behåll gammal safe fallback.
             */
            return TryGetDirectDirection(
                destination,
                out direction
            );
        }

        NavigationWorld world =
            NavigationWorld.Instance;

        if (world == null)
            return false;

        // -----------------------------------------------------
        // DESTINATION REGION
        // -----------------------------------------------------

        if (!world.TryGetRegionAt(
                destination,
                out NavigationRegion destinationRegion))
        {
            /*
             * Destinationen ligger utanför registrerad
             * navigation.
             *
             * Vi försöker inte göra lokal A* mot en punkt
             * som inte tillhör någon region.
             */
            return false;
        }

        worldDestinationRegion =
            destinationRegion;

        // -----------------------------------------------------
        // SAME REGION
        // -----------------------------------------------------

        if (destinationRegion ==
            navigationRegion)
        {
            /*
             * NPC:n har nått samma region som final target.
             *
             * World-navigation behövs inte längre.
             */
            ClearWorldPathOnly();

            return TryGetLocalMovementDirection(
                currentPosition,
                destination,
                out direction
            );
        }

        // -----------------------------------------------------
        // DIFFERENT REGION
        // -----------------------------------------------------

        if (!EnsureWorldRoute(
                world,
                currentPosition,
                destination,
                destinationRegion))
        {
            return false;
        }

        /*
         * World-routen kan ha synkroniserats med en ny region.
         *
         * Om NPC:n exempelvis precis korsat portal A->B ska
         * worldPortalIndex nu peka på nästa transition.
         */
        if (!TryGetCurrentPortal(
                out NavigationRegionPortal portal,
                out NavigationRegion nextRegion))
        {
            /*
             * Något förändrades i world-grafen.
             *
             * Invalidera routen så nästa frame kan bygga om den.
             */
            ClearWorldPathOnly();

            return false;
        }

        Vector2 portalPointCurrent =
            portal.GetPointForRegion(
                navigationRegion
            );

        Vector2 portalPointNext =
            portal.GetPointForRegion(
                nextRegion
            );

        float distanceToPortal =
            Vector2.Distance(
                currentPosition,
                portalPointCurrent
            );

        // -----------------------------------------------------
        // REACH CURRENT SIDE OF PORTAL
        // -----------------------------------------------------

        if (distanceToPortal >
            portalReachDistance)
        {
            return TryGetLocalMovementDirection(
                currentPosition,
                portalPointCurrent,
                out direction
            );
        }

        // -----------------------------------------------------
        // CROSS PORTAL
        // -----------------------------------------------------

        /*
         * Vi är framme vid den säkra cellpunkten på vår sida.
         *
         * Lokal A* behövs inte för själva halva cell-steget
         * över regiongränsen.
         *
         * Portalen byggdes endast om PointA <-> PointB
         * verifierades som traverserbar.
         */
        ClearLocalNavigationForPortalCrossing();

        Vector2 crossingDelta =
            portalPointNext -
            currentPosition;

        if (crossingDelta.sqrMagnitude <=
            0.0001f)
        {
            return false;
        }

        direction =
            crossingDelta.normalized;

        return true;
    }

    // =========================================================
    // WORLD ROUTE
    // =========================================================

    private bool EnsureWorldRoute(
        NavigationWorld world,
        Vector2 currentPosition,
        Vector2 destination,
        NavigationRegion destinationRegion)
    {
        if (world == null ||
            navigationRegion == null ||
            destinationRegion == null)
        {
            return false;
        }

        /*
         * Försök först återanvända den existerande world-routen.
         *
         * Spelaren kan röra sig mycket INNE I samma region utan
         * att hela high-level-routen behöver räknas om.
         */
        if (TrySynchronizeExistingWorldRoute(
                navigationRegion,
                destinationRegion))
        {
            return true;
        }

        NavigationWorldPath newPath =
            NavigationWorldPathfinder
                .FindPath(
                    world,
                    currentPosition,
                    destination
                );

        if (newPath == null ||
            !newPath.IsValid ||
            !newPath.RequiresRegionTransition)
        {
            if (navigationDebugLogs)
            {
                Debug.LogWarning(
                    $"[NAV WORLD] {name} kunde inte hitta world-route | " +
                    $"from={navigationRegion.name} | " +
                    $"to={destinationRegion.name}",
                    this
                );
            }

            ClearWorldPathOnly();

            return false;
        }

        /*
         * En ny high-level-route kan välja en annan första portal.
         *
         * Den gamla lokala pathen är därför inte längre säker att
         * följa.
         */
        InvalidateLocalPath();

        currentWorldPath =
            newPath;

        worldDestinationRegion =
            destinationRegion;

        worldPortalIndex =
            0;

        /*
         * Hitta vår faktiska position i routen.
         *
         * Normalt index 0, men detta gör systemet robust även om
         * routen skapades precis när agenten passerade en border.
         */
        SynchronizeWorldPortalIndex(
            navigationRegion
        );

        if (navigationDebugLogs)
        {
            Debug.Log(
                $"[NAV WORLD] {name} WORLD ROUTE | " +
                $"regions={currentWorldPath.RegionCount} | " +
                $"portals={currentWorldPath.PortalCount} | " +
                $"currentRegion={navigationRegion.name} | " +
                $"targetRegion={destinationRegion.name}",
                this
            );
        }

        return true;
    }

    /// <summary>
    /// Försöker behålla nuvarande world-route.
    ///
    /// Returnerar false om:
    /// - target bytt region
    /// - current region inte längre finns i routen
    /// - routen blivit ogiltig
    /// </summary>
    private bool TrySynchronizeExistingWorldRoute(
        NavigationRegion currentRegion,
        NavigationRegion destinationRegion)
    {
        if (!HasWorldPath)
            return false;

        if (currentRegion == null ||
            destinationRegion == null)
        {
            return false;
        }

        NavigationRegion routeDestination =
            currentWorldPath.GetRegion(
                currentWorldPath.RegionCount -
                1
            );

        if (routeDestination !=
            destinationRegion)
        {
            return false;
        }

        return SynchronizeWorldPortalIndex(
            currentRegion
        );
    }

    /// <summary>
    /// Synkar worldPortalIndex mot den region NPC:n faktiskt
    /// befinner sig i.
    ///
    /// Regions:
    ///
    /// 0 = A
    /// 1 = B
    /// 2 = C
    ///
    /// Portals:
    ///
    /// 0 = A -> B
    /// 1 = B -> C
    ///
    /// Om NPC:n nu är i B ska portalIndex alltså vara 1.
    /// </summary>
    private bool SynchronizeWorldPortalIndex(
        NavigationRegion currentRegion)
    {
        if (!HasWorldPath ||
            currentRegion == null)
        {
            return false;
        }

        int foundRegionIndex =
            -1;

        for (int i = 0;
             i < currentWorldPath.RegionCount;
             i++)
        {
            if (currentWorldPath.GetRegion(i) !=
                currentRegion)
            {
                continue;
            }

            foundRegionIndex =
                i;

            break;
        }

        if (foundRegionIndex < 0)
        {
            return false;
        }

        int previousPortalIndex =
            worldPortalIndex;

        worldPortalIndex =
            foundRegionIndex;

        /*
         * Vi har korsat minst en regiongräns.
         *
         * Den gamla lokala pathen pekade på föregående portal
         * och ska kastas bort.
         */
        if (worldPortalIndex !=
            previousPortalIndex)
        {
            InvalidateLocalPath();

            ResetProgressTracking(
                transform.position
            );
        }

        return true;
    }

    private bool TryGetCurrentPortal(
        out NavigationRegionPortal portal,
        out NavigationRegion nextRegion)
    {
        portal =
            null;

        nextRegion =
            null;

        if (!HasWorldPath ||
            navigationRegion == null)
        {
            return false;
        }

        /*
         * Sista regionen har ingen ytterligare portal.
         */
        if (worldPortalIndex < 0 ||
            worldPortalIndex >=
            currentWorldPath.PortalCount)
        {
            return false;
        }

        portal =
            currentWorldPath.GetPortal(
                worldPortalIndex
            );

        if (portal == null ||
            !portal.Connects(
                navigationRegion))
        {
            portal =
                null;

            return false;
        }

        nextRegion =
            portal.GetOtherRegion(
                navigationRegion
            );

        return
            nextRegion != null;
    }

    // =========================================================
    // LOCAL MOVEMENT
    // =========================================================

    /// <summary>
    /// Hanterar movement till ETT mål inne i den aktuella regionen.
    ///
    /// Målet kan vara:
    /// - slutdestinationen
    /// - aktuell portals PointA/PointB
    /// </summary>
    private bool TryGetLocalMovementDirection(
        Vector2 currentPosition,
        Vector2 localDestination,
        out Vector2 direction)
    {
        direction =
            Vector2.zero;

        if (navigationRegion == null)
            return false;

        // -----------------------------------------------------
        // ACTIVE LOCAL PATH
        // -----------------------------------------------------

        if (HasPath)
        {
            TryScheduleUpdatedPath(
                currentPosition,
                localDestination
            );

            AdvanceReachedWaypoints(
                currentPosition
            );

            if (waypointIndex >=
                currentPath.PointCount)
            {
                ClearPathOnly();
            }
            else
            {
                AdvanceVisibleWaypoints(
                    currentPosition
                );

                if (TryGetCurrentWaypointDirection(
                        currentPosition,
                        out direction))
                {
                    UpdateStuckDetection(
                        currentPosition,
                        localDestination
                    );

                    return true;
                }
            }
        }

        // -----------------------------------------------------
        // NO ACTIVE PATH
        // -----------------------------------------------------

        ResetProgressTracking(
            currentPosition
        );

        bool mayUseDirectMovement =
            preferDirectMovement &&
            forcedPathTimer <= 0f;

        if (mayUseDirectMovement &&
            navigationRegion
                .IsDirectPathClear(
                    currentPosition,
                    localDestination
                ))
        {
            return TryGetDirectionToPoint(
                currentPosition,
                localDestination,
                out direction
            );
        }

        // -----------------------------------------------------
        // NEED LOCAL A*
        // -----------------------------------------------------

        if (ShouldRequestPath(
                localDestination))
        {
            RequestPath(
                currentPosition,
                localDestination
            );
        }

        /*
         * Resultatet kommer från NavigationPathScheduler.
         */
        return false;
    }

    // =========================================================
    // PHYSICAL BLOCK FEEDBACK
    // =========================================================

    /// <summary>
    /// NPCMovement anropar detta när navigationen gav en riktning
    /// men Rigidbody:n faktiskt inte kunde röra sig.
    ///
    /// destination är AI:ns SLUTGILTIGA target-position.
    /// Agenten översätter själv detta till rätt lokalt mål.
    /// </summary>
    public void NotifyPhysicalMovementBlocked(
        Vector2 destination)
    {
        requestedDestination =
            destination;

        hasDestination =
            true;

        forcedPathTimer =
            Mathf.Max(
                forcedPathTimer,
                physicalBlockPathDuration
            );

        if (!ResolveNavigationRegion())
            return;

        /*
         * Väntande request får först komma tillbaka.
         *
         * Annars kan vi återintroducera den gamla buggen där
         * agenten cancellerade sin egen request varje FixedUpdate.
         */
        if (pathRequestPending)
            return;

        Vector2 localGoal;

        bool portalCrossing;

        if (!TryResolveCurrentLocalGoal(
                destination,
                out localGoal,
                out portalCrossing))
        {
            return;
        }

        /*
         * Under själva portal-crossingen ska vi INTE försöka köra
         * lokal A* till PointB, eftersom PointB ligger i nästa region.
         */
        if (portalCrossing)
        {
            return;
        }

        repathTimer =
            0f;

        RequestPath(
            transform.position,
            localGoal,
            forceGridPath: true
        );
    }

    /// <summary>
    /// Räknar ut vilket lokalt mål som motsvarar final destination
    /// just nu.
    /// </summary>
    private bool TryResolveCurrentLocalGoal(
        Vector2 finalDestination,
        out Vector2 localGoal,
        out bool portalCrossing)
    {
        localGoal =
            finalDestination;

        portalCrossing =
            false;

        if (!ResolveNavigationRegion())
            return false;

        NavigationWorld world =
            NavigationWorld.Instance;

        if (world == null ||
            !world.TryGetRegionAt(
                finalDestination,
                out NavigationRegion targetRegion))
        {
            return false;
        }

        if (targetRegion ==
            navigationRegion)
        {
            return true;
        }

        if (!EnsureWorldRoute(
                world,
                transform.position,
                finalDestination,
                targetRegion))
        {
            return false;
        }

        if (!TryGetCurrentPortal(
                out NavigationRegionPortal portal,
                out NavigationRegion nextRegion))
        {
            return false;
        }

        Vector2 currentSide =
            portal.GetPointForRegion(
                navigationRegion
            );

        float distance =
            Vector2.Distance(
                transform.position,
                currentSide
            );

        if (distance <=
            portalReachDistance)
        {
            localGoal =
                portal.GetPointForRegion(
                    nextRegion
                );

            portalCrossing =
                true;

            return true;
        }

        localGoal =
            currentSide;

        return true;
    }

    // =========================================================
    // CURRENT WAYPOINT
    // =========================================================

    private bool TryGetCurrentWaypointDirection(
        Vector2 currentPosition,
        out Vector2 direction)
    {
        direction =
            Vector2.zero;

        if (!HasPath ||
            waypointIndex < 0 ||
            waypointIndex >=
            currentPath.PointCount)
        {
            return false;
        }

        Vector2 waypoint =
            currentPath.GetPoint(
                waypointIndex
            );

        return TryGetDirectionToPoint(
            currentPosition,
            waypoint,
            out direction
        );
    }

    private static bool TryGetDirectionToPoint(
        Vector2 from,
        Vector2 to,
        out Vector2 direction)
    {
        Vector2 delta =
            to -
            from;

        if (delta.sqrMagnitude <=
            0.0001f)
        {
            direction =
                Vector2.zero;

            return false;
        }

        direction =
            delta.normalized;

        return true;
    }

    // =========================================================
    // PATH REQUEST
    // =========================================================

    private bool ShouldRequestPath(
        Vector2 localDestination)
    {
        if (pathRequestPending)
        {
            float destinationChange =
                Vector2.Distance(
                    localDestination,
                    pathDestination
                );

            return
                destinationChange >=
                targetMoveThreshold &&
                repathTimer <= 0f;
        }

        if (!HasPath)
        {
            return
                repathTimer <= 0f;
        }

        float targetMovement =
            Vector2.Distance(
                localDestination,
                pathDestination
            );

        if (targetMovement >=
            targetMoveThreshold)
        {
            return
                repathTimer <= 0f;
        }

        if (waypointIndex >=
            currentPath.PointCount)
        {
            return
                repathTimer <= 0f;
        }

        return false;
    }

    private void TryScheduleUpdatedPath(
        Vector2 currentPosition,
        Vector2 localDestination)
    {
        float targetMovement =
            Vector2.Distance(
                localDestination,
                pathDestination
            );

        if (targetMovement <
            targetMoveThreshold)
        {
            return;
        }

        if (repathTimer > 0f)
            return;

        RequestPath(
            currentPosition,
            localDestination
        );
    }

    private void RequestPath(
        Vector2 start,
        Vector2 localDestination,
        bool forceGridPath = false)
    {
        if (navigationRegion == null)
        {
            if (navigationDebugLogs)
            {
                Debug.LogWarning(
                    $"[NAV DEBUG] {name} REQUEST FAILED: NO REGION",
                    this
                );
            }

            return;
        }

        /*
         * Säkerhetsregel:
         *
         * Lokal A* får ALDRIG söka mot en punkt utanför den
         * region som requesten körs på.
         */
        if (!navigationRegion
                .ContainsWorldPosition(
                    localDestination))
        {
            if (navigationDebugLogs)
            {
                Debug.LogWarning(
                    $"[NAV DEBUG] {name} LOCAL REQUEST OUTSIDE REGION | " +
                    $"region={navigationRegion.name} | " +
                    $"destination={localDestination}",
                    this
                );
            }

            return;
        }

        pathRequestVersion++;

        int version =
            pathRequestVersion;

        pathDestination =
            localDestination;

        pathRequestPending =
            true;

        repathTimer =
            repathInterval;

        if (navigationDebugLogs)
        {
            Debug.Log(
                $"[NAV DEBUG] {name} REQUEST LOCAL PATH | " +
                $"version={version} | " +
                $"region={navigationRegion.name} | " +
                $"start={start} | " +
                $"destination={localDestination} | " +
                $"forceGrid={forceGridPath} | " +
                $"hadPath={HasPath}",
                this
            );
        }

        NavigationPathScheduler
            .Instance
            .RequestPath(
                this,
                navigationRegion,
                start,
                localDestination,
                version,
                forceGridPath
            );
    }

    // =========================================================
    // SCHEDULED RESULT
    // =========================================================

    public void ReceiveScheduledPath(
        NavigationPath path,
        Vector2 destination,
        int version)
    {
        if (version !=
            pathRequestVersion)
        {
            if (navigationDebugLogs)
            {
                Debug.LogWarning(
                    $"[NAV DEBUG] {name} PATH REJECTED: OLD VERSION | " +
                    $"received={version} | " +
                    $"expected={pathRequestVersion}",
                    this
                );
            }

            return;
        }

        pathRequestPending =
            false;

        pathDestination =
            destination;

        if (path == null ||
            !path.IsValid)
        {
            if (navigationDebugLogs)
            {
                Debug.LogWarning(
                    $"[NAV DEBUG] {name} LOCAL PATH INVALID | " +
                    $"region={navigationRegion?.name} | " +
                    $"destination={destination}",
                    this
                );
            }

            return;
        }

        currentPath =
            path;

        waypointIndex =
            0;

        /*
         * Punkt 0 är requestens exakta startposition.
         */
        if (currentPath.PointCount > 1)
        {
            waypointIndex =
                1;
        }

        Vector2 currentPosition =
            transform.position;

        AdvanceReachedWaypoints(
            currentPosition
        );

        AdvanceVisibleWaypoints(
            currentPosition
        );

        ResetProgressTracking(
            currentPosition
        );

        if (navigationDebugLogs)
        {
            Debug.Log(
                $"[NAV DEBUG] {name} LOCAL PATH ACCEPTED | " +
                $"points={currentPath.PointCount} | " +
                $"waypoint={waypointIndex} | " +
                $"destination={destination}",
                this
            );
        }
    }

    // =========================================================
    // WAYPOINT ADVANCEMENT
    // =========================================================

    private void AdvanceReachedWaypoints(
        Vector2 currentPosition)
    {
        if (!HasPath)
            return;

        float reachDistanceSqr =
            waypointReachDistance *
            waypointReachDistance;

        while (waypointIndex <
               currentPath.PointCount)
        {
            Vector2 waypoint =
                currentPath.GetPoint(
                    waypointIndex
                );

            float distanceSqr =
                (
                    waypoint -
                    currentPosition
                ).sqrMagnitude;

            if (distanceSqr >
                reachDistanceSqr)
            {
                break;
            }

            waypointIndex++;
        }
    }

    private void AdvanceVisibleWaypoints(
        Vector2 currentPosition)
    {
        if (!HasPath ||
            navigationRegion == null)
        {
            return;
        }

        if (waypointIndex < 0 ||
            waypointIndex >=
            currentPath.PointCount - 1)
        {
            return;
        }

        for (int candidate =
                 currentPath.PointCount - 1;
             candidate >
                 waypointIndex;
             candidate--)
        {
            Vector2 candidatePoint =
                currentPath.GetPoint(
                    candidate
                );

            if (!navigationRegion
                    .IsDirectPathClear(
                        currentPosition,
                        candidatePoint
                    ))
            {
                continue;
            }

            waypointIndex =
                candidate;

            return;
        }
    }

    // =========================================================
    // STUCK RECOVERY
    // =========================================================

    private void UpdateStuckDetection(
        Vector2 currentPosition,
        Vector2 localDestination)
    {
        float movedDistance =
            Vector2.Distance(
                currentPosition,
                progressCheckPosition
            );

        if (movedDistance >=
            minimumProgressDistance)
        {
            ResetProgressTracking(
                currentPosition
            );

            return;
        }

        progressTimer +=
            Time.fixedDeltaTime;

        if (progressTimer <
            stuckCheckDuration)
        {
            return;
        }

        progressTimer =
            0f;

        progressCheckPosition =
            currentPosition;

        if (repathTimer > 0f ||
            pathRequestPending)
        {
            return;
        }

        RequestPath(
            currentPosition,
            localDestination,
            forceGridPath: true
        );
    }

    private void ResetProgressTracking(
        Vector2 currentPosition)
    {
        progressCheckPosition =
            currentPosition;

        progressTimer =
            0f;
    }

    // =========================================================
    // DIRECT MOVEMENT
    // =========================================================

    private bool TryGetDirectDirection(
        Vector2 destination,
        out Vector2 direction)
    {
        return TryGetDirectionToPoint(
            transform.position,
            destination,
            out direction
        );
    }

    // =========================================================
    // REGION
    // =========================================================

    private bool ResolveNavigationRegion()
    {
        Vector2 currentPosition =
            transform.position;

        if (navigationRegion != null &&
            navigationRegion.isActiveAndEnabled &&
            navigationRegion
                .ContainsWorldPosition(
                    currentPosition))
        {
            return true;
        }

        NavigationRegion previousRegion =
            navigationRegion;

        navigationRegion =
            null;

        NavigationWorld world =
            NavigationWorld.Instance;

        if (world == null)
            return false;

        if (!world.TryGetRegionAt(
                currentPosition,
                out NavigationRegion region))
        {
            return false;
        }

        navigationRegion =
            region;

        /*
         * En riktig regionövergång har inträffat.
         *
         * Synka world-routen omedelbart.
         */
        if (previousRegion !=
                navigationRegion &&
            HasWorldPath)
        {
            if (!SynchronizeWorldPortalIndex(
                    navigationRegion))
            {
                /*
                 * NPC:n hamnade i en region som inte längre
                 * tillhör den planerade routen.
                 */
                ClearWorldPathOnly();
            }
        }

        return true;
    }

    // =========================================================
    // RESET
    // =========================================================

    public void ClearDestination()
    {
        hasDestination =
            false;

        requestedDestination =
            Vector2.zero;

        pathDestination =
            Vector2.zero;

        worldDestinationRegion =
            null;

        InvalidateLocalPath();

        ClearWorldPathOnly();

        ResetProgressTracking(
            transform.position
        );
    }

    public void ForceRepath()
    {
        repathTimer =
            0f;

        InvalidateLocalPath();

        ClearWorldPathOnly();

        ResetProgressTracking(
            transform.position
        );
    }

    /// <summary>
    /// Avbryter lokal request/path men behåller final destination
    /// och eventuell world-route.
    /// </summary>
    private void InvalidateLocalPath()
    {
        pathRequestVersion++;

        pathRequestPending =
            false;

        NavigationPathScheduler
            .Instance
            .CancelRequests(
                this
            );

        ClearPathOnly();
    }

    /// <summary>
    /// Används precis under en portalövergång.
    ///
    /// Vi behöver kasta lokal path men ska inte kasta
    /// high-level world-routen.
    /// </summary>
    private void ClearLocalNavigationForPortalCrossing()
    {
        if (HasPath ||
            pathRequestPending)
        {
            InvalidateLocalPath();
        }

        ResetProgressTracking(
            transform.position
        );
    }

    private void ClearPathOnly()
    {
        currentPath =
            null;

        waypointIndex =
            0;
    }

    private void ClearWorldPathOnly()
    {
        currentWorldPath =
            null;

        worldPortalIndex =
            0;

        worldDestinationRegion =
            null;
    }

#if UNITY_EDITOR

    // =========================================================
    // DEBUG
    // =========================================================

    private void OnDrawGizmosSelected()
    {
        DrawLocalPathGizmos();

        DrawWorldPathGizmos();
    }

    private void DrawLocalPathGizmos()
    {
        if (currentPath == null)
            return;

        if (drawRawPath &&
            currentPath.RawPoints != null)
        {
            DrawPath(
                currentPath.RawPoints
            );
        }

        if (drawCurrentPath &&
            currentPath.Points != null)
        {
            DrawPath(
                currentPath.Points
            );
        }

        if (drawCurrentWaypoint &&
            currentPath.IsValid &&
            waypointIndex >= 0 &&
            waypointIndex <
            currentPath.PointCount)
        {
            Vector2 waypoint =
                currentPath.GetPoint(
                    waypointIndex
                );

            Gizmos.DrawWireSphere(
                waypoint,
                waypointReachDistance
            );

            Gizmos.DrawLine(
                transform.position,
                waypoint
            );
        }
    }

    private void DrawWorldPathGizmos()
    {
        if (!drawWorldRoute ||
            currentWorldPath == null ||
            !currentWorldPath.IsValid)
        {
            return;
        }

        Vector2 previous =
            transform.position;

        for (int i =
                 Mathf.Clamp(
                     worldPortalIndex,
                     0,
                     currentWorldPath.PortalCount
                 );
             i < currentWorldPath.PortalCount;
             i++)
        {
            NavigationRegionPortal portal =
                currentWorldPath.GetPortal(
                    i
                );

            if (portal == null)
                continue;

            Gizmos.DrawWireSphere(
                portal.WorldCenter,
                portalReachDistance
            );

            Gizmos.DrawLine(
                previous,
                portal.WorldCenter
            );

            previous =
                portal.WorldCenter;
        }

        Gizmos.DrawLine(
            previous,
            requestedDestination
        );

        Gizmos.DrawWireSphere(
            requestedDestination,
            portalReachDistance *
            0.75f
        );
    }

    private static void DrawPath(
        System.Collections.Generic
            .IReadOnlyList<Vector2> points)
    {
        if (points == null ||
            points.Count < 2)
        {
            return;
        }

        for (int i = 0;
             i < points.Count - 1;
             i++)
        {
            Gizmos.DrawLine(
                points[i],
                points[i + 1]
            );
        }
    }

#endif
}