using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Central scheduler för navigation-pathfinding.
///
/// NPCNavigationAgent begär paths härifrån istället för
/// att köra AStarPathfinder direkt.
///
/// Syftet är att:
/// - begränsa antal A*-sökningar per frame
/// - undvika stora CPU-spikes när många NPC:er repathar samtidigt
/// - slå ihop flera väntande requests från samma NPC
/// - skapa en central punkt för framtida pathfinding-budgetering
/// </summary>
public sealed class NavigationPathScheduler :
    MonoBehaviour
{
    // =========================================================
    // SINGLETON
    // =========================================================

    private static NavigationPathScheduler instance;

    public static NavigationPathScheduler Instance
    {
        get
        {
            if (instance != null)
                return instance;

            instance =
                FindFirstObjectByType<
                    NavigationPathScheduler>();

            if (instance != null)
                return instance;

            GameObject runtimeObject =
                new GameObject(
                    "Navigation Path Scheduler Runtime"
                );

            instance =
                runtimeObject.AddComponent<
                    NavigationPathScheduler>();

            return instance;
        }
    }

    // =========================================================
    // SETTINGS
    // =========================================================

    [Header("Path Budget")]

    [SerializeField]
    [Min(1)]
    [Tooltip(
        "Maximalt antal A*-sökningar som får köras per frame."
    )]
    private int maximumPathsPerFrame =
        2;

    [SerializeField]
    [Min(0.1f)]
    [Tooltip(
        "Extra säkerhetsbudget i millisekunder. " +
        "Schedulern slutar processa paths denna frame " +
        "om budgeten har överskridits."
    )]
    private float maximumMillisecondsPerFrame =
        2f;

    [Header("Debug")]

    [SerializeField]
    private bool showDebugInfo;

    // =========================================================
    // REQUEST
    // =========================================================

    private sealed class PathRequest
    {
        public NPCNavigationAgent Agent;
        public bool ForceGridPath;
        public NavigationRegion Region;

        public Vector2 Start;
        public Vector2 Destination;

        public int Version;
    }

    /*
     * Queuen innehåller endast agenten.
     *
     * Själva senaste requesten ligger i pendingRequests.
     *
     * Om samma NPC ändrar destination medan den väntar
     * ersätter vi alltså request-data istället för att lägga
     * ännu en post i kön.
     */
    private readonly Queue<
        NPCNavigationAgent>
        requestQueue =
            new();

    private readonly Dictionary<
        NPCNavigationAgent,
        PathRequest>
        pendingRequests =
            new();

    // =========================================================
    // UNITY
    // =========================================================

    private void Awake()
    {
        if (instance != null &&
            instance != this)
        {
            Destroy(
                gameObject
            );

            return;
        }

        instance =
            this;
    }

    private void OnDestroy()
    {
        if (instance == this)
        {
            instance =
                null;
        }
    }

    private void OnValidate()
    {
        maximumPathsPerFrame =
            Mathf.Max(
                1,
                maximumPathsPerFrame
            );

        maximumMillisecondsPerFrame =
            Mathf.Max(
                0.1f,
                maximumMillisecondsPerFrame
            );
    }

    private void Update()
    {
        ProcessRequests();
    }

    // =========================================================
    // REQUEST API
    // =========================================================

    public void RequestPath(
        NPCNavigationAgent agent,
        NavigationRegion region,
        Vector2 start,
        Vector2 destination,
        int version,
        bool forceGridPath = false)
    {
        if (agent == null ||
            region == null)
        {
            return;
        }

        bool alreadyQueued =
            pendingRequests.ContainsKey(
                agent
            );

        PathRequest request =
            new PathRequest
            {
                Agent = agent,

                Region = region,

                Start = start,

                Destination = destination,

                Version = version, 

                ForceGridPath = forceGridPath
            };

        /*
         * Viktigt:
         *
         * Finns NPC:n redan i kön ersätter vi bara requesten.
         *
         * Exempel:
         *
         * target position 10,5
         * target position 10.5,5
         * target position 11,5
         *
         * innan schedulern hunnit processa NPC:n
         *
         * => bara den senaste destinationen används.
         */
        pendingRequests[
            agent
        ] =
            request;

        if (!alreadyQueued)
        {
            requestQueue.Enqueue(
                agent
            );
        }
    }

    public void CancelRequests(
        NPCNavigationAgent agent)
    {
        if (agent == null)
            return;

        /*
         * Vi behöver inte försöka ta bort agenten ur Queue<T>.
         *
         * När den senare poppas finns ingen request kvar
         * i dictionaryn och posten ignoreras.
         */
        pendingRequests.Remove(
            agent
        );
    }

    // =========================================================
    // PROCESSING
    // =========================================================

    private void ProcessRequests()
    {
        if (requestQueue.Count == 0)
            return;

        int processed =
            0;

        float startTime =
            Time.realtimeSinceStartup;

        while (requestQueue.Count > 0 &&
               processed <
               maximumPathsPerFrame)
        {
            float elapsedMilliseconds =
                (
                    Time.realtimeSinceStartup -
                    startTime
                ) *
                1000f;

            /*
             * Kör alltid åtminstone en request om kön innehåller
             * arbete.
             *
             * Därefter respekteras tidsbudgeten.
             */
            if (processed > 0 &&
                elapsedMilliseconds >=
                maximumMillisecondsPerFrame)
            {
                break;
            }

            NPCNavigationAgent agent =
                requestQueue.Dequeue();

            if (agent == null)
            {
                continue;
            }

            if (!pendingRequests
                    .TryGetValue(
                        agent,
                        out PathRequest request))
            {
                continue;
            }

            pendingRequests.Remove(
                agent
            );

            ProcessRequest(
                request
            );

            processed++;
        }

        if (showDebugInfo &&
            processed > 0)
        {
            float elapsedMilliseconds =
                (
                    Time.realtimeSinceStartup -
                    startTime
                ) *
                1000f;

            Debug.Log(
                $"Navigation scheduler: " +
                $"{processed} paths, " +
                $"{elapsedMilliseconds:F2} ms, " +
                $"{requestQueue.Count} queued.",
                this
            );
        }
    }

    private static void ProcessRequest(
        PathRequest request)
    {
        if (request == null ||
            request.Agent == null ||
            request.Region == null)
        {
            return;
        }

        NavigationPath path =
            AStarPathfinder.FindPath(
            request.Region,
            request.Start,
            request.Destination,
            allowDirectFastPath:
            !request.ForceGridPath
        );

        request.Agent
            .ReceiveScheduledPath(
                path,
                request.Destination,
                request.Version
            );
    }

    // =========================================================
    // DEBUG
    // =========================================================

    public int PendingRequestCount =>
        pendingRequests.Count;

    public int QueueCount =>
        requestQueue.Count;
}
