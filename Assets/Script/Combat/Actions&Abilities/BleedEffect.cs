using UnityEngine;

[CreateAssetMenu(menuName = "RPG/Effects/Bleed")]
public class BleedEffect : AbilityEffect
{
    public int damagePerTick = 2;
    public float duration = 6f;
    public float tickInterval = 2f;

    public override void Apply(CharacterStats caster, CharacterStats target)
    {
        if (target == null) return;

        BuffSystem buffs = target.GetComponent<BuffSystem>();
        if (buffs != null)
        {
            buffs.ApplyEffect(this, caster);
        }
    }

    public override string GetTooltipText(CharacterStats caster)
    {
        return $"Bleed: {damagePerTick} dmg every {tickInterval}s for {duration}s";
    }
}
