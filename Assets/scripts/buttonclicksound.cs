using UnityEngine;
using UnityEngine.UI;

// Kisi bhi Button par laga do — click hote hi automatically SFX bajayega
[RequireComponent(typeof(Button))]
public class ButtonClickSound : MonoBehaviour
{
    public AudioClip clickSound;

    private void Start()
    {
        GetComponent<Button>().onClick.AddListener(PlayClick);
    }

    private void PlayClick()
    {
        if (SettingsManager.Instance != null)
            SettingsManager.Instance.PlaySfx(clickSound);
    }
}