using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// Global hover-resolution för CharacterStats-entiteter.
///
/// När musen lämnar en karaktär behålls dess hoverpresentation
/// under en kort period. Om en annan karaktär hovras växlar
/// systemet däremot omedelbart till den nya karaktären.
/// </summary>
public sealed class CharacterHoverController :
    MonoBehaviour
{
    [Header("Physics")]

    [SerializeField]
    private Camera worldCamera;

    [SerializeField]
    private LayerMask characterLayers =
        ~0;

    [Header("UI Blocking")]

    [SerializeField]
    private bool ignoreWorldHoverWhenPointerIsOverUI =
        true;

    [Header("Hover Retention")]

    [SerializeField]
    [Min(0f)]
    [Tooltip(
        "Hur länge nameplate och tooltip ligger kvar efter " +
        "att musen lämnat karaktären."
    )]
    private float hoverRetentionDuration =
        2f;

    private CharacterStats hoveredCharacter;
    private NameplateUI hoveredNameplate;

    private float hoverRetentionTimer;
    private bool isPointerCurrentlyOverCharacter;

    private void Awake()
    {
        if (worldCamera == null)
        {
            worldCamera =
                Camera.main;
        }
    }

    private void Update()
    {
        if (worldCamera == null)
            return;

        bool pointerBlockedByUI =
            ignoreWorldHoverWhenPointerIsOverUI &&
            EventSystem.current != null &&
            EventSystem.current
                .IsPointerOverGameObject();

        CharacterStats candidate =
            pointerBlockedByUI
                ? null
                : ResolveHoveredCharacter();

        UpdateHover(
            candidate
        );
    }

    private void UpdateHover(
        CharacterStats candidate)
    {
        if (candidate != null)
        {
            isPointerCurrentlyOverCharacter =
                true;

            hoverRetentionTimer =
                hoverRetentionDuration;

            if (candidate !=
                hoveredCharacter)
            {
                SetHoveredCharacter(
                    candidate
                );
            }

            return;
        }

        isPointerCurrentlyOverCharacter =
            false;

        if (hoveredCharacter == null)
            return;

        hoverRetentionTimer -=
            Time.unscaledDeltaTime;

        if (hoverRetentionTimer <= 0f)
        {
            ClearHover();
        }
    }

    private CharacterStats
        ResolveHoveredCharacter()
    {
        Vector3 mouseWorld =
            worldCamera.ScreenToWorldPoint(
                Input.mousePosition
            );

        Vector2 point =
            new Vector2(
                mouseWorld.x,
                mouseWorld.y
            );

        Collider2D collider =
            Physics2D.OverlapPoint(
                point,
                characterLayers
            );

        if (collider == null)
            return null;

        return
            TargetUtility.GetCharacterStats(
                collider
            );
    }

    private void SetHoveredCharacter(
        CharacterStats character)
    {
        /*
         * Vid direkt växling från en NPC till en annan ska den
         * gamla inte ligga kvar under retentiontiden.
         */
        ClearCurrentPresentation();

        if (character == null)
            return;

        hoveredCharacter =
            character;

        hoverRetentionTimer =
            hoverRetentionDuration;

        NameplateUI.TryGet(
            character,
            out hoveredNameplate
        );

        hoveredNameplate?.SetHovered(
            true
        );

        if (ItemTooltip.Instance != null)
        {
            ItemTooltip.Instance
                .ShowFixedBottomRight(
                    new CharacterTooltipProvider(
                        character
                    ),
                    PlayerReference.Player
                );
        }
    }

    private void ClearHover()
    {
        ClearCurrentPresentation();

        hoveredCharacter =
            null;

        hoveredNameplate =
            null;

        hoverRetentionTimer =
            0f;

        isPointerCurrentlyOverCharacter =
            false;
    }

    private void ClearCurrentPresentation()
    {
        hoveredNameplate?.SetHovered(
            false
        );

        ItemTooltip.Instance?.Hide();
    }

    private void OnDisable()
    {
        ClearHover();
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        hoverRetentionDuration =
            Mathf.Max(
                0f,
                hoverRetentionDuration
            );
    }
#endif
}