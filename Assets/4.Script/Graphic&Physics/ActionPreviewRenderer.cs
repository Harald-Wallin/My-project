using UnityEngine;

/// <summary>
/// Visar den aktiva spelar-actionens targeting-preview.
///
/// Gameplay-geometrin kommer från TargetResolver.
/// Denna klass bestämmer endast hur geometrin ska visualiseras.
///
/// Två separata TargetShapeRenderer används:
///
/// outlineShapeRenderer:
/// - visar full targetingform eller maximal range
/// - kan visas utan fill
///
/// fillShapeRenderer:
/// - visar charge-progress
/// - har ingen egen outline
///
/// Detta gör det möjligt att senare ersätta mesh-renderingen
/// med sprites, shaders eller handritade preview-assets utan att
/// förändra gameplaylogiken.
/// </summary>
[RequireComponent(
    typeof(CharacterActionController)
)]
[RequireComponent(
    typeof(CharacterStats)
)]
public sealed class ActionPreviewRenderer :
    MonoBehaviour
{
    [Header("Normal Preview Colors")]

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

    [Header("Charge Preview Colors")]

    [SerializeField]
    [Tooltip(
        "Outline som visar abilityns maximala form eller range " +
        "under charge."
    )]
    private Color chargeMaximumOutlineColor =
        new Color(
            0.7f,
            0.7f,
            0.7f,
            0.65f
        );

    [SerializeField]
    [Tooltip(
        "Fill som visar nuvarande charge-progress."
    )]
    private Color chargeFillColor =
        new Color(
            0.2f,
            0.8f,
            1f,
            0.35f
        );

    [SerializeField]
    [Tooltip(
        "Fill som används när aktuell charge-targeting är " +
        "ogiltig."
    )]
    private Color invalidChargeFillColor =
        new Color(
            1f,
            0.15f,
            0.15f,
            0.35f
        );

    [Header("Shape Quality")]

    [SerializeField]
    [Range(12, 128)]
    private int circleSegments =
        48;

    [SerializeField]
    [Range(4, 128)]
    private int coneSegments =
        32;

    [SerializeField]
    [Min(0.01f)]
    private float pointRadius =
        0.15f;

    [SerializeField]
    [Min(0.01f)]
    private float selfRadius =
        0.5f;

    [SerializeField]
    [Min(0.001f)]
    private float outlineWidth =
        0.04f;

    [Header("Rendering")]

    [SerializeField]
    private string sortingLayerName =
        "Default";

    [SerializeField]
    private int sortingOrder =
        100;

    private CharacterStats stats;

    private CharacterActionController
        actionController;

    private GameObject outlineRendererObject;
    private GameObject fillRendererObject;

    private TargetShapeRenderer
        outlineShapeRenderer;

    private TargetShapeRenderer
        fillShapeRenderer;

    private bool activeChargeIsFull;

    private void Awake()
    {
        stats =
            GetComponent<
                CharacterStats>();

        actionController =
            GetComponent<
                CharacterActionController>();

        /*
         * NPC-actions använder samma gameplaytargeting,
         * men spelarens targeting-preview ska inte skapas
         * på NPC:er.
         */
        if (!(stats is PlayerStats))
        {
            enabled =
                false;

            return;
        }

        CreateShapeRenderers();
    }

    private void OnEnable()
    {
        if (!(stats is PlayerStats))
            return;

        if (actionController == null)
        {
            actionController =
                GetComponent<
                    CharacterActionController>();
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

        actionController.OnFullChargeReached +=
            HandleFullChargeReached;
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

            actionController.OnFullChargeReached -=
                HandleFullChargeReached;
        }

        ClearPreview();
    }

    private void HandleFullChargeReached(
    ActionContext context)
    {
        if (context == null ||
            context !=
            actionController.CurrentContext)
        {
            return;
        }

        activeChargeIsFull =
            true;

        RenderPreview(
            context
        );
    }

    private void OnDestroy()
    {
        if (outlineRendererObject != null)
        {
            Destroy(
                outlineRendererObject
            );
        }

        if (fillRendererObject != null)
        {
            Destroy(
                fillRendererObject
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

    private void CreateShapeRenderers()
    {
        CreateOutlineRenderer();
        CreateFillRenderer();
    }

    private void CreateOutlineRenderer()
    {
        outlineRendererObject =
            new GameObject(
                "Action Preview Outline Runtime"
            );

        outlineRendererObject.transform
            .SetParent(
                transform,
                false
            );

        outlineShapeRenderer =
            outlineRendererObject
                .AddComponent<
                    TargetShapeRenderer>();

        outlineShapeRenderer.Initialize(
            sortingLayerName,
            sortingOrder,
            circleSegments,
            coneSegments,
            pointRadius,
            selfRadius,
            outlineWidth,
            renderOutline: true
        );
    }

    private void CreateFillRenderer()
    {
        fillRendererObject =
            new GameObject(
                "Action Preview Fill Runtime"
            );

        fillRendererObject.transform
            .SetParent(
                transform,
                false
            );

        fillShapeRenderer =
            fillRendererObject
                .AddComponent<
                    TargetShapeRenderer>();

        /*
         * Fill-renderern ligger något under outlinen och
         * skapar ingen egen LineRenderer.
         */
        fillShapeRenderer.Initialize(
            sortingLayerName,
            sortingOrder - 1,
            circleSegments,
            coneSegments,
            pointRadius,
            selfRadius,
            outlineWidth,
            renderOutline: false
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
        if (!ShouldRenderContext(
                context))
        {
            ClearPreview();

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
        if (phase ==
                ActionPhase.Preview ||
            phase ==
                ActionPhase.Charging)
        {
            if (phase ==
                ActionPhase.Charging &&
                context != null &&
                context.ChargeProgress < 1f)
            {
                activeChargeIsFull =
                    false;
            }

            RenderPreview(
                context
            );

            return;
        }

        activeChargeIsFull =
            false;

        ClearPreview();
    }

    private void HandleActionEnded(
    ActionContext context)
    {
        activeChargeIsFull =
            false;

        ClearPreview();
    }

    private static bool ShouldRenderContext(
        ActionContext context)
    {
        if (context == null)
            return false;

        return
            context.Phase ==
                ActionPhase.Preview ||
            context.Phase ==
                ActionPhase.Charging;
    }

    private void RenderPreview(
        ActionContext context)
    {
        if (outlineShapeRenderer == null ||
            fillShapeRenderer == null)
        {
            return;
        }

        AbilityData ability =
            context?.Ability;

        if (ability == null)
        {
            ClearPreview();

            return;
        }

        TargetingResult targeting =
            context.Targeting;

        if (targeting == null ||
            targeting.Settings == null)
        {
            ClearPreview();

            return;
        }

        switch (ability.PreviewMode)
        {
            case ActionPreviewMode.None:
                ClearPreview();
                break;

            case ActionPreviewMode.FullGeometry:
                RenderFullGeometry(
                    targeting
                );
                break;

            case ActionPreviewMode.ChargeIndicator:
                RenderChargeIndicator(
                    context,
                    targeting
                );
                break;

            case ActionPreviewMode.ChargeFilledGeometry:
                RenderChargeFilledGeometry(
                    context,
                    targeting
                );
                break;

            default:
                ClearPreview();
                break;
        }
    }

    /// <summary>
    /// Visar hela targetingformen med vanlig fill och outline.
    /// </summary>
    private void RenderFullGeometry(
        TargetingResult targeting)
    {
        fillShapeRenderer.Clear();

        Color fillColor =
            targeting.IsValid
                ? validFillColor
                : invalidFillColor;

        Color outlineColor =
            targeting.IsValid
                ? validOutlineColor
                : invalidOutlineColor;

        outlineShapeRenderer.Render(
            targeting,
            fillColor,
            outlineColor
        );
    }

    /// <summary>
    /// Visar endast targetingformens outline.
    ///
    /// Kan användas för charge-actions där vi bara vill visa
    /// abilityns riktning eller maximum range utan någon fill.
    /// </summary>
    private void RenderChargeIndicator(
     ActionContext context,
     TargetingResult targeting)
    {
        fillShapeRenderer.Clear();

        Color outlineColor =
            targeting.IsValid
                ? validOutlineColor
                : invalidOutlineColor;

        Vector2 terminalOffset =
            GetFullChargeTerminalOffset(
                context,
                targeting
            );

        outlineShapeRenderer.Render(
            targeting,
            Color.clear,
            outlineColor,
            1f,
            terminalOffset
        );
    }

    /// <summary>
    /// Visar charge som en fill i targetingformen.
    ///
    /// Beteende:
    ///
    /// Damage only:
    /// - full statisk outline
    /// - fill växer med ChargeProgress
    ///
    /// Range only:
    /// - grå outline visar maximum range
    /// - fill visar aktuell chargad range
    ///
    /// Damage + Range:
    /// - endast aktuell växande fill
    /// - samma form representerar både damage och range
    /// </summary>
    private void RenderChargeFilledGeometry(
        ActionContext context,
        TargetingResult targeting)
    {
        AbilityData ability =
            context.Ability;

        AbilityChargeSettings chargeSettings =
            ability.ChargeSettings;

        if (chargeSettings == null)
        {
            RenderFullGeometry(
                targeting
            );

            return;
        }

        float chargeProgress =
            Mathf.Clamp01(
                context.ChargeProgress
            );

        bool scalesDamage =
            chargeSettings.ScalesDamage;

        bool scalesRange =
            chargeSettings.ScalesRange;

        Color currentFillColor =
            targeting.IsValid
                ? chargeFillColor
                : invalidChargeFillColor;

        Vector2 terminalOffset =
            GetFullChargeTerminalOffset(
                context,
                targeting
            );

        /*
         * DAMAGE + RANGE
         *
         * TargetResolver har redan byggt formen med nuvarande
         * EffectiveRange. Formen växer alltså fysiskt under charge.
         *
         * Vi visar ingen separat maximum-outline eftersom fillen
         * representerar både damage och range.
         */
        if (scalesDamage &&
            scalesRange)
        {
            outlineShapeRenderer.Clear();

            fillShapeRenderer.Render(
                targeting,
                currentFillColor,
                Color.clear,
                1f,
                terminalOffset
            );

            return;
        }

        if (scalesRange)
        {
            /*
             * RANGE ONLY
             *
             * TargetResolver har redan skapat targetingformen med
             * aktuell chargad EffectiveRange.
             *
             * Vi visar därför endast den faktiska formen som växer.
             * Ingen statisk maximum-outline visas.
             */
            outlineShapeRenderer.Clear();

            fillShapeRenderer.Render(
                targeting,
                currentFillColor,
                Color.clear,
                1f,
                terminalOffset
            );

            return;
        }

        /*
         * DAMAGE ONLY
         *
         * Abilityns range är statisk.
         * Outlinen visar hela targetingformen.
         *
         * Fillen växer inuti formen baserat på ChargeProgress.
         */
        if (scalesDamage)
        {
            outlineShapeRenderer.Render(
                targeting,
                Color.clear,
                chargeMaximumOutlineColor
            );

            fillShapeRenderer.Render(
                targeting,
                currentFillColor,
                Color.clear,
                chargeProgress,
                terminalOffset
            );

            return;
        }

        /*
         * Charge ability utan konfigurerad damage- eller
         * range-skalning.
         *
         * Vi visar ändå en växande fill för att spelaren ska
         * kunna läsa av charge-tiden visuellt.
         */
        outlineShapeRenderer.Render(
            targeting,
            Color.clear,
            chargeMaximumOutlineColor
        );

        fillShapeRenderer.Render(
            targeting,
            currentFillColor,
            Color.clear,
            chargeProgress, 
            terminalOffset
        );
    }

    /// <summary>
    /// Renderar abilityns maximala targetingform som en grå
    /// outline.
    ///
    /// TargetingResult innehåller den aktuella chargade formen,
    /// medan AbilityTargetingSettings.Range innehåller maximum.
    /// </summary>
    private void RenderMaximumRangeOutline(
        TargetingResult targeting)
    {
        AbilityTargetingSettings settings =
            targeting.Settings;

        Vector2 maximumTargetPoint =
            ResolveMaximumTargetPoint(
                targeting
            );

        float maximumDistance =
            Vector2.Distance(
                targeting.Origin,
                maximumTargetPoint
            );

        outlineShapeRenderer.Render(
            settings,
            targeting.Origin,
            maximumTargetPoint,
            targeting.Direction,
            maximumDistance,
            targeting.PrimaryTarget,
            Color.clear,
            chargeMaximumOutlineColor,
            shapeScale: 1f,
            rangeOverride: settings.Range
        );
    }

    /// <summary>
    /// Beräknar var maximum-previewn ska sluta.
    ///
    /// Full Range:
    /// - använder alltid hela settings.Range
    ///
    /// To Cursor:
    /// - använder musens avstånd, men aldrig längre än Range
    /// </summary>
    private static Vector2 ResolveMaximumTargetPoint(
        TargetingResult targeting)
    {
        AbilityTargetingSettings settings =
            targeting.Settings;

        Vector2 direction =
            targeting.Direction.sqrMagnitude >
            0.0001f
                ? targeting.Direction.normalized
                : Vector2.down;

        if (settings.TargetingMode ==
                TargetingMode.Line &&
            settings.LineLengthMode ==
                LineLengthMode.FullRange)
        {
            return
                targeting.Origin +
                direction *
                settings.Range;
        }

        float rawAimDistance =
            Vector2.Distance(
                targeting.Origin,
                targeting.RawAimPoint
            );

        float maximumDistance =
            Mathf.Min(
                rawAimDistance,
                settings.Range
            );

        return
            targeting.Origin +
            direction *
            maximumDistance;
    }

    private void ClearPreview()
    {
        activeChargeIsFull =
            false;

        outlineShapeRenderer?.Clear();
        fillShapeRenderer?.Clear();
    }

    private Vector2 GetFullChargeTerminalOffset(
    ActionContext context,
    TargetingResult targeting)
    {
        if (!activeChargeIsFull ||
            context == null ||
            targeting == null ||
            context.Phase !=
                ActionPhase.Charging ||
            targeting.Settings.TargetingMode !=
                TargetingMode.Line)
        {
            return Vector2.zero;
        }

        ChargeCompletionPreviewEffect effect =
            context
                .Ability
                ?.ChargeCompletionPreviewEffect;

        if (effect == null ||
            !effect.IsEnabled)
        {
            return Vector2.zero;
        }

        switch (effect.EffectType)
        {
            case ChargeCompletionPreviewEffectType
                .LineEndShake:

                return effect.GetTerminalOffset(
                    targeting.Direction,
                    Time.unscaledTime
                );

            case ChargeCompletionPreviewEffectType.None:
            default:
                return Vector2.zero;
        }
    }

    private void LateUpdate()
    {
        if (!activeChargeIsFull ||
            actionController == null ||
            !actionController.IsCharging)
        {
            return;
        }

        ActionContext context =
            actionController.CurrentContext;

        if (context == null)
            return;

        RenderPreview(
            context
        );
    }
}