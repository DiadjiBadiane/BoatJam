// Assets/Scripts/UI/HomeScreenAnimator.cs
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

public class HomeScreenAnimator : MonoBehaviour
{
    [Header("Background Elements")]
    public RawImage backgroundImage;
    public RectTransform cloud1;
    public RectTransform cloud2;
    public RectTransform cloud3;
    public RectTransform wave1;
    public RectTransform wave2;
    public RectTransform wave3;
    public Image glint;

    [Header("Logo Elements")]
    public RectTransform boatIcon;
    public TextMeshProUGUI titleText;
    public TextMeshProUGUI subtitleText;
    public GameObject[] stars;

    [Header("Buttons")]
    public Button playButton;
    public Button levelsButton;
    public Button settingsButton;
    public Button creditsButton;

    [Header("Decorations")]
    public RectTransform decoBoat1;
    public RectTransform decoBoat2;

    [Header("Version")]
    public TextMeshProUGUI versionText;

    void Start()
    {
        StartCoroutine(AnimateClouds());
        StartCoroutine(AnimateWaves());
        StartCoroutine(AnimateGlint());
        StartCoroutine(AnimateBoatIcon());
        StartCoroutine(AnimateDecorations());
        StartCoroutine(FadeInElements());
    }

    IEnumerator AnimateClouds()
    {
        while (true)
        {
            if (cloud1) AnimateDrift(cloud1, 22f);
            if (cloud2) AnimateDrift(cloud2, 30f, -8f);
            if (cloud3) AnimateDrift(cloud3, 26f, -14f);
            yield return new WaitForSeconds(30f);
        }
    }

    void AnimateDrift(RectTransform cloud, float duration, float delay = 0f)
    {
        StartCoroutine(DriftCoroutine(cloud, duration, delay));
    }

    IEnumerator DriftCoroutine(RectTransform cloud, float duration, float delay)
    {
        yield return new WaitForSeconds(delay);
        Vector3 startPos = cloud.anchoredPosition;
        Vector3 endPos = new Vector3(450f, startPos.y, startPos.z);
        float elapsed = 0f;
        while (elapsed < duration)
        {
            cloud.anchoredPosition = Vector3.Lerp(startPos, endPos, elapsed / duration);
            elapsed += Time.deltaTime;
            yield return null;
        }
        cloud.anchoredPosition = startPos; // Reset
    }

    IEnumerator AnimateWaves()
    {
        while (true)
        {
            if (wave1) AnimateWave(wave1, 4f);
            if (wave2) AnimateWave(wave2, 5f, -1f);
            if (wave3) AnimateWave(wave3, 3.5f, -2f);
            yield return new WaitForSeconds(5f);
        }
    }

    void AnimateWave(RectTransform wave, float duration, float delay = 0f)
    {
        StartCoroutine(WaveCoroutine(wave, duration, delay));
    }

    IEnumerator WaveCoroutine(RectTransform wave, float duration, float delay)
    {
        yield return new WaitForSeconds(delay);
        Vector3 startPos = wave.anchoredPosition;
        Vector3 endPos = new Vector3(startPos.x + 50f, startPos.y, startPos.z); // Approximate translateX
        float elapsed = 0f;
        bool alternate = true;
        while (elapsed < duration)
        {
            float t = elapsed / duration;
            if (alternate)
                wave.anchoredPosition = Vector3.Lerp(startPos, endPos, t);
            else
                wave.anchoredPosition = Vector3.Lerp(endPos, startPos, t);
            elapsed += Time.deltaTime;
            yield return null;
        }
        alternate = !alternate;
    }

    IEnumerator AnimateGlint()
    {
        while (true)
        {
            if (glint)
            {
                float elapsed = 0f;
                while (elapsed < 3f)
                {
                    float t = elapsed / 3f;
                    glint.color = new Color(glint.color.r, glint.color.g, glint.color.b, Mathf.Lerp(0.4f, 0.9f, t));
                    glint.rectTransform.sizeDelta = new Vector2(Mathf.Lerp(160f, 240f, t), 6f);
                    elapsed += Time.deltaTime;
                    yield return null;
                }
                elapsed = 0f;
                while (elapsed < 3f)
                {
                    float t = elapsed / 3f;
                    glint.color = new Color(glint.color.r, glint.color.g, glint.color.b, Mathf.Lerp(0.9f, 0.4f, t));
                    glint.rectTransform.sizeDelta = new Vector2(Mathf.Lerp(240f, 160f, t), 6f);
                    elapsed += Time.deltaTime;
                    yield return null;
                }
            }
            yield return null;
        }
    }

    IEnumerator AnimateBoatIcon()
    {
        while (true)
        {
            if (boatIcon)
            {
                float elapsed = 0f;
                while (elapsed < 3f)
                {
                    float t = elapsed / 3f;
                    boatIcon.anchoredPosition = new Vector2(boatIcon.anchoredPosition.x, Mathf.Lerp(0, -8f, t));
                    boatIcon.rotation = Quaternion.Euler(0, 0, Mathf.Lerp(-3f, 3f, t));
                    elapsed += Time.deltaTime;
                    yield return null;
                }
                elapsed = 0f;
                while (elapsed < 3f)
                {
                    float t = elapsed / 3f;
                    boatIcon.anchoredPosition = new Vector2(boatIcon.anchoredPosition.x, Mathf.Lerp(-8f, 0, t));
                    boatIcon.rotation = Quaternion.Euler(0, 0, Mathf.Lerp(3f, -3f, t));
                    elapsed += Time.deltaTime;
                    yield return null;
                }
            }
            yield return null;
        }
    }

    IEnumerator AnimateDecorations()
    {
        while (true)
        {
            if (decoBoat1) AnimateDeco(decoBoat1, 4f);
            if (decoBoat2) AnimateDeco(decoBoat2, 5f, -2f);
            yield return new WaitForSeconds(5f);
        }
    }

    void AnimateDeco(RectTransform deco, float duration, float delay = 0f)
    {
        StartCoroutine(DecoCoroutine(deco, duration, delay));
    }

    IEnumerator DecoCoroutine(RectTransform deco, float duration, float delay)
    {
        yield return new WaitForSeconds(delay);
        Vector3 startPos = deco.anchoredPosition;
        Quaternion startRot = deco.rotation;
        Vector3 endPos = new Vector3(startPos.x, startPos.y - 12f, startPos.z);
        Quaternion endRot = Quaternion.Euler(0, 0, 5f);
        float elapsed = 0f;
        bool alternate = true;
        while (elapsed < duration)
        {
            float t = elapsed / duration;
            if (alternate)
            {
                deco.anchoredPosition = Vector3.Lerp(startPos, endPos, t);
                deco.rotation = Quaternion.Lerp(startRot, endRot, t);
            }
            else
            {
                deco.anchoredPosition = Vector3.Lerp(endPos, startPos, t);
                deco.rotation = Quaternion.Lerp(endRot, startRot, t);
            }
            elapsed += Time.deltaTime;
            yield return null;
        }
        alternate = !alternate;
    }

    IEnumerator FadeInElements()
    {
        // Fade in logo area
        yield return new WaitForSeconds(0.2f);
        if (boatIcon) FadeIn(boatIcon.gameObject, 0.8f);
        if (titleText) FadeIn(titleText.gameObject, 0.8f);
        if (subtitleText) FadeIn(subtitleText.gameObject, 0.8f);
        foreach (var star in stars) if (star) FadeIn(star, 0.8f);

        // Fade in buttons
        yield return new WaitForSeconds(0.2f);
        if (playButton) FadeIn(playButton.gameObject, 0.8f);
        if (levelsButton) FadeIn(levelsButton.gameObject, 0.8f);
        if (settingsButton) FadeIn(settingsButton.gameObject, 0.8f);
        if (creditsButton) FadeIn(creditsButton.gameObject, 0.8f);

        // Fade in version
        yield return new WaitForSeconds(0.2f);
        if (versionText) FadeIn(versionText.gameObject, 0.8f);
    }

    void FadeIn(GameObject obj, float duration)
    {
        CanvasGroup cg = obj.GetComponent<CanvasGroup>();
        if (cg == null) cg = obj.AddComponent<CanvasGroup>();
        cg.alpha = 0;
        StartCoroutine(FadeInCoroutine(cg, duration));
    }

    IEnumerator FadeInCoroutine(CanvasGroup cg, float duration)
    {
        float elapsed = 0f;
        while (elapsed < duration)
        {
            cg.alpha = elapsed / duration;
            elapsed += Time.deltaTime;
            yield return null;
        }
        cg.alpha = 1;
    }
}