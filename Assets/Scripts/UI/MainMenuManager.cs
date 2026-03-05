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
    public int          totalLevels = 10;      // update as you add levels

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
        
        // Version label
        if (versionLabel != null)
            versionLabel.text = $"v{Application.version}";

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

    // ── Navigation ────────────────────────────────────────────────────────────

    void ShowPanel(GameObject target)
    {
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
        BuildLevelButtons();
        ShowPanel(levelSelectPanel);
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

        // Clear old buttons
        foreach (Transform child in levelButtonContainer)
            Destroy(child.gameObject);

        int unlocked = PlayerPrefs.GetInt(LEVELS_UNLOCKED_KEY, 1);

        for (int i = 0; i < totalLevels; i++)
        {
            int levelIndex = i; // capture for lambda
            GameObject btn = Instantiate(levelButtonPrefab, levelButtonContainer);
            btn.name = $"LevelBtn_{i + 1}";

            TextMeshProUGUI label = btn.GetComponentInChildren<TextMeshProUGUI>();
            if (label != null) label.text = (i + 1).ToString();

            Button b = btn.GetComponent<Button>();
            bool isUnlocked = (i < unlocked);

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
