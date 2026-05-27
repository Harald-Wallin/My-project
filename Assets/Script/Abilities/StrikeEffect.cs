using UnityEngine;

[CreateAssetMenu(menuName = "RPG/Effects/Scaled Damage")]
public class ScaledDamageEffect : AbilityEffect
{
    public float strengthMultiplier = 1.5f;
    

    public override void Apply(CharacterStats caster, CharacterStats target)
    {
        if (target == null || caster == null) return;

        int damage = Mathf.RoundToInt(
        caster.GetStat(StatType.Strength) * strengthMultiplier
        + caster.GetStat(StatType.WeaponDamage)
        );

        CombatResolver.DealDamage(caster, target, damage);
    }

    public override string GetTooltipText(CharacterStats caster)
    {
        if (caster == null) return "";

        float damage = caster.GetStat(StatType.Strength) * strengthMultiplier;

        return $"Deals {damage:0} damage";
    }
}
