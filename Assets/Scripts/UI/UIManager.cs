// Assets/Scripts/UI/UIManager.cs
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using UnityEngine.EventSystems;
using TMPro;

public class UIManager : MonoBehaviour
{
    public static UIManager Instance { get; private set; }

    [Header("Optional — leave null, auto-built")]
    public TextMeshProUGUI levelLabel;
    public GameObject      winPanel;
    public GameObject      pausePanel;
    [SerializeField] Canvas mainCanvas;

    // ── Colors ────────────────────────────────────────────────────────────────
    static readonly Color ORANGE    = new Color(0.96f, 0.62f, 0.07f, 1f);
    static readonly Color ORANGE_DK = new Color(0.71f, 0.33f, 0.04f, 1f);
    static readonly Color GLASS     = new Color(1f, 1f, 1f, 0.12f);
    static readonly Color GLASS_BRD = new Color(1f, 1f, 1f, 0.18f);
    static readonly Color DARK_BG   = new Color(0.06f, 0.13f, 0.22f, 0.96f);

    // ── Runtime refs ──────────────────────────────────────────────────────────
    GameManager     gameManager;
    TextMeshProUGUI hudLevelText;
    TextMeshProUGUI hudMovesText;
    int             movesCount;
    static Sprite   s_Rounded;

    // ── Undo stack ────────────────────────────────────────────────────────────
    struct BoatState
    {
        public Vector3    worldPos;
        public Vector2Int gridPos;
    }
    readonly Stack<Dictionary<BoatMovement, BoatState>> undoStack
        = new Stack<Dictionary<BoatMovement, BoatState>>();

