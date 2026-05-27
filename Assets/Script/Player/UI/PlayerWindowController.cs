using UnityEngine;

public class PlayerWindowController : MonoBehaviour
{
    [SerializeField] private GameObject playerWindow;
    private CharacterStats stats;

    void Start()
    {
        playerWindow.SetActive(false);
        stats = PlayerReference.Player;
    }

    // 👇 DESSA ÄR NYCKELN
    public void Open()
    {
        playerWindow.SetActive(true);
    }

    public void Close()
    {
        playerWindow.SetActive(false);
    }

    public void Toggle()
    {
        playerWindow.SetActive(!playerWindow.activeSelf);
    }

    public void ToggleFromButton()
    {
        Toggle();
    }

    public bool IsOpen()
    {
        return playerWindow != null && playerWindow.activeSelf;
    }

    void OnDestroy()
    {
        //if (stats != null)
        //{
        //    stats.OnStatsChanged -= UpdateUI;
        //    stats.OnHealthChanged -= UpdateUI;
        //}
    }

}




