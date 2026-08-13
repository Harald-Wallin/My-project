using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Optimerad A*-pathfinder för NavigationRegion.
///
/// Viktiga egenskaper:
/// - binary min-heap istället för linjär open-list
/// - inga SearchNode-objekt per sökning
/// - återanvänder intern search-memory mellan paths
/// - generations-ID gör att stora arrays inte behöver nollställas
///   inför varje sökning
/// - behåller samma publika API som tidigare
///
/// OBS:
/// Den interna workspace:n är avsedd för synkrona path-requests
/// på Unitys main thread.
///
/// När NavigationPathScheduler införs kommer requests fortfarande
/// kunna exekveras sekventiellt genom denna pathfinder.
/// </summary>
public static class AStarPathfinder
{
    private const float DiagonalCost =
        1.41421356f;

    // =========================================================
    // SEARCH DATA
    // =========================================================

    private struct SearchRecord
    {
        public float GCost;
        public float HCost;

        public int ParentIndex;

        public int HeapIndex;

        public int SearchGeneration;

        public bool Closed;

        public float FCost =>
            GCost +
            HCost;
    }

    /// <summary>
    /// Delad runtime-memory för A*.
    ///
    /// Arrayerna växer endast när en större region behöver
    /// sökas och återanvänds därefter.
    /// </summary>
    private sealed class SearchWorkspace
    {
        private SearchRecord[] records =
            System.Array.Empty<SearchRecord>();

        private int[] heap =
            System.Array.Empty<int>();

        private int heapCount;

        private int generation;

        private int columns;

        public int HeapCount =>
            heapCount;

        public void BeginSearch(
            int requiredCapacity,
            int gridColumns)
        {
            columns =
                Mathf.Max(
                    1,
                    gridColumns
                );

            EnsureCapacity(
                requiredCapacity
            );

            generation++;

            /*
             * Extrem edge case:
             *
             * Om generation skulle overflowa börjar vi om.
             * Detta händer först efter miljarder searches,
             * men gör systemet tekniskt robust.
             */
            if (generation <= 0)
            {
                System.Array.Clear(
                    records,
                    0,
                    records.Length
                );

                generation =
                    1;
            }

            heapCount =
                0;
        }

        public ref SearchRecord GetRecord(
            int index)
        {
            ref SearchRecord record =
                ref records[index];

            if (record.SearchGeneration !=
                generation)
            {
                record.GCost =
                    float.PositiveInfinity;

                record.HCost =
                    0f;

                record.ParentIndex =
                    -1;

                record.HeapIndex =
                    -1;

                record.SearchGeneration =
                    generation;

                record.Closed =
                    false;
            }

            return ref record;
        }

        public bool HasRecord(
            int index)
        {
            return
                records[index]
                    .SearchGeneration ==
                generation;
        }

        // =====================================================
        // HEAP
        // =====================================================

        public void AddOrUpdateOpen(
            int cellIndex)
        {
            ref SearchRecord record =
                ref GetRecord(
                    cellIndex
                );

            /*
             * Redan i heapen:
             * GCost kan precis ha minskat.
             *
             * Då behöver noden bara bubbla upp.
             */
            if (record.HeapIndex >= 0)
            {
                BubbleUp(
                    record.HeapIndex
                );

                return;
            }

            EnsureHeapCapacity(
                heapCount + 1
            );

            int heapIndex =
                heapCount;

            heap[heapIndex] =
                cellIndex;

            record.HeapIndex =
                heapIndex;

            heapCount++;

            BubbleUp(
                heapIndex
            );
        }

        public int PopBest()
        {
            if (heapCount <= 0)
                return -1;

            int bestCellIndex =
                heap[0];

            ref SearchRecord bestRecord =
                ref GetRecord(
                    bestCellIndex
                );

            bestRecord.HeapIndex =
                -1;

            heapCount--;

            if (heapCount <= 0)
            {
                return bestCellIndex;
            }

            int replacement =
                heap[heapCount];

            heap[0] =
                replacement;

            ref SearchRecord replacementRecord =
                ref GetRecord(
                    replacement
                );

            replacementRecord.HeapIndex =
                0;

            BubbleDown(
                0
            );

            return bestCellIndex;
        }

        private void BubbleUp(
            int heapIndex)
        {
            while (heapIndex > 0)
            {
                int parentIndex =
                    (
                        heapIndex -
                        1
                    ) /
                    2;

                int currentCell =
                    heap[heapIndex];

                int parentCell =
                    heap[parentIndex];

                if (!IsBetter(
                        currentCell,
                        parentCell))
                {
                    break;
                }

                SwapHeapEntries(
                    heapIndex,
                    parentIndex
                );

                heapIndex =
                    parentIndex;
            }
        }

        private void BubbleDown(
            int heapIndex)
        {
            while (true)
            {
                int leftChild =
                    heapIndex *
                    2 +
                    1;

                if (leftChild >=
                    heapCount)
                {
                    return;
                }

                int rightChild =
                    leftChild +
                    1;

                int bestChild =
                    leftChild;

                if (rightChild <
                        heapCount &&
                    IsBetter(
                        heap[rightChild],
                        heap[leftChild]))
                {
                    bestChild =
                        rightChild;
                }

                if (!IsBetter(
                        heap[bestChild],
                        heap[heapIndex]))
                {
                    return;
                }

                SwapHeapEntries(
                    heapIndex,
                    bestChild
                );

                heapIndex =
                    bestChild;
            }
        }

        private bool IsBetter(
            int firstCellIndex,
            int secondCellIndex)
        {
            ref SearchRecord first =
                ref GetRecord(
                    firstCellIndex
                );

            ref SearchRecord second =
                ref GetRecord(
                    secondCellIndex
                );

            float firstF =
                first.FCost;

            float secondF =
                second.FCost;

            if (firstF <
                secondF)
            {
                return true;
            }

            if (firstF >
                secondF)
            {
                return false;
            }

            /*
             * Samma FCost:
             *
             * Föredra lägre HCost.
             *
             * Detta brukar ge något rakare och mer målmedvetna
             * paths när flera alternativ är lika billiga.
             */
            return
                first.HCost <
                second.HCost;
        }

        private void SwapHeapEntries(
            int firstHeapIndex,
            int secondHeapIndex)
        {
            int firstCell =
                heap[firstHeapIndex];

            int secondCell =
                heap[secondHeapIndex];

            heap[firstHeapIndex] =
                secondCell;

            heap[secondHeapIndex] =
                firstCell;

            ref SearchRecord firstRecord =
                ref GetRecord(
                    firstCell
                );

            ref SearchRecord secondRecord =
                ref GetRecord(
                    secondCell
                );

            firstRecord.HeapIndex =
                secondHeapIndex;

            secondRecord.HeapIndex =
                firstHeapIndex;
        }

        // =====================================================
        // INDEX HELPERS
        // =====================================================

        public int ToIndex(
            int x,
            int y)
        {
            return
                y *
                columns +
                x;
        }

        public int GetX(
            int index)
        {
            return
                index %
                columns;
        }

        public int GetY(
            int index)
        {
            return
                index /
                columns;
        }

        // =====================================================
        // CAPACITY
        // =====================================================

        private void EnsureCapacity(
            int requiredCapacity)
        {
            requiredCapacity =
                Mathf.Max(
                    1,
                    requiredCapacity
                );

            if (records.Length <
                requiredCapacity)
            {
                int newCapacity =
                    GetExpandedCapacity(
                        records.Length,
                        requiredCapacity
                    );

                System.Array.Resize(
                    ref records,
                    newCapacity
                );
            }

            EnsureHeapCapacity(
                requiredCapacity
            );
        }

        private void EnsureHeapCapacity(
            int requiredCapacity)
        {
            if (heap.Length >=
                requiredCapacity)
            {
                return;
            }

            int newCapacity =
                GetExpandedCapacity(
                    heap.Length,
                    requiredCapacity
                );

            System.Array.Resize(
                ref heap,
                newCapacity
            );
        }

        private static int GetExpandedCapacity(
            int currentCapacity,
            int requiredCapacity)
        {
            int capacity =
                Mathf.Max(
                    64,
                    currentCapacity
                );

            while (capacity <
                   requiredCapacity)
            {
                capacity *=
                    2;
            }

            return capacity;
        }
    }

    private static readonly
        SearchWorkspace Workspace =
            new();

    // =========================================================
    // DIRECTIONS
    // =========================================================

    private static readonly Vector2Int[]
        CardinalDirections =
        {
            new Vector2Int(
                1,
                0
            ),

            new Vector2Int(
                -1,
                0
            ),

            new Vector2Int(
                0,
                1
            ),

            new Vector2Int(
                0,
                -1
            )
        };

    private static readonly Vector2Int[]
        DiagonalDirections =
        {
            new Vector2Int(
                1,
                1
            ),

            new Vector2Int(
                1,
                -1
            ),

            new Vector2Int(
                -1,
                1
            ),

            new Vector2Int(
                -1,
                -1
            )
        };

    // =========================================================
    // PUBLIC API
    // =========================================================

    public static NavigationPath FindPath(
        NavigationRegion region,
        Vector2 startWorldPosition,
        Vector2 targetWorldPosition,
        bool allowDirectFastPath = true)
    {
        NavigationPath path =
            new();

        if (region == null)
            return path;

        // -----------------------------------------------------
        // FAST PATH
        // -----------------------------------------------------

        /*
         * Om destinationen redan går att nå direkt behövs
         * varken grid-search eller heap.
         */
        if (allowDirectFastPath &&
            region.IsDirectPathClear(
            startWorldPosition,
            targetWorldPosition))
        {
            Vector2[] directPoints =
            {
                startWorldPosition,
                targetWorldPosition
            };

            path.SetRawPoints(
                directPoints
            );

            path.SetPoints(
                directPoints
            );

            return path;
        }

        // -----------------------------------------------------
        // START / TARGET CELLS
        // -----------------------------------------------------

        if (!region
                .TryGetClosestWalkableCell(
                    startWorldPosition,
                    out NavigationCell start))
        {
            return path;
        }

        if (!region
                .TryGetClosestWalkableCell(
                    targetWorldPosition,
                    out NavigationCell target))
        {
            return path;
        }

        int totalCells =
            region.Columns *
            region.Rows;

        Workspace.BeginSearch(
            totalCells,
            region.Columns
        );

        int startIndex =
            Workspace.ToIndex(
                start.X,
                start.Y
            );

        int targetIndex =
            Workspace.ToIndex(
                target.X,
                target.Y
            );

        // -----------------------------------------------------
        // INITIALIZE START
        // -----------------------------------------------------

        ref SearchRecord startRecord =
            ref Workspace.GetRecord(
                startIndex
            );

        startRecord.GCost =
            0f;

        startRecord.HCost =
            Heuristic(
                start.X,
                start.Y,
                target.X,
                target.Y,
                region.AllowDiagonalMovement
            );

        startRecord.ParentIndex =
            -1;

        Workspace.AddOrUpdateOpen(
            startIndex
        );

        // -----------------------------------------------------
        // SEARCH
        // -----------------------------------------------------

        bool foundDestination =
            false;

        while (Workspace.HeapCount > 0)
        {
            int currentIndex =
                Workspace.PopBest();

            if (currentIndex < 0)
                break;

            ref SearchRecord currentRecord =
                ref Workspace.GetRecord(
                    currentIndex
                );

            if (currentRecord.Closed)
                continue;

            currentRecord.Closed =
                true;

            if (currentIndex ==
                targetIndex)
            {
                foundDestination =
                    true;

                break;
            }

            int currentX =
                Workspace.GetX(
                    currentIndex
                );

            int currentY =
                Workspace.GetY(
                    currentIndex
                );

            EvaluateCardinalNeighbours(
                region,
                currentIndex,
                currentX,
                currentY,
                target
            );

            if (region.AllowDiagonalMovement)
            {
                EvaluateDiagonalNeighbours(
                    region,
                    currentIndex,
                    currentX,
                    currentY,
                    target
                );
            }
        }

        if (!foundDestination)
            return path;

        // -----------------------------------------------------
        // BUILD RAW PATH
        // -----------------------------------------------------

        List<Vector2> rawPoints =
            BuildWorldPath(
                region,
                startIndex,
                targetIndex,
                startWorldPosition,
                targetWorldPosition
            );

        if (rawPoints.Count == 0)
            return path;

        path.SetRawPoints(
            rawPoints
        );

        // -----------------------------------------------------
        // SMOOTH
        // -----------------------------------------------------

        List<Vector2> smoothed =
    NavigationPathSmoother.Smooth(
        region,
        rawPoints
    );

        /*
         * En special situation kan uppstå när NPC:n fysiskt står
         * närmare World-geometri än regionens NavigationRadius.
         *
         * A* kan då fortfarande hitta en korrekt grid-path från
         * närmaste walkable startcell, men path-smoothing kan inte
         * verifiera segmentet från NPC:ns exakta position till den
         * första gridcellen med full navigation-clearance.
         *
         * Smoothern får då inte förvandla en fungerande rå A*-path
         * till en oanvändbar enpunkts-path.
         */
        if (smoothed == null ||
            smoothed.Count < 2)
        {
            /*
             * Behåll den råa grid-pathen.
             *
             * NPCMovement står fortfarande för den slutliga fysiska
             * Rigidbody-kollisionskontrollen, så NPC:n kan aldrig
             * klippa genom World bara för att smoothing misslyckades.
             */
            path.SetPoints(
                rawPoints
            );
        }
        else
        {
            path.SetPoints(
                smoothed
            );
        }

        return path;
    }

    // =========================================================
    // CARDINAL NEIGHBOURS
    // =========================================================

    private static void
        EvaluateCardinalNeighbours(
            NavigationRegion region,
            int currentIndex,
            int currentX,
            int currentY,
            NavigationCell target)
    {
        for (int i = 0;
             i <
             CardinalDirections.Length;
             i++)
        {
            Vector2Int offset =
                CardinalDirections[i];

            int neighbourX =
                currentX +
                offset.x;

            int neighbourY =
                currentY +
                offset.y;

            TryEvaluateNeighbour(
                region,
                currentIndex,
                neighbourX,
                neighbourY,
                target,
                1f
            );
        }
    }

    // =========================================================
    // DIAGONAL NEIGHBOURS
    // =========================================================

    private static void
        EvaluateDiagonalNeighbours(
            NavigationRegion region,
            int currentIndex,
            int currentX,
            int currentY,
            NavigationCell target)
    {
        for (int i = 0;
             i <
             DiagonalDirections.Length;
             i++)
        {
            Vector2Int offset =
                DiagonalDirections[i];

            int neighbourX =
                currentX +
                offset.x;

            int neighbourY =
                currentY +
                offset.y;

            /*
             * Förhindrar att agenten "klipper" diagonalt
             * mellan blockerade hörn.
             */
            if (region.PreventCornerCutting)
            {
                bool horizontalClear =
                    region.IsWalkable(
                        currentX +
                        offset.x,
                        currentY
                    );

                bool verticalClear =
                    region.IsWalkable(
                        currentX,
                        currentY +
                        offset.y
                    );

                if (!horizontalClear ||
                    !verticalClear)
                {
                    continue;
                }
            }

            TryEvaluateNeighbour(
                region,
                currentIndex,
                neighbourX,
                neighbourY,
                target,
                DiagonalCost
            );
        }
    }

    // =========================================================
    // NEIGHBOUR EVALUATION
    // =========================================================

    private static void
        TryEvaluateNeighbour(
            NavigationRegion region,
            int currentIndex,
            int neighbourX,
            int neighbourY,
            NavigationCell target,
            float movementCost)
    {
        if (!region.IsWalkable(
                neighbourX,
                neighbourY))
        {
            return;
        }

        int neighbourIndex =
            Workspace.ToIndex(
                neighbourX,
                neighbourY
            );

        ref SearchRecord neighbour =
            ref Workspace.GetRecord(
                neighbourIndex
            );

        if (neighbour.Closed)
            return;

        ref SearchRecord current =
            ref Workspace.GetRecord(
                currentIndex
            );

        float tentativeG =
            current.GCost +
            movementCost;

        if (tentativeG >=
            neighbour.GCost)
        {
            return;
        }

        neighbour.ParentIndex =
            currentIndex;

        neighbour.GCost =
            tentativeG;

        neighbour.HCost =
            Heuristic(
                neighbourX,
                neighbourY,
                target.X,
                target.Y,
                region.AllowDiagonalMovement
            );

        /*
         * Om noden redan finns i heapen gör detta endast
         * en decrease-key/bubble-up.
         *
         * Ingen List.Contains behövs.
         */
        Workspace.AddOrUpdateOpen(
            neighbourIndex
        );
    }

    // =========================================================
    // HEURISTIC
    // =========================================================

    private static float Heuristic(
        int x,
        int y,
        int targetX,
        int targetY,
        bool allowDiagonalMovement)
    {
        int dx =
            Mathf.Abs(
                targetX -
                x
            );

        int dy =
            Mathf.Abs(
                targetY -
                y
            );

        /*
         * Utan diagonaler använder vi Manhattan distance.
         */
        if (!allowDiagonalMovement)
        {
            return
                dx +
                dy;
        }

        /*
         * Med diagonaler använder vi octile distance.
         */
        int diagonal =
            Mathf.Min(
                dx,
                dy
            );

        int straight =
            Mathf.Max(
                dx,
                dy
            ) -
            diagonal;

        return
            diagonal *
            DiagonalCost +
            straight;
    }

    // =========================================================
    // BUILD WORLD PATH
    // =========================================================

    private static List<Vector2>
        BuildWorldPath(
            NavigationRegion region,
            int startIndex,
            int destinationIndex,
            Vector2 exactStart,
            Vector2 exactTarget)
    {
        List<Vector2> reversed =
            new();

        int currentIndex =
            destinationIndex;

        /*
         * Säkerhetsgräns så en trasig parent-chain aldrig
         * kan skapa en oändlig loop.
         */
        int maximumSteps =
            region.Columns *
            region.Rows;

        int steps =
            0;

        while (currentIndex >= 0 &&
               steps <
               maximumSteps)
        {
            int x =
                Workspace.GetX(
                    currentIndex
                );

            int y =
                Workspace.GetY(
                    currentIndex
                );

            reversed.Add(
                region.GridToWorld(
                    x,
                    y
                )
            );

            if (currentIndex ==
                startIndex)
            {
                break;
            }

            ref SearchRecord record =
                ref Workspace.GetRecord(
                    currentIndex
                );

            currentIndex =
                record.ParentIndex;

            steps++;
        }

        if (reversed.Count == 0)
        {
            return new List<Vector2>();
        }

        reversed.Reverse();

        List<Vector2> result =
            new(
                reversed.Count +
                2
            );

        result.Add(
            exactStart
        );

        for (int i = 0;
             i < reversed.Count;
             i++)
        {
            Vector2 point =
                reversed[i];

            Vector2 previous =
                result[
                    result.Count -
                    1
                ];

            if ((point - previous)
                    .sqrMagnitude <=
                0.0001f)
            {
                continue;
            }

            result.Add(
                point
            );
        }

        /*
         * Försök använda den exakta destinationen.
         *
         * Om target befinner sig inne i blockerad World-geometri
         * avslutar vi istället vid närmaste walkable cell.
         *
         * Det förhindrar att en NPC hittar en korrekt A*-path och
         * sedan i sista steget försöker springa rakt in i väggen.
         */
        Vector2 lastPoint =
            result[
                result.Count -
                1
            ];

        if ((exactTarget - lastPoint).sqrMagnitude > 0.0001f &&
            region.IsDirectPathClear(lastPoint,exactTarget))
        {
            result.Add(exactTarget);
        }

        return result;
    }
}