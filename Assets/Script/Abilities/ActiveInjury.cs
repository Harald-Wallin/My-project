using UnityEngine;

public class ActiveInjury : ActiveBuff
{
    private InjuryEffect injury;

    public ActiveInjury(InjuryEffect effect)
    {
        injury = effect;

        sourceEffect = effect;

        duration = effect.duration;
    }

    public override bool IsStackable => injury.stackable;

    public override int MaxStacks => injury.maxStacks;

    public override void Update(float deltaTime, CharacterStats target)
    {
        if (elapsed == 0f)
        {
            ApplyStacks(target);
        }

        elapsed += deltaTime;

        if (IsFinished)
        {
            target.RemoveModifiersFromSource(sourceEffect);
        }
    }

    void ApplyStacks(CharacterStats target)
    {
        for (int i = 0; i < stacks; i++)
        {
            StatModifier mod = new StatModifier(
                injury.stat,
                injury.value,
                injury.type,
                sourceEffect,
                ModifierSourceType.Buff
            );

            target.AddModifier(mod);
        }
    }

    public override void OnStackChanged(CharacterStats target)
    {
        target.RemoveModifiersFromSource(sourceEffect);

        ApplyStacks(target);
    }
}