using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// Spelarens inputbrygga till CharacterActionController.
///
/// Ansvar:
/// - starta migrerade abilities från action bar/hotkeys
/// - uppdatera targeting-preview från musen
/// - hitta explicit SingleTarget under muspekaren
/// - bekräfta och avbryta preview
///
/// Komponenten används endast av spelaren.
/// NPC:er anropar CharacterActionController direkt från sin AI.
/// </summary>
[RequireComponent(typeof(CharacterActionController))]
public sealed class PlayerActionInput :
    MonoBehaviour
{
    [Header("World Input")]

    [SerializeField]
    private Camera worldCamera;

    [Header("Target Detection")]

    [SerializeField]
    [Min(1)]
    private int targetDetectionBufferSize = 16;

    [Header("Confirmation")]

    [SerializeField]
    private KeyCode cancelKey = KeyCode.Escape;

    [SerializeField]
    private bool rightClickCancels = true;

    private CharacterActionController actionController;

    private Collider2D[] targetDetectionBuffer;

    private int previewStartedFrame = -1;

    public CharacterActionController ActionController =>
        actionController;

    private void Awake()
    {
        actionController =
            GetComponent<CharacterActionController>();

        CreateTargetDetectionBuffer();

        ResolveWorldCamera();
    }

    private void OnEnable()
    {
        if (actionController == null)
        {
            actionController =
                GetComponent<CharacterActionController>();
        }

        if (actionController != null)
        {
            actionController.OnPreviewStarted +=
                HandlePreviewStarted;
        }
    }

    private void OnDisable()
    {
        if (actionController != null)
        {
            actionController.OnPreviewStarted -=
                HandlePreviewStarted;
        }
    }

    private void OnValidate()
    {
        targetDetectionBufferSize =
            Mathf.Max(
                1,
                targetDetectionBufferSize
            );
    }

    private void Update()
    {
        if (actionController == null ||
            !actionController.IsPreviewing)
        {
            return;
        }

        UpdateCurrentPreview();

        if (ShouldCancelPreview())
        {
            actionController.CancelCurrentAction();
            return;
        }

        if (ShouldConfirmPreview())
        {
            /*
             * Uppdatera en sista gång på klickets exakta
             * musposition innan actionen bekräftas.
             */
            UpdateCurrentPreview();

            actionController.ConfirmCurrentAction();
        }
    }

    /// <summary>
    /// Startar en ability med spelarens nuvarande musposition
    /// och eventuellt target under muspekaren.
    ///
    /// Anropas av ActionSlot för migrerade abilities.
    /// </summary>
    public bool TryStartAbility(
        AbilityData ability)
    {
        if (ability == null ||
            actionController == null)
        {
            return false;
        }

        if (!ability.UsesActionSettings)
        {
            Debug.LogWarning(
                $"'{ability.abilityName}' använder inte det nya " +
                $"actionsystemets inställningar.",
                this
            );

            return false;
        }

        if (!TryGetMouseWorldPosition(
                out Vector2 aimPoint))
        {
            return false;
        }

        GameObject explicitTarget =
            ResolveExplicitTarget(
                ability,
                aimPoint
            );

        Vector2 requestedDirection =
            GetDirectionToAimPoint(
                aimPoint
            );

        bool started =
            actionController.TryStartAction(
                ability,
                aimPoint,
                explicitTarget,
                requestedDirection
            );

        /*
         * Eventet bör redan ha registrerat frame-numret.
         * Den direkta tilldelningen fungerar som extra säkerhet.
         */
        if (started &&
            actionController.IsPreviewing)
        {
            previewStartedFrame =
                Time.frameCount;
        }

        return started;
    }

    /// <summary>
    /// Uppdaterar den aktiva previewn utifrån musens
    /// aktuella världsposition.
    /// </summary>
    public bool UpdateCurrentPreview()
    {
        if (actionController == null ||
            !actionController.IsPreviewing)
        {
            return false;
        }

        ActionContext context =
            actionController.CurrentContext;

        if (context == null ||
            context.Ability == null)
        {
            return false;
        }

        if (!TryGetMouseWorldPosition(
                out Vector2 aimPoint))
        {
            return false;
        }

        GameObject explicitTarget =
            ResolveExplicitTarget(
                context.Ability,
                aimPoint
            );

        Vector2 requestedDirection =
            GetDirectionToAimPoint(
                aimPoint
            );

        return actionController.UpdatePreview(
            aimPoint,
            explicitTarget,
            requestedDirection
        );
    }

    private void HandlePreviewStarted(
        ActionContext context)
    {
        /*
         * Förhindrar att samma vänsterklick som startade
         * previewn via action baren också omedelbart
         * bekräftar den.
         */
        previewStartedFrame =
            Time.frameCount;
    }

    private bool ShouldConfirmPreview()
    {
        if (!Input.GetMouseButtonDown(0))
            return false;

        /*
         * Startklicket och confirm-klicket måste ske på
         * olika frames.
         */
        if (Time.frameCount <=
            previewStartedFrame)
        {
            return false;
        }

        if (IsPointerOverUI())
            return false;

        return true;
    }

    private bool ShouldCancelPreview()
    {
        if (Input.GetKeyDown(cancelKey))
            return true;

        if (!rightClickCancels)
            return false;

        if (!Input.GetMouseButtonDown(1))
            return false;

        /*
         * Högerklick på UI ska inte påverka action-previewn.
         */
        return !IsPointerOverUI();
    }

    private GameObject ResolveExplicitTarget(
        AbilityData ability,
        Vector2 aimPoint)
    {
        if (ability == null)
            return null;

        AbilityTargetingSettings settings =
            ability.TargetingSettings;

        if (settings == null)
            return null;

        if (settings.TargetingMode !=
            TargetingMode.SingleTarget)
        {
            return null;
        }

        EnsureTargetDetectionBuffer();

        int hitCount =
            Physics2D.OverlapPointNonAlloc(
                aimPoint,
                targetDetectionBuffer,
                settings.TargetLayers
            );

        for (int i = 0;
             i < hitCount;
             i++)
        {
            Collider2D hit =
                targetDetectionBuffer[i];

            if (hit == null)
                continue;

            GameObject target =
                TargetUtility.ResolveTargetObject(
                    hit
                );

            if (target == null)
            {
                target =
                    TargetUtility.ResolveCharacterTarget(
                        hit.gameObject
                    );
            }

            if (target == null)
                continue;

            if (!TargetValidator.IsSupportedTarget(
                    target))
            {
                continue;
            }

            return target;
        }

        return null;
    }

    private Vector2 GetDirectionToAimPoint(
        Vector2 aimPoint)
    {
        Vector2 origin =
            transform.position;

        Vector2 direction =
            aimPoint - origin;

        if (direction.sqrMagnitude <=
            Mathf.Epsilon)
        {
            ActionContext context =
                actionController != null
                    ? actionController.CurrentContext
                    : null;

            if (context != null)
                return context.AimDirection;

            return Vector2.down;
        }

        return direction.normalized;
    }

    private bool TryGetMouseWorldPosition(
        out Vector2 worldPosition)
    {
        ResolveWorldCamera();

        if (worldCamera == null)
        {
            worldPosition =
                transform.position;

            return false;
        }

        Vector3 mouseScreenPosition =
            Input.mousePosition;

        Vector3 mouseWorldPosition =
            worldCamera.ScreenToWorldPoint(
                mouseScreenPosition
            );

        worldPosition =
            new Vector2(
                mouseWorldPosition.x,
                mouseWorldPosition.y
            );

        return true;
    }

    private void ResolveWorldCamera()
    {
        if (worldCamera != null)
            return;

        worldCamera =
            Camera.main;
    }

    private static bool IsPointerOverUI()
    {
        return
            EventSystem.current != null &&
            EventSystem.current
                .IsPointerOverGameObject();
    }

    private void CreateTargetDetectionBuffer()
    {
        targetDetectionBuffer =
            new Collider2D[
                Mathf.Max(
                    1,
                    targetDetectionBufferSize
                )
            ];
    }

    private void EnsureTargetDetectionBuffer()
    {
        int requiredSize =
            Mathf.Max(
                1,
                targetDetectionBufferSize
            );

        if (targetDetectionBuffer != null &&
            targetDetectionBuffer.Length ==
            requiredSize)
        {
            return;
        }

        CreateTargetDetectionBuffer();
    }
}