    // ─────────────────────────────────────────────────────────────────────────

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DestroyLegacyObjects();
    }

    void Start()
    {
        PlatformBootstrap.ApplyDefaults();

        gameManager = GameManager.Instance ?? FindObjectOfType<GameManager>();
        FixCameraBackground();

        if (mainCanvas == null)
        {
            var all = FindObjectsOfType<Canvas>();
            foreach (var c in all)
                if (c.gameObject.scene.name == "GameScene") { mainCanvas = c; break; }
            if (mainCanvas == null && all.Length > 0) mainCanvas = all[0];
        }

        EnsureEventSystem();
        NormalizeCanvas();
        BuildHUD();

        winPanel   = BuildWinPanel();
        pausePanel = BuildPausePanel();
        HideOverlays();

        Debug.Log($"UIManager.Start complete — winPanel={winPanel != null}, pausePanel={pausePanel != null}");
    }

    void OnEnable()  => BoatMovement.OnAnyBoatMoved += OnBoatMoved;
    void OnDisable() => BoatMovement.OnAnyBoatMoved -= OnBoatMoved;
    void OnDestroy() { if (Instance == this) Instance = null; }

    void OnBoatMoved(BoatMovement _) => RegisterMove();

    // ── Camera background ─────────────────────────────────────────────────────

    void FixCameraBackground()
    {
        Color oceanMid = Hex("0369a1");
        foreach (var cam in FindObjectsOfType<Camera>())
        {
            cam.clearFlags      = CameraClearFlags.SolidColor;
            cam.backgroundColor = oceanMid;
        }
        RenderSettings.skybox       = null;
        RenderSettings.ambientLight = new Color(0.4f, 0.55f, 0.7f);
    }

    // ── Snapshot API ─────────────────────────────────────────────────────────

    public void CapturePreMoveSnapshot()
    {
        var boats    = FindObjectsOfType<BoatMovement>();
        var snapshot = new Dictionary<BoatMovement, BoatState>(boats.Length);
        foreach (var boat in boats)
            snapshot[boat] = new BoatState
            {
                worldPos = boat.transform.position,
                gridPos  = boat.GridPosition
            };
        undoStack.Push(snapshot);
    }

    // ── Public API ────────────────────────────────────────────────────────────

    public void ShowGame(int levelNumber)
    {
        HideOverlays();
        movesCount = 0;
        undoStack.Clear();
        Refresh(hudLevelText, $"LVL {levelNumber}");
        Refresh(hudMovesText, "0");
        Time.timeScale = 1f;
    }

    public void ShowWinPanel()
    {
        // Safety: rebuild the win panel if it was somehow destroyed or never built
        if (winPanel == null)
        {
            Debug.LogWarning("UIManager.ShowWinPanel: winPanel was null — rebuilding.");
            if (mainCanvas == null)
            {
                var all = FindObjectsOfType<Canvas>();
                if (all.Length > 0) mainCanvas = all[0];
            }
            winPanel = BuildWinPanel();
        }

        // Make sure it renders on top of everything
        winPanel.transform.SetAsLastSibling();
        winPanel.SetActive(true);

        if (pausePanel != null) pausePanel.SetActive(false);

        // Re-wire NextLevel button every time in case references were lost
        var nextBtn = winPanel.transform.Find("Card/BtnRow/NextBtn")?.GetComponent<Button>();
        if (nextBtn != null)
        {
            nextBtn.onClick.RemoveAllListeners();
            nextBtn.onClick.AddListener(NextLevel);
            nextBtn.interactable = true;
            Debug.Log("UIManager.ShowWinPanel: NextBtn wired to NextLevel.");
        }
        else
        {
            Debug.LogError("UIManager.ShowWinPanel: NextBtn not found in win panel hierarchy!");
        }

        // Re-wire Replay button too
        var replayBtn = winPanel.transform.Find("Card/BtnRow/ReplayBtn")?.GetComponent<Button>();
        if (replayBtn != null)
        {
            replayBtn.onClick.RemoveAllListeners();
            replayBtn.onClick.AddListener(RestartLevel);
        }

        int stars = movesCount <= 5 ? 3 : movesCount <= 12 ? 2 : 1;
        UpdateStars(stars);

        Debug.Log($"UIManager.ShowWinPanel shown — stars={stars}, movesCount={movesCount}");
    }

    public void TogglePause()
    {
        if (pausePanel == null) return;
        bool p = !pausePanel.activeSelf;
        pausePanel.SetActive(p);
        if (winPanel != null && p) winPanel.SetActive(false);
        Time.timeScale = p ? 0f : 1f;
    }

    public void ResumeGame()
    {
        if (pausePanel != null) pausePanel.SetActive(false);
        Time.timeScale = 1f;
    }

    public void RestartLevel()
    {
        Time.timeScale = 1f;
        var gm = GameManager.Instance ?? FindObjectOfType<GameManager>();
        gm?.ReloadCurrentLevel();
    }

    public void NextLevel()
    {
        Debug.Log("UIManager.NextLevel() called");
        Time.timeScale = 1f;
        var gm = GameManager.Instance ?? FindObjectOfType<GameManager>();
        if (gm != null)
            gm.LoadNextLevel();
        else
            Debug.LogError("UIManager.NextLevel: GameManager not found!");
    }

    public void GoHome()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene("MainMenu");
    }

    public void RegisterMove()
    {
        movesCount++;
        Refresh(hudMovesText, movesCount.ToString());
    }

    // ── Undo ─────────────────────────────────────────────────────────────────

    public void UndoMove()
    {
        if (undoStack.Count == 0) return;

        var snapshot = undoStack.Pop();

        // Step 1: restore transforms and grid positions (no grid touch yet)
        foreach (var kv in snapshot)
        {
            if (kv.Key == null) continue;
            kv.Key.transform.position = kv.Value.worldPos;
            kv.Key.ForceGridPosition(kv.Value.gridPos);
        }

        // Step 2: wipe the grid entirely
        GridManager.Instance.ClearGrid();

        // Step 3: re-register all boats from their restored positions
        foreach (var kv in snapshot)
        {
            if (kv.Key == null) continue;
            GridManager.Instance.RegisterBoat(kv.Key);
            kv.Key.OnUndoRestored();
        }

        // Step 4: update counter
        if (movesCount > 0) movesCount--;
        Refresh(hudMovesText, movesCount.ToString());
    }

    // ── Stars ─────────────────────────────────────────────────────────────────

    void UpdateStars(int count)
    {
        if (winPanel == null) return;
        var row = winPanel.transform.Find("Card/WinStars");
        if (row == null) return;
        for (int i = 0; i < row.childCount; i++)
        {
            var img = row.GetChild(i).GetComponent<Image>();
            if (img != null)
                img.color = i < count
                    ? new Color(1f, 0.82f, 0.10f)
                    : new Color(1f, 1f, 1f, 0.20f);
        }
    }

    void HideOverlays()
    {
        if (winPanel   != null) winPanel.SetActive(false);
        if (pausePanel != null) pausePanel.SetActive(false);
    }

    // ── Legacy cleanup ────────────────────────────────────────────────────────

    void DestroyLegacyObjects()
    {
        string[] names = {
            "PauseButton","HomeButton","PauseBtn","HomeBtn",
            "PAUSE","HOME","Pause","Home",
            "WinPanel","PausePanel","WinButton","WIN",
            "LevelLabel","RuntimeGameHUD","RuntimeOceanOverlay"
        };
        foreach (var n in names)
        {
            var go = GameObject.Find(n);
            if (go != null && go != gameObject) Destroy(go);
        }
    }

    // ── Canvas ────────────────────────────────────────────────────────────────

    void NormalizeCanvas()
    {
        if (mainCanvas == null) return;
        mainCanvas.renderMode   = RenderMode.ScreenSpaceOverlay;
        mainCanvas.sortingOrder = 10;
        mainCanvas.worldCamera  = null;
        var s = mainCanvas.GetComponent<CanvasScaler>() ?? mainCanvas.gameObject.AddComponent<CanvasScaler>();
        s.uiScaleMode         = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        s.referenceResolution = new Vector2(1080f, 1920f);
        s.matchWidthOrHeight  = 0.5f;

        // Keep gameplay UI inside safe areas on mobile devices.
        if (mainCanvas.GetComponent<SafeAreaFitter>() == null)
            mainCanvas.gameObject.AddComponent<SafeAreaFitter>();
    }

    void EnsureEventSystem()
    {
        var all = FindObjectsOfType<EventSystem>();
        if (all.Length == 0)
            new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
        else
            for (int i = 1; i < all.Length; i++) Destroy(all[i].gameObject);
    }

    // ── HUD ───────────────────────────────────────────────────────────────────

    void BuildHUD()
    {
        var root = NewGO("HUD", mainCanvas.transform);
        Stretch(root);
        BuildTopBar(root.transform);
        BuildBottomBar(root.transform);
    }

    void BuildTopBar(Transform parent)
    {
        var bar = NewGO("TopBar", parent);
        var rt  = bar.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0f, 1f); rt.anchorMax = new Vector2(1f, 1f);
        rt.pivot     = new Vector2(0.5f, 1f);
        rt.anchoredPosition = new Vector2(0f, -48f);
        rt.sizeDelta        = new Vector2(0f, 70f);
        rt.offsetMin = new Vector2(20f, rt.offsetMin.y);
        rt.offsetMax = new Vector2(-20f, rt.offsetMax.y);

        var lvlCard = GlassCard("LvlCard", bar.transform);
        var lcRT    = lvlCard.GetComponent<RectTransform>();
        lcRT.anchorMin = new Vector2(0f, 0f); lcRT.anchorMax = new Vector2(0.30f, 1f);
        lcRT.offsetMin = lcRT.offsetMax = Vector2.zero;
        hudLevelText = MakeTMP("LvlTxt", lvlCard.transform, "LVL 1", 32f,
            FontStyles.Bold, Color.white, TextAlignmentOptions.Center);
        StretchRT(hudLevelText.rectTransform);

        var movCard = GlassCard("MovCard", bar.transform);
        var mcRT    = movCard.GetComponent<RectTransform>();
        mcRT.anchorMin = new Vector2(0.35f, 0f); mcRT.anchorMax = new Vector2(0.73f, 1f);
        mcRT.offsetMin = mcRT.offsetMax = Vector2.zero;

        var movLbl = MakeTMP("MovLbl", movCard.transform, "MOVES", 14f,
            FontStyles.Bold, new Color(1f, 1f, 1f, 0.6f), TextAlignmentOptions.Center);
        movLbl.rectTransform.anchorMin = new Vector2(0f, 0.55f);
        movLbl.rectTransform.anchorMax = new Vector2(1f, 1f);
        movLbl.rectTransform.offsetMin = movLbl.rectTransform.offsetMax = Vector2.zero;
        movLbl.characterSpacing = 2f;

        hudMovesText = MakeTMP("MovNum", movCard.transform, "0", 38f,
            FontStyles.Bold, Color.white, TextAlignmentOptions.Center);
        hudMovesText.rectTransform.anchorMin = new Vector2(0f, 0f);
        hudMovesText.rectTransform.anchorMax = new Vector2(1f, 0.6f);
        hudMovesText.rectTransform.offsetMin = hudMovesText.rectTransform.offsetMax = Vector2.zero;

        var hintCard = GlassCard("HintCard", bar.transform);
        var hcRT     = hintCard.GetComponent<RectTransform>();
        hcRT.anchorMin = new Vector2(0.77f, 0f); hcRT.anchorMax = new Vector2(1f, 1f);
        hcRT.offsetMin = hcRT.offsetMax = Vector2.zero;
        hintCard.AddComponent<Button>().targetGraphic = hintCard.GetComponent<Image>();
        MakeTMP("HintLbl", hintCard.transform, "HINT", 18f,
            FontStyles.Bold, Color.white, TextAlignmentOptions.Center);
    }

    void BuildBottomBar(Transform parent)
    {
        var bar = NewGO("BottomBar", parent);
        var rt  = bar.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0f, 0f); rt.anchorMax = new Vector2(1f, 0f);
        rt.pivot     = new Vector2(0.5f, 0f);
        rt.anchoredPosition = new Vector2(0f, 36f);
        rt.sizeDelta        = new Vector2(0f, 64f);
        rt.offsetMin = new Vector2(20f, rt.offsetMin.y);
        rt.offsetMax = new Vector2(-20f, rt.offsetMax.y);

        var hl = bar.AddComponent<HorizontalLayoutGroup>();
        hl.childAlignment        = TextAnchor.MiddleCenter;
        hl.spacing               = 12f;
        hl.childControlWidth     = true;  hl.childControlHeight     = true;
        hl.childForceExpandWidth = true;  hl.childForceExpandHeight = true;

        var pauseGO = MakeBtn("PauseBtn", bar.transform, "PAUSE", GLASS_BRD, 18f);
        pauseGO.AddComponent<LayoutElement>().flexibleWidth = 1f;
        pauseGO.GetComponent<Button>().onClick.AddListener(TogglePause);

        var undoGO = MakeBtn("UndoBtn", bar.transform, "UNDO", ORANGE, 22f);
        undoGO.AddComponent<LayoutElement>().flexibleWidth = 2f;
        var slab = NewGO("Slab", undoGO.transform);
        var sRT  = slab.GetComponent<RectTransform>();
        sRT.anchorMin = new Vector2(0f, 0f); sRT.anchorMax = new Vector2(1f, 0f);
        sRT.pivot = new Vector2(0.5f, 1f);
        sRT.offsetMin = new Vector2(3f, -7f); sRT.offsetMax = new Vector2(-3f, 0f);
        var sImg = slab.AddComponent<Image>(); sImg.color = ORANGE_DK; Round(sImg); sImg.raycastTarget = false;
        undoGO.GetComponent<Button>().onClick.AddListener(UndoMove);

        var homeGO = MakeBtn("HomeBtn", bar.transform, "HOME", GLASS_BRD, 18f);
        homeGO.AddComponent<LayoutElement>().flexibleWidth = 1f;
        homeGO.GetComponent<Button>().onClick.AddListener(GoHome);
    }

    // ── Win panel ─────────────────────────────────────────────────────────────

    GameObject BuildWinPanel()
    {
        var panel = NewGO("WinPanel", mainCanvas.transform);
        Stretch(panel);
        panel.AddComponent<Image>().color = new Color(0f, 0.05f, 0.15f, 0.78f);

        var card = NewGO("Card", panel.transform);
        var cRT  = card.GetComponent<RectTransform>();
        // Center the card in the middle of the screen
        cRT.anchorMin = new Vector2(0.5f, 0.5f); cRT.anchorMax = new Vector2(0.5f, 0.5f);
        cRT.pivot     = new Vector2(0.5f, 0.5f); cRT.anchoredPosition = Vector2.zero;
        cRT.sizeDelta = new Vector2(640f, 300f);
        var cImg = card.AddComponent<Image>(); cImg.color = DARK_BG; Round(cImg);

        var vl = card.AddComponent<VerticalLayoutGroup>();
        vl.childAlignment        = TextAnchor.UpperCenter;
        vl.spacing               = 14f;
        vl.padding               = new RectOffset(28, 28, 28, 28);
        vl.childControlWidth     = true;  vl.childControlHeight     = false;
        vl.childForceExpandWidth = true;  vl.childForceExpandHeight = false;

        // Title
        var titleGO = NewLayoutGO("WinTitle", card.transform, 40f);
        var t = titleGO.AddComponent<TextMeshProUGUI>();
        t.text = "LEVEL CLEAR!"; t.fontSize = 26f; t.fontStyle = FontStyles.Bold;
        t.color = ORANGE; t.alignment = TextAlignmentOptions.Center;

        // Stars row
        var starsRow = NewLayoutGO("WinStars", card.transform, 44f);
        var sHL = starsRow.AddComponent<HorizontalLayoutGroup>();
        sHL.childAlignment = TextAnchor.MiddleCenter; sHL.spacing = 14f;
        sHL.childControlWidth = false; sHL.childControlHeight = false;
        sHL.childForceExpandWidth = false; sHL.childForceExpandHeight = false;
        for (int i = 0; i < 3; i++)
        {
            var s  = NewGO($"Star{i}", starsRow.transform);
            s.GetComponent<RectTransform>().sizeDelta = new Vector2(38f, 38f);
            s.AddComponent<Image>().color = new Color(1f, 1f, 1f, 0.2f);
            Round(s.GetComponent<Image>());
        }

        // Button row — named exactly "BtnRow" so ShowWinPanel can find children
        var row = NewLayoutGO("BtnRow", card.transform, 62f);
        var rHL = row.AddComponent<HorizontalLayoutGroup>();
        rHL.childAlignment = TextAnchor.MiddleCenter; rHL.spacing = 12f;
        rHL.childControlWidth = true; rHL.childControlHeight = true;
        rHL.childForceExpandWidth = true; rHL.childForceExpandHeight = true;

        // REPLAY button — named exactly "ReplayBtn"
        var replayGO = MakeBtn("ReplayBtn", row.transform, "REPLAY", GLASS_BRD, 16f);
        replayGO.AddComponent<LayoutElement>().flexibleWidth = 1f;
        replayGO.GetComponent<Button>().onClick.AddListener(RestartLevel);

        // NEXT LEVEL button — named exactly "NextBtn"
        var nextGO = MakeBtn("NextBtn", row.transform, "NEXT LEVEL  ▶", ORANGE, 18f);
        nextGO.AddComponent<LayoutElement>().flexibleWidth = 2f;
        // 3D slab shadow
        var ns = NewGO("Slab", nextGO.transform);
        var nsRT = ns.GetComponent<RectTransform>();
        nsRT.anchorMin = new Vector2(0f, 0f); nsRT.anchorMax = new Vector2(1f, 0f);
        nsRT.pivot = new Vector2(0.5f, 1f);
        nsRT.offsetMin = new Vector2(3f, -7f); nsRT.offsetMax = new Vector2(-3f, 0f);
        var nsImg = ns.AddComponent<Image>(); nsImg.color = ORANGE_DK; Round(nsImg); nsImg.raycastTarget = false;
        // Wire NextLevel — done here AND re-wired in ShowWinPanel for safety
        nextGO.GetComponent<Button>().onClick.AddListener(NextLevel);

        Debug.Log($"BuildWinPanel complete — NextBtn path: {nextGO.transform.GetHierarchyPath()}");
        return panel;
    }

    // ── Pause panel ───────────────────────────────────────────────────────────

    GameObject BuildPausePanel()
    {
        var panel = NewGO("PausePanel", mainCanvas.transform);
        Stretch(panel);
        panel.AddComponent<Image>().color = new Color(0f, 0.05f, 0.15f, 0.80f);

        var card = NewGO("Card", panel.transform);
        var cRT  = card.GetComponent<RectTransform>();
        cRT.anchorMin = new Vector2(0.5f, 0.5f); cRT.anchorMax = new Vector2(0.5f, 0.5f);
        cRT.pivot = new Vector2(0.5f, 0.5f); cRT.anchoredPosition = Vector2.zero;
        cRT.sizeDelta = new Vector2(360f, 290f);
        var cImg = card.AddComponent<Image>(); cImg.color = DARK_BG; Round(cImg);

        var vl = card.AddComponent<VerticalLayoutGroup>();
        vl.childAlignment        = TextAnchor.UpperCenter;
        vl.spacing               = 14f;
        vl.padding               = new RectOffset(28, 28, 32, 28);
        vl.childControlWidth     = true;  vl.childControlHeight     = false;
        vl.childForceExpandWidth = true;  vl.childForceExpandHeight = false;

        var titleGO = NewLayoutGO("Title", card.transform, 42f);
        var t = titleGO.AddComponent<TextMeshProUGUI>();
        t.text = "PAUSED"; t.fontSize = 26f; t.fontStyle = FontStyles.Bold;
        t.color = Color.white; t.alignment = TextAlignmentOptions.Center;

        PauseCardBtn(card.transform, "ResumeBtn",  "RESUME",  true,  ResumeGame);
        PauseCardBtn(card.transform, "RestartBtn", "RESTART", false, RestartLevel);
        PauseCardBtn(card.transform, "HomeBtn2",   "HOME",    false, GoHome);

        return panel;
    }

    void PauseCardBtn(Transform parent, string name, string label, bool primary,
        UnityEngine.Events.UnityAction action)
    {
        var go  = NewLayoutGO(name, parent, 54f);
        var img = go.AddComponent<Image>();
        img.color = primary ? ORANGE : GLASS_BRD; Round(img);
        if (!primary)
        {
            var fill = NewGO("Fill", go.transform); StretchRT(fill.GetComponent<RectTransform>());
            fill.AddComponent<Image>().color = GLASS;
            Round(fill.GetComponent<Image>()); fill.GetComponent<Image>().raycastTarget = false;
        }
        var btn = go.AddComponent<Button>(); btn.targetGraphic = img;
        btn.onClick.AddListener(action);
        var lbl = MakeTMP("Lbl", go.transform, label, 18f, FontStyles.Bold, Color.white, TextAlignmentOptions.Center);
        StretchRT(lbl.rectTransform);
    }

    // ── UI primitives ─────────────────────────────────────────────────────────

    static void Refresh(TextMeshProUGUI t, string v)
    {
        if (t == null) return;
        t.text = string.Empty; t.ForceMeshUpdate();
        t.text = v;            t.ForceMeshUpdate();
    }

    static GameObject NewGO(string name, Transform parent)
    {
        var go = new GameObject(name); go.transform.SetParent(parent, false);
        go.AddComponent<RectTransform>(); return go;
    }

    static void Stretch(GameObject go)
    {
        var rt = go.GetComponent<RectTransform>() ?? go.AddComponent<RectTransform>();
        rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
        rt.offsetMin = rt.offsetMax = Vector2.zero;
    }

    static void StretchRT(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
        rt.offsetMin = rt.offsetMax = Vector2.zero;
    }

    static GameObject GlassCard(string name, Transform parent)
    {
        var go  = NewGO(name, parent);
        go.AddComponent<Image>().color = GLASS;
        Round(go.GetComponent<Image>());
        return go;
    }

    static GameObject MakeBtn(string name, Transform parent, string label, Color color, float fontSize)
    {
        var go  = NewGO(name, parent);
        var img = go.AddComponent<Image>(); img.color = color; Round(img);
        var btn = go.AddComponent<Button>(); btn.targetGraphic = img;
        var cols = btn.colors;
        cols.highlightedColor = new Color(1f, 1f, 1f, 0.9f);
        cols.pressedColor     = new Color(0.8f, 0.8f, 0.8f, 0.9f);
        btn.colors = cols;
        var lbl = MakeTMP("Lbl", go.transform, label, fontSize, FontStyles.Bold, Color.white, TextAlignmentOptions.Center);
        StretchRT(lbl.rectTransform);
        return go;
    }

    static GameObject NewLayoutGO(string name, Transform parent, float height)
    {
        var go = NewGO(name, parent);
        go.AddComponent<LayoutElement>().preferredHeight = height;
        return go;
    }

    static TextMeshProUGUI MakeTMP(string name, Transform parent, string text,
        float size, FontStyles style, Color color, TextAlignmentOptions align)
    {
        var go  = NewGO(name, parent);
        var tmp = go.AddComponent<TextMeshProUGUI>();
        tmp.text = text; tmp.fontSize = size; tmp.fontStyle = style;
        tmp.color = color; tmp.alignment = align; tmp.raycastTarget = false;
        return tmp;
    }

    static void Round(Image img) { img.sprite = GetRounded(); img.type = Image.Type.Sliced; }

    static Sprite GetRounded()
    {
        if (s_Rounded != null) return s_Rounded;
        const int sz = 64; const float r = 13f;
        var tex = new Texture2D(sz, sz, TextureFormat.RGBA32, false);
        for (int y = 0; y < sz; y++)
        for (int x = 0; x < sz; x++)
        {
            float cx = Mathf.Clamp(x+.5f,r,sz-r), cy = Mathf.Clamp(y+.5f,r,sz-r);
            float dx = x+.5f-cx, dy = y+.5f-cy;
            tex.SetPixel(x, y, dx*dx+dy*dy <= r*r ? Color.white : Color.clear);
        }
        tex.Apply();
        s_Rounded = Sprite.Create(tex, new Rect(0,0,sz,sz),
            new Vector2(.5f,.5f), 64f, 0, SpriteMeshType.FullRect, new Vector4(16,16,16,16));
        return s_Rounded;
    }

    static Color Hex(string h) { ColorUtility.TryParseHtmlString("#" + h, out Color c); return c; }
}

// Extension to help with debug logging of hierarchy paths
public static class TransformExtensions
{
    public static string GetHierarchyPath(this Transform t)
    {
        string path = t.name;
        while (t.parent != null) { t = t.parent; path = t.name + "/" + path; }
        return path;
    }
}