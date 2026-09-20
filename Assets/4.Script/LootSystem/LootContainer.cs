using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class LootContainer :
    MonoBehaviour,
    ILootSource,
    IInteractionOption
{
    [System.Serializable]
    private sealed class WeightedRespawnNode
    {
        [SerializeField]
        private Transform node;

        [SerializeField, Min(0f)]
        private float weight = 1f;

        public Transform Node =>
            node;

        public float Weight =>
            Mathf.Max(
                0f,
                weight
            );

#if UNITY_EDITOR
        public void Normalize()
        {
            weight =
                Mathf.Max(
                    0f,
                    weight
                );
        }
#endif
    }


    // =====================================================
    // IDENTITY
    // =====================================================

    [Header("Identity")]

    [SerializeField]
    private string containerName;


    // =====================================================
    // LOOT
    // =====================================================

    [Header("Loot")]

    [SerializeField]
    private List<LootTable> lootTables =
        new();

    [SerializeField, Min(0)]
    private int minLootRolls = 1;

    [SerializeField, Min(0)]
    private int maxLootRolls = 1;


    // =====================================================
    // VISUALS
    // =====================================================

    [Header("Visuals")]

    [SerializeField]
    private GameObject lootShimmer;


    // =====================================================
    // LIFECYCLE
    // =====================================================

    [Header("Lifecycle")]

    [Tooltip(
        "Sekunder från första öppningen tills containern despawnar, " +
        "även om loot finns kvar. 0 stänger av denna timer.")]
    [SerializeField, Min(0f)]
    private float lifetimeAfterFirstOpen =
        1800f;

    [Tooltip(
        "Hur länge en tom container ligger kvar innan den despawnar.")]
    [SerializeField, Min(0f)]
    private float emptyDespawnDelay =
        7f;


    // =====================================================
    // RESPAWN
    // =====================================================

    [Header("Respawn")]

    [SerializeField]
    private bool canRespawn;

    [Tooltip(
        "Sekunder mellan despawn och nästa spawn.")]
    [SerializeField, Min(0f)]
    private float respawnTime =
        3600f;

    [Tooltip(
        "Valfria spawnpunkter. Om listan saknar giltiga punkter " +
        "används objektets ursprungliga position. Vikter behöver " +
        "inte summera till 100.")]
    [SerializeField]
    private List<WeightedRespawnNode> respawnNodes =
        new();


    // =====================================================
    // RUNTIME CONTENTS
    // =====================================================

    private readonly LootContents contents =
        new();

    private GameObject shimmerInstance;


    // =====================================================
    // PRESENTATION STATE
    // =====================================================

    private Renderer[] cachedRenderers;
    private bool[] rendererInitialStates;

    private Collider2D[] cachedColliders;
    private bool[] colliderInitialStates;


    // =====================================================
    // LIFECYCLE STATE
    // =====================================================

    private Vector3 originalPosition;

    private bool initialized;
    private bool isSpawned;
    private bool lifetimeStarted;

    private Coroutine lifetimeCoroutine;
    private Coroutine emptyDespawnCoroutine;
    private Coroutine respawnCoroutine;


    // =====================================================
    // LOOT SOURCE
    // =====================================================

    public string LootTitle =>
        !string.IsNullOrWhiteSpace(
            containerName)
            ? containerName
            : gameObject.name;


    public IReadOnlyList<ItemData> LootItems =>
        contents.Items;


    public int CoinAmount =>
        contents.CoinAmount;


    public bool HasLoot =>
        contents.HasLoot;


    // =====================================================
    // INTERACTION OPTION
    // =====================================================

    public InteractionCategory Category =>
        InteractionCategory.Other;


    public InteractionPresentation
        GetPresentation()
    {
        return new InteractionPresentation(
            Category,
            "Loot"
        );
    }


    public string GetStatusText()
    {
        return string.Empty;
    }


    public bool CanInteract(
        in InteractionContext context)
    {
        return
            context.IsValid &&
            initialized &&
            isSpawned &&
            HasLoot &&
            LootUI.Instance != null;
    }


    public void Interact(
        in InteractionContext context)
    {
        if (!CanInteract(
                context))
        {
            return;
        }

        StartLifetimeIfNeeded();

        LootUI.Instance.Show(
            this
        );
    }


    // =====================================================
    // UNITY
    // =====================================================

    private void Awake()
    {
        originalPosition =
            transform.position;

        CachePresentationState();
    }


    private void Start()
    {
        initialized = true;

        BeginSpawnLifecycle();
    }


    private void OnDestroy()
    {
        DestroyShimmer();
    }


    // =====================================================
    // SPAWN LIFECYCLE
    // =====================================================

    private void BeginSpawnLifecycle()
    {
        StopSpawnTimers();

        contents.Clear();

        lifetimeStarted = false;
        isSpawned = true;

        SetWorldPresentationVisible(
            true
        );

        GenerateLoot();

        UpdateShimmer();

        /*
         * En generation kan legitimt resultera i noll loot.
         * Då ska inte ett dött, permanent tomt objekt stå kvar.
         */
        if (!HasLoot)
        {
            ScheduleEmptyDespawn();
        }
    }


    private void GenerateLoot()
    {
        if (lootTables == null ||
            lootTables.Count == 0)
        {
            return;
        }

        LootGenerationResult result =
            LootGenerator.GenerateLootResult(
                lootTables,
                minLootRolls,
                maxLootRolls
            );

        contents.Apply(
            result
        );
    }


    private void StartLifetimeIfNeeded()
    {
        if (!isSpawned ||
            lifetimeStarted)
        {
            return;
        }

        lifetimeStarted = true;

        /*
         * 0 betyder att containern endast despawnar
         * när den blir tom.
         */
        if (lifetimeAfterFirstOpen <= 0f)
            return;

        lifetimeCoroutine =
            StartCoroutine(
                LifetimeRoutine()
            );
    }


    private IEnumerator LifetimeRoutine()
    {
        yield return new WaitForSeconds(
            lifetimeAfterFirstOpen
        );

        lifetimeCoroutine = null;

        Despawn();
    }


    private void ScheduleEmptyDespawn()
    {
        if (!isSpawned ||
            HasLoot ||
            emptyDespawnCoroutine != null)
        {
            return;
        }

        if (emptyDespawnDelay <= 0f)
        {
            Despawn();
            return;
        }

        emptyDespawnCoroutine =
            StartCoroutine(
                EmptyDespawnRoutine()
            );
    }


    private IEnumerator EmptyDespawnRoutine()
    {
        yield return new WaitForSeconds(
            emptyDespawnDelay
        );

        emptyDespawnCoroutine = null;

        if (!HasLoot)
        {
            Despawn();
        }
    }


    // =====================================================
    // DESPAWN
    // =====================================================

    private void Despawn()
    {
        if (!isSpawned)
            return;

        isSpawned = false;
        lifetimeStarted = false;

        StopSpawnTimers();

        DestroyShimmer();

        /*
         * Loot hör till just denna lifecycle.
         * När containern despawnar är den generationen slut.
         */
        contents.Clear();

        SetWorldPresentationVisible(
            false
        );

        /*
         * Om just denna source visas i LootUI kommer Refresh()
         * se att den nu saknar loot och stänga fönstret.
         *
         * Om ett annat loot source visas refreshas bara det
         * aktuella fönstret.
         */
        if (LootUI.Instance != null)
        {
            LootUI.Instance.Refresh();
        }

        if (canRespawn)
        {
            StartRespawnTimer();
        }
    }


    private void StopSpawnTimers()
    {
        if (lifetimeCoroutine != null)
        {
            StopCoroutine(
                lifetimeCoroutine
            );

            lifetimeCoroutine = null;
        }

        if (emptyDespawnCoroutine != null)
        {
            StopCoroutine(
                emptyDespawnCoroutine
            );

            emptyDespawnCoroutine = null;
        }
    }


    // =====================================================
    // RESPAWN
    // =====================================================

    private void StartRespawnTimer()
    {
        if (respawnCoroutine != null)
            return;

        respawnCoroutine =
            StartCoroutine(
                RespawnRoutine()
            );
    }


    private IEnumerator RespawnRoutine()
    {
        if (respawnTime > 0f)
        {
            yield return new WaitForSeconds(
                respawnTime
            );
        }
        else
        {
            /*
             * Minst en frame mellan despawn och respawn.
             * Undviker samma-frame-loopar om både delays
             * och respawn time är 0.
             */
            yield return null;
        }

        respawnCoroutine = null;

        Respawn();
    }


    private void Respawn()
    {
        if (isSpawned)
            return;

        transform.position =
            GetRespawnPosition();

        BeginSpawnLifecycle();
    }


    private Vector3 GetRespawnPosition()
    {
        if (respawnNodes == null ||
            respawnNodes.Count == 0)
        {
            return originalPosition;
        }

        float totalWeight = 0f;

        WeightedRespawnNode lastValidNode =
            null;

        for (int i = 0;
             i < respawnNodes.Count;
             i++)
        {
            WeightedRespawnNode node =
                respawnNodes[i];

            if (node == null ||
                node.Node == null ||
                node.Weight <= 0f)
            {
                continue;
            }

            totalWeight +=
                node.Weight;

            lastValidNode =
                node;
        }

        if (totalWeight <= 0f ||
            lastValidNode == null)
        {
            return originalPosition;
        }

        float roll =
            Random.Range(
                0f,
                totalWeight
            );

        for (int i = 0;
             i < respawnNodes.Count;
             i++)
        {
            WeightedRespawnNode node =
                respawnNodes[i];

            if (node == null ||
                node.Node == null ||
                node.Weight <= 0f)
            {
                continue;
            }

            roll -=
                node.Weight;

            if (roll <= 0f)
            {
                return node.Node.position;
            }
        }

        /*
         * Floating-point fallback.
         */
        return lastValidNode.Node.position;
    }


    // =====================================================
    // LOOT CONTENTS
    // =====================================================

    public int GetItemQuantity(
        ItemData item)
    {
        return contents.GetItemQuantity(
            item
        );
    }


    public bool TryTakeItems(
        ItemData item,
        int quantity)
    {
        if (!isSpawned)
            return false;

        bool taken =
            contents.TryTakeItems(
                item,
                quantity
            );

        if (taken)
        {
            HandleContentsChanged();
        }

        return taken;
    }


    public int TakeAllCoins()
    {
        if (!isSpawned)
            return 0;

        int taken =
            contents.TakeAllCoins();

        if (taken > 0)
        {
            HandleContentsChanged();
        }

        return taken;
    }


    private void HandleContentsChanged()
    {
        UpdateShimmer();

        if (!HasLoot)
        {
            ScheduleEmptyDespawn();
        }
    }


    // =====================================================
    // WORLD PRESENTATION
    // =====================================================

    private void CachePresentationState()
    {
        cachedRenderers =
            GetComponentsInChildren<
                Renderer>(
                    true
                );

        rendererInitialStates =
            new bool[
                cachedRenderers.Length
            ];

        for (int i = 0;
             i < cachedRenderers.Length;
             i++)
        {
            Renderer renderer =
                cachedRenderers[i];

            rendererInitialStates[i] =
                renderer != null &&
                renderer.enabled;
        }


        cachedColliders =
            GetComponentsInChildren<
                Collider2D>(
                    true
                );

        colliderInitialStates =
            new bool[
                cachedColliders.Length
            ];

        for (int i = 0;
             i < cachedColliders.Length;
             i++)
        {
            Collider2D collider =
                cachedColliders[i];

            colliderInitialStates[i] =
                collider != null &&
                collider.enabled;
        }
    }


    private void SetWorldPresentationVisible(
        bool visible)
    {
        if (cachedRenderers != null)
        {
            for (int i = 0;
                 i < cachedRenderers.Length;
                 i++)
            {
                Renderer renderer =
                    cachedRenderers[i];

                if (renderer == null)
                    continue;

                renderer.enabled =
                    visible &&
                    rendererInitialStates[i];
            }
        }

        if (cachedColliders != null)
        {
            for (int i = 0;
                 i < cachedColliders.Length;
                 i++)
            {
                Collider2D collider =
                    cachedColliders[i];

                if (collider == null)
                    continue;

                collider.enabled =
                    visible &&
                    colliderInitialStates[i];
            }
        }
    }


    // =====================================================
    // VISUALS
    // =====================================================

    public void RefreshLootVisuals()
    {
        HandleContentsChanged();
    }


    private void UpdateShimmer()
    {
        if (!isSpawned ||
            !HasLoot)
        {
            DestroyShimmer();
            return;
        }

        if (lootShimmer == null ||
            shimmerInstance != null)
        {
            return;
        }

        shimmerInstance =
            Instantiate(
                lootShimmer,
                transform
            );

        shimmerInstance.transform.localPosition =
            Vector3.zero;

        shimmerInstance.transform.localRotation =
            Quaternion.identity;
    }


    private void DestroyShimmer()
    {
        if (shimmerInstance == null)
            return;

        Destroy(
            shimmerInstance
        );

        shimmerInstance = null;
    }


#if UNITY_EDITOR
    private void OnValidate()
    {
        minLootRolls =
            Mathf.Max(
                0,
                minLootRolls
            );

        maxLootRolls =
            Mathf.Max(
                minLootRolls,
                maxLootRolls
            );

        lifetimeAfterFirstOpen =
            Mathf.Max(
                0f,
                lifetimeAfterFirstOpen
            );

        emptyDespawnDelay =
            Mathf.Max(
                0f,
                emptyDespawnDelay
            );

        respawnTime =
            Mathf.Max(
                0f,
                respawnTime
            );

        if (respawnNodes == null)
            return;

        for (int i = 0;
             i < respawnNodes.Count;
             i++)
        {
            respawnNodes[i]
                ?.Normalize();
        }
    }
#endif
}