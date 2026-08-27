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

    public event Action<FavourRuntime>
    FavourRewardSelectionChanged;

    [Header("Optional Starting Favours")]

    [SerializeField]
    private List<FavourData>
        startingFavours =
            new();

    public static PlayerFavourManager Instance
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

    public PlayerCurrency PlayerCurrency
    {
        get;
        private set;
    }

    public PlayerReputationManager PlayerReputation
    {
        get;
        private set;
    }

    public PlayerAbilityCollection PlayerAbilities
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

    public IEnumerable<FavourRuntime> Runtimes =>
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

        Instance = this;

        ResolveLocalReferences();
    }

    private void OnEnable()
    {
        CharacterCombatEvents
            .CharacterDefeated +=
            HandleCharacterDefeated;
    }

    private void Start()
    {
        ResolveReferences();

        SubscribeToPlayerSystems();

        RegisterStartingFavours();

        RefreshAllAvailability();
    }

    private void OnDisable()
    {
        CharacterCombatEvents
            .CharacterDefeated -=
            HandleCharacterDefeated;

        UnsubscribeFromPlayerSystems();
    }

    private void OnDestroy()
    {
        UnsubscribeFromRuntimes();

        if (Instance == this)
        {
            Instance = null;
        }
    }

    private void ResolveLocalReferences()
    {
        Player =
            GetComponent<PlayerStats>();

        PlayerInventory =
            GetComponent<Inventory>();

        if (PlayerInventory == null)
        {
            PlayerInventory =
                GetComponentInChildren<
                    Inventory>(
                    true
                );
        }

        PlayerCurrency =
            GetComponent<PlayerCurrency>();

        if (PlayerCurrency == null)
        {
            PlayerCurrency =
                GetComponentInChildren<
                    PlayerCurrency>(
                    true
                );
        }

        PlayerReputation =
            GetComponent<
                PlayerReputationManager>();

        if (PlayerReputation == null)
        {
            PlayerReputation =
                GetComponentInChildren<
                    PlayerReputationManager>(
                    true
                );
        }

        PlayerAbilities =
            GetComponent<
                PlayerAbilityCollection>();

        if (PlayerAbilities == null)
        {
            PlayerAbilities =
                GetComponentInChildren<
                    PlayerAbilityCollection>(
                    true
                );
        }
    }

    private void ResolveReferences()
    {
        if (Player == null)
        {
            Player =
                GetComponent<PlayerStats>();
        }

        if (PlayerInventory == null)
        {
            PlayerInventory =
                Inventory.Instance;
        }

        if (PlayerCurrency == null)
        {
            PlayerCurrency =
                global::PlayerCurrency.Instance;
        }

        if (PlayerReputation == null &&
            Player != null)
        {
            PlayerReputation =
                Player.GetComponent<
                    PlayerReputationManager>();
        }

        if (PlayerAbilities == null &&
            Player != null)
        {
            PlayerAbilities =
                Player.GetComponentInChildren<
                    PlayerAbilityCollection>(
                    true
                );
        }
    }

    private void SubscribeToPlayerSystems()
    {
        UnsubscribeFromPlayerSystems();

        if (PlayerInventory != null)
        {
            PlayerInventory
                .OnInventoryChanged +=
                HandleRequirementSourceChanged;
        }

        if (PlayerCurrency != null)
        {
            PlayerCurrency
                .OnCoinsChanged +=
                HandleRequirementSourceChanged;
        }

        if (Player != null)
        {
            Player.OnLevelChanged +=
                HandleRequirementSourceChanged;
        }

        if (PlayerReputation != null)
        {
            PlayerReputation
                .OnReputationChanged +=
                HandleReputationChanged;
        }
    }

    private void UnsubscribeFromPlayerSystems()
    {
        if (PlayerInventory != null)
        {
            PlayerInventory
                .OnInventoryChanged -=
                HandleRequirementSourceChanged;
        }

        if (PlayerCurrency != null)
        {
            PlayerCurrency
                .OnCoinsChanged -=
                HandleRequirementSourceChanged;
        }

        if (Player != null)
        {
            Player.OnLevelChanged -=
                HandleRequirementSourceChanged;
        }

        if (PlayerReputation != null)
        {
            PlayerReputation
                .OnReputationChanged -=
                HandleReputationChanged;
        }
    }

    private void HandleRequirementSourceChanged()
    {
        RefreshAllAvailability();
    }

    private void HandleReputationChanged(
        FactionReputationData reputation)
    {
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

        runtime.RewardSelectionChanged +=
            HandleRuntimeRewardSelectionChanged;

        FavourRegistered?.Invoke(
            runtime
        );

        return runtime;
    }

    private void HandleRuntimeRewardSelectionChanged(
    FavourRuntime runtime)
    {
        FavourRewardSelectionChanged?.Invoke(
            runtime
        );
    }

    public bool TryGetRuntime(
        FavourData favour,
        out FavourRuntime runtime)
    {
        runtime = null;

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
        runtime = null;

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

    public bool IsCompleted(
        FavourData favour)
    {
        return TryGetRuntime(
                   favour,
                   out FavourRuntime runtime
               ) &&
               runtime.State ==
               FavourState.Completed;
    }

    public bool HasAccepted(FavourData favour)
    {
        if (!TryGetRuntime(
                favour,
                out FavourRuntime runtime))
        {
            return false;
        }

        if (runtime == null)
            return false;

        return runtime.State !=
                   FavourState.Unavailable &&
               runtime.State !=
                   FavourState.Available;
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

    public bool CanGrantAbility(
    AbilityData ability)
    {
        return
            ability != null &&
            PlayerAbilities != null;
    }

    public bool TryGrantAbility(
    AbilityData ability)
    {
        if (ability == null)
            return false;

        if (PlayerAbilities == null)
        {
            ResolveReferences();
        }

        if (PlayerAbilities == null)
        {
            Debug.LogError(
                $"Kan inte dela ut ability " +
                $"'{ability.name}': spelaren saknar " +
                $"{nameof(PlayerAbilityCollection)}.",
                this
            );

            return false;
        }

        return PlayerAbilities.LearnAbility(
            ability
        );
    }

    private void HandleCharacterDefeated(
        CharacterDefeatedResult result)
    {
        CreateRuntimeSnapshot();

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
        CreateRuntimeSnapshot();

        foreach (FavourRuntime runtime
                 in runtimeSnapshot)
        {
            runtime.RefreshAvailability();
        }
    }

    private void CreateRuntimeSnapshot()
    {
        runtimeSnapshot.Clear();

        foreach (FavourRuntime runtime
                 in runtimesById.Values)
        {
            runtimeSnapshot.Add(
                runtime
            );
        }
    }

    private void UnsubscribeFromRuntimes()
    {
        foreach (FavourRuntime runtime
                 in runtimesById.Values)
        {
            if (runtime == null)
                continue;

            runtime.StateChanged -=
                HandleRuntimeStateChanged;

            runtime.ProgressChanged -=
                HandleRuntimeProgressChanged;

            runtime.RewardSelectionChanged -=
                HandleRuntimeRewardSelectionChanged;
        }
    }

    // =========================================================
    // FAVOUR ITEM / COLLECT QUERIES
    // =========================================================

    /// <summary>
    /// Returnerar true om spelaren just nu har minst ett aktivt,
    /// ofärdigt Collect-objective som fortfarande behöver itemet.
    /// </summary>
    public bool IsCollectObjectiveActive(
        ItemData item)
    {
        if (item == null)
            return false;

        foreach (FavourRuntime favour
                 in runtimesById.Values)
        {
            if (CanCollectItemForFavour(
                    favour,
                    item))
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Returnerar true endast om den angivna favouren just nu
    /// aktivt behöver det angivna itemet.
    ///
    /// Detta är den auktoritativa loot-regeln för FavourItems.
    /// </summary>
    public bool CanDropFavourItem(
        FavourData requiredFavour,
        ItemData item)
    {
        if (requiredFavour == null ||
            item == null)
        {
            return false;
        }

        if (!TryGetRuntime(
                requiredFavour,
                out FavourRuntime runtime))
        {
            return false;
        }

        return CanCollectItemForFavour(
            runtime,
            item
        );
    }

    private static bool CanCollectItemForFavour(
        FavourRuntime favour,
        ItemData item)
    {
        if (favour == null ||
            item == null)
        {
            return false;
        }


        if (favour.State !=
            FavourState.Active)
        {
            return false;
        }

        foreach (FavourObjectiveRuntime objective
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

            if (!Inventory.ItemsMatch(
                    collect.RequiredItem,
                    item))
            {
                continue;
            }

            return true;
        }

        return false;
    }

    public int GetRemainingCollectAmount(
    ItemData item)
    {
        if (item == null)
            return 0;

        int totalRemaining =
            0;

        foreach (FavourRuntime favour
                 in runtimesById.Values)
        {
            if (favour == null ||
                favour.State !=
                    FavourState.Active)
            {
                continue;
            }

            foreach (FavourObjectiveRuntime objective
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