using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(
    fileName = "New World Scatter Palette",
    menuName = "World/Scatter Palette"
)]
public class WorldScatterPalette :
    ScriptableObject
{
    [SerializeField]
    private List<ScatterGroup> groups =
        new();

    public IReadOnlyList<ScatterGroup> Groups =>
        groups;
}

[Serializable]
public class ScatterGroup
{
    [SerializeField]
    private bool enabled =
        true;

    [SerializeField]
    private string groupName =
        "New Group";

    [SerializeField]
    [Range(0f, 1f)]
    private float density =
        1f;

    [SerializeField]
    private List<GameObject> prefabs =
        new();

    public bool Enabled =>
        enabled;

    public string GroupName =>
        groupName;

    public float Density =>
        Mathf.Clamp01(
            density
        );

    public IReadOnlyList<GameObject> Prefabs =>
        prefabs;

    public GameObject GetRandomPrefab()
    {
        if (prefabs == null ||
            prefabs.Count == 0)
        {
            return null;
        }

        int validCount = 0;

        for (int i = 0;
             i < prefabs.Count;
             i++)
        {
            if (prefabs[i] != null)
            {
                validCount++;
            }
        }

        if (validCount == 0)
            return null;

        int targetIndex =
            UnityEngine.Random.Range(
                0,
                validCount
            );

        int currentIndex = 0;

        for (int i = 0;
             i < prefabs.Count;
             i++)
        {
            GameObject prefab =
                prefabs[i];

            if (prefab == null)
                continue;

            if (currentIndex ==
                targetIndex)
            {
                return prefab;
            }

            currentIndex++;
        }

        return null;
    }
}