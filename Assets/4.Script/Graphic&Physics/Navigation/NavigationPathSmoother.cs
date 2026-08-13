using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Tar en rå grid-path och reducerar den till ett mindre antal
/// waypoints genom line-of-travel-tester med agentens clearance.
/// </summary>
public static class NavigationPathSmoother
{
    public static List<Vector2> Smooth(
    NavigationRegion region,
    IReadOnlyList<Vector2> rawPoints)
    {
        List<Vector2> result =
            new();

        if (region == null ||
            rawPoints == null ||
            rawPoints.Count == 0)
        {
            return result;
        }

        if (rawPoints.Count == 1)
        {
            result.Add(
                rawPoints[0]
            );

            return result;
        }

        int anchorIndex =
            0;

        result.Add(
            rawPoints[
                anchorIndex
            ]
        );

        while (anchorIndex <
               rawPoints.Count - 1)
        {
            int furthestVisible =
                -1;

            /*
             * Börja från slutet och hitta den längsta waypoint
             * som verkligen går att nå med full navigation-clearance.
             */
            for (int candidate =
                     rawPoints.Count - 1;
                 candidate >
                     anchorIndex;
                 candidate--)
            {
                if (!region
                        .IsDirectPathClear(
                            rawPoints[
                                anchorIndex
                            ],
                            rawPoints[
                                candidate
                            ]
                        ))
                {
                    continue;
                }

                furthestVisible =
                    candidate;

                break;
            }

            /*
             * Om inte ens nästa råa waypoint går att nå är
             * pathen geometriskt inkonsekvent.
             *
             * Vi bryter istället för att skapa en unsafe segment.
             */
            if (furthestVisible < 0)
            {
                break;
            }

            Vector2 nextPoint =
                rawPoints[
                    furthestVisible
                ];

            Vector2 previousPoint =
                result[
                    result.Count -
                    1
                ];

            if (
                (
                    nextPoint -
                    previousPoint
                ).sqrMagnitude >
                0.0001f
            )
            {
                result.Add(
                    nextPoint
                );
            }

            anchorIndex =
                furthestVisible;
        }

        return result;
    }
}
