using System;
using System.Collections.Generic;
using UnityEngine;

public sealed class PlayerBaseAttackCollection :
    MonoBehaviour
{
    [SerializeField]
    private List<AbilityData> learnedAttacks =
        new();

    [SerializeField]
    private AbilityData equippedAttack;

    public event Action<AbilityData>
        OnEquippedAttackChanged;

    public IReadOnlyList<AbilityData>
        GetLearnedAttacks()
    {
        return learnedAttacks;
    }

    public AbilityData GetEquippedAttack()
    {
        return equippedAttack;
    }

    public bool EquipAttack(
        AbilityData attack)
    {
        if (attack != null &&
            !attack.IsBaseAttack)
        {
            Debug.LogWarning(
                $"Ability '{attack.abilityName}' kan inte " +
                $"utrustas som base attack eftersom dess " +
                $"Usage Type inte är BaseAttack.",
                this
            );

            return false;
        }

        if (equippedAttack == attack)
            return true;

        equippedAttack = attack;

        OnEquippedAttackChanged?.Invoke(
            equippedAttack
        );

        return true;
    }

    public bool LearnAttack(
        AbilityData attack)
    {
        if (attack == null)
            return false;

        if (!attack.IsBaseAttack)
        {
            Debug.LogWarning(
                $"Ability '{attack.abilityName}' kan inte läras " +
                $"som base attack eftersom dess Usage Type " +
                $"inte är BaseAttack.",
                this
            );

            return false;
        }

        if (learnedAttacks.Contains(attack))
            return false;

        learnedAttacks.Add(attack);

        AnnouncementSpawner.Instance
            ?.QueueAnnouncement(
                AnnouncementSpawner
                    .Instance
                    .Database
                    .abilityLearned,
                AnnouncementFormatter
                    .BuildAbilityLearnedAnnouncement(
                        attack.abilityName
                    )
            );

        SpellbookNotificationManager.Instance
            ?.NotifyNewEntry();

        return true;
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        learnedAttacks ??=
            new List<AbilityData>();

        for (int i = learnedAttacks.Count - 1;
             i >= 0;
             i--)
        {
            AbilityData attack =
                learnedAttacks[i];

            if (attack == null)
                continue;

            if (attack.IsBaseAttack)
                continue;

            Debug.LogWarning(
                $"'{attack.abilityName}' har tagits bort från " +
                $"{nameof(PlayerBaseAttackCollection)} eftersom " +
                $"dess Usage Type inte är BaseAttack.",
                this
            );

            learnedAttacks.RemoveAt(i);
        }

        if (equippedAttack != null &&
            !equippedAttack.IsBaseAttack)
        {
            Debug.LogWarning(
                $"Equipped attack '{equippedAttack.abilityName}' " +
                $"är inte markerad som BaseAttack och har därför " +
                $"tagits bort från slotten.",
                this
            );

            equippedAttack = null;
        }
    }
#endif
}