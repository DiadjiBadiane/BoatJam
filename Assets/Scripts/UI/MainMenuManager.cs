// Assets/Scripts/UI/MainMenuManager.cs
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;
using System.Collections;

/// <summary>
/// Controls the Main Menu scene.
/// Panels: HomePanel, SettingsPanel, CreditsPanel, LevelSelectPanel.
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
    public Slider  musicSlider;
    public Slider  sfxSlider;
    public Toggle  vibrationToggle;
    public Button  settingsCloseButton;

    // ── Credits ───────────────────────────────────────────────────────────────
    [Header("Credits")]
    public Button creditsCloseButton;

    // ── Level Select ──────────────────────────────────────────────────────────
    [Header("Level Select")]
    public Transform    levelButtonContainer;  // parent for level buttons
    public GameObject   levelButtonPrefab;     // prefab: Button + TMP label
    public Button       levelSelectCloseButton;
    private int         totalLevels;           // determined dynamically from Resources

    // ── Version ───────────────────────────────────────────────────────────────
    [Header("Version")]
    public TextMeshProUGUI versionLabel;

    // ── Const ──────────────────────────────────────────────────────────────────
    private const string MUSIC_VOL_KEY     = "MusicVolume";
    private const string SFX_VOL_KEY       = "SFXVolume";
    private const string VIBRATION_KEY     = "Vibration";
    private const string LEVELS_UNLOCKED_KEY = "LevelsUnlocked";

    // ─────────────────────────────────────────────────────────────────────────

    void Start()
    {
        // Determine total levels from Resources
        var loadedLevels = Resources.LoadAll<LevelData>("Levels");
        totalLevels = loadedLevels.Length;
        Debug.Log($"MainMenuManager: Found {totalLevels} levels in Resources/Levels");

        // Version label
        if (versionLabel != null)
            versionLabel.text = $"v{Application.version}";

        // Find buttons if not assigned (for dynamic UI)
        if (levelSelectButton == null)
            levelSelectButton = GameObject.Find("LevelSelectButton")?.GetComponent<Button>()
                             ?? GameObject.Find("LevelsButton")?.GetComponent<Button>();
        if (playButton == null)
            playButton = GameObject.Find("PlayButton")?.GetComponent<Button>();
        if (settingsButton == null)
            settingsButton = GameObject.Find("SettingsButton")?.GetComponent<Button>();
        if (creditsButton == null)
            creditsButton = GameObject.Find("CreditsButton")?.GetComponent<Button>();
        if (quitButton == null)
            quitButton = GameObject.Find("QuitButton")?.GetComponent<Button>();

        // Find panel references if not assigned (works with manually-created hierarchy)
        if (levelSelectPanel == null)
            levelSelectPanel = GameObject.Find("LevelSelectPanel");
        if (homePanel == null)
            homePanel = GameObject.Find("HomePanel");
        if (levelButtonContainer == null && levelSelectPanel != null)
        {
            Transform content = levelSelectPanel.transform.Find("LevelScrollView/Viewport/Content")
                             ?? levelSelectPanel.transform.Find("Content");
            if (content != null)
                levelButtonContainer = content;
        }
        if (levelSelectCloseButton == null && levelSelectPanel != null)
            levelSelectCloseButton = levelSelectPanel.GetComponentInChildren<Button>(true);

        // Create a runtime template if no prefab was assigned in Inspector.
        EnsureRuntimeLevelButtonPrefab();

        // Keep menu canvas in a stable UI mode (prevents camera-coupled UI distortion).
        NormalizeMenuCanvas();

        // Only normalize the level panel; keep existing home/settings/credits layout intact.
        NormalizePanelRect(levelSelectPanel);

        // Wire up buttons
        playButton?.onClick.AddListener(OnPlayPressed);
        levelSelectButton?.onClick.AddListener(OpenLevelSelect);
        settingsButton?.onClick.AddListener(OpenSettings);
        creditsButton?.onClick.AddListener(OpenCredits);
        quitButton?.onClick.AddListener(OnQuitPressed);

        settingsCloseButton?.onClick.AddListener(CloseSettings);
        creditsCloseButton?.onClick.AddListener(CloseCredits);
        levelSelectCloseButton?.onClick.AddListener(CloseLevelSelect);

        // Load saved settings
        LoadSettings();

        // Subscribe to slider / toggle changes
        musicSlider?.onValueChanged.AddListener(v => PlayerPrefs.SetFloat(MUSIC_VOL_KEY, v));
        sfxSlider?.onValueChanged.AddListener(v => PlayerPrefs.SetFloat(SFX_VOL_KEY, v));
        vibrationToggle?.onValueChanged.AddListener(v => PlayerPrefs.SetInt(VIBRATION_KEY, v ? 1 : 0));

        // Show only home panel
        ShowPanel(homePanel);
    }

    void NormalizePanelRect(GameObject panel)
    {
        if (panel == null) return;

        RectTransform rect = panel.GetComponent<RectTransform>();
        if (rect == null) return;

        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
        rect.anchoredPosition = Vector2.zero;
        rect.localScale = Vector3.one;
    }

    void NormalizeMenuCanvas()
    {
        Canvas canvas = null;
        if (homePanel != null)
            canvas = homePanel.GetComponentInParent<Canvas>();
        if (canvas == null && levelSelectPanel != null)
            canvas = levelSelectPanel.GetComponentInParent<Canvas>();
        if (canvas == null)
            return;

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

    void EnsureRuntimeLevelButtonPrefab()
    {
        if (levelButtonPrefab != null || levelButtonContainer == null)
            return;

        var template = new GameObject("LevelButtonTemplate");
        template.transform.SetParent(levelButtonContainer, false);
        var rect = template.AddComponent<RectTransform>();
        rect.sizeDelta = new Vector2(150f, 70f);

        var image = template.AddComponent<Image>();
        image.color = new Color(1f, 1f, 1f, 0.12f);
        template.AddComponent<Button>();

        var textObj = new GameObject("Text");
        textObj.transform.SetParent(template.transform, false);
        var textRect = textObj.AddComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = Vector2.zero;
        textRect.offsetMax = Vector2.zero;
        var label = textObj.AddComponent<TextMeshProUGUI>();
        label.text = "1";
        label.alignment = TextAlignmentOptions.Center;
        label.fontSize = 30;
        label.fontStyle = FontStyles.Bold;
        label.color = Color.white;

        var lockObj = new GameObject("LockIcon");
        lockObj.transform.SetParent(template.transform, false);
        var lockRect = lockObj.AddComponent<RectTransform>();
        lockRect.anchorMin = new Vector2(0.5f, 0.2f);
        lockRect.anchorMax = new Vector2(0.5f, 0.2f);
        lockRect.sizeDelta = new Vector2(80f, 24f);
        lockRect.anchoredPosition = Vector2.zero;
        var lockLabel = lockObj.AddComponent<TextMeshProUGUI>();
        lockLabel.text = "LOCK";
        lockLabel.alignment = TextAlignmentOptions.Center;
        lockLabel.fontSize = 16;
        lockLabel.color = new Color(1f, 0.85f, 0.3f, 0.95f);
        lockObj.SetActive(false);

        template.SetActive(false);
        levelButtonPrefab = template;
    }

    // ── Navigation ────────────────────────────────────────────────────────────

    void ShowPanel(GameObject target)
    {
        if (target == null)
        {
            Debug.LogError("MainMenuManager: Tried to show a null panel. Check Inspector references.");
            if (homePanel != null)
                homePanel.SetActive(true);
            return;
        }

        homePanel?.SetActive(false);
        settingsPanel?.SetActive(false);
        creditsPanel?.SetActive(false);
        levelSelectPanel?.SetActive(false);
        target?.SetActive(true);
    }

    public void OnPlayPressed()
    {
        // Jump straight to level 1
        PlayerPrefs.SetInt("SelectedLevel", 0);
        PlayerPrefs.Save();
        SceneManager.LoadScene("GameScene");
    }

    public void OpenLevelSelect()
    {
        if (levelSelectPanel == null)
        {
            Debug.LogError("MainMenuManager: levelSelectPanel is not assigned in Inspector.");
            ShowPanel(homePanel);
            return;
        }

        if (levelButtonContainer == null || levelButtonPrefab == null)
        {
            Debug.LogError("MainMenuManager: Level select references missing (levelButtonContainer or levelButtonPrefab).");
            ShowPanel(homePanel);
            return;
        }

        NormalizePanelRect(levelSelectPanel);
        NormalizeLevelSelectLayout();
        ShowPanel(levelSelectPanel);
        levelSelectPanel.transform.SetAsLastSibling();

        // Ensure RectTransform sizes are valid before computing grid button sizes.
        Canvas.ForceUpdateCanvases();
        RectTransform containerRect = levelButtonContainer as RectTransform;
        if (containerRect != null)
            LayoutRebuilder.ForceRebuildLayoutImmediate(containerRect);

        BuildLevelButtons();
    }

    void NormalizeLevelSelectLayout()
    {
        if (levelSelectPanel == null) return;

        // Ensure fullscreen panel with a visible dim background overlay.
        RectTransform panelRect = levelSelectPanel.GetComponent<RectTransform>();
        if (panelRect != null)
        {
            panelRect.anchorMin = Vector2.zero;
            panelRect.anchorMax = Vector2.one;
            panelRect.offsetMin = Vector2.zero;
            panelRect.offsetMax = Vector2.zero;
            panelRect.localScale = Vector3.one;
        }

        Image panelBg = levelSelectPanel.GetComponent<Image>();
        if (panelBg == null)
            panelBg = levelSelectPanel.AddComponent<Image>();
        panelBg.color = new Color(0.05f, 0.1f, 0.18f, 0.92f);

        // Expand scroll view area.
        Transform scroll = levelSelectPanel.transform.Find("Scroll View")
                        ?? levelSelectPanel.transform.Find("LevelScrollView");
        if (scroll != null)
        {
            RectTransform scrollRect = scroll.GetComponent<RectTransform>();
            if (scrollRect != null)
            {
                scrollRect.anchorMin = new Vector2(0.08f, 0.2f);
                scrollRect.anchorMax = new Vector2(0.92f, 0.85f);
                scrollRect.offsetMin = Vector2.zero;
                scrollRect.offsetMax = Vector2.zero;
                scrollRect.localScale = Vector3.one;
            }
        }

        // Place close button at bottom center.
        Button closeBtn = levelSelectCloseButton;
        if (closeBtn == null)
            closeBtn = levelSelectPanel.transform.Find("CloseButton")?.GetComponent<Button>()
                    ?? levelSelectPanel.transform.Find("LevelSelectCloseButton")?.GetComponent<Button>();

        if (closeBtn != null)
        {
            RectTransform closeRect = closeBtn.GetComponent<RectTransform>();
            if (closeRect != null)
            {
                closeRect.anchorMin = new Vector2(0.5f, 0.08f);
                closeRect.anchorMax = new Vector2(0.5f, 0.08f);
                closeRect.pivot = new Vector2(0.5f, 0.5f);
                closeRect.anchoredPosition = Vector2.zero;
                closeRect.sizeDelta = new Vector2(260f, 56f);
                closeRect.localScale = Vector3.one;
            }
        }
    }

    public void CloseLevelSelect() => ShowPanel(homePanel);

    public void OpenSettings()     => ShowPanel(settingsPanel);
    public void CloseSettings()
    {
        PlayerPrefs.Save();
        ShowPanel(homePanel);
    }

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

    // ── Level Select builder ──────────────────────────────────────────────────

    void BuildLevelButtons()
    {
        if (levelButtonContainer == null || levelButtonPrefab == null) return;

        ConfigureLevelGridLayout();

        // Clear old generated buttons but keep the template if it lives in the same container.
        foreach (Transform child in levelButtonContainer)
        {
            if (child.gameObject == levelButtonPrefab)
                continue;
            Destroy(child.gameObject);
        }

        for (int i = 0; i < totalLevels; i++)
        {
            int levelIndex = i; // capture for lambda
            GameObject btn = Instantiate(levelButtonPrefab, levelButtonContainer);
            btn.SetActive(true);
            btn.name = $"LevelBtn_{i + 1}";

            TextMeshProUGUI label = btn.GetComponentInChildren<TextMeshProUGUI>();
            if (label != null) label.text = (i + 1).ToString();

            Button b = btn.GetComponent<Button>();
            bool isUnlocked = true; // all levels selectable from the level-select screen

            if (b != null)
            {
                b.interactable = isUnlocked;
                if (isUnlocked)
                    b.onClick.AddListener(() => LoadLevel(levelIndex));
            }

            // Visual lock/unlock indicator
            Transform lockIcon = btn.transform.Find("LockIcon");
            if (lockIcon != null) lockIcon.gameObject.SetActive(!isUnlocked);
        }
    }

    void ConfigureLevelGridLayout()
    {
        var grid = levelButtonContainer.GetComponent<GridLayoutGroup>();
        if (grid == null) return;

        // Fill width with 4 columns (or fewer if there are less levels).
        int columns = Mathf.Max(1, Mathf.Min(4, totalLevels));
        float horizontalPadding = 24f;
        float spacing = 16f;

        RectTransform viewportRect = levelButtonContainer.parent as RectTransform;
        RectTransform contentRect = levelButtonContainer as RectTransform;
        if (viewportRect == null || contentRect == null) return;

        // Stretch content to viewport width so the grid can occupy full row width.
        contentRect.anchorMin = new Vector2(0f, 1f);
        contentRect.anchorMax = new Vector2(1f, 1f);
        contentRect.pivot = new Vector2(0.5f, 1f);
        contentRect.offsetMin = new Vector2(horizontalPadding, contentRect.offsetMin.y);
        contentRect.offsetMax = new Vector2(-horizontalPadding, contentRect.offsetMax.y);

        float availableWidth = viewportRect.rect.width - (horizontalPadding * 2f);
        float totalSpacing = spacing * (columns - 1);
        float cellWidth = Mathf.Max(90f, (availableWidth - totalSpacing) / columns);
        float cellHeight = Mathf.Max(70f, cellWidth * 0.6f);

        grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        grid.constraintCount = columns;
        grid.spacing = new Vector2(spacing, spacing);
        grid.cellSize = new Vector2(cellWidth, cellHeight);
        grid.childAlignment = TextAnchor.UpperLeft;
    }

    void LoadLevel(int index)
    {
        PlayerPrefs.SetInt("SelectedLevel", index);
        PlayerPrefs.Save();
        SceneManager.LoadScene("GameScene");
    }

    // ── Settings persistence ──────────────────────────────────────────────────

    void LoadSettings()
    {
        if (musicSlider != null)
            musicSlider.value = PlayerPrefs.GetFloat(MUSIC_VOL_KEY, 0.8f);
        if (sfxSlider != null)
            sfxSlider.value = PlayerPrefs.GetFloat(SFX_VOL_KEY, 1f);
        if (vibrationToggle != null)
            vibrationToggle.isOn = PlayerPrefs.GetInt(VIBRATION_KEY, 1) == 1;
    }

    // ── Public helper (called by GameManager after completing a level) ─────────
    public static void UnlockNextLevel(int justCompleted)
    {
        int current = PlayerPrefs.GetInt(LEVELS_UNLOCKED_KEY, 1);
        int newUnlock = justCompleted + 2; // unlock the level after the one just beaten
        if (newUnlock > current)
        {
            PlayerPrefs.SetInt(LEVELS_UNLOCKED_KEY, newUnlock);
            PlayerPrefs.Save();
        }
    }
}
