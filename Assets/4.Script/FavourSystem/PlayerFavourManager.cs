using System;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(PlayerStats))]
public sealed class PlayerFavourManager :
    MonoBehaviour
{
    private readonly Dictionary<
        string,
        FavourRuntime>
        runtimesById =
            new();

    private readonly List<FavourRuntime>
        runtimeSnapshot =
            new();

    [Header("Optional Starting Favours")]

    [SerializeField]
    private List<FavourData>
        startingFavours =
            new();

    public static PlayerFavourManager
        Instance
    {
        get;
        private set;
    }

    public PlayerStats Player
    {
        get;
        private set;
    }

    public Inventory PlayerInventory
    {
        get;
        private set;
    }

    public event Action<FavourRuntime>
        FavourRegistered;

    public event Action<FavourRuntime>
        FavourStateChanged;

    public event Action<FavourRuntime>
        FavourProgressChanged;

    public IEnumerable<FavourRuntime>
        Runtimes =>
            runtimesById.Values;

    private void Awake()
    {
        if (Instance != null &&
            Instance != this)
        {
            Debug.LogWarning(
                "Flera PlayerFavourManager hittades. " +
                "Den nya komponenten stängs av.",
                this
            );

            enabled = false;
            return;
        }

        Instance =
            this;

        Player =
            GetComponent<PlayerStats>();

        PlayerInventory =
            GetComponent<Inventory>();

        if (PlayerInventory == null)
        {
            PlayerInventory =
                GetComponentInChildren<
                    Inventory
                >();
        }
    }

    private void OnEnable()
    {
        CharacterCombatEvents
            .CharacterDefeated +=
            HandleCharacterDefeated;
    }

    private void Start()
    {
        if (PlayerInventory == null)
        {
            PlayerInventory =
                Inventory.Instance;
        }

        SubscribeToInventory();

        RegisterStartingFavours();
    }

    private void OnDisable()
    {
        CharacterCombatEvents
            .CharacterDefeated -=
            HandleCharacterDefeated;

        UnsubscribeFromInventory();
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }

    private void SubscribeToInventory()
    {
        if (PlayerInventory == null)
            return;

        PlayerInventory.OnInventoryChanged -=
            HandleInventoryChanged;

        PlayerInventory.OnInventoryChanged +=
            HandleInventoryChanged;
    }

    private void UnsubscribeFromInventory()
    {
        if (PlayerInventory == null)
            return;

        PlayerInventory.OnInventoryChanged -=
            HandleInventoryChanged;
    }

    private void HandleInventoryChanged()
    {
        /*
         * CollectObjectiveRuntime uppdaterar sin egen progress
         * genom samma inventory-event.
         *
         * Managern uppdaterar availability för favours som har
         * ItemRequirementData.
         */
        RefreshAllAvailability();
    }

    private void RegisterStartingFavours()
    {
        foreach (FavourData favour
                 in startingFavours)
        {
            RegisterFavour(
                favour
            );
        }
    }

    public FavourRuntime RegisterFavour(
        FavourData favour)
    {
        if (favour == null)
            return null;

        if (string.IsNullOrWhiteSpace(
                favour.Id))
        {
            Debug.LogError(
                $"Favour '{favour.name}' saknar permanent ID.",
                favour
            );

            return null;
        }

        if (runtimesById.TryGetValue(
                favour.Id,
                out FavourRuntime existing))
        {
            existing.RefreshAvailability();

            return existing;
        }

        FavourRuntime runtime =
            new FavourRuntime(
                favour,
                this
            );

        runtimesById.Add(
            favour.Id,
            runtime
        );

        runtime.StateChanged +=
            HandleRuntimeStateChanged;

        runtime.ProgressChanged +=
            HandleRuntimeProgressChanged;

        runtime.RefreshAvailability();

        FavourRegistered?.Invoke(
            runtime
        );

        return runtime;
    }

    public bool TryGetRuntime(
        FavourData favour,
        out FavourRuntime runtime)
    {
        runtime =
            null;

        if (favour == null ||
            string.IsNullOrWhiteSpace(
                favour.Id))
        {
            return false;
        }

        return runtimesById.TryGetValue(
            favour.Id,
            out runtime
        );
    }

    public bool TryGetRuntime(
        string favourId,
        out FavourRuntime runtime)
    {
        runtime =
            null;

        if (string.IsNullOrWhiteSpace(
                favourId))
        {
            return false;
        }

        return runtimesById.TryGetValue(
            favourId,
            out runtime
        );
    }

    public bool TryAccept(
        FavourData favour)
    {
        FavourRuntime runtime =
            RegisterFavour(
                favour
            );

        return runtime != null &&
               runtime.TryActivate();
    }

    public bool TryTurnIn(
        FavourData favour)
    {
        if (!TryGetRuntime(
                favour,
                out FavourRuntime runtime))
        {
            return false;
        }

        return runtime.TryTurnIn();
    }

    private void HandleCharacterDefeated(
        CharacterDefeatedResult result)
    {
        runtimeSnapshot.Clear();

        foreach (FavourRuntime runtime
                 in runtimesById.Values)
        {
            runtimeSnapshot.Add(
                runtime
            );
        }

        foreach (FavourRuntime runtime
                 in runtimeSnapshot)
        {
            runtime.HandleCharacterDefeated(
                result
            );
        }
    }

    private void HandleRuntimeStateChanged(
        FavourRuntime runtime)
    {
        FavourStateChanged?.Invoke(
            runtime
        );

        RefreshAllAvailability();
    }

    private void HandleRuntimeProgressChanged(
        FavourRuntime runtime)
    {
        FavourProgressChanged?.Invoke(
            runtime
        );
    }

    public void RefreshAllAvailability()
    {
        runtimeSnapshot.Clear();

        foreach (FavourRuntime runtime
                 in runtimesById.Values)
        {
            runtimeSnapshot.Add(
                runtime
            );
        }

        foreach (FavourRuntime runtime
                 in runtimeSnapshot)
        {
            runtime.RefreshAvailability();
        }
    }

    /// <summary>
    /// Returnerar true om ett aktivt CollectObjective efterfrågar
    /// det angivna föremålet.
    ///
    /// Kan senare användas av loot-, spawn- och world-system.
    /// </summary>
    public bool IsCollectObjectiveActive(
        ItemData item)
    {
        if (item == null)
            return false;

        foreach (FavourRuntime favour
                 in runtimesById.Values)
        {
            if (favour == null ||
                favour.State !=
                FavourState.Active)
            {
                continue;
            }

            foreach (FavourObjectiveRuntime
                     objective
                     in favour.Objectives)
            {
                if (objective is not
                    CollectObjectiveRuntime collect)
                {
                    continue;
                }

                if (!collect.IsActive ||
                    collect.IsComplete)
                {
                    continue;
                }

                if (Inventory.ItemsMatch(
                        collect.RequiredItem,
                        item))
                {
                    return true;
                }
            }
        }

        return false;
    }

    /// <summary>
    /// Hur många ytterligare exemplar som totalt behövs av alla
    /// aktiva och inkompletta CollectObjectives för detta item.
    /// </summary>
    public int GetRemainingCollectAmount(
        ItemData item)
    {
        if (item == null)
            return 0;

        int totalRemaining = 0;

        foreach (FavourRuntime favour
                 in runtimesById.Values)
        {
            if (favour == null ||
                favour.State !=
                FavourState.Active)
            {
                continue;
            }

            foreach (FavourObjectiveRuntime
                     objective
                     in favour.Objectives)
            {
                if (objective is not
                    CollectObjectiveRuntime collect)
                {
                    continue;
                }

                if (!collect.IsActive)
                    continue;

                if (!Inventory.ItemsMatch(
                        collect.RequiredItem,
                        item))
                {
                    continue;
                }

                totalRemaining +=
                    Mathf.Max(
                        0,
                        collect.RequiredProgress -
                        collect.CurrentProgress
                    );
            }
        }

        return totalRemaining;
    }
}