using UnityEngine;

/// <summary>
/// Runtime-navigation för en NPC.
///
/// Ansvar:
/// - hittar NavigationRegion för NPC:n
/// - väljer direct movement när vägen är fri
/// - begär A* när World-geometri blockerar
/// - följer smoothade waypoints
/// - repathar när destinationen förändras
///
/// Ansvarar INTE för:
/// - Rigidbody2D movement
/// - movement speed
/// - animation
/// - AI states
/// - combat decisions
/// </summary>
[RequireComponent(typeof(NPCMovement))]
public sealed class NPCNavigationAgent : MonoBehaviour
{
    [Header("Region")]

    [SerializeField]
    private NavigationRegion navigationRegion;

    [Header("Pathfinding")]

    [SerializeField]
    [Min(0.05f)]
    private float waypointReachDistance =
        0.35f;

    [SerializeField]
    [Min(0.05f)]
    [Tooltip(
        "Minsta tid mellan A*-sökningar."
    )]
    private float repathInterval =
        0.25f;

    [SerializeField]
    [Min(0f)]
    [Tooltip(
        "Hur långt destinationen måste ha flyttats innan " +
        "en befintlig path betraktas som gammal."
    )]
    private float targetMoveThreshold =
        0.75f;

    [SerializeField]
    [Tooltip(
        "Om agenten automatiskt får övergå till direkt movement " +
        "så fort destinationen blir synlig igen."
    )]
    private bool preferDirectMovement =
        true;

    [Header("Debug")]

    [SerializeField]
    private bool drawCurrentPath =
        true;

    [SerializeField]
    private bool drawRawPath;

    [SerializeField]
    private bool drawCurrentWaypoint =
        true;

    private NavigationPath currentPath;

    private int waypointIndex;

    private Vector2 requestedDestination;
    private Vector2 pathDestination;

    private float repathTimer;

    private bool hasDestination;

    public bool HasDestination =>
        hasDestination;

    public bool HasPath =>
        currentPath != null &&
        currentPath.IsValid;

    public NavigationRegion CurrentRegion =>
        navigationRegion;

    public Vector2 Destination =>
        requestedDestination;

    private void Awake()
    {
        ResolveNavigationRegion();
    }

    private void Update()
    {
        if (repathTimer > 0f)
        {
            repathTimer -=
                Time.deltaTime;
        }
    }

    /// <summary>
    /// Returnerar riktningen NPCMovement bör röra sig i
    /// för att nå destinationen.
    ///
    /// false betyder att agenten inte har någon användbar
    /// movement-riktning just nu.
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

        if (!ResolveNavigationRegion())
        {
            /*
             * NavigationRegion saknas.
             *
             * Vi faller tillbaka till direkt movement så att
             * NPC:n inte blir helt immobil.
             */
            return TryGetDirectDirection(
                destination,
                out direction
            );
        }

        Vector2 currentPosition =
            transform.position;

        /*
         * Direkt väg är alltid förstahandsvalet.
         *
         * A* används bara när World-geometri faktiskt
         * blockerar den raka vägen.
         */
        if (preferDirectMovement &&
            navigationRegion.IsDirectPathClear(
                currentPosition,
                destination))
        {
            ClearPathOnly();

            return TryGetDirectDirection(
                destination,
                out direction
            );
        }

        bool needsPath =
            ShouldRequestPath(
                destination
            );

        if (needsPath)
        {
            RequestPath(
                currentPosition,
                destination
            );
        }

        if (!HasPath)
        {
            /*
             * Ingen A*-path hittades.
             *
             * Vi returnerar ingen riktning här istället för
             * att medvetet försöka springa rakt genom ett hinder.
             */
            return false;
        }

        AdvanceReachedWaypoints(
            currentPosition
        );

        if (!HasPath ||
            waypointIndex >=
            currentPath.PointCount)
        {
            return false;
        }

        /*
         * Path-smoothing gjordes när pathen skapades.
         *
         * Men under movement kan vi dessutom hoppa över
         * waypoints om en senare punkt nu går att nå direkt.
         */
        AdvanceVisibleWaypoints(
            currentPosition
        );

        Vector2 waypoint =
            currentPath.GetPoint(
                waypointIndex
            );

        Vector2 toWaypoint =
            waypoint -
            currentPosition;

        if (toWaypoint.sqrMagnitude <=
            0.0001f)
        {
            return false;
        }

        direction =
            toWaypoint.normalized;

        return true;
    }

    public void ClearDestination()
    {
        hasDestination =
            false;

        requestedDestination =
            Vector2.zero;

        pathDestination =
            Vector2.zero;

        ClearPathOnly();
    }

    public void ForceRepath()
    {
        repathTimer =
            0f;

        ClearPathOnly();
    }

    private bool ShouldRequestPath(
        Vector2 destination)
    {
        if (!HasPath)
            return repathTimer <= 0f;

        float targetMovement =
            Vector2.Distance(
                destination,
                pathDestination
            );

        if (targetMovement >=
            targetMoveThreshold)
        {
            return repathTimer <= 0f;
        }

        if (waypointIndex >=
            currentPath.PointCount)
        {
            return repathTimer <= 0f;
        }

        return false;
    }

    private void RequestPath(
        Vector2 start,
        Vector2 destination)
    {
        if (navigationRegion == null)
            return;

        currentPath =
            AStarPathfinder.FindPath(
                navigationRegion,
                start,
                destination
            );

        pathDestination =
            destination;

        repathTimer =
            repathInterval;

        waypointIndex =
            0;

        if (!HasPath)
            return;

        /*
         * Pathens första punkt är normalt NPC:ns exakta
         * startposition och behöver därför inte följas.
         */
        if (currentPath.PointCount > 1)
        {
            waypointIndex =
                1;
        }
    }

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

        if (waypointIndex >=
            currentPath.PointCount - 1)
        {
            return;
        }

        /*
         * Leta bakifrån.
         *
         * Om NPC:n kan nå en senare waypoint direkt finns
         * ingen anledning att följa mellanliggande punkter.
         */
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
                        candidatePoint))
            {
                continue;
            }

            waypointIndex =
                candidate;

            return;
        }
    }

    private bool TryGetDirectDirection(
        Vector2 destination,
        out Vector2 direction)
    {
        Vector2 delta =
            destination -
            (Vector2)transform.position;

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

    private bool ResolveNavigationRegion()
    {
        if (navigationRegion != null &&
            navigationRegion
                .ContainsWorldPosition(
                    transform.position))
        {
            return true;
        }

        /*
         * Temporär lösning för fas 1:
         *
         * Vi söker efter regionen i scenen.
         *
         * När NavigationWorld införs ersätts detta med ett
         * O(1)-liknande region-lookup istället.
         */
        NavigationRegion[] regions =
            FindObjectsByType<
                NavigationRegion>(
                FindObjectsSortMode.None
            );

        for (int i = 0;
             i < regions.Length;
             i++)
        {
            NavigationRegion region =
                regions[i];

            if (region == null)
                continue;

            if (!region
                    .ContainsWorldPosition(
                        transform.position))
            {
                continue;
            }

            navigationRegion =
                region;

            return true;
        }

        navigationRegion =
            null;

        return false;
    }

    private void ClearPathOnly()
    {
        currentPath =
            null;

        waypointIndex =
            0;
    }

#if UNITY_EDITOR

    private void OnDrawGizmosSelected()
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
