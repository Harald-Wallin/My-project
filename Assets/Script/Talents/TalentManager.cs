using System.Collections.Generic;
using UnityEngine;

public class TalentManager : MonoBehaviour
{
    public static TalentManager Instance;

    public int availablePoints = 0;
    [SerializeField] private List<TalentData> allTalents;

    public List<TalentRuntime> talents = new List<TalentRuntime>();

    private PlayerStats player;

    void Awake()
    {
        Instance = this;
        player = PlayerReference.Player;

        Initialize(allTalents);
        
    }

    public void Initialize(List<TalentData> allTalents)
    {
        talents.Clear();

        foreach (var t in allTalents)
        {
            talents.Add(new TalentRuntime(t));
        }
    }

    public bool TrySpendPoint(TalentRuntime talent)
    {
        if (availablePoints <= 0)
            return false;

        if (talent.currentPoints >= talent.data.maxPoints)
            return false;

        talent.currentPoints++;
        availablePoints--;

        ApplyTalent(talent);
        HandleUnlocks(talent);

        return true;
    }

    void ApplyTalent(TalentRuntime talent)
    {
        var player = PlayerReference.Player;
        // 🔥 ta bort gamla modifiers från denna talent
        player.RemoveModifiersFromSource(talent);

        foreach (var effect in talent.data.effects)
        {
            if (effect is StatModifierEffect statEffect)
            {
                float scaledValue = statEffect.value * talent.currentPoints;

                player.AddModifier(new StatModifier(
                    statEffect.stat,
                    scaledValue,
                    statEffect.type,
                    talent,
                    ModifierSourceType.Talent
                ));
            }
        }
    }

    void HandleUnlocks(TalentRuntime talent)
    {
        var player = PlayerReference.Player;

        if (player == null)
            return;

        // ONLY unlock on first point
        if (talent.currentPoints != 1)
            return;

        var abilities =
            player.GetComponent<PlayerAbilityCollection>();

        var attacks =
            player.GetComponent<PlayerBaseAttackCollection>();

        if (talent.data.unlockedAbility != null)
        {
            abilities.LearnAbility(
                talent.data.unlockedAbility
            );
        }

        if (talent.data.unlockedBaseAttack != null)
        {
            attacks.LearnAttack(
                talent.data.unlockedBaseAttack
            );
        }

        SpellbookUI spellbook =
            FindFirstObjectByType<SpellbookUI>();

        if (spellbook != null)
        {
            spellbook.Refresh();
        }
    }
}
