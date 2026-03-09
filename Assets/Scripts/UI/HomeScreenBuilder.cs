// Assets/Scripts/UI/HomeScreenBuilder.cs
// Builds the entire Boat Jam home screen in code, pixel-perfect to the HTML mockup.
//
// SETUP (one-time):
//   1. Attach this script to your Canvas GameObject.
//   2. Hit Play — the home panel is built automatically.
//      OR right-click the component → "Build Home Screen" to build in Editor.
//   3. MainMenuManager references are wired automatically if found in the scene.

using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class HomeScreenBuilder : MonoBehaviour
{
    // ── Sprite loading ────────────────────────────────────────────────────────
    // Put your PNGs in Assets/Resources/Icons/ with these exact names:
    //   anchor.png, levels.png, settings.png, credits.png, boat.png
    // Set each PNG's Texture Type to "Sprite (2D and UI)" in the Inspector.

    static Sprite LoadIcon(string name)
        => Resources.Load<Sprite>($"Icons/{name}");

    // ── Layout constants ──────────────────────────────────────────────────────
    const float SIDE_PAD    = 32f;
    const float TOP_PAD     = 60f;
    const float BOTTOM_PAD  = 48f;
    const float LOGO_H      = 230f;
    const float BTN_PLAY_H  = 68f;
    const float BTN_SEC_H   = 60f;
    const float BTN_ROW_H   = 54f;
    const float BTN_GAP     = 14f;
    const float VERSION_H   = 20f;
    const float VERSION_PAD = 10f;

    // ── Colors ────────────────────────────────────────────────────────────────
    static Color SKY_TOP    = Hex("0ea5e9");
    static Color SKY_MID    = Hex("0284c7");
    static Color SKY_LOW    = Hex("0369a1");
    static Color SEA_MID    = Hex("1e3a5f");
    static Color SEA_BOT    = Hex("0f2239");
    static Color ORANGE     = new Color(0.96f, 0.62f, 0.07f, 1f);
    static Color ORANGE_SHD = new Color(0.706f, 0.325f, 0.035f, 1f);
    static Color GLASS_BRD  = new Color(1f, 1f, 1f, 0.25f);
    static Color GLASS_FILL = new Color(1f, 1f, 1f, 0.10f);
    static Color CLOUD_CLR  = new Color(1f, 1f, 1f, 0.18f);
    static Color GLINT_CLR  = new Color(1f, 0.863f, 0.314f, 0.6f);
    static Color STAR_CLR   = new Color(1f, 0.82f, 0.10f, 1f);

    // ─────────────────────────────────────────────────────────────────────────

    void Start() => Build();

    [ContextMenu("Build Home Screen")]
    public void Build()
    {
        var old = transform.Find("HomePanel");
        if (old != null) DestroyImmediate(old.gameObject);

        var panel = NewGO("HomePanel", transform);
        Stretch(panel);

        BuildBackground(panel.transform);
        BuildContent(panel.transform);
        ApplySprites(panel.transform);
        WireMainMenuManager(panel);
    }

    // ── Background ────────────────────────────────────────────────────────────

    void BuildBackground(Transform root)
    {
        var bg = NewGO("Background", root);
        Stretch(bg);

        // Sky-to-sea gradient via stacked layers
        Layer("Sky_Top", bg.transform, 0.55f, 1.0f,  SKY_TOP);
        Layer("Sky_Mid", bg.transform, 0.30f, 0.65f, SKY_MID);
        Layer("Sky_Low", bg.transform, 0.10f, 0.45f, SKY_LOW);
        Layer("Sea_Mid", bg.transform, 0.00f, 0.25f, SEA_MID);
        Layer("Sea_Bot", bg.transform, 0.00f, 0.12f, SEA_BOT);

        // Clouds
        MakeCloud(bg.transform, "Cloud1", 140f, 40f, -200f, 0.88f, 22f,  0f);
        MakeCloud(bg.transform, "Cloud2",  90f, 28f, -150f, 0.82f, 30f, -8f);
        MakeCloud(bg.transform, "Cloud3", 110f, 35f,  100f, 0.90f, 26f,-14f);

        // Waves
        MakeWave(bg.transform, "Wave1", 0.47f,  80f, new Color(0.22f,0.74f,0.98f,0.25f),  4f,   0f);
        MakeWave(bg.transform, "Wave2", 0.43f, 100f, new Color(0.055f,0.647f,0.914f,0.25f),5f,  -1f);
        MakeWave(bg.transform, "Wave3", 0.41f,  60f, new Color(0.49f,0.83f,0.99f,0.25f),  3.5f, -2f);

        // Glint
        var g  = NewGO("Glint", bg.transform);
        var gr = g.GetComponent<RectTransform>();
        gr.anchorMin = new Vector2(0.5f, 0.42f); gr.anchorMax = new Vector2(0.5f, 0.42f);
        gr.pivot     = new Vector2(0.5f, 0.5f);  gr.sizeDelta = new Vector2(200f, 6f);
        var gi = g.AddComponent<Image>(); gi.color = GLINT_CLR; Round(gi);
        g.AddComponent<GlintPulse>();

        // Deco boats
        MakeDecoBoat(bg.transform, "DecoBoat1", "⛵", 28f, new Vector2(0.08f, 0.57f), 4f,  0f);
        MakeDecoBoat(bg.transform, "DecoBoat2", "🚤", 20f, new Vector2(0.88f, 0.52f), 5f, -2f);
    }

    void Layer(string name, Transform parent, float minY, float maxY, Color color)
    {
        var go = NewGO(name, parent);
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0f, minY); rt.anchorMax = new Vector2(1f, maxY);
        rt.offsetMin = rt.offsetMax = Vector2.zero;
        go.AddComponent<Image>().color = color;
    }

    void MakeCloud(Transform parent, string name, float w, float h,
                   float startX, float anchorY, float dur, float delay)
    {
        var go = NewGO(name, parent);
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0f, anchorY); rt.anchorMax = new Vector2(0f, anchorY);
        rt.pivot     = new Vector2(0f, 0.5f);    rt.sizeDelta = new Vector2(w, h);
        rt.anchoredPosition = new Vector2(startX, 0f);
        var img = go.AddComponent<Image>(); img.color = CLOUD_CLR; Round(img);
        var d = go.AddComponent<CloudDrifter>(); d.duration = dur; d.delay = delay;
        d.startX = startX; d.endX = 520f;
    }

    void MakeWave(Transform parent, string name, float anchorY, float height,
                  Color color, float dur, float delay)
    {
        var go = NewGO(name, parent);
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(-0.1f, anchorY); rt.anchorMax = new Vector2(1.1f, anchorY);
        rt.pivot     = new Vector2(0.5f, 0.5f);     rt.sizeDelta = new Vector2(0f, height);
        var img = go.AddComponent<Image>(); img.color = color; Round(img);
        var w = go.AddComponent<WaveRocker>(); w.duration = dur; w.delay = delay;
    }

    void MakeDecoBoat(Transform parent, string name, string emoji, float fontSize,
                      Vector2 anchor, float dur, float delay)
    {
        var go = NewGO(name, parent);
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = anchor; rt.anchorMax = anchor;
        rt.pivot = new Vector2(0.5f, 0.5f); rt.sizeDelta = new Vector2(50f, 50f);
        var tmp = go.AddComponent<TextMeshProUGUI>();
        tmp.text = emoji; tmp.fontSize = fontSize;
        tmp.color = CLOUD_CLR; tmp.alignment = TextAlignmentOptions.Center;
        var bob = go.AddComponent<Bobber>();
        bob.amplitude = 12f; bob.tiltDeg = 5f; bob.duration = dur; bob.delay = delay;
    }

    // ── Content ───────────────────────────────────────────────────────────────

    void BuildContent(Transform root)
    {
        var c = NewGO("Content", root);
        Stretch(c);
        var vl = c.AddComponent<VerticalLayoutGroup>();
        vl.childAlignment       = TextAnchor.UpperCenter;
        vl.spacing              = 0f;
        vl.padding              = new RectOffset(
            Mathf.RoundToInt(SIDE_PAD), Mathf.RoundToInt(SIDE_PAD),
            Mathf.RoundToInt(TOP_PAD),  Mathf.RoundToInt(BOTTOM_PAD));
        vl.childControlWidth      = true;
        vl.childControlHeight     = false;
        vl.childForceExpandWidth  = true;
        vl.childForceExpandHeight = false;

        BuildLogo(c.transform);
        Spacer(c.transform);
        BuildButtons(c.transform);
        BuildVersion(c.transform);
    }

    // ── Logo ──────────────────────────────────────────────────────────────────

    void BuildLogo(Transform parent)
    {
        var area = NewGO("LogoArea", parent); LE(area, LOGO_H);
        var vl = area.AddComponent<VerticalLayoutGroup>();
        vl.childAlignment = TextAnchor.UpperCenter; vl.spacing = 6f;
        vl.childControlWidth = true; vl.childControlHeight = false;
        vl.childForceExpandWidth = true; vl.childForceExpandHeight = false;

        // Boat icon — uses sprite from Resources/Icons/boat, falls back to placeholder
        var boatGO = NewGO("BoatIcon", area.transform); LE(boatGO, 88f);
        var boatImg = boatGO.AddComponent<Image>();
        boatImg.color = Color.white;
        boatImg.preserveAspect = true;
        var bob = boatGO.AddComponent<Bobber>(); bob.amplitude = 8f; bob.tiltDeg = 3f; bob.duration = 3f;

        // BOAT JAM title
        var titleGO = NewGO("Title", area.transform); LE(titleGO, 64f);
        var titleTMP = titleGO.AddComponent<TextMeshProUGUI>();
        titleTMP.text = "BOAT JAM"; titleTMP.fontSize = 82f;
        titleTMP.fontStyle = FontStyles.Bold; titleTMP.color = Color.white;
        titleTMP.alignment = TextAlignmentOptions.Center;
        titleTMP.outlineWidth = 0.25f; titleTMP.outlineColor = new Color32(3, 105, 161, 255);

        // Subtitle
        var subGO = NewGO("Subtitle", area.transform); LE(subGO, 22f);
        var subTMP = subGO.AddComponent<TextMeshProUGUI>();
        subTMP.text = "HARBOR ESCAPE"; subTMP.fontSize = 29f;
        subTMP.fontStyle = FontStyles.Bold; subTMP.color = new Color(1f,1f,1f,0.65f);
        subTMP.alignment = TextAlignmentOptions.Center; subTMP.characterSpacing = 4f;

        // Stars
        var starsGO = NewGO("Stars", area.transform); LE(starsGO, 28f);
        var hl = starsGO.AddComponent<HorizontalLayoutGroup>();
        hl.childAlignment = TextAnchor.MiddleCenter; hl.spacing = 4f;
        hl.childControlWidth = false; hl.childControlHeight = false;
        hl.childForceExpandWidth = false; hl.childForceExpandHeight = false;
        for (int i = 0; i < 5; i++)
        {
            var s = NewGO($"Star{i}", starsGO.transform);
            s.GetComponent<RectTransform>().sizeDelta = new Vector2(22f, 22f);
            var st = s.AddComponent<TextMeshProUGUI>();
            st.text = "★"; st.fontSize = 18f; st.color = STAR_CLR;
            st.alignment = TextAlignmentOptions.Center;
        }
    }

    // ── Buttons ───────────────────────────────────────────────────────────────

    void BuildButtons(Transform parent)
    {
        float totalH = BTN_PLAY_H + BTN_GAP + BTN_SEC_H + BTN_GAP + BTN_ROW_H;
        var area = NewGO("Buttons", parent); LE(area, totalH);
        var vl = area.AddComponent<VerticalLayoutGroup>();
        vl.childAlignment = TextAnchor.UpperCenter; vl.spacing = BTN_GAP;
        vl.childControlWidth = true; vl.childControlHeight = false;
        vl.childForceExpandWidth = true; vl.childForceExpandHeight = false;

        // PLAY
        var playGO = NewGO("PlayButton", area.transform); LE(playGO, BTN_PLAY_H);
        var playImg = playGO.AddComponent<Image>(); playImg.color = ORANGE; Round(playImg);

        var shadowGO = NewGO("Shadow", playGO.transform);
        var shadowRT = shadowGO.GetComponent<RectTransform>();
        shadowRT.anchorMin = new Vector2(0f,0f); shadowRT.anchorMax = new Vector2(1f,0f);
        shadowRT.pivot = new Vector2(0.5f,1f);
        shadowRT.offsetMin = new Vector2(4f,-8f); shadowRT.offsetMax = new Vector2(-4f,0f);
        var shadowImg = shadowGO.AddComponent<Image>(); shadowImg.color = ORANGE_SHD;
        shadowImg.raycastTarget = false; Round(shadowImg);

        var playBtn = playGO.AddComponent<Button>(); playBtn.targetGraphic = playImg;
        TintBtn(playBtn, new Color(1f,0.72f,0.17f,1f), new Color(0.86f,0.52f,0.02f,1f));

        var playLbl = Label("Label", playGO.transform, "PLAY", 38f, FontStyles.Bold, Color.white);
        StretchFill(playLbl); playLbl.GetComponent<TextMeshProUGUI>().raycastTarget = false;
        // Anchor icon to the left of PLAY text — small white square as placeholder,
        // replace playIconImg.sprite with your anchor sprite in the Inspector if desired
        AddBtnIcon(playGO.transform, "AnchorIcon", -80f, 42f);

        // LEVELS
        SecondaryBtn(area.transform, "LevelsButton", "LEVELS", BTN_SEC_H, 38f);
        AddBtnIcon(area.transform.Find("LevelsButton"), "LevelsIcon", -110f, 52f);

        // Settings + Credits row
        var rowGO = NewGO("BottomRow", area.transform); LE(rowGO, BTN_ROW_H);
        var rowHL = rowGO.AddComponent<HorizontalLayoutGroup>();
        rowHL.spacing = BTN_GAP; rowHL.childAlignment = TextAnchor.MiddleCenter;
        rowHL.childControlWidth = true; rowHL.childControlHeight = true;
        rowHL.childForceExpandWidth = true; rowHL.childForceExpandHeight = true;
        SecondaryBtn(rowGO.transform, "SettingsButton", "Settings", BTN_ROW_H, 32f);
        AddBtnIcon(rowGO.transform.Find("SettingsButton"), "SettingsIcon", -92f, 52f);
        SecondaryBtn(rowGO.transform, "CreditsButton",  "Credits",  BTN_ROW_H, 32f);
        AddBtnIcon(rowGO.transform.Find("CreditsButton"), "CreditsIcon", -88f, 52f);
    }

    void SecondaryBtn(Transform parent, string name, string text, float height, float fontSize)
    {
        var go = NewGO(name, parent); LE(go, height);

        // Outer border layer
        var borderImg = go.AddComponent<Image>(); borderImg.color = GLASS_BRD; Round(borderImg);

        // Inner fill (inset 2px)
        var fill = NewGO("Fill", go.transform);
        var fillRT = fill.GetComponent<RectTransform>();
        fillRT.anchorMin = Vector2.zero; fillRT.anchorMax = Vector2.one;
        fillRT.offsetMin = new Vector2(2f,2f); fillRT.offsetMax = new Vector2(-2f,-2f);
        var fillImg = fill.AddComponent<Image>(); fillImg.color = GLASS_FILL;
        fillImg.raycastTarget = false; Round(fillImg);

        var btn = go.AddComponent<Button>(); btn.targetGraphic = borderImg;
        TintBtn(btn, new Color(1f,1f,1f,0.32f), new Color(1f,1f,1f,0.12f));

        var lbl = Label("Label", go.transform, text, fontSize, FontStyles.Bold, Color.white);
        StretchFill(lbl); lbl.GetComponent<TextMeshProUGUI>().raycastTarget = false;
    }

    void BuildVersion(Transform parent)
    {
        var sp = NewGO("VersionSpacer", parent);
        sp.AddComponent<LayoutElement>().preferredHeight = VERSION_PAD;
        var go = NewGO("Version", parent); LE(go, VERSION_H);
        var tmp = go.AddComponent<TextMeshProUGUI>();
        tmp.text = $"v{Application.version}"; tmp.fontSize = 11f;
        tmp.color = new Color(1f,1f,1f,0.3f); tmp.alignment = TextAlignmentOptions.Center;
        tmp.characterSpacing = 2f;
    }

    // ── Apply sprites to icon slots ───────────────────────────────────────────

    void ApplySprites(Transform panel)
    {
        SetIcon(panel, "Content/Buttons/PlayButton/AnchorIcon",           LoadIcon("anchor"));
        SetIcon(panel, "Content/Buttons/LevelsButton/LevelsIcon",         LoadIcon("levels"));
        SetIcon(panel, "Content/Buttons/BottomRow/SettingsButton/SettingsIcon", LoadIcon("settings"));
        SetIcon(panel, "Content/Buttons/BottomRow/CreditsButton/CreditsIcon",   LoadIcon("credits"));
        SetIcon(panel, "Content/LogoArea/BoatIcon",                       LoadIcon("boat"));
    }

    static void SetIcon(Transform panel, string path, Sprite sprite)
    {
        if (sprite == null) return;
        var t = panel.Find(path);
        if (t == null) return;
        var img = t.GetComponent<Image>();
        if (img != null) { img.sprite = sprite; img.preserveAspect = true; }
    }

    // ── Wire MainMenuManager ──────────────────────────────────────────────────

    void WireMainMenuManager(GameObject panel)
    {
        var mm = FindObjectOfType<MainMenuManager>();
        if (mm == null) return;
        mm.homePanel = panel;

        Button Find(string path) => panel.transform.Find(path)?.GetComponent<Button>();
        mm.playButton        = Find("Content/Buttons/PlayButton");
        mm.levelSelectButton = Find("Content/Buttons/LevelsButton");
        mm.settingsButton    = Find("Content/Buttons/BottomRow/SettingsButton");
        mm.creditsButton     = Find("Content/Buttons/BottomRow/CreditsButton");
        mm.RebindButtonListeners();

        Debug.Log("HomeScreenBuilder: Wired MainMenuManager.");
    }

    // ── Static helpers ────────────────────────────────────────────────────────

    static GameObject NewGO(string name, Transform parent)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.AddComponent<RectTransform>();
        return go;
    }

    static void Stretch(GameObject go)
    {
        var rt = go.GetComponent<RectTransform>() ?? go.AddComponent<RectTransform>();
        rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
        rt.offsetMin = rt.offsetMax = Vector2.zero;
    }

    static void LE(GameObject go, float h)
    {
        var le = go.GetComponent<LayoutElement>() ?? go.AddComponent<LayoutElement>();
        le.preferredHeight = h;
    }

    static void Spacer(Transform parent)
    {
        var s = new GameObject("Spacer");
        s.transform.SetParent(parent, false);
        s.AddComponent<RectTransform>();
        s.AddComponent<LayoutElement>().flexibleHeight = 1f;
    }

    // ── Button icon helper ────────────────────────────────────────────────────
    // Adds a small Image to the left of centre inside a button.
    // offsetX nudges it left of centre; size controls width+height.
    // Assign a sprite to the returned Image, or it stays as a white shape.
    static Image AddBtnIcon(Transform btnTransform, string name, float offsetX, float size)
    {
        if (btnTransform == null) return null;
        var go = NewGO(name, btnTransform);
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot     = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = new Vector2(offsetX, 0f);
        rt.sizeDelta = new Vector2(size, size);
        var img = go.AddComponent<Image>();
        img.color         = Color.white;
        img.raycastTarget = false;
        img.preserveAspect = true;
        return img;
    }

    // ── Rounded sprite (generated at runtime, cached) ─────────────────────────

    static Sprite _roundedSprite;

    static Sprite GetRoundedSprite()
    {
        if (_roundedSprite != null) return _roundedSprite;

        const int size = 128, radius = 24, border = 8;
        var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        var pixels = new Color32[size * size];

        for (int y = 0; y < size; y++)
        for (int x = 0; x < size; x++)
        {
            int cx = Mathf.Clamp(x, radius, size - radius - 1);
            int cy = Mathf.Clamp(y, radius, size - radius - 1);
            float dist = Mathf.Sqrt((x - cx) * (x - cx) + (y - cy) * (y - cy));
            byte a = (byte)(dist <= radius ? 255 : 0);
            pixels[y * size + x] = new Color32(255, 255, 255, a);
        }

        tex.SetPixels32(pixels);
        tex.Apply();

        _roundedSprite = Sprite.Create(tex,
            new Rect(0, 0, size, size),
            new Vector2(0.5f, 0.5f),
            100f,
            0,
            SpriteMeshType.FullRect,
            new Vector4(border, border, border, border));  // 9-slice border

        return _roundedSprite;
    }

    static void Round(Image img)
    {
        img.sprite = GetRoundedSprite();
        img.type   = Image.Type.Sliced;
    }

    static GameObject Label(string name, Transform parent, string text,
        float size, FontStyles style, Color color)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.AddComponent<RectTransform>();
        var tmp = go.AddComponent<TextMeshProUGUI>();
        tmp.text = text; tmp.fontSize = size; tmp.fontStyle = style;
        tmp.color = color; tmp.alignment = TextAlignmentOptions.Center;
        tmp.overflowMode = TextOverflowModes.Overflow;
        return go;
    }

    static void StretchFill(GameObject go)
    {
        var rt = go.GetComponent<RectTransform>(); if (!rt) return;
        rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
        rt.offsetMin = rt.offsetMax = Vector2.zero;
    }

    static void TintBtn(Button btn, Color highlight, Color pressed)
    {
        var c = btn.colors; c.highlightedColor = highlight; c.pressedColor = pressed; btn.colors = c;
    }

    static Color Hex(string hex)
    {
        ColorUtility.TryParseHtmlString("#" + hex, out var c); return c;
    }
}

