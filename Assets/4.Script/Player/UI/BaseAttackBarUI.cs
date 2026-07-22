using UnityEngine;

public class BaseAttackBarUI : MonoBehaviour
{
    [SerializeField]
    private BaseAttackSlotUI slot;

    void Start()
    {
        PlayerStats player =
            PlayerReference.Player;

        if (player == null)
            return;

        BaseAttackController controller =
            player.GetComponent<BaseAttackController>();

        PlayerBaseAttackCollection collection =
            player.GetComponent<PlayerBaseAttackCollection>();

        slot.Initialize(
            controller,
            collection
        );
    }
}