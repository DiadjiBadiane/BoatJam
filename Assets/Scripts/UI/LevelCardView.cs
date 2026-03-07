using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class LevelCardView : MonoBehaviour
{
    [Header("References")]
    public Button button;
    public Image background;
    public TextMeshProUGUI levelNumberLabel;
    public GameObject lockIcon;
    public Image[] stars;

    [Header("Style")]
    public bool autoApplyRoundedStyle = true;
    public bool autoApplyRaisedShadow = true;
    public bool showLockUntilCompleted = false;
    public bool forceRoundedSquareBody = true;
    public Sprite roundedSquareSprite; // optional: assign a custom sprite in Inspector

    [Header("Colors")]
    public Color unlockedColor = new Color(0.22f, 0.60f, 0.40f, 1f);
    public Color lockedColor   = new Color(1f,    1f,    1f,    0.08f);
    public Color selectedColor = new Color(0.96f, 0.62f, 0.07f, 1f);
    public Color starOnColor   = new Color(1f,    0.83f, 0.20f, 1f);
    public Color starOffColor  = new Color(1f,    1f,    1f,    0.20f);

    // ── Shared rounded-square sprite (generated once, reused by all cards) ──
    static Sprite s_RoundedSquare;

    static readonly string[] StarNames = { "Stars1", "Stars2", "Stars3" };
    bool styleApplied;
    Image runtimeBody;

    // ────────────────────────────────────────────────────────────────────────

    void Awake()
    {
        if (button == null) button = GetComponent<Button>();
        if (background == null) background = GetComponent<Image>();

        if (levelNumberLabel == null)
        {
            var t = transform.Find("LevelNumber (TMP)") ?? transform.Find("LevelNumber");
            if (t != null) levelNumberLabel = t.GetComponent<TextMeshProUGUI>();
            if (levelNumberLabel == null)
                levelNumberLabel = GetComponentInChildren<TextMeshProUGUI>(true);
        }
        levelNumberLabel.color = Color.white;

        if (lockIcon == null)
        {
            var t = transform.Find("LockIcon") ?? transform.Find("Lock");
            if (t != null) lockIcon = t.gameObject;
        }

        if (stars == null || stars.Length == 0)
        {
            stars = new Image[3];
            for (int i = 0; i < StarNames.Length; i++)
            {
                var t = transform.Find(StarNames[i]);
                if (t != null) stars[i] = t.GetComponent<Image>();
            }

            if (stars[0] == null || stars[1] == null || stars[2] == null)
            {
                Transform starsRow = transform.Find("StarsRow");
                if (starsRow != null)
                {
                    var rowImages = starsRow.GetComponentsInChildren<Image>(true);
                    int idx = 0;
                    for (int i = 0; i < rowImages.Length && idx < 3; i++)
                    {
                        if (rowImages[i].transform == starsRow) continue;
                        stars[idx++] = rowImages[i];
                    }
                }
            }
        }
        

    


        EnsureVisibleBody();

        if (autoApplyRoundedStyle)
            ApplyCardStyle();

        EnsureLockSpriteIfMissing();
    }

    // ── Public API ───────────────────────────────────────────────────────────

    public void SetData(int levelNumber, bool unlocked, int starsEarned, bool selected)
    {
        int clampedStars = Mathf.Clamp(starsEarned, 0, 3);
        bool showLock = !unlocked || (showLockUntilCompleted && clampedStars <= 0);

        // Level number label
        if (levelNumberLabel != null)
        {
            levelNumberLabel.text    = levelNumber.ToString();
            levelNumberLabel.enabled = !showLock;
        }

        // Button interactability
        if (button != null)
            button.interactable = unlocked;

        // Lock icon
        if (lockIcon != null)
            lockIcon.SetActive(showLock);

        // Card background color
        if (background != null)
        {
            if      (showLock)  background.color = lockedColor;
            else if (selected)  background.color = selectedColor;
            else                background.color = unlockedColor;
        }

        // Stars
        if (stars != null)
        {
            for (int i = 0; i < stars.Length; i++)
            {
                if (stars[i] == null) continue;
                stars[i].color   = i < clampedStars ? starOnColor : starOffColor;
                stars[i].enabled = !showLock;
            }
        }

        // Pulse ring on current (selected) level
        RemovePulseRing();
        if (selected && !showLock)
            AddPulseRing();
    }

    // ── Pulse ring ───────────────────────────────────────────────────────────

    void AddPulseRing()
    {
        var ring = new GameObject("PulseRing");
        ring.transform.SetParent(transform, false);
        ring.transform.SetAsLastSibling();

        var rt = ring.AddComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = new Vector2(-6f, -6f);
        rt.offsetMax = new Vector2( 6f,  6f);

        var img = ring.AddComponent<Image>();
        img.sprite        = GetOrCreateRoundedSquareSprite();
        img.type          = Image.Type.Sliced;
        img.color         = new Color(0.96f, 0.62f, 0.07f, 0.7f);
        img.raycastTarget = false;

        ring.AddComponent<PulseRingAnimation>();
    }

    void RemovePulseRing()
    {
        var old = transform.Find("PulseRing");
        if (old != null) Destroy(old.gameObject);
    }

    // ── Card visual style ────────────────────────────────────────────────────

    void ApplyCardStyle()
    {
        if (styleApplied) return;

        // ── 3D raised slab (bottom dark strip, like a physical button) ──
        if (transform.Find("CardSlab") == null)
        {
            var slab    = new GameObject("CardSlab");
            slab.transform.SetParent(transform, false);
            slab.transform.SetAsFirstSibling();   // behind everything
            var slabRT  = slab.AddComponent<RectTransform>();
            slabRT.anchorMin = Vector2.zero;
            slabRT.anchorMax = Vector2.one;
            slabRT.offsetMin = new Vector2(0f, -6f);   // extends 6px below card
            slabRT.offsetMax = new Vector2(0f,  0f);
            var slabImg = slab.AddComponent<Image>();
            slabImg.sprite        = GetOrCreateRoundedSquareSprite();
            slabImg.type          = Image.Type.Sliced;
            slabImg.color         = new Color(0f, 0.06f, 0.14f, 0.55f);
            slabImg.raycastTarget = false;
        }

        // ── Inner border overlay (subtle white rim) ──
        if (transform.Find("CardBorderOverlay") == null)
        {
            var obj = new GameObject("CardBorderOverlay");
            obj.transform.SetParent(transform, false);
            obj.transform.SetAsLastSibling();
            var rt  = obj.AddComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = new Vector2( 2f,  2f);
            rt.offsetMax = new Vector2(-2f, -2f);
            var img = obj.AddComponent<Image>();
            img.sprite        = GetOrCreateRoundedSquareSprite();
            img.type          = Image.Type.Sliced;
            img.color         = new Color(1f, 1f, 1f, 0.18f);
            img.raycastTarget = false;
        }

        // ── Drop shadow via Unity Shadow component ──
        if (autoApplyRaisedShadow)
        {
            var shadow = GetComponent<Shadow>() ?? gameObject.AddComponent<Shadow>();
            shadow.effectColor     = new Color(0f, 0.06f, 0.18f, 0.55f);
            shadow.effectDistance  = new Vector2(2f, -6f);
            shadow.useGraphicAlpha = true;
        }

        styleApplied = true;
    }

    // ── Rounded square body ──────────────────────────────────────────────────

    void EnsureVisibleBody()
    {
        bool needsRuntimeBody = forceRoundedSquareBody || background == null;
        if (!needsRuntimeBody && background.sprite != null)
        {
            string n = background.sprite.name;
            if (n == "UISprite" || n.Contains("Capsule"))
                needsRuntimeBody = true;
        }

        if (!needsRuntimeBody) return;

        Transform bodyT = transform.Find("CardBodyRuntime");
        if (bodyT == null)
        {
            var bodyObj = new GameObject("CardBodyRuntime");
            bodyObj.transform.SetParent(transform, false);
            bodyObj.transform.SetAsFirstSibling();
            var bodyRect = bodyObj.AddComponent<RectTransform>();
            bodyRect.anchorMin = Vector2.zero;
            bodyRect.anchorMax = Vector2.one;
            bodyRect.offsetMin = Vector2.zero;
            bodyRect.offsetMax = Vector2.zero;
            runtimeBody = bodyObj.AddComponent<Image>();
        }
        else
        {
            runtimeBody = bodyT.GetComponent<Image>() ?? bodyT.gameObject.AddComponent<Image>();
        }

        if (runtimeBody != null)
        {
            // Priority: Inspector field → Editor path → generated sprite
            Sprite bodySprite = roundedSquareSprite;

#if UNITY_EDITOR
            if (bodySprite == null)
                bodySprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/rounded_square.png");
#endif
            if (bodySprite == null)
                bodySprite = GetOrCreateRoundedSquareSprite();

            runtimeBody.sprite                = bodySprite;
            runtimeBody.type                  = Image.Type.Sliced;
            runtimeBody.pixelsPerUnitMultiplier = 1f;
            runtimeBody.raycastTarget         = true;
        }

        if (button != null && runtimeBody != null)
            button.targetGraphic = runtimeBody;

        if (background != null && background != runtimeBody)
        {
            background.color         = new Color(background.color.r, background.color.g, background.color.b, 0f);
            background.raycastTarget = false;
        }

        if (runtimeBody != null)
            background = runtimeBody;
    }

    // ── Lock icon ────────────────────────────────────────────────────────────

    void EnsureLockSpriteIfMissing()
    {
        if (lockIcon == null) return;

        // Centre the lock icon on the card
        var lockRect = lockIcon.GetComponent<RectTransform>();
        if (lockRect != null)
        {
            lockRect.anchorMin        = new Vector2(0.5f, 0.5f);
            lockRect.anchorMax        = new Vector2(0.5f, 0.5f);
            lockRect.pivot            = new Vector2(0.5f, 0.5f);
            lockRect.anchoredPosition = Vector2.zero;
            lockRect.sizeDelta        = new Vector2(44f, 44f);
        }

        var lockImage = lockIcon.GetComponent<Image>();
        if (lockImage == null) return;
        if (lockImage.sprite != null) return;

#if UNITY_EDITOR
        var lockSprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/lock.png");
        if (lockSprite != null)
        {
            lockImage.sprite         = lockSprite;
            lockImage.preserveAspect = true;
            lockImage.type           = Image.Type.Simple;
            lockImage.color          = new Color(0.85f, 0.88f, 0.94f, 0.95f);
            return;
        }
#endif
        // Fallback: white square placeholder so the lock is at least visible
        lockImage.sprite         = GetOrCreateRoundedSquareSprite();
        lockImage.type           = Image.Type.Sliced;
        lockImage.preserveAspect = true;
        lockImage.color          = new Color(0.78f, 0.82f, 0.9f, 0.90f);
    }

    // ── Rounded square sprite generator ─────────────────────────────────────
    // Creates a 128×128 white rounded-rectangle texture once and caches it.
    // The 9-slice border equals the corner radius so it scales cleanly.

    static Sprite GetOrCreateRoundedSquareSprite()
    {
        if (s_RoundedSquare != null) return s_RoundedSquare;

        const int size   = 128;
        const int radius = 28; // corner radius in pixels — tweak for rounder/squarer look

        var tex    = new Texture2D(size, size, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Bilinear;
        var pixels = new Color32[size * size];

        for (int y = 0; y < size; y++)
        for (int x = 0; x < size; x++)
        {
            // Clamp to nearest corner centre
            int cx = (x < radius) ? radius : (x >= size - radius ? size - 1 - radius : x);
            int cy = (y < radius) ? radius : (y >= size - radius ? size - 1 - radius : y);
            float dx = x - cx, dy = y - cy;
            bool inside = (dx * dx + dy * dy) <= ((float)radius * radius);
            pixels[y * size + x] = inside
                ? new Color32(255, 255, 255, 255)
                : new Color32(0,   0,   0,   0);
        }

        tex.SetPixels32(pixels);
        tex.Apply();

        s_RoundedSquare = Sprite.Create(
            tex,
            new Rect(0, 0, size, size),
            new Vector2(0.5f, 0.5f),
            100f,
            0,
            SpriteMeshType.FullRect,
            new Vector4(radius, radius, radius, radius) // 9-slice borders
        );

        return s_RoundedSquare;
    }
}