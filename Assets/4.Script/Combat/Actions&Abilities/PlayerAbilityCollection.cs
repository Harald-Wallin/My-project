using System.Collections.Generic;
using UnityEngine;

public class PlayerAbilityCollection :
    MonoBehaviour
{
    [Header("Learned")]

    [SerializeField]
    private List<AbilityData> learnedAbilities =
        new();

    [Header("Equipped")]

    [SerializeField]
    private AbilityData[] equippedAbilities =
        new AbilityData[9];

    public List<AbilityData> GetLearnedAbilities()
    {
        return learnedAbilities;
    }

    public bool HasLearned(
        AbilityData ability)
    {
        return
            ability != null &&
            learnedAbilities.Contains(
                ability
            );
    }

    public void LearnAbility(
        AbilityData ability)
    {
        if (ability == null ||
            ability.IsBaseAttack)
        {
            return;
        }

        if (learnedAbilities.Contains(
                ability))
        {
            return;
        }

        learnedAbilities.Add(
            ability
        );

        AnnouncementSpawner.Instance
            ?.QueueAnnouncement(
                AnnouncementSpawner
                    .Instance
                    .Database
                    .abilityLearned,
                AnnouncementFormatter
                    .BuildAbilityLearnedAnnouncement(
                        ability.abilityName
                    )
            );

        SpellbookNotificationManager.Instance
            ?.NotifyNewEntry();
    }

    public AbilityData[] GetEquippedAbilities()
    {
        return equippedAbilities;
    }

    public void SetEquippedAbility(
        int slot,
        AbilityData ability)
    {
        if (slot < 0 ||
            slot >= equippedAbilities.Length)
        {
            return;
        }

        if (ability != null &&
            ability.IsBaseAttack)
        {
            return;
        }

        equippedAbilities[slot] =
            ability;
    }

    public List<AbilityData>
        GetAllSpellbookEntries()
    {
        List<AbilityData> result =
            new();

        result.AddRange(
            learnedAbilities
        );

        PlayerBaseAttackCollection attacks =
            GetComponent<
                PlayerBaseAttackCollection
            >();

        if (attacks != null)
        {
            IReadOnlyList<AbilityData>
                learnedAttacks =
                    attacks.GetLearnedAttacks();

            for (int i = 0;
                 i < learnedAttacks.Count;
                 i++)
            {
                AbilityData attack =
                    learnedAttacks[i];

                if (attack == null ||
                    result.Contains(attack))
                {
                    continue;
                }

                result.Add(
                    attack
                );
            }
        }

        return result;
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        learnedAbilities ??=
            new List<AbilityData>();

        if (equippedAbilities == null ||
            equippedAbilities.Length != 9)
        {
            AbilityData[] resized =
                new AbilityData[9];

            if (equippedAbilities != null)
            {
                int copyCount =
                    Mathf.Min(
                        equippedAbilities.Length,
                        resized.Length
                    );

                for (int i = 0;
                     i < copyCount;
                     i++)
                {
                    resized[i] =
                        equippedAbilities[i];
                }
            }

            equippedAbilities =
                resized;
        }

        for (int i =
                 learnedAbilities.Count - 1;
             i >= 0;
             i--)
        {
            AbilityData ability =
                learnedAbilities[i];

            if (ability != null &&
                ability.IsBaseAttack)
            {
                learnedAbilities.RemoveAt(
                    i
                );
            }
        }

        for (int i = 0;
             i < equippedAbilities.Length;
             i++)
        {
            AbilityData ability =
                equippedAbilities[i];

            if (ability != null &&
                ability.IsBaseAttack)
            {
                equippedAbilities[i] =
                    null;
            }
        }
    }
#endif
}