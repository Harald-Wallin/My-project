using System.Collections;
using UnityEngine;

public class MobSpawner : MonoBehaviour
{
    [Header("Spawn")]
    [SerializeField] private GameObject mobPrefab;
    [SerializeField] private float respawnTime = 30f;

    [Header("Patrol")]
    [SerializeField] private PatrolPath patrolPath;

    private GameObject currentMob;

    void Start()
    {
        SpawnMob();
    }

    void SpawnMob()
    {
        currentMob = Instantiate(
            mobPrefab,
            transform.position,
            Quaternion.identity);

        NPCBehavior ai =
            currentMob.GetComponent<NPCBehavior>();

        if (ai != null && patrolPath != null)
        {
            ai.SetPatrolPath(patrolPath);
        }
    }

    public void OnMobDied()
    {
        StartCoroutine(RespawnCoroutine());
    }

    IEnumerator RespawnCoroutine()
    {
        yield return new WaitForSeconds(respawnTime);

        SpawnMob();
    }
}