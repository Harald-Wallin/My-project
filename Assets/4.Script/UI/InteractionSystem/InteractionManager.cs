using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// Central ingångspunkt för spelarens interaktioner med världen.
///
/// Managern:
/// - läser högerklick,
/// - hittar ett InteractionTarget,
/// - kontrollerar avstånd,
/// - filtrerar tillgängliga interaktioner,
/// - kör ett ensamt alternativ direkt,
/// - förbereder flera alternativ för en valmeny.
/// </summary>
[DisallowMultipleComponent]
public sealed class InteractionManager : MonoBehaviour
{
    private readonly List<IInteractionOption>
        availableOptions = new();

    [Header("Input")]

    [Tooltip(
        "Musknapp eller tangent som startar world interaction.")]
    [SerializeField]
    private KeyCode interactionInput =
        KeyCode.Mouse1;

    [Header("Raycast")]

    [Tooltip(
        "Layer som innehåller InteractionTarget-colliders.")]
    [SerializeField]
    private LayerMask interactionLayerMask;

    [Header("References")]

    [SerializeField]
    private Camera worldCamera;

    private PlayerStats player;

    public static InteractionManager Instance
    {
        get;
        private set;
    }

    public InteractionTarget CurrentTarget
    {
        get;
        private set;
    }

    public IReadOnlyList<IInteractionOption>
        CurrentOptions => availableOptions;

    public bool HasCurrentInteraction =>
        CurrentTarget != null &&
        availableOptions.Count > 0;

    private void Awake()
    {
        if (Instance != null &&
            Instance != this)
        {
            Debug.LogError(
                "Flera InteractionManager finns i scenen. " +
                "Den nya komponenten tas bort.",
                this);

            Destroy(gameObject);
            return;
        }

        Instance = this;

        ResolveWorldCamera();
    }

    private void Start()
    {
        ResolvePlayer();

        if (player == null)
        {
            Debug.LogError(
                "InteractionManager kunde inte hitta spelaren " +
                "via PlayerReference.Player.",
                this);
        }

        if (worldCamera == null)
        {
            Debug.LogError(
                "InteractionManager saknar en world camera.",
                this);
        }
    }

    private void Update()
    {
        if (!Input.GetKeyDown(interactionInput))
            return;

        if (IsTypingInInputField())
            return;

        if (IsPointerOverUI())
            return;

        TryInteractAtPointer();
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }

    /// <summary>
    /// Försöker hitta och interagera med ett target
    /// under muspekaren.
    /// </summary>
    public bool TryInteractAtPointer()
    {
        if (!EnsureReferences())
            return false;

        Vector3 mouseWorldPosition =
            worldCamera.ScreenToWorldPoint(
                Input.mousePosition);

        Vector2 interactionPoint =
            new(
                mouseWorldPosition.x,
                mouseWorldPosition.y);

        Collider2D hit =
            Physics2D.OverlapPoint(
                interactionPoint,
                interactionLayerMask);

        if (hit == null)
        {
            ClearCurrentInteraction();
            return false;
        }

        InteractionTarget target =
            FindInteractionTarget(hit);

        if (target == null)
        {
            Debug.LogWarning(
                $"Collider '{hit.name}' ligger på interaction-lagret " +
                "men saknar InteractionTarget.",
                hit);

            ClearCurrentInteraction();
            return false;
        }

        return TryInteract(target);
    }

    /// <summary>
    /// Försöker interagera med ett specifikt target.
    /// Kan senare även användas av exempelvis tangentbordsinteraktion
    /// eller gamepad-fokusering.
    /// </summary>
    public bool TryInteract(
        InteractionTarget target)
    {
        if (target == null)
            return false;

        if (!EnsureReferences())
            return false;

        if (!target.IsWithinInteractionDistance(
                player.transform))
        {
            ClearCurrentInteraction();
            return false;
        }

        target.GetInteractionOptions(
            availableOptions);

        InteractionContext context =
            new(
                player,
                target);

        RemoveUnavailableOptions(
            context);

        if (availableOptions.Count == 0)
        {
            ClearCurrentInteraction();
            return false;
        }

        CurrentTarget = target;

        if (availableOptions.Count == 1)
        {
            return ExecuteOption(
                availableOptions[0],
                context);
        }

        OpenSelectionWindow(context);
        return true;
    }

    /// <summary>
    /// Exekverar ett specifikt interaktionsalternativ.
    ///
    /// Metoden används senare av InteractionSelectionWindow
    /// när spelaren har valt ett alternativ.
    /// </summary>
    public bool ExecuteOption(
        IInteractionOption option)
    {
        if (option == null ||
            CurrentTarget == null)
        {
            return false;
        }

        if (!EnsureReferences())
            return false;

        InteractionContext context =
            new(
                player,
                CurrentTarget);

        return ExecuteOption(
            option,
            context);
    }

    public void ClearCurrentInteraction()
    {
        CurrentTarget = null;
        availableOptions.Clear();
    }

    private bool ExecuteOption(
        IInteractionOption option,
        in InteractionContext context)
    {
        if (option == null ||
            !context.IsValid)
        {
            return false;
        }

        if (!context.Target
                .IsWithinInteractionDistance(
                    context.Player.transform))
        {
            ClearCurrentInteraction();
            return false;
        }

        if (!option.CanInteract(context))
        {
            ClearCurrentInteraction();
            return false;
        }

        option.Interact(context);
        return true;
    }

    private void RemoveUnavailableOptions(
        in InteractionContext context)
    {
        for (int i =
                 availableOptions.Count - 1;
             i >= 0;
             i--)
        {
            IInteractionOption option =
                availableOptions[i];

            if (option == null ||
                !option.CanInteract(context))
            {
                availableOptions.RemoveAt(i);
            }
        }
    }

    private void OpenSelectionWindow(
        in InteractionContext context)
    {
        /*
         * Nästa implementation kopplar in
         * InteractionSelectionWindow här.
         *
         * Vi kör inte det första alternativet automatiskt när
         * flera alternativ finns. Det skulle kunna öppna exempelvis
         * Vendor när spelaren avsåg att välja en favour.
         */

        Debug.Log(
            $"'{context.Target.InteractionOwner.name}' har " +
            $"{availableOptions.Count} tillgängliga interaktioner. " +
            "InteractionSelectionWindow behöver öppnas.",
            context.Target);
    }

    private static InteractionTarget
        FindInteractionTarget(
            Collider2D hit)
    {
        if (hit == null)
            return null;

        InteractionTarget target =
            hit.GetComponent<InteractionTarget>();

        if (target != null)
            return target;

        return hit.GetComponentInParent<
            InteractionTarget>();
    }

    private bool EnsureReferences()
    {
        if (player == null)
        {
            ResolvePlayer();
        }

        if (worldCamera == null)
        {
            ResolveWorldCamera();
        }

        return player != null &&
               worldCamera != null;
    }

    private void ResolvePlayer()
    {
        player = PlayerReference.Player;
    }

    private void ResolveWorldCamera()
    {
        if (worldCamera == null)
        {
            worldCamera = Camera.main;
        }
    }

    private static bool IsPointerOverUI()
    {
        return EventSystem.current != null &&
               EventSystem.current
                   .IsPointerOverGameObject();
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

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (interactionLayerMask.value == 0)
        {
            interactionLayerMask =
                LayerMask.GetMask(
                    "Transparent Interactable");
        }
    }
#endif
}