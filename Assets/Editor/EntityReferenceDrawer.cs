#if UNITY_EDITOR

using UnityEditor;
using UnityEngine;

[CustomPropertyDrawer(
    typeof(EntityReference)
)]
public sealed class EntityReferenceDrawer :
    PropertyDrawer
{
    private const float Spacing =
        2f;

    public override float GetPropertyHeight(
        SerializedProperty property,
        GUIContent label)
    {
        return
            EditorGUIUtility.singleLineHeight *
            2f +
            Spacing;
    }

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

        SerializedProperty idProperty =
            property.FindPropertyRelative(
                "entityId"
            );

        SerializedProperty nameProperty =
            property.FindPropertyRelative(
                "displayName"
            );

        float lineHeight =
            EditorGUIUtility.singleLineHeight;

        Rect firstLine =
            new Rect(
                position.x,
                position.y,
                position.width,
                lineHeight
            );

        Rect secondLine =
            new Rect(
                position.x,
                position.y +
                lineHeight +
                Spacing,
                position.width,
                lineHeight
            );

        DrawEntityField(
            firstLine,
            label,
            idProperty,
            nameProperty
        );

        using (
            new EditorGUI.DisabledScope(
                true
            ))
        {
            EditorGUI.TextField(
                secondLine,
                "Saved Entity ID",
                idProperty.stringValue
            );
        }

        EditorGUI.EndProperty();
    }

    private static void DrawEntityField(
        Rect position,
        GUIContent label,
        SerializedProperty idProperty,
        SerializedProperty nameProperty)
    {
        Rect contentRect =
            EditorGUI.PrefixLabel(
                position,
                label
            );

        const float clearWidth =
            22f;

        Rect fieldRect =
            new Rect(
                contentRect.x,
                contentRect.y,
                contentRect.width -
                clearWidth -
                2f,
                contentRect.height
            );

        Rect clearRect =
            new Rect(
                fieldRect.xMax + 2f,
                contentRect.y,
                clearWidth,
                contentRect.height
            );

        string currentId =
            idProperty.stringValue;

        string currentName =
            nameProperty.stringValue;

        string displayText;

        if (string.IsNullOrWhiteSpace(
                currentId))
        {
            displayText =
                "None (Entity)";
        }
        else if (!string.IsNullOrWhiteSpace(
                     currentName))
        {
            displayText =
                $"{currentName} [{currentId}]";
        }
        else
        {
            displayText =
                currentId;
        }

        GUI.Box(
            fieldRect,
            displayText,
            EditorStyles.objectField
        );

        HandleDragAndDrop(
            fieldRect,
            idProperty,
            nameProperty
        );

        HandleObjectPickerClick(
            fieldRect,
            idProperty,
            nameProperty
        );

        if (GUI.Button(
                clearRect,
                "×"))
        {
            ClearReference(
                idProperty,
                nameProperty
            );
        }
    }

    private static void HandleDragAndDrop(
        Rect fieldRect,
        SerializedProperty idProperty,
        SerializedProperty nameProperty)
    {
        Event currentEvent =
            Event.current;

        if (currentEvent == null ||
            !fieldRect.Contains(
                currentEvent.mousePosition))
        {
            return;
        }

        if (currentEvent.type ==
            EventType.DragUpdated)
        {
            if (TryGetDraggedIdentity(
                    out _))
            {
                DragAndDrop.visualMode =
                    DragAndDropVisualMode.Copy;

                currentEvent.Use();
            }

            return;
        }

        if (currentEvent.type !=
            EventType.DragPerform)
        {
            return;
        }

        if (!TryGetDraggedIdentity(
                out EntityIdentity identity))
        {
            DragAndDrop.visualMode =
                DragAndDropVisualMode.Rejected;

            return;
        }

        DragAndDrop.AcceptDrag();

        AssignIdentity(
            identity,
            idProperty,
            nameProperty
        );

        currentEvent.Use();
    }

    private static void HandleObjectPickerClick(
        Rect fieldRect,
        SerializedProperty idProperty,
        SerializedProperty nameProperty)
    {
        Event currentEvent =
            Event.current;

        if (currentEvent == null ||
            currentEvent.type !=
                EventType.MouseDown ||
            currentEvent.button != 0 ||
            !fieldRect.Contains(
                currentEvent.mousePosition))
        {
            return;
        }

        /*
         * Ett vanligt ObjectField kan inte lagra scene objects
         * i ett ScriptableObject-asset.
         *
         * Därför använder vi drag-and-drop som den primära
         * authoring-metoden och sparar endast EntityIdentity-ID:t.
         *
         * Klick på ett redan konfigurerat fält gör därför
         * inget destruktivt.
         */
        currentEvent.Use();
    }

    private static bool TryGetDraggedIdentity(
        out EntityIdentity identity)
    {
        identity = null;

        Object[] references =
            DragAndDrop.objectReferences;

        if (references == null ||
            references.Length == 0)
        {
            return false;
        }

        foreach (Object draggedObject
                 in references)
        {
            EntityIdentity candidate =
                EntityTargetUtility
                    .GetIdentity(
                        draggedObject
                    );

            if (candidate == null ||
                string.IsNullOrWhiteSpace(
                    candidate.Id))
            {
                continue;
            }

            identity =
                candidate;

            return true;
        }

        return false;
    }

    private static void AssignIdentity(
        EntityIdentity identity,
        SerializedProperty idProperty,
        SerializedProperty nameProperty)
    {
        if (identity == null)
            return;

        idProperty.stringValue =
            identity.Id;

        nameProperty.stringValue =
            identity.DisplayName;

        idProperty.serializedObject
            .ApplyModifiedProperties();

        EditorUtility.SetDirty(
            idProperty.serializedObject
                .targetObject
        );
    }

    private static void ClearReference(
        SerializedProperty idProperty,
        SerializedProperty nameProperty)
    {
        idProperty.stringValue =
            string.Empty;

        nameProperty.stringValue =
            string.Empty;

        idProperty.serializedObject
            .ApplyModifiedProperties();

        EditorUtility.SetDirty(
            idProperty.serializedObject
                .targetObject
        );
    }
}

#endif