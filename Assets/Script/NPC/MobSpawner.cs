using System.Collections;
using UnityEngine;

public class MobSpawner : MonoBehaviour
{
    public GameObject mobPrefab;
    public float respawnTime = 30f;

    [Header("Level")]
    [SerializeField] int mobLevel = 1;


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

