public class ActiveBleed : ActiveBuff
{
    private BleedEffect effect;
    private float tickTimer;
    private CharacterStats owner;

    public ActiveBleed(BleedEffect effect, CharacterStats owner)
    {
        this.effect = effect;
        this.owner = owner;

        sourceEffect = effect;
        duration = effect.duration;
    }

    public override void Update(float deltaTime, CharacterStats target)
    {
        elapsed += deltaTime;
        tickTimer += deltaTime;

        if (tickTimer >= effect.tickInterval)
        {
            tickTimer = 0f;

            //BleedEffect
            BleedVFXSpawner.Instance?.Spawn(
            target.effectPoint != null
            ? target.effectPoint.position
            : target.transform.position
            );

            target.TakeRawDamage(
                effect.damagePerTick,
                owner
            );
        }
    }
}
