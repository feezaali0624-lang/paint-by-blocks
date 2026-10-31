using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;
using System.Collections;

public class SplashController : MonoBehaviour
{
    [Header("UI References")]
    public Image loadingBarFill;       // Fill Area wali Image
    public RectTransform knob;         // Knob wali Image ka Rect Transform
    public TextMeshProUGUI loadingText; // Loading Text

    [Header("Settings")]
    public float loadDuration = 3f;    // Kitne seconds mein loading complete ho (3 seconds)

    private float barWidth;            // Bar ki actual width store karne ke liye

    void Start()
    {
        // Bar ki actual width nikal lein (jo aapne Inspector mein set ki hai)
        if (loadingBarFill != null)
        {
            barWidth = loadingBarFill.rectTransform.rect.width;
        }

        StartCoroutine(LoadGameAsync());
    }

    IEnumerator LoadGameAsync()
    {
        float progress = 0f;
        float timer = 0f;

        while (timer < loadDuration)
        {
            timer += Time.deltaTime;
            progress = Mathf.Clamp01(timer / loadDuration); // 0 se 1 tak value

            // 1. Bar ko fill karein
            if (loadingBarFill != null)
            {
                loadingBarFill.fillAmount = progress;
            }

            // 2. Knob ko bar ke saath move karwayein
            if (knob != null)
            {
                // Knob ki position bar ki width ke hisaab se set karein
                float knobX = progress * barWidth;
                knob.anchoredPosition = new Vector2(knobX, 0f);
            }

            // 3. Text update karein (0 se 100)
            if (loadingText != null)
            {
                loadingText.text = "LOADING ... " + Mathf.RoundToInt(progress * 100).ToString();
            }

            yield return null; // Har frame update karein (smooth animation)
        }

        // 100% hone ke baad thoda ruk kar next scene par jayein
        yield return new WaitForSeconds(0.5f);
        SceneManager.LoadScene("Gameplay scene");
    }
}