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

        Rect contentRect =
            EditorGUI.PrefixLabel(
                firstLine,
                label
            );

        const float clearWidth =
            22f;

        Rect dropRect =
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
                dropRect.xMax + 2f,
                contentRect.y,
                clearWidth,
                contentRect.height
            );

        string currentId =
            idProperty.stringValue;

        string currentName =
            nameProperty.stringValue;

        string buttonText;

        if (string.IsNullOrWhiteSpace(
                currentId))
        {
            buttonText =
                "Drag Entity here";
        }
        else if (!string.IsNullOrWhiteSpace(
                     currentName))
        {
            buttonText =
                $"{currentName} [{currentId}]";
        }
        else
        {
            buttonText =
                currentId;
        }

        GUI.Box(
            dropRect,
            buttonText,
            EditorStyles.objectField
        );

        HandleDragAndDrop(
            dropRect,
            idProperty,
            nameProperty
        );

        if (GUI.Button(
                clearRect,
                "×"))
        {
            idProperty.stringValue =
                string.Empty;

            nameProperty.stringValue =
                string.Empty;
        }

        using (
            new EditorGUI.DisabledScope(
                true
            ))
        {
            EditorGUI.TextField(
                secondLine,
                "Saved Entity ID",
                currentId
            );
        }

        EditorGUI.EndProperty();
    }

    private static void HandleDragAndDrop(
        Rect dropRect,
        SerializedProperty idProperty,
        SerializedProperty nameProperty)
    {
        Event currentEvent =
            Event.current;

        if (!dropRect.Contains(
                currentEvent.mousePosition))
        {
            return;
        }

        switch (currentEvent.type)
        {
            case EventType.DragUpdated:

                if (TryGetDraggedIdentity(
                        out _))
                {
                    DragAndDrop.visualMode =
                        DragAndDropVisualMode.Copy;

                    currentEvent.Use();
                }

                break;

            case EventType.DragPerform:

                if (!TryGetDraggedIdentity(
                        out EntityIdentity identity))
                {
                    return;
                }

                DragAndDrop.AcceptDrag();

                idProperty.stringValue =
                    identity.Id;

                nameProperty.stringValue =
                    identity.DisplayName;

                currentEvent.Use();

                break;
        }
    }

    private static bool TryGetDraggedIdentity(
        out EntityIdentity identity)
    {
        identity = null;

        if (DragAndDrop.objectReferences ==
            null)
        {
            return false;
        }

        foreach (Object draggedObject
                 in DragAndDrop.objectReferences)
        {
            identity =
                EntityTargetUtility
                    .GetIdentity(
                        draggedObject
                    );

            if (identity != null)
            {
                return true;
            }
        }

        return false;
    }
}

#endif
