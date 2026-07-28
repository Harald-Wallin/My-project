using UnityEngine;

public sealed class LootableCorpse :
    MonoBehaviour
{
    [Header("Identity")]

    [SerializeField]
    private string corpseName;

    [Header("Interaction")]

    [SerializeField]
    private float lootRange = 2f;

    [Header("Visuals")]

    [SerializeField]
    private GameObject lootShimmer;

    [Header("Lifetime")]

    [SerializeField]
    private float emptyCorpseLifetime = 10f;

    [SerializeField]
    private float lootCorpseLifetime = 60f;

    private LootContainer loot;
    private Transform player;
    private GameObject shimmerInstance;

    public string CorpseName =>
        corpseName;

    private void Awake()
    {
        loot =
            GetComponent<
                LootContainer>();

        if (loot == null)
        {
            Debug.LogError(
                "LootableCorpse: Missing LootContainer.",
                this
            );
        }

        PlayerMovement playerMovement =
            FindFirstObjectByType<
                PlayerMovement>();

        if (playerMovement != null)
        {
            player =
                playerMovement.transform;
        }
        else
        {
            Debug.LogError(
                "LootableCorpse: PlayerMovement not found.",
                this
            );
        }
    }

    private void Start()
    {
        /*
         * Loot genereras efter Instantiate har anropat Awake.
         * Därför bestäms lifetime först i Start.
         */
        float lifetime =
            loot != null &&
            loot.HasLoot
                ? lootCorpseLifetime
                : emptyCorpseLifetime;

        Destroy(
            gameObject,
            lifetime
        );

        UpdateShimmer();
    }

    private void Update()
    {
        if (Input.GetMouseButtonDown(
                1))
        {
            TryLoot();
        }
    }

    private void UpdateShimmer()
    {
        bool hasLoot =
            loot != null &&
            loot.HasLoot;

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

                shimmerInstance
                    .transform
                    .localPosition =
                    Vector3.zero;
            }

            return;
        }

        if (shimmerInstance != null)
        {
            Destroy(
                shimmerInstance
            );

            shimmerInstance =
                null;
        }
    }

    public void RefreshVisuals()
    {
        UpdateShimmer();
    }

    private void TryLoot()
    {
        if (loot == null ||
            player == null ||
            LootUI.Instance == null)
        {
            return;
        }

        Camera mainCamera =
            Camera.main;

        if (mainCamera == null)
            return;

        Vector2 mouseWorld =
            mainCamera.ScreenToWorldPoint(
                Input.mousePosition
            );

        Collider2D collider =
            GetComponent<Collider2D>();

        if (collider == null ||
            !collider.OverlapPoint(
                mouseWorld))
        {
            return;
        }

        if (Vector2.Distance(
                transform.position,
                player.position) >
            lootRange)
        {
            Debug.Log(
                "Too far away to loot.",
                this
            );

            return;
        }

        if (!loot.HasLoot)
            return;

        LootUI.Instance.Show(
            loot,
            corpseName
        );
    }
}