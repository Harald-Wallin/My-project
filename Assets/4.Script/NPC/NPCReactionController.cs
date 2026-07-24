using UnityEngine;

public class NPCReactionController : MonoBehaviour
{
    [Header("Awareness")]
    [SerializeField]
    private float awarenessRadius = 5f;

    [SerializeField]
    private float alertedRadiusMultiplier = 3f;

    [SerializeField]
    private bool drawGizmos = true;
    private CharacterStats lastThreatSource;
    private bool currentlyDetectingPlayer;
    private readonly System.Collections.Generic.HashSet<NPCReactionController>
    alreadyAlertedNPCs =
    new();

    [Header("Reaction")]
    [SerializeField]
    private NPCReactionType reactionType;
    public bool IsDetectingPlayer =>
    currentlyDetectingPlayer;

    public bool IsReacting =>
    ai != null &&
    (
        ai.CurrentState == AIState.Aggro ||
        ai.CurrentState == AIState.Fleeing ||
        ai.CurrentState == AIState.Holding
    );

    [Header("Temporary Hostility")]
    [SerializeField]
    private float hostilityDuration = 300f;
    public bool IsHostile =>
    IsReputationHostile() || IsTemporarilyHostile;

    private NPCBehavior ai;

    private CharacterStats selfStats;
    private Rigidbody2D rb;

    private Transform player;

    [Header("Alert")]
    [SerializeField]
    private float alertDuration = 10f;

    private float alertTimer;

    public bool IsAlerted =>
        alertTimer > 0f;

    public Faction Faction =>
        selfStats != null
        ? selfStats.faction
        : null;

    bool IsReputationHostile()
    {
        PlayerStats playerStats =
            PlayerReference.Player;

        if (playerStats == null)
            return false;

        return selfStats.IsHostileTo(playerStats);
    }

    void Awake()
    {
        selfStats = GetComponent<CharacterStats>();
        ai = GetComponent<NPCBehavior>();
        rb = GetComponent<Rigidbody2D>();

        player = PlayerReference.Player?.transform;

        if (selfStats != null)
        {
            selfStats.OnDamagedBy += OnDamaged;
        }
    }

    void Update()
    {
        UpdatePlayerDetection();
        HandlePassiveHostility();
        HandleAlertTimer();
        HandleAwarenessPropagation();
    }

    void UpdatePlayerDetection()
    {
        currentlyDetectingPlayer = false;

        if (!IsAlerted)
            return;

        if (player == null)
            return;

        float radius = GetCurrentAwarenessRadius();

        float distance =
            Vector2.Distance(
                transform.position,
                player.position
            );

        currentlyDetectingPlayer =
            distance <= radius;
    }

    float GetCurrentAwarenessRadius()
    {
        if (IsAlerted)
        {
            return awarenessRadius *
                   alertedRadiusMultiplier;
        }

        return awarenessRadius;
    }
    public void ClearAwarenessMemory()
    {
        alreadyAlertedNPCs.Clear();
    }


    void HandleAlertTimer()
    {
        bool shouldRefreshAlert = false;

        if (currentlyDetectingPlayer)
        {
            shouldRefreshAlert = true;
        }

        if (ai != null)
        {
            if (ai.CurrentState == AIState.Aggro)
            {
                shouldRefreshAlert = true;
            }

            if (ai.CurrentState == AIState.Fleeing)
            {
                shouldRefreshAlert = true;
            }
        }

        if (shouldRefreshAlert)
        {
            RefreshAlert();
        }

        if (alertTimer <= 0f)
            return;

        alertTimer -= Time.deltaTime;

        if (alertTimer > 0f)
            return;

        alertTimer = 0f;
        ClearAwarenessMemory();

        if (FactionAwarenessSystem.Instance != null)
        {
            FactionAwarenessSystem.Instance
                .UnregisterAlertedNPC(this);
        }
    }

    void HandleAwarenessPropagation()
    {
        if (!IsAlerted)
            return;

        if (lastThreatSource == null)
            return;

        Collider2D[] hits =
            Physics2D.OverlapCircleAll(
                transform.position,
                GetCurrentAwarenessRadius()
            );

        foreach (var hit in hits)
        {
            NPCReactionController other =
                hit.GetComponent<NPCReactionController>();

            if (other == null)
                continue;

            if (other == this)
                continue;

            if (other.Faction != selfStats.faction)
                continue;

            // KRITISKT:
            // Har vi redan alertat denna NPC?
            if (alreadyAlertedNPCs.Contains(other))
                continue;

            alreadyAlertedNPCs.Add(other);

            other.ForceAlert(lastThreatSource);
        }
    }

    void OnDamaged(CharacterStats attacker)
    {
        if (selfStats.currentHP <= 0)
        {
            RefreshAlert();
            HandleAwarenessPropagation();
        }

        if (attacker == null)
            return;

        bool shouldReact = false;

        if (selfStats.IsHostileTo(attacker))
        {
            shouldReact = true;
        }

        PlayerStats playerAttacker = attacker as PlayerStats;

        if (playerAttacker != null)
        {
            PlayerReputationManager rep = playerAttacker.GetComponent<PlayerReputationManager>();

            if (rep != null &&
                selfStats.faction != null &&
                rep.IsMurderEnabled(selfStats.faction))
            {
                shouldReact = true;
            }
        }

        if (!shouldReact)
        {
            return;
        }

        lastThreatSource = attacker;

        RefreshAlert();

        if (playerAttacker != null)
        {
            BecomeTemporarilyHostile(playerAttacker);
        }

        switch (reactionType)
        {
            case NPCReactionType.Flee:

                if (ai != null)
                {
                    ai.StartFleeing(attacker);
                }
                break;

            case NPCReactionType.Aggro:

                if (ai != null)
                {
                    ai.ForceAggro(attacker);
                }

                AlertNearbyAggroNPCs(attacker);

                break;
        }
    }

