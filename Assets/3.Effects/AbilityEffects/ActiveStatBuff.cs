using System.Collections.Generic;

public sealed class ActiveStatBuff :
    ActiveBuff
{
    private readonly StatBuffEffect effect;

    private readonly List<StatModifier>
        appliedModifiers =
            new();

    public ActiveStatBuff(
        StatBuffEffect effect)
    {
        this.effect = effect;

        sourceEffect = effect;
        duration = effect.Duration;
    }

    public override void OnApplied(
        CharacterStats target)
    {
        RebuildModifiers(
            target
        );
    }

    public override void Update(
        float deltaTime,
        CharacterStats target)
    {
        elapsed += deltaTime;
    }

    public override void OnStackChanged(
        CharacterStats target)
    {
        RebuildModifiers(
            target
        );
    }

    public override void OnRemoved(
        CharacterStats target)
    {
        RemoveModifiers(
            target
        );
    }

    private void RebuildModifiers(
        CharacterStats target)
    {
        if (target == null)
            return;

        RemoveModifiers(
            target
        );

        StatBuffModifierData[] definitions =
            effect.Modifiers;

        if (definitions == null)
            return;

        for (int stackIndex = 0;
             stackIndex < stacks;
             stackIndex++)
        {
            for (int modifierIndex = 0;
                 modifierIndex < definitions.Length;
                 modifierIndex++)
            {
                StatBuffModifierData definition =
                    definitions[modifierIndex];

                if (definition == null)
                    continue;

                StatModifier modifier =
                    new StatModifier(
                        definition.Stat,
                        definition.Value,
                        definition.ModifierType,
                        this,
                        ModifierSourceType.Buff
                    );

                appliedModifiers.Add(
                    modifier
                );

                target.AddModifier(
                    modifier
                );
            }
        }
    }

    private void RemoveModifiers(
        CharacterStats target)
    {
        if (target == null)
            return;

        target.RemoveModifiersFromSource(
            this
        );

        appliedModifiers.Clear();
    }
}
