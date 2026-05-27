using UnityEngine;

public class ReputationWindowToggle : MonoBehaviour
{
    public GameObject reputationWindow;
    void ToggleWindow()
    {
        Debug.Log("Toggled Reputation Window through ToggleWindow");
        if (reputationWindow == null)
            return;

        reputationWindow.SetActive(!reputationWindow.activeSelf);
    }
}

