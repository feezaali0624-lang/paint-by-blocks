using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class MainMenuController : MonoBehaviour
{
    [Header("Panels")]
    public GameObject mainMenuPanel;
    public GameObject gameplayPanel;
    public GameObject settingsPanel;

    [Header("Main Menu Buttons")]
    public Button homeButton;           // Main menu ka HOME (Play) button
    public Button hapticButton;
    public Button soundButton;

    [Header("Gameplay Buttons")]
    public Button gameplayHomeButton;   // Naya - Gameplay se Main Menu jane ke liye

    [Header("Settings Panel Buttons")]
    public Button settingsHapticButton;
    public Button settingsSoundButton;
    public Button settingsCloseButton;
    public Button resumeButton;
    public Button replayButton;
    public Button homeButtonInSettings;

    [Header("Main Menu Button Icons")]
    public Image hapticButtonIcon;
    public Image soundButtonIcon;

    [Header("Settings Panel Button Icons")]
    public Image settingsHapticIcon;
    public Image settingsSoundIcon;

    [Header("Sprites (agar alag hain)")]
    public Sprite hapticOnSprite;
    public Sprite hapticOffSprite;
    public Sprite soundOnSprite;
    public Sprite soundOffSprite;

    [Header("Colors (agar sirf color change karna hai)")]
    public Color iconOnColor = Color.white;
    public Color iconOffColor = new Color(0.5f, 0.5f, 0.5f, 1f);

    [Header("Game References")]
    public PieceGridRenderer pieceGridRenderer; // Replay ke liye

    private bool isHapticOn;
    private bool isSoundOn;

    void Start()
    {
        if (mainMenuPanel != null) mainMenuPanel.SetActive(true);
        if (gameplayPanel != null) gameplayPanel.SetActive(false);
        if (settingsPanel != null) settingsPanel.SetActive(false);

        if (SettingsManager.Instance != null)
        {
            isHapticOn = SettingsManager.Instance.HapticsEnabled;
            isSoundOn = SettingsManager.Instance.SoundEnabled;
        }
        else
        {
            isHapticOn = true;
            isSoundOn = true;
        }

        UpdateAllIcons();

        // Main Menu Buttons
        if (homeButton != null) homeButton.onClick.AddListener(OnHomePressed);
        if (hapticButton != null) hapticButton.onClick.AddListener(OnHapticPressed);
        if (soundButton != null) soundButton.onClick.AddListener(OnSoundPressed);

        // Gameplay Buttons (Naya)
        if (gameplayHomeButton != null) gameplayHomeButton.onClick.AddListener(OnGameplayHomePressed);

        // Settings Panel Buttons
        if (settingsHapticButton != null) settingsHapticButton.onClick.AddListener(OnHapticPressed);
        if (settingsSoundButton != null) settingsSoundButton.onClick.AddListener(OnSoundPressed);
        if (settingsCloseButton != null) settingsCloseButton.onClick.AddListener(CloseSettings);
        if (resumeButton != null) resumeButton.onClick.AddListener(OnResumePressed);
        if (replayButton != null) replayButton.onClick.AddListener(OnReplayPressed);
        if (homeButtonInSettings != null) homeButtonInSettings.onClick.AddListener(OnHomeFromSettingsPressed);
    }

    // ---------------- HOME (Main Menu se Gameplay) ----------------
    public void OnHomePressed()
    {
        if (mainMenuPanel != null) mainMenuPanel.SetActive(false);
        if (gameplayPanel != null) gameplayPanel.SetActive(true);
    }

    // ---------------- GAMEPLAY SE MAIN MENU (Naya) ----------------
    // ---------------- GAMEPLAY SE MAIN MENU (Naya) ----------------
    public void OnGameplayHomePressed()
    {
       
        if (pieceGridRenderer != null)
            pieceGridRenderer.ResetBoard();

        if (gameplayPanel != null) gameplayPanel.SetActive(false);
        if (mainMenuPanel != null) mainMenuPanel.SetActive(true);
    }

    // ---------------- SETTINGS OPEN/CLOSE ----------------
    public void OpenSettings()
    {
        if (settingsPanel != null) settingsPanel.SetActive(true);
    }

    public void CloseSettings()
    {
        if (settingsPanel != null) settingsPanel.SetActive(false);
    }

    // ---------------- RESUME (Settings band karo, Gameplay wapas) ----------------
    public void OnResumePressed()
    {
        CloseSettings();
    }

    // ---------------- REPLAY (Board reset karo) ----------------
    public void OnReplayPressed()
    {
        if (pieceGridRenderer != null)
            pieceGridRenderer.ResetBoard();

        CloseSettings();
    }

    // ---------------- HOME (Settings se wapas Main Menu) ----------------
    public void OnHomeFromSettingsPressed()
    {
        CloseSettings();

        if (gameplayPanel != null) gameplayPanel.SetActive(false);
        if (mainMenuPanel != null) mainMenuPanel.SetActive(true);
    }

    // ---------------- HAPTIC ----------------
    private void OnHapticPressed()
    {
        isHapticOn = !isHapticOn;

        if (SettingsManager.Instance != null)
            SettingsManager.Instance.SetHapticsEnabled(isHapticOn);

        UpdateAllIcons();

        if (isHapticOn)
            SimpleHaptics.LightTick(showDebugToast: false);
    }

    // ---------------- SOUND ----------------
    private void OnSoundPressed()
    {
        isSoundOn = !isSoundOn;

        if (SettingsManager.Instance != null)
            SettingsManager.Instance.SetSoundEnabled(isSoundOn);

        UpdateAllIcons();
    }

    // ---------------- ICON UPDATE ----------------
    private void UpdateAllIcons()
    {
        UpdateIcon(hapticButtonIcon, isHapticOn, hapticOnSprite, hapticOffSprite);
        UpdateIcon(soundButtonIcon, isSoundOn, soundOnSprite, soundOffSprite);
        UpdateIcon(settingsHapticIcon, isHapticOn, hapticOnSprite, hapticOffSprite);
        UpdateIcon(settingsSoundIcon, isSoundOn, soundOnSprite, soundOffSprite);
    }

    private void UpdateIcon(Image icon, bool isOn, Sprite onSprite, Sprite offSprite)
    {
        if (icon == null) return;

        if (onSprite != null && offSprite != null)
            icon.sprite = isOn ? onSprite : offSprite;
        else
            icon.color = isOn ? iconOnColor : iconOffColor;
    }
}