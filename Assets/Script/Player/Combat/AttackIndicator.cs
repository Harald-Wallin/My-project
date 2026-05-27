using UnityEngine;

public class AttackIndicator : MonoBehaviour
{
    [Header("References")]
    private BaseAttackController baseAttackController;
    public Transform marker;          // Marker-pilen

    [Header("Ellipse Settings")]
    public float radiusX = 1f;        // Horisontell radie
    public float radiusY = 0.5f;      // Vertikal radie

    [Header("Marker Settings")]
    [SerializeField] private Vector3 baseScale = Vector3.one; // Marker utgångsskala
    private SpriteRenderer markerSpriteRenderer;
    private Color originalColor;
    public Transform cooldownFill;



    void Awake()
    {
        baseAttackController = GetComponentInParent<BaseAttackController>();

        if (marker != null)
        {
            markerSpriteRenderer =
                marker.GetComponent<SpriteRenderer>();

            originalColor =
                markerSpriteRenderer.color;
        }
    }

    void Update()
    {
        if (baseAttackController == null || marker == null)
            return;

        UpdateMarkerPositionAndRotation();
    }

    void UpdateMarkerPositionAndRotation()
    {
        // 1️⃣ Räkna ut attackDirection mot musen
        Vector2 direction = baseAttackController.CurrentDirection.normalized;
        if (direction.sqrMagnitude < 0.0001f) return;

        // 2️⃣ Placera marker på ellipsen längs attackDirection
        float x = direction.x * radiusX;
        float y = direction.y * radiusY;
        marker.localPosition = new Vector3(x, y, 0f);

        // 3️⃣ Rotera marker så att spetsen pekar mot attackDirection
        float angleDegrees = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        float spriteOffset = 90f; // justera om din pil sprite pekar åt annat håll
        marker.rotation = Quaternion.Euler(0f, 0f, angleDegrees + spriteOffset);

        // 4️⃣ 3D-skala för djup/känsla
        Vector3 scale = baseScale;
        scale.y *= (1f + 0.5f * -direction.y);                  // vänder på Y-skalan
        scale.x *= (1f + 0.3f * (1f - Mathf.Abs(direction.x))); // X-skala samma som förut
        marker.localScale = scale;


        if (cooldownFill != null)
        {
            float normalized = Mathf.Clamp01(baseAttackController.GetCooldownNormalized());

            // 1 när redo, 0 när precis attackerad
            float fillAmount = 1f - normalized;

            Vector3 fillScale = baseScale;
            fillScale.x *= fillAmount; // krymper från full → 0 under cooldown

            cooldownFill.localScale = fillScale;

            // Aktivera bara under cooldown
            cooldownFill.gameObject.SetActive(fillAmount > 0.01f);
        }
    }

}

