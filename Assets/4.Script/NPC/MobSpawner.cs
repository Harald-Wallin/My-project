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
    private bool hasSpawnedOnce;

    void Start()
    {
        SpawnMob();
    }

    void SpawnMob()
    {
        bool isRespawn =
            hasSpawnedOnce;

        currentMob =
            Instantiate(
                mobPrefab,
                transform.position,
                Quaternion.identity
            );

        NPCBehavior ai =
            currentMob.GetComponent<
                NPCBehavior>();

        if (ai != null)
        {
            ai.SetSpawner(
                this,
                applySpawnAggroDelay:
                    isRespawn
            );

            if (patrolPath != null)
            {
                ai.SetPatrolPath(
                    patrolPath
                );
            }
        }

        hasSpawnedOnce =
            true;
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