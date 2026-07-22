using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AnnouncementSpawner : MonoBehaviour
{
    public static AnnouncementSpawner Instance;

    [SerializeField]
    private AnnouncementInstance prefab;

    [SerializeField]
    private AnnouncementDatabase database;
    public AnnouncementDatabase Database => database;

    [SerializeField]
    private Transform parentContainer;

    private Queue<AnnouncementRequest> queue = new Queue<AnnouncementRequest>();

    private bool isPlaying;

    void Awake()
    {
        Instance = this;
    }

    public void QueueAnnouncement(AnnouncementData data,string customMessage = null, AudioClip overrideSound = null)
    {
        if (data == null)
            return;

        queue.Enqueue(new AnnouncementRequest(
                data,
                customMessage,
                overrideSound
            )
        );

        if (!isPlaying)
        {
            StartCoroutine(ProcessQueue());
        }
    }

    IEnumerator ProcessQueue()
    {
        isPlaying = true;

        while (queue.Count > 0)
        {
            AnnouncementRequest request = queue.Dequeue();

            AnnouncementData data =  request.data;

            AnnouncementInstance instance = Instantiate(prefab,parentContainer);

            instance.Initialize(data,request.customMessage);

            UISoundManager.Instance?.Play(request.overrideSound != null ? request.overrideSound : data.sound);

            yield return new WaitForSeconds(
                data.duration + 0.25f
            );
        }

        isPlaying = false;
    }


}
