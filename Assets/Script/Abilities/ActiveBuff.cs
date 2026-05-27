using UnityEngine;

public abstract class ActiveBuff
{
    protected AbilityEffect sourceEffect; // 🔥 CORE

    public float duration;
    protected float elapsed;

    public int stacks = 1;
    public virtual bool IsStackable => false;
    public virtual int MaxStacks => 1;

    public bool IsFinished => elapsed >= duration;

    public float RemainingTime => duration - elapsed;
    public bool RemoveOnDeath => sourceEffect.removeOnDeath;

    // 🔥 AUTO DATA
    public Sprite Icon => sourceEffect.icon;
    public string Name => sourceEffect.name;

    public virtual string GetDescription(CharacterStats viewer)
    {
        return sourceEffect.GetTooltipText(viewer);
    }

    public abstract void Update(float deltaTime, CharacterStats target);

    public AbilityEffect SourceEffect => sourceEffect;

    public void ResetDuration()
    {
        elapsed = 0f;
    }

    public void SetDuration(float newDuration)
    {
        duration = newDuration;
    }

    public virtual void OnStackChanged(CharacterStats target) { }
}
