using UnityEngine;

public static class LineOfSightUtility
{
    private static readonly int losMask = LayerMask.GetMask("World");

    public static bool HasLineOfSight(
        Vector2 from,
        Vector2 to
    )
    {
        Debug.DrawLine(from,to,Color.red,1f);

        Vector2 direction = to - from;

        float distance = direction.magnitude;

        if (distance <= 0.01f)
            return true;

        /*RaycastHit2D hit =
            Physics2D.Raycast(
                from,
                direction.normalized,
                distance,
                losMask
            );

        if (hit.collider != null)
        {
            Debug.Log(
                $"Ray hit: {hit.collider.name} | Layer = {LayerMask.LayerToName(hit.collider.gameObject.layer)}"
            );
        }*/

        RaycastHit2D[] hits =
    Physics2D.RaycastAll(
        from,
        direction.normalized,
        distance
    );

        foreach (var h in hits)
        {
            Debug.Log(
                $"LoS hit: {h.collider.name} | Layer={LayerMask.LayerToName(h.collider.gameObject.layer)}"
            );
        }

        //return hit.collider == null;
        return hits.Length == 0;
    }
}
