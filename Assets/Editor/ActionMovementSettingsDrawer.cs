#if UNITY_EDITOR

using UnityEditor;
using UnityEngine;

[CustomPropertyDrawer(
    typeof(ActionMovementSettings)
)]
public sealed class
    ActionMovementSettingsDrawer :
    PropertyDrawer
{
    private const float Spacing =
        2f;

    public override void OnGUI(
        Rect position,
        SerializedProperty property,
        GUIContent label)
    {
        EditorGUI.BeginProperty(
            position,
            label,
            property
        );

        SerializedProperty mode =
            property.FindPropertyRelative(
                "mode"
            );

        SerializedProperty multiplier =
            property.FindPropertyRelative(
                "speedMultiplier"
            );

        Rect modeRect =
            new Rect(
                position.x,
                position.y,
                position.width,
                EditorGUIUtility
                    .singleLineHeight
            );

        EditorGUI.PropertyField(
            modeRect,
            mode,
            label
        );

        ActionMovementMode selectedMode =
            (ActionMovementMode)
            mode.enumValueIndex;

        if (selectedMode ==
            ActionMovementMode
                .SpeedMultiplier)
        {
            Rect multiplierRect =
                new Rect(
                    position.x,
                    modeRect.yMax +
                    Spacing,
                    position.width,
                    EditorGUIUtility
                        .singleLineHeight
                );

            EditorGUI.indentLevel++;

            EditorGUI.PropertyField(
                multiplierRect,
                multiplier,
                new GUIContent(
                    "Speed Multiplier"
                )
            );

            EditorGUI.indentLevel--;
        }

        EditorGUI.EndProperty();
    }

    public override float GetPropertyHeight(
        SerializedProperty property,
        GUIContent label)
    {
        SerializedProperty mode =
            property.FindPropertyRelative(
                "mode"
            );

        ActionMovementMode selectedMode =
            (ActionMovementMode)
            mode.enumValueIndex;

        float height =
            EditorGUIUtility
                .singleLineHeight;

        if (selectedMode ==
            ActionMovementMode
                .SpeedMultiplier)
        {
            height +=
                Spacing +
                EditorGUIUtility
                    .singleLineHeight;
        }

        return height;
    }
}

#endif
