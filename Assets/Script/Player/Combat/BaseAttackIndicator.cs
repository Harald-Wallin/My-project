using UnityEngine;

public class BaseAttackIndicator : MonoBehaviour
{
    [SerializeField] private Transform indicator;
    [SerializeField] private Transform attackOrigin;

    [SerializeField] private float distance = 0.5f;

    void Update()
    {
        if (indicator == null || attackOrigin == null)
            return;

        Vector3 mouseWorld =
            Camera.main.ScreenToWorldPoint(Input.mousePosition);

        mouseWorld.z = 0f;

        Vector2 direction =
            (mouseWorld - attackOrigin.position).normalized;

        indicator.position =
            attackOrigin.position +
            (Vector3)(direction * distance);

        float angle =
            Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;

        indicator.rotation =
            Quaternion.Euler(0f, 0f, angle);
    }
}