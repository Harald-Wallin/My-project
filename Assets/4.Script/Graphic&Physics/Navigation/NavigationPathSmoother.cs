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
        List<Vector2>
            result =
                new();

        if (region == null ||
            rawPoints == null ||
            rawPoints.Count == 0)
        {
            return result;
        }

        if (rawPoints.Count <= 2)
        {
            for (int i = 0;
                 i < rawPoints.Count;
                 i++)
            {
                result.Add(
                    rawPoints[i]
                );
            }

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
                anchorIndex + 1;

            for (int candidate =
                     rawPoints.Count - 1;
                 candidate >
                     anchorIndex + 1;
                 candidate--)
            {
                bool clear =
                    region
                        .IsDirectPathClear(
                            rawPoints[
                                anchorIndex
                            ],
                            rawPoints[
                                candidate
                            ]
                        );

                if (!clear)
                    continue;

                furthestVisible =
                    candidate;

                break;
            }

            Vector2 nextPoint =
                rawPoints[
                    furthestVisible
                ];

            if (
                (
                    nextPoint -
                    result[
                        result.Count -
                        1
                    ]
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
