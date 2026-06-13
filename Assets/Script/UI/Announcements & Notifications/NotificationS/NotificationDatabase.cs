using UnityEngine;

[CreateAssetMenu(menuName = "RPG/Notification Database")]
public class NotificationDatabase : ScriptableObject
{
    [Header("Errors")]
    public NotificationData inventoryFull;

    public NotificationData notEnoughCoins;

    public NotificationData abilityOnCooldown;

    public NotificationData notEnoughWard;

    [Header("Progression")]
    public NotificationData levelUp;

    public NotificationData reputationGain;

    public NotificationData reputationLoss;

    public NotificationData zoneDiscovery;
}
