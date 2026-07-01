using UnityEngine;

public static class CombatTargeting
{
    public static bool CanAttack(
    CharacterStats attacker,
    CharacterStats target
)
    {

        if (attacker == null || target == null)
        {
            return false;
        }

        if (attacker == target)
        {
            return false;
        }

        // =========================
        // PLAYER RULES
        // =========================

        PlayerStats player =
            attacker.GetComponent<PlayerStats>();

        if (player != null)
        {
            return CanPlayerAttack(player, target);
        }

        // =========================
        // NPC RULES
        // =========================

        NPCBehavior ai =
            attacker.GetComponent<NPCBehavior>();

        if (ai != null)
        {
            if (ai.CurrentTarget == target)
            {
                return true;
            }
        }

        return attacker.IsHostileTo(target);
    }

    static bool CanPlayerAttack(
        PlayerStats player,
        CharacterStats target
    )
    {
        if (target.faction == null)
            return true;

        // HOSTILE = alltid OK
        if (player.IsHostileTo(target))
            return true;

        // MURDER MODE
        PlayerReputationManager rep =
            player.GetComponent<PlayerReputationManager>();

        if (rep == null)
            return false;

        return rep.IsMurderEnabled(target.faction);
    }
}
