using UnityEngine;

/// <summary>
/// Visar spelarens passiva base attack-form och dess
/// cooldown/readiness.
///
/// Denna komponent renderar aldrig en aktiv action.
///
/// All aktiv targeting-, cast- och charge-preview hanteras
/// exklusivt av ActionPreviewRenderer.
/// </summary>
[RequireComponent(typeof(BaseAttackController))]
[RequireComponent(typeof(PlayerBaseAttackCollection))]
[RequireComponent(typeof(CharacterStats))]
[RequireComponent(typeof(CharacterActionController))]
public sealed class BaseAttackPreviewRenderer :
    MonoBehaviour
{
    [Header("Visibility")]

    [SerializeField]
    [Tooltip(
        "Om den passiva base attack-indikatorn ska visas " +
        "även utanför combat."
    )]
    private bool showOutsideCombat = true;

    [SerializeField]
    [Tooltip(
        "Döljer den passiva base attack-indikatorn medan " +
        "vilken action som helst är aktiv.\n\n" +
        "Rekommenderas aktiverad eftersom ActionPreviewRenderer " +
        "äger all aktiv targeting och charge-preview."
    )]
    private bool hideDuringActiveAction = true;

    [SerializeField]
    [Tooltip(
        "Om den passiva grundformen ska följa abilityns " +
        "Action Preview Mode.\n\n" +
        "När detta är aktiverat döljer Preview Mode: None " +
        "den passiva grundformen."
    )]
    private bool respectAbilityPreviewMode = true;

    [SerializeField]
    [Tooltip(
        "Tillåter readiness-fyllnaden även när abilityns " +
        "Preview Mode är None.\n\n" +
        "Observera att readiness-fyllnaden fortfarande avslöjar " +
        "attackens targetingform och räckvidd."
    )]
    private bool showReadinessWhenPreviewModeIsNone = true;

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
        "Döljer readiness-fyllnaden när attacken är helt redo."
    )]
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

    private BaseAttackController
        baseAttackController;

    private PlayerBaseAttackCollection
        collection;

    private CharacterStateController
        stateController;

    private CharacterActionController
        actionController;

    private GameObject previewRoot;

    private TargetShapeRenderer
        baseShapeRenderer;

    private TargetShapeRenderer
        readinessShapeRenderer;

    private void Awake()
    {
        stats =
            GetComponent<CharacterStats>();

        baseAttackController =
            GetComponent<BaseAttackController>();

        collection =
            GetComponent<
                PlayerBaseAttackCollection>();

        stateController =
            GetComponent<
                CharacterStateController>();

        actionController =
            GetComponent<
                CharacterActionController>();

        /*
         * Den passiva base attack-indikatorn är endast
         * avsedd för spelaren.
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

        ResolveReferences();
        Subscribe();

        RenderCurrentAttack();
    }

    private void OnDisable()
    {
        Unsubscribe();
        HidePreview();
    }

    private void OnDestroy()
    {
        Unsubscribe();

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

    private void ResolveReferences()
    {
        if (baseAttackController == null)
        {
            baseAttackController =
                GetComponent<
                    BaseAttackController>();
        }

        if (collection == null)
        {
            collection =
                GetComponent<
                    PlayerBaseAttackCollection>();
        }

        if (stateController == null)
        {
            stateController =
                GetComponent<
                    CharacterStateController>();
        }

        if (actionController == null)
        {
            actionController =
                GetComponent<
                    CharacterActionController>();
        }
    }

    private void Subscribe()
    {
        if (collection == null)
            return;

        collection.OnEquippedAttackChanged -=
            HandleEquippedAttackChanged;

        collection.OnEquippedAttackChanged +=
            HandleEquippedAttackChanged;
    }

    private void Unsubscribe()
    {
        if (collection == null)
            return;

        collection.OnEquippedAttackChanged -=
            HandleEquippedAttackChanged;
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
                "Base Attack Readiness Runtime"
            );

        previewRoot.transform.SetParent(
            transform,
            false
        );

        CreateBaseShapeRenderer();
        CreateReadinessShapeRenderer();

        previewRoot.SetActive(
            false
        );
    }

    private void CreateBaseShapeRenderer()
    {
        GameObject baseObject =
            new GameObject(
                "Passive Base Shape"
            );

        baseObject.transform.SetParent(
            previewRoot.transform,
            false
        );

        baseShapeRenderer =
            baseObject.AddComponent<
                TargetShapeRenderer>();

        baseShapeRenderer.Initialize(
            sortingLayerName,
            sortingOrder,
            circleSegments,
            coneSegments,
            0.15f,
            0.5f,
            outlineWidth,
            renderOutline: true
        );
    }

    private void CreateReadinessShapeRenderer()
    {
        GameObject readinessObject =
            new GameObject(
                "Readiness Fill Shape"
            );

        readinessObject.transform.SetParent(
            previewRoot.transform,
            false
        );

        readinessShapeRenderer =
            readinessObject.AddComponent<
                TargetShapeRenderer>();

        readinessShapeRenderer.Initialize(
            sortingLayerName,
            sortingOrder + 1,
            circleSegments,
            coneSegments,
            0.15f,
            0.5f,
            outlineWidth,
            renderOutline: false
        );
    }

    private void RenderCurrentAttack()
    {
        ResolveReferences();

        if (!HasRequiredReferences())
        {
            HidePreview();
            return;
        }

        AbilityData attack =
            collection.GetEquippedAttack();

        if (!CanDisplayAttack(
                attack))
        {
            HidePreview();
            return;
        }

        AbilityTargetingSettings settings =
            attack.TargetingSettings;

        bool inCombat =
            stateController != null &&
            stateController.InCombat;

        if (!showOutsideCombat &&
            !inCombat)
        {
            HidePreview();
            return;
        }

        /*
         * Under en aktiv action är ActionPreviewRenderer den
         * enda auktoritativa renderern.
         *
         * Detta gäller Preview, Cast, Charge, Execution och
         * Recovery, inte endast ActionPhase.Preview.
         */
        if (hideDuringActiveAction &&
            actionController != null &&
            actionController.HasActiveAction)
        {
            HidePreview();
            return;
        }

        bool previewModeIsNone =
            attack.PreviewMode ==
            ActionPreviewMode.None;

        bool showBaseShape =
            !respectAbilityPreviewMode ||
            !previewModeIsNone;

        bool showReadinessShape =
            !previewModeIsNone ||
            showReadinessWhenPreviewModeIsNone;

        Vector2 origin =
            transform.position;

        Vector2 direction =
            GetSafeDirection(
                baseAttackController
                    .CurrentDirection
            );

        float range =
            Mathf.Max(
                0f,
                settings.Range
            );

        Vector2 targetPoint =
            origin +
            direction * range;

        float readiness =
            Mathf.Clamp01(
                baseAttackController
                    .GetReadinessNormalized()
            );

        bool renderedAnything =
            false;

        if (showBaseShape)
        {
            renderedAnything |=
                RenderBaseShape(
                    settings,
                    origin,
                    targetPoint,
                    direction,
                    range,
                    inCombat
                );
        }
        else
        {
            baseShapeRenderer.Clear();
        }

        if (showReadinessShape)
        {
            renderedAnything |=
                RenderReadinessShape(
                    settings,
                    origin,
                    targetPoint,
                    direction,
                    range,
                    readiness
                );
        }
        else
        {
            readinessShapeRenderer.Clear();
        }

        previewRoot.SetActive(
            renderedAnything
        );
    }

    private bool RenderBaseShape(
        AbilityTargetingSettings settings,
        Vector2 origin,
        Vector2 targetPoint,
        Vector2 direction,
        float distance,
        bool inCombat)
    {
        Color fillColor =
            inCombat
                ? combatFillColor
                : passiveFillColor;

        Color outlineColor =
            inCombat
                ? combatOutlineColor
                : passiveOutlineColor;

        return baseShapeRenderer.Render(
            settings,
            origin,
            targetPoint,
            direction,
            distance,
            null,
            fillColor,
            outlineColor,
            shapeScale: 1f,
            rangeOverride: settings.Range
        );
    }

    private bool RenderReadinessShape(
        AbilityTargetingSettings settings,
        Vector2 origin,
        Vector2 targetPoint,
        Vector2 direction,
        float distance,
        float readiness)
    {
        bool shouldShow =
            readiness > 0f &&
            (
                !hideReadinessFillWhenReady ||
                readiness < 0.999f
            );

        if (!shouldShow)
        {
            readinessShapeRenderer.Clear();
            return false;
        }

        return readinessShapeRenderer.Render(
            settings,
            origin,
            targetPoint,
            direction,
            distance,
            null,
            readinessFillColor,
            Color.clear,
            shapeScale: readiness,
            rangeOverride: settings.Range
        );
    }

    private bool HasRequiredReferences()
    {
        return
            baseAttackController != null &&
            collection != null &&
            previewRoot != null &&
            baseShapeRenderer != null &&
            readinessShapeRenderer != null;
    }

    private static bool CanDisplayAttack(
        AbilityData attack)
    {
        if (attack == null)
            return false;

        if (!attack.IsBaseAttack)
            return false;

        if (!attack.UsesActionSettings)
            return false;

        if (attack.TargetingSettings == null)
            return false;

        return SupportsPermanentPreview(
            attack.TargetingSettings
                .TargetingMode
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
            targetingMode ==
                TargetingMode.Cone ||
            targetingMode ==
                TargetingMode.Circle ||
            targetingMode ==
                TargetingMode.Self ||
            targetingMode ==
                TargetingMode.Line;
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