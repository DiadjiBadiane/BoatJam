// Assets/Scripts/UI/SettingsScreenBuilder.cs
//
// Builds the Boat Jam Settings screen entirely in code.
// Mirrors the style of HomeScreenBuilder (same colors, rounded cards, glassmorphism rows).
//
// ── SETUP ────────────────────────────────────────────────────────────────────
//   1. Attach this script to the same Canvas GameObject as HomeScreenBuilder.
//   2. The panel is built automatically on Start, or right-click → "Build Settings Screen".
//   3. MainMenuManager will wire the back button automatically.
//
// ── REQUIRED ICONS (place in Assets/Resources/Icons/) ────────────────────────
//   music.png          – musical note
//   sfx.png            – speaker / sound wave
//   vibration.png      – phone vibrating
//   language.png       – globe
//   graphics.png       – lightning bolt / diamond
//   hints.png          – lightbulb
//   timer.png          – stopwatch
//   leaderboard.png    – trophy / podium
//   reset.png          – trash / refresh
//   back.png           – left arrow / chevron
//
//   All PNGs: Texture Type = "Sprite (2D and UI)", white-on-transparent so the
//   tint color (set in code) controls their appearance.
// ─────────────────────────────────────────────────────────────────────────────

using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class SettingsScreenBuilder : MonoBehaviour
{
    // ── Sprite loader (same Resources/Icons/ folder as HomeScreenBuilder) ─────
    static Sprite Icon(string name) => Resources.Load<Sprite>($"Icons/{name}");

    // ── Layout ────────────────────────────────────────────────────────────────
    const float SIDE_PAD   = 28f;
    const float TOP_PAD    = 56f;   // room for status bar / notch
    const float HDR_H      = 56f;   // header bar height
    const float SEC_LBL_H  = 28f;   // section label height
    const float SEC_GAP    = 12f;   // gap above a section label
    const float ROW_H      = 68f;   // each settings row
    const float CARD_RADIUS = 22f;
    const float ICON_BOX   = 36f;   // the coloured icon bubble
    const float ICON_IMG   = 22f;   // icon image inside the bubble
    const float SLIDER_H   = 6f;
    const float TOGGLE_W   = 50f;
    const float TOGGLE_H   = 28f;
    const float CHIP_W     = 52f;
    const float CHIP_H     = 30f;
    const float CHIP_GAP   = 6f;
    const float RESET_H    = 56f;
    const float VERSION_H  = 20f;
    const float BTM_PAD    = 36f;

    // ── Colors (identical to HomeScreenBuilder) ───────────────────────────────
    static readonly Color SKY_TOP    = Hex("0ea5e9");
    static readonly Color SKY_MID    = Hex("0284c7");
    static readonly Color SEA_MID    = Hex("1e3a5f");
    static readonly Color ORANGE     = new Color(0.96f, 0.62f, 0.07f, 1f);
    static readonly Color ORANGE_DRK = new Color(0.85f, 0.45f, 0.02f, 1f);
    static readonly Color GLASS_BRD  = new Color(1f, 1f, 1f, 0.22f);
    static readonly Color GLASS_FILL = new Color(1f, 1f, 1f, 0.10f);
    static readonly Color GLASS_ROW  = new Color(1f, 1f, 1f, 0.07f);
    static readonly Color CLOUD_CLR  = new Color(1f, 1f, 1f, 0.18f);
    static readonly Color GLINT_CLR  = new Color(1f, 0.863f, 0.314f, 0.6f);
    static readonly Color WHITE      = Color.white;
    static readonly Color WHITE65    = new Color(1f, 1f, 1f, 0.65f);
    static readonly Color WHITE40    = new Color(1f, 1f, 1f, 0.40f);
    static readonly Color WHITE25    = new Color(1f, 1f, 1f, 0.25f);
    static readonly Color WHITE12    = new Color(1f, 1f, 1f, 0.12f);
    static readonly Color RED_BORDER = new Color(0.94f, 0.27f, 0.27f, 0.50f);
    static readonly Color RED_FILL   = new Color(0.94f, 0.27f, 0.27f, 0.12f);
    static readonly Color RED_TEXT   = new Color(1f, 0.64f, 0.64f, 1f);

    // Icon bubble tints — each section gets its own accent colour
    static readonly Color TINT_SOUND   = Hex("0ea5e9");   // sky blue
    static readonly Color TINT_DISPLAY = Hex("6366f1");   // indigo
    static readonly Color TINT_GAME    = Hex("f59e0b");   // orange (matches play button)

    // ── Exposed references wired by Build() ──────────────────────────────────
    [HideInInspector] public Slider musicSlider;
    [HideInInspector] public Slider sfxSlider;
    [HideInInspector] public Toggle vibrationToggle;
    [HideInInspector] public Toggle hintsToggle;
    [HideInInspector] public Toggle timerToggle;
    [HideInInspector] public Button backButton;
    [HideInInspector] public Button leaderboardButton;
    [HideInInspector] public Button resetButton;

    float _uiScale = 1f;

    float S(float v) => v * _uiScale;
    int SI(float v) => Mathf.RoundToInt(S(v));

    // ─────────────────────────────────────────────────────────────────────────

    void Start() => WireToManager();

    public void WireToManager()
    {
        var mm = FindObjectOfType<MainMenuManager>();
        if (mm == null) { Debug.LogWarning("[SettingsScreenBuilder] MainMenuManager not found."); return; }
        var panel = transform.Find("SettingsPanel")?.gameObject;
        if (panel == null) { Debug.LogWarning("[SettingsScreenBuilder] SettingsPanel not found."); return; }
        mm.settingsPanel       = panel;
        mm.musicSlider         = musicSlider;
        mm.sfxSlider           = sfxSlider;
        mm.vibrationToggle     = vibrationToggle;
        mm.settingsCloseButton = backButton;
        mm.RebindButtonListeners();
        Debug.Log("[SettingsScreenBuilder] Wired to MainMenuManager.");
    }

    public GameObject EnsureBuilt()
    {
        var panel = transform.Find("SettingsPanel")?.gameObject;
        bool needsBuild = panel == null || panel.transform.Find("ScrollView/Viewport/Content") == null;
        if (!needsBuild)
            return panel;

        Build();
        WireToManager();
        return transform.Find("SettingsPanel")?.gameObject;
    }

    [ContextMenu("Build Settings Screen")]
    public void Build()
    {
        _uiScale = Screen.width > Screen.height ? 1.45f : 1f;

        var old = transform.Find("SettingsPanel");
        if (old != null)
        {
            if (Application.isPlaying)
                Destroy(old.gameObject);
            else
                DestroyImmediate(old.gameObject);
        }

        var panel = MakeGO("SettingsPanel", transform);
        Stretch(panel);
        panel.SetActive(false); // hidden until MainMenuManager shows it

        BuildBackground(panel.transform);
        BuildScrollContent(panel.transform);
    }

    // ── Background (mirrors HomeScreenBuilder.BuildBackground) ───────────────

    void BuildBackground(Transform root)
    {
        var bg = MakeGO("Background", root);
        Stretch(bg);

        // Same uniform sky + subtle sea at the bottom
        Layer("Sky",     bg.transform, 0.18f, 1.0f,  SKY_TOP);
        Layer("Sea_Mid", bg.transform, 0.00f, 0.26f, SKY_MID);
        Layer("Sea_Bot", bg.transform, 0.00f, 0.09f, SEA_MID);

        // Drifting clouds (top area, same as home)
        MakeCloud(bg.transform, "Cloud1", 120f, 34f, -200f, 0.90f, 24f,  0f);
        MakeCloud(bg.transform, "Cloud2",  80f, 24f, -150f, 0.84f, 32f, -10f);
        MakeCloud(bg.transform, "Cloud3", 100f, 30f,  80f,  0.92f, 28f, -16f);

        // Subtle waves at the sea/sky boundary
        MakeWave(bg.transform, "Wave1", 0.24f, 60f, new Color(0.22f, 0.74f, 0.98f, 0.18f), 4f,  0f);
        MakeWave(bg.transform, "Wave2", 0.21f, 80f, new Color(0.05f, 0.65f, 0.91f, 0.18f), 5f, -1f);

        // Glint
        var g  = MakeGO("Glint", bg.transform);
        var gr = g.GetComponent<RectTransform>();
        gr.anchorMin = new Vector2(0.5f, 0.22f); gr.anchorMax = new Vector2(0.5f, 0.22f);
        gr.pivot     = new Vector2(0.5f, 0.5f);  gr.sizeDelta = new Vector2(S(180f), S(6f));
        var gi = g.AddComponent<Image>(); gi.color = GLINT_CLR; Rounded(gi);
        g.AddComponent<GlintPulse>();

        // Tiny deco boats at the bottom
        MakeDecoBoat(bg.transform, "Deco1", "⛵", S(24f), new Vector2(0.07f, 0.12f), 4f,  0f);
        MakeDecoBoat(bg.transform, "Deco2", "🚤", S(18f), new Vector2(0.88f, 0.09f), 5f, -2f);
    }

    // ── Scrollable content ────────────────────────────────────────────────────

    void BuildScrollContent(Transform root)
    {
        // Full-screen scroll view so the page scrolls on small phones
        var sv    = MakeGO("ScrollView", root);
        Stretch(sv);
        var svImg = sv.AddComponent<Image>();
        svImg.color = Color.clear;
        var scroll = sv.AddComponent<ScrollRect>();
        scroll.horizontal = false;
        scroll.scrollSensitivity = 30f;
        scroll.movementType = ScrollRect.MovementType.Clamped;

        // Viewport
        var vp    = MakeGO("Viewport", sv.transform);
        Stretch(vp);
        var vpImg  = vp.AddComponent<Image>(); vpImg.color = new Color(1f, 1f, 1f, 0.001f);
        vp.AddComponent<RectMask2D>();
        scroll.viewport = vp.GetComponent<RectTransform>();

        // Content — vertical layout, grows with children
        var content = MakeGO("Content", vp.transform);
        var crt     = content.GetComponent<RectTransform>();
        crt.anchorMin = new Vector2(0f, 1f);
        crt.anchorMax = new Vector2(1f, 1f);
        crt.pivot     = new Vector2(0.5f, 1f);
        crt.offsetMin = crt.offsetMax = Vector2.zero;

        var csf = content.AddComponent<ContentSizeFitter>();
        csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        var vl = content.AddComponent<VerticalLayoutGroup>();
        vl.childAlignment        = TextAnchor.UpperCenter;
        vl.spacing               = 0f;
        vl.padding               = new RectOffset(
            SI(SIDE_PAD), SI(SIDE_PAD),
            SI(TOP_PAD),  SI(BTM_PAD));
        vl.childControlWidth     = true;
        vl.childControlHeight    = true;
        vl.childForceExpandWidth = true;
        vl.childForceExpandHeight = false;

        scroll.content = crt;

        // ── Header ────────────────────────────────────────────────────────────
        BuildHeader(content.transform);

        // ── Section: Sound ────────────────────────────────────────────────────
        SectionLabel(content.transform, "🔊  SOUND");
        var soundCard = Card(content.transform);
        musicSlider = RowSlider(soundCard.transform, "Music",        "music",     TINT_SOUND, 0.7f);
        HorizontalDivider(soundCard.transform);
        sfxSlider   = RowSlider(soundCard.transform, "Sound Effects","sfx",       TINT_SOUND, 0.9f);

        // ── Section: Display ──────────────────────────────────────────────────
        SectionLabel(content.transform, "🌊  DISPLAY");
        var displayCard = Card(content.transform);
        vibrationToggle = RowToggle(displayCard.transform, "Vibration",  "Haptic feedback on interactions", "vibration", TINT_DISPLAY, true);
        HorizontalDivider(displayCard.transform);
        RowChips(displayCard.transform,  "Language", "",       "language", TINT_DISPLAY,
                 new[]{"EN","FR","ES"}, 0);
        HorizontalDivider(displayCard.transform);
        RowChips(displayCard.transform,  "Graphics Quality", "Higher = more battery", "graphics", TINT_DISPLAY,
                 new[]{"Low","Mid","High"}, 1);

        // ── Section: Game ─────────────────────────────────────────────────────
        SectionLabel(content.transform, "⚓  GAME");
        var gameCard = Card(content.transform);
        hintsToggle = RowToggle(gameCard.transform, "Show Hints",  "Display move suggestions", "hints", TINT_GAME, false);
        HorizontalDivider(gameCard.transform);
        timerToggle = RowToggle(gameCard.transform, "Show Timer",  "Track time per puzzle",    "timer", TINT_GAME, true);
        HorizontalDivider(gameCard.transform);
        leaderboardButton = RowChevron(gameCard.transform, "Leaderboard", "Compare with other captains", "leaderboard", TINT_GAME);

        // ── Reset button ──────────────────────────────────────────────────────
        Gap(content.transform, 18f);
        resetButton = ResetBtn(content.transform);

        // ── Version ───────────────────────────────────────────────────────────
        Gap(content.transform, 14f);
        var verGO  = MakeGO("Version", content.transform);
        LE(verGO, VERSION_H);
        var verTMP = verGO.AddComponent<TextMeshProUGUI>();
        verTMP.text           = $"v{Application.version}";
        verTMP.fontSize       = S(11f);
        verTMP.color          = new Color(1f,1f,1f,0.25f);
        verTMP.alignment      = TextAlignmentOptions.Center;
        verTMP.characterSpacing = S(2f);
    }

    // ── Header ────────────────────────────────────────────────────────────────

    void BuildHeader(Transform parent)
    {
        var hdr = MakeGO("Header", parent);
        LE(hdr, HDR_H);

        var hl = hdr.AddComponent<HorizontalLayoutGroup>();
        hl.childAlignment       = TextAnchor.MiddleCenter;
        hl.spacing              = 0f;
        hl.childControlWidth    = false;
        hl.childControlHeight   = true;
        hl.childForceExpandWidth  = false;
        hl.childForceExpandHeight = true;

        // Back button (left)
        var backGO  = MakeGO("BackButton", hdr.transform);
        var backLE  = backGO.AddComponent<LayoutElement>();
        backLE.preferredWidth = S(48f); backLE.flexibleWidth = 0f;
        var backImg = backGO.AddComponent<Image>();
        backImg.color = WHITE12; Rounded(backImg);
        backButton = backGO.AddComponent<Button>();
        TintBtn(backButton, WHITE25, new Color(1f,1f,1f,0.06f));

        var backIconImg = MakeGO("Icon", backGO.transform);
        var biRT = backIconImg.GetComponent<RectTransform>();
        biRT.anchorMin = biRT.anchorMax = new Vector2(0.5f, 0.5f);
        biRT.pivot     = new Vector2(0.5f, 0.5f);
        biRT.sizeDelta = new Vector2(S(22f), S(22f));
        var biImg = backIconImg.AddComponent<Image>();
        biImg.color = WHITE; biImg.preserveAspect = true; biImg.raycastTarget = false;
        var backSprite = Icon("back");
        if (backSprite != null) biImg.sprite = backSprite;
        // Fallback: small white "‹" label if no sprite
        if (backSprite == null)
        {
            var bTxt = MakeGO("Label", backGO.transform);
            StretchFill(bTxt);
            var bTMP = bTxt.AddComponent<TextMeshProUGUI>();
            bTMP.text = "‹"; bTMP.fontSize = S(32f); bTMP.color = WHITE;
            bTMP.alignment = TextAlignmentOptions.Center; bTMP.raycastTarget = false;
            Destroy(backIconImg); // remove blank image
        }

        // Title (centre, fills remaining space)
        var titleGO  = MakeGO("Title", hdr.transform);
        var titleLE  = titleGO.AddComponent<LayoutElement>();
        titleLE.flexibleWidth = 1f;
        var titleTMP = titleGO.AddComponent<TextMeshProUGUI>();
        titleTMP.text        = "SETTINGS";
        titleTMP.fontSize    = S(28f);
        titleTMP.fontStyle   = FontStyles.Bold;
        titleTMP.color       = WHITE;
        titleTMP.alignment   = TextAlignmentOptions.Center;

        // Ghost spacer (right) — mirrors back button width so title stays centred
        var ghost = MakeGO("Ghost", hdr.transform);
        var gLE   = ghost.AddComponent<LayoutElement>();
        gLE.preferredWidth = S(48f); gLE.flexibleWidth = 0f;
    }

    // ── Section label ─────────────────────────────────────────────────────────

    void SectionLabel(Transform parent, string text)
    {
        Gap(parent, SEC_GAP);
        var go  = MakeGO("SectionLabel_" + text, parent);
        LE(go, SEC_LBL_H);
        var tmp = go.AddComponent<TextMeshProUGUI>();
        tmp.text           = text;
        tmp.fontSize       = S(11f);
        tmp.fontStyle      = FontStyles.Bold;
        tmp.color          = WHITE40;
        tmp.alignment      = TextAlignmentOptions.Left;
        tmp.characterSpacing = S(3f);
    }

    // ── Card container ────────────────────────────────────────────────────────

    GameObject Card(Transform parent)
    {
        Gap(parent, 6f);
        var go  = MakeGO("Card", parent);
        var le  = go.AddComponent<LayoutElement>();
        le.flexibleHeight = 0f; // will be sized by children via ContentSizeFitter

        var csf = go.AddComponent<ContentSizeFitter>();
        csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        var vl = go.AddComponent<VerticalLayoutGroup>();
        vl.childAlignment        = TextAnchor.UpperCenter;
        vl.spacing               = 0f;
        vl.childControlWidth     = true;
        vl.childControlHeight    = true;
        vl.childForceExpandWidth = true;
        vl.childForceExpandHeight = false;

        var img = go.AddComponent<Image>();
        img.color = GLASS_FILL; Rounded(img);

        // Outer border layer (rendered behind, inset=0)
        var border = MakeGO("Border", go.transform);
        Stretch(border);
        border.transform.SetAsFirstSibling();
        var bImg = border.AddComponent<Image>();
        bImg.color = GLASS_BRD; Rounded(bImg);
        bImg.raycastTarget = false;

        return go;
    }

    // ── Horizontal divider between rows ──────────────────────────────────────

    void HorizontalDivider(Transform parent)
    {
        var go  = MakeGO("Divider", parent);
        LE(go, 1f);
        var img = go.AddComponent<Image>();
        img.color         = new Color(1f, 1f, 1f, 0.10f);
        img.raycastTarget = false;
    }

    // ── Row: Slider ───────────────────────────────────────────────────────────
    // Returns the Slider component so MainMenuManager can wire it.

    Slider RowSlider(Transform parent, string label, string iconName, Color tint, float defaultValue)
    {
        var row = MakeRow(parent, label, "", iconName, tint, false);
        // The row body already has a label; we need to add the slider below it

        // Find the RowBody child and add slider underneath the label
        var body = row.transform.Find("RowBody");

        // Volume icon small + slider + icon small
        var sliderWrap = MakeGO("SliderWrap", body.transform);
        var swRT = sliderWrap.GetComponent<RectTransform>();
        swRT.anchorMin = new Vector2(0f, 0f);
        swRT.anchorMax = new Vector2(1f, 0f);
        swRT.pivot     = new Vector2(0f, 1f);
        swRT.anchoredPosition = new Vector2(0f, -S(24f));
        swRT.sizeDelta = new Vector2(0f, S(20f));

        var swHL = sliderWrap.AddComponent<HorizontalLayoutGroup>();
        swHL.childAlignment        = TextAnchor.MiddleCenter;
        swHL.spacing               = 8f;
        swHL.childControlWidth     = false;
        swHL.childControlHeight    = false;
        swHL.childForceExpandWidth = false;
        swHL.childForceExpandHeight = false;

        // Low icon
        var loIcon = MakeGO("LowIcon", sliderWrap.transform);
        loIcon.GetComponent<RectTransform>().sizeDelta = new Vector2(S(16f), S(16f));
        var loTMP = loIcon.AddComponent<TextMeshProUGUI>();
        loTMP.text = "🔈"; loTMP.fontSize = S(13f);
        loTMP.alignment = TextAlignmentOptions.Center;
        loTMP.color = WHITE40;

        // Slider
        var sliderGO = MakeGO("Slider", sliderWrap.transform);
        var sliderRT = sliderGO.GetComponent<RectTransform>();
        sliderRT.sizeDelta = new Vector2(S(120f), S(20f));

        var sliderLE = sliderGO.AddComponent<LayoutElement>();
        sliderLE.preferredWidth  = 0f;
        sliderLE.flexibleWidth   = 1f;
        sliderLE.preferredHeight = S(20f);

        // Background track
        var bgGO  = MakeGO("Background", sliderGO.transform);
        var bgRT  = bgGO.GetComponent<RectTransform>();
        bgRT.anchorMin = new Vector2(0f, 0.5f); bgRT.anchorMax = new Vector2(1f, 0.5f);
        bgRT.pivot     = new Vector2(0.5f, 0.5f);
        bgRT.sizeDelta = new Vector2(0f, S(SLIDER_H));
        var bgImg = bgGO.AddComponent<Image>(); bgImg.color = WHITE25;

        // Fill area
        var fillArea = MakeGO("Fill Area", sliderGO.transform);
        var faRT     = fillArea.GetComponent<RectTransform>();
        faRT.anchorMin = new Vector2(0f, 0.5f); faRT.anchorMax = new Vector2(1f, 0.5f);
        faRT.pivot     = new Vector2(0.5f, 0.5f);
        faRT.offsetMin = new Vector2(0f, -S(3f)); faRT.offsetMax = new Vector2(-S(10f), S(3f));

        var fillGO  = MakeGO("Fill", fillArea.transform);
        var fillRT  = fillGO.GetComponent<RectTransform>();
        fillRT.anchorMin = Vector2.zero; fillRT.anchorMax = new Vector2(0f, 1f);
        fillRT.offsetMin = fillRT.offsetMax = Vector2.zero;
        var fillImg = fillGO.AddComponent<Image>(); fillImg.color = ORANGE;

        // Handle
        var handleArea = MakeGO("Handle Slide Area", sliderGO.transform);
        var haRT       = handleArea.GetComponent<RectTransform>();
        haRT.anchorMin = Vector2.zero; haRT.anchorMax = Vector2.one;
        haRT.offsetMin = new Vector2(S(10f), 0f); haRT.offsetMax = new Vector2(-S(10f), 0f);

        var handleGO  = MakeGO("Handle", handleArea.transform);
        var handleRT  = handleGO.GetComponent<RectTransform>();
        handleRT.anchorMin = handleRT.anchorMax = new Vector2(0f, 0.5f);
        handleRT.pivot     = new Vector2(0.5f, 0.5f);
        handleRT.sizeDelta = new Vector2(S(20f), S(20f));
        var handleImg = handleGO.AddComponent<Image>();
        handleImg.color = WHITE;
        Rounded(handleImg);

        // Wire Slider component
        var slider = sliderGO.AddComponent<Slider>();
        slider.fillRect   = fillRT;
        slider.handleRect = handleRT;
        slider.targetGraphic = handleImg;
        slider.direction  = Slider.Direction.LeftToRight;
        slider.minValue   = 0f; slider.maxValue = 1f;
        slider.value      = defaultValue;
        var cols = slider.colors;
        cols.highlightedColor = new Color(1f, 0.80f, 0.30f, 1f);
        cols.pressedColor     = ORANGE_DRK;
        slider.colors = cols;

        // High icon
        var hiIcon = MakeGO("HighIcon", sliderWrap.transform);
        hiIcon.GetComponent<RectTransform>().sizeDelta = new Vector2(S(16f), S(16f));
        var hiTMP = hiIcon.AddComponent<TextMeshProUGUI>();
        hiTMP.text = "🔊"; hiTMP.fontSize = S(13f);
        hiTMP.alignment = TextAlignmentOptions.Center;
        hiTMP.color = WHITE65;

        // Make the row taller to fit slider
        var rowLE = row.GetComponent<LayoutElement>();
        if (rowLE != null) rowLE.preferredHeight = ROW_H + 22f;

        return slider;
    }

    // ── Row: Toggle ───────────────────────────────────────────────────────────

    Toggle RowToggle(Transform parent, string label, string sub, string iconName,
                     Color tint, bool defaultOn)
    {
        var row = MakeRow(parent, label, sub, iconName, tint, false);

        // Toggle container (right side of row)
        var toggleGO = MakeGO("Toggle", row.transform);
        var toggleRT = toggleGO.GetComponent<RectTransform>();
        toggleRT.anchorMin = new Vector2(1f, 0.5f);
        toggleRT.anchorMax = new Vector2(1f, 0.5f);
        toggleRT.pivot     = new Vector2(1f, 0.5f);
        toggleRT.anchoredPosition = new Vector2(-S(2f), 0f);
        toggleRT.sizeDelta = new Vector2(S(TOGGLE_W), S(TOGGLE_H));

        // Track
        var trackGO  = MakeGO("Track", toggleGO.transform);
        var trackRT  = trackGO.GetComponent<RectTransform>();
        trackRT.anchorMin = Vector2.zero; trackRT.anchorMax = Vector2.one;
        trackRT.offsetMin = trackRT.offsetMax = Vector2.zero;
        var trackImg = trackGO.AddComponent<Image>();
        trackImg.color = defaultOn ? ORANGE : WHITE25;
        Rounded(trackImg);

        // Thumb
        float thumbSize = S(TOGGLE_H - 6f);
        var thumbGO  = MakeGO("Thumb", toggleGO.transform);
        var thumbRT  = thumbGO.GetComponent<RectTransform>();
        thumbRT.anchorMin = new Vector2(0f, 0.5f);
        thumbRT.anchorMax = new Vector2(0f, 0.5f);
        thumbRT.pivot     = new Vector2(0.5f, 0.5f);
        thumbRT.sizeDelta = new Vector2(thumbSize, thumbSize);
        thumbRT.anchoredPosition = new Vector2(
            defaultOn ? (S(TOGGLE_W) - thumbSize * 0.5f - S(3f)) : (thumbSize * 0.5f + S(3f)), 0f);
        var thumbImg = thumbGO.AddComponent<Image>();
        thumbImg.color  = WHITE;
        Rounded(thumbImg);

        // Toggle component (invisible graphic area = whole row for easier tapping)
        var toggle = row.AddComponent<Toggle>();
        toggle.targetGraphic = trackImg;
        toggle.graphic       = thumbImg;
        toggle.isOn          = defaultOn;

        // Animate track + thumb on value change
        toggle.onValueChanged.AddListener(on =>
        {
            trackImg.color = on ? ORANGE : WHITE25;
            thumbRT.anchoredPosition = new Vector2(
                on ? (S(TOGGLE_W) - thumbSize * 0.5f - S(3f)) : (thumbSize * 0.5f + S(3f)), 0f);
            PlayerPrefs.SetInt("Setting_" + iconName, on ? 1 : 0);
        });

        return toggle;
    }

    // ── Row: Chips (language / graphics) ─────────────────────────────────────

    void RowChips(Transform parent, string label, string sub, string iconName,
                  Color tint, string[] options, int defaultIndex)
    {
        var row = MakeRow(parent, label, sub, iconName, tint, false);

        // Chips container (right side)
        var chipsGO = MakeGO("Chips", row.transform);
        var chipsRT = chipsGO.GetComponent<RectTransform>();
        float totalW = options.Length * S(CHIP_W) + (options.Length - 1) * S(CHIP_GAP);
        chipsRT.anchorMin = new Vector2(1f, 0.5f);
        chipsRT.anchorMax = new Vector2(1f, 0.5f);
        chipsRT.pivot     = new Vector2(1f, 0.5f);
        chipsRT.anchoredPosition = new Vector2(-S(2f), 0f);
        chipsRT.sizeDelta = new Vector2(totalW, S(CHIP_H));

        var hl = chipsGO.AddComponent<HorizontalLayoutGroup>();
        hl.childAlignment       = TextAnchor.MiddleCenter;
        hl.spacing              = S(CHIP_GAP);
        hl.childControlWidth    = false;
        hl.childControlHeight   = false;
        hl.childForceExpandWidth  = false;
        hl.childForceExpandHeight = false;

        var chipImages  = new Image[options.Length];
        var chipLabels  = new TextMeshProUGUI[options.Length];
        int selected    = defaultIndex;

        for (int i = 0; i < options.Length; i++)
        {
            int idx = i; // capture for lambda
            var chipGO  = MakeGO("Chip_" + options[i], chipsGO.transform);
            var chipRT  = chipGO.GetComponent<RectTransform>();
            chipRT.sizeDelta = new Vector2(S(CHIP_W), S(CHIP_H));

            var chipImg = chipGO.AddComponent<Image>();
            chipImg.color = (i == defaultIndex) ? ORANGE : WHITE12;
            Rounded(chipImg);
            chipImages[i] = chipImg;

            var lbl = MakeGO("Label", chipGO.transform);
            StretchFill(lbl);
            var lTMP = lbl.AddComponent<TextMeshProUGUI>();
            lTMP.text      = options[i];
            lTMP.fontSize  = S(13f);
            lTMP.fontStyle = FontStyles.Bold;
            lTMP.color     = (i == defaultIndex) ? WHITE : WHITE40;
            lTMP.alignment = TextAlignmentOptions.Center;
            lTMP.raycastTarget = false;
            chipLabels[i] = lTMP;

            var btn = chipGO.AddComponent<Button>();
            btn.targetGraphic = chipImg;
            btn.onClick.AddListener(() =>
            {
                for (int j = 0; j < chipImages.Length; j++)
                {
                    chipImages[j].color  = (j == idx) ? ORANGE   : WHITE12;
                    chipLabels[j].color  = (j == idx) ? WHITE     : WHITE40;
                }
                PlayerPrefs.SetInt("Setting_" + iconName, idx);
            });
        }
    }

    // ── Row: Chevron (leaderboard / navigate) ─────────────────────────────────

    Button RowChevron(Transform parent, string label, string sub, string iconName, Color tint)
    {
        var row = MakeRow(parent, label, sub, iconName, tint);

        // Chevron "›" on the right
        var chevGO  = MakeGO("Chevron", row.transform);
        var chevRT  = chevGO.GetComponent<RectTransform>();
        chevRT.anchorMin = new Vector2(1f, 0.5f);
        chevRT.anchorMax = new Vector2(1f, 0.5f);
        chevRT.pivot     = new Vector2(1f, 0.5f);
        chevRT.anchoredPosition = new Vector2(-S(4f), 0f);
        chevRT.sizeDelta = new Vector2(S(20f), S(24f));
        var chevTMP = chevGO.AddComponent<TextMeshProUGUI>();
        chevTMP.text      = "›";
        chevTMP.fontSize  = S(22f);
        chevTMP.color     = WHITE40;
        chevTMP.alignment = TextAlignmentOptions.Center;
        chevTMP.raycastTarget = false;

        return row.GetComponent<Button>() ?? row.AddComponent<Button>();
    }

    // ── Shared row builder ────────────────────────────────────────────────────
    // Creates the row background, icon bubble, and label/sub-label.
    // Returns the row GO so callers can add their control widget.

    GameObject MakeRow(Transform parent, string label, string sub, string iconName, Color tint, bool addButton = true)
    {
        var row   = MakeGO("Row_" + label, parent);
        LE(row, ROW_H);

        // Transparent image so rows can still host selectable controls cleanly.
        var rowImg = row.AddComponent<Image>();
        rowImg.color = Color.clear;
        if (addButton)
        {
            var rowBtn = row.AddComponent<Button>();
            var rowCols = rowBtn.colors;
            rowCols.normalColor      = Color.clear;
            rowCols.highlightedColor = GLASS_ROW;
            rowCols.pressedColor     = new Color(1f,1f,1f,0.14f);
            rowCols.selectedColor    = Color.clear;
            rowCols.fadeDuration     = 0.1f;
            rowBtn.colors            = rowCols;
            rowBtn.targetGraphic     = rowImg;
            rowBtn.transition        = Selectable.Transition.ColorTint;
        }

        // Icon bubble
        var bubble   = MakeGO("IconBubble", row.transform);
        var bubbleRT = bubble.GetComponent<RectTransform>();
        bubbleRT.anchorMin = new Vector2(0f, 0.5f);
        bubbleRT.anchorMax = new Vector2(0f, 0.5f);
        bubbleRT.pivot     = new Vector2(0f, 0.5f);
        bubbleRT.anchoredPosition = new Vector2(S(14f), 0f);
        bubbleRT.sizeDelta = new Vector2(S(ICON_BOX), S(ICON_BOX));
        var bubbleImg = bubble.AddComponent<Image>();
        bubbleImg.color = new Color(tint.r, tint.g, tint.b, 0.28f);
        Rounded(bubbleImg);
        bubbleImg.raycastTarget = false;

        // Icon image inside bubble
        var iconGO  = MakeGO("Icon", bubble.transform);
        var iconRT  = iconGO.GetComponent<RectTransform>();
        iconRT.anchorMin = iconRT.anchorMax = new Vector2(0.5f, 0.5f);
        iconRT.pivot     = new Vector2(0.5f, 0.5f);
        iconRT.sizeDelta = new Vector2(S(ICON_IMG), S(ICON_IMG));
        var iconImg = iconGO.AddComponent<Image>();
        iconImg.color = tint; iconImg.preserveAspect = true;
        iconImg.raycastTarget = false;
        var sp = Icon(iconName);
        if (sp != null) iconImg.sprite = sp;

        // Row body (label + optional sub)
        var body   = MakeGO("RowBody", row.transform);
        var bodyRT = body.GetComponent<RectTransform>();
        bodyRT.anchorMin = new Vector2(0f, 0f);
        bodyRT.anchorMax = new Vector2(1f, 1f);
        // Left offset: pad + bubble + gap; right: leave room for control (70 px)
        bodyRT.offsetMin = new Vector2(S(14f + ICON_BOX + 12f), 0f);
        bodyRT.offsetMax = new Vector2(-S(74f), 0f);

        // Main label
        var lblGO  = MakeGO("Label", body.transform);
        var lblRT  = lblGO.GetComponent<RectTransform>();
        lblRT.anchorMin = new Vector2(0f, string.IsNullOrEmpty(sub) ? 0f : 0.5f);
        lblRT.anchorMax = Vector2.one;
        lblRT.offsetMin = lblRT.offsetMax = Vector2.zero;
        var lblTMP = lblGO.AddComponent<TextMeshProUGUI>();
        lblTMP.text      = label;
        lblTMP.fontSize  = S(16f);
        lblTMP.fontStyle = FontStyles.Bold;
        lblTMP.color     = WHITE;
        lblTMP.alignment = TextAlignmentOptions.Left;
        lblTMP.overflowMode = TextOverflowModes.Ellipsis;
        lblTMP.raycastTarget = false;

        // Sub-label
        if (!string.IsNullOrEmpty(sub))
        {
            var subGO  = MakeGO("Sub", body.transform);
            var subRT  = subGO.GetComponent<RectTransform>();
            subRT.anchorMin = Vector2.zero;
            subRT.anchorMax = new Vector2(1f, 0.5f);
            subRT.offsetMin = subRT.offsetMax = Vector2.zero;
            var subTMP = subGO.AddComponent<TextMeshProUGUI>();
            subTMP.text      = sub;
            subTMP.fontSize  = S(12f);
            subTMP.color     = WHITE40;
            subTMP.alignment = TextAlignmentOptions.Left;
            subTMP.overflowMode = TextOverflowModes.Ellipsis;
            subTMP.raycastTarget = false;
        }

        return row;
    }

    // ── Reset button ──────────────────────────────────────────────────────────

    Button ResetBtn(Transform parent)
    {
        var go  = MakeGO("ResetButton", parent);
        LE(go, RESET_H);

        var img = go.AddComponent<Image>();
        img.color = RED_FILL; Rounded(img);

        // Red border overlay
        var border = MakeGO("Border", go.transform);
        Stretch(border);
        var bImg = border.AddComponent<Image>();
        bImg.color = RED_BORDER; Rounded(bImg);
        bImg.raycastTarget = false;

        var lbl = MakeGO("Label", go.transform);
        StretchFill(lbl);
        var lTMP = lbl.AddComponent<TextMeshProUGUI>();
        lTMP.text      = "🗑   Reset Progress";
        lTMP.fontSize  = S(16f);
        lTMP.fontStyle = FontStyles.Bold;
        lTMP.color     = RED_TEXT;
        lTMP.alignment = TextAlignmentOptions.Center;
        lTMP.raycastTarget = false;

        var btn = go.AddComponent<Button>();
        btn.targetGraphic = img;
        var cols = btn.colors;
        cols.highlightedColor = new Color(0.94f, 0.27f, 0.27f, 0.22f);
        cols.pressedColor     = new Color(0.94f, 0.27f, 0.27f, 0.06f);
        btn.colors = cols;

        return btn;
    }

    // ── Small helpers ─────────────────────────────────────────────────────────

    // Invisible gap element (vertical spacer with fixed height)
    void Gap(Transform parent, float h)
    {
        var go = MakeGO("Gap", parent);
        LE(go, h);
    }

    // ── Background helpers (identical to HomeScreenBuilder) ───────────────────

    void Layer(string name, Transform parent, float minY, float maxY, Color color)
    {
        var go = MakeGO(name, parent);
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0f, minY); rt.anchorMax = new Vector2(1f, maxY);
        rt.offsetMin = rt.offsetMax = Vector2.zero;
        go.AddComponent<Image>().color = color;
    }

    void MakeCloud(Transform parent, string name, float w, float h,
                   float startX, float anchorY, float dur, float delay)
    {
        var go = MakeGO(name, parent);
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0f, anchorY); rt.anchorMax = new Vector2(0f, anchorY);
        rt.pivot     = new Vector2(0f, 0.5f);    rt.sizeDelta = new Vector2(w, h);
        rt.anchoredPosition = new Vector2(startX, 0f);
        var img = go.AddComponent<Image>(); img.color = CLOUD_CLR; Rounded(img);
        var d = go.AddComponent<CloudDrifter>(); d.duration = dur; d.delay = delay;
        d.startX = startX; d.endX = 520f;
    }

    void MakeWave(Transform parent, string name, float anchorY, float height,
                  Color color, float dur, float delay)
    {
        var go = MakeGO(name, parent);
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(-0.1f, anchorY); rt.anchorMax = new Vector2(1.1f, anchorY);
        rt.pivot     = new Vector2(0.5f, 0.5f);     rt.sizeDelta = new Vector2(0f, height);
        var img = go.AddComponent<Image>(); img.color = color; Rounded(img);
        var wv = go.AddComponent<WaveRocker>(); wv.duration = dur; wv.delay = delay;
    }

    void MakeDecoBoat(Transform parent, string name, string emoji, float fontSize,
                      Vector2 anchor, float dur, float delay)
    {
        var go  = MakeGO(name, parent);
        var rt  = go.GetComponent<RectTransform>();
        rt.anchorMin = anchor; rt.anchorMax = anchor;
        rt.pivot = new Vector2(0.5f, 0.5f); rt.sizeDelta = new Vector2(50f, 50f);
        var tmp = go.AddComponent<TextMeshProUGUI>();
        tmp.text = emoji; tmp.fontSize = fontSize;
        tmp.color = CLOUD_CLR; tmp.alignment = TextAlignmentOptions.Center;
        var bob = go.AddComponent<Bobber>();
        bob.amplitude = 10f; bob.tiltDeg = 5f; bob.duration = dur; bob.delay = delay;
    }

    // ── UI utility ────────────────────────────────────────────────────────────

    static GameObject MakeGO(string name, Transform parent)
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

    static void StretchFill(GameObject go)
    {
        var rt = go.GetComponent<RectTransform>(); if (!rt) return;
        rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
        rt.offsetMin = rt.offsetMax = Vector2.zero;
    }

    void LE(GameObject go, float h)
    {
        var le = go.GetComponent<LayoutElement>() ?? go.AddComponent<LayoutElement>();
        le.preferredHeight = S(h);
    }

    static void TintBtn(Button btn, Color highlight, Color pressed)
    {
        var c = btn.colors;
        c.highlightedColor = highlight; c.pressedColor = pressed;
        btn.colors = c;
    }

    static Sprite _roundedSprite;
    static void Rounded(Image img)
    {
        if (_roundedSprite == null)
        {
            const int sz = 128, r = 24, b = 8;
            var tex = new Texture2D(sz, sz, TextureFormat.RGBA32, false);
            var px  = new Color32[sz * sz];
            for (int y = 0; y < sz; y++)
            for (int x = 0; x < sz; x++)
            {
                int cx = Mathf.Clamp(x, r, sz - r - 1);
                int cy = Mathf.Clamp(y, r, sz - r - 1);
                float d = Mathf.Sqrt((x-cx)*(x-cx)+(y-cy)*(y-cy));
                px[y*sz+x] = new Color32(255,255,255,(byte)(d<=r?255:0));
            }
            tex.SetPixels32(px); tex.Apply();
            _roundedSprite = Sprite.Create(tex, new Rect(0,0,sz,sz),
                new Vector2(.5f,.5f), 100f, 0, SpriteMeshType.FullRect,
                new Vector4(b,b,b,b));
        }
        img.sprite = _roundedSprite;
        img.type   = Image.Type.Sliced;
    }

    static Color Hex(string h)
    {
        ColorUtility.TryParseHtmlString("#" + h, out Color c); return c;
    }
}