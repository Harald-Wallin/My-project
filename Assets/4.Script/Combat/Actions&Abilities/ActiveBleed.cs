using UnityEngine;

public sealed class ActiveBleed :
    ActiveBuff
{
    private readonly BleedEffect effect;
    private readonly CharacterStats source;

    private float tickTimer;

    public ActiveBleed(
        BleedEffect effect,
        CharacterStats source)
    {
        this.effect = effect;
        this.source = source;

        sourceEffect = effect;
        duration = effect.duration;
    }

    public override void Update(
        float deltaTime,
        CharacterStats target)
    {
        elapsed += deltaTime;
        tickTimer += deltaTime;

        while (tickTimer >=
               effect.tickInterval)
        {
            tickTimer -=
                effect.tickInterval;

            if (target == null ||
                target.currentHP <= 0)
            {
                return;
            }

            Vector3 position =
                target.effectPoint != null
                    ? target.effectPoint.position
                    : target.transform.position;

            BleedVFXSpawner.Instance?.Spawn(
                position
            );

            CombatResolver.DealRawDamage(
                source,
                target,
                effect.damagePerTick
            );
        }
    }
}