    void BecomeTemporarilyHostile(
    PlayerStats player
    )
    {
        if (player == null)
            return;

        FactionHostilitySystem.Instance
            ?.AddHostility(
                Faction,
                player,
                hostilityDuration
            );
    }

    public bool IsTemporarilyHostile
    {
        get
        {
            PlayerStats player =
                PlayerReference.Player;

            if (player == null)
                return false;

            return FactionHostilitySystem.Instance != null &&
                   FactionHostilitySystem.Instance
                   .IsHostileToPlayer(
                        Faction,
                        player
                   );
        }
    }

    void RefreshAlert()
    {
        alertTimer = alertDuration;

        if (FactionAwarenessSystem.Instance != null)
        {
            FactionAwarenessSystem.Instance
                .RegisterAlertedNPC(this);
        }
    }

    public void ResetEncounterState()
    {
        lastThreatSource = null;
        currentlyDetectingPlayer = false;

        alertTimer = 0f;

        ClearAwarenessMemory();

        FactionAwarenessSystem.Instance
            ?.UnregisterAlertedNPC(
                this
            );
    }


    public void OnWitnessedCrime(CharacterStats attacker)
    {
        if (attacker == null)
            return;

        BecomeTemporarilyHostile((PlayerStats)attacker);
        RefreshAlert();

        lastThreatSource = attacker;

        switch (reactionType)
        {
            case NPCReactionType.Flee:

                if (ai != null)
                {
                    ai.StartFleeing(attacker);
                }

                break;

            case NPCReactionType.Aggro:

                if (ai != null)
                {
                    ai.ForceAggro(attacker);
                }

                break;
        }
    }

    void AlertNearbyAggroNPCs(CharacterStats attacker)
    {
        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position,GetCurrentAwarenessRadius());

        foreach (var hit in hits)
        {
            NPCReactionController other =
                hit.GetComponent<NPCReactionController>();

            if (other == null)
                continue;

            if (other == this)
                continue;

            if (other.Faction != selfStats.faction)
                continue;

            other.ForceAlert(attacker);
        }
    }

    void HandlePassiveHostility()
    {
        //VIKTIGT!!! OM NPCn inte har någon reaktionstyp, ska den inte agera på samma sätt som "mänskliga factions"
        if (reactionType == NPCReactionType.None)
            return;

        if (selfStats == null)
            return;

        PlayerStats player = PlayerReference.Player;

        if (player == null)
            return;

        if (!IsHostile)
            return;

        // IMPORTANT:
        // Temporary hostility should NOT recursively re-alert NPCs

        float distance =
            Vector2.Distance(
                transform.position,
                player.transform.position
            );

        if (distance > awarenessRadius)
            return;

        // Prevent re-trigger spam
        if (IsReacting)
            return;

        switch (reactionType)
        {
            case NPCReactionType.Flee:

                if (ai != null)
                {
                    ai.StartFleeing(player);
                }

                break;

            case NPCReactionType.Aggro:

                if (ai != null)
                {
                 ai.ForceAggro(player);
                }

                break;
        }
    }

    public void ForceAlert(CharacterStats attacker)
    {
        if (attacker == null)
            return;

        bool wasAlreadyAlerted = IsAlerted;

        //if (attacker is PlayerStats)
        //{
        //    BecomeTemporarilyHostile((PlayerStats)attacker);
            RefreshAlert();
        //}

        if (attacker is PlayerStats player)
        {
            BecomeTemporarilyHostile(player);
        }

        lastThreatSource = attacker;

        switch (reactionType)
        {
            case NPCReactionType.Flee:

                if (ai != null)
                {
                    ai.StartFleeing(attacker);
                }

                break;

            case NPCReactionType.Aggro:

                if (ai != null)
                {
                    ai.ForceAggro(attacker);
                }

                break;
        }
    }

    public bool DoesCurrentlyDetectPlayer()
    {
        return currentlyDetectingPlayer;
    }

    public bool BlocksInteraction(PlayerStats player)
    {
        if (player == null)
            return false;

        if (selfStats == null)
            return false;

        // Permanent hostility
        if (selfStats.IsHostileTo(player))
            return true;

        // Temporary hostility
        if (IsTemporarilyHostile)
            return true;

        return false;
    }

    void OnDestroy()
    {
        if (selfStats != null)
        {
            selfStats.OnDamagedBy -= OnDamaged;
        }
    }

    //-----------------GIZMOS-----------------
    void OnDrawGizmosSelected()
    {
        if (!drawGizmos)
            return;

        float radius =
            IsHostile
            ? GetCurrentAwarenessRadius()
            : awarenessRadius;

        Gizmos.color =
            IsHostile
            ? Color.yellow
            : Color.green;

        Gizmos.DrawWireSphere(
            transform.position,
            radius
        );
    }
}