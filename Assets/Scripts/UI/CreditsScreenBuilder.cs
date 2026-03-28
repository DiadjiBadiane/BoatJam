// Assets/Scripts/UI/CreditsScreenBuilder.cs
//
// Builds the Boat Jam Credits screen entirely in code.
// Exact same visual language as HomeScreenBuilder & SettingsScreenBuilder.
//
// ── SETUP ────────────────────────────────────────────────────────────────────
//   1. Attach to the same Canvas GameObject as HomeScreenBuilder.
//   2. Panel is built in Awake (hidden), wired to MainMenuManager in Start.
//   3. Right-click the component → "Build Credits Screen" to rebuild in Editor.
//
// ── ICONS (Assets/Resources/Icons/) ─────────────────────────────────────────
//   back.png          – left arrow / chevron  (shared with SettingsScreenBuilder)
//   team.png          – group / people icon
//   tools.png         – wrench / hammer icon
//   thanks.png        – heart / sparkle icon
//   social.png        – globe / network icon
//   twitter.png       – X / bird logo
//   instagram.png     – camera / Instagram logo
//   discord.png       – Discord bubble logo
//
//   All PNGs: white-on-transparent, Texture Type = "Sprite (2D and UI)".
//   Missing icons degrade gracefully — coloured bubbles show instead.
// ─────────────────────────────────────────────────────────────────────────────

using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class CreditsScreenBuilder : MonoBehaviour
{
    // ── Sprite loader ─────────────────────────────────────────────────────────
    static Sprite Icon(string name) => Resources.Load<Sprite>($"Icons/{name}");

    // ── Layout ────────────────────────────────────────────────────────────────
    const float SIDE_PAD  = 28f;
    const float TOP_PAD   = 56f;
    const float HDR_H     = 56f;
    const float SEC_LBL_H = 28f;
    const float SEC_GAP   = 20f;
    const float ROW_H     = 70f;   // credit row height
    const float TOOL_H    = 60f;   // tool row height
    const float VERSION_H = 20f;
    const float BTM_PAD   = 40f;

    // ── Colors ────────────────────────────────────────────────────────────────
    static readonly Color SKY_TOP   = Hex("0ea5e9");
    static readonly Color SKY_MID   = Hex("0284c7");
    static readonly Color SEA_MID   = Hex("1e3a5f");
    static readonly Color ORANGE    = new Color(0.96f, 0.62f, 0.07f, 1f);
    static readonly Color GLASS_BRD = new Color(1f, 1f, 1f, 0.22f);
    static readonly Color GLASS_FILL= new Color(1f, 1f, 1f, 0.10f);
    static readonly Color CLOUD_CLR = new Color(1f, 1f, 1f, 0.18f);
    static readonly Color GLINT_CLR = new Color(1f, 0.863f, 0.314f, 0.6f);
    static readonly Color WHITE     = Color.white;
    static readonly Color WHITE65   = new Color(1f, 1f, 1f, 0.65f);
    static readonly Color WHITE40   = new Color(1f, 1f, 1f, 0.40f);
    static readonly Color WHITE25   = new Color(1f, 1f, 1f, 0.25f);
    static readonly Color WHITE12   = new Color(1f, 1f, 1f, 0.12f);
    static readonly Color WHITE06   = new Color(1f, 1f, 1f, 0.06f);

    // Badge palette
    static readonly Color BADGE_ORANGE_BG   = new Color(0.96f, 0.62f, 0.07f, 0.18f);
    static readonly Color BADGE_ORANGE_BRD  = new Color(0.96f, 0.62f, 0.07f, 0.35f);
    static readonly Color BADGE_ORANGE_TXT  = new Color(0.98f, 0.75f, 0.14f, 1f);
    static readonly Color BADGE_BLUE_BG     = new Color(0.39f, 0.40f, 0.95f, 0.18f);
    static readonly Color BADGE_BLUE_BRD    = new Color(0.39f, 0.40f, 0.95f, 0.35f);
    static readonly Color BADGE_BLUE_TXT    = new Color(0.65f, 0.71f, 0.99f, 1f);
    static readonly Color BADGE_TEAL_BG     = new Color(0.08f, 0.72f, 0.65f, 0.18f);
    static readonly Color BADGE_TEAL_BRD    = new Color(0.08f, 0.72f, 0.65f, 0.35f);
    static readonly Color BADGE_TEAL_TXT    = new Color(0.37f, 0.92f, 0.83f, 1f);
    static readonly Color BADGE_PINK_BG     = new Color(0.93f, 0.28f, 0.60f, 0.18f);
    static readonly Color BADGE_PINK_BRD    = new Color(0.93f, 0.28f, 0.60f, 0.35f);
    static readonly Color BADGE_PINK_TXT    = new Color(0.98f, 0.66f, 0.83f, 1f);

    // Thanks card accent
    static readonly Color THANKS_BG  = new Color(0.96f, 0.62f, 0.07f, 0.12f);
    static readonly Color THANKS_BRD = new Color(0.96f, 0.62f, 0.07f, 0.30f);

    // Avatar bubble tints
    static readonly Color AVT_ORANGE = new Color(0.96f, 0.62f, 0.07f, 0.20f);
    static readonly Color AVT_BLUE   = new Color(0.39f, 0.40f, 0.95f, 0.20f);
    static readonly Color AVT_TEAL   = new Color(0.08f, 0.72f, 0.65f, 0.20f);
    static readonly Color AVT_PINK   = new Color(0.93f, 0.28f, 0.60f, 0.20f);

    // ── Public reference for MainMenuManager ──────────────────────────────────
    [HideInInspector] public Button backButton;

    // ─────────────────────────────────────────────────────────────────────────


    /// Called by MainMenuManager.OpenCredits() — rebuilds if the panel is missing or incomplete.
    public GameObject EnsureBuilt()
    {
        var panel = transform.Find("CreditsPanel")?.gameObject;
        var content = panel?.transform.Find("ScrollView/Viewport/Content");
        bool hasExpectedHierarchy = content != null
            && content.Find("Header") != null
            && content.Find("Hero") != null
            && content.childCount >= 8;
        bool needsBuild = panel == null || !hasExpectedHierarchy;
        if (!needsBuild)
            return panel;

        Build();
        WireToManager();
        return transform.Find("CreditsPanel")?.gameObject;
    }

    void Awake()
    {
        if (Application.isPlaying)
            Build();
    }

    void Start() => WireToManager();

    public void WireToManager()
    {
        var mm = FindObjectOfType<MainMenuManager>();
        if (mm == null) { Debug.LogWarning("[CreditsScreenBuilder] MainMenuManager not found."); return; }
        var panel = transform.Find("CreditsPanel")?.gameObject;
        if (panel == null) { Debug.LogWarning("[CreditsScreenBuilder] CreditsPanel not found."); return; }
        mm.creditsPanel       = panel;
        mm.creditsCloseButton = backButton;
        mm.RebindButtonListeners();
        Debug.Log("[CreditsScreenBuilder] Wired to MainMenuManager.");
    }

    [ContextMenu("Build Credits Screen")]
    public void Build()
    {
        var old = transform.Find("CreditsPanel");
        if (old != null)
        {
            if (Application.isPlaying)
                Destroy(old.gameObject);
            else
                DestroyImmediate(old.gameObject);
        }

        var panel = MakeGO("CreditsPanel", transform);
        Stretch(panel);
        panel.SetActive(false);

        BuildBackground(panel.transform);
        BuildScrollContent(panel.transform);
    }

    // ── Background ────────────────────────────────────────────────────────────

    void BuildBackground(Transform root)
    {
        var bg = MakeGO("Background", root);
        Stretch(bg);

        Layer("Sky",     bg.transform, 0.18f, 1.0f,  SKY_TOP);
        Layer("Sea_Mid", bg.transform, 0.00f, 0.26f, SKY_MID);
        Layer("Sea_Bot", bg.transform, 0.00f, 0.09f, SEA_MID);

        MakeCloud(bg.transform, "Cloud1", 120f, 34f, -200f, 0.90f, 24f,   0f);
        MakeCloud(bg.transform, "Cloud2",  80f, 24f, -150f, 0.84f, 32f, -10f);
        MakeCloud(bg.transform, "Cloud3", 100f, 30f,   80f, 0.92f, 28f, -16f);

        MakeWave(bg.transform, "Wave1", 0.24f, 60f, new Color(0.22f, 0.74f, 0.98f, 0.18f), 4f,  0f);
        MakeWave(bg.transform, "Wave2", 0.21f, 80f, new Color(0.05f, 0.65f, 0.91f, 0.18f), 5f, -1f);

        // Glint
        var g  = MakeGO("Glint", bg.transform);
        var gr = g.GetComponent<RectTransform>();
        gr.anchorMin = new Vector2(0.5f, 0.22f); gr.anchorMax = new Vector2(0.5f, 0.22f);
        gr.pivot     = new Vector2(0.5f, 0.5f);  gr.sizeDelta = new Vector2(180f, 6f);
        var gi = g.AddComponent<Image>(); gi.color = GLINT_CLR; Rounded(gi);
        g.AddComponent<GlintPulse>();

        MakeDecoBoat(bg.transform, "Deco1", "⛵", 22f, new Vector2(0.07f, 0.12f), 4f,  0f);
        MakeDecoBoat(bg.transform, "Deco2", "🚤", 17f, new Vector2(0.88f, 0.09f), 5f, -2f);
    }

    // ── Scroll content ────────────────────────────────────────────────────────

    void BuildScrollContent(Transform root)
    {
        // ScrollView
        var sv    = MakeGO("ScrollView", root);
        Stretch(sv);
        sv.AddComponent<Image>().color = Color.clear;
        var scroll = sv.AddComponent<ScrollRect>();
        scroll.horizontal        = false;
        scroll.scrollSensitivity = 30f;
        scroll.movementType      = ScrollRect.MovementType.Clamped;

        // Viewport
        var vp    = MakeGO("Viewport", sv.transform);
        Stretch(vp);
        var vpImg = vp.AddComponent<Image>();
        vpImg.color = new Color(1f, 1f, 1f, 0.001f);
        vp.AddComponent<RectMask2D>();
        scroll.viewport = vp.GetComponent<RectTransform>();

        // Content
        var content = MakeGO("Content", vp.transform);
        var crt     = content.GetComponent<RectTransform>();
        crt.anchorMin = new Vector2(0f, 1f);
        crt.anchorMax = new Vector2(1f, 1f);
        crt.pivot     = new Vector2(0.5f, 1f);
        crt.offsetMin = crt.offsetMax = Vector2.zero;
        scroll.content = crt;

        var csf = content.AddComponent<ContentSizeFitter>();
        csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        var vl = content.AddComponent<VerticalLayoutGroup>();
        vl.childAlignment        = TextAnchor.UpperCenter;
        vl.spacing               = 0f;
        vl.padding               = new RectOffset(
            Mathf.RoundToInt(SIDE_PAD), Mathf.RoundToInt(SIDE_PAD),
            Mathf.RoundToInt(TOP_PAD),  Mathf.RoundToInt(BTM_PAD));
        vl.childControlWidth      = true;
        vl.childControlHeight     = true;
        vl.childForceExpandWidth  = true;
        vl.childForceExpandHeight = false;

        // ── Header ────────────────────────────────────────────────────────────
        BuildHeader(content.transform);

        // ── Hero ──────────────────────────────────────────────────────────────
        BuildHero(content.transform);

        // ── Team ──────────────────────────────────────────────────────────────
        SectionLabel(content.transform, "👥   TEAM");
        var teamCard = Card(content.transform);
        CreditRow(teamCard.transform, "🧑‍💻", AVT_ORANGE, "Your Name",     "Game Design & Development", "LEAD", BADGE_ORANGE_BG, BADGE_ORANGE_BRD, BADGE_ORANGE_TXT, true);
        Divider(teamCard.transform);
        CreditRow(teamCard.transform, "🎨",   AVT_BLUE,   "Artist Name",   "UI & Visual Design",        "ART",  BADGE_BLUE_BG,   BADGE_BLUE_BRD,   BADGE_BLUE_TXT,   false);
        Divider(teamCard.transform);
        CreditRow(teamCard.transform, "🎵",   AVT_TEAL,   "Composer Name", "Music & Sound Effects",     "AUDIO",BADGE_TEAL_BG,   BADGE_TEAL_BRD,   BADGE_TEAL_TXT,   false);
        Divider(teamCard.transform);
        CreditRow(teamCard.transform, "🧪",   AVT_PINK,   "Tester Name",   "QA & Playtesting",          "QA",   BADGE_PINK_BG,   BADGE_PINK_BRD,   BADGE_PINK_TXT,   false);

        // ── Tools & Assets ────────────────────────────────────────────────────
        SectionLabel(content.transform, "🛠   TOOLS & ASSETS");
        var toolCard = Card(content.transform);
        ToolRow(toolCard.transform, "🎮", "Unity 6",            "Game engine",                    true);
        Divider(toolCard.transform);
        ToolRow(toolCard.transform, "⚓", "25 Boats And Ships",  "3D boat asset pack",             false);
        Divider(toolCard.transform);
        ToolRow(toolCard.transform, "✍️", "TextMesh Pro",        "UI typography",                  false);
        Divider(toolCard.transform);
        ToolRow(toolCard.transform, "🔊", "Freesound.org",       "Sound effects library",          false);
        Divider(toolCard.transform);
        ToolRow(toolCard.transform, "🎨", "Google Fonts",        "Righteous & Nunito typefaces",   false);

        // ── Special Thanks ────────────────────────────────────────────────────
        SectionLabel(content.transform, "✨   SPECIAL THANKS");
        BuildThanksCard(content.transform);

        // ── Socials ───────────────────────────────────────────────────────────
        SectionLabel(content.transform, "🌐   FIND US");
        BuildSocials(content.transform);

        // ── Footer ────────────────────────────────────────────────────────────
        Gap(content.transform, 24f);
        BuildFooter(content.transform);
    }

    // ── Header ────────────────────────────────────────────────────────────────

    void BuildHeader(Transform parent)
    {
        var hdr = MakeGO("Header", parent);
        LE(hdr, HDR_H);
        var hl = hdr.AddComponent<HorizontalLayoutGroup>();
        hl.childAlignment        = TextAnchor.MiddleCenter;
        hl.childControlWidth     = false;
        hl.childControlHeight    = true;
        hl.childForceExpandWidth  = false;
        hl.childForceExpandHeight = true;
        hl.spacing = 0f;

        // Back button
        var backGO  = MakeGO("BackButton", hdr.transform);
        var backLE  = backGO.AddComponent<LayoutElement>();
        backLE.preferredWidth = 48f; backLE.flexibleWidth = 0f;
        var backImg = backGO.AddComponent<Image>(); backImg.color = WHITE12; Rounded(backImg);
        backButton = backGO.AddComponent<Button>();
        TintBtn(backButton, WHITE25, WHITE06);

        // Back icon or fallback glyph
        var backSp = Icon("back");
        if (backSp != null)
        {
            var iconGO  = MakeGO("Icon", backGO.transform);
            var iconRT  = iconGO.GetComponent<RectTransform>();
            iconRT.anchorMin = iconRT.anchorMax = new Vector2(0.5f, 0.5f);
            iconRT.pivot = new Vector2(0.5f, 0.5f); iconRT.sizeDelta = new Vector2(22f, 22f);
            var iconImg = iconGO.AddComponent<Image>();
            iconImg.sprite = backSp; iconImg.color = WHITE; iconImg.preserveAspect = true;
            iconImg.raycastTarget = false;
        }
        else
        {
            var bTxt = MakeGO("Label", backGO.transform); StretchFill(bTxt);
            var bTMP = bTxt.AddComponent<TextMeshProUGUI>();
            bTMP.text = "‹"; bTMP.fontSize = 32f; bTMP.color = WHITE;
            bTMP.alignment = TextAlignmentOptions.Center; bTMP.raycastTarget = false;
        }

        // Title
        var titleGO  = MakeGO("Title", hdr.transform);
        var titleLE  = titleGO.AddComponent<LayoutElement>(); titleLE.flexibleWidth = 1f;
        var titleTMP = titleGO.AddComponent<TextMeshProUGUI>();
        titleTMP.text         = "CREDITS";
        titleTMP.fontSize     = 26f;
        titleTMP.fontStyle    = FontStyles.Bold;
        titleTMP.color        = WHITE;
        titleTMP.alignment    = TextAlignmentOptions.Center;
        if (titleTMP.font == null && TMP_Settings.defaultFontAsset != null)
            titleTMP.font = TMP_Settings.defaultFontAsset;
        SafeSetOutline(titleTMP, 0.2f, new Color32(3, 105, 161, 255));

        // Ghost spacer (keeps title centred)
        var ghost   = MakeGO("Ghost", hdr.transform);
        var ghostLE = ghost.AddComponent<LayoutElement>();
        ghostLE.preferredWidth = 48f; ghostLE.flexibleWidth = 0f;
    }

    // ── Hero (boat + title + tagline) ─────────────────────────────────────────

    void BuildHero(Transform parent)
    {
        Gap(parent, 8f);
        var hero = MakeGO("Hero", parent);
        LE(hero, 130f);

        var vl = hero.AddComponent<VerticalLayoutGroup>();
        vl.childAlignment        = TextAnchor.MiddleCenter;
        vl.spacing               = 4f;
        vl.childControlWidth     = true;
        vl.childControlHeight    = false;
        vl.childForceExpandWidth = true;
        vl.childForceExpandHeight = false;

        // Boat icon
        var boatGO  = MakeGO("Boat", hero.transform); LE(boatGO, 68f);
        var boatTMP = boatGO.AddComponent<TextMeshProUGUI>();
        boatTMP.text = "⛵"; boatTMP.fontSize = 56f;
        boatTMP.alignment = TextAlignmentOptions.Center; boatTMP.color = WHITE;
        var bob = boatGO.AddComponent<Bobber>(); bob.amplitude = 7f; bob.tiltDeg = 3f; bob.duration = 3f;

        // Game name
        var nameGO  = MakeGO("GameName", hero.transform); LE(nameGO, 36f);
        var nameTMP = nameGO.AddComponent<TextMeshProUGUI>();
        nameTMP.text         = "BOAT JAM";
        nameTMP.fontSize     = 32f;
        nameTMP.fontStyle    = FontStyles.Bold;
        nameTMP.color        = WHITE;
        nameTMP.alignment    = TextAlignmentOptions.Center;
        if (nameTMP.font == null && TMP_Settings.defaultFontAsset != null)
            nameTMP.font = TMP_Settings.defaultFontAsset;
        SafeSetOutline(nameTMP, 0.22f, new Color32(3, 105, 161, 255));

        // Tagline
        var tagGO  = MakeGO("Tagline", hero.transform); LE(tagGO, 20f);
        var tagTMP = tagGO.AddComponent<TextMeshProUGUI>();
        tagTMP.text            = "HARBOR ESCAPE";
        tagTMP.fontSize        = 12f;
        tagTMP.fontStyle       = FontStyles.Bold;
        tagTMP.color           = WHITE65;
        tagTMP.alignment       = TextAlignmentOptions.Center;
        tagTMP.characterSpacing = 4f;
    }

    // ── Section label ─────────────────────────────────────────────────────────

    void SectionLabel(Transform parent, string text)
    {
        Gap(parent, SEC_GAP);
        var go  = MakeGO("Section_" + text, parent);
        LE(go, SEC_LBL_H);
        var tmp = go.AddComponent<TextMeshProUGUI>();
        tmp.text            = text;
        tmp.fontSize        = 11f;
        tmp.fontStyle       = FontStyles.Bold;
        tmp.color           = WHITE40;
        tmp.alignment       = TextAlignmentOptions.Left;
        tmp.characterSpacing = 3f;
    }

    // ── Card container ────────────────────────────────────────────────────────

    GameObject Card(Transform parent)
    {
        Gap(parent, 6f);
        var go  = MakeGO("Card", parent);
        var le  = go.AddComponent<LayoutElement>();
        le.flexibleHeight = 0f;

        go.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        var vl = go.AddComponent<VerticalLayoutGroup>();
        vl.childAlignment         = TextAnchor.UpperCenter;
        vl.spacing                = 0f;
        vl.childControlWidth      = true;
        vl.childControlHeight     = true;
        vl.childForceExpandWidth  = true;
        vl.childForceExpandHeight = false;

        var img = go.AddComponent<Image>(); img.color = GLASS_FILL; Rounded(img);
        // Border overlay
        var bdr = MakeGO("Border", go.transform); Stretch(bdr);
        bdr.transform.SetAsFirstSibling();
        var bImg = bdr.AddComponent<Image>(); bImg.color = GLASS_BRD; Rounded(bImg);
        bImg.raycastTarget = false;
        return go;
    }

    // ── Divider ───────────────────────────────────────────────────────────────

    static void Divider(Transform parent)
    {
        var go  = MakeGO("Divider", parent); LE(go, 1f);
        var img = go.AddComponent<Image>();
        img.color = new Color(1f, 1f, 1f, 0.10f); img.raycastTarget = false;
    }

    // ── Credit row (team member) ──────────────────────────────────────────────

    void CreditRow(Transform parent,
                   string emoji, Color avatarTint,
                   string name,  string role,
                   string badgeText,
                   Color badgeBg, Color badgeBrd, Color badgeTxt,
                   bool isFirst)
    {
        var row   = MakeGO("CreditRow_" + name, parent);
        LE(row, ROW_H);

        // Subtle press feedback
        var rowImg = row.AddComponent<Image>(); rowImg.color = Color.clear;
        var rowBtn = row.AddComponent<Button>(); rowBtn.targetGraphic = rowImg;
        rowBtn.transition = Selectable.Transition.ColorTint;
        var rc = rowBtn.colors;
        rc.normalColor = Color.clear; rc.highlightedColor = new Color(1f,1f,1f,0.06f);
        rc.pressedColor = new Color(1f,1f,1f,0.12f); rowBtn.colors = rc;

        // Avatar circle
        var ava   = MakeGO("Avatar", row.transform);
        var avaRT = ava.GetComponent<RectTransform>();
        avaRT.anchorMin = new Vector2(0f, 0.5f); avaRT.anchorMax = new Vector2(0f, 0.5f);
        avaRT.pivot     = new Vector2(0f, 0.5f);
        avaRT.anchoredPosition = new Vector2(14f, 0f);
        avaRT.sizeDelta = new Vector2(42f, 42f);
        var avaImg = ava.AddComponent<Image>();
        avaImg.color = avatarTint;
        // Make it a circle via a round sprite
        avaImg.sprite = GetCircleSprite(); avaImg.type = Image.Type.Simple;
        avaImg.raycastTarget = false;

        // Border ring on avatar
        var avaBrd = MakeGO("Ring", ava.transform); StretchFill(avaBrd);
        var avaBrdImg = avaBrd.AddComponent<Image>();
        avaBrdImg.color = WHITE25;
        avaBrdImg.sprite = GetCircleSprite(); avaBrdImg.type = Image.Type.Simple;
        avaBrdImg.raycastTarget = false;

        // Emoji inside avatar
        var emoGO  = MakeGO("Emoji", ava.transform); StretchFill(emoGO);
        var emoTMP = emoGO.AddComponent<TextMeshProUGUI>();
        emoTMP.text = emoji; emoTMP.fontSize = 20f;
        emoTMP.alignment = TextAlignmentOptions.Center;
        emoTMP.raycastTarget = false;

        // Body
        var body   = MakeGO("Body", row.transform);
        var bodyRT = body.GetComponent<RectTransform>();
        bodyRT.anchorMin = new Vector2(0f, 0f); bodyRT.anchorMax = new Vector2(1f, 1f);
        bodyRT.offsetMin = new Vector2(14f + 42f + 12f, 0f);
        bodyRT.offsetMax = new Vector2(-80f, 0f);

        var nameGO  = MakeGO("Name", body.transform);
        var nameRT  = nameGO.GetComponent<RectTransform>();
        nameRT.anchorMin = new Vector2(0f, 0.5f); nameRT.anchorMax = Vector2.one;
        nameRT.offsetMin = nameRT.offsetMax = Vector2.zero;
        var nameTMP = nameGO.AddComponent<TextMeshProUGUI>();
        nameTMP.text = name; nameTMP.fontSize = 15f; nameTMP.fontStyle = FontStyles.Bold;
        nameTMP.color = WHITE; nameTMP.alignment = TextAlignmentOptions.Left;
        nameTMP.raycastTarget = false;

        var roleGO  = MakeGO("Role", body.transform);
        var roleRT  = roleGO.GetComponent<RectTransform>();
        roleRT.anchorMin = Vector2.zero; roleRT.anchorMax = new Vector2(1f, 0.5f);
        roleRT.offsetMin = roleRT.offsetMax = Vector2.zero;
        var roleTMP = roleGO.AddComponent<TextMeshProUGUI>();
        roleTMP.text = role; roleTMP.fontSize = 11f;
        roleTMP.color = WHITE40; roleTMP.alignment = TextAlignmentOptions.Left;
        roleTMP.raycastTarget = false;

        // Badge
        var badge   = MakeGO("Badge", row.transform);
        var badgeRT = badge.GetComponent<RectTransform>();
        badgeRT.anchorMin = new Vector2(1f, 0.5f); badgeRT.anchorMax = new Vector2(1f, 0.5f);
        badgeRT.pivot     = new Vector2(1f, 0.5f);
        badgeRT.anchoredPosition = new Vector2(-14f, 0f);
        badgeRT.sizeDelta = new Vector2(52f, 24f);
        var badgeImg = badge.AddComponent<Image>(); badgeImg.color = badgeBg; Rounded(badgeImg);

        // Badge border
        var badgeBrdGO = MakeGO("Border", badge.transform); StretchFill(badgeBrdGO);
        var badgeBrdImg = badgeBrdGO.AddComponent<Image>();
        badgeBrdImg.color = badgeBrd; Rounded(badgeBrdImg); badgeBrdImg.raycastTarget = false;

        // Badge text
        var badgeLbl = MakeGO("Label", badge.transform); StretchFill(badgeLbl);
        var badgeTMP = badgeLbl.AddComponent<TextMeshProUGUI>();
        badgeTMP.text = badgeText; badgeTMP.fontSize = 10f; badgeTMP.fontStyle = FontStyles.Bold;
        badgeTMP.color = badgeTxt; badgeTMP.alignment = TextAlignmentOptions.Center;
        badgeTMP.characterSpacing = 1f; badgeTMP.raycastTarget = false;
    }

    // ── Tool row (asset / software) ───────────────────────────────────────────

    void ToolRow(Transform parent, string emoji, string toolName, string desc, bool isFirst)
    {
        var row   = MakeGO("Tool_" + toolName, parent);
        LE(row, TOOL_H);

        var rowImg = row.AddComponent<Image>(); rowImg.color = Color.clear;
        var rowBtn = row.AddComponent<Button>(); rowBtn.targetGraphic = rowImg;
        rowBtn.transition = Selectable.Transition.ColorTint;
        var rc = rowBtn.colors;
        rc.normalColor = Color.clear; rc.highlightedColor = new Color(1f,1f,1f,0.06f);
        rc.pressedColor = new Color(1f,1f,1f,0.12f); rowBtn.colors = rc;

        // Icon box
        var box   = MakeGO("IconBox", row.transform);
        var boxRT = box.GetComponent<RectTransform>();
        boxRT.anchorMin = new Vector2(0f, 0.5f); boxRT.anchorMax = new Vector2(0f, 0.5f);
        boxRT.pivot     = new Vector2(0f, 0.5f);
        boxRT.anchoredPosition = new Vector2(14f, 0f);
        boxRT.sizeDelta = new Vector2(36f, 36f);
        var boxImg = box.AddComponent<Image>(); boxImg.color = WHITE12; Rounded(boxImg);
        boxImg.raycastTarget = false;

        var emoGO  = MakeGO("Emoji", box.transform); StretchFill(emoGO);
        var emoTMP = emoGO.AddComponent<TextMeshProUGUI>();
        emoTMP.text = emoji; emoTMP.fontSize = 18f;
        emoTMP.alignment = TextAlignmentOptions.Center; emoTMP.raycastTarget = false;

        // Body
        var body   = MakeGO("Body", row.transform);
        var bodyRT = body.GetComponent<RectTransform>();
        bodyRT.anchorMin = new Vector2(0f, 0f); bodyRT.anchorMax = new Vector2(1f, 1f);
        bodyRT.offsetMin = new Vector2(14f + 36f + 12f, 0f);
        bodyRT.offsetMax = new Vector2(-14f, 0f);

        var nameGO  = MakeGO("Name", body.transform);
        var nameRT  = nameGO.GetComponent<RectTransform>();
        nameRT.anchorMin = new Vector2(0f, 0.5f); nameRT.anchorMax = Vector2.one;
        nameRT.offsetMin = nameRT.offsetMax = Vector2.zero;
        var nameTMP = nameGO.AddComponent<TextMeshProUGUI>();
        nameTMP.text = toolName; nameTMP.fontSize = 14f; nameTMP.fontStyle = FontStyles.Bold;
        nameTMP.color = WHITE; nameTMP.alignment = TextAlignmentOptions.Left;
        nameTMP.overflowMode = TextOverflowModes.Ellipsis; nameTMP.raycastTarget = false;

        var descGO  = MakeGO("Desc", body.transform);
        var descRT  = descGO.GetComponent<RectTransform>();
        descRT.anchorMin = Vector2.zero; descRT.anchorMax = new Vector2(1f, 0.5f);
        descRT.offsetMin = descRT.offsetMax = Vector2.zero;
        var descTMP = descGO.AddComponent<TextMeshProUGUI>();
        descTMP.text = desc; descTMP.fontSize = 11f;
        descTMP.color = WHITE40; descTMP.alignment = TextAlignmentOptions.Left;
        descTMP.overflowMode = TextOverflowModes.Ellipsis; descTMP.raycastTarget = false;
    }

    // ── Special Thanks card ───────────────────────────────────────────────────

    void BuildThanksCard(Transform parent)
    {
        Gap(parent, 6f);
        var go  = MakeGO("ThanksCard", parent);
        var le  = go.AddComponent<LayoutElement>();
        le.flexibleHeight = 0f;

        go.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        var vl = go.AddComponent<VerticalLayoutGroup>();
        vl.childAlignment        = TextAnchor.UpperCenter;
        vl.spacing               = 0f;
        vl.padding               = new RectOffset(24, 24, 22, 22);
        vl.childControlWidth     = true;
        vl.childControlHeight    = true;
        vl.childForceExpandWidth = true;
        vl.childForceExpandHeight = false;

        var bg = go.AddComponent<Image>(); bg.color = THANKS_BG; Rounded(bg);
        var brd = MakeGO("Border", go.transform); Stretch(brd);
        brd.transform.SetAsFirstSibling();
        var brdImg = brd.AddComponent<Image>(); brdImg.color = THANKS_BRD;
        Rounded(brdImg); brdImg.raycastTarget = false;

        // Boat emoji large
        var emoGO  = MakeGO("Emoji", go.transform); LE(emoGO, 52f);
        var emoTMP = emoGO.AddComponent<TextMeshProUGUI>();
        emoTMP.text = "🌊"; emoTMP.fontSize = 38f;
        emoTMP.alignment = TextAlignmentOptions.Center;

        Gap(go.transform, 4f);

        // Title
        var titleGO  = MakeGO("ThanksTitle", go.transform); LE(titleGO, 30f);
        var titleTMP = titleGO.AddComponent<TextMeshProUGUI>();
        titleTMP.text      = "To All Our Players";
        titleTMP.fontSize  = 20f;
        titleTMP.fontStyle = FontStyles.Bold;
        titleTMP.color     = new Color(0.98f, 0.75f, 0.14f, 1f);
        titleTMP.alignment = TextAlignmentOptions.Center;
        titleTMP.characterSpacing = 1f;

        Gap(go.transform, 8f);

        // Body text
        var bodyGO  = MakeGO("ThanksBody", go.transform); LE(bodyGO, 72f);
        var bodyTMP = bodyGO.AddComponent<TextMeshProUGUI>();
        bodyTMP.text      = "Thank you for sailing with us!\nEvery puzzle solved, every harbour\nescaped — you make it all worth it.\nFair winds, captain.  ⚓";
        bodyTMP.fontSize  = 13f;
        bodyTMP.color     = WHITE65;
        bodyTMP.alignment = TextAlignmentOptions.Center;
        bodyTMP.lineSpacing = 4f;
    }

    // ── Socials row ───────────────────────────────────────────────────────────

    void BuildSocials(Transform parent)
    {
        Gap(parent, 6f);
        var row   = MakeGO("Socials", parent);
        LE(row, 72f);

        var hl = row.AddComponent<HorizontalLayoutGroup>();
        hl.childAlignment        = TextAnchor.MiddleCenter;
        hl.spacing               = 12f;
        hl.childControlWidth     = false;
        hl.childControlHeight    = true;
        hl.childForceExpandWidth  = false;
        hl.childForceExpandHeight = true;

        SocialBtn(row.transform, "𝕏",   "TWITTER",   Icon("twitter"));
        SocialBtn(row.transform, "📸",  "INSTAGRAM", Icon("instagram"));
        SocialBtn(row.transform, "💬",  "DISCORD",   Icon("discord"));
    }

    void SocialBtn(Transform parent, string fallbackEmoji, string label, Sprite sprite)
    {
        var go   = MakeGO("Social_" + label, parent);
        var le   = go.AddComponent<LayoutElement>();
        le.preferredWidth = 90f; le.flexibleWidth = 1f;

        var img  = go.AddComponent<Image>(); img.color = GLASS_FILL; Rounded(img);
        var brd  = MakeGO("Border", go.transform); Stretch(brd);
        brd.transform.SetAsFirstSibling();
        var brdImg = brd.AddComponent<Image>(); brdImg.color = GLASS_BRD;
        Rounded(brdImg); brdImg.raycastTarget = false;

        var btn  = go.AddComponent<Button>(); btn.targetGraphic = img;
        TintBtn(btn, new Color(1f,1f,1f,0.20f), new Color(1f,1f,1f,0.06f));

        var vl = go.AddComponent<VerticalLayoutGroup>();
        vl.childAlignment        = TextAnchor.MiddleCenter;
        vl.spacing               = 4f;
        vl.padding               = new RectOffset(0, 0, 12, 10);
        vl.childControlWidth     = true;
        vl.childControlHeight    = false;
        vl.childForceExpandWidth = true;
        vl.childForceExpandHeight = false;

        // Icon or emoji
        var iconGO = MakeGO("Icon", go.transform); LE(iconGO, 28f);
        if (sprite != null)
        {
            var iconImg = iconGO.AddComponent<Image>();
            iconImg.sprite = sprite; iconImg.color = WHITE;
            iconImg.preserveAspect = true; iconImg.raycastTarget = false;
        }
        else
        {
            var iconTMP = iconGO.AddComponent<TextMeshProUGUI>();
            iconTMP.text = fallbackEmoji; iconTMP.fontSize = 22f;
            iconTMP.alignment = TextAlignmentOptions.Center; iconTMP.raycastTarget = false;
        }

        // Label
        var lblGO  = MakeGO("Label", go.transform); LE(lblGO, 16f);
        var lblTMP = lblGO.AddComponent<TextMeshProUGUI>();
        lblTMP.text            = label;
        lblTMP.fontSize        = 10f;
        lblTMP.fontStyle       = FontStyles.Bold;
        lblTMP.color           = WHITE40;
        lblTMP.alignment       = TextAlignmentOptions.Center;
        lblTMP.characterSpacing = 1f;
        lblTMP.raycastTarget   = false;
    }

    // ── Footer ────────────────────────────────────────────────────────────────

    void BuildFooter(Transform parent)
    {
        var copy = MakeGO("Copyright", parent); LE(copy, 18f);
        var cTMP = copy.AddComponent<TextMeshProUGUI>();
        cTMP.text      = "© 2025 Boat Jam — All rights reserved";
        cTMP.fontSize  = 11f;
        cTMP.fontStyle = FontStyles.Bold;
        cTMP.color     = new Color(1f, 1f, 1f, 0.35f);
        cTMP.alignment = TextAlignmentOptions.Center;

        Gap(parent, 4f);

        var ver = MakeGO("Version", parent); LE(ver, VERSION_H);
        var vTMP = ver.AddComponent<TextMeshProUGUI>();
        vTMP.text            = $"v{Application.version}";
        vTMP.fontSize        = 11f;
        vTMP.color           = new Color(1f, 1f, 1f, 0.22f);
        vTMP.alignment       = TextAlignmentOptions.Center;
        vTMP.characterSpacing = 2f;
    }

    // ── Background helpers ────────────────────────────────────────────────────

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
        rt.pivot     = new Vector2(0.5f,  0.5f);    rt.sizeDelta = new Vector2(0f, height);
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

    // ── Circle sprite (for avatars) ───────────────────────────────────────────

    static Sprite _circleSprite;
    static Sprite GetCircleSprite()
    {
        if (_circleSprite != null) return _circleSprite;
        const int sz = 128, r = 62;
        var tex = new Texture2D(sz, sz, TextureFormat.RGBA32, false);
        var px  = new Color32[sz * sz];
        float cx = sz * 0.5f, cy = sz * 0.5f;
        for (int y = 0; y < sz; y++)
        for (int x = 0; x < sz; x++)
        {
            float dx = x - cx, dy = y - cy;
            px[y * sz + x] = new Color32(255, 255, 255,
                (byte)(Mathf.Sqrt(dx*dx+dy*dy) <= r ? 255 : 0));
        }
        tex.SetPixels32(px); tex.Apply();
        _circleSprite = Sprite.Create(tex, new Rect(0,0,sz,sz), new Vector2(.5f,.5f), 100f);
        return _circleSprite;
    }

    // ── Generic UI helpers ────────────────────────────────────────────────────

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

    static void LE(GameObject go, float h)
    {
        var le = go.GetComponent<LayoutElement>() ?? go.AddComponent<LayoutElement>();
        le.preferredHeight = h;
    }

    static void Gap(Transform parent, float h)
    {
        var go = MakeGO("Gap", parent); LE(go, h);
    }

    static void TintBtn(Button btn, Color highlight, Color pressed)
    {
        var c = btn.colors;
        c.highlightedColor = highlight; c.pressedColor = pressed;
        btn.colors = c;
    }

    static void SafeSetOutline(TextMeshProUGUI tmp, float width, Color32 color)
    {
        if (tmp == null) return;

        try
        {
            if (tmp.font == null && TMP_Settings.defaultFontAsset != null)
                tmp.font = TMP_Settings.defaultFontAsset;

            if (tmp.fontSharedMaterial == null)
                return;

            tmp.outlineWidth = width;
            tmp.outlineColor = color;
        }
        catch (System.NullReferenceException)
        {
            // Some TMP setups can report a material, then still throw while setting outline.
            // In that case we skip outline styling instead of crashing the whole screen build.
        }
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
                int cx = Mathf.Clamp(x, r, sz-r-1);
                int cy = Mathf.Clamp(y, r, sz-r-1);
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