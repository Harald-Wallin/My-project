using TMPro;
using UnityEngine;

public class FloatingText : MonoBehaviour
{
    [SerializeField] private TMP_Text text;

    public float moveUpSpeed = 8f;
    public float lifetime = 1.2f;

    private float timer;
    private Vector3 moveDirection;
    private CanvasGroup canvasGroup;

    private bool isCritText;
    private float critHoldTimer;

    void Awake()
    {
        canvasGroup = GetComponentInParent<CanvasGroup>();
        timer = lifetime;

        moveDirection = new Vector3(
            Random.Range(-0.3f, 0.3f),
            1f,
            0f
        ).normalized;
    }

    void Update()
    {
        if (isCritText)
        {
            HandleCritAnimation();
        }
        else
        {
            transform.Translate(
                moveDirection *
                moveUpSpeed *
                Time.deltaTime
            );
        }

        timer -= Time.deltaTime;

        if (canvasGroup != null)
        {
            canvasGroup.alpha =
                Mathf.Clamp01(
                    timer / lifetime
                );
        }

        if (timer <= 0f)
        {
            Destroy(gameObject);
        }
    }

    public void Setup(
    string value,
    FloatingTextStyle style)
    {
        text.text = value;

        switch (style)
        {
            case FloatingTextStyle.PlayerDamage:
                text.color = Color.white;
                break;

            case FloatingTextStyle.EnemyDamage:
                text.color = new Color(
                    1f,
                    0.35f,
                    0.35f
                );
                break;

            case FloatingTextStyle.PlayerCrit:
                SetupCritStyle(
                    new Color(
                        1f,
                        0.97f,
                        0.82f
                    )
                );
                break;

            case FloatingTextStyle.EnemyCrit:
                SetupCritStyle(
                    new Color(
                        1f,
                        0.55f,
                        0.45f
                    )
                );
                break;

            case FloatingTextStyle.Miss:
                text.color = Color.gray;
                text.fontSize *= 0.7f;
                break;

            case FloatingTextStyle.Evade:
                text.color = Color.gray;
                text.fontSize *= 0.7f;
                break;
        }
    }

    void SetupCritStyle(Color color)
    {
        isCritText = true;

        critHoldTimer = 0.25f;

        text.color = color;

        text.fontSize *= 2f;

        lifetime += 0.4f;
    }

    void HandleCritAnimation()
    {
        if (critHoldTimer > 0f)
        {
            critHoldTimer -= Time.deltaTime;

            float pulse =
                1f +
                Mathf.Sin(
                    Time.time * 25f
                ) * 0.08f;

            transform.localScale =
                Vector3.one * pulse;

            return;
        }

        transform.Translate(
            moveDirection *
            moveUpSpeed *
            Time.deltaTime
        );
    }
}






