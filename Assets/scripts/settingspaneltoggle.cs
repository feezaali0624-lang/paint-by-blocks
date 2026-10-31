using UnityEngine;

// Settings panel ko open/close karne ke liye simple toggle script
public class SettingsPanelToggle : MonoBehaviour
{
    public GameObject settingsPanel;

    public void OpenPanel()
    {
        settingsPanel.SetActive(true);
    }

    public void ClosePanel()
    {
        settingsPanel.SetActive(false);
    }
}