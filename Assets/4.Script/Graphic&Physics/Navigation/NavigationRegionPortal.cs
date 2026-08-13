using UnityEngine;

/// <summary>
/// Representerar en sammanhängande walkable passage mellan
/// två intilliggande NavigationRegions.
///
/// En portal är INTE ett GameObject.
///
/// Den innehåller endast runtime-data som world-navigationen
/// kan använda för att planera vägar mellan regions.
///
/// PointA:
/// Säker walkable punkt inne i RegionA.
///
/// PointB:
/// Säker walkable punkt inne i RegionB.
///
/// WorldCenter:
/// Mitten av själva regiongränsen.
/// </summary>
public sealed class NavigationRegionPortal
{
    // =========================================================
    // REGIONS
    // =========================================================

    public NavigationRegion RegionA
    {
        get;
    }

    public NavigationRegion RegionB
    {
        get;
    }

    // =========================================================
    // POSITIONS
    // =========================================================

    public Vector2 PointA
    {
        get;
    }

    public Vector2 PointB
    {
        get;
    }

    public Vector2 WorldCenter
    {
        get;
    }

    // =========================================================
    // PORTAL DATA
    // =========================================================

    /// <summary>
    /// Ungefärlig fysisk bredd på den sammanhängande öppningen.
    /// </summary>
    public float Width
    {
        get;
    }

    /// <summary>
    /// Grid-riktningen från RegionA till RegionB.
    ///
    /// Exempel:
    /// Vector2Int.right
    /// betyder att RegionB ligger öster om RegionA.
    /// </summary>
    public Vector2Int DirectionFromA
    {
        get;
    }

    // =========================================================
    // CONSTRUCTOR
    // =========================================================

    public NavigationRegionPortal(
        NavigationRegion regionA,
        NavigationRegion regionB,
        Vector2 pointA,
        Vector2 pointB,
        float width,
        Vector2Int directionFromA)
    {
        RegionA =
            regionA;

        RegionB =
            regionB;

        PointA =
            pointA;

        PointB =
            pointB;

        WorldCenter =
            (
                pointA +
                pointB
            ) *
            0.5f;

        Width =
            Mathf.Max(
                0f,
                width
            );

        DirectionFromA =
            directionFromA;
    }

    // =========================================================
    // QUERY
    // =========================================================

    public bool Connects(
        NavigationRegion region)
    {
        return
            region != null &&
            (
                region == RegionA ||
                region == RegionB
            );
    }

    public bool Connects(
        NavigationRegion first,
        NavigationRegion second)
    {
        if (first == null ||
            second == null)
        {
            return false;
        }

        return
            (
                RegionA == first &&
                RegionB == second
            ) ||
            (
                RegionA == second &&
                RegionB == first
            );
    }

    /// <summary>
    /// Returnerar regionen på andra sidan portalen.
    /// </summary>
    public NavigationRegion GetOtherRegion(
        NavigationRegion region)
    {
        if (region == RegionA)
            return RegionB;

        if (region == RegionB)
            return RegionA;

        return null;
    }

    /// <summary>
    /// Returnerar den walkable waypoint som ligger inne i
    /// den angivna regionen.
    /// </summary>
    public Vector2 GetPointForRegion(
        NavigationRegion region)
    {
        if (region == RegionA)
            return PointA;

        if (region == RegionB)
            return PointB;

        return WorldCenter;
    }

    /// <summary>
    /// Returnerar passage-riktningen sett från angiven region.
    /// </summary>
    public Vector2Int GetDirectionFrom(
        NavigationRegion region)
    {
        if (region == RegionA)
        {
            return DirectionFromA;
        }

        if (region == RegionB)
        {
            return -DirectionFromA;
        }

        return Vector2Int.zero;
    }
}
