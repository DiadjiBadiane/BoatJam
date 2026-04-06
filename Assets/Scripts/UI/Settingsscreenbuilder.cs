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
    const float SIDE_PAD   = 24f;
    const float TOP_PAD    = 60f;   // room for status bar / notch
    const float HDR_H      = 64f;   // header bar height
    const float SEC_LBL_H  = 32f;   // section label height
    const float SEC_GAP    = 14f;   // gap above a section label
    const float ROW_H      = 76f;   // each settings row
    const float CARD_RADIUS = 22f;
    const float ICON_BOX   = 44f;   // the coloured icon bubble
    const float ICON_IMG   = 26f;   // icon image inside the bubble
    const float SLIDER_H   = 6f;
    const float TOGGLE_W   = 54f;
    const float TOGGLE_H   = 30f;
    const float CHIP_W     = 56f;
    const float CHIP_H     = 32f;
    const float CHIP_GAP   = 7f;
    const float RESET_H    = 62f;
    const float VERSION_H  = 24f;
    const float BTM_PAD    = 40f;
    const float CTRL_INSET = 16f;   // right inset for all controls (toggles/chips) inside card

    // ── Colors (identical to HomeScreenBuilder) ───────────────────────────────
    static readonly Color SKY_TOP    = Hex("0878a8");
    static readonly Color SKY_MID    = Hex("065f8f");
    static readonly Color SEA_MID    = Hex("152d4a");
    static readonly Color ORANGE     = new Color(0.96f, 0.62f, 0.07f, 1f);
    static readonly Color ORANGE_DRK = new Color(0.85f, 0.45f, 0.02f, 1f);
    static readonly Color GLASS_BRD  = new Color(1f, 1f, 1f, 0.22f);
    static readonly Color GLASS_FILL = new Color(0f, 0.04f, 0.12f, 0.45f);
    static readonly Color GLASS_ROW  = new Color(1f, 1f, 1f, 0.07f);
    static readonly Color CLOUD_CLR  = new Color(1f, 1f, 1f, 0.18f);
    static readonly Color GLINT_CLR  = new Color(1f, 0.863f, 0.314f, 0.6f);
    static readonly Color WHITE      = Color.white;
    static readonly Color WHITE65    = new Color(1f, 1f, 1f, 0.65f);
    static readonly Color WHITE40    = new Color(1f, 1f, 1f, 0.40f);
    static readonly Color WHITE25    = new Color(1f, 1f, 1f, 0.25f);
    static readonly Color WHITE12    = new Color(1f, 1f, 1f, 0.12f);
    static readonly Color RED_BORDER = new Color(0.94f, 0.27f, 0.27f, 0.30f);
    static readonly Color RED_FILL   = new Color(0.94f, 0.27f, 0.27f, 0.07f);
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
        _uiScale = Screen.width > Screen.height ? 1.85f : 1.5f;

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

        // 5-stop gradient — darkened for readability
        Layer("Sky_Top",  bg.transform, 0.55f, 1.00f, SKY_TOP);
        Layer("Sky_Mid",  bg.transform, 0.25f, 0.55f, SKY_MID);
        Layer("Sky_Low",  bg.transform, 0.10f, 0.25f, Hex("05496e"));
        Layer("Sea_Mid",  bg.transform, 0.04f, 0.10f, SEA_MID);
        Layer("Sea_Bot",  bg.transform, 0.00f, 0.04f, Hex("0a1628"));

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
        var soundCard = Card(content.transform);
        musicSlider = RowSlider(soundCard.transform, "Music",        "music",     TINT_SOUND, 0.7f, "ui.settings.music");
        HorizontalDivider(soundCard.transform);
        sfxSlider   = RowSlider(soundCard.transform, "Sound Effects","sfx",       TINT_SOUND, 0.9f, "ui.settings.sound_effects");

        // ── Section: Display ──────────────────────────────────────────────────
        Gap(content.transform, 14f);
        var displayCard = Card(content.transform);
        vibrationToggle = RowToggle(displayCard.transform, "Vibration",  "Haptic feedback on interactions", "vibration", TINT_DISPLAY, true, "ui.settings.vibration", "ui.settings.vibration_desc");
        HorizontalDivider(displayCard.transform);
        RowChips(displayCard.transform,  "Language", "",       "language", TINT_DISPLAY,
                 new[]{"EN","FR","ES"}, 0, "ui.settings.language", null, null);
        HorizontalDivider(displayCard.transform);
        RowChips(displayCard.transform,  "Graphics Quality", "Higher = more battery", "graphics", TINT_DISPLAY,
                 new[]{"Low","Mid","High"}, 1, "ui.settings.graphics_quality", "ui.settings.graphics_desc",
                 new[]{"ui.settings.graphics_low","ui.settings.graphics_mid","ui.settings.graphics_high"});

        // ── Section: Game ─────────────────────────────────────────────────────
        Gap(content.transform, 14f);
        var gameCard = Card(content.transform);
        hintsToggle = RowToggle(gameCard.transform, "Show Hints",  "Display move suggestions", "hints", TINT_GAME, false, "ui.settings.show_hints", "ui.settings.hints_desc");
        HorizontalDivider(gameCard.transform);
        timerToggle = RowToggle(gameCard.transform, "Show Timer",  "Track time per puzzle",    "timer", TINT_GAME, true, "ui.settings.show_timer", "ui.settings.timer_desc");
        HorizontalDivider(gameCard.transform);
        leaderboardButton = RowChevron(gameCard.transform, "Leaderboard", "Compare with other captains", "leaderboard", TINT_GAME, "ui.settings.leaderboard", "ui.settings.leaderboard_desc");

        // ── Reset button ──────────────────────────────────────────────────────
        Gap(content.transform, 18f);
        resetButton = ResetBtn(content.transform);

        // ── Version ───────────────────────────────────────────────────────────
        Gap(content.transform, 14f);
        var verGO  = MakeGO("Version", content.transform);
        LE(verGO, VERSION_H);
        var verTMP = verGO.AddComponent<TextMeshProUGUI>();
        verTMP.text           = $"v{Application.version}";
        verTMP.fontSize       = S(13f);
        verTMP.color          = new Color(1f,1f,1f,0.25f);
        verTMP.alignment      = TextAlignmentOptions.Center;
        verTMP.characterSpacing = S(2f);
    }

    // ── Header ────────────────────────────────────────────────────────────────

    void BuildHeader(Transform parent)
    {
        var hdr = MakeGO("Header", parent);
        LE(hdr, HDR_H);

        // ── Back button — absolutely anchored to LEFT of header ──
        var backGO = MakeGO("BackButton", hdr.transform);
        var backRT = backGO.GetComponent<RectTransform>();
        backRT.anchorMin = new Vector2(0f, 0.5f);
        backRT.anchorMax = new Vector2(0f, 0.5f);
        backRT.pivot     = new Vector2(0f, 0.5f);
        backRT.anchoredPosition = new Vector2(0f, 0f);
        backRT.sizeDelta = new Vector2(S(46f), S(46f));

        var backImg = backGO.AddComponent<Image>();
        backImg.color = WHITE12; Rounded(backImg);

        // Border overlay on back button
        var backBorder = MakeGO("Border", backGO.transform);
        Stretch(backBorder);
        var bbImg = backBorder.AddComponent<Image>();
        bbImg.color = new Color(1f, 1f, 1f, 0.25f); Rounded(bbImg);
        bbImg.raycastTarget = false;

        backButton = backGO.AddComponent<Button>();
        TintBtn(backButton, WHITE25, new Color(1f, 1f, 1f, 0.06f));

        // Icon inside back button
        var backIconGO = MakeGO("Icon", backGO.transform);
        var biRT = backIconGO.GetComponent<RectTransform>();
        biRT.anchorMin = biRT.anchorMax = new Vector2(0.5f, 0.5f);
        biRT.pivot     = new Vector2(0.5f, 0.5f);
        biRT.sizeDelta = new Vector2(S(24f), S(24f));
        var biImg = backIconGO.AddComponent<Image>();
        biImg.color = WHITE; biImg.preserveAspect = true; biImg.raycastTarget = false;
        var backSprite = Icon("back");
        if (backSprite != null)
        {
            biImg.sprite = backSprite;
        }
        else
        {
            // Fallback text chevron
            var bTxt = MakeGO("Label", backGO.transform);
            StretchFill(bTxt);
            var bTMP = bTxt.AddComponent<TextMeshProUGUI>();
            bTMP.text = "‹"; bTMP.fontSize = S(34f); bTMP.color = WHITE;
            bTMP.alignment = TextAlignmentOptions.Center; bTMP.raycastTarget = false;
            if (Application.isPlaying) Destroy(backIconGO); else DestroyImmediate(backIconGO);
        }

        // ── Title — centred in full header width using HorizontalLayoutGroup ──
        var titleGO = MakeGO("Title", hdr.transform);
        var titleRT = titleGO.GetComponent<RectTransform>();
        titleRT.anchorMin = Vector2.zero;
        titleRT.anchorMax = Vector2.one;
        titleRT.offsetMin = titleRT.offsetMax = Vector2.zero;

        // Centred container that auto-sizes to fit icon + text
        var titleContent = MakeGO("TitleContent", titleGO.transform);
        var tcRT = titleContent.GetComponent<RectTransform>();
        tcRT.anchorMin = new Vector2(0.5f, 0.5f);
        tcRT.anchorMax = new Vector2(0.5f, 0.5f);
        tcRT.pivot     = new Vector2(0.5f, 0.5f);

        var tcCSF = titleContent.AddComponent<ContentSizeFitter>();
        tcCSF.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
        tcCSF.verticalFit   = ContentSizeFitter.FitMode.PreferredSize;

        var titleHL = titleContent.AddComponent<HorizontalLayoutGroup>();
        titleHL.childAlignment        = TextAnchor.MiddleCenter;
        titleHL.spacing               = S(8f);
        titleHL.childControlWidth     = true;
        titleHL.childControlHeight    = true;
        titleHL.childForceExpandWidth = false;
        titleHL.childForceExpandHeight = false;

        // Settings icon sprite (left of title text)
        var settingsIconGO = MakeGO("SettingsIcon", titleContent.transform);
        var siLE = settingsIconGO.AddComponent<LayoutElement>();
        siLE.preferredWidth  = S(30f);
        siLE.preferredHeight = S(30f);
        var siImg = settingsIconGO.AddComponent<Image>();
        siImg.color = WHITE; siImg.preserveAspect = true; siImg.raycastTarget = false;
        var settingsSprite = Icon("settings");
        if (settingsSprite != null)
            siImg.sprite = settingsSprite;
        else
            settingsIconGO.SetActive(false);

        // Title text
        var titleTextGO = MakeGO("TitleText", titleContent.transform);
        var titleTMP = titleTextGO.AddComponent<TextMeshProUGUI>();
        titleTMP.text             = LocalizationManager.T("ui.settings.title");
        titleTMP.fontSize         = S(30f);
        titleTMP.fontStyle        = FontStyles.Bold;
        titleTMP.color            = WHITE;
        titleTMP.alignment        = TextAlignmentOptions.Center;
        titleTMP.characterSpacing = S(2f);
        titleTMP.raycastTarget    = false;
        Loc(titleTextGO, "ui.settings.title");
        // Shadow to mimic HTML text-shadow
        var titleShadow = titleTextGO.AddComponent<UnityEngine.UI.Shadow>();
        titleShadow.effectColor    = Hex("0369a1");
        titleShadow.effectDistance = new Vector2(0f, -S(2f));
    }

    // ── Section label ─────────────────────────────────────────────────────────

    void SectionLabel(Transform parent, string text, string iconName = null)
    {
        Gap(parent, SEC_GAP);
        var go  = MakeGO("SectionLabel_" + text, parent);
        LE(go, SEC_LBL_H);

        if (iconName != null)
        {
            var hl = go.AddComponent<HorizontalLayoutGroup>();
            hl.childAlignment        = TextAnchor.MiddleLeft;
            hl.spacing               = S(6f);
            hl.childControlWidth     = false;
            hl.childControlHeight    = true;
            hl.childForceExpandWidth = false;
            hl.childForceExpandHeight = false;
            hl.padding               = new RectOffset(SI(4f), 0, 0, 0);

            var iconGO = MakeGO("Icon", go.transform);
            var iconLE = iconGO.AddComponent<LayoutElement>();
            iconLE.preferredWidth  = S(16f);
            iconLE.preferredHeight = S(16f);
            var iconImg = iconGO.AddComponent<Image>();
            iconImg.color          = new Color(1f, 1f, 1f, 0.45f);
            iconImg.preserveAspect = true;
            iconImg.raycastTarget  = false;
            var sp = Icon(iconName);
            if (sp != null) iconImg.sprite = sp;

            var txtGO = MakeGO("Text", go.transform);
            var txtLE = txtGO.AddComponent<LayoutElement>();
            txtLE.flexibleWidth = 1f;
            var tmp = txtGO.AddComponent<TextMeshProUGUI>();
            tmp.text             = text.ToUpper();
            tmp.fontSize         = S(13f);
            tmp.fontStyle        = FontStyles.Bold;
            tmp.color            = new Color(1f, 1f, 1f, 0.45f);
            tmp.alignment        = TextAlignmentOptions.Left;
            tmp.characterSpacing = S(3f);
        }
        else
        {
            var tmp = go.AddComponent<TextMeshProUGUI>();
            tmp.text             = text.ToUpper();
            tmp.fontSize         = S(13f);
            tmp.fontStyle        = FontStyles.Bold;
            tmp.color            = new Color(1f, 1f, 1f, 0.45f);
            tmp.alignment        = TextAlignmentOptions.Left;
            tmp.characterSpacing = S(3f);
        }
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

        // Outer border layer (excluded from layout)
        var border = MakeGO("Border", go.transform);
        Stretch(border);
        border.transform.SetAsFirstSibling();
        var bImg = border.AddComponent<Image>();
        bImg.color = GLASS_BRD; Rounded(bImg);
        bImg.raycastTarget = false;
        var borderLE = border.AddComponent<LayoutElement>();
        borderLE.ignoreLayout = true;

        // Inner top-edge gloss (very subtle top highlight like glass, excluded from layout)
        var gloss = MakeGO("Gloss", go.transform);
        var glossLE = gloss.AddComponent<LayoutElement>();
        glossLE.ignoreLayout = true;
        var grt   = gloss.GetComponent<RectTransform>();
        grt.anchorMin = new Vector2(0f, 1f); grt.anchorMax = new Vector2(1f, 1f);
        grt.pivot = new Vector2(0.5f, 1f);
        grt.offsetMin = new Vector2(S(4f),  -S(3f));
        grt.offsetMax = new Vector2(-S(4f),  0f);
        grt.sizeDelta = new Vector2(0f, S(3f));
        var gImg = gloss.AddComponent<Image>();
        gImg.color = new Color(1f, 1f, 1f, 0.18f); Rounded(gImg);
        gImg.raycastTarget = false;

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

    Slider RowSlider(Transform parent, string label, string iconName, Color tint, float defaultValue, string locKey = null)
    {
        // Taller row to accommodate label + slider
        const float SLIDER_ROW_H = 90f;

        var row = MakeRow(parent, label, "", iconName, tint, false, locKey, null);
        var rowLE = row.GetComponent<LayoutElement>();
        if (rowLE != null) rowLE.preferredHeight = S(SLIDER_ROW_H);

        var body = row.transform.Find("RowBody");
        if (body != null)
        {
            var bodyRT = body.GetComponent<RectTransform>();
            // Widen the body right edge to leave room for toggle (TOGGLE_W + CTRL_INSET*2)
            float ctrlAreaW = S(TOGGLE_W) + S(CTRL_INSET) * 2f;
            bodyRT.offsetMax = new Vector2(-ctrlAreaW, 0f);

            // Label occupies top half of body, bottom-aligned to sit level with icon
            var lblRT = body.transform.Find("Label")?.GetComponent<RectTransform>();
            if (lblRT != null)
            {
                lblRT.anchorMin = new Vector2(0f, 0.45f);
                lblRT.anchorMax = Vector2.one;
                lblRT.offsetMin = lblRT.offsetMax = Vector2.zero;
                var lblTMP = lblRT.GetComponent<TextMeshProUGUI>();
                if (lblTMP != null) lblTMP.alignment = TextAlignmentOptions.BottomLeft;
            }

            // Slider wrap — bottom half of body
            var sliderWrap = MakeGO("SliderWrap", body.transform);
            var swRT = sliderWrap.GetComponent<RectTransform>();
            swRT.anchorMin = new Vector2(0f, 0f);
            swRT.anchorMax = new Vector2(1f, 0.5f);
            swRT.offsetMin = Vector2.zero;
            swRT.offsetMax = Vector2.zero;

            var swHL = sliderWrap.AddComponent<HorizontalLayoutGroup>();
            swHL.childAlignment         = TextAnchor.MiddleCenter;
            swHL.spacing                = S(8f);
            swHL.childControlWidth      = false;
            swHL.childControlHeight     = false;
            swHL.childForceExpandWidth  = false;
            swHL.childForceExpandHeight = false;

            // Low volume icon
            var loIcon = MakeGO("LowIcon", sliderWrap.transform);
            loIcon.GetComponent<RectTransform>().sizeDelta = new Vector2(S(18f), S(18f));
            var loTMP = loIcon.AddComponent<TextMeshProUGUI>();
            loTMP.text = "🔈"; loTMP.fontSize = S(14f);
            loTMP.alignment = TextAlignmentOptions.Center;
            loTMP.color = WHITE40;

            // Slider GO
            var sliderGO = MakeGO("Slider", sliderWrap.transform);
            var sliderRT = sliderGO.GetComponent<RectTransform>();
            sliderRT.sizeDelta = new Vector2(S(120f), S(22f));
            var sliderLE = sliderGO.AddComponent<LayoutElement>();
            sliderLE.preferredWidth  = 0f;
            sliderLE.flexibleWidth   = 1f;
            sliderLE.preferredHeight = S(22f);

            // Track background
            var bgGO = MakeGO("Background", sliderGO.transform);
            var bgRT = bgGO.GetComponent<RectTransform>();
            bgRT.anchorMin = new Vector2(0f, 0.5f); bgRT.anchorMax = new Vector2(1f, 0.5f);
            bgRT.pivot = new Vector2(0.5f, 0.5f); bgRT.sizeDelta = new Vector2(0f, S(SLIDER_H));
            var bgImg = bgGO.AddComponent<Image>(); bgImg.color = WHITE25; Rounded(bgImg);

            // Fill area
            var fillArea = MakeGO("Fill Area", sliderGO.transform);
            var faRT = fillArea.GetComponent<RectTransform>();
            faRT.anchorMin = new Vector2(0f, 0.5f); faRT.anchorMax = new Vector2(1f, 0.5f);
            faRT.pivot = new Vector2(0.5f, 0.5f);
            faRT.offsetMin = new Vector2(S(5f), -S(3f)); faRT.offsetMax = new Vector2(-S(5f), S(3f));

            var fillGO = MakeGO("Fill", fillArea.transform);
            var fillRT = fillGO.GetComponent<RectTransform>();
            fillRT.anchorMin = Vector2.zero; fillRT.anchorMax = new Vector2(0f, 1f);
            fillRT.offsetMin = fillRT.offsetMax = Vector2.zero;
            var fillImg = fillGO.AddComponent<Image>(); fillImg.color = ORANGE; Rounded(fillImg);

            // Handle slide area
            var handleArea = MakeGO("Handle Slide Area", sliderGO.transform);
            var haRT = handleArea.GetComponent<RectTransform>();
            haRT.anchorMin = Vector2.zero; haRT.anchorMax = Vector2.one;
            haRT.offsetMin = new Vector2(S(11f), 0f); haRT.offsetMax = new Vector2(-S(11f), 0f);

            var handleGO = MakeGO("Handle", handleArea.transform);
            var handleRT = handleGO.GetComponent<RectTransform>();
            handleRT.anchorMin = handleRT.anchorMax = new Vector2(0f, 0.5f);
            handleRT.pivot = new Vector2(0.5f, 0.5f);
            handleRT.sizeDelta = new Vector2(S(22f), S(22f));
            var handleImg = handleGO.AddComponent<Image>();
            handleImg.color = WHITE;
            Rounded(handleImg);

            // Slider component
            var slider = sliderGO.AddComponent<Slider>();
            slider.fillRect      = fillRT;
            slider.handleRect    = handleRT;
            slider.targetGraphic = handleImg;
            slider.direction     = Slider.Direction.LeftToRight;
            slider.minValue = 0f; slider.maxValue = 1f;
            slider.value = defaultValue;
            var cols = slider.colors;
            cols.highlightedColor = new Color(1f, 0.80f, 0.30f, 1f);
            cols.pressedColor     = ORANGE_DRK;
            slider.colors = cols;
            string prefsKey = iconName == "music" ? "MusicVolume" : "SFXVolume";
            slider.onValueChanged.AddListener(v => {
                PlayerPrefs.SetFloat(prefsKey, v);
                if (AudioManager.Instance != null) AudioManager.Instance.ApplyVolumes();
            });

            // High volume icon
            var hiIcon = MakeGO("HighIcon", sliderWrap.transform);
            hiIcon.GetComponent<RectTransform>().sizeDelta = new Vector2(S(18f), S(18f));
            var hiTMP = hiIcon.AddComponent<TextMeshProUGUI>();
            hiTMP.text = "🔊"; hiTMP.fontSize = S(14f);
            hiTMP.alignment = TextAlignmentOptions.Center;
            hiTMP.color = WHITE65;

            // Toggle (ON/OFF switch) — right side with CTRL_INSET
            var toggleGO = MakeGO("Toggle", row.transform);
            var toggleRT = toggleGO.GetComponent<RectTransform>();
            toggleRT.anchorMin = new Vector2(1f, 0.5f);
            toggleRT.anchorMax = new Vector2(1f, 0.5f);
            toggleRT.pivot     = new Vector2(1f, 0.5f);
            toggleRT.anchoredPosition = new Vector2(-S(CTRL_INSET), 0f);
            toggleRT.sizeDelta = new Vector2(S(TOGGLE_W), S(TOGGLE_H));

            var trackGO  = MakeGO("Track", toggleGO.transform);
            var trackRT2 = trackGO.GetComponent<RectTransform>();
            trackRT2.anchorMin = Vector2.zero; trackRT2.anchorMax = Vector2.one;
            trackRT2.offsetMin = trackRT2.offsetMax = Vector2.zero;
            var trackImg = trackGO.AddComponent<Image>();
            trackImg.color = ORANGE; Rounded(trackImg);

            float thumbSz = S(TOGGLE_H - 6f);
            var thumbGO  = MakeGO("Thumb", toggleGO.transform);
            var thumbRT2 = thumbGO.GetComponent<RectTransform>();
            thumbRT2.anchorMin = new Vector2(0f, 0.5f);
            thumbRT2.anchorMax = new Vector2(0f, 0.5f);
            thumbRT2.pivot     = new Vector2(0.5f, 0.5f);
            thumbRT2.sizeDelta = new Vector2(thumbSz, thumbSz);
            thumbRT2.anchoredPosition = new Vector2(S(TOGGLE_W) - thumbSz * 0.5f - S(3f), 0f);
            var thumbImg2 = thumbGO.AddComponent<Image>();
            thumbImg2.color = WHITE; Rounded(thumbImg2);

            var toggle = toggleGO.AddComponent<Toggle>();
            toggle.targetGraphic = trackImg;
            toggle.graphic       = thumbImg2;
            toggle.isOn          = true;
            toggle.onValueChanged.AddListener(on =>
            {
                trackImg.color = on ? ORANGE : WHITE25;
                thumbRT2.anchoredPosition = new Vector2(
                    on ? (S(TOGGLE_W) - thumbSz * 0.5f - S(3f)) : (thumbSz * 0.5f + S(3f)), 0f);
                PlayerPrefs.SetInt("Setting_" + iconName + "_on", on ? 1 : 0);
                if (AudioManager.Instance != null)
                {
                    if (iconName == "music" && AudioManager.Instance.musicSource != null)
                        AudioManager.Instance.musicSource.mute = !on;
                    else if (iconName == "sfx" && AudioManager.Instance.sfxSource != null)
                        AudioManager.Instance.sfxSource.mute = !on;
                }
            });

            return slider;
        }

        return null;
    }

    // ── Row: Toggle ───────────────────────────────────────────────────────────

    Toggle RowToggle(Transform parent, string label, string sub, string iconName,
                     Color tint, bool defaultOn, string locKey = null, string locSubKey = null)
    {
        var row = MakeRow(parent, label, sub, iconName, tint, false, locKey, locSubKey);

        // Toggle container — right-anchored with consistent inset from card edge
        var toggleGO = MakeGO("Toggle", row.transform);
        var toggleRT = toggleGO.GetComponent<RectTransform>();
        toggleRT.anchorMin = new Vector2(1f, 0.5f);
        toggleRT.anchorMax = new Vector2(1f, 0.5f);
        toggleRT.pivot     = new Vector2(1f, 0.5f);
        toggleRT.anchoredPosition = new Vector2(-S(CTRL_INSET), 0f);
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
                  Color tint, string[] options, int defaultIndex,
                  string locKey = null, string locSubKey = null, string[] locChipKeys = null)
    {
        var row = MakeRow(parent, label, sub, iconName, tint, false, locKey, locSubKey);

        // Chips container (right side)
        var chipsGO = MakeGO("Chips", row.transform);
        var chipsRT = chipsGO.GetComponent<RectTransform>();
        float totalW = options.Length * S(CHIP_W) + (options.Length - 1) * S(CHIP_GAP);
        chipsRT.anchorMin = new Vector2(1f, 0.5f);
        chipsRT.anchorMax = new Vector2(1f, 0.5f);
        chipsRT.pivot     = new Vector2(1f, 0.5f);
        chipsRT.anchoredPosition = new Vector2(-S(CTRL_INSET), 0f);
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
            if (locChipKeys != null && i < locChipKeys.Length && locChipKeys[i] != null)
                Loc(lbl, locChipKeys[i]);

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
                if (iconName == "language" && LocalizationManager.Instance != null)
                    LocalizationManager.Instance.SetLanguageByIndex(idx);
                if (iconName == "graphics")
                    GraphicsQualityManager.Apply(idx);
            });
        }
    }

    // ── Row: Chevron (leaderboard / navigate) ─────────────────────────────────

    Button RowChevron(Transform parent, string label, string sub, string iconName, Color tint,
                       string locKey = null, string locSubKey = null)
    {
        var row = MakeRow(parent, label, sub, iconName, tint, true, locKey, locSubKey);

        // Chevron "›" on the right
        var chevGO  = MakeGO("Chevron", row.transform);
        var chevRT  = chevGO.GetComponent<RectTransform>();
        chevRT.anchorMin = new Vector2(1f, 0.5f);
        chevRT.anchorMax = new Vector2(1f, 0.5f);
        chevRT.pivot     = new Vector2(1f, 0.5f);
        chevRT.anchoredPosition = new Vector2(-S(CTRL_INSET), 0f);
        chevRT.sizeDelta = new Vector2(S(24f), S(28f));
        var chevTMP = chevGO.AddComponent<TextMeshProUGUI>();
        chevTMP.text      = "›";
        chevTMP.fontSize  = S(26f);
        chevTMP.color     = WHITE40;
        chevTMP.alignment = TextAlignmentOptions.Center;
        chevTMP.raycastTarget = false;

        return row.GetComponent<Button>() ?? row.AddComponent<Button>();
    }

    // ── Shared row builder ────────────────────────────────────────────────────
    // Creates the row background, icon bubble, and label/sub-label.
    // Returns the row GO so callers can add their control widget.

    GameObject MakeRow(Transform parent, string label, string sub, string iconName, Color tint,
                        bool addButton = true, string locLabelKey = null, string locSubKey = null)
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

        // ── Icon bubble — left-anchored with consistent padding ──
        float leftPad  = S(16f);                     // padding from card edge to bubble
        float bubbleW  = S(ICON_BOX);
        float bodyLeft = leftPad + bubbleW + S(14f); // left edge of text body

        var bubble   = MakeGO("IconBubble", row.transform);
        var bubbleRT = bubble.GetComponent<RectTransform>();
        bubbleRT.anchorMin = new Vector2(0f, 0.5f);
        bubbleRT.anchorMax = new Vector2(0f, 0.5f);
        bubbleRT.pivot     = new Vector2(0f, 0.5f);
        bubbleRT.anchoredPosition = new Vector2(leftPad, 0f);
        bubbleRT.sizeDelta = new Vector2(bubbleW, bubbleW);
        var bubbleImg = bubble.AddComponent<Image>();
        bubbleImg.color = new Color(1f, 1f, 1f, 0.12f);
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

        // ── Row body — text area between bubble and control widget ──
        // Right offset leaves room for the control: toggle (TOGGLE_W) + CTRL_INSET on each side
        float ctrlAreaW = S(TOGGLE_W) + S(CTRL_INSET) * 2f;

        var body   = MakeGO("RowBody", row.transform);
        var bodyRT = body.GetComponent<RectTransform>();
        bodyRT.anchorMin = new Vector2(0f, 0f);
        bodyRT.anchorMax = new Vector2(1f, 1f);
        bodyRT.offsetMin = new Vector2(bodyLeft, 0f);
        bodyRT.offsetMax = new Vector2(-ctrlAreaW, 0f);

        // Main label
        var lblGO  = MakeGO("Label", body.transform);
        var lblRT  = lblGO.GetComponent<RectTransform>();
        bool hasSub = !string.IsNullOrEmpty(sub);
        lblRT.anchorMin = new Vector2(0f, hasSub ? 0.5f : 0f);
        lblRT.anchorMax = Vector2.one;
        lblRT.offsetMin = lblRT.offsetMax = Vector2.zero;
        var lblTMP = lblGO.AddComponent<TextMeshProUGUI>();
        lblTMP.text      = label;
        lblTMP.fontSize  = S(17f);
        lblTMP.fontStyle = FontStyles.Bold;
        lblTMP.color     = WHITE;
        lblTMP.alignment = hasSub ? TextAlignmentOptions.BottomLeft : TextAlignmentOptions.Left;
        lblTMP.overflowMode = TextOverflowModes.Ellipsis;
        lblTMP.raycastTarget = false;
        if (locLabelKey != null) Loc(lblGO, locLabelKey);

        // Sub-label
        if (hasSub)
        {
            var subGO  = MakeGO("Sub", body.transform);
            var subRT  = subGO.GetComponent<RectTransform>();
            subRT.anchorMin = Vector2.zero;
            subRT.anchorMax = new Vector2(1f, 0.5f);
            subRT.offsetMin = subRT.offsetMax = Vector2.zero;
            var subTMP = subGO.AddComponent<TextMeshProUGUI>();
            subTMP.text      = sub;
            subTMP.fontSize  = S(13f);
            subTMP.color     = WHITE40;
            subTMP.alignment = TextAlignmentOptions.TopLeft;
            subTMP.overflowMode = TextOverflowModes.Ellipsis;
            subTMP.raycastTarget = false;
            if (locSubKey != null) Loc(subGO, locSubKey);
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

        // Red border overlay (inset 1px to simulate CSS border)
        var border = MakeGO("Border", go.transform);
        var borderRT = border.GetComponent<RectTransform>();
        borderRT.anchorMin = Vector2.zero; borderRT.anchorMax = Vector2.one;
        borderRT.offsetMin = new Vector2(1f, 1f); borderRT.offsetMax = new Vector2(-1f, -1f);
        var bImg = border.AddComponent<Image>();
        bImg.color = RED_BORDER; Rounded(bImg);
        bImg.raycastTarget = false;

        // Icon + text in a centred horizontal layout
        var rowGO = MakeGO("Content", go.transform);
        StretchFill(rowGO);
        var hl = rowGO.AddComponent<HorizontalLayoutGroup>();
        hl.childAlignment        = TextAnchor.MiddleCenter;
        hl.spacing               = S(10f);
        hl.childControlWidth     = false;
        hl.childControlHeight    = false;
        hl.childForceExpandWidth = false;
        hl.childForceExpandHeight = false;

        // Reset icon
        var iconGO  = MakeGO("ResetIcon", rowGO.transform);
        var iconRT  = iconGO.GetComponent<RectTransform>();
        iconRT.sizeDelta = new Vector2(S(30f), S(30f));
        var iconImg = iconGO.AddComponent<Image>();
        iconImg.preserveAspect = true;
        iconImg.raycastTarget  = false;
        iconImg.color          = RED_TEXT;
        var resetSprite = Icon("reset");
        if (resetSprite != null) iconImg.sprite = resetSprite;

        var lbl = MakeGO("Label", rowGO.transform);
        var lblRT = lbl.GetComponent<RectTransform>();
        lblRT.sizeDelta = new Vector2(S(200f), S(RESET_H));
        var lTMP = lbl.AddComponent<TextMeshProUGUI>();
        lTMP.text      = LocalizationManager.T("ui.settings.reset_progress");
        lTMP.fontSize  = S(18f);
        lTMP.fontStyle = FontStyles.Bold;
        lTMP.color     = RED_TEXT;
        lTMP.alignment = TextAlignmentOptions.MidlineLeft;
        lTMP.raycastTarget = false;
        Loc(lbl, "ui.settings.reset_progress");

        var btn = go.AddComponent<Button>();
        btn.targetGraphic = img;
        var cols = btn.colors;
        cols.normalColor      = Color.white;
        cols.highlightedColor = new Color(1.1f, 0.94f, 0.94f, 1f);
        cols.pressedColor     = new Color(0.85f, 0.85f, 0.85f, 1f);
        btn.colors = cols;

        // Wire reset action directly so it always works
        btn.onClick.AddListener(() =>
        {
            var mm = FindObjectOfType<MainMenuManager>();
            if (mm != null) mm.ResetSavedLevelProgress();
        });

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

    /// <summary>Attach a LocalizedText component to a GO that already has TextMeshProUGUI.</summary>
    static void Loc(GameObject go, string key)
    {
        var lt = go.AddComponent<LocalizedText>();
        lt.locKey = key;
    }

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
            const int sz = 128, r = 32, b = 32;
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
