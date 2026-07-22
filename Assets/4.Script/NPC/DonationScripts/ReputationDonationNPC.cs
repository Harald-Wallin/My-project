using UnityEngine;

public class ReputationDonationNPC : MonoBehaviour, IInteractable
{
    [Header("Donation")]
    [SerializeField] private ItemData requiredItem;

    [SerializeField] private int reputationPerItem = 5;
    [SerializeField] private int experiencePerItem = 0;

    [Header("Faction")]
    [SerializeField] private Faction faction;

    [Header("Interaction Requirements")]
    [SerializeField] private bool useReputationRequirement = false;

    [SerializeField]
    private ReputationState requiredReputation =
        ReputationState.Indifferent;

    [TextArea]
    [SerializeField]
    private string rejectedMessage =
        "I don't trust you enough.";

    public ItemData RequiredItem => requiredItem;

    public int ReputationPerItem => reputationPerItem;

    public int ExperiencePerItem => experiencePerItem;

    public Faction Faction => faction;

    public bool CanInteract(PlayerReputationManager repManager)
    {
        if (!useReputationRequirement)
            return true;

        if (repManager == null)
            return false;

        if (faction == null)
            return false;

        return repManager.GetReputationState(faction)
            >= requiredReputation;
    }

    public void Interact(PlayerStats player)
    {
        PlayerReputationManager repManager =
            player.GetComponent<PlayerReputationManager>();

        if (!CanInteract(repManager))
        {
            Debug.Log(rejectedMessage);
            return;
        }

        OpenDonationUI();
    }

    public void OpenDonationUI()
    {
        if (DonationUI.Instance != null)
        {
            DonationUI.Instance.Open(this);
        }
        else
        {
            Debug.LogWarning("No DonationUI found in scene.");
        }
    }

    public void Donate(int amount)
    {
        if (amount <= 0)
            return;

        if (requiredItem == null)
            return;

        int owned =
            Inventory.Instance.GetItemCount(requiredItem);

        amount = Mathf.Min(amount, owned);

        if (amount <= 0)
            return;

        bool removed =
            Inventory.Instance.RemoveItemAmount(requiredItem, amount);

        if (!removed)
            return;

        PlayerStats player =
            PlayerReference.Player;

        if (player == null)
            return;

        PlayerReputationManager repManager =
            player.GetComponent<PlayerReputationManager>();

        if (repManager != null && faction != null)
        {
            int reputationGain =
                reputationPerItem * amount;

            repManager.AddReputation(
                faction,
                reputationGain
            );
        }

        if (experiencePerItem > 0)
        {
            int expGain =
                experiencePerItem * amount;

            player.GainExp(expGain);
        }

        Debug.Log(
            $"Donated {amount}x {requiredItem.itemName}"
        );
    }
}
