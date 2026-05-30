using System.Collections.Generic;
using UnityEngine;

public class PlayerAbilityCollection : MonoBehaviour
{
    [Header("Learned")]
    [SerializeField]
    private List<AbilityData> learnedAbilities =
        new List<AbilityData>();

    [Header("Equipped")]
    [SerializeField]
    private AbilityData[] equippedAbilities =
        new AbilityData[9];

    [Header("Base Attack")]
    [SerializeField]
    private BaseAttackData equippedBaseAttack;

    // =========================
    // LEARNED
    // =========================

    public List<AbilityData> GetLearnedAbilities()
    {
        return learnedAbilities;
    }

    public bool HasLearned(AbilityData ability)
    {
        return learnedAbilities.Contains(ability);
    }

    public void LearnAbility(AbilityData ability)
    {
        if (ability == null)
            return;

        if (learnedAbilities.Contains(ability))
            return;

        learnedAbilities.Add(ability);
    }

    // =========================
    // EQUIPPED
    // =========================

    public AbilityData[] GetEquippedAbilities()
    {
        return equippedAbilities;
    }

    public void SetEquippedAbility(
        int slot,
        AbilityData ability
    )
    {
        if (slot < 0 || slot >= equippedAbilities.Length)
            return;

        equippedAbilities[slot] = ability;
    }

    // =========================
    // BASE ATTACK
    // =========================

    public BaseAttackData GetBaseAttack()
    {
        return equippedBaseAttack;
    }

    public void SetBaseAttack(BaseAttackData attack)
    {
        equippedBaseAttack = attack;
    }

    public List<AbilityData> GetAllSpellbookEntries()
    {
        List<AbilityData> result =
            new List<AbilityData>();

        result.AddRange(learnedAbilities);

        PlayerBaseAttackCollection attacks =
            GetComponent<PlayerBaseAttackCollection>();

        if (attacks != null)
        {
            foreach (var attack in attacks.GetLearnedAttacks())
            {
                result.Add(attack);
            }
        }

        Debug.Log("Spellbook entries: " + result.Count);
        return result;
    }
}