using UnityEngine;

public class LootableCorpse : MonoBehaviour
{
    [SerializeField] private string corpseName;
    [SerializeField] private float lootRange = 2f;
    [SerializeField] private GameObject lootShimmer;

    [SerializeField] private float emptyCorpseLifetime = 10f;
    [SerializeField] private float lootCorpseLifetime = 60f;

    private LootContainer loot;
    private Transform player;
    private GameObject shimmerInstance;
    public string CorpseName => corpseName;

    void Awake()
    {
        // Hämta loot på samma objekt
        loot = GetComponent<LootContainer>();
        if (loot == null)
        {
            Debug.LogError("LootableCorpse: Missing LootContainer");
        }

        // Hitta spelaren (via PlayerMovement)
        PlayerMovement pm = FindFirstObjectByType<PlayerMovement>();
        if (pm != null)
        {
            player = pm.transform;
        }
        else
        {
            Debug.LogError("LootableCorpse: PlayerMovement not found");
        }

        float lifetime = loot != null && loot.items.Count > 0
        ? lootCorpseLifetime
        : emptyCorpseLifetime;

        Destroy(gameObject, lifetime);
    }

    void Start()
    {
        UpdateShimmer();
    }

    void Update()
    {
        if (Input.GetMouseButtonDown(1)) // 1 = högerklick
        {
            TryLoot();
        }
    }

    void UpdateShimmer()
    {
        bool hasLoot =
            loot != null &&
            loot.items != null &&
            loot.items.Count > 0;

        if (hasLoot)
        {
            if (shimmerInstance == null &&
                lootShimmer != null)
            {
                shimmerInstance =
                    Instantiate(
                        lootShimmer,
                        transform
                    );

                shimmerInstance.transform.localPosition =
                    Vector3.zero;
            }
        }
        else
        {
            if (shimmerInstance != null)
            {
                Destroy(shimmerInstance);
                shimmerInstance = null;
            }
        }
    }

    public void RefreshVisuals()
    {
        UpdateShimmer();
    }

    void TryLoot()
    {
        Debug.Log("Clicked on corpse: " + gameObject.name);
        if (loot == null || player == null || LootUI.Instance == null)
            return;

        // Kolla om musen faktiskt är över detta objekt
        Vector2 mouseWorld = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        Collider2D col = GetComponent<Collider2D>();

        if (col == null || !col.OverlapPoint(mouseWorld))
            return;

        if (Vector2.Distance(transform.position, player.position) > lootRange)
        {
            Debug.Log("Too far away to loot");
            return;
        }

        //Om corpse är tomt, visa inte loot UI
        if (loot.items.Count == 0)
        {
            return;
        }

        LootUI.Instance.Show(loot, corpseName);
    }

}
