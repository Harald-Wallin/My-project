using UnityEngine;

public class GlobalUIManager : MonoBehaviour
{
    [SerializeField] private GameObject spellbook;

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.B))
            ToggleSpellbook();

        if (Input.GetKeyDown(KeyCode.I))
            ToggleInventory();

        if (Input.GetKeyDown(KeyCode.P))
            TogglePlayerWindow();

        if (Input.GetKeyDown(KeyCode.T))
            ToggleTalentWindow();

        if (Input.GetKeyDown(KeyCode.U))
            ToggleReputationWindow();

        if (Input.GetKeyDown(KeyCode.Escape))
            CloseAll();
    }

    // =========================
    // TOGGLES
    // =========================

    public void ToggleSpellbook()
    {
        if (spellbook != null)
            spellbook.SetActive(!spellbook.activeSelf);
    }

    public void ToggleInventory()
    {
        var inv = FindFirstObjectByType<InventoryUI>(FindObjectsInactive.Include);
        if (inv != null)
            inv.Toggle();
    }

    public void TogglePlayerWindow()
    {
        var pw = FindFirstObjectByType<PlayerWindowController>(FindObjectsInactive.Include);
        if (pw != null)
            pw.Toggle();
    }

    public void ToggleTalentWindow()
    {
        var tw = FindFirstObjectByType<TalentWindowUI>(FindObjectsInactive.Include);
        if (tw != null)
            tw.Toggle();
    }

    public void ToggleReputationWindow()
    {
        var rep = FindFirstObjectByType<ReputationWindowUI>(FindObjectsInactive.Include);

        if (rep != null)
            rep.gameObject.SetActive(!rep.gameObject.activeSelf);
    }

    // =========================
    // CLOSE ALL (ESC)
    // =========================

    void CloseAll()
    {
        var talent = FindFirstObjectByType<TalentWindowUI>(FindObjectsInactive.Include);
        if (talent != null && talent.gameObject.activeSelf)
            talent.Close();

        var spell = FindFirstObjectByType<SpellbookUI>(FindObjectsInactive.Include);
        if (spell != null && spell.gameObject.activeSelf)
            spell.Close();

        var vendor = FindFirstObjectByType<VendorUI>(FindObjectsInactive.Include);
        if (vendor != null && vendor.IsOpen())
            vendor.Close();

        var inv = FindFirstObjectByType<InventoryUI>(FindObjectsInactive.Include);
        if (inv != null && inv.IsOpen())
            inv.Close();

        var loot = FindFirstObjectByType<LootUI>(FindObjectsInactive.Include);
        if (loot != null && loot.gameObject.activeSelf)
            loot.Close();

        var pw = FindFirstObjectByType<PlayerWindowController>(FindObjectsInactive.Include);
        if (pw != null && pw.IsOpen())
            pw.Close();

        var rep = FindFirstObjectByType<ReputationWindowUI>(FindObjectsInactive.Include);
        if (rep != null && rep.gameObject.activeSelf)
            rep.gameObject.SetActive(false);

        var don = FindFirstObjectByType<DonationUI>(FindObjectsInactive.Include);
        if (don != null && don.gameObject.activeSelf)
            don.Close();
    }
}



/*using UnityEngine;

public class GlobalUIManager : MonoBehaviour
{
    [Header("Direct references (optional but recommended)")]
    [SerializeField] private GameObject spellbook;

    void Update()
    {
        HandleHotkeys();
        HandleEscape();
    }

    void HandleHotkeys()
    {
        if (Input.GetKeyDown(KeyCode.B))
            ToggleSpellbook();

        if (Input.GetKeyDown(KeyCode.I))
            ToggleInventory();

        if (Input.GetKeyDown(KeyCode.P))
            TogglePlayerWindow();

        if (Input.GetKeyDown(KeyCode.R)) // ex reputation
            ToggleReputationWindow();

        if (Input.GetKeyDown(KeyCode.T)) // ex talents
            ToggleTalentWindow();
    }

    void HandleEscape()
    {
        if (!Input.GetKeyDown(KeyCode.Escape))
            return;

        CloseIfOpen<TalentWindowUI>();
        CloseIfOpen<SpellbookUI>();
        CloseIfOpen<VendorUI>();
        CloseIfOpen<InventoryUI>();
        CloseIfOpen<LootUI>();
        CloseIfOpen<PlayerWindowController>();
        CloseIfOpen<ReputationWindowUI>();
    }

    // 🔥 GENERISK CLOSE
    void CloseIfOpen<T>() where T : MonoBehaviour
    {
        var ui = FindFirstObjectByType<T>();

        if (ui == null)
            return;

        if (ui.gameObject.activeSelf)
            ui.gameObject.SetActive(false);
    }

    // =========================
    // BUTTON / HOTKEY FUNCTIONS
    // =========================

    public void ToggleSpellbook()
    {
        if (spellbook != null)
        {
            spellbook.SetActive(!spellbook.activeSelf);
            return;
        }

        var ui = FindFirstObjectByType<SpellbookUI>();
        if (ui != null)
            ui.gameObject.SetActive(!ui.gameObject.activeSelf);
    }

    public void ToggleInventory()
    {
        var ui = FindFirstObjectByType<InventoryUI>();
        if (ui != null)
            ui.gameObject.SetActive(!ui.gameObject.activeSelf);
    }

    public void TogglePlayerWindow()
    {
        var ui = FindFirstObjectByType<PlayerWindowController>();
        if (ui != null)
            ui.Toggle(); // denna har redan Toggle()
    }

    public void ToggleReputationWindow()
    {
        var ui = FindFirstObjectByType<ReputationWindowUI>();
        if (ui != null)
            ui.gameObject.SetActive(!ui.gameObject.activeSelf);
    }

    public void ToggleTalentWindow()
    {
        var ui = FindFirstObjectByType<TalentWindowUI>();
        if (ui != null)
            ui.gameObject.SetActive(!ui.gameObject.activeSelf);
    }
}*/