using UnityEngine;

public enum AbilityType
{
    Melee,
    Spell,
    Buff,
    Curse
}

[CreateAssetMenu(menuName = "RPG/Ability")]
public class AbilityData : ScriptableObject, ITooltipProvider
{
    [Header("Basic")]
    public string abilityName;
    [TextArea] public string description;
    public Sprite icon;

    [Header("Type")]
    public AbilityType types;

    [Header("Tags")]
    public AbilityTag[] tags;

    [Header("Timing")]
    public float cooldown = 5f;
    public float globalCooldown = 0.8f;
    public float castTime = 0f;

    [Header("Costs")]
    public int wardCost = 0;

    [Header("Combat Rules")]
    public bool alwaysHits = false;
    public bool canCrit = true;
    public bool canMiss = true;
    public bool isSelfCast = false;
    public bool requiresHitCheck = false;

    [Header("Effects")]
    public AbilityEffect[] effects;

    public virtual void Use(CharacterStats caster, CharacterStats target)
    {
        if (target == null)
            return;

        if (requiresHitCheck)
        {
            if (!CombatResolver.RollHit(caster, target))
            {
                DamageResult missResult = new DamageResult
                {
                    isMiss = true
                };

                target.TakeDamage(missResult, caster);
                return;
            }

            if (CombatResolver.RollDodge(caster, target))
            {
                DamageResult evadeResult = new DamageResult
                {
                    isEvaded = true
                };

                target.TakeDamage(evadeResult, caster);
                return;
            }
        }

        foreach (var effect in effects)
        {
            effect.Apply(caster, target);
        }
    }

    public virtual TooltipData  GetTooltipData(CharacterStats caster)
    {
        TooltipData data = new TooltipData();

        data.title = abilityName;
        data.subtitle = types.ToString();

        if (wardCost > 0)
        {
            data.stats.Add($"<color=#7FD9FF>{wardCost} Wards</color>");
        }

        data.description = description;

        foreach (var effect in effects)
        {
            string text = effect.GetTooltipText(caster);

            if (!string.IsNullOrEmpty(text))
                data.stats.Add($"<color=#FF5555>{text}</color>");
        }

        if (castTime > 0)
            data.stats.Add($"<color=white>Cast Time: {castTime:0.0}s</color>");
        if (cooldown > 0)
            data.stats.Add(
            $"<color=white>Cooldown: {cooldown:0}s</color>"
);


        return data;
    }
}
