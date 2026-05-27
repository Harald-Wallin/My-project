using UnityEngine;
using System.Collections;

[CreateAssetMenu(menuName = "RPG/Effects/Timed Stat Modifier")]
public class TimedStatModifierEffect : AbilityEffect
{
    public StatType stat;
    public float value;
    public ModifierType type;
    public float duration = 2f;

    public override void Apply(CharacterStats caster, CharacterStats target)
    {
        if (target == null) return;

        var mod = new StatModifier(stat, value, type, this, ModifierSourceType.Buff);
        target.AddModifier(mod);

        target.StartCoroutine(RemoveAfterDuration(target, mod));
    }

    IEnumerator RemoveAfterDuration(CharacterStats target, StatModifier mod)
    {
        yield return new WaitForSeconds(duration);

        target.RemoveModifiersFromSource(mod.source);
    }
}
