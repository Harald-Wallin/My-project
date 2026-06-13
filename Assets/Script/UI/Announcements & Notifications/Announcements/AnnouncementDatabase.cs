using UnityEngine;

[CreateAssetMenu(menuName = "RPG/Announcement Database")]
public class AnnouncementDatabase : ScriptableObject
{
    public AnnouncementData levelUp;

    //public AnnouncementData zoneDiscovery;

    public AnnouncementData factionDiscovered;

    public AnnouncementData reputationRankChanged;
}
