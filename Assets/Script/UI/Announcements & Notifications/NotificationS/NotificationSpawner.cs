using System.Collections.Generic;
using UnityEngine;

public class NotificationSpawner : MonoBehaviour
{
    public static NotificationSpawner Instance;

    [SerializeField]
    private NotificationInstance prefab;

    [SerializeField]
    private Transform parentContainer;

    [SerializeField]
    private NotificationDatabase database;

    public NotificationDatabase Database => database;

    private Dictionary<NotificationData, float> cooldowns = new Dictionary<NotificationData, float>();

    void Awake()
    {
        Instance = this;
    }

    public void Show(NotificationData data)
    {
        if (cooldowns.ContainsKey(data))
        {
            return;
        }

        if (data == null)
            return;

        NotificationInstance notification = Instantiate(prefab,parentContainer);

        // Alert Cooldown, spam-prevention for the same notification
        cooldowns[data] = 0.5f;
        notification.Initialize(data);

        UISoundManager.Instance?.Play(data.sound);
    }

    void Update()
    {
        List<NotificationData> keys =
            new List<NotificationData>(cooldowns.Keys);

        foreach (var key in keys)
        {
            cooldowns[key] -= Time.deltaTime;

            if (cooldowns[key] <= 0)
            {
                cooldowns.Remove(key);
            }
        }

        if (Input.GetKeyDown(KeyCode.O))
        {
            Show(database.inventoryFull);
        }
    }
}