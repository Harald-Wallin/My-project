public class ActiveStun : ActiveBuff
{
    private CharacterStats target;

    public ActiveStun(StunEffect effect, CharacterStats target)
    {
        this.target = target;

        sourceEffect = effect;
        duration = effect.duration;

        target.AddStun();
    }

    public override void Update(float deltaTime, CharacterStats owner)
    {
        elapsed += deltaTime;

        if (IsFinished)
        {
            target.RemoveStun();
        }
    }
}
