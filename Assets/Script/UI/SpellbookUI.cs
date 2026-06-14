using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System.Linq;

public class SpellbookUI : MonoBehaviour
{
    [SerializeField] private Transform contentParent;
    [SerializeField] private GameObject abilityEntryPrefab;
    [SerializeField] private CanvasGroup canvasGroup;

    [Header("Filtering")]
    [SerializeField] private TMP_InputField searchInput;
    private AbilityTag? activeFilter = null;

    [SerializeField] private SpellbookTabButton showAllTab;
    [SerializeField] private SpellbookTabButton meleeTab;
    [SerializeField] private SpellbookTabButton buffTab;
    [SerializeField] private SpellbookTabButton baseAttackTab;

    private PlayerAbilityCollection Collection =>
    PlayerReference.Player.GetComponent<PlayerAbilityCollection>();
    private bool isOpen = false;

    void Start()
    {
        if (canvasGroup != null)
        {
            canvasGroup.alpha = 0f;
            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = false;
        }

        if (searchInput != null)
        {
            searchInput.onValueChanged.AddListener(_ => Refresh());
        }

        Refresh();
    }

    void Populate()
    {
        var abilities = Collection.GetAllSpellbookEntries();

        string search =
            searchInput != null
            ? searchInput.text.ToLower()
            : "";

        foreach (var ability in abilities)
        {
            if (ability == null)
                continue;

            // =========================
            // SEARCH FILTER
            // =========================

            if (!string.IsNullOrEmpty(search))
            {
                bool matchesSearch =
                    ability.abilityName.ToLower().Contains(search);

                if (!matchesSearch)
                    continue;
            }

            // =========================
            // TAG FILTERS
            // =========================

            if (!PassesTagFilters(ability))
                continue;

            // =========================
            // CREATE ENTRY
            // =========================

            var go =
                Instantiate(
                    abilityEntryPrefab,
                    contentParent
                );

            go.GetComponent<AbilityEntryUI>()
                .Setup(ability);

            var drag =
                go.GetComponentInChildren<DraggableAbility>();

            if (drag != null)
            {
                drag.ability = ability;
            }
        }
    }

    //LÄGG TILL NYA FILTER HÄR
    bool PassesTagFilters(AbilityData ability)
    {

        // =========================
        // NO TAG FILTER
        // =========================

        if (activeFilter == null)
            return true;

        // =========================
        // TAG FILTER
        // =========================

        foreach (var tag in ability.tags)
        {
            if (tag == activeFilter.Value)
                return true;
        }

        return false;
    }

    void SelectTab(SpellbookTabButton selectedTab)
    {
        showAllTab.SetSelected(showAllTab == selectedTab);
        meleeTab.SetSelected(meleeTab == selectedTab);
        buffTab.SetSelected(buffTab == selectedTab);
        baseAttackTab.SetSelected(baseAttackTab == selectedTab);
    }

    public void Refresh()
    {
        //Debug.Log("SPELLBOOK REFRESH");
        //Debug.Log(Collection);
        //Debug.Log(Collection.GetAllSpellbookEntries().Count);

        // Rensa gamla entries
        foreach (Transform child in contentParent)
        {
            Destroy(child.gameObject);
        }
        // Skapa nya entries
        Populate();
    }

    public void ShowAll()
    {
        Debug.Log("SHOW ALL CLICKED");
        activeFilter = null;

        SelectTab(showAllTab);

        Refresh();
    }

    public void FilterMelee()
    {
        Debug.Log("MELEE CLICKED");
        activeFilter = AbilityTag.Melee;

        SelectTab(meleeTab);

        Refresh();
    }

    public void FilterBuff()
    {
        Debug.Log("BUFF CLICKED");
        activeFilter = AbilityTag.Buff;

        SelectTab(buffTab);

        Refresh();
    }

    public void FilterBaseAttack()
    {
        Debug.Log("BASE ATTACK CLICKED");
        activeFilter = AbilityTag.BaseAttack;

        SelectTab(baseAttackTab);

        Refresh();
    }

    public void Toggle()
    {
        isOpen = !isOpen;

        if (canvasGroup != null)
        {
            canvasGroup.alpha = isOpen ? 1f : 0f;
            canvasGroup.interactable = isOpen;
            canvasGroup.blocksRaycasts = isOpen;
        }

        if (isOpen)
        {
            SpellbookNotificationManager.Instance
                ?.ClearUnread();

            Refresh();
        }
    }

    public void Close()
    {
        isOpen = false;

        if (canvasGroup != null)
        {
            canvasGroup.alpha = 0f;
            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = false;
        }
    }
}

