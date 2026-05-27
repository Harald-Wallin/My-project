using UnityEngine;

public class CorpseGlow : MonoBehaviour
{
    [SerializeField] private float pulseSpeed = 2f;
    [SerializeField] private float minAlpha = 0.6f;
    [SerializeField] private float maxAlpha = 1f;

    private SpriteRenderer[] renderers;
    private LootContainer loot;

    void Awake()
    {
        renderers = GetComponentsInChildren<SpriteRenderer>();
        loot = GetComponent<LootContainer>();
    }

    void Update()
    {
        if (loot == null || loot.items.Count == 0)
            return;

        float t = (Mathf.Sin(Time.time * pulseSpeed) + 1f) / 2f;
        float alpha = Mathf.Lerp(minAlpha, maxAlpha, t);

        foreach (var sr in renderers)
        {
            Color c = sr.color;
            c.a = alpha;
            sr.color = c;
        }
    }
}

