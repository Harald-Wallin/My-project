using UnityEngine;

public abstract class ActiveBuff
{
    protected AbilityEffect sourceEffect;

    public float duration;

    protected float elapsed;

    public int stacks = 1;

    public virtual bool IsStackable =>
        sourceEffect != null &&
        sourceEffect.stackable;

    public virtual int MaxStacks =>
        sourceEffect != null
            ? Mathf.Max(
                1,
                sourceEffect.maxStacks
            )
            : 1;

    public bool IsFinished =>
        elapsed >= duration;

    public float RemainingTime =>
        Mathf.Max(
            0f,
            duration - elapsed
        );

    public bool RemoveOnDeath =>
        sourceEffect == null ||
        sourceEffect.removeOnDeath;

    public Sprite Icon =>
        sourceEffect != null
            ? sourceEffect.icon
            : null;

    public string Name =>
        sourceEffect != null
            ? sourceEffect.name
            : GetType().Name;

    public AbilityEffect SourceEffect =>
        sourceEffect;

    /// <summary>
    /// Anropas exakt en gång när buffen läggs till.
    /// </summary>
    public virtual void OnApplied(
        CharacterStats target)
    {
    }

    public abstract void Update(
        float deltaTime,
        CharacterStats target);

    /// <summary>
    /// Anropas innan buffen tas bort, oavsett borttagningsorsak.
    /// </summary>
    public virtual void OnRemoved(
        CharacterStats target)
    {
    }

    public virtual void OnStackChanged(
        CharacterStats target)
    {
    }

    public virtual string GetDescription(
        CharacterStats viewer)
    {
        return sourceEffect != null
            ? sourceEffect.GetTooltipText(
                viewer
            )
            : string.Empty;
    }

    public void ResetDuration()
    {
        elapsed = 0f;
    }

    public void SetDuration(
        float newDuration)
    {
        duration =
            Mathf.Max(
                0f,
                newDuration
            );
    }
}