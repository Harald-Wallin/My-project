using UnityEngine;
using System.Collections.Generic;

public class AbilityController : MonoBehaviour
{
    private Dictionary<AbilityData, float> cooldownTimers =
        new Dictionary<AbilityData, float>();

    private float globalCooldownTimer = 0f;

    private CharacterStats stats;
    private PlayerAbilityCollection collection;
    [SerializeField]
    private AbilityData[] equippedAbilities;

    private CharacterActionController actionController;

    void Awake()
    {
        stats = GetComponent<CharacterStats>();
        collection = GetComponent<PlayerAbilityCollection>();
        actionController = GetComponent<CharacterActionController>();
    }

    void Update()
    {
        if (globalCooldownTimer > 0f)
            globalCooldownTimer -= Time.deltaTime;

        List<AbilityData> keys =
            new List<AbilityData>(cooldownTimers.Keys);

        foreach (var ability in keys)
        {
            cooldownTimers[ability] -= Time.deltaTime;

            if (cooldownTimers[ability] <= 0f)
            {
                cooldownTimers.Remove(ability);
            }
        }
    }

    public bool TryUseAbility(AbilityData ability, CharacterStats explicitTarget = null)
    {
        if (ability == null)
            return false;

        if (ability.UsesActionSettings)
        {
            if (actionController == null)
            {
                Debug.LogError(
                    $"{name} försökte använda den migrerade abilityn " +
                    $"'{ability.abilityName}', men saknar " +
                    $"{nameof(CharacterActionController)}.",
                    this
                );

                return false;
            }

            return actionController.TryStartAction(
                ability,
                explicitTarget
            );
        }

        if (!stats.CanAct())
            return false;

        //GCD
        if (globalCooldownTimer > 0f)
        {
            NotificationSpawner.Instance?.Show(NotificationSpawner.Instance.Database.abilityOnCooldown);

            return false;
        }

        // individual cooldown
        if (cooldownTimers.ContainsKey(ability))
        {
            NotificationSpawner.Instance?.Show(NotificationSpawner.Instance.Database.abilityOnCooldown);

            return false;
        }

        CharacterStats target = null;

        if (ability.isSelfCast)
        {
            target = stats;
        }
        else if (explicitTarget != null)
        {
            target = explicitTarget;
        }
        else
        {
            target = FindTarget();
        }

        if (target == null)
            return false;

        if (!ability.isSelfCast)
        {
            if (!CombatTargeting.CanAttack(stats, target))
                return false;
        }

        if (ability.wardCost > 0)
        {
            WardSystem ward =
                GetComponent<WardSystem>();

            if (ward == null)
                return false;

            if (!ward.TrySpendWard(ability.wardCost))
            {
                NotificationSpawner.Instance?.Show(NotificationSpawner.Instance.Database.notEnoughWard);

                return false;
            }
        }

        ability.Use(stats, target);

        cooldownTimers[ability] = ability.cooldown;
        globalCooldownTimer = ability.globalCooldown;

        return true;
    }

    public void ResetRuntimeState()
    {
        cooldownTimers.Clear();
        globalCooldownTimer = 0f;
    }

    CharacterStats FindTarget()
    {
        Collider2D[] hits =
            Physics2D.OverlapCircleAll(
                transform.position,
                2f,
                LayerMask.GetMask("Hitbox")
            );

        foreach (var hit in hits)
        {
            CombatHitbox hitbox =
                hit.GetComponent<CombatHitbox>();

            if (hitbox == null)
                continue;

            CharacterStats target =
                hitbox.Owner;

            if (target == null)
                continue;

            if (target == stats)
                continue;

            if (!CombatTargeting.CanAttack(stats, target))
                continue;

            return target;
        }

        return null;
    }

    public float GetCooldownRemaining(
    AbilityData ability)
    {
        if (ability == null)
            return 0f;

        if (ability.UsesActionSettings)
        {
            return actionController != null
                ? actionController
                    .GetCooldownRemaining(ability)
                : 0f;
        }

        float abilityCD = 0f;

        if (cooldownTimers.TryGetValue(
                ability,
                out float time))
        {
            abilityCD =
                Mathf.Max(
                    0f,
                    time
                );
        }

        if (abilityCD > 0f)
            return abilityCD;

        return Mathf.Max(
            0f,
            globalCooldownTimer
        );
    }

    public float GetMaxCooldown(
    AbilityData ability)
    {
        if (ability == null)
            return 0f;

        if (ability.UsesActionSettings)
        {
            return actionController != null
                ? actionController
                    .GetMaxCooldown(ability)
                : 0f;
        }

        if (cooldownTimers.ContainsKey(
                ability))
        {
            return ability.cooldown;
        }

        return ability.globalCooldown;
    }

    public AbilityData[] GetEquippedAbilities()
    {
        // Player uses collection
        if (collection != null)
        {
            return collection.GetEquippedAbilities();
        }

        // NPC fallback
        return equippedAbilities;
    }

    public void SetAbilityInSlot(
        int slot,
        AbilityData ability
    )
    {
        if (collection == null)
            return;

        collection.SetEquippedAbility(
            slot,
            ability
        );
    }
}