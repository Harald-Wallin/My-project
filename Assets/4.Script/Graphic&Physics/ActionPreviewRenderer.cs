using UnityEngine;

/// <summary>
/// Visar den aktiva spelar-actionens targeting-preview.
///
/// Själva formrenderingen hanteras av TargetShapeRenderer.
/// </summary>
[RequireComponent(typeof(CharacterActionController))]
[RequireComponent(typeof(CharacterStats))]
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

    private CharacterStats stats;
    private CharacterActionController actionController;

    private GameObject rendererObject;
    private TargetShapeRenderer shapeRenderer;

    private void Awake()
    {
        stats =
            GetComponent<CharacterStats>();

        actionController =
            GetComponent<CharacterActionController>();

        /*
         * NPC-actions kan använda Preview-fasen internt, men
         * deras targetingform ska inte visas för spelaren.
         */
        if (!(stats is PlayerStats))
        {
            enabled = false;
            return;
        }

        CreateShapeRenderer();
    }

    private void OnEnable()
    {
        if (!(stats is PlayerStats))
            return;

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

        shapeRenderer?.Clear();
    }

    private void OnDestroy()
    {
        if (rendererObject != null)
        {
            Destroy(
                rendererObject
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

    private void CreateShapeRenderer()
    {
        rendererObject =
            new GameObject(
                "Action Preview Runtime"
            );

        rendererObject.transform.SetParent(
            transform,
            false
        );

        shapeRenderer =
            rendererObject.AddComponent<
                TargetShapeRenderer
            >();

        shapeRenderer.Initialize(
            sortingLayerName,
            sortingOrder,
            circleSegments,
            coneSegments,
            pointRadius,
            selfRadius,
            outlineWidth,
            true
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
            shapeRenderer?.Clear();
        }
    }

    private void HandleActionEnded(
        ActionContext context)
    {
        shapeRenderer?.Clear();
    }

    private void RenderPreview(
        ActionContext context)
    {
        TargetingResult targeting =
            context?.Targeting;

        if (targeting == null ||
            targeting.Settings == null ||
            shapeRenderer == null)
        {
            shapeRenderer?.Clear();
            return;
        }

        Color fillColor =
            targeting.IsValid
                ? validFillColor
                : invalidFillColor;

        Color outlineColor =
            targeting.IsValid
                ? validOutlineColor
                : invalidOutlineColor;

        shapeRenderer.Render(
            targeting,
            fillColor,
            outlineColor
        );
    }
}