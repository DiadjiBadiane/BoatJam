// Assets/Scripts/UI/HomeScreenBuilder.cs
// Builds the entire Leave my Boat home screen in code, pixel-perfect to the HTML mockup.
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
    [Header("Artwork")]
    [SerializeField] Texture2D logoTexture;
    [SerializeField] Texture2D menuWaterTexture;
    [SerializeField] Material menuWaterMaterial;

    // ── Sprite loading ────────────────────────────────────────────────────────
    // Put your PNGs in Assets/Resources/Icons/ with these exact names:
    //   anchor.png, levels.png, settings.png, credits.png, boat.png
    // Set each PNG's Texture Type to "Sprite (2D and UI)" in the Inspector.

    static Sprite LoadIcon(string name)
        => Resources.Load<Sprite>($"Icons/{name}");

    static Texture2D LoadTexture(string name)
        => Resources.Load<Texture2D>($"Icons/{name}");

    // ── Layout constants ──────────────────────────────────────────────────────
    const float SIDE_PAD    = 80f;
    const float TOP_PAD     = 0f;
    const float BOTTOM_PAD  = 28f;
    const float STACK_GAP   = 18f;
    const float LOGO_H      = 390f;
    const float LOGO_IMG_H  = 330f;
    const float BTN_PLAY_H  = 68f;
    const float BTN_SEC_H   = 60f;
    const float BTN_ROW_H   = 54f;
    const float BTN_GAP     = 14f;
    const float VERSION_H   = 20f;
    const float STACK_Y     = 34f;

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
    static Color WATER_BASE = Hex("42afd8");
    static Color WATER_PATTERN_A = new Color(1f, 1f, 1f, 0.18f);
    static Color WATER_PATTERN_B = new Color(0.70f, 0.96f, 1f, 0.08f);
    static Color WATER_SHADE  = new Color(0.02f, 0.20f, 0.31f, 0.05f);

    // ─────────────────────────────────────────────────────────────────────────

    void Start() => Build();

    [ContextMenu("Build Home Screen")]
    public void Build()
    {
        var old = transform.Find("HomePanel");
        if (old != null) DestroyImmediate(old.gameObject);
        var oldBackdrop = transform.Find("MenuBackdrop");
        if (oldBackdrop != null) DestroyImmediate(oldBackdrop.gameObject);

        var backdrop = NewGO("MenuBackdrop", transform);
        Stretch(backdrop);
        backdrop.transform.SetAsFirstSibling();
        BuildBackground(backdrop.transform);

        var panel = NewGO("HomePanel", transform);
        Stretch(panel);

        BuildContent(panel.transform);
        ApplySprites(panel.transform);
        WireMainMenuManager(panel);
    }

    // ── Background ────────────────────────────────────────────────────────────

    void BuildBackground(Transform root)
    {
        var bg = NewGO("Background", root);
        Stretch(bg);

#if UNITY_EDITOR
        // Auto-find the water material in the editor so the 3D shader activates
        // without needing a manual inspector assignment.
        if (menuWaterMaterial == null)
            menuWaterMaterial = FindWaterMaterialInEditor();
#endif

        var shaderWater = GetComponent<MenuWaterBackground3D>();
        if (menuWaterMaterial != null)
        {
            if (shaderWater == null)
                shaderWater = gameObject.AddComponent<MenuWaterBackground3D>();

            var targetCamera = Camera.main;
            if (targetCamera == null)
                targetCamera = FindAnyObjectByType<Camera>();

            if (shaderWater.Configure(menuWaterMaterial, targetCamera))
            {
                // Subtle colour tint that unifies the 3D water with the UI palette
                var tint = NewGO("WaterTint", bg.transform);
                Stretch(tint);
                var tintImg = tint.AddComponent<Image>();
                tintImg.color = new Color(0.02f, 0.15f, 0.22f, 0.06f);
                tintImg.raycastTarget = false;

                // Deep-water gradient — darker at the bottom, open at the top
                var depth = NewGO("WaterDepth", bg.transform);
                var depthRT = depth.GetComponent<RectTransform>();
                depthRT.anchorMin = new Vector2(0f, 0f);
                depthRT.anchorMax = new Vector2(1f, 0.45f);
                depthRT.offsetMin = depthRT.offsetMax = Vector2.zero;
                var depthImg = depth.AddComponent<Image>();
                depthImg.color = new Color(0f, 0.05f, 0.20f, 0.32f);
                depthImg.raycastTarget = false;

                // Animated shimmer lines — light caustics skimming the surface
                BuildWaterShimmerLines(bg.transform);
                return;
            }
        }
        else if (shaderWater != null)
        {
            shaderWater.Clear();
        }

        if (menuWaterTexture != null)
        {
            var baseFill = NewGO("WaterBase", bg.transform);
            Stretch(baseFill);
            var baseImg = baseFill.AddComponent<Image>();
            baseImg.color = WATER_BASE;
            baseImg.raycastTarget = false;

            BuildAnimatedWaterLayer(bg.transform, "WaterPatternA", menuWaterTexture, WATER_PATTERN_A, 220f, new Vector2(0.020f, -0.006f));
            BuildAnimatedWaterLayer(bg.transform, "WaterPatternB", menuWaterTexture, WATER_PATTERN_B, 320f, new Vector2(-0.012f, 0.004f));

            var shade = NewGO("WaterShade", bg.transform);
            Stretch(shade);
            var shadeImg = shade.AddComponent<Image>();
            shadeImg.color = WATER_SHADE;
            shadeImg.raycastTarget = false;

            return;
        }

        BuildLegacyBackground(bg.transform);
    }

    void BuildLegacyBackground(Transform parent)
    {
        // Sky-to-sea gradient via stacked layers
        Layer("Sky_Top", parent, 0.55f, 1.0f,  SKY_TOP);
        Layer("Sky_Mid", parent, 0.30f, 0.65f, SKY_MID);
        Layer("Sky_Low", parent, 0.10f, 0.45f, SKY_LOW);
        Layer("Sea_Mid", parent, 0.00f, 0.25f, SEA_MID);
        Layer("Sea_Bot", parent, 0.00f, 0.12f, SEA_BOT);

        // Clouds
        MakeCloud(parent, "Cloud1", 140f, 40f, -200f, 0.88f, 22f,  0f);
        MakeCloud(parent, "Cloud2",  90f, 28f, -150f, 0.82f, 30f, -8f);
        MakeCloud(parent, "Cloud3", 110f, 35f,  100f, 0.90f, 26f,-14f);

        // Waves
        MakeWave(parent, "Wave1", 0.47f,  80f, new Color(0.22f,0.74f,0.98f,0.25f),  4f,   0f);
        MakeWave(parent, "Wave2", 0.43f, 100f, new Color(0.055f,0.647f,0.914f,0.25f),5f,  -1f);
        MakeWave(parent, "Wave3", 0.41f,  60f, new Color(0.49f,0.83f,0.99f,0.25f),  3.5f, -2f);

        // Deco boats
        MakeDecoBoat(parent, "DecoBoat1", "⛵", 28f, new Vector2(0.08f, 0.57f), 4f,  0f);
        MakeDecoBoat(parent, "DecoBoat2", "🚤", 20f, new Vector2(0.88f, 0.52f), 5f, -2f);
    }

    void Layer(string name, Transform parent, float minY, float maxY, Color color)
    {
        var go = NewGO(name, parent);
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0f, minY); rt.anchorMax = new Vector2(1f, maxY);
        rt.offsetMin = rt.offsetMax = Vector2.zero;
        go.AddComponent<Image>().color = color;
    }

    void BuildAnimatedWaterLayer(Transform parent, string name, Texture texture, Color color, float tileSize, Vector2 scrollSpeed)
    {
        var go = NewGO(name, parent);
        Stretch(go);

        var raw = go.AddComponent<RawImage>();
        raw.texture = texture;
        raw.color = color;
        raw.raycastTarget = false;

        var tiled = go.AddComponent<AnimatedTiledRawImage>();
        tiled.tileSize = tileSize;
        tiled.scrollSpeed = scrollSpeed;
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
        var stack = NewGO("CenterStack", c.transform);
        var stackRT = stack.GetComponent<RectTransform>();
        float stackHeight = LOGO_H + STACK_GAP + BTN_PLAY_H + BTN_GAP + BTN_SEC_H + BTN_GAP + BTN_ROW_H;
        stackRT.anchorMin = new Vector2(0f, 0.5f);
        stackRT.anchorMax = new Vector2(1f, 0.5f);
        stackRT.pivot = new Vector2(0.5f, 0.5f);
        stackRT.sizeDelta = new Vector2(-SIDE_PAD * 2f, stackHeight);
        stackRT.anchoredPosition = new Vector2(0f, STACK_Y);

        var stackLayout = stack.AddComponent<VerticalLayoutGroup>();
        stackLayout.childAlignment       = TextAnchor.UpperCenter;
        stackLayout.spacing              = STACK_GAP;
        stackLayout.childControlWidth    = true;
        stackLayout.childControlHeight   = true;
        stackLayout.childForceExpandWidth  = true;
        stackLayout.childForceExpandHeight = false;

        BuildLogo(stack.transform);
        BuildButtons(stack.transform);
        BuildVersion(c.transform);
    }

    // ── Logo ──────────────────────────────────────────────────────────────────

    void BuildLogo(Transform parent)
    {
        if (logoTexture == null)
            logoTexture = LoadTexture("leave_my_boat_logo");

        var area = NewGO("LogoArea", parent); LE(area, LOGO_H);

        var runtimeLogo = CreateSpriteFromTexture(logoTexture);
        if (runtimeLogo != null)
        {
            var logoGO = NewGO("LogoImage", area.transform);
            var logoRT = logoGO.GetComponent<RectTransform>();
            logoRT.anchorMin = logoRT.anchorMax = new Vector2(0.5f, 0.72f);
            logoRT.pivot = new Vector2(0.5f, 0.5f);
            logoRT.sizeDelta = new Vector2(LOGO_IMG_H, LOGO_IMG_H);
            logoRT.anchoredPosition = Vector2.zero;

            var logoImg = logoGO.AddComponent<Image>();
            logoImg.sprite = runtimeLogo;
            logoImg.color = Color.white;
            logoImg.preserveAspect = true;
            logoImg.raycastTarget = false;

            var shadow = logoGO.AddComponent<Shadow>();
            shadow.effectColor = new Color(0f, 0.12f, 0.24f, 0.35f);
            shadow.effectDistance = new Vector2(0f, -12f);

            var bounce = logoGO.AddComponent<LogoBounce>();
            bounce.amplitude = 4f;
            bounce.stretchAmount = 0.018f;
            bounce.duration = 3.8f;
        }
        else
        {
            // Boat icon — uses sprite from Resources/Icons/boat, falls back to placeholder
            var boatGO = NewGO("BoatIcon", area.transform); LE(boatGO, 88f);
            var boatImg = boatGO.AddComponent<Image>();
            boatImg.color = Color.white;
            boatImg.preserveAspect = true;
            var bob = boatGO.AddComponent<Bobber>(); bob.amplitude = 8f; bob.tiltDeg = 3f; bob.duration = 3f;

            // LEAVE MY BOAT title
            var titleGO = NewGO("Title", area.transform); LE(titleGO, 64f);
            var titleTMP = titleGO.AddComponent<TextMeshProUGUI>();
            titleTMP.text = "LEAVE MY BOAT"; titleTMP.fontSize = 82f;
            titleTMP.fontStyle = FontStyles.Bold; titleTMP.color = Color.white;
            titleTMP.alignment = TextAlignmentOptions.Center;
            titleTMP.outlineWidth = 0.25f; titleTMP.outlineColor = new Color32(3, 105, 161, 255);
        }

        // Subtitle
        var subGO = NewGO("Subtitle", area.transform);
        var subRT = subGO.GetComponent<RectTransform>();
        subRT.anchorMin = subRT.anchorMax = new Vector2(0.5f, 0.14f);
        subRT.pivot = new Vector2(0.5f, 0.5f);
        subRT.sizeDelta = new Vector2(640f, 32f);
        subRT.anchoredPosition = Vector2.zero;

        var subTMP = subGO.AddComponent<TextMeshProUGUI>();
        subTMP.text = "HARBOR ESCAPE"; subTMP.fontSize = 30f;
        subTMP.fontStyle = FontStyles.Bold; subTMP.color = new Color(1f,1f,1f,0.7f);
        subTMP.alignment = TextAlignmentOptions.Center; subTMP.characterSpacing = 4f;
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
        var playImg = playGO.AddComponent<Image>(); playImg.color = ORANGE; Flat(playImg);

        var shadowGO = NewGO("Shadow", playGO.transform);
        var shadowRT = shadowGO.GetComponent<RectTransform>();
        shadowRT.anchorMin = new Vector2(0f,0f); shadowRT.anchorMax = new Vector2(1f,0f);
        shadowRT.pivot = new Vector2(0.5f,1f);
        shadowRT.offsetMin = new Vector2(4f,-8f); shadowRT.offsetMax = new Vector2(-4f,0f);
        var shadowImg = shadowGO.AddComponent<Image>(); shadowImg.color = ORANGE_SHD;
        shadowImg.raycastTarget = false; Flat(shadowImg);

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
        var borderImg = go.AddComponent<Image>(); borderImg.color = GLASS_BRD; Flat(borderImg);

        // Inner fill (inset 2px)
        var fill = NewGO("Fill", go.transform);
        var fillRT = fill.GetComponent<RectTransform>();
        fillRT.anchorMin = Vector2.zero; fillRT.anchorMax = Vector2.one;
        fillRT.offsetMin = new Vector2(2f,2f); fillRT.offsetMax = new Vector2(-2f,-2f);
        var fillImg = fill.AddComponent<Image>(); fillImg.color = GLASS_FILL;
        fillImg.raycastTarget = false; Flat(fillImg);

        var btn = go.AddComponent<Button>(); btn.targetGraphic = borderImg;
        TintBtn(btn, new Color(1f,1f,1f,0.32f), new Color(1f,1f,1f,0.12f));

        var lbl = Label("Label", go.transform, text, fontSize, FontStyles.Bold, Color.white);
        StretchFill(lbl); lbl.GetComponent<TextMeshProUGUI>().raycastTarget = false;
    }

    void BuildVersion(Transform parent)
    {
        var go = NewGO("Version", parent);
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0f);
        rt.pivot = new Vector2(0.5f, 0f);
        rt.anchoredPosition = new Vector2(0f, BOTTOM_PAD);
        rt.sizeDelta = new Vector2(180f, VERSION_H);
        var tmp = go.AddComponent<TextMeshProUGUI>();
        tmp.text = $"v{Application.version}"; tmp.fontSize = 11f;
        tmp.color = new Color(1f,1f,1f,0.3f); tmp.alignment = TextAlignmentOptions.Center;
        tmp.characterSpacing = 2f;
    }

    // ── Apply sprites to icon slots ───────────────────────────────────────────

    void ApplySprites(Transform panel)
    {
        SetIcon(panel, "Content/CenterStack/Buttons/PlayButton/AnchorIcon",           LoadIcon("anchor"));
        SetIcon(panel, "Content/CenterStack/Buttons/LevelsButton/LevelsIcon",         LoadIcon("levels"));
        SetIcon(panel, "Content/CenterStack/Buttons/BottomRow/SettingsButton/SettingsIcon", LoadIcon("settings"));
        SetIcon(panel, "Content/CenterStack/Buttons/BottomRow/CreditsButton/CreditsIcon",   LoadIcon("credits"));
        SetIcon(panel, "Content/CenterStack/LogoArea/BoatIcon",                       LoadIcon("boat"));
    }

    static void SetIcon(Transform panel, string path, Sprite sprite)
    {
        if (sprite == null) return;
        var t = panel.Find(path);
        if (t == null) return;
        var img = t.GetComponent<Image>();
        if (img != null) { img.sprite = sprite; img.preserveAspect = true; }
    }

    // ── Editor-only material auto-finder ─────────────────────────────────────

#if UNITY_EDITOR
    static Material FindWaterMaterialInEditor()
    {
        // Use the UNLIT variant — WaterLit requires scene lights to show surface patterns;
        // a menu scene typically has none, so everything renders as flat base colour.
        // WaterUnlit bakes colour variation into the output directly (foam, normals, caves).
        // Priority 1: StylizedOceanWater — WaterUnlit, no reflections, works without lights
        var guids = UnityEditor.AssetDatabase.FindAssets("M_StylizedOceanWater t:Material");
        foreach (var g in guids)
        {
            var mat = UnityEditor.AssetDatabase.LoadAssetAtPath<Material>(
                UnityEditor.AssetDatabase.GUIDToAssetPath(g));
            if (mat != null) return mat;
        }
        // Priority 2: StylizedColdWater — also WaterUnlit
        guids = UnityEditor.AssetDatabase.FindAssets("M_StylizedColdWater t:Material");
        foreach (var g in guids)
        {
            var mat = UnityEditor.AssetDatabase.LoadAssetAtPath<Material>(
                UnityEditor.AssetDatabase.GUIDToAssetPath(g));
            if (mat != null) return mat;
        }
        return null;
    }
#endif

    // ── Animated water atmosphere ─────────────────────────────────────────────

    void BuildWaterShimmerLines(Transform parent)
    {
        // Five horizontal bands that pulse in/out like light caustics on water
        float[] yPos  = { 0.28f, 0.42f, 0.53f, 0.63f, 0.74f };
        float[] delay = { 0.0f,  1.3f,  0.6f,  2.2f,  0.9f  };
        float[] dur   = { 4.4f,  3.6f,  5.2f,  3.9f,  4.7f  };
        float[] alpha = { 0.12f, 0.08f, 0.10f, 0.06f, 0.09f };

        for (int i = 0; i < yPos.Length; i++)
        {
            var go = NewGO($"Shimmer{i}", parent);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(-0.15f, yPos[i]);
            rt.anchorMax = new Vector2( 1.15f, yPos[i] + 0.020f);
            rt.offsetMin = rt.offsetMax = Vector2.zero;

            var img = go.AddComponent<Image>();
            img.color = new Color(1f, 1f, 1f, 0f);
            img.raycastTarget = false;

            var s = go.AddComponent<ShimmerLine>();
            s.baseAnchorY = yPos[i];
            s.duration    = dur[i];
            s.delay       = delay[i];
            s.peakAlpha   = alpha[i];
        }
    }

    // ── Wire MainMenuManager ──────────────────────────────────────────────────

    void WireMainMenuManager(GameObject panel)
    {
        var mm = FindAnyObjectByType<MainMenuManager>();
        if (mm == null) return;
        mm.homePanel = panel;

        Button Find(string path) => panel.transform.Find(path)?.GetComponent<Button>();
        mm.playButton        = Find("Content/CenterStack/Buttons/PlayButton");
        mm.levelSelectButton = Find("Content/CenterStack/Buttons/LevelsButton");
        mm.settingsButton    = Find("Content/CenterStack/Buttons/BottomRow/SettingsButton");
        mm.creditsButton     = Find("Content/CenterStack/Buttons/BottomRow/CreditsButton");
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

    static Sprite _roundedSprite;
    static Sprite _flatSprite;
    static readonly System.Collections.Generic.Dictionary<Texture2D, Sprite> s_RuntimeSprites = new System.Collections.Generic.Dictionary<Texture2D, Sprite>();

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

    static Sprite GetFlatSprite()
    {
        if (_flatSprite != null) return _flatSprite;

        var tex = new Texture2D(4, 4, TextureFormat.RGBA32, false);
        tex.name = "UI_RuntimeFlat_MainMenu";

        var pixels = new Color32[16];
        for (int i = 0; i < pixels.Length; i++)
            pixels[i] = new Color32(255, 255, 255, 255);

        tex.SetPixels32(pixels);
        tex.Apply();

        _flatSprite = Sprite.Create(
            tex,
            new Rect(0, 0, 4, 4),
            new Vector2(0.5f, 0.5f),
            100f);

        return _flatSprite;
    }

    static void Flat(Image img)
    {
        img.sprite = GetFlatSprite();
        img.type = Image.Type.Simple;
    }

    static Sprite CreateSpriteFromTexture(Texture2D texture)
    {
        if (texture == null) return null;
        if (s_RuntimeSprites.TryGetValue(texture, out var sprite) && sprite != null) return sprite;

        Rect spriteRect = GetOpaqueTextureRect(texture, 12);
        sprite = Sprite.Create(
            texture,
            spriteRect,
            new Vector2(0.5f, 0.5f),
            100f);

        s_RuntimeSprites[texture] = sprite;
        return sprite;
    }

    static Rect GetOpaqueTextureRect(Texture2D texture, int padding)
    {
        if (texture == null)
            return new Rect(0f, 0f, 1f, 1f);

        try
        {
            var pixels = texture.GetPixels32();
            int minX = texture.width;
            int minY = texture.height;
            int maxX = -1;
            int maxY = -1;

            for (int y = 0; y < texture.height; y++)
            {
                int row = y * texture.width;
                for (int x = 0; x < texture.width; x++)
                {
                    if (pixels[row + x].a <= 8)
                        continue;

                    if (x < minX) minX = x;
                    if (y < minY) minY = y;
                    if (x > maxX) maxX = x;
                    if (y > maxY) maxY = y;
                }
            }

            if (maxX < minX || maxY < minY)
                return new Rect(0f, 0f, texture.width, texture.height);

            minX = Mathf.Max(0, minX - padding);
            minY = Mathf.Max(0, minY - padding);
            maxX = Mathf.Min(texture.width - 1, maxX + padding);
            maxY = Mathf.Min(texture.height - 1, maxY + padding);

            return new Rect(minX, minY, maxX - minX + 1, maxY - minY + 1);
        }
        catch (UnityException)
        {
            return new Rect(0f, 0f, texture.width, texture.height);
        }
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

public class LogoBounce : MonoBehaviour
{
    public float amplitude = 4f;
    public float stretchAmount = 0.018f;
    public float duration = 3.8f;

    RectTransform rt;
    float t;
    float baseY;
    Vector3 baseScale;

    void Awake()
    {
        rt = GetComponent<RectTransform>();
        baseY = rt.anchoredPosition.y;
        baseScale = rt.localScale;
    }

    void Update()
    {
        t += Time.deltaTime;

        float phase = (t / duration) * Mathf.PI * 2f;
        float bob = Mathf.Sin(phase);
        float stretch = Mathf.Sin(phase - 0.35f);

        var pos = rt.anchoredPosition;
        pos.y = baseY + bob * amplitude;
        rt.anchoredPosition = pos;

        float scaleX = 1f - stretch * stretchAmount;
        float scaleY = 1f + stretch * stretchAmount;
        rt.localScale = new Vector3(baseScale.x * scaleX, baseScale.y * scaleY, baseScale.z);
    }
}

/// Fades a horizontal band in and out to simulate light caustics on the water surface.
public class ShimmerLine : MonoBehaviour
{
    public float baseAnchorY = 0.5f;
    public float duration    = 4f;
    public float delay       = 0f;
    public float peakAlpha   = 0.10f;

    Image         img;
    RectTransform rt;
    float         t;

    void Awake()
    {
        img = GetComponent<Image>();
        rt  = GetComponent<RectTransform>();
        t   = delay > 0f ? -delay : 0f;
    }

    void Update()
    {
        t += Time.deltaTime;
        if (t < 0f) return;

        // Smooth sine fade so the band pulses rather than strobes
        float phase = Mathf.Sin((t / duration) * Mathf.PI * 2f);
        float a     = (phase * 0.5f + 0.5f) * peakAlpha;
        var c = img.color;
        img.color = new Color(c.r, c.g, c.b, a);

        // Gentle vertical drift that follows the wave rhythm
        float drift = Mathf.Sin(t * 0.68f + baseAnchorY * 3.1f) * 0.014f;
        float newY  = baseAnchorY + drift;
        rt.anchorMin = new Vector2(rt.anchorMin.x, newY);
        rt.anchorMax = new Vector2(rt.anchorMax.x, newY + 0.020f);
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
