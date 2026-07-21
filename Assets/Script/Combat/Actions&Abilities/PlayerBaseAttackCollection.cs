using System;
using System.Collections.Generic;
using UnityEngine;

public class PlayerBaseAttackCollection : MonoBehaviour
{
    [SerializeField]
    private List<BaseAttackData> learnedAttacks =
        new();

    [SerializeField]
    private BaseAttackData equippedAttack;

    public event Action<BaseAttackData>
        OnEquippedAttackChanged;

    public List<BaseAttackData> GetLearnedAttacks()
    {
        return learnedAttacks;
    }

    public BaseAttackData GetEquippedAttack()
    {
        return equippedAttack;
    }

    public void EquipAttack(
        BaseAttackData attack)
    {
        if (equippedAttack == attack)
            return;

        equippedAttack = attack;

        OnEquippedAttackChanged?.Invoke(
            equippedAttack
        );
    }

    public void LearnAttack(
        BaseAttackData attack)
    {
        if (attack == null)
            return;

        if (learnedAttacks.Contains(attack))
            return;

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
    }
}
