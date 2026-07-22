using System.Text;
using UnityEngine;

[CreateAssetMenu(
    menuName = "RPG/Effects/Stat Buff"
)]
public sealed class StatBuffEffect :
    AbilityEffect
{
    [SerializeField]
    [Min(0f)]
    private float duration = 30f;

    [SerializeField]
    private StatBuffModifierData[] modifiers;

    public float Duration =>
        Mathf.Max(
            0f,
            duration
        );

    public StatBuffModifierData[] Modifiers =>
        modifiers;

    public override void Execute(
        AbilityEffectExecutionContext context)
    {
        if (context == null ||
            context.Target == null)
        {
            return;
        }

        BuffSystem buffs =
            context.Target.GetComponent<
                BuffSystem
            >();

        buffs?.ApplyEffect(
            this,
            context.Caster
        );
    }

    public override ActiveBuff CreateActiveBuff(
        CharacterStats source,
        CharacterStats target)
    {
        return new ActiveStatBuff(
            this
        );
    }

    public override string GetTooltipText(
        CharacterStats caster)
    {
        if (modifiers == null ||
            modifiers.Length == 0)
        {
            return
                $"Lasts {Duration:0.#}s";
        }

        StringBuilder builder =
            new StringBuilder();

        for (int i = 0;
             i < modifiers.Length;
             i++)
        {
            StatBuffModifierData modifier =
                modifiers[i];

            if (modifier == null)
                continue;

            if (builder.Length > 0)
            {
                builder.Append(", ");
            }

            builder.Append(
                modifier.GetTooltipText()
            );
        }

        if (builder.Length > 0)
        {
            builder.Append(
                $" for {Duration:0.#}s"
            );
        }

        return builder.ToString();
    }

#if UNITY_EDITOR
    protected override void OnValidate()
    {
        base.OnValidate();

        duration =
            Mathf.Max(
                0f,
                duration
            );
    }
#endif
}