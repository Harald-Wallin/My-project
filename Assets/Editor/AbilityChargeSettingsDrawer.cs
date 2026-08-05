#if UNITY_EDITOR

using UnityEditor;
using UnityEngine;

[CustomPropertyDrawer(
    typeof(AbilityChargeSettings)
)]
public sealed class
    AbilityChargeSettingsDrawer :
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

        SerializedProperty scalingMode =
            property.FindPropertyRelative(
                "scalingMode"
            );

        SerializedProperty damageMultiplier =
            property.FindPropertyRelative(
                "minimumDamageMultiplier"
            );

        SerializedProperty rangeMultiplier =
            property.FindPropertyRelative(
                "minimumRangeMultiplier"
            );

        float y =
            position.y;

        Draw(
            ref y,
            position,
            scalingMode,
            new GUIContent(
                "Scaling Mode"
            )
        );

        ChargeScalingMode selectedMode =
            (ChargeScalingMode)
            scalingMode.intValue;

        if ((selectedMode &
             ChargeScalingMode.Damage) != 0)
        {
            Draw(
                ref y,
                position,
                damageMultiplier,
                new GUIContent(
                    "Minimum Damage Multiplier"
                )
            );
        }

        if ((selectedMode &
             ChargeScalingMode.Range) != 0)
        {
            Draw(
                ref y,
                position,
                rangeMultiplier,
                new GUIContent(
                    "Minimum Range Multiplier"
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
            Height(
                property.FindPropertyRelative(
                    "scalingMode"
                )
            );

        ChargeScalingMode selectedMode =
            (ChargeScalingMode)
            property
                .FindPropertyRelative(
                    "scalingMode"
                )
                .intValue;

        if ((selectedMode &
             ChargeScalingMode.Damage) != 0)
        {
            height +=
                Height(
                    property
                        .FindPropertyRelative(
                            "minimumDamageMultiplier"
                        )
                );
        }

        if ((selectedMode &
             ChargeScalingMode.Range) != 0)
        {
            height +=
                Height(
                    property
                        .FindPropertyRelative(
                            "minimumRangeMultiplier"
                        )
                );
        }

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

        EditorGUI.PropertyField(
            new Rect(
                totalPosition.x,
                y,
                totalPosition.width,
                height
            ),
            property,
            label,
            true
        );

        y +=
            height +
            Spacing;
    }

    private static float Height(
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
