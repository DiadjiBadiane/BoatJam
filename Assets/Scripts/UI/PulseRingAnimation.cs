// Assets/Scripts/UI/PulseRingAnimation.cs
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Attached automatically by LevelCardView to the PulseRing child object
/// on the currently selected (active) level card.
/// Expands and fades the ring outward in a loop.
/// </summary>
[RequireComponent(typeof(RectTransform))]
[RequireComponent(typeof(Image))]
public class PulseRingAnimation : MonoBehaviour
{
    const float Duration  = 1.5f;   // seconds per pulse cycle
    const float StartInset =  6f;   // how far inside the card edge the ring starts
    const float EndInset   = 18f;   // how far outside the card edge the ring ends
    const float StartAlpha = 0.70f;
    const float EndAlpha   = 0.00f;

    float elapsed;
    RectTransform rt;
    Image img;
    Color baseColor;

    void Awake()
    {
        rt       = GetComponent<RectTransform>();
        img      = GetComponent<Image>();
        baseColor = img != null ? img.color : new Color(0.96f, 0.62f, 0.07f, 1f);
    }

    void Update()
    {
        elapsed = (elapsed + Time.deltaTime) % Duration;
        float t = elapsed / Duration; // 0 → 1

        float inset  = Mathf.Lerp(-StartInset, -EndInset, t);
        rt.offsetMin = new Vector2( inset,  inset);
        rt.offsetMax = new Vector2(-inset, -inset);

        float alpha = Mathf.Lerp(StartAlpha, EndAlpha, t);
        img.color   = new Color(baseColor.r, baseColor.g, baseColor.b, alpha);
    }
}