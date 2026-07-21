using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Ritar den aktiva action-previewns targetingform.
///
/// Formen använder samma TargetingResult som execution:
/// - Origin
/// - TargetPoint
/// - Direction
/// - Distance
/// - AbilityTargetingSettings
/// </summary>
[RequireComponent(typeof(CharacterActionController))]
public sealed class ActionPreviewRenderer :
    MonoBehaviour
{
    [Header("Colors")]

    [SerializeField]
    private Color validFillColor =
        new Color(
            0.2f,
            0.8f,
            1f,
            0.22f
        );

    [SerializeField]
    private Color validOutlineColor =
        new Color(
            0.35f,
            0.9f,
            1f,
            0.95f
        );

    [SerializeField]
    private Color invalidFillColor =
        new Color(
            1f,
            0.15f,
            0.15f,
            0.22f
        );

    [SerializeField]
    private Color invalidOutlineColor =
        new Color(
            1f,
            0.25f,
            0.25f,
            0.95f
        );

    [Header("Shape Quality")]

    [SerializeField]
    [Range(12, 128)]
    private int circleSegments = 48;

    [SerializeField]
    [Range(4, 128)]
    private int coneSegments = 32;

    [SerializeField]
    [Min(0.01f)]
    private float pointRadius = 0.15f;

    [SerializeField]
    [Min(0.01f)]
    private float selfRadius = 0.5f;

    [SerializeField]
    [Min(0.001f)]
    private float outlineWidth = 0.04f;

    [Header("Rendering")]

    [SerializeField]
    private string sortingLayerName =
        "Default";

    [SerializeField]
    private int sortingOrder = 100;

    private CharacterActionController actionController;

    private GameObject previewRoot;

    private MeshFilter meshFilter;
    private MeshRenderer meshRenderer;
    private LineRenderer outlineRenderer;

    private Mesh previewMesh;

    private Material fillMaterial;
    private Material outlineMaterial;

    private readonly List<Vector3> meshVertices =
        new();

    private readonly List<int> meshTriangles =
        new();

    private readonly List<Vector3> outlinePoints =
        new();

    private void Awake()
    {
        actionController =
            GetComponent<CharacterActionController>();

        CreateRenderingObjects();
        HidePreview();
    }

    private void OnEnable()
    {
        if (actionController == null)
        {
            actionController =
                GetComponent<CharacterActionController>();
        }

        if (actionController == null)
            return;

        actionController.OnPreviewStarted +=
            HandlePreviewStarted;

        actionController.OnTargetingUpdated +=
            HandleTargetingUpdated;

        actionController.OnPhaseChanged +=
            HandlePhaseChanged;

        actionController.OnActionCancelled +=
            HandleActionEnded;

        actionController.OnActionCompleted +=
            HandleActionEnded;
    }

    private void OnDisable()
    {
        if (actionController != null)
        {
            actionController.OnPreviewStarted -=
                HandlePreviewStarted;

            actionController.OnTargetingUpdated -=
                HandleTargetingUpdated;

            actionController.OnPhaseChanged -=
                HandlePhaseChanged;

            actionController.OnActionCancelled -=
                HandleActionEnded;

            actionController.OnActionCompleted -=
                HandleActionEnded;
        }

        HidePreview();
    }

    private void OnDestroy()
    {
        if (previewMesh != null)
        {
            Destroy(
                previewMesh
            );
        }

        if (fillMaterial != null)
        {
            Destroy(
                fillMaterial
            );
        }

        if (outlineMaterial != null)
        {
            Destroy(
                outlineMaterial
            );
        }

        if (previewRoot != null)
        {
            Destroy(
                previewRoot
            );
        }
    }

    private void OnValidate()
    {
        circleSegments =
            Mathf.Clamp(
                circleSegments,
                12,
                128
            );

        coneSegments =
            Mathf.Clamp(
                coneSegments,
                4,
                128
            );

        pointRadius =
            Mathf.Max(
                0.01f,
                pointRadius
            );

        selfRadius =
            Mathf.Max(
                0.01f,
                selfRadius
            );

        outlineWidth =
            Mathf.Max(
                0.001f,
                outlineWidth
            );
    }

    private void HandlePreviewStarted(
        ActionContext context)
    {
        RenderPreview(
            context
        );
    }

    private void HandleTargetingUpdated(
        ActionContext context)
    {
        if (context == null ||
            context.Phase != ActionPhase.Preview)
        {
            return;
        }

        RenderPreview(
            context
        );
    }

    private void HandlePhaseChanged(
        ActionContext context,
        ActionPhase phase)
    {
        if (phase != ActionPhase.Preview)
        {
            HidePreview();
        }
    }

    private void HandleActionEnded(
        ActionContext context)
    {
        HidePreview();
    }

    private void CreateRenderingObjects()
    {
        previewRoot =
            new GameObject(
                "Action Preview Runtime"
            );

        previewRoot.transform.SetParent(
            transform,
            false
        );

        GameObject fillObject =
            new GameObject(
                "Fill"
            );

        fillObject.transform.SetParent(
            previewRoot.transform,
            false
        );

        meshFilter =
            fillObject.AddComponent<MeshFilter>();

        meshRenderer =
            fillObject.AddComponent<MeshRenderer>();

        GameObject outlineObject =
            new GameObject(
                "Outline"
            );

        outlineObject.transform.SetParent(
            previewRoot.transform,
            false
        );

        outlineRenderer =
            outlineObject.AddComponent<LineRenderer>();

        Shader spriteShader =
            Shader.Find(
                "Sprites/Default"
            );

        if (spriteShader == null)
        {
            Debug.LogError(
                "ActionPreviewRenderer kunde inte hitta " +
                "shadern 'Sprites/Default'.",
                this
            );

            return;
        }

        fillMaterial =
            new Material(
                spriteShader
            );

        fillMaterial.name =
            "Action Preview Fill Runtime";

        outlineMaterial =
            new Material(
                spriteShader
            );

        outlineMaterial.name =
            "Action Preview Outline Runtime";

        meshRenderer.material =
            fillMaterial;

        meshRenderer.sortingLayerName =
            sortingLayerName;

        meshRenderer.sortingOrder =
            sortingOrder;

        outlineRenderer.material =
            outlineMaterial;

        outlineRenderer.sortingLayerName =
            sortingLayerName;

        outlineRenderer.sortingOrder =
            sortingOrder + 1;

        outlineRenderer.useWorldSpace =
            false;

        outlineRenderer.loop =
            true;

        outlineRenderer.startWidth =
            outlineWidth;

        outlineRenderer.endWidth =
            outlineWidth;

        outlineRenderer.numCapVertices =
            4;

        outlineRenderer.numCornerVertices =
            4;

        previewMesh =
            new Mesh();

        previewMesh.name =
            "Action Preview Mesh Runtime";

        meshFilter.sharedMesh =
            previewMesh;
    }

    private void RenderPreview(
        ActionContext context)
    {
        if (context == null ||
            context.Targeting == null ||
            context.Targeting.Settings == null)
        {
            HidePreview();
            return;
        }

        TargetingResult targeting =
            context.Targeting;

        AbilityTargetingSettings settings =
            targeting.Settings;

        ClearGeometry();

        switch (settings.TargetingMode)
        {
            case TargetingMode.Self:
                BuildCircle(
                    targeting.Origin,
                    selfRadius,
                    circleSegments
                );
                break;

            case TargetingMode.SingleTarget:
                BuildSingleTarget(
                    targeting
                );
                break;

            case TargetingMode.Point:
                BuildCircle(
                    targeting.TargetPoint,
                    pointRadius,
                    circleSegments
                );
                break;

            case TargetingMode.Circle:
                BuildCircle(
                    targeting.TargetPoint,
                    settings.Radius,
                    circleSegments
                );
                break;

            case TargetingMode.Cone:
                BuildCone(
                    targeting.Origin,
                    targeting.Direction,
                    settings.Range,
                    settings.ConeAngle
                );
                break;

            case TargetingMode.Line:
                BuildLine(
                    targeting.Origin,
                    targeting.Direction,
                    targeting.Distance,
                    settings.LineWidth
                );
                break;
        }

        ApplyGeometry();

        ApplyColors(
            targeting.IsValid
        );

        previewRoot.SetActive(
            true
        );
    }

    private void BuildSingleTarget(
        TargetingResult targeting)
    {
        GameObject target =
            targeting.PrimaryTarget;

        if (target == null &&
            targeting.GeometryTargets.Count > 0)
        {
            target =
                targeting.GeometryTargets[0];
        }

        if (target == null)
        {
            BuildCircle(
                targeting.TargetPoint,
                pointRadius,
                circleSegments
            );

            return;
        }

        if (!TryGetTargetBounds(
                target,
                out Bounds bounds))
        {
            BuildCircle(
                targeting.TargetPoint,
                selfRadius,
                circleSegments
            );

            return;
        }

        float radius =
            Mathf.Max(
                bounds.extents.x,
                bounds.extents.y,
                pointRadius
            );

        BuildCircle(
            bounds.center,
            radius * 1.1f,
            circleSegments
        );
    }

    private void BuildCircle(
        Vector2 worldCenter,
        float radius,
        int segments)
    {
        radius =
            Mathf.Max(
                0.01f,
                radius
            );

        segments =
            Mathf.Max(
                3,
                segments
            );

        Vector3 center =
            WorldToPreviewLocal(
                worldCenter
            );

        meshVertices.Add(
            center
        );

        for (int i = 0;
             i < segments;
             i++)
        {
            float angle =
                i /
                (float)segments *
                Mathf.PI *
                2f;

            Vector2 worldPoint =
                worldCenter +
                new Vector2(
                    Mathf.Cos(angle),
                    Mathf.Sin(angle)
                ) * radius;

            Vector3 localPoint =
                WorldToPreviewLocal(
                    worldPoint
                );

            meshVertices.Add(
                localPoint
            );

            outlinePoints.Add(
                localPoint
            );
        }

        for (int i = 0;
             i < segments;
             i++)
        {
            int current =
                i + 1;

            int next =
                ((i + 1) % segments) + 1;

            meshTriangles.Add(0);
            meshTriangles.Add(current);
            meshTriangles.Add(next);
        }
    }

    private void BuildCone(
        Vector2 worldOrigin,
        Vector2 direction,
        float range,
        float totalAngle)
    {
        range =
            Mathf.Max(
                0.01f,
                range
            );

        totalAngle =
            Mathf.Clamp(
                totalAngle,
                0f,
                360f
            );

        if (totalAngle >= 359.9f)
        {
            BuildCircle(
                worldOrigin,
                range,
                circleSegments
            );

            return;
        }

        direction =
            GetSafeDirection(
                direction
            );

        int segments =
            Mathf.Max(
                2,
                coneSegments
            );

        float centerAngle =
            Mathf.Atan2(
                direction.y,
                direction.x
            ) * Mathf.Rad2Deg;

        float startAngle =
            centerAngle -
            totalAngle * 0.5f;

        Vector3 localOrigin =
            WorldToPreviewLocal(
                worldOrigin
            );

        meshVertices.Add(
            localOrigin
        );

        outlinePoints.Add(
            localOrigin
        );

        for (int i = 0;
             i <= segments;
             i++)
        {
            float progress =
                i /
                (float)segments;

            float angleDegrees =
                Mathf.Lerp(
                    startAngle,
                    startAngle + totalAngle,
                    progress
                );

            float angleRadians =
                angleDegrees *
                Mathf.Deg2Rad;

            Vector2 worldPoint =
                worldOrigin +
                new Vector2(
                    Mathf.Cos(angleRadians),
                    Mathf.Sin(angleRadians)
                ) * range;

            Vector3 localPoint =
                WorldToPreviewLocal(
                    worldPoint
                );

            meshVertices.Add(
                localPoint
            );

            outlinePoints.Add(
                localPoint
            );
        }

        for (int i = 0;
             i < segments;
             i++)
        {
            meshTriangles.Add(0);
            meshTriangles.Add(i + 1);
            meshTriangles.Add(i + 2);
        }
    }

    private void BuildLine(
        Vector2 worldOrigin,
        Vector2 direction,
        float length,
        float width)
    {
        length =
            Mathf.Max(
                0.01f,
                length
            );

        width =
            Mathf.Max(
                0.01f,
                width
            );

        direction =
            GetSafeDirection(
                direction
            );

        Vector2 perpendicular =
            new Vector2(
                -direction.y,
                direction.x
            );

        float halfWidth =
            width * 0.5f;

        Vector2 worldEnd =
            worldOrigin +
            direction * length;

        Vector2 point0 =
            worldOrigin +
            perpendicular * halfWidth;

        Vector2 point1 =
            worldEnd +
            perpendicular * halfWidth;

        Vector2 point2 =
            worldEnd -
            perpendicular * halfWidth;

        Vector2 point3 =
            worldOrigin -
            perpendicular * halfWidth;

        Vector3 local0 =
            WorldToPreviewLocal(
                point0
            );

        Vector3 local1 =
            WorldToPreviewLocal(
                point1
            );

        Vector3 local2 =
            WorldToPreviewLocal(
                point2
            );

        Vector3 local3 =
            WorldToPreviewLocal(
                point3
            );

        meshVertices.Add(local0);
        meshVertices.Add(local1);
        meshVertices.Add(local2);
        meshVertices.Add(local3);

        meshTriangles.Add(0);
        meshTriangles.Add(1);
        meshTriangles.Add(2);

        meshTriangles.Add(0);
        meshTriangles.Add(2);
        meshTriangles.Add(3);

        outlinePoints.Add(local0);
        outlinePoints.Add(local1);
        outlinePoints.Add(local2);
        outlinePoints.Add(local3);
    }

    private void ApplyGeometry()
    {
        previewMesh.Clear();

        previewMesh.SetVertices(
            meshVertices
        );

        previewMesh.SetTriangles(
            meshTriangles,
            0
        );

        previewMesh.RecalculateBounds();

        outlineRenderer.positionCount =
            outlinePoints.Count;

        for (int i = 0;
             i < outlinePoints.Count;
             i++)
        {
            outlineRenderer.SetPosition(
                i,
                outlinePoints[i]
            );
        }

        outlineRenderer.loop =
            outlinePoints.Count >= 3;
    }

    private void ApplyColors(
        bool isValid)
    {
        Color fillColor =
            isValid
                ? validFillColor
                : invalidFillColor;

        Color outlineColor =
            isValid
                ? validOutlineColor
                : invalidOutlineColor;

        if (fillMaterial != null)
        {
            fillMaterial.color =
                fillColor;
        }

        if (outlineMaterial != null)
        {
            outlineMaterial.color =
                outlineColor;
        }

        outlineRenderer.startColor =
            outlineColor;

        outlineRenderer.endColor =
            outlineColor;

        outlineRenderer.startWidth =
            outlineWidth;

        outlineRenderer.endWidth =
            outlineWidth;
    }

    private void ClearGeometry()
    {
        meshVertices.Clear();
        meshTriangles.Clear();
        outlinePoints.Clear();
    }

    private void HidePreview()
    {
        if (previewRoot != null)
        {
            previewRoot.SetActive(
                false
            );
        }

        if (previewMesh != null)
        {
            previewMesh.Clear();
        }

        if (outlineRenderer != null)
        {
            outlineRenderer.positionCount =
                0;
        }

        ClearGeometry();
    }

    private Vector3 WorldToPreviewLocal(
        Vector2 worldPoint)
    {
        return previewRoot.transform
            .InverseTransformPoint(
                new Vector3(
                    worldPoint.x,
                    worldPoint.y,
                    0f
                )
            );
    }

    private static Vector2 GetSafeDirection(
        Vector2 direction)
    {
        if (direction.sqrMagnitude <=
            Mathf.Epsilon)
        {
            return Vector2.down;
        }

        return direction.normalized;
    }

    private static bool TryGetTargetBounds(
        GameObject target,
        out Bounds bounds)
    {
        bounds = new Bounds();

        if (target == null)
            return false;

        SpriteRenderer[] spriteRenderers =
            target.GetComponentsInChildren<
                SpriteRenderer
            >(true);

        bool hasBounds = false;

        for (int i = 0;
             i < spriteRenderers.Length;
             i++)
        {
            SpriteRenderer spriteRenderer =
                spriteRenderers[i];

            if (spriteRenderer == null ||
                spriteRenderer.sprite == null)
            {
                continue;
            }

            if (!hasBounds)
            {
                bounds =
                    spriteRenderer.bounds;

                hasBounds = true;
            }
            else
            {
                bounds.Encapsulate(
                    spriteRenderer.bounds
                );
            }
        }

        if (hasBounds)
            return true;

        Collider2D[] colliders =
            target.GetComponentsInChildren<
                Collider2D
            >(true);

        for (int i = 0;
             i < colliders.Length;
             i++)
        {
            Collider2D targetCollider =
                colliders[i];

            if (targetCollider == null)
                continue;

            if (!hasBounds)
            {
                bounds =
                    targetCollider.bounds;

                hasBounds = true;
            }
            else
            {
                bounds.Encapsulate(
                    targetCollider.bounds
                );
            }
        }

        return hasBounds;
    }
}
