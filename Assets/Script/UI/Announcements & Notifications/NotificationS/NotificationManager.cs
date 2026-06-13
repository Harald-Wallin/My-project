using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;

public class NotificationManager : MonoBehaviour
{
    public static NotificationManager Instance;

    [SerializeField]
    //private NotificationUI notificationUI;

   // [SerializeField]
    private NotificationData testNotification;

    [SerializeField]
    private NotificationDatabase database;
    public NotificationDatabase Database => database;


    private Queue<NotificationData> queue = new Queue<NotificationData>();

    private Coroutine currentRoutine;

    void Awake()
    {
        Instance = this;
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.O))
        {
            Show(testNotification);
        }
    }

    public void Show(NotificationData data)
    {
        if (data == null)
            return;

        if (currentRoutine != null)
            StopCoroutine(currentRoutine);

        //TESTBLOCK

        if (data.sound != null)
        {
            Debug.Log("Playing sound: " + data.sound.name);
        }
        else
        {
            Debug.Log("NO SOUND ASSIGNED");
        }

        //notificationUI.Show(data);

        UISoundManager.Instance?.Play(data.sound);

        currentRoutine = StartCoroutine(HideRoutine(data.duration));
    }

    public void QueueNotification(NotificationData data)
    {
        queue.Enqueue(data);
    }

    IEnumerator HideRoutine(float duration)
    {
        yield return new WaitForSeconds(duration);

        //notificationUI.Hide();

        currentRoutine = null;
    }
}
