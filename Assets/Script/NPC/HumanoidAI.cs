using UnityEngine;

public class HumanoidAI : NPCBehavior
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