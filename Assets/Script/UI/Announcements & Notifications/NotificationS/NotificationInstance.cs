using TMPro;
using UnityEngine;

public class NotificationInstance : MonoBehaviour
{
    [SerializeField]
    private TMP_Text text;

    [SerializeField]
    private CanvasGroup canvasGroup;

    private NotificationData data;

    private float timer;

    private RectTransform rectTransform;

    void Awake()
    {
        rectTransform =
            GetComponent<RectTransform>();
    }

    public void Initialize(NotificationData notification)
    {
        data = notification;

        text.text = data.message;
        text.color = data.textColor;
        text.fontSize = data.fontSize;

        timer = data.duration;

        canvasGroup.alpha = 1f;
    }

    void Update()
    {
        if (data == null)
            return;

        timer -= Time.deltaTime;

        HandleAnimation();

        float fadeStartTime =
            data.duration * 0.5f;

        if (timer <= fadeStartTime)
        {
            canvasGroup.alpha =
                timer / fadeStartTime;
        }

        if (timer <= 0f)
        {
            Destroy(gameObject);
        }
    }

    void HandleAnimation()
    {
        switch (data.animationType)
        {
            case NotificationAnimationType.FloatUp:

                rectTransform.anchoredPosition +=
                    Vector2.up * 50f * Time.deltaTime;

                break;
        }
    }
}