// ── Animation components ──────────────────────────────────────────────────────

/// Drifts a cloud from startX to endX and loops.
public class CloudDrifter : MonoBehaviour
{
    public float duration = 22f, delay = 0f, startX = -200f, endX = 520f;
    RectTransform rt; float t;
    void Awake()  { rt = GetComponent<RectTransform>(); t = delay > 0 ? -delay : 0f; }
    void Update() {
        t += Time.deltaTime; if (t < 0f) return;
        var p = rt.anchoredPosition; p.x = Mathf.Lerp(startX, endX, (t % duration) / duration);
        rt.anchoredPosition = p;
    }
}

/// Rocks left-right via sine.
public class WaveRocker : MonoBehaviour
{
    public float duration = 4f, delay = 0f, amount = 28f;
    RectTransform rt; float t, baseX;
    void Awake()  { rt = GetComponent<RectTransform>(); baseX = rt.anchoredPosition.x; t = delay > 0 ? -delay : 0f; }
    void Update() {
        t += Time.deltaTime; if (t < 0f) return;
        var p = rt.anchoredPosition; p.x = baseX + Mathf.Sin((t / duration) * Mathf.PI * 2f) * amount;
        rt.anchoredPosition = p;
    }
}

/// Bobs up-down with tilt.
public class Bobber : MonoBehaviour
{
    public float amplitude = 8f, tiltDeg = 3f, duration = 3f, delay = 0f;
    RectTransform rt; float t, baseY;
    void Awake()  { rt = GetComponent<RectTransform>(); baseY = rt.anchoredPosition.y; t = delay > 0 ? -delay : 0f; }
    void Update() {
        t += Time.deltaTime; if (t < 0f) return;
        float s = Mathf.Sin((t / duration) * Mathf.PI * 2f);
        var p = rt.anchoredPosition; p.y = baseY + s * amplitude; rt.anchoredPosition = p;
        rt.localRotation = Quaternion.Euler(0f, 0f, s * tiltDeg);
    }
}

/// Pulses the sun glint in width and alpha.
public class GlintPulse : MonoBehaviour
{
    public float duration = 3f;
    Image img; RectTransform rt; float t;
    void Awake() { img = GetComponent<Image>(); rt = GetComponent<RectTransform>(); }
    void Update() {
        t += Time.deltaTime;
        float p = Mathf.PingPong(t / duration, 1f);
        var c = img.color; img.color = new Color(c.r, c.g, c.b, Mathf.Lerp(0.4f, 0.9f, p));
        rt.sizeDelta = new Vector2(Mathf.Lerp(160f, 240f, p), rt.sizeDelta.y);
    }
}