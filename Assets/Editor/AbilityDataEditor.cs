#if UNITY_EDITOR

using UnityEditor;
using UnityEngine;

[CustomEditor(
    typeof(AbilityData),
    true
)]
public sealed class AbilityDataEditor :
    Editor
{
    private SerializedProperty abilityName;
    private SerializedProperty description;
    private SerializedProperty icon;
    private SerializedProperty types;
    private SerializedProperty usageType;
    private SerializedProperty tags;

    private SerializedProperty previewMode;
    private SerializedProperty targetingSettings;
    private SerializedProperty timingSettings;
    private SerializedProperty executionSettings;
    private SerializedProperty chargeSettings;
    private SerializedProperty chargeCompletionEffect;

    private SerializedProperty wardCost;

    private SerializedProperty alwaysHits;
    private SerializedProperty canCrit;
    private SerializedProperty canMiss;
    private SerializedProperty requiresHitCheck;
    private SerializedProperty entersCombatState;

    private SerializedProperty effects;

    private SerializedProperty cooldown;
    private SerializedProperty globalCooldown;
    private SerializedProperty castTime;
    private SerializedProperty isSelfCast;

    private bool showLegacyConfiguration;

    private void OnEnable()
    {
        abilityName =
            serializedObject.FindProperty(
                "abilityName"
            );

        description =
            serializedObject.FindProperty(
                "description"
            );

        icon =
            serializedObject.FindProperty(
                "icon"
            );

        types =
            serializedObject.FindProperty(
                "types"
            );

        usageType =
            serializedObject.FindProperty(
                "usageType"
            );

        tags =
            serializedObject.FindProperty(
                "tags"
            );

        previewMode =
            serializedObject.FindProperty(
                "previewMode"
            );

        targetingSettings =
            serializedObject.FindProperty(
                "targetingSettings"
            );

        timingSettings =
            serializedObject.FindProperty(
                "timingSettings"
            );

        executionSettings =
            serializedObject.FindProperty(
                "executionSettings"
            );

        chargeSettings =
            serializedObject.FindProperty(
                "chargeSettings"
            );

        chargeCompletionEffect =
            serializedObject.FindProperty(
                "chargeCompletionPreviewEffect"
            );

        wardCost =
            serializedObject.FindProperty(
                "wardCost"
            );

        alwaysHits =
            serializedObject.FindProperty(
                "alwaysHits"
            );

        canCrit =
            serializedObject.FindProperty(
                "canCrit"
            );

        canMiss =
            serializedObject.FindProperty(
                "canMiss"
            );

        requiresHitCheck =
            serializedObject.FindProperty(
                "requiresHitCheck"
            );

        entersCombatState =
            serializedObject.FindProperty(
                "entersCombatState"
            );

        effects =
            serializedObject.FindProperty(
                "effects"
            );

        cooldown =
            serializedObject.FindProperty(
                "cooldown"
            );

        globalCooldown =
            serializedObject.FindProperty(
                "globalCooldown"
            );

        castTime =
            serializedObject.FindProperty(
                "castTime"
            );

        isSelfCast =
            serializedObject.FindProperty(
                "isSelfCast"
            );
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        DrawSectionHeader(
            "Identity"
        );

        EditorGUILayout.PropertyField(
            abilityName
        );

        EditorGUILayout.PropertyField(
            description
        );

        EditorGUILayout.PropertyField(
            icon
        );

        EditorGUILayout.Space();

        DrawSectionHeader(
            "Classification"
        );

        EditorGUILayout.PropertyField(
            types
        );

        EditorGUILayout.PropertyField(
            usageType
        );

        EditorGUILayout.PropertyField(
            tags,
            true
        );

        EditorGUILayout.Space();

        DrawSectionHeader(
            "Targeting"
        );

        EditorGUILayout.PropertyField(
            previewMode
        );

        EditorGUILayout.PropertyField(
            targetingSettings,
            new GUIContent(
                "Targeting Settings"
            ),
            true
        );

        EditorGUILayout.Space();

        DrawSectionHeader(
            "Timing"
        );

        EditorGUILayout.PropertyField(
            timingSettings,
            new GUIContent(
                "Timing Settings"
            ),
            true
        );

        ActionTimingType timingType =
            ReadTimingType();

        EditorGUILayout.Space();

        DrawSectionHeader(
            "Execution"
        );

        EditorGUILayout.PropertyField(
            executionSettings,
            new GUIContent(
                "Execution Settings"
            ),
            true
        );

        if (timingType ==
            ActionTimingType.Charge)
        {
            EditorGUILayout.Space();

            DrawSectionHeader(
                "Charge"
            );

            EditorGUILayout.PropertyField(
                chargeSettings,
                new GUIContent(
                    "Charge Settings"
                ),
                true
            );

            EditorGUILayout.PropertyField(
                chargeCompletionEffect,
                new GUIContent(
                    "Full Charge Preview Effect"
                ),
                true
            );
        }

        EditorGUILayout.Space();

        DrawSectionHeader(
            "Costs"
        );

        EditorGUILayout.PropertyField(
            wardCost
        );

        EditorGUILayout.Space();

        DrawSectionHeader(
            "Combat Rules"
        );

        EditorGUILayout.PropertyField(
            alwaysHits
        );

        EditorGUILayout.PropertyField(
            canCrit
        );

        EditorGUILayout.PropertyField(
            canMiss
        );

        EditorGUILayout.PropertyField(
            requiresHitCheck
        );

        EditorGUILayout.PropertyField(
            entersCombatState
        );

        EditorGUILayout.Space();

        DrawSectionHeader(
            "Effects"
        );

        EditorGUILayout.PropertyField(
            effects,
            true
        );

        EditorGUILayout.Space();

        DrawLegacySection();

        serializedObject
            .ApplyModifiedProperties();
    }

    private ActionTimingType ReadTimingType()
    {
        SerializedProperty typeProperty =
            timingSettings
                .FindPropertyRelative(
                    "timingType"
                );

        return
            (ActionTimingType)
            typeProperty.enumValueIndex;
    }

    private void DrawLegacySection()
    {
        EditorGUILayout.BeginVertical(
            EditorStyles.helpBox
        );

        showLegacyConfiguration =
            EditorGUILayout.Foldout(
                showLegacyConfiguration,
                "Legacy Configuration",
                true
            );

        if (showLegacyConfiguration)
        {
            EditorGUILayout.HelpBox(
                "Dessa värden används endast av äldre, " +
                "icke-migrerade abilityvägar. Det nya " +
                "CharacterActionController-systemet använder " +
                "Targeting, Timing och Execution ovan.",
                MessageType.Warning
            );

            EditorGUILayout.PropertyField(
                cooldown
            );

            EditorGUILayout.PropertyField(
                globalCooldown
            );

            EditorGUILayout.PropertyField(
                castTime
            );

            EditorGUILayout.PropertyField(
                isSelfCast
            );
        }

        EditorGUILayout.EndVertical();
    }

    private static void DrawSectionHeader(
        string title)
    {
        EditorGUILayout.LabelField(
            title,
            EditorStyles.boldLabel
        );
    }
}

#endif