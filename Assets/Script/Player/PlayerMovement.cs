using TMPro;
using Unity.VisualScripting.Antlr3.Runtime.Misc;
using UnityEngine;
using UnityEngine.EventSystems;

public class PlayerMovement : MonoBehaviour

{
    private HumanoidVisualController visual;
    private HumanoidEquipment equipment;
    private SpriteRenderer spriteRenderer;

    [SerializeField] float skinWidth = 0.02f;

    private CharacterStats stats;
    private Vector3 lastMousePosition;
    private bool mouseMoved;
    private Vector2 lastMovement;

    private CharacterStateController stateController;

    private Rigidbody2D rb;
    private Vector2 movement;

    // NY RAD
    public Vector2 FacingDirection { get; private set; } = Vector2.down;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        visual = GetComponentInChildren<HumanoidVisualController>();
        equipment = GetComponent<HumanoidEquipment>();
        stats = GetComponent<CharacterStats>();
        stateController = GetComponent<CharacterStateController>();

    }

    void Start()
    {
        if (visual != null)
        {
            visual.SetFacing(FacingDirection);
            visual.SetMoving(false);
        }
    }

    void Update()
    {
        if (IsTypingInInputField())
        {
            rb.linearVelocity = Vector2.zero;
            return;
        }

        Vector2 currentMovement;
        currentMovement.x = Input.GetAxisRaw("Horizontal");
        currentMovement.y = Input.GetAxisRaw("Vertical");
        currentMovement = currentMovement.normalized;

        movement = currentMovement;
        // Kolla om movement ändrades
        if (currentMovement != lastMovement)
        {
            if (stateController == null || stateController.CanRotate)
            {
                if (currentMovement != Vector2.zero)
                {
                    UpdateFacingDirectionFromMovement();
                }
            }

            lastMovement = currentMovement;
        }

        // --- Kolla mus ---
        if (stateController == null || stateController.CanRotate)
        {
            if (Input.mousePosition != lastMousePosition)
            {
                UpdateFacingDirectionFromMouse();
                lastMousePosition = Input.mousePosition;
            }
        }

        bool moving = (stateController == null || stateController.CanMove) && movement != Vector2.zero;

        if (visual != null)
        {
            visual.SetFacing(FacingDirection);
            visual.SetMoving(moving);
        }

        if (equipment != null)
        {
            equipment.UpdateVisualDirection(FacingDirection);
        }
    }

    bool IsTypingInInputField()
    {
        if (EventSystem.current == null)
            return false;

        GameObject selected =
            EventSystem.current.currentSelectedGameObject;

        if (selected == null)
            return false;

        return selected.GetComponent<TMP_InputField>() != null;
    }

    void UpdateFacingDirectionFromMouse()
    {
        Vector3 mouseWorld = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        mouseWorld.z = 0f;

        Vector2 direction = (mouseWorld - transform.position).normalized;

        if (Mathf.Abs(direction.y) > Mathf.Abs(direction.x))
        {
            FacingDirection = direction.y > 0 ? Vector2.up : Vector2.down;
        }
        else
        {
            FacingDirection = direction.x > 0 ? Vector2.right : Vector2.left;
        }
    }

    void UpdateFacingDirectionFromMovement()
    {
        if (Mathf.Abs(movement.y) > Mathf.Abs(movement.x))
        {
            FacingDirection = movement.y > 0 ? Vector2.up : Vector2.down;
        }
        else
        {
            FacingDirection = movement.x > 0 ? Vector2.right : Vector2.left;
        }
    }



    void FixedUpdate()
    {
        // 🔒 Stoppa ALL extern fysikpåverkan
        rb.linearVelocity = Vector2.zero;

        if (stateController != null && !stateController.CanMove)
        {
            rb.linearVelocity = Vector2.zero;
            return;
        }

        if (movement == Vector2.zero)
            return;

        /*float speedMultiplier = GetComponent<CharacterStats>().GetFinalMoveSpeedMultiplier();*/
        float moveSpeedStat = stats.GetStat(StatType.MovementSpeed);

        Vector2 desiredMove =
         movement * moveSpeedStat * Time.fixedDeltaTime;

        ContactFilter2D filter = new ContactFilter2D();
        filter.useLayerMask = true;
        filter.layerMask = LayerMask.GetMask("Blocking", "Enemy", "NPC");
        filter.useTriggers = false;

        RaycastHit2D[] hits = new RaycastHit2D[1];

        int hitCount = rb.Cast(
            desiredMove.normalized,
            filter,
            hits,
            desiredMove.magnitude + skinWidth
        );


        if (hitCount == 0)
        {
            rb.MovePosition(rb.position + desiredMove * (1f - skinWidth));

        }
        else
        {
            Vector2 hitNormal = hits[0].normal;

            float dot = Vector2.Dot(desiredMove, hitNormal);

            if (dot < 0f)
            {
                // 1️⃣ Rör oss BORT från hindret → tillåt FULL rörelse
                rb.MovePosition(rb.position + desiredMove);
            }
            else
            {
                // 2️⃣ Rör oss IN i eller längs hindret → glid
                Vector2 slideMove = desiredMove - hitNormal * dot;

                if (slideMove.sqrMagnitude > 0.0001f)
                {
                    rb.MovePosition(rb.position + slideMove);
                }
            }
        }

    }

}




