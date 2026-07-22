using UnityEngine;

public static class AnnouncementFormatter
{
    public static string BuildReputationAnnouncement(
        string rankName,
        Color rankColor,
        string factionName
    )
    {
        string colorHex =
            ColorUtility.ToHtmlStringRGB(rankColor);

        return
            $"<size=24>You are now</size>\n" +
            $"<size=42><color=#{colorHex}>{rankName}</color></size>\n" +
            $"<size=24>with</size>\n" +
            $"<size=42>{factionName}</size>";
    }

    public static string BuildLevelAnnouncement(
        int level
    )
    {
        return
            $"<size=55>Hail!</size>\n" +
            $"<size=120>LEVEL {level}</size>";
    }

    public static string BuildFactionDiscoveryAnnouncement(
        string factionName
    )
    {
        return
            $"<size=55>Faction Discovered</size>\n" +
            $"<size=110>{factionName}</size>";
    }

    public static string BuildZoneAnnouncement(
        string zoneName
    )
    {
        return
            $"<size=55>Entered Zone</size>\n" +
            $"<size=110>{zoneName}</size>";
    }

    public static string BuildAbilityLearnedAnnouncement(
    string abilityName
)
    {
        return
            $"<size=24>Learned:</size>\n" +
            $"<size=48>{abilityName}</size>";
    }
}
