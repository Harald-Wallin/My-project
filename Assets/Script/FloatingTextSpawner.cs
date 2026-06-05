using UnityEngine;

public class FloatingTextSpawner : MonoBehaviour
{
    [Header("Prefabs")]
    public GameObject playerDamagePrefab;

    public static FloatingTextSpawner Instance;

    void Awake()
    {
        Instance = this;
    }

    public void SpawnPlayerDamage(
        Vector3 position,
        int damage,
        bool isCrit)
    {
        SpawnText(
            position,
            "-" + damage,
            isCrit
                ? FloatingTextStyle.PlayerCrit
                : FloatingTextStyle.PlayerDamage
        );
    }

    public void SpawnEnemyDamage(
        Vector3 position,
        int damage,
        bool isCrit)
    {
        SpawnText(
            position,
            "-" + damage,
            isCrit
                ? FloatingTextStyle.EnemyCrit
                : FloatingTextStyle.EnemyDamage
        );
    }

    public void SpawnCustomText(
        Vector3 position,
        string textValue,
        bool isCrit)
    {
        FloatingTextStyle style = textValue == "Miss" ? FloatingTextStyle.Miss
        : textValue == "Evade" ? FloatingTextStyle.Evade
        : textValue.StartsWith("Block") ? FloatingTextStyle.Block
        : textValue == "Evade" ? FloatingTextStyle.Evade
        : FloatingTextStyle.PlayerDamage;

        SpawnText(
            position,
            textValue,
            style
        );
    }

    void SpawnText(
        Vector3 position,
        string value,
        FloatingTextStyle style)
    {
        GameObject obj = Instantiate(
            playerDamagePrefab,
            position + Vector3.up * 1.5f,
            Quaternion.identity
        );

        FloatingText text =
            obj.GetComponentInChildren<FloatingText>();

        if (text != null)
        {
            text.Setup(
                value,
                style
            );
        }
    }
}