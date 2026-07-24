using System;

public abstract class FavourObjectiveRuntime
{
    private bool active;

    protected FavourObjectiveRuntime(
        FavourObjectiveData data,
        FavourRuntime favour)
    {
        Data =
            data;

        Favour =
            favour;
    }

    public FavourObjectiveData Data
    {
        get;
    }

    public FavourRuntime Favour
    {
        get;
    }

    public bool IsActive =>
        active;

    public abstract bool IsComplete
    {
        get;
    }

    public abstract int CurrentProgress
    {
        get;
    }

    public abstract int RequiredProgress
    {
        get;
    }

    public event Action<
        FavourObjectiveRuntime>
        ProgressChanged;

    internal void Activate()
    {
        if (active)
            return;

        active = true;

        OnActivated();
    }

    internal void Deactivate()
    {
        if (!active)
            return;

        active = false;

        OnDeactivated();
    }

    internal void HandleCharacterDefeated(
        CharacterDefeatedResult result)
    {
        if (!active ||
            IsComplete ||
            result == null)
        {
            return;
        }

        OnCharacterDefeated(
            result
        );
    }

    protected void RaiseProgressChanged()
    {
        ProgressChanged?.Invoke(
            this
        );
    }

    protected virtual void OnActivated()
    {
    }

    protected virtual void OnDeactivated()
    {
    }

    protected virtual void OnCharacterDefeated(
        CharacterDefeatedResult result)
    {
    }

    public abstract void ResetProgress();
}
