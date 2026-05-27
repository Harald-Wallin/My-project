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
        transform.Translate(moveDirection * moveUpSpeed * Time.deltaTime);

        timer -= Time.deltaTime;

        if (canvasGroup != null)
            canvasGroup.alpha = Mathf.Clamp01(timer / lifetime);

        if (timer <= 0f)
            Destroy(gameObject);
    }

    public void Setup(string value, bool isCrit)
    {
        text.text = value;

        if (value == "Miss")
            text.color = Color.gray;

        else if (value == "Evade")
            text.color = Color.cyan;

        else if (isCrit)
        {
            text.color = Color.red;
            text.fontSize *= 1.5f;
            lifetime += 0.3f;
        }
    }
}






