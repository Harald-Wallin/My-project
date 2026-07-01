using UnityEngine;
using System.Collections;

public class ActionBarUI : MonoBehaviour
{
    [SerializeField] private ActionSlot[] slots;

    private AbilityController abilityController;
    private PlayerAbilityCollection collection;

    IEnumerator Start()
    {
        yield return null; // 🔥 vänta 1 frame

        var player = PlayerReference.Player;

        if (player == null)
        {
            yield break;
        }

        collection = player.GetComponent<PlayerAbilityCollection>();

        if (collection == null)
        {
            yield break;
        }

        abilityController = player.GetComponent<AbilityController>();

        if (abilityController == null)
        {
            yield break;
        }

        var abilities = collection.GetEquippedAbilities();

        for (int i = 0; i < slots.Length; i++)
        {
            if (slots[i] == null)
            {
                continue;
            }

            AbilityData ability =
                i < abilities.Length ? abilities[i] : null;

            slots[i].Initialize(
                abilityController,
                ability,
                i
            );
        }
    }
}
