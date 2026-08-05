#if UNITY_EDITOR

using UnityEditor;
using UnityEngine;

[CustomPropertyDrawer(
    typeof(AbilityTimingSettings)
)]
public sealed class
    AbilityTimingSettingsDrawer :
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

        SerializedProperty timingType =
            property.FindPropertyRelative(
                "timingType"
            );

        SerializedProperty castDuration =
            property.FindPropertyRelative(
                "castDuration"
            );

        SerializedProperty channelDuration =
            property.FindPropertyRelative(
                "channelDuration"
            );

        SerializedProperty channelTickInterval =
            property.FindPropertyRelative(
                "channelTickInterval"
            );

        SerializedProperty maximumChargeDuration =
            property.FindPropertyRelative(
                "maximumChargeDuration"
            );

        SerializedProperty recoveryDuration =
            property.FindPropertyRelative(
                "recoveryDuration"
            );

        SerializedProperty castMovement =
            property.FindPropertyRelative(
                "castMovement"
            );

        SerializedProperty chargeMovement =
            property.FindPropertyRelative(
                "chargeMovement"
            );

        SerializedProperty channelMovement =
            property.FindPropertyRelative(
                "channelMovement"
            );

        SerializedProperty recoveryMovement =
            property.FindPropertyRelative(
                "recoveryMovement"
            );

        float y =
            position.y;

        DrawProperty(
            ref y,
            position,
            timingType,
            new GUIContent(
                "Timing Type"
            )
        );

        ActionTimingType selectedType =
            (ActionTimingType)
            timingType.enumValueIndex;

        switch (selectedType)
        {
            case ActionTimingType.Cast:

                DrawProperty(
                    ref y,
                    position,
                    castDuration,
                    new GUIContent(
                        "Cast Duration"
                    )
                );

                DrawProperty(
                    ref y,
                    position,
                    castMovement,
                    new GUIContent(
                        "Movement During Cast"
                    )
                );
                break;

            case ActionTimingType.Charge:

                DrawProperty(
                    ref y,
                    position,
                    maximumChargeDuration,
                    new GUIContent(
                        "Maximum Charge Duration"
                    )
                );

                DrawProperty(
                    ref y,
                    position,
                    chargeMovement,
                    new GUIContent(
                        "Movement During Charge"
                    )
                );
                break;

            case ActionTimingType.Channel:

                DrawProperty(
                    ref y,
                    position,
                    channelDuration,
                    new GUIContent(
                        "Channel Duration"
                    )
                );

                DrawProperty(
                    ref y,
                    position,
                    channelTickInterval,
                    new GUIContent(
                        "Channel Tick Interval"
                    )
                );

                DrawProperty(
                    ref y,
                    position,
                    channelMovement,
                    new GUIContent(
                        "Movement During Channel"
                    )
                );
                break;
        }

        DrawProperty(
            ref y,
            position,
            recoveryDuration,
            new GUIContent(
                "Recovery Duration"
            )
        );

        if (recoveryDuration.floatValue >
            0f)
        {
            DrawProperty(
                ref y,
                position,
                recoveryMovement,
                new GUIContent(
                    "Movement During Recovery"
                )
            );
        }

        EditorGUI.EndProperty();
    }

    public override float GetPropertyHeight(
        SerializedProperty property,
        GUIContent label)
    {
        float height =
            PropertyHeight(
                property.FindPropertyRelative(
                    "timingType"
                )
            );

        ActionTimingType selectedType =
            (ActionTimingType)
            property
                .FindPropertyRelative(
                    "timingType"
                )
                .enumValueIndex;

        switch (selectedType)
        {
            case ActionTimingType.Cast:

                height +=
                    PropertyHeight(
                        property
                            .FindPropertyRelative(
                                "castDuration"
                            )
                    );

                height +=
                    PropertyHeight(
                        property
                            .FindPropertyRelative(
                                "castMovement"
                            )
                    );
                break;

            case ActionTimingType.Charge:

                height +=
                    PropertyHeight(
                        property
                            .FindPropertyRelative(
                                "maximumChargeDuration"
                            )
                    );

                height +=
                    PropertyHeight(
                        property
                            .FindPropertyRelative(
                                "chargeMovement"
                            )
                    );
                break;

            case ActionTimingType.Channel:

                height +=
                    PropertyHeight(
                        property
                            .FindPropertyRelative(
                                "channelDuration"
                            )
                    );

                height +=
                    PropertyHeight(
                        property
                            .FindPropertyRelative(
                                "channelTickInterval"
                            )
                    );

                height +=
                    PropertyHeight(
                        property
                            .FindPropertyRelative(
                                "channelMovement"
                            )
                    );
                break;
        }

        SerializedProperty recoveryDuration =
            property.FindPropertyRelative(
                "recoveryDuration"
            );

        height +=
            PropertyHeight(
                recoveryDuration
            );

        if (recoveryDuration.floatValue >
            0f)
        {
            height +=
                PropertyHeight(
                    property
                        .FindPropertyRelative(
                            "recoveryMovement"
                        )
                );
        }

        return height;
    }

    private static void DrawProperty(
        ref float y,
        Rect totalPosition,
        SerializedProperty property,
        GUIContent label)
    {
        float height =
            EditorGUI.GetPropertyHeight(
                property,
                label,
                true
            );

        Rect rect =
            new Rect(
                totalPosition.x,
                y,
                totalPosition.width,
                height
            );

        EditorGUI.PropertyField(
            rect,
            property,
            label,
            true
        );

        y +=
            height +
            Spacing;
    }

    private static float PropertyHeight(
        SerializedProperty property)
    {
        return
            EditorGUI.GetPropertyHeight(
                property,
                true
            ) +
            Spacing;
    }
}

#endif
