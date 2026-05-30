using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

public class GlobalUIManager : MonoBehaviour
{
    [SerializeField] private GameObject spellbook;

    void Update()
    {
        if (IsTypingInInputField())
            return;

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

    bool IsTypingInInputField()
    {
        if (EventSystem.current == null)
            return false;

        GameObject selected =
            EventSystem.current.currentSelectedGameObject;

        if (selected == null)
            return false;

        return selected.GetComponent<TMP_InputField>() != null;
    }

    // =========================
    // TOGGLES
    // =========================

    /*ublic void ToggleSpellbook()
     {
         if (spellbook != null)
             spellbook.SetActive(!spellbook.activeSelf);
     }*/

    public void ToggleSpellbook()
    {
        var sb = FindFirstObjectByType<SpellbookUI>(FindObjectsInactive.Include);
        if (sb != null)
            sb.Toggle();
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