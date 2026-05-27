using UnityEngine;

public class SpellbookUI : MonoBehaviour
{
    [SerializeField] private Transform contentParent;
    [SerializeField] private GameObject abilityEntryPrefab;

    private PlayerAbilityCollection collection;
    private bool isOpen = false;

    void Start()
    {
        gameObject.SetActive(false);

        var player = PlayerReference.Player;
        collection = player.GetComponent<PlayerAbilityCollection>();

        Refresh();
    }

    void Populate()
    {
        var abilities = collection.GetLearnedAbilities();

        foreach (var ability in abilities)
        {
            if (ability == null) continue;

            var go = Instantiate(abilityEntryPrefab, contentParent);

            // 🔹 1. Setup UI
            go.GetComponent<AbilityEntryUI>().Setup(ability);

            // 🔹 2. Sätt drag-data (VIKTIG!)
            var drag = go.GetComponentInChildren<DraggableAbility>();
            if (drag != null)
            {
                drag.ability = ability;
            }
        }
    }

    public void Refresh()
    {
        // Rensa gamla entries
        foreach (Transform child in contentParent)
        {
            Destroy(child.gameObject);
        }
        // Skapa nya entries
        Populate();
    }

    public void Toggle()
    {
        isOpen = !isOpen;
        gameObject.SetActive(isOpen);
        if (isOpen)
        {
            Refresh();
        }
    }

    public void Close()
    {
        isOpen = false;
        gameObject.SetActive(false);
    }
}

