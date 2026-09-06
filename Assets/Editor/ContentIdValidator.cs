using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public static class ContentIdValidator
{
    // =========================================================
    // MENU
    // =========================================================

    [MenuItem(
        "RPG/Validation/Validate Content IDs"
    )]
    public static void ValidateAllContentIds()
    {
        int errorCount = 0;

        errorCount +=
            ValidateType<ItemData>(
                "ItemData",
                item => item.Id
            );

        errorCount +=
            ValidateType<AbilityData>(
                "AbilityData",
                ability => ability.Id
            );

        errorCount +=
            ValidateType<TalentData>(
                "TalentData",
                talent => talent.Id
            );

        errorCount +=
            ValidateType<FavourData>(
                "FavourData",
                favour => favour.Id
            );

        errorCount +=
            ValidateType<CreatureDefinition>(
                "CreatureDefinition",
                creature => creature.Id
            );

        errorCount +=
            ValidateType<Faction>(
                "Faction",
                faction => faction.Id
            );

        if (errorCount == 0)
        {
            Debug.Log(
                "[CONTENT ID] Validation complete. " +
                "No duplicate or empty Content IDs found."
            );
        }
        else
        {
            Debug.LogError(
                $"[CONTENT ID] Validation found " +
                $"{errorCount} problem(s)."
            );
        }
    }

    // =========================================================
    // TYPE VALIDATION
    // =========================================================

    private static int ValidateType<T>(
        string typeName,
        Func<T, string> getId)
        where T : ScriptableObject
    {
        string[] guids =
            AssetDatabase.FindAssets(
                $"t:{typeof(T).Name}"
            );

        Dictionary<string, List<T>>
            assetsById =
                new(
                    StringComparer.Ordinal
                );

        int errorCount = 0;

        foreach (string guid in guids)
        {
            string path =
                AssetDatabase.GUIDToAssetPath(
                    guid
                );

            T asset =
                AssetDatabase.LoadAssetAtPath<T>(
                    path
                );

            if (asset == null)
                continue;

            string id =
                getId(asset);

            if (string.IsNullOrWhiteSpace(
                    id))
            {
                Debug.LogError(
                    $"[CONTENT ID] {typeName} " +
                    $"'{asset.name}' has an empty Content ID.\n" +
                    $"Asset: {path}",
                    asset
                );

                errorCount++;

                continue;
            }

            if (!assetsById.TryGetValue(
                    id,
                    out List<T> matchingAssets))
            {
                matchingAssets =
                    new List<T>();

                assetsById.Add(
                    id,
                    matchingAssets
                );
            }

            matchingAssets.Add(
                asset
            );
        }

        foreach (KeyValuePair<
                     string,
                     List<T>>
                 pair
                 in assetsById)
        {
            if (pair.Value.Count <= 1)
                continue;

            errorCount++;

            LogDuplicateGroup(
                typeName,
                pair.Key,
                pair.Value
            );
        }

        return errorCount;
    }

    // =========================================================
    // DUPLICATE LOGGING
    // =========================================================

    private static void LogDuplicateGroup<T>(
        string typeName,
        string id,
        List<T> assets)
        where T : ScriptableObject
    {
        string message =
            "[DUPLICATE CONTENT ID]\n\n" +
            $"Type: {typeName}\n" +
            $"ID: {id}\n\n" +
            "Assets:\n";

        foreach (T asset in assets)
        {
            if (asset == null)
                continue;

            string path =
                AssetDatabase.GetAssetPath(
                    asset
                );

            message +=
                $"- {asset.name}\n" +
                $"  {path}\n";
        }

        UnityEngine.Object context =
            assets.Count > 0
                ? assets[0]
                : null;

        Debug.LogError(
            message,
            context
        );
    }
}

// =============================================================
// AUTOMATIC VALIDATION
// =============================================================

public sealed class ContentIdAssetPostprocessor :
    AssetPostprocessor
{
    private static bool validationQueued;

    private static void OnPostprocessAllAssets(
        string[] importedAssets,
        string[] deletedAssets,
        string[] movedAssets,
        string[] movedFromAssetPaths)
    {
        if (ContainsRelevantAsset(
                importedAssets) ||
            ContainsRelevantAsset(
                movedAssets))
        {
            QueueValidation();
        }
    }

    private static bool ContainsRelevantAsset(
        string[] paths)
    {
        if (paths == null)
            return false;

        foreach (string path in paths)
        {
            if (string.IsNullOrWhiteSpace(
                    path))
            {
                continue;
            }

            ScriptableObject asset =
                AssetDatabase
                    .LoadAssetAtPath<
                        ScriptableObject>(
                        path
                    );

            if (asset == null)
                continue;

            if (asset is ItemData ||
                asset is AbilityData ||
                asset is TalentData ||
                asset is FavourData ||
                asset is CreatureDefinition ||
                asset is Faction)
            {
                return true;
            }
        }

        return false;
    }

    private static void QueueValidation()
    {
        if (validationQueued)
            return;

        validationQueued = true;

        EditorApplication.delayCall +=
            RunQueuedValidation;
    }

    private static void RunQueuedValidation()
    {
        validationQueued = false;

        ContentIdValidator
            .ValidateAllContentIds();
    }
}
