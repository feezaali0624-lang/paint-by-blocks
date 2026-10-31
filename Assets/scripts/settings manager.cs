using UnityEngine;
using UnityEngine.UI;

// Settings Panel: Music volume slider, Sound volume slider, Haptics on/off toggle.
// Sab PlayerPrefs mein save hota hai, app dobara khulne par yaad rehta hai.
public class SettingsManager : MonoBehaviour
{
    public static SettingsManager Instance { get; private set; }

    private const string MusicVolumeKey = "SettingsMusicVolume";
    private const string SoundVolumeKey = "SettingsSoundVolume";
    private const string HapticsKey = "SettingsHapticsEnabled";

    public float MusicVolume { get; private set; } = 1f;
    public float SoundVolume { get; private set; } = 1f;
    public bool HapticsEnabled { get; private set; } = true;

    public bool SoundEnabled => SoundVolume > 0f;

    [Header("Audio Sources")]
    public AudioSource musicSource;
    public AudioSource sfxSource;

    [Header("Settings Panel UI")]
    public Slider musicSlider;
    public Slider soundSlider;
    public Toggle hapticsToggle;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        LoadSettings();
    }

    private void Start()
    {
        ApplyAudioVolumes();
        SyncUIWithSettings();
    }

    private void LoadSettings()
    {
        MusicVolume = PlayerPrefs.GetFloat(MusicVolumeKey, 1f);
        SoundVolume = PlayerPrefs.GetFloat(SoundVolumeKey, 1f);
        HapticsEnabled = PlayerPrefs.GetInt(HapticsKey, 1) == 1;
    }

    private void SyncUIWithSettings()
    {
        if (musicSlider != null)
        {
            Debug.Log("[SettingsManager] musicSlider mila, interactable = " + musicSlider.interactable);
            musicSlider.minValue = 0f;
            musicSlider.maxValue = 1f;
            musicSlider.value = MusicVolume;
            musicSlider.onValueChanged.AddListener(SetMusicVolume);
        }
        else
        {
            Debug.LogWarning("[SettingsManager] musicSlider assign NAHI hai Inspector mein!");
        }

        if (soundSlider != null)
        {
            Debug.Log("[SettingsManager] soundSlider mila, interactable = " + soundSlider.interactable);
            soundSlider.minValue = 0f;
            soundSlider.maxValue = 1f;
            soundSlider.value = SoundVolume;
            soundSlider.onValueChanged.AddListener(SetSoundVolume);
        }
        else
        {
            Debug.LogWarning("[SettingsManager] soundSlider assign NAHI hai Inspector mein!");
        }

        if (hapticsToggle != null)
        {
            hapticsToggle.isOn = HapticsEnabled;
            hapticsToggle.onValueChanged.AddListener(SetHapticsEnabled);
        }
    }

    private void ApplyAudioVolumes()
    {
        if (musicSource != null) musicSource.volume = MusicVolume;
        if (sfxSource != null) sfxSource.volume = SoundVolume;
    }

    // ---------------- PUBLIC SETTERS ----------------

    public void SetMusicVolume(float value)
    {
        Debug.Log("[SettingsManager] SetMusicVolume CALLED with value = " + value);

        MusicVolume = Mathf.Clamp01(value);
        if (musicSource != null) musicSource.volume = MusicVolume;

        PlayerPrefs.SetFloat(MusicVolumeKey, MusicVolume);
        PlayerPrefs.Save();
    }

    public void SetSoundVolume(float value)
    {
        Debug.Log("[SettingsManager] SetSoundVolume CALLED with value = " + value);

        SoundVolume = Mathf.Clamp01(value);
        if (sfxSource != null) sfxSource.volume = SoundVolume;

        PlayerPrefs.SetFloat(SoundVolumeKey, SoundVolume);
        PlayerPrefs.Save();
    }

    public void SetHapticsEnabled(bool enabled)
    {
        HapticsEnabled = enabled;
        PlayerPrefs.SetInt(HapticsKey, enabled ? 1 : 0);
        PlayerPrefs.Save();
    }

    public void SetSoundEnabled(bool enabled)
    {
        SetSoundVolume(enabled ? 1f : 0f);
    }

    // ---------------- SOUND HELPER ----------------

    public void PlaySfx(AudioClip clip)
    {
        if (clip == null || sfxSource == null) return;
        if (SoundVolume <= 0f) return;

        sfxSource.PlayOneShot(clip, SoundVolume);
    }
}