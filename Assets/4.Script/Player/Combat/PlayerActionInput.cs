using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// Spelarens centrala inputbrygga till actionsystemet.
///
/// Ansvar:
/// - vanliga abilities
/// - base attacks
/// - targeting-preview
/// - Hold-and-Release
/// - charge-aim
/// - charge-release
/// - charge-cancel
/// - base attack-swap
///
/// All spelarinput för Charge/HoldAndRelease ska gå genom
/// denna komponent.
/// </summary>
[RequireComponent(
    typeof(CharacterActionController)
)]
public sealed class PlayerActionInput :
    MonoBehaviour
{
    private enum HeldActionInputSource
    {
        None,

        BaseAttackMouse,

        ActionSlotHotkey,

        ActionSlotPointer
    }

    [Header("World Input")]

    [SerializeField]
    private Camera worldCamera;

    [Header("Base Attack")]

    [SerializeField]
    private KeyCode baseAttackSwapKey =
        KeyCode.Tab;

    [SerializeField]
    [Tooltip(
        "Förhindrar byte av base attack medan en action pågår."
    )]
    private bool blockSwapDuringAction =
        true;

    [Header("Target Detection")]

    [SerializeField]
    [Min(1)]
    private int targetDetectionBufferSize =
        16;

    [Header("Cancellation")]

    [SerializeField]
    private KeyCode cancelKey =
        KeyCode.Escape;

    [SerializeField]
    private bool rightClickCancels =
        true;

    private CharacterActionController
        actionController;

    private PlayerBaseAttackCollection
        baseAttackCollection;

    private Collider2D[]
        targetDetectionBuffer;

    private AbilityData heldAbility;

    private HeldActionInputSource
        heldInputSource =
            HeldActionInputSource.None;

    private int previewStartedFrame =
        -1;

    private readonly List<RaycastResult>
        uiRaycastResults =
            new();

    public CharacterActionController
        ActionController =>
            actionController;

    public bool HasHeldAbilityInput =>
        heldAbility != null;

    public AbilityData HeldAbility =>
        heldAbility;

    private void Awake()
    {
        ResolveReferences();
        CreateTargetDetectionBuffer();
        ResolveWorldCamera();
    }

    private void OnEnable()
    {
        ResolveReferences();

        if (actionController != null)
        {
            actionController.OnPreviewStarted -=
                HandlePreviewStarted;

            actionController.OnPreviewStarted +=
                HandlePreviewStarted;

            actionController.OnActionCancelled -=
                HandleActionEnded;

            actionController.OnActionCancelled +=
                HandleActionEnded;

            actionController.OnActionCompleted -=
                HandleActionEnded;

            actionController.OnActionCompleted +=
                HandleActionEnded;
        }
    }

    private void OnDisable()
    {
        if (actionController != null)
        {
            actionController.OnPreviewStarted -=
                HandlePreviewStarted;

            actionController.OnActionCancelled -=
                HandleActionEnded;

            actionController.OnActionCompleted -=
                HandleActionEnded;
        }

        CancelHeldAbilityInput();
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
        HandleBaseAttackSwapInput();
        HandleBaseAttackMouseInput();
        HandleHeldActionInput();
        HandlePreviewInput();
    }

    private void ResolveReferences()
    {
        if (actionController == null)
        {
            actionController =
                GetComponent<
                    CharacterActionController>();
        }

        if (baseAttackCollection == null)
        {
            baseAttackCollection =
                GetComponent<
                    PlayerBaseAttackCollection>();
        }
    }

    // =========================================================
    // BASE ATTACK INPUT
    // =========================================================

    private void HandleBaseAttackMouseInput()
    {
        if (baseAttackCollection == null ||
            actionController == null)
        {
            return;
        }

        AbilityData attack =
            baseAttackCollection
                .GetActiveAttack();

        if (attack == null)
            return;

        /*
         * Musknappen tillhör redan en aktiv Hold-and-Release-
         * action från en annan inputkälla.
         */
        if (heldInputSource !=
                HeldActionInputSource.None &&
            heldInputSource !=
                HeldActionInputSource
                    .BaseAttackMouse)
        {
            return;
        }

        if (Input.GetMouseButtonDown(0))
        {
            if (IsPointerBlockingWorldInput())
                return;

            BeginAbilityInput(
                attack,
                HeldActionInputSource
                    .BaseAttackMouse
            );
        }

        if (Input.GetMouseButtonUp(0) &&
            heldInputSource ==
                HeldActionInputSource
                    .BaseAttackMouse)
        {
            ReleaseAbilityInput(
                attack,
                HeldActionInputSource
                    .BaseAttackMouse
            );
        }
    }

    private void HandleBaseAttackSwapInput()
    {
        if (!Input.GetKeyDown(
                baseAttackSwapKey))
        {
            return;
        }

        if (IsTypingInInputField())
            return;

        if (baseAttackCollection == null)
        {
            baseAttackCollection =
                GetComponent<
                    PlayerBaseAttackCollection>();
        }

        if (baseAttackCollection == null)
            return;

        if (blockSwapDuringAction &&
            actionController != null &&
            actionController.HasActiveAction)
        {
            return;
        }

        baseAttackCollection
            .CycleActiveAttack();
    }

    // =========================================================
    // GENERIC ABILITY INPUT
    // =========================================================

    /// <summary>
    /// Används av ActionSlot för en hotkey som trycks ned.
    /// </summary>
    public bool BeginAbilityHotkeyInput(
        AbilityData ability)
    {
        return BeginAbilityInput(
            ability,
            HeldActionInputSource
                .ActionSlotHotkey
        );
    }

    /// <summary>
    /// Används av ActionSlot när en hotkey släpps.
    /// </summary>
    public bool ReleaseAbilityHotkeyInput(
        AbilityData ability)
    {
        return ReleaseAbilityInput(
            ability,
            HeldActionInputSource
                .ActionSlotHotkey
        );
    }

    /// <summary>
    /// Används när spelaren trycker ned vänster musknapp
    /// på en actionbar-slot.
    /// </summary>
    public bool BeginAbilityPointerInput(
        AbilityData ability)
    {
        return BeginAbilityInput(
            ability,
            HeldActionInputSource
                .ActionSlotPointer
        );
    }

    /// <summary>
    /// Används när spelaren släpper vänster musknapp
    /// efter att ha hållit på en actionbar-slot.
    /// </summary>
    public bool ReleaseAbilityPointerInput(
        AbilityData ability)
    {
        return ReleaseAbilityInput(
            ability,
            HeldActionInputSource
                .ActionSlotPointer
        );
    }

    /// <summary>
    /// Startar inputen för en ability.
    ///
    /// Vanliga abilities:
    /// - aktiveras direkt
    ///
    /// HoldAndRelease:
    /// - startar charge
    /// - sparas som heldAbility
    /// - exekveras först vid ReleaseAbilityInput
    /// </summary>
    private bool BeginAbilityInput(
        AbilityData ability,
        HeldActionInputSource source)
    {
        if (ability == null ||
            actionController == null)
        {
            return false;
        }

        if (heldAbility != null)
            return false;

        bool started =
            TryStartAbility(
                ability
            );

        if (!started)
            return false;

        if (!ability.UsesHoldAndRelease)
        {
            return true;
        }

        if (!actionController.IsCharging)
        {
            return false;
        }

        heldAbility =
            ability;

        heldInputSource =
            source;

        return true;
    }

    private bool ReleaseAbilityInput(
        AbilityData ability,
        HeldActionInputSource source)
    {
        if (heldAbility == null ||
            heldAbility != ability ||
            heldInputSource != source)
        {
            return false;
        }

        AbilityData releasedAbility =
            heldAbility;

        ClearHeldInputState();

        if (actionController == null ||
            !actionController.IsCharging)
        {
            return false;
        }

        /*
         * Targetingen uppdateras på release-framen innan
         * CharacterActionController skapar execution-contexten.
         */
        UpdateActiveChargeTargeting();

        bool released =
            actionController
                .ReleaseCurrentCharge();

        if (!released &&
            actionController.IsCharging)
        {
            actionController
                .CancelCurrentAction();
        }

        return releasedAbility != null &&
               released;
    }

    /// <summary>
    /// Kan användas av drag-and-drop eller annan UI-kod för
    /// att avbryta en pågående Hold-and-Release-input.
    /// </summary>
    public bool CancelHeldAbilityInput()
    {
        bool hadHeldInput =
            heldAbility != null;

        ClearHeldInputState();

        if (actionController != null &&
            actionController.IsCharging)
        {
            actionController
                .CancelCurrentAction();
        }

        return hadHeldInput;
    }

    /// <summary>
    /// Avbryter endast om den angivna abilityn är den som
    /// för närvarande hålls.
    /// </summary>
    public bool CancelHeldAbilityInput(
        AbilityData ability)
    {
        if (ability == null ||
            heldAbility != ability)
        {
            return false;
        }

        return CancelHeldAbilityInput();
    }

    private void ClearHeldInputState()
    {
        heldAbility =
            null;

        heldInputSource =
            HeldActionInputSource.None;
    }

    // =========================================================
    // HELD ACTION UPDATE
    // =========================================================

    private void HandleHeldActionInput()
    {
        if (heldAbility == null)
            return;

        if (actionController == null ||
            !actionController.IsCharging)
        {
            ClearHeldInputState();
            return;
        }

        if (ShouldCancelActiveAction())
        {
            CancelHeldAbilityInput();
            return;
        }

        /*
         * Aim uppdateras varje frame oavsett om charge påverkar
         * damage, range eller båda.
         */
        UpdateActiveChargeTargeting();
    }

    private bool UpdateActiveChargeTargeting()
    {
        if (actionController == null ||
            !actionController.IsCharging)
        {
            return false;
        }

        ActionContext context =
            actionController
                .CurrentContext;

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

        return actionController
            .UpdateChargeTargeting(
                aimPoint,
                explicitTarget,
                requestedDirection
            );
    }

    private bool ShouldCancelActiveAction()
    {
        if (Input.GetKeyDown(
                cancelKey))
        {
            return true;
        }

        if (!rightClickCancels)
            return false;

        if (!Input.GetMouseButtonDown(1))
            return false;

        return !IsPointerBlockingWorldInput();
    }

    // =========================================================
    // ABILITY START
    // =========================================================

    /// <summary>
    /// Startar vilken migrerad ability som helst.
    ///
    /// Metoden fungerar både för:
    /// - base attacks
    /// - vanliga abilities
    /// - instant
    /// - cast
    /// - charge
    /// - samtliga targetingformer
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
            actionController
                .TryStartAction(
                    ability,
                    aimPoint,
                    explicitTarget,
                    requestedDirection
                );

        if (started &&
            actionController.IsPreviewing)
        {
            previewStartedFrame =
                Time.frameCount;
        }

        return started;
    }

    // =========================================================
    // CONFIRMED PREVIEW
    // =========================================================

    private void HandlePreviewInput()
    {
        if (actionController == null ||
            !actionController.IsPreviewing)
        {
            return;
        }

        UpdateCurrentPreview();

        if (ShouldCancelPreview())
        {
            actionController
                .CancelCurrentAction();

            return;
        }

        if (!ShouldConfirmPreview())
            return;

        UpdateCurrentPreview();

        actionController
            .ConfirmCurrentAction();
    }

    public bool UpdateCurrentPreview()
    {
        if (actionController == null ||
            !actionController.IsPreviewing)
        {
            return false;
        }

        ActionContext context =
            actionController
                .CurrentContext;

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

        return actionController
            .UpdatePreview(
                aimPoint,
                explicitTarget,
                requestedDirection
            );
    }

    private void HandlePreviewStarted(
        ActionContext context)
    {
        previewStartedFrame =
            Time.frameCount;
    }

    private void HandleActionEnded(
        ActionContext context)
    {
        ClearHeldInputState();
    }

    private bool ShouldConfirmPreview()
    {
        if (!Input.GetMouseButtonDown(0))
            return false;

        if (Time.frameCount <=
            previewStartedFrame)
        {
            return false;
        }

        return !IsPointerBlockingWorldInput();
    }

    private bool ShouldCancelPreview()
    {
        if (Input.GetKeyDown(
                cancelKey))
        {
            return true;
        }

        if (!rightClickCancels)
            return false;

        if (!Input.GetMouseButtonDown(1))
            return false;

        return !IsPointerBlockingWorldInput();
    }

    // =========================================================
    // TARGET RESOLUTION
    // =========================================================

    private GameObject ResolveExplicitTarget(
        AbilityData ability,
        Vector2 aimPoint)
    {
        if (ability == null)
            return null;

        AbilityTargetingSettings settings =
            ability.TargetingSettings;

        if (settings == null ||
            settings.TargetingMode !=
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
                    TargetUtility
                        .ResolveCharacterTarget(
                            hit.gameObject
                        );
            }

            if (target == null)
                continue;

            if (!TargetValidator
                    .IsSupportedTarget(
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

        if (direction.sqrMagnitude >
            Mathf.Epsilon)
        {
            return direction.normalized;
        }

        ActionContext context =
            actionController != null
                ? actionController.CurrentContext
                : null;

        if (context != null)
        {
            return context.AimDirection;
        }

        return Vector2.down;
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

        Vector3 mouseWorldPosition =
            worldCamera.ScreenToWorldPoint(
                Input.mousePosition
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

    // =========================================================
    // UI BLOCKING
    // =========================================================

    /// <summary>
    /// Nameplates får ta emot hover-raycasts men blockerar inte
    /// world input.
    ///
    /// Övrigt UI blockerar world actions.
    /// </summary>
    private bool IsPointerBlockingWorldInput()
    {
        EventSystem eventSystem =
            EventSystem.current;

        if (eventSystem == null)
            return false;

        PointerEventData pointerData =
            new PointerEventData(
                eventSystem
            )
            {
                position =
                    Input.mousePosition
            };

        uiRaycastResults.Clear();

        eventSystem.RaycastAll(
            pointerData,
            uiRaycastResults
        );

        for (int i = 0;
             i < uiRaycastResults.Count;
             i++)
        {
            GameObject hitObject =
                uiRaycastResults[i]
                    .gameObject;

            if (hitObject == null)
                continue;

            /*
             * Nameplates kan reagera på hover men räknas inte
             * som klickblockerande UI.
             */
            if (hitObject.GetComponentInParent<
                    NameplateUI>() != null)
            {
                continue;
            }

            return true;
        }

        return false;
    }

    private static bool IsTypingInInputField()
    {
        if (EventSystem.current == null)
            return false;

        GameObject selected =
            EventSystem.current
                .currentSelectedGameObject;

        if (selected == null)
            return false;

        return selected.GetComponent<
                   TMP_InputField>() != null;
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