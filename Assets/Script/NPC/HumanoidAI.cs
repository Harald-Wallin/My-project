using UnityEngine;

public class HumanoidAI : AgressiveMobAI
{
    [Header("Assist Settings")]
    public float assistRadius = 10f;

    [Header("Guard Settings")]
    [SerializeField] private bool useReputationAggro = false;
    [SerializeField] private float customLeashDistance = 20f;

    protected override void Start()
    {
        base.Start();

        if (useReputationAggro)
            maxDistanceFromSpawn = customLeashDistance;
    }

    protected override bool ShouldAggro(CharacterStats potentialTarget)
    {
        PlayerStats playerStats =
            potentialTarget as PlayerStats;

        if (playerStats == null)
            return false;

        NPCReactionController reaction =
            GetComponent<NPCReactionController>();

        if (reaction != null &&
            reaction.IsTemporarilyHostile)
        {
            return true;
        }

        if (!useReputationAggro)
            return true;

        PlayerReputationManager repManager =
            playerStats.GetComponent<PlayerReputationManager>();

        if (repManager == null)
            return false;

        ReputationState state =
            repManager.GetReputationState(selfStats.faction);

        return state == ReputationState.Hated;
    }
    /*protected override bool ShouldAggro(CharacterStats potentialTarget)
    {
        if (!useReputationAggro)
            return true;

        PlayerStats playerStats = potentialTarget as PlayerStats;
        if (playerStats == null)
            return false;

        PlayerReputationManager repManager =
            playerStats.GetComponent<PlayerReputationManager>();

        if (repManager == null)
            return false;

        ReputationState state =
            repManager.GetReputationState(selfStats.faction);

        return state == ReputationState.Hated;
    }*/

    protected override void HandleDamaged(CharacterStats attacker)
    {
        base.HandleDamaged(attacker);

        if (!useReputationAggro)
            return;

        PlayerStats playerStats =
            attacker as PlayerStats;

        if (playerStats == null)
            return;

        if (selfStats.faction == null)
            return;

        currentTargetStats = playerStats;
        player = playerStats.transform;
        isAggro = true;

        Collider2D[] hits =
            Physics2D.OverlapCircleAll(
                transform.position,
                aggroRange
            );

        foreach (var hit in hits)
        {
            GuardAI guard =
                hit.GetComponent<GuardAI>();

            if (guard != null &&
                guard.selfStats.faction ==
                selfStats.faction)
            {
                guard.ForceAggro(playerStats);
            }
        }
    }

    protected virtual void TriggerCombatPhrase()
    {
    }
}