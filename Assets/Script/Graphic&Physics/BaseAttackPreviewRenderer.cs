using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Permanent, diskret targeting-preview för spelarens
/// utrustade base attack.
///
/// Grundformen visar attackens fulla område.
/// En separat vit mesh fylls utåt när attacken blir redo.
///
/// Nuvarande implementation matchar BaseAttackControllers
/// legacy-targeting:
/// - range
/// - arcAngle
/// - riktning mot musen
/// </summary>
[RequireComponent(typeof(BaseAttackController))]
[RequireComponent(typeof(PlayerBaseAttackCollection))]
public sealed class BaseAttackPreviewRenderer :
    MonoBehaviour
{
    [Header("Visibility")]

    [SerializeField]
    private bool showOutsideCombat = true;

    [SerializeField]
    private bool hideDuringActionPreview = true;

    [Header("Passive Colors")]

    [SerializeField]
    private Color passiveFillColor =
        new Color(
            0.25f,
            0.8f,
            1f,
            0.07f
        );

    [SerializeField]
    private Color passiveOutlineColor =
        new Color(
            0.35f,
            0.9f,
            1f,
            0.22f
        );

    [Header("Combat Colors")]

    [SerializeField]
    private Color combatFillColor =
        new Color(
            0.25f,
            0.8f,
            1f,
            0.12f
        );

    [SerializeField]
    private Color combatOutlineColor =
        new Color(
            0.35f,
            0.9f,
            1f,
            0.38f
        );

    [Header("Cooldown / Readiness")]

    [SerializeField]
    private Color readinessFillColor =
        new Color(
            1f,
            1f,
            1f,
            0.16f
        );

    [SerializeField]
    [Tooltip(
        "Döljer den vita readiness-fyllningen när attacken " +
        "är helt redo."
    )]
    private bool hideReadinessFillWhenReady;

    [Header("Shape Quality")]

    [SerializeField]
    [Range(4, 128)]
    private int coneSegments = 32;

    [SerializeField]
    [Min(0.001f)]
    private float outlineWidth = 0.035f;

    [Header("Rendering")]

    [SerializeField]
    private string sortingLayerName =
        "Default";

    [SerializeField]
    private int sortingOrder = 90;

    private BaseAttackController baseAttackController;

    private PlayerBaseAttackCollection collection;

    private CharacterStateController stateController;

    private CharacterActionController actionController;

    private GameObject previewRoot;

    private MeshFilter baseMeshFilter;
    private MeshRenderer baseMeshRenderer;

    private MeshFilter readinessMeshFilter;
    private MeshRenderer readinessMeshRenderer;

    private LineRenderer outlineRenderer;

    private Mesh baseMesh;
    private Mesh readinessMesh;

    private Material baseMaterial;
    private Material readinessMaterial;
    private Material outlineMaterial;

    private readonly List<Vector3> vertices =
        new();

    private readonly List<int> triangles =
        new();

    private readonly List<Vector3> outlinePoints =
        new();

    private void Awake()
    {
        baseAttackController =
            GetComponent<BaseAttackController>();

        collection =
            GetComponent<PlayerBaseAttackCollection>();

        stateController =
            GetComponent<CharacterStateController>();

        actionController =
            GetComponent<CharacterActionController>();

        CreateRenderingObjects();
    }

    private void OnEnable()
    {
        if (collection == null)
        {
            collection =
                GetComponent<
                    PlayerBaseAttackCollection
                >();
        }

        if (collection != null)
        {
            collection.OnEquippedAttackChanged +=
                HandleEquippedAttackChanged;
        }
    }

    private void OnDisable()
    {
        if (collection != null)
        {
            collection.OnEquippedAttackChanged -=
                HandleEquippedAttackChanged;
        }

        HidePreview();
    }

    private void OnDestroy()
    {
        DestroyRuntimeObject(
            baseMesh
        );

        DestroyRuntimeObject(
            readinessMesh
        );

        DestroyRuntimeObject(
            baseMaterial
        );

        DestroyRuntimeObject(
            readinessMaterial
        );

        DestroyRuntimeObject(
            outlineMaterial
        );

        if (previewRoot != null)
        {
            Destroy(
                previewRoot
            );
        }
    }

    private void OnValidate()
    {
        coneSegments =
            Mathf.Clamp(
                coneSegments,
                4,
                128
            );

        outlineWidth =
            Mathf.Max(
                0.001f,
                outlineWidth
            );
    }

    private void LateUpdate()
    {
        RenderCurrentAttack();
    }

    private void HandleEquippedAttackChanged(
        BaseAttackData attack)
    {
        if (attack == null)
        {
            HidePreview();
            return;
        }

        RenderCurrentAttack();
    }

    private void RenderCurrentAttack()
    {
        if (baseAttackController == null ||
            collection == null ||
            previewRoot == null)
        {
            HidePreview();
            return;
        }

        BaseAttackData attack =
            collection.GetEquippedAttack();

        if (attack == null)
        {
            HidePreview();
            return;
        }

        bool inCombat =
            stateController != null &&
            stateController.InCombat;

        if (!showOutsideCombat &&
            !inCombat)
        {
            HidePreview();
            return;
        }

        if (hideDuringActionPreview &&
            actionController != null &&
            actionController.IsPreviewing)
        {
            HidePreview();
            return;
        }

        Vector2 origin =
            transform.position;

        Vector2 direction =
            GetSafeDirection(
                baseAttackController
                    .CurrentDirection
            );

        float range =
            Mathf.Max(
                0.01f,
                attack.range
            );

        float angle =
            Mathf.Clamp(
                attack.arcAngle,
                0f,
                360f
            );

        BuildBaseShape(
            origin,
            direction,
            range,
            angle
        );

        float readiness =
            Mathf.Clamp01(
                baseAttackController
                    .GetReadinessNormalized()
            );

        BuildReadinessShape(
            origin,
            direction,
            range,
            angle,
            readiness
        );

        ApplyColors(
            inCombat,
            readiness
        );

        previewRoot.SetActive(
            true
        );
    }

    private void BuildBaseShape(
        Vector2 origin,
        Vector2 direction,
        float range,
        float angle)
    {
        ClearGeometry();

        BuildConeGeometry(
            origin,
            direction,
            range,
            angle,
            true
        );

        ApplyMesh(
            baseMesh,
            vertices,
            triangles
        );

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

    private void BuildReadinessShape(
        Vector2 origin,
        Vector2 direction,
        float range,
        float angle,
        float readiness)
    {
        vertices.Clear();
        triangles.Clear();
        outlinePoints.Clear();

        if (readiness <= 0f)
        {
            readinessMesh.Clear();
            return;
        }

        /*
         * Den vita formen växer från castern ut mot
         * attackens maximala range.
         */
        float filledRange =
            range * readiness;

        BuildConeGeometry(
            origin,
            direction,
            filledRange,
            angle,
            false
        );

        ApplyMesh(
            readinessMesh,
            vertices,
            triangles
        );
    }

    private void BuildConeGeometry(
        Vector2 worldOrigin,
        Vector2 direction,
        float range,
        float totalAngle,
        bool buildOutline)
    {
        range =
            Mathf.Max(
                0.001f,
                range
            );

        totalAngle =
            Mathf.Clamp(
                totalAngle,
                0f,
                360f
            );

        direction =
            GetSafeDirection(
                direction
            );

        if (totalAngle >= 359.9f)
        {
            BuildCircleGeometry(
                worldOrigin,
                range,
                buildOutline
            );

            return;
        }

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

        vertices.Add(
            localOrigin
        );

        if (buildOutline)
        {
            outlinePoints.Add(
                localOrigin
            );
        }

        for (int i = 0;
             i <= coneSegments;
             i++)
        {
            float progress =
                i /
                (float)coneSegments;

            float angleDegrees =
                Mathf.Lerp(
                    startAngle,
                    startAngle + totalAngle,
                    progress
                );

            float radians =
                angleDegrees *
                Mathf.Deg2Rad;

            Vector2 worldPoint =
                worldOrigin +
                new Vector2(
                    Mathf.Cos(radians),
                    Mathf.Sin(radians)
                ) * range;

            Vector3 localPoint =
                WorldToPreviewLocal(
                    worldPoint
                );

            vertices.Add(
                localPoint
            );

            if (buildOutline)
            {
                outlinePoints.Add(
                    localPoint
                );
            }
        }

        for (int i = 0;
             i < coneSegments;
             i++)
        {
            triangles.Add(0);
            triangles.Add(i + 1);
            triangles.Add(i + 2);
        }
    }

    private void BuildCircleGeometry(
        Vector2 worldCenter,
        float radius,
        bool buildOutline)
    {
        int segments =
            Mathf.Max(
                12,
                coneSegments * 2
            );

        Vector3 center =
            WorldToPreviewLocal(
                worldCenter
            );

        vertices.Add(
            center
        );

        for (int i = 0;
             i < segments;
             i++)
        {
            float radians =
                i /
                (float)segments *
                Mathf.PI *
                2f;

            Vector2 worldPoint =
                worldCenter +
                new Vector2(
                    Mathf.Cos(radians),
                    Mathf.Sin(radians)
                ) * radius;

            Vector3 localPoint =
                WorldToPreviewLocal(
                    worldPoint
                );

            vertices.Add(
                localPoint
            );

            if (buildOutline)
            {
                outlinePoints.Add(
                    localPoint
                );
            }
        }

        for (int i = 0;
             i < segments;
             i++)
        {
            int current =
                i + 1;

            int next =
                ((i + 1) % segments) +
                1;

            triangles.Add(0);
            triangles.Add(current);
            triangles.Add(next);
        }
    }

    private void ApplyColors(
        bool inCombat,
        float readiness)
    {
        Color fillColor =
            inCombat
                ? combatFillColor
                : passiveFillColor;

        Color outlineColor =
            inCombat
                ? combatOutlineColor
                : passiveOutlineColor;

        if (baseMaterial != null)
        {
            baseMaterial.color =
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

        if (readinessMaterial != null)
        {
            readinessMaterial.color =
                readinessFillColor;
        }

        bool showReadiness =
            readiness > 0f &&
            (!hideReadinessFillWhenReady ||
             readiness < 0.999f);

        readinessMeshRenderer.enabled =
            showReadiness;
    }

    private void CreateRenderingObjects()
    {
        previewRoot =
            new GameObject(
                "Base Attack Preview Runtime"
            );

        previewRoot.transform.SetParent(
            transform,
            false
        );

        Shader spriteShader =
            Shader.Find(
                "Sprites/Default"
            );

        if (spriteShader == null)
        {
            Debug.LogError(
                "BaseAttackPreviewRenderer kunde inte hitta " +
                "shadern 'Sprites/Default'.",
                this
            );

            previewRoot.SetActive(
                false
            );

            return;
        }

        CreateBaseFill(
            spriteShader
        );

        CreateReadinessFill(
            spriteShader
        );

        CreateOutline(
            spriteShader
        );

        previewRoot.SetActive(
            false
        );
    }

    private void CreateBaseFill(
        Shader shader)
    {
        GameObject fillObject =
            new GameObject(
                "Passive Fill"
            );

        fillObject.transform.SetParent(
            previewRoot.transform,
            false
        );

        baseMeshFilter =
            fillObject.AddComponent<MeshFilter>();

        baseMeshRenderer =
            fillObject.AddComponent<MeshRenderer>();

        baseMesh =
            new Mesh
            {
                name =
                    "Base Attack Passive Mesh Runtime"
            };

        baseMaterial =
            new Material(shader)
            {
                name =
                    "Base Attack Passive Material Runtime"
            };

        baseMeshFilter.sharedMesh =
            baseMesh;

        baseMeshRenderer.material =
            baseMaterial;

        baseMeshRenderer.sortingLayerName =
            sortingLayerName;

        baseMeshRenderer.sortingOrder =
            sortingOrder;
    }

    private void CreateReadinessFill(
        Shader shader)
    {
        GameObject readinessObject =
            new GameObject(
                "Readiness Fill"
            );

        readinessObject.transform.SetParent(
            previewRoot.transform,
            false
        );

        readinessMeshFilter =
            readinessObject.AddComponent<MeshFilter>();

        readinessMeshRenderer =
            readinessObject.AddComponent<MeshRenderer>();

        readinessMesh =
            new Mesh
            {
                name =
                    "Base Attack Readiness Mesh Runtime"
            };

        readinessMaterial =
            new Material(shader)
            {
                name =
                    "Base Attack Readiness Material Runtime"
            };

        readinessMeshFilter.sharedMesh =
            readinessMesh;

        readinessMeshRenderer.material =
            readinessMaterial;

        readinessMeshRenderer.sortingLayerName =
            sortingLayerName;

        readinessMeshRenderer.sortingOrder =
            sortingOrder + 1;
    }

    private void CreateOutline(
        Shader shader)
    {
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

        outlineMaterial =
            new Material(shader)
            {
                name =
                    "Base Attack Outline Material Runtime"
            };

        outlineRenderer.material =
            outlineMaterial;

        outlineRenderer.sortingLayerName =
            sortingLayerName;

        outlineRenderer.sortingOrder =
            sortingOrder + 2;

        outlineRenderer.useWorldSpace =
            false;

        outlineRenderer.loop =
            true;

        outlineRenderer.numCapVertices =
            4;

        outlineRenderer.numCornerVertices =
            4;

        outlineRenderer.startWidth =
            outlineWidth;

        outlineRenderer.endWidth =
            outlineWidth;
    }

    private static void ApplyMesh(
        Mesh mesh,
        List<Vector3> meshVertices,
        List<int> meshTriangles)
    {
        if (mesh == null)
            return;

        mesh.Clear();

        mesh.SetVertices(
            meshVertices
        );

        mesh.SetTriangles(
            meshTriangles,
            0
        );

        mesh.RecalculateBounds();
    }

    private void ClearGeometry()
    {
        vertices.Clear();
        triangles.Clear();
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

        baseMesh?.Clear();
        readinessMesh?.Clear();

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
            0.0001f)
        {
            return Vector2.right;
        }

        return direction.normalized;
    }

    private static void DestroyRuntimeObject(
        Object runtimeObject)
    {
        if (runtimeObject != null)
        {
            Destroy(
                runtimeObject
            );
        }
    }
}
