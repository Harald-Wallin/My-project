public sealed class CharacterDefeatedResult
{
    public CharacterStats Victim { get; }

    public CreatureDefinition Creature { get; }

    public DamageSourceContext FinalBlow { get; }

    public DamageContributionSnapshot Contributions { get; }

    public CharacterStats FinalBlowSource =>
        FinalBlow.DirectSource;

    public CharacterStats FinalBlowCreditOwner =>
        FinalBlow.CreditOwner;

    public CharacterDefeatedResult(
        CharacterStats victim,
        CreatureDefinition creature,
        DamageSourceContext finalBlow,
        DamageContributionSnapshot contributions)
    {
        Victim = victim;
        Creature = creature;
        FinalBlow = finalBlow;

        Contributions =
            contributions ??
            new DamageContributionSnapshot(
                null,
                0
            );
    }

    public float GetDamageShare(
        CharacterStats creditOwner)
    {
        return Contributions.GetDamageShare(
            creditOwner
        );
    }

    public bool HasMinimumDamageShare(
        CharacterStats creditOwner,
        float minimumShare)
    {
        return Contributions.HasMinimumShare(
            creditOwner,
            minimumShare
        );
    }
}
