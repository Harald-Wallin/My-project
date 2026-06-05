using System.Collections.Generic;
using UnityEngine;

public class TalentManager : MonoBehaviour
{
    public static TalentManager Instance;

    public int availablePoints = 0;
    [SerializeField] private List<TalentData> allTalents;
    [SerializeField] private List<int> tierRequirements;

    public IReadOnlyList<TalentData> AllTalents => allTalents;

    public List<TalentRuntime> talents = new List<TalentRuntime>();

    private PlayerStats player;

    PlayerStats Player => PlayerReference.Player;

    void Awake()
    {
        Instance = this;

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

    public int GetTierRequirement(int tier)
    {
        if (tier <= 1)
            return 0;

        int index = tier - 1;

        if (index >= tierRequirements.Count)
            return int.MaxValue;

        return tierRequirements[index];
    }

    public bool TrySpendPoint(TalentRuntime talent)
    {
        //Debug.Log("TRY SPEND POINT");

        if (!CanLearnTalent(talent))
            return false;

        talent.currentPoints++;
        availablePoints--;

        ApplyTalent(talent);
        //Debug.Log("CALLING HANDLE UNLOCKS");
        HandleUnlocks(talent);

        return true;
    }

    void ApplyTalent(TalentRuntime talent)
    {
        var player = Player;
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
        //Debug.Log("HANDLE UNLOCKS RUNNING");

        var player = Player;

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
            //Debug.Log("Refreshing spellbook");
            //Debug.Log(spellbook);
            spellbook.Refresh();
        }
    }

    public bool CanLearnTalent(TalentRuntime talent)
    {
        if (talent == null)
            return false;

        // No points
        if (availablePoints <= 0)
            return false;

        // Max rank reached
        if (talent.currentPoints >= talent.data.maxPoints)
            return false;

        foreach (var requirement in talent.data.requirements)
        {
            TalentRuntime runtime =
                talents.Find(
                    t => t.data == requirement.talent
                );

            if (runtime == null)
                return false;

            if (runtime.currentPoints <
                requirement.requiredPoints)
            {
                return false;
            }
        }

        int talentTier = talent.data.tier;

        // Tier 1 always available
        if (talentTier <= 1)
            return true;

        int requiredPoints = GetTierRequirement(talentTier);

        int spentPointsInPreviousTiers = 0;

        foreach (var t in talents)
        {
            if (t.data.tier < talentTier)
            {
                spentPointsInPreviousTiers += t.currentPoints;
            }
        }

        return spentPointsInPreviousTiers >= requiredPoints;
    }
}
