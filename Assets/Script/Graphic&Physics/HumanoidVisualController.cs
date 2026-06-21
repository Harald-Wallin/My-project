using UnityEngine;


public enum FacingDirection
{
    Front,
    Back,
    Left,
    Right
}
public class HumanoidVisualController : MonoBehaviour
{
    [Header("=== SKIN RENDERERS ===")]
    [SerializeField] private SpriteRenderer skinHead;
    [SerializeField] private SpriteRenderer skinTorso;
    [SerializeField] private SpriteRenderer skinArms;
    [SerializeField] private SpriteRenderer skinLegs;
    [SerializeField] private SpriteRenderer skinFeet;

    [Header("=== HAIR / BEARD ===")]
    [SerializeField] private SpriteRenderer hair;
    [SerializeField] private SpriteRenderer beard;

    [Header("ANIMATIONS")]
    private float animationTimer;
    private int animationFrame;
    [SerializeField] private float frameRate = 8f;
    private HumanoidAnimationState currentState = HumanoidAnimationState.Idle;
    private Vector2 currentDirection = Vector2.down;

    [Header("Initial Facing")]
    [SerializeField] private FacingDirection initialFacing = FacingDirection.Front;

    public HumanoidAnimationSet headAnimations;
    public HumanoidAnimationSet torsoAnimations;
    public HumanoidAnimationSet armsAnimations;
    public HumanoidAnimationSet legsAnimations;
    public HumanoidAnimationSet feetAnimations;


    private void Start()
    {
        ApplyInitialFacing();
    }

    void Update()
    {
        animationTimer += Time.deltaTime;

        if (animationTimer > 1f / frameRate)
        {
            animationTimer = 0f;
            animationFrame++;
        }
        UpdateSkinDirection(currentDirection);
    }

    public void UpdateSkinDirection(Vector2 dir)
    {
        if (dir == Vector2.zero)
            dir = Vector2.down;

        currentDirection = dir;

        DirectionalSpriteSet headSet = GetSet(headAnimations);
        DirectionalSpriteSet torsoSet = GetSet(torsoAnimations);
        DirectionalSpriteSet armsSet = GetSet(armsAnimations);
        DirectionalSpriteSet legsSet = GetSet(legsAnimations);
        DirectionalSpriteSet feetSet = GetSet(feetAnimations);

        if (headSet != null)
            skinHead.sprite = headSet.GetSprite(dir, animationFrame);

        if (torsoSet != null)
            skinTorso.sprite = torsoSet.GetSprite(dir, animationFrame);

        if (armsSet != null)
            skinArms.sprite = armsSet.GetSprite(dir, animationFrame);

        if (legsSet != null)
            skinLegs.sprite = legsSet.GetSprite(dir, animationFrame);

        if (feetSet != null)
            skinFeet.sprite = feetSet.GetSprite(dir, animationFrame);
    }



    DirectionalSpriteSet GetSet(HumanoidAnimationSet set)
    {
        switch (currentState)
        {
            case HumanoidAnimationState.Walk:
                return set.walk;

            default:
                return set.idle;
        }
    }

    public void SetAnimationState(HumanoidAnimationState state)
    {
        if (currentState == state)
            return;

        currentState = state;
        animationFrame = 0;
    }

    public void SetHairVisible(bool state)
    {
        if (hair != null)
            hair.enabled = state;
    }

    public void SetBeardVisible(bool state)
    {
        if (beard != null)
            beard.enabled = state;
    }

    public void SetTorsoVisible(bool state)
    {
        if (skinTorso != null)
            skinTorso.enabled = state;
    }

    public void SetArmsVisible(bool state)
    {
        if (skinArms != null)
            skinArms.enabled = state;
    }

    public void SetHeadVisible(bool state)
    {
        if (skinHead != null)
            skinHead.enabled = state;
    }

    public void SetLegsVisible(bool state)
    {
        if (skinLegs != null)
            skinLegs.enabled = state;
    }

    public void SetFeetVisible(bool state)
    {
        if (skinFeet != null)
            skinFeet.enabled = state;
    }

    void ApplyInitialFacing()
    {
        switch (initialFacing)
        {
            case FacingDirection.Front:
                currentDirection = Vector2.down;
                break;

            case FacingDirection.Back:
                currentDirection = Vector2.up;
                break;

            case FacingDirection.Left:
                currentDirection = Vector2.left;
                break;

            case FacingDirection.Right:
                currentDirection = Vector2.right;
                break;
        }

        UpdateSkinDirection(currentDirection);
    }
}