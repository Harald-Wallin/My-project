using System;
using System.Collections.Generic;

public sealed class FavourRuntime
{
    private readonly List<
        FavourObjectiveRuntime>
        objectives =
            new();

    public FavourRuntime(
        FavourData data,
        PlayerFavourManager manager)
    {
        Data =
            data;

        Manager =
            manager;

        BuildObjectives();

        State =
            FavourState.Unavailable;
    }

    public FavourData Data
    {
        get;
    }

    public PlayerFavourManager Manager
    {
        get;
    }

    public FavourState State
    {
        get;
        private set;
    }

    public IReadOnlyList<
        FavourObjectiveRuntime>
        Objectives =>
            objectives;

    public event Action<FavourRuntime>
        StateChanged;

    public event Action<FavourRuntime>
        ProgressChanged;

    private void BuildObjectives()
    {
        objectives.Clear();

        if (Data?.Objectives == null)
            return;

        foreach (FavourObjectiveData data
                 in Data.Objectives)
        {
            if (data == null)
                continue;

            FavourObjectiveRuntime runtime =
                data.CreateRuntime(
                    this
                );

            if (runtime == null)
                continue;

            runtime.ProgressChanged +=
                HandleObjectiveProgressChanged;

            objectives.Add(
                runtime
            );
        }
    }

    public void RefreshAvailability()
    {
        if (State !=
                FavourState.Unavailable &&
            State !=
                FavourState.Available)
        {
            return;
        }

        SetState(
            AreRequirementsMet()
                ? FavourState.Available
                : FavourState.Unavailable
        );
    }

    public bool AreRequirementsMet()
    {
        if (Data?.Requirements == null)
            return true;

        foreach (FavourRequirementData
                 requirement
                 in Data.Requirements)
        {
            if (requirement == null)
                continue;

            if (!requirement.IsMet(
                    Manager))
            {
                return false;
            }
        }

        return true;
    }

    public bool TryActivate()
    {
        RefreshAvailability();

        if (State !=
            FavourState.Available)
        {
            return false;
        }

        if (objectives.Count == 0)
            return false;

        SetState(
            FavourState.Active
        );

        ActivateObjectives();

        EvaluateCompletion();

        return true;
    }

    public bool TryTurnIn()
    {
        if (State !=
            FavourState.ReadyToTurnIn)
        {
            return false;
        }

        /*
         * Kontrollerar den aktuella sanningen en sista gång.
         * Detta är viktigt för CollectObjective.
         */
        if (!AreAllObjectivesComplete())
        {
            SetState(
                FavourState.Active
            );

            ActivateObjectives();

            return false;
        }

        Complete();

        return true;
    }

    internal void HandleCharacterDefeated(
        CharacterDefeatedResult result)
    {
        if (State !=
            FavourState.Active)
        {
            return;
        }

        foreach (FavourObjectiveRuntime
                 objective
                 in objectives)
        {
            objective.HandleCharacterDefeated(
                result
            );
        }
    }

    private void HandleObjectiveProgressChanged(
        FavourObjectiveRuntime objective)
    {
        ProgressChanged?.Invoke(
            this
        );

        EvaluateCompletion();
    }

    private void EvaluateCompletion()
    {
        if (State != FavourState.Active &&
            State != FavourState.ReadyToTurnIn)
        {
            return;
        }

        bool allComplete =
            AreAllObjectivesComplete();

        if (!allComplete)
        {
            if (State ==
                FavourState.ReadyToTurnIn)
            {
                SetState(
                    FavourState.Active
                );
            }

            return;
        }

        if (Data.CompletionPolicy ==
            FavourCompletionPolicy.Automatic)
        {
            Complete();
            return;
        }

        /*
         * Objectives förblir aktiva i ReadyToTurnIn så att
         * inventorybaserad progress fortfarande kan förändras.
         */
        SetState(
            FavourState.ReadyToTurnIn
        );
    }

    private bool AreAllObjectivesComplete()
    {
        if (objectives.Count == 0)
            return false;

        foreach (FavourObjectiveRuntime
                 objective
                 in objectives)
        {
            if (objective == null ||
                !objective.IsComplete)
            {
                return false;
            }
        }

        return true;
    }

    private void Complete()
    {
        if (State ==
            FavourState.Completed)
        {
            return;
        }

        DeactivateObjectives();

        GrantRewards();

        SetState(
            FavourState.Completed
        );

        RegisterFollowUps();
    }

    private void ActivateObjectives()
    {
        foreach (FavourObjectiveRuntime
                 objective
                 in objectives)
        {
            objective?.Activate();
        }
    }

    private void DeactivateObjectives()
    {
        foreach (FavourObjectiveRuntime
                 objective
                 in objectives)
        {
            objective?.Deactivate();
        }
    }

    private void GrantRewards()
    {
        if (Data?.Rewards == null)
            return;

        foreach (FavourRewardData reward
                 in Data.Rewards)
        {
            reward?.Grant(
                Manager
            );
        }
    }

    private void RegisterFollowUps()
    {
        if (Data?.FollowUps == null ||
            Manager == null)
        {
            return;
        }

        foreach (FavourData followUp
                 in Data.FollowUps)
        {
            if (followUp == null)
                continue;

            Manager.RegisterFavour(
                followUp
            );
        }
    }

    public void ResetObjectives()
    {
        foreach (FavourObjectiveRuntime
                 objective
                 in objectives)
        {
            objective?.ResetProgress();
        }

        EvaluateCompletion();
    }

    private void SetState(
        FavourState newState)
    {
        if (State == newState)
            return;

        State =
            newState;

        StateChanged?.Invoke(
            this
        );
    }
}