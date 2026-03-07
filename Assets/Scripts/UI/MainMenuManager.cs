// Assets/Scripts/UI/MainMenuManager.cs
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Controls the Main Menu scene.
/// The level select header, progress bar and scroll area are all
/// built and positioned entirely in code — no scene wiring needed
/// beyond the Scroll View Content transform and Close Button.
/// </summary>
public class MainMenuManager : MonoBehaviour
{
    // ── Panels ────────────────────────────────────────────────────────────────
    [Header("Panels")]
    public GameObject homePanel;
    public GameObject settingsPanel;
    public GameObject creditsPanel;
    public GameObject levelSelectPanel;

    // ── Home buttons ──────────────────────────────────────────────────────────
    [Header("Home Buttons")]
    public Button playButton;
    public Button levelSelectButton;
    public Button settingsButton;
    public Button creditsButton;
    public Button quitButton;

    // ── Settings ──────────────────────────────────────────────────────────────
    [Header("Settings")]
    public Slider musicSlider;
    public Slider sfxSlider;
    public Toggle vibrationToggle;
    public Button settingsCloseButton;

    // ── Credits ───────────────────────────────────────────────────────────────
    [Header("Credits")]
    public Button creditsCloseButton;

    // ── Level Select ──────────────────────────────────────────────────────────
    [Header("Level Select")]
    public Transform  levelButtonContainer;   // Scroll View > Viewport > Content
    public GameObject levelButtonPrefab;
    public Button     levelSelectCloseButton;
    private int       totalLevels;

    // ── Debug ─────────────────────────────────────────────────────────────────
    [Header("Debug")]
    [Tooltip("Tick once to wipe saved progress. Untick after first run.")]
    public bool resetProgressOnStart;

    // ── Version ───────────────────────────────────────────────────────────────
    [Header("Version")]
    public TextMeshProUGUI versionLabel;

    // ── Runtime references (built in code) ───────────────────────────────────
    RectTransform progressFillRect;
    TextMeshProUGUI progressCountLabel;
    TextMeshProUGUI progressWorldLabel;

    // ── Constants ─────────────────────────────────────────────────────────────
    const string MUSIC_VOL_KEY = "MusicVolume";
    const string SFX_VOL_KEY   = "SFXVolume";
    const string VIBRATION_KEY = "Vibration";

    // Layout pixel heights (in canvas units at 1920×1080 reference)
    const float HEADER_HEIGHT   = 80f;
    const float PROGRESS_HEIGHT = 50f;
    const float HEADER_TOP_PAD  = 90f;   // gap from top of panel  ← was 20
    const float SIDE_PAD        = 24f;
    const float CLOSE_BTN_H     = 48f;
    const float CLOSE_BTN_W     = 240f;
    const float CLOSE_BTN_PAD   = 20f;   // gap from bottom

    // ─────────────────────────────────────────────────────────────────────────

    void Start()
    {
        if (resetProgressOnStart)
        {
            ResetSavedLevelProgress();
            Debug.Log("MainMenuManager: Progress reset on start.");
        }
        else if (!PlayerPrefs.HasKey("LevelsUnlocked"))
        {
            PlayerPrefs.SetInt("LevelsUnlocked", 1);
            PlayerPrefs.Save();
            Debug.Log("MainMenuManager: First run – seeded LevelsUnlocked = 1.");
        }

        var loadedLevels = Resources.LoadAll<LevelData>("Levels");
        totalLevels = loadedLevels.Length;
        Debug.Log($"MainMenuManager: Found {totalLevels} level(s).");

        if (versionLabel != null)
            versionLabel.text = $"v{Application.version}";

        // Auto-find if not wired
        if (playButton        == null) playButton        = Find<Button>("PlayButton");
        if (levelSelectButton == null) levelSelectButton = Find<Button>("LevelSelectButton") ?? Find<Button>("LevelsButton");
        if (settingsButton    == null) settingsButton    = Find<Button>("SettingsButton");
        if (creditsButton     == null) creditsButton     = Find<Button>("CreditsButton");
        if (quitButton        == null) quitButton        = Find<Button>("QuitButton");
        if (homePanel        == null)  homePanel        = GameObject.Find("HomePanel");
        if (levelSelectPanel == null)  levelSelectPanel = GameObject.Find("LevelSelectPanel");

        if (levelButtonContainer == null && levelSelectPanel != null)
        {
            Transform c = levelSelectPanel.transform.Find("Scroll View/Viewport/Content")
                       ?? levelSelectPanel.transform.Find("LevelScrollView/Viewport/Content")
                       ?? levelSelectPanel.transform.Find("Content");
            if (c != null) levelButtonContainer = c;
        }

        if (levelSelectCloseButton == null && levelSelectPanel != null)
            levelSelectCloseButton = levelSelectPanel.transform.Find("CloseButton")?.GetComponent<Button>();

        EnsureRuntimeLevelButtonPrefab();
        NormalizeMenuCanvas();
        NormalizePanelRect(levelSelectPanel);

        playButton            ?.onClick.AddListener(OnPlayPressed);
        levelSelectButton     ?.onClick.AddListener(OpenLevelSelect);
        settingsButton        ?.onClick.AddListener(OpenSettings);
        creditsButton         ?.onClick.AddListener(OpenCredits);
        quitButton            ?.onClick.AddListener(OnQuitPressed);
        settingsCloseButton   ?.onClick.AddListener(CloseSettings);
        creditsCloseButton    ?.onClick.AddListener(CloseCredits);
        levelSelectCloseButton?.onClick.AddListener(CloseLevelSelect);

        LoadSettings();
        musicSlider    ?.onValueChanged.AddListener(v => PlayerPrefs.SetFloat(MUSIC_VOL_KEY, v));
        sfxSlider      ?.onValueChanged.AddListener(v => PlayerPrefs.SetFloat(SFX_VOL_KEY,   v));
        vibrationToggle?.onValueChanged.AddListener(v => PlayerPrefs.SetInt(VIBRATION_KEY, v ? 1 : 0));

        ShowPanel(homePanel);
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    static T Find<T>(string name) where T : Component
        => GameObject.Find(name)?.GetComponent<T>();

    // ── Navigation ────────────────────────────────────────────────────────────

    void ShowPanel(GameObject target)
    {
        if (target == null) { homePanel?.SetActive(true); return; }
        homePanel        ?.SetActive(false);
        settingsPanel    ?.SetActive(false);
        creditsPanel     ?.SetActive(false);
        levelSelectPanel ?.SetActive(false);
        target.SetActive(true);
    }

    public void OnPlayPressed()
    {
        PlayerPrefs.SetInt("SelectedLevel", 0);
        PlayerPrefs.Save();
        SceneManager.LoadScene("GameScene");
    }

    public void OpenLevelSelect()
    {
        if (levelSelectPanel == null || levelButtonContainer == null || levelButtonPrefab == null)
        {
            Debug.LogError("MainMenuManager: Level select references missing.");
            return;
        }

        NormalizePanelRect(levelSelectPanel);
        BuildLevelSelectUI();     // builds header + progress + repositions scroll + close btn
        ShowPanel(levelSelectPanel);
        levelSelectPanel.transform.SetAsLastSibling();

        Canvas.ForceUpdateCanvases();
        var cr = levelButtonContainer as RectTransform;
        if (cr != null) LayoutRebuilder.ForceRebuildLayoutImmediate(cr);

        BuildLevelButtons();
    }

    public void CloseLevelSelect() => ShowPanel(homePanel);
    public void OpenSettings()     => ShowPanel(settingsPanel);
    public void CloseSettings()    { PlayerPrefs.Save(); ShowPanel(homePanel); }
    public void OpenCredits()      => ShowPanel(creditsPanel);
    public void CloseCredits()     => ShowPanel(homePanel);

    public void OnQuitPressed()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    // ── Build entire level-select UI in code ──────────────────────────────────

    void BuildLevelSelectUI()
    {
        if (levelSelectPanel == null) return;

        // Make panel transparent so scene background shows through
        var panelImg = levelSelectPanel.GetComponent<Image>() ?? levelSelectPanel.AddComponent<Image>();
        panelImg.color = Color.clear;

        float scrollTop    = HEADER_TOP_PAD + HEADER_HEIGHT + 16f + PROGRESS_HEIGHT + 24f;
        float scrollBottom = CLOSE_BTN_PAD  + CLOSE_BTN_H  + 12f;

        BuildHeader();
        BuildProgressBar();
        RepositionScrollView(scrollTop, scrollBottom);
        RepositionCloseButton();
        RefreshProgressValues();
    }

    // ── Header row ────────────────────────────────────────────────────────────

    void BuildHeader()
    {
        var old = levelSelectPanel.transform.Find("_Header");
        if (old != null) DestroyImmediate(old.gameObject);

        var header = NewRect("_Header", levelSelectPanel.transform);
        header.anchorMin        = new Vector2(0f, 1f);
        header.anchorMax        = new Vector2(1f, 1f);
        header.pivot            = new Vector2(0.5f, 1f);
        header.anchoredPosition = new Vector2(0f, -HEADER_TOP_PAD);
        header.sizeDelta        = new Vector2(0f, HEADER_HEIGHT);
        header.offsetMin        = new Vector2(SIDE_PAD,  header.offsetMin.y);
        header.offsetMax        = new Vector2(-SIDE_PAD, header.offsetMax.y);

        // ── Back button — square, full header height, rounded, transparent ──
        var backObj  = new GameObject("_BackBtn");
        backObj.transform.SetParent(header, false);
        var backRect = backObj.AddComponent<RectTransform>();
        backRect.anchorMin        = new Vector2(0f, 0f);
        backRect.anchorMax        = new Vector2(0f, 1f);
        backRect.pivot            = new Vector2(0f, 0.5f);
        backRect.anchoredPosition = Vector2.zero;
        backRect.sizeDelta        = new Vector2(HEADER_HEIGHT, 0f);

        var backImg = backObj.AddComponent<Image>();
        backImg.color = new Color(1f, 1f, 1f, 0.12f);
        ApplyRoundedSprite(backImg);

        var backBtn = backObj.AddComponent<Button>();
        var cols    = backBtn.colors;
        cols.highlightedColor = new Color(1f, 1f, 1f, 0.25f);
        cols.pressedColor     = new Color(1f, 1f, 1f, 0.06f);
        backBtn.colors = cols;
        backBtn.onClick.AddListener(CloseLevelSelect);

        var backLabel = MakeTMP("BackLabel", backObj.transform, "‹", 48, FontStyles.Bold, Color.white, TextAlignmentOptions.Center);
        var blRect = backLabel.GetComponent<RectTransform>();
        blRect.anchorMin = Vector2.zero; blRect.anchorMax = Vector2.one;
        blRect.offsetMin = new Vector2(8f, 4f); blRect.offsetMax = new Vector2(-8f, -4f);
        backLabel.GetComponent<TextMeshProUGUI>().raycastTarget = false;

        // ── Title row: anchor icon + LEVELS text, centred ──
        var titleRow     = new GameObject("_TitleRow");
        titleRow.transform.SetParent(header, false);
        var titleRowRect = titleRow.AddComponent<RectTransform>();
        titleRowRect.anchorMin = Vector2.zero; titleRowRect.anchorMax = Vector2.one;
        titleRowRect.offsetMin = Vector2.zero; titleRowRect.offsetMax = Vector2.zero;

        var titleHL = titleRow.AddComponent<HorizontalLayoutGroup>();
        titleHL.childAlignment       = TextAnchor.MiddleCenter;
        titleHL.spacing              = 12f;
        titleHL.childControlWidth    = false;
        titleHL.childControlHeight   = false;
        titleHL.childForceExpandWidth  = false;
        titleHL.childForceExpandHeight = false;

        // Anchor icon (loads from Resources/Icons/anchor)
        var iconGO  = new GameObject("AnchorIcon");
        iconGO.transform.SetParent(titleRow.transform, false);
        var iconRT  = iconGO.AddComponent<RectTransform>();
        iconRT.sizeDelta = new Vector2(68f, 68f);
        var iconImg = iconGO.AddComponent<Image>();
        iconImg.color          = Color.white;
        iconImg.preserveAspect = true;
        iconImg.raycastTarget  = false;
        var anchorSprite = Resources.Load<Sprite>("Icons/anchor");
        if (anchorSprite != null) iconImg.sprite = anchorSprite;

        // LEVELS label
        var titleGO  = new GameObject("_Title");
        titleGO.transform.SetParent(titleRow.transform, false);
        var titleRT  = titleGO.AddComponent<RectTransform>();
        titleRT.sizeDelta = new Vector2(220f, HEADER_HEIGHT);
        var titleTMP = titleGO.AddComponent<TextMeshProUGUI>();
        titleTMP.text          = "LEVELS";
        titleTMP.fontSize      = 38f;
        titleTMP.fontStyle     = FontStyles.Bold;
        titleTMP.color         = Color.white;
        titleTMP.alignment     = TextAlignmentOptions.Center;
        titleTMP.raycastTarget = false;
    }

    // ── Progress bar ─────────────────────────────────────────────────────────

    void BuildProgressBar()
    {
        var old = levelSelectPanel.transform.Find("_Progress");
        if (old != null) DestroyImmediate(old.gameObject);

        float topOffset = -(HEADER_TOP_PAD + HEADER_HEIGHT + 16f);

        var block = NewRect("_Progress", levelSelectPanel.transform);
        block.anchorMin        = new Vector2(0f, 1f);
        block.anchorMax        = new Vector2(1f, 1f);
        block.pivot            = new Vector2(0.5f, 1f);
        block.anchoredPosition = new Vector2(0f, topOffset);
        block.sizeDelta        = new Vector2(0f, PROGRESS_HEIGHT);
        block.offsetMin        = new Vector2(SIDE_PAD,  block.offsetMin.y);
        block.offsetMax        = new Vector2(-SIDE_PAD, block.offsetMax.y);

        // World label (left)
        progressWorldLabel = MakeTMP("WorldLabel", block, "Harbor World", 28, FontStyles.Bold,
            new Color(1f, 1f, 1f, 0.80f), TextAlignmentOptions.Left).GetComponent<TextMeshProUGUI>();
        var wlRect = progressWorldLabel.GetComponent<RectTransform>();
        wlRect.anchorMin = new Vector2(0f, 0.55f); wlRect.anchorMax = new Vector2(0.6f, 1f);
        wlRect.offsetMin = Vector2.zero;            wlRect.offsetMax = Vector2.zero;

        // Count label (right)
        progressCountLabel = MakeTMP("CountLabel", block, "0 / 0 complete", 26, FontStyles.Bold,
            new Color(1f, 1f, 1f, 0.60f), TextAlignmentOptions.Right).GetComponent<TextMeshProUGUI>();
        var clRect = progressCountLabel.GetComponent<RectTransform>();
        clRect.anchorMin = new Vector2(0.6f, 0.55f); clRect.anchorMax = new Vector2(1f, 1f);
        clRect.offsetMin = Vector2.zero;              clRect.offsetMax = Vector2.zero;

        // Track
        var trackObj  = new GameObject("Track");
        trackObj.transform.SetParent(block, false);
        var trackRect = trackObj.AddComponent<RectTransform>();
        trackRect.anchorMin = new Vector2(0f, 0f); trackRect.anchorMax = new Vector2(1f, 0.45f);
        trackRect.offsetMin = Vector2.zero;         trackRect.offsetMax = Vector2.zero;
        var trackImg  = trackObj.AddComponent<Image>();
        trackImg.color = new Color(1f, 1f, 1f, 0.12f);
        ApplyRoundedSprite(trackImg);

        // Fill
        var fillObj  = new GameObject("Fill");
        fillObj.transform.SetParent(trackObj.transform, false);
        progressFillRect = fillObj.AddComponent<RectTransform>();
        progressFillRect.anchorMin = new Vector2(0f, 0f);
        progressFillRect.anchorMax = new Vector2(0f, 1f); // width driven at runtime
        progressFillRect.offsetMin = Vector2.zero;
        progressFillRect.offsetMax = Vector2.zero;
        var fillImg  = fillObj.AddComponent<Image>();
        fillImg.color = new Color(0.96f, 0.62f, 0.07f, 1f);
        ApplyRoundedSprite(fillImg);
    }

    void RefreshProgressValues()
    {
        int unlockedCount  = LevelProgress.GetUnlockedLevelCount();
        int completedCount = Mathf.Max(0, unlockedCount - 1);
        float fill         = totalLevels > 0 ? Mathf.Clamp01((float)completedCount / totalLevels) : 0f;

        if (progressWorldLabel != null) progressWorldLabel.text = "Harbor World";
        if (progressCountLabel != null) progressCountLabel.text = $"{completedCount} / {totalLevels} complete";
        if (progressFillRect   != null)
        {
            progressFillRect.anchorMax = new Vector2(fill, 1f);
            progressFillRect.offsetMax = Vector2.zero;
        }
    }

    // ── Reposition Scroll View ────────────────────────────────────────────────

    void RepositionScrollView(float topPx, float bottomPx)
    {
        Transform scroll = levelSelectPanel.transform.Find("Scroll View")
                        ?? levelSelectPanel.transform.Find("LevelScrollView");
        if (scroll == null) return;

        // Strip background / masks
        var scrollImg = scroll.GetComponent<Image>();
        if (scrollImg != null) scrollImg.color = Color.clear;

        Transform viewport = scroll.Find("Viewport");
        if (viewport != null)
        {
            var vImg = viewport.GetComponent<Image>();
            if (vImg != null) vImg.color = Color.clear;
            var mask = viewport.GetComponent<Mask>();
            if (mask != null) { mask.showMaskGraphic = false; mask.enabled = false; }
            var rm = viewport.GetComponent<RectMask2D>();
            if (rm != null) rm.enabled = false;
        }

        var sr = scroll.GetComponent<RectTransform>();
        if (sr == null) return;
        sr.anchorMin        = Vector2.zero;
        sr.anchorMax        = Vector2.one;
        sr.pivot            = new Vector2(0.5f, 0.5f);
        sr.offsetMin        = new Vector2(SIDE_PAD,  bottomPx);
        sr.offsetMax        = new Vector2(-SIDE_PAD, -topPx);
    }

    // ── Reposition Close Button ───────────────────────────────────────────────

    void RepositionCloseButton()
    {
        var closeBtn = levelSelectCloseButton
                    ?? levelSelectPanel.transform.Find("CloseButton")?.GetComponent<Button>();
        if (closeBtn == null) return;

        var cr = closeBtn.GetComponent<RectTransform>();
        if (cr == null) return;
        cr.anchorMin        = new Vector2(0.5f, 0f);
        cr.anchorMax        = new Vector2(0.5f, 0f);
        cr.pivot            = new Vector2(0.5f, 0f);
        cr.anchoredPosition = new Vector2(0f, CLOSE_BTN_PAD);
        cr.sizeDelta        = new Vector2(CLOSE_BTN_W, CLOSE_BTN_H);
    }

    // ── Level grid ────────────────────────────────────────────────────────────

    void BuildLevelButtons()
    {
        if (levelButtonContainer == null || levelButtonPrefab == null) return;

        ConfigureLevelGridLayout();

        int selectedLevelIndex = Mathf.Max(0, PlayerPrefs.GetInt("SelectedLevel", 0));

        foreach (Transform child in levelButtonContainer)
        {
            if (child.gameObject == levelButtonPrefab) continue;
            Destroy(child.gameObject);
        }

        for (int i = 0; i < totalLevels; i++)
        {
            int levelIndex = i;
            var btn = Instantiate(levelButtonPrefab, levelButtonContainer);
            btn.SetActive(true);
            btn.name = $"LevelBtn_{i + 1}";

            bool isUnlocked  = LevelProgress.IsLevelUnlocked(levelIndex);
            int  starsEarned = LevelProgress.GetStars(levelIndex);
            bool isSelected  = isUnlocked && levelIndex == selectedLevelIndex;

            var b = btn.GetComponent<Button>();
            if (b != null)
            {
                b.interactable = isUnlocked;
                if (isUnlocked)
                    b.onClick.AddListener(() => LoadLevel(levelIndex));
            }

            var cv = btn.GetComponent<LevelCardView>() ?? btn.AddComponent<LevelCardView>();
            cv.SetData(levelIndex + 1, isUnlocked, starsEarned, isSelected);
        }

        var contentRect = levelButtonContainer as RectTransform;
        if (contentRect != null)
        {
            contentRect.anchoredPosition = Vector2.zero;
            LayoutRebuilder.ForceRebuildLayoutImmediate(contentRect);
        }

        var scrollRect = levelButtonContainer.GetComponentInParent<ScrollRect>();
        if (scrollRect != null) scrollRect.verticalNormalizedPosition = 1f;
    }

    void ConfigureLevelGridLayout()
    {
        var grid = levelButtonContainer.GetComponent<GridLayoutGroup>()
                ?? levelButtonContainer.gameObject.AddComponent<GridLayoutGroup>();

        const int   columns = 4;
        const float pad     = 12f;
        const float gap     = 8f;

        var viewportRect = levelButtonContainer.parent as RectTransform;
        var contentRect  = levelButtonContainer as RectTransform;
        if (viewportRect == null || contentRect == null) return;

        var fitter = levelButtonContainer.GetComponent<ContentSizeFitter>()
                  ?? levelButtonContainer.gameObject.AddComponent<ContentSizeFitter>();
        fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
        fitter.verticalFit   = ContentSizeFitter.FitMode.PreferredSize;

        contentRect.anchorMin        = new Vector2(0f, 1f);
        contentRect.anchorMax        = new Vector2(1f, 1f);
        contentRect.pivot            = new Vector2(0.5f, 1f);
        contentRect.anchoredPosition = Vector2.zero;
        contentRect.offsetMin        = new Vector2( pad, contentRect.offsetMin.y);
        contentRect.offsetMax        = new Vector2(-pad, contentRect.offsetMax.y);

        float avail      = viewportRect.rect.width - pad * 2f;
        float cellWidth  = Mathf.Max(72f, (avail - gap * (columns - 1)) / columns);
        float cellHeight = Mathf.Clamp(cellWidth * 0.90f, 70f, 110f);

        grid.constraint      = GridLayoutGroup.Constraint.FixedColumnCount;
        grid.constraintCount = columns;
        grid.spacing         = new Vector2(gap, gap);
        grid.cellSize        = new Vector2(cellWidth, cellHeight);
        grid.startCorner     = GridLayoutGroup.Corner.UpperLeft;
        grid.startAxis       = GridLayoutGroup.Axis.Horizontal;
        grid.childAlignment  = TextAnchor.UpperLeft;

        LayoutRebuilder.ForceRebuildLayoutImmediate(contentRect);
    }

    void LoadLevel(int index)
    {
        PlayerPrefs.SetInt("SelectedLevel", index);
        PlayerPrefs.Save();
        SceneManager.LoadScene("GameScene");
    }

    // ── Canvas / panel helpers ────────────────────────────────────────────────

    void NormalizePanelRect(GameObject panel)
    {
        if (panel == null) return;
        var r = panel.GetComponent<RectTransform>();
        if (r == null) return;
        r.anchorMin = Vector2.zero; r.anchorMax = Vector2.one;
        r.offsetMin = Vector2.zero; r.offsetMax = Vector2.zero;
        r.anchoredPosition = Vector2.zero; r.localScale = Vector3.one;
    }

    void NormalizeMenuCanvas()
    {
        Canvas canvas = homePanel?.GetComponentInParent<Canvas>()
                     ?? levelSelectPanel?.GetComponentInParent<Canvas>();
        if (canvas == null) return;
        if (canvas.renderMode != RenderMode.ScreenSpaceOverlay)
        {
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.worldCamera = null;
        }
        var scaler = canvas.GetComponent<CanvasScaler>();
        if (scaler != null)
        {
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            if (scaler.referenceResolution.x <= 0f || scaler.referenceResolution.y <= 0f)
                scaler.referenceResolution = new Vector2(1920f, 1080f);
        }
    }

    // ── UI building utilities ─────────────────────────────────────────────────

    /// Create an empty RectTransform child.
    static RectTransform NewRect(string name, Transform parent)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        return go.AddComponent<RectTransform>();
    }

    /// Create a TMP label, return the GameObject.
    static GameObject MakeTMP(string name, Transform parent, string text,
        float fontSize, FontStyles style, Color color, TextAlignmentOptions alignment)
    {
        var go  = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.AddComponent<RectTransform>();
        var tmp = go.AddComponent<TextMeshProUGUI>();
        tmp.text      = text;
        tmp.fontSize  = fontSize;
        tmp.fontStyle = style;
        tmp.color     = color;
        tmp.alignment = alignment;
        tmp.overflowMode = TextOverflowModes.Overflow;
        return go;
    }

    /// Stretch a RectTransform to fill its parent.
    static void StretchFill(GameObject go)
    {
        var r = go.GetComponent<RectTransform>();
        if (r == null) return;
        r.anchorMin = Vector2.zero; r.anchorMax = Vector2.one;
        r.offsetMin = Vector2.zero; r.offsetMax = Vector2.zero;
    }

    /// Apply a rounded-rect sprite if available (same helper as LevelCardView).
    static void ApplyRoundedSprite(Image img)
    {
        var sprite = Resources.GetBuiltinResource<Sprite>("UI/Skin/InputFieldBackground.psd");
        if (sprite != null) { img.sprite = sprite; img.type = Image.Type.Sliced; }
    }

    // ── Runtime prefab builder ────────────────────────────────────────────────

    void EnsureRuntimeLevelButtonPrefab()
    {
        if (levelButtonPrefab != null || levelButtonContainer == null) return;

        var res = Resources.Load<GameObject>("UI/LevelCard");
        if (res != null) { levelButtonPrefab = res; return; }

#if UNITY_EDITOR
        var ep = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/UI/LevelCard.prefab");
        if (ep != null) { levelButtonPrefab = ep; return; }
#endif

        var t = new GameObject("LevelButtonTemplate");
        t.transform.SetParent(levelButtonContainer, false);
        t.AddComponent<RectTransform>().sizeDelta = new Vector2(110f, 100f);

        var img = t.AddComponent<Image>(); img.color = new Color(0.22f, 0.60f, 0.40f, 1f);
        t.AddComponent<Button>();

        var tObj = new GameObject("LevelNumber (TMP)"); tObj.transform.SetParent(t.transform, false);
        var tRect = tObj.AddComponent<RectTransform>();
        tRect.anchorMin = new Vector2(0.5f, 0.58f); tRect.anchorMax = new Vector2(0.5f, 0.58f);
        tRect.sizeDelta = new Vector2(80f, 48f); tRect.anchoredPosition = Vector2.zero;
        var lbl = tObj.AddComponent<TextMeshProUGUI>();
        lbl.text = "1"; lbl.alignment = TextAlignmentOptions.Center;
        lbl.fontSize = 42; lbl.fontStyle = FontStyles.Bold;
        lbl.color = new Color(0.14f, 0.19f, 0.27f, 0.95f);

        var sr = new GameObject("StarsRow"); sr.transform.SetParent(t.transform, false);
        var srRect = sr.AddComponent<RectTransform>();
        srRect.anchorMin = new Vector2(0.5f, 0.18f); srRect.anchorMax = new Vector2(0.5f, 0.18f);
        srRect.sizeDelta = new Vector2(90f, 20f); srRect.anchoredPosition = Vector2.zero;
        var lo = sr.AddComponent<HorizontalLayoutGroup>();
        lo.childAlignment = TextAnchor.MiddleCenter; lo.spacing = 6f;
        lo.childControlWidth = lo.childControlHeight = false;
        lo.childForceExpandWidth = lo.childForceExpandHeight = false;

        var ss = Resources.GetBuiltinResource<Sprite>("UI/Skin/Knob.psd");
        for (int i = 1; i <= 3; i++)
        {
            var s = new GameObject($"Stars{i}"); s.transform.SetParent(sr.transform, false);
            s.AddComponent<RectTransform>().sizeDelta = new Vector2(13f, 13f);
            var si = s.AddComponent<Image>(); si.color = new Color(1f, 0.83f, 0.32f, 1f);
            if (ss != null) si.sprite = ss;
        }

        var lk = new GameObject("LockIcon"); lk.transform.SetParent(t.transform, false);
        var lkR = lk.AddComponent<RectTransform>();
        lkR.anchorMin = lkR.anchorMax = new Vector2(0.5f, 0.5f);
        lkR.pivot = new Vector2(0.5f, 0.5f); lkR.anchoredPosition = Vector2.zero;
        lkR.sizeDelta = new Vector2(36f, 36f);
        lk.AddComponent<Image>().color = new Color(0.78f, 0.82f, 0.9f, 0.9f);
        lk.SetActive(false);

        var cv = t.AddComponent<LevelCardView>();
        cv.button = t.GetComponent<Button>(); cv.background = img;
        cv.levelNumberLabel = lbl; cv.lockIcon = lk;
        cv.stars = new[] {
            sr.transform.Find("Stars1")?.GetComponent<Image>(),
            sr.transform.Find("Stars2")?.GetComponent<Image>(),
            sr.transform.Find("Stars3")?.GetComponent<Image>()
        };

        t.SetActive(false);
        levelButtonPrefab = t;
    }

    // ── Settings ──────────────────────────────────────────────────────────────

    void LoadSettings()
    {
        if (musicSlider     != null) musicSlider.value    = PlayerPrefs.GetFloat(MUSIC_VOL_KEY, 0.8f);
        if (sfxSlider       != null) sfxSlider.value      = PlayerPrefs.GetFloat(SFX_VOL_KEY,   1f);
        if (vibrationToggle != null) vibrationToggle.isOn = PlayerPrefs.GetInt(VIBRATION_KEY, 1) == 1;
    }

    public static void UnlockNextLevel(int justCompleted)
        => LevelProgress.UnlockNextLevel(justCompleted);

    [ContextMenu("Reset Saved Level Progress")]
    public void ResetSavedLevelProgress()
    {
        PlayerPrefs.SetInt("LevelsUnlocked", 1);
        PlayerPrefs.SetInt("SelectedLevel",  0);
        for (int i = 0; i < Mathf.Max(1, totalLevels); i++)
            PlayerPrefs.DeleteKey($"LevelStars_{i}");
        PlayerPrefs.Save();
        Debug.Log("MainMenuManager: Level progress reset.");
    }
}