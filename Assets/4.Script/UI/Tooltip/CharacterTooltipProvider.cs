public sealed class CharacterTooltipProvider :
    ITooltipProvider
{
    private readonly CharacterStats target;

    public CharacterTooltipProvider(
        CharacterStats target)
    {
        this.target = target;
    }

    public TooltipData GetTooltipData(
        CharacterStats viewer = null)
    {
        TooltipData data =
            new TooltipData();

        if (target == null)
        {
            data.title =
                "Unknown Character";

            return data;
        }

        data.title =
            target.DisplayName;

        data.subtitle =
            $"Level {target.level}";

        string roleName =
    target.RoleName;

        if (!string.IsNullOrWhiteSpace(
                roleName))
        {
            data.description =
                roleName;
        }

        if (target.HasVisibleFaction)
        {
            data.stats.Add(
                $"Faction: {target.FactionName}"
            );
        }

        data.stats.Add(
            $"Health: {target.currentHP} / " +
            $"{target.GetMaxHP()}"
        );

        data.showFooter = false;

        return data;
    }
}
