using UnityEngine;
using UnityEngine.UI;
using System.Collections;

// Unity ke default Toggle (checkbox jaisa) ko ek sliding pill-switch jaisa dikhata hai —
// knob left-right slide karta hai, background sprite/color on-off ke hisaab se badalta hai.
[RequireComponent(typeof(Toggle))]
public class ToggleSwitchVisual : MonoBehaviour
{
    [Header("References")]
    public Image backgroundImage;   // pill/capsule shape wali background Image
    public RectTransform knob;      // gol knob jo slide karega

    [Header("Background Sprites (agar assign karo to color ki jagah ye use hongi)")]
    public Sprite onSprite;
    public Sprite offSprite;

    [Header("Background Colors (agar sprites nahi diye)")]
    public Color onColor = new Color(0.3f, 0.85f, 0.4f);
    public Color offColor = new Color(0.9f, 0.3f, 0.3f);

    [Header("Knob Positions")]
    public Vector2 knobOnPosition = new Vector2(15f, 0f);
    public Vector2 knobOffPosition = new Vector2(-15f, 0f);
    public float animDuration = 0.15f;

    private Toggle toggleComponent;
    private Coroutine slideCoroutine;

    private void Awake()
    {
        toggleComponent = GetComponent<Toggle>();
    }

    private void OnEnable()
    {
        toggleComponent.onValueChanged.AddListener(UpdateVisual);
        UpdateVisual(toggleComponent.isOn, instant: true);
    }

    private void OnDisable()
    {
        toggleComponent.onValueChanged.RemoveListener(UpdateVisual);
    }

    private void UpdateVisual(bool isOn)
    {
        UpdateVisual(isOn, instant: false);
    }

    private void UpdateVisual(bool isOn, bool instant)
    {
        if (backgroundImage != null)
        {
            if (onSprite != null && offSprite != null)
                backgroundImage.sprite = isOn ? onSprite : offSprite;
            else
                backgroundImage.color = isOn ? onColor : offColor;
        }

        if (knob != null)
        {
            Vector2 target = isOn ? knobOnPosition : knobOffPosition;

            if (instant)
            {
                knob.anchoredPosition = target;
            }
            else
            {
                if (slideCoroutine != null) StopCoroutine(slideCoroutine);
                slideCoroutine = StartCoroutine(SlideKnob(target));
            }
        }
    }

    private IEnumerator SlideKnob(Vector2 target)
    {
        Vector2 start = knob.anchoredPosition;
        float elapsed = 0f;

        while (elapsed < animDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / animDuration);
            knob.anchoredPosition = Vector2.Lerp(start, target, t);
            yield return null;
        }

        knob.anchoredPosition = target;
    }
}