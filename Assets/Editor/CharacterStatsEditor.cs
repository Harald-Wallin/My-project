using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(CharacterStats), true)]
public class CharacterStatsEditor : Editor
{
    private bool showPrimary = true;
    private bool showDerived = true;

    private void OnEnable()
    {
        RefreshTargets();
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        DrawPropertiesExcluding(
            serializedObject,
            "stats",
            "derivedStats"
        );

        EditorGUILayout.Space(10);

        StatDatabase database =
            StatDatabase.Instance;

        if (database == null)
        {
            EditorGUILayout.HelpBox(
                "StatDatabase kunde inte hittas i Resources/Stats " +
                "eller Resources.",
                MessageType.Error
            );

            serializedObject.ApplyModifiedProperties();
            return;
        }

        showPrimary =
            EditorGUILayout.BeginFoldoutHeaderGroup(
                showPrimary,
                "Primary Stats"
            );

        if (showPrimary)
        {
            DrawPrimaryStats(database);
        }

        EditorGUILayout.EndFoldoutHeaderGroup();

        showDerived =
            EditorGUILayout.BeginFoldoutHeaderGroup(
                showDerived,
                "Derived Stats"
            );

        if (showDerived)
        {
            DrawDerivedStats(database);
        }

        EditorGUILayout.EndFoldoutHeaderGroup();

        bool changed =
            serializedObject.ApplyModifiedProperties();

        if (changed)
        {
            RefreshTargets();
            serializedObject.Update();
        }
    }

    private void DrawPrimaryStats(
        StatDatabase database)
    {
        SerializedProperty statsProperty =
            serializedObject.FindProperty("stats");

        foreach (StatDefinition definition in database.Stats)
        {
            if (definition == null)
                continue;

            if (definition.kind != StatKind.Primary)
                continue;

            if (!definition.visible)
                continue;

            SerializedProperty entry =
                FindEntry(
                    statsProperty,
                    definition.stat
                );

            if (entry == null)
                continue;

            SerializedProperty value =
                entry.FindPropertyRelative("value");

            using (new EditorGUI.DisabledScope(
                       !definition.editable))
            {
                EditorGUILayout.PropertyField(
                    value,
                    new GUIContent(definition.DisplayName)
                );
            }
        }
    }

    private void DrawDerivedStats(
        StatDatabase database)
    {
        SerializedProperty derivedProperty =
            serializedObject.FindProperty("derivedStats");

        using (new EditorGUI.DisabledScope(true))
        {
            foreach (StatDefinition definition in database.Stats)
            {
                if (definition == null)
                    continue;

                if (definition.kind != StatKind.Derived)
                    continue;

                if (!definition.visible)
                    continue;

                SerializedProperty entry =
                    FindEntry(
                        derivedProperty,
                        definition.stat
                    );

                if (entry == null)
                    continue;

                SerializedProperty value =
                    entry.FindPropertyRelative("value");

                EditorGUILayout.FloatField(
                    definition.DisplayName,
                    value.floatValue
                );
            }
        }
    }

    private SerializedProperty FindEntry(
        SerializedProperty listProperty,
        StatType stat)
    {
        for (int i = 0;
             i < listProperty.arraySize;
             i++)
        {
            SerializedProperty entry =
                listProperty.GetArrayElementAtIndex(i);

            SerializedProperty statProperty =
                entry.FindPropertyRelative("stat");

            if ((StatType)statProperty.enumValueIndex == stat)
            {
                return entry;
            }
        }

        return null;
    }

    private void RefreshTargets()
    {
        foreach (Object inspectedTarget in targets)
        {
            if (inspectedTarget is not CharacterStats characterStats)
                continue;

            characterStats.RefreshStats();

            EditorUtility.SetDirty(characterStats);
        }
    }
}