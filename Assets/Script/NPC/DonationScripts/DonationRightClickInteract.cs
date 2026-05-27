using UnityEngine;

[RequireComponent(typeof(ReputationDonationNPC))]
public class DonationRightClickInteract : MonoBehaviour
{
    [SerializeField]
    private float interactionRange = 3f;

    [SerializeField]
    private Collider2D interactionCollider;

    private ReputationDonationNPC donationNPC;

    private Transform player;

    private void Awake()
    {
        donationNPC =
            GetComponent<ReputationDonationNPC>();

        if (interactionCollider == null)
        {
            Debug.LogError(
                "Donation interaction collider missing."
            );
        }

        PlayerMovement pm =
            FindFirstObjectByType<PlayerMovement>();

        if (pm != null)
        {
            player = pm.transform;
        }
    }

    private void Update()
    {
        if (Input.GetMouseButtonDown(1))
        {
            Vector2 mouseWorld =
                Camera.main.ScreenToWorldPoint(
                    Input.mousePosition
                );

            TryInteract(mouseWorld);
        }
    }

    void TryInteract(Vector2 mouseWorld)
    {
        if (donationNPC == null)
            return;

        if (player == null)
            return;

        if (interactionCollider == null)
            return;

        if (!interactionCollider.OverlapPoint(mouseWorld))
            return;

        float distance =
            Vector2.Distance(
                player.position,
                transform.position
            );

        if (distance > interactionRange)
            return;

        NPCReactionController reaction = donationNPC.GetComponent<NPCReactionController>();

        if (reaction != null)
        {
            if (reaction.BlocksInteraction(
                PlayerReference.Player))
            {
                return;
            }
        }

        donationNPC.Interact(
            PlayerReference.Player
        );
    }
}
