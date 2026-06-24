using System.Collections;
using UnityEngine;

public class MobSpawner : MonoBehaviour
{
    public GameObject mobPrefab;
    public float respawnTime = 30f;

    [Header("Level")]
    [SerializeField] int mobLevel = 1;

    [Header("Patrol")]
    [SerializeField]
    private PatrolPath patrolPath;


    GameObject currentMob;

    void Start()
    {
     
        SpawnMob();
    }

    void SpawnMob()
    {
        currentMob = Instantiate(
            mobPrefab,
            transform.position,
            Quaternion.identity
        );

        Enemy enemy = currentMob.GetComponent<Enemy>();
        if (enemy != null)
        {
            enemy.spawner = this;
            enemy.SetLevel(mobLevel);
        }

        AgressiveMobAI ai = currentMob.GetComponent<AgressiveMobAI>();

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

