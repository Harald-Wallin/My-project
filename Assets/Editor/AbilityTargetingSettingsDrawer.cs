#if UNITY_EDITOR

using UnityEditor;
using UnityEngine;

[CustomPropertyDrawer(
    typeof(AbilityTargetingSettings)
)]
public sealed class
    AbilityTargetingSettingsDrawer :
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

        float y =
            position.y;

        SerializedProperty targetingMode =
            property.FindPropertyRelative(
                "targetingMode"
            );

        Draw(
            ref y,
            position,
            targetingMode,
            new GUIContent(
                "Targeting Mode"
            )
        );

        Draw(
            ref y,
            position,
            property.FindPropertyRelative(
                "allowedRelations"
            ),
            new GUIContent(
                "Allowed Relations"
            )
        );

        TargetingMode selectedMode =
            (TargetingMode)
            targetingMode.enumValueIndex;

        if (selectedMode !=
            TargetingMode.Self)
        {
            Draw(
                ref y,
                position,
                property.FindPropertyRelative(
                    "range"
                ),
                new GUIContent(
                    "Range"
                )
            );

            Draw(
                ref y,
                position,
                property.FindPropertyRelative(
                    "minimumRange"
                ),
                new GUIContent(
                    "Minimum Range"
                )
            );
        }

        switch (selectedMode)
        {
            case TargetingMode.Circle:

                Draw(
                    ref y,
                    position,
                    property.FindPropertyRelative(
                        "radius"
                    ),
                    new GUIContent(
                        "Radius"
                    )
                );
                break;

            case TargetingMode.Cone:

                Draw(
                    ref y,
                    position,
                    property.FindPropertyRelative(
                        "coneAngle"
                    ),
                    new GUIContent(
                        "Cone Angle"
                    )
                );
                break;

            case TargetingMode.Line:

                Draw(
                    ref y,
                    position,
                    property.FindPropertyRelative(
                        "lineWidth"
                    ),
                    new GUIContent(
                        "Line Width"
                    )
                );

                Draw(
                    ref y,
                    position,
                    property.FindPropertyRelative(
                        "lineLengthMode"
                    ),
                    new GUIContent(
                        "Line Length Mode"
                    )
                );
                break;
        }

        Draw(
            ref y,
            position,
            property.FindPropertyRelative(
                "selectionMode"
            ),
            new GUIContent(
                "Selection Mode"
            )
        );

        Draw(
            ref y,
            position,
            property.FindPropertyRelative(
                "maximumTargets"
            ),
            new GUIContent(
                "Maximum Targets"
            )
        );

        Draw(
            ref y,
            position,
            property.FindPropertyRelative(
                "requiresAffectedTarget"
            ),
            new GUIContent(
                "Requires Affected Target"
            )
        );

        Draw(
            ref y,
            position,
            property.FindPropertyRelative(
                "targetLayers"
            ),
            new GUIContent(
                "Target Layers"
            )
        );

        Draw(
            ref y,
            position,
            property.FindPropertyRelative(
                "lineOfSight"
            ),
            new GUIContent(
                "Line Of Sight"
            )
        );

        EditorGUI.EndProperty();
    }

    public override float GetPropertyHeight(
        SerializedProperty property,
        GUIContent label)
    {
        float height = 0f;

        SerializedProperty targetingMode =
            property.FindPropertyRelative(
                "targetingMode"
            );

        AddHeight(
            ref height,
            targetingMode
        );

        AddHeight(
            ref height,
            property.FindPropertyRelative(
                "allowedRelations"
            )
        );

        TargetingMode selectedMode =
            (TargetingMode)
            targetingMode.enumValueIndex;

        if (selectedMode !=
            TargetingMode.Self)
        {
            AddHeight(
                ref height,
                property.FindPropertyRelative(
                    "range"
                )
            );

            AddHeight(
                ref height,
                property.FindPropertyRelative(
                    "minimumRange"
                )
            );
        }

        switch (selectedMode)
        {
            case TargetingMode.Circle:

                AddHeight(
                    ref height,
                    property.FindPropertyRelative(
                        "radius"
                    )
                );
                break;

            case TargetingMode.Cone:

                AddHeight(
                    ref height,
                    property.FindPropertyRelative(
                        "coneAngle"
                    )
                );
                break;

            case TargetingMode.Line:

                AddHeight(
                    ref height,
                    property.FindPropertyRelative(
                        "lineWidth"
                    )
                );

                AddHeight(
                    ref height,
                    property.FindPropertyRelative(
                        "lineLengthMode"
                    )
                );
                break;
        }

        AddHeight(
            ref height,
            property.FindPropertyRelative(
                "selectionMode"
            )
        );

        AddHeight(
            ref height,
            property.FindPropertyRelative(
                "maximumTargets"
            )
        );

        AddHeight(
            ref height,
            property.FindPropertyRelative(
                "requiresAffectedTarget"
            )
        );

        AddHeight(
            ref height,
            property.FindPropertyRelative(
                "targetLayers"
            )
        );

        AddHeight(
            ref height,
            property.FindPropertyRelative(
                "lineOfSight"
            )
        );

        return height;
    }

    private static void Draw(
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

    private static void AddHeight(
        ref float total,
        SerializedProperty property)
    {
        total +=
            EditorGUI.GetPropertyHeight(
                property,
                true
            ) +
            Spacing;
    }
}

#endif
