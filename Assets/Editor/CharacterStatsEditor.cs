using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(CharacterStats), true)]
public class CharacterStatsEditor : Editor
{
    SerializedProperty statsProperty;
    SerializedProperty derivedStatsProperty;

    bool showPrimary = true;
    bool showDerived = true;

    void OnEnable()
    {
        statsProperty = serializedObject.FindProperty("stats");
        derivedStatsProperty = serializedObject.FindProperty("derivedStats");

        EnsureAllStatsExist();
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        DrawDefaultInspectorExceptStats();

        EditorGUILayout.Space(10);

        showPrimary = EditorGUILayout.BeginFoldoutHeaderGroup(showPrimary,"Primary Stats");

        if (showPrimary)
        {
            DrawStatsByKind(StatKind.Primary);
        }

        EditorGUILayout.EndFoldoutHeaderGroup();

        showDerived = EditorGUILayout.BeginFoldoutHeaderGroup(showDerived,"Derived Stats");

        if (showDerived)
        {
            DrawDerivedStats();
        }

        EditorGUILayout.EndFoldoutHeaderGroup();

        if (GUI.changed)
        {
            serializedObject.ApplyModifiedProperties();

            foreach (CharacterStats stats in targets)
            {
                stats.RecalculateDerivedStats();

                EditorUtility.SetDirty(stats);
            }

            serializedObject.Update();
        }

        serializedObject.ApplyModifiedProperties();
    }

    void DrawStatsByKind(StatKind kind)
    {
        foreach (var definition in StatDatabase.Instance.stats)
        {
            if (definition.kind != kind)
                continue;

            if (!definition.visible)
                continue;
            DrawStat(definition.stat);
        }
    }

    void DrawPlayerSection()
    {
        EditorGUILayout.Space();

        EditorGUILayout.LabelField(
            "Player",
            EditorStyles.boldLabel);

        DrawPropertiesExcluding(
            serializedObject,
            "m_Script",
            "stats",
            "derivedStats"
        );
    }

    void DrawDerivedStats()
    {
        GUI.enabled = false;

        for (int i = 0; i < derivedStatsProperty.arraySize; i++)
        {
            SerializedProperty stat =
                derivedStatsProperty.GetArrayElementAtIndex(i);

            EditorGUILayout.BeginHorizontal();

            StatType statType = (StatType)stat.FindPropertyRelative("stat").enumValueIndex;

            StatDefinition definition =
                StatDatabase.Instance.GetDefinition(statType);

            string label =
                definition != null
                ? definition.displayName
                : statType.ToString();

            EditorGUILayout.LabelField(
                label,
                GUILayout.Width(180));

            EditorGUILayout.FloatField(
                stat.FindPropertyRelative("value").floatValue);

            EditorGUILayout.EndHorizontal();
        }

        GUI.enabled = true;
    }

    void DrawDefaultInspectorExceptStats()
    {
        DrawPropertiesExcluding( serializedObject,"stats","derivedStats");
    }

    void DrawCategory(string title, params StatType[] statTypes)
    {
        EditorGUILayout.Space();
        EditorGUILayout.LabelField(title, EditorStyles.boldLabel);

        foreach (var stat in statTypes)
        {
            DrawStat(stat);
        }
    }

    void DrawStat(StatType stat)
    {
        SerializedProperty entry = FindEntry(stat);

        if (entry == null)
            return;

        SerializedProperty value =
            entry.FindPropertyRelative("value");

        StatDefinition definition =
            StatDatabase.Instance.GetDefinition(stat);

        string label =
            definition != null
            ? definition.displayName
            : stat.ToString();

        bool editable =
            definition == null ||
            definition.editable;

        GUI.enabled = editable;

        EditorGUILayout.PropertyField(
            value,
            new GUIContent(label)
        );

        GUI.enabled = true;
    }

    SerializedProperty FindEntry(StatType stat)
    {
        for (int i = 0; i < statsProperty.arraySize; i++)
        {
            SerializedProperty entry =
                statsProperty.GetArrayElementAtIndex(i);

            SerializedProperty statProp =
                entry.FindPropertyRelative("stat");

            if ((StatType)statProp.enumValueIndex == stat)
                return entry;
        }

        return null;
    }

    void EnsureAllStatsExist()
    {
        serializedObject.Update();

        HashSet<StatType> existing = new();

        for (int i = 0; i < statsProperty.arraySize; i++)
        {
            var entry = statsProperty.GetArrayElementAtIndex(i);

            existing.Add(
                (StatType)entry
                .FindPropertyRelative("stat")
                .enumValueIndex);
        }

        foreach (var definition in StatDatabase.Instance.stats)
        {
            if (definition.kind != StatKind.Primary)
                continue;

            if (existing.Contains(definition.stat))
                continue;

            int index = statsProperty.arraySize;

            statsProperty.InsertArrayElementAtIndex(index);

            SerializedProperty newEntry =
                statsProperty.GetArrayElementAtIndex(index);

            newEntry.FindPropertyRelative("stat").enumValueIndex =
                (int)definition.stat;

            newEntry.FindPropertyRelative("value").floatValue = 0;
        }

        serializedObject.ApplyModifiedProperties();
    }
}
