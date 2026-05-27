using UnityEngine;

public class FloatingTextSpawner : MonoBehaviour
{
    [Header("Prefabs")]
    public GameObject playerDamagePrefab;
    //[SerializeField] private Transform worldCanvas;

    public static FloatingTextSpawner Instance;

    void Awake()
    {
        Instance = this;
    }

    public void SpawnPlayerDamage(
        Vector3 position,
        int damage,
        bool isCrit
        )
        {
        GameObject obj = Instantiate(
            playerDamagePrefab,
            position + Vector3.up * 1.5f,
            Quaternion.identity
        );

        FloatingText text = obj.GetComponentInChildren<FloatingText>();
        if (text != null)
        {
            text.Setup("-" + damage, isCrit);
        }
    }

    public void SpawnEnemyDamage(Vector3 position, int damage, bool isCrit)
    {
        // samma som player men annan färg/stil om du vill
        // Enemy damage -> player is target (not source)
        SpawnCustomText(position, "-" + damage, isCrit);
    }

    public void SpawnCustomText(Vector3 position, string textValue, bool isCrit)
    {
        GameObject obj = Instantiate(
            playerDamagePrefab,
            position + Vector3.up * 1.5f,
            Quaternion.identity
        );

        FloatingText text = obj.GetComponentInChildren<FloatingText>();
        if (text != null)
        {
            text.Setup(textValue, isCrit);
        }
    }
}


