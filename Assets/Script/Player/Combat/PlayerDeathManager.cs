using System.Collections.Generic;
using UnityEngine;

public class PlayerDeathManager : MonoBehaviour
{
    [Header("Main Death Debuffs")]
    [SerializeField] private List<InjuryEffect> mainDebuffs;

    [Header("Random Death Debuffs")]
    [SerializeField] private List<InjuryEffect> secondaryDebuffs;

    [Header("Settings")]
    [SerializeField] private int permanentDeathThreshold = 4;

    [SerializeField] private float durationPerDeath = 240f;

    [Header("Debug")]
    [SerializeField] private int deathStacks = 0;

    [SerializeField] private int totalDeathsLifetime = 0;

    private bool permanentStateReached = false;

    // =========================================================

    public void HandleDeath(PlayerStats player, Vector3 respawnPosition)
    {
        totalDeathsLifetime++;

        if (!permanentStateReached)
        {
            deathStacks++;

            if (deathStacks > permanentDeathThreshold)
                deathStacks = permanentDeathThreshold;
        }

        float duration = GetCurrentDuration();

        ApplyMainDebuffs(player, duration);

        ApplyRandomDebuff(player, duration);

        if (deathStacks >= permanentDeathThreshold)
        {
            MakeDebuffsPermanent();
        }

        //Debug.Log($"Death stacks: {deathStacks}");

        //reset aggro
        AgressiveMobAI[] mobs =
        FindObjectsByType<AgressiveMobAI>(
        FindObjectsSortMode.None
        );

        foreach (var mob in mobs)
        {
            mob.ResetAggro();
            // Ensure they actively return to spawn point as well
            mob.ReturnToSpawn();
        }

        //respawn
        RespawnPlayer(player, respawnPosition);
    }

    // =========================================================

    float GetCurrentDuration()
    {
        if (permanentStateReached)
            return Mathf.Infinity;

        return deathStacks * durationPerDeath;
    }

    // =========================================================

    void ApplyMainDebuffs(PlayerStats player, float duration)
    {
        BuffSystem buffs = player.GetComponent<BuffSystem>();

        foreach (var debuff in mainDebuffs)
        {
            buffs.ApplyEffect(debuff, player, duration);
        }
    }

    // =========================================================

    void ApplyRandomDebuff(PlayerStats player, float duration)
    {
        if (secondaryDebuffs.Count == 0)
            return;

        BuffSystem buffs = player.GetComponent<BuffSystem>();

        InjuryEffect randomDebuff =
            secondaryDebuffs[Random.Range(0, secondaryDebuffs.Count)];

        buffs.ApplyEffect(randomDebuff, player, duration);
    }

    // =========================================================

    void MakeDebuffsPermanent()
    {
        permanentStateReached = true;

        //Debug.Log("Death debuffs became permanent!");
    }

    // =========================================================

    void RespawnPlayer(PlayerStats player, Vector3 respawnPosition)
    {
        player.transform.position = respawnPosition;

        player.currentHP = player.GetMaxHP();

        player.RaiseHealthChangedExternally();

        //Nykod v

        Rigidbody2D rb = player.GetComponent<Rigidbody2D>();

        if (rb != null)
        {
            rb.linearVelocity = Vector2.zero;
            rb.angularVelocity = 0f;
            rb.position = respawnPosition;
        }
        else
        {
            player.transform.position = respawnPosition;
        }
    }
}