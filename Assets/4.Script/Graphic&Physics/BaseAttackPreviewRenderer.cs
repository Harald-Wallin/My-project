using UnityEngine;

/// <summary>
/// Visar spelarens permanenta base attack-form samt attackens
/// readiness/cooldown-fyllnad.
///
/// Själva formrenderingen hanteras av TargetShapeRenderer.
/// </summary>
[RequireComponent(typeof(BaseAttackController))]
[RequireComponent(typeof(PlayerBaseAttackCollection))]
[RequireComponent(typeof(CharacterStats))]
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
    private bool hideReadinessFillWhenReady;

    [Header("Shape Quality")]

    [SerializeField]
    [Range(4, 128)]
    private int coneSegments = 32;

    [SerializeField]
    [Range(12, 128)]
    private int circleSegments = 48;

    [SerializeField]
    [Min(0.001f)]
    private float outlineWidth = 0.035f;

    [Header("Rendering")]

    [SerializeField]
    private string sortingLayerName =
        "Default";

    [SerializeField]
    private int sortingOrder = 90;

    private CharacterStats stats;
    private BaseAttackController baseAttackController;
    private PlayerBaseAttackCollection collection;
    private CharacterStateController stateController;
    private CharacterActionController actionController;

    private GameObject previewRoot;

    private TargetShapeRenderer baseShapeRenderer;
    private TargetShapeRenderer readinessShapeRenderer;

    private void Awake()
    {
        stats =
            GetComponent<CharacterStats>();

        baseAttackController =
            GetComponent<BaseAttackController>();

        collection =
            GetComponent<PlayerBaseAttackCollection>();

        stateController =
            GetComponent<CharacterStateController>();

        actionController =
            GetComponent<CharacterActionController>();

        /*
         * Den permanenta base attack-previewn är endast
         * spelar-UI.
         */
        if (!(stats is PlayerStats))
        {
            enabled = false;
            return;
        }

        CreateRenderingObjects();
    }

    private void OnEnable()
    {
        if (!(stats is PlayerStats))
            return;

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

        circleSegments =
            Mathf.Clamp(
                circleSegments,
                12,
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
        if (!(stats is PlayerStats))
            return;

        RenderCurrentAttack();
    }

    private void HandleEquippedAttackChanged(
        AbilityData attack)
    {
        if (attack == null)
        {
            HidePreview();
            return;
        }

        RenderCurrentAttack();
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

        GameObject baseObject =
            new GameObject(
                "Base Shape"
            );

        baseObject.transform.SetParent(
            previewRoot.transform,
            false
        );

        baseShapeRenderer =
            baseObject.AddComponent<
                TargetShapeRenderer
            >();

        baseShapeRenderer.Initialize(
            sortingLayerName,
            sortingOrder,
            circleSegments,
            coneSegments,
            0.15f,
            0.5f,
            outlineWidth,
            true
        );

        GameObject readinessObject =
            new GameObject(
                "Readiness Shape"
            );

        readinessObject.transform.SetParent(
            previewRoot.transform,
            false
        );

        readinessShapeRenderer =
            readinessObject.AddComponent<
                TargetShapeRenderer
            >();

        readinessShapeRenderer.Initialize(
            sortingLayerName,
            sortingOrder + 1,
            circleSegments,
            coneSegments,
            0.15f,
            0.5f,
            outlineWidth,
            false
        );
    }

    private void RenderCurrentAttack()
    {
        if (baseAttackController == null ||
            collection == null ||
            previewRoot == null ||
            baseShapeRenderer == null ||
            readinessShapeRenderer == null)
        {
            HidePreview();
            return;
        }

        AbilityData attack =
            collection.GetEquippedAttack();

        if (attack == null ||
            !attack.IsBaseAttack ||
            !attack.UsesActionSettings)
        {
            HidePreview();
            return;
        }

        AbilityTargetingSettings settings =
            attack.TargetingSettings;

        if (settings == null)
        {
            HidePreview();
            return;
        }

        if (!SupportsPermanentPreview(
                settings.TargetingMode))
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
                baseAttackController.CurrentDirection
            );

        Vector2 targetPoint =
            origin +
            direction * settings.Range;

        float distance =
            settings.Range;

        float readiness =
            Mathf.Clamp01(
                baseAttackController
                    .GetReadinessNormalized()
            );

        Color fillColor =
            inCombat
                ? combatFillColor
                : passiveFillColor;

        Color outlineColor =
            inCombat
                ? combatOutlineColor
                : passiveOutlineColor;

        bool renderedBaseShape =
            baseShapeRenderer.Render(
                settings,
                origin,
                targetPoint,
                direction,
                distance,
                null,
                fillColor,
                outlineColor,
                1f
            );

        if (!renderedBaseShape)
        {
            HidePreview();
            return;
        }

        bool showReadiness =
            readiness > 0f &&
            (!hideReadinessFillWhenReady ||
             readiness < 0.999f);

        if (showReadiness)
        {
            readinessShapeRenderer.Render(
                settings,
                origin,
                targetPoint,
                direction,
                distance,
                null,
                readinessFillColor,
                readinessFillColor,
                readiness
            );
        }
        else
        {
            readinessShapeRenderer.Clear();
        }

        previewRoot.SetActive(
            true
        );
    }

    private void HidePreview()
    {
        baseShapeRenderer?.Clear();
        readinessShapeRenderer?.Clear();

        if (previewRoot != null)
        {
            previewRoot.SetActive(
                false
            );
        }
    }

    private static bool SupportsPermanentPreview(
        TargetingMode targetingMode)
    {
        return
            targetingMode == TargetingMode.Cone ||
            targetingMode == TargetingMode.Circle ||
            targetingMode == TargetingMode.Self ||
            targetingMode == TargetingMode.Line;
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
}