using UnityEngine;

public class CombatHitbox : MonoBehaviour
{
    [SerializeField] private CharacterStats owner;

    public CharacterStats Owner => owner;

    void Awake()
    {
        if (owner == null)
        {
            owner = GetComponentInParent<CharacterStats>();
        }
    }
}
