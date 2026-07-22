using UnityEngine;

[CreateAssetMenu(
    menuName = "RPG/Effects/Spawn Projectile"
)]
public sealed class SpawnProjectileEffect :
    AbilityEffect
{
    [Header("Projectile")]

    [SerializeField]
    private AbilityProjectile projectilePrefab;

    [SerializeField]
    private AbilityProjectileHitMode hitMode =
        AbilityProjectileHitMode
            .IntendedTargetOnly;

    [SerializeField]
    [Tooltip(
        "Fungerar endast med IntendedTargetOnly. " +
        "När aktiverad uppdateras destinationen kontinuerligt."
    )]
    private bool homing;

    [SerializeField]
    [Min(0.01f)]
    private float speed = 8f;

    [SerializeField]
    [Min(0.1f)]
    private float lifetime = 10f;

    [Header("Spawn")]

    [SerializeField]
    private Vector3 spawnOffset;

    [SerializeField]
    [Tooltip(
        "Om castern har en effectPoint används den som " +
        "projektilens startpunkt."
    )]
    private bool useCasterEffectPoint = true;

    public override void Execute(
        AbilityEffectExecutionContext context)
    {
        if (context == null ||
            context.Action == null ||
            projectilePrefab == null)
        {
            return;
        }

        Vector3 spawnPosition =
            ResolveSpawnPosition(
                context
            ) +
            spawnOffset;

        AbilityProjectile projectile =
            Object.Instantiate(
                projectilePrefab,
                spawnPosition,
                Quaternion.identity
            );

        if (projectile == null)
            return;

        projectile.Initialize(
            context.Action,
            hitMode,
            context.Ability.Effects,
            speed,
            lifetime,
            homing
        );
    }

    private Vector3 ResolveSpawnPosition(
        AbilityEffectExecutionContext context)
    {
        CharacterStats caster =
            context.Caster;

        if (caster == null)
            return context.Origin;

        if (useCasterEffectPoint &&
            caster.effectPoint != null)
        {
            return
                caster.effectPoint.position;
        }

        return caster.transform.position;
    }

#if UNITY_EDITOR
    protected override void OnValidate()
    {
        base.OnValidate();

        speed =
            Mathf.Max(
                0.01f,
                speed
            );

        lifetime =
            Mathf.Max(
                0.1f,
                lifetime
            );

        if (hitMode !=
            AbilityProjectileHitMode
                .IntendedTargetOnly)
        {
            homing = false;
        }
    }
#endif
}