// Assets/Scripts/Core/GameManager.cs
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("References")]
    public LevelLoader levelLoader;
    public UIManager   uiManager;

    [Header("Levels")]
    [Tooltip("Drag all LevelData assets here in order")]
    public LevelData[] levels;

    [Header("Progress")]
    [Range(1, 3)]
    public int starsAwardedOnWin = 3;

    public LevelData CurrentLevel      { get; private set; }
    public int       CurrentLevelIndex { get; private set; }

    bool _levelCompletionShown;
    int  _framesSinceLoad;          // guard: skip win-check for 2 frames after load

    // ── Lifecycle ─────────────────────────────────────────────────────────────

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    void Start()
    {
        // Always resolve uiManager at runtime — never trust a stale Inspector ref.
        uiManager = UIManager.Instance ?? FindObjectOfType<UIManager>();

        // Log level assets found in Resources (debug helper)
        var loaded = Resources.LoadAll<LevelData>("Levels");
        string info = $"Resources.LoadAll found {loaded.Length} level(s): ";
        foreach (var l in loaded)
            info += $"{l.name}[{l.gridWidth}x{l.gridHeight}, boats={l.boats.Count}] ";
        Debug.Log(info);

        int startIndex = PlayerPrefs.GetInt("SelectedLevel", 0);
        LoadLevel(startIndex);
    }

    void Update()
    {
        if (_levelCompletionShown || CurrentLevel == null) return;

        // Skip the first 2 frames after a level loads — LevelLoader needs at least
        // one frame to destroy old boats and spawn new ones. Without this guard,
        // Update() sees the hero from the PREVIOUS level still at the exit and
        // immediately fires OnLevelComplete() on the brand-new level.
        _framesSinceLoad++;
        if (_framesSinceLoad < 3) return;

        var boats = FindObjectsOfType<BoatMovement>();
        foreach (var b in boats)
        {
            if (b == null || !b.isHero) continue;
            if (b.IsMoving) return;

            if (GridManager.Instance.HasHeroEscaped(b) || HeroTouchingExitLane(b))
            {
                OnLevelComplete();
                return;
            }
        }
    }

    bool HeroTouchingExitLane(BoatMovement hero)
    {
        var cells = hero.GetOccupiedCells();
        foreach (var cell in cells)
        {
            if ( CurrentLevel.exitOnRight && cell.x >= GridManager.Instance.width && cell.y == CurrentLevel.exitRow) return true;
            if (!CurrentLevel.exitOnRight && cell.x < 0                           && cell.y == CurrentLevel.exitRow) return true;
        }
        return false;
    }

    // ── Level management ──────────────────────────────────────────────────────

    public void LoadLevel(int index)
    {
        if (levels == null || levels.Length == 0)
        {
            Debug.LogError("GameManager: no levels assigned!");
            return;
        }

        index = Mathf.Clamp(index, 0, levels.Length - 1);
        CurrentLevelIndex     = index;
        CurrentLevel          = levels[index];
        _levelCompletionShown = false;
        _framesSinceLoad      = 0;          // reset win-check guard

        levelLoader.LoadLevel(CurrentLevel);

        // Re-resolve UIManager in case it was rebuilt (e.g. scene reload)
        if (uiManager == null)
            uiManager = UIManager.Instance ?? FindObjectOfType<UIManager>();

        uiManager?.ShowGame(index + 1);
    }

    public void OnLevelComplete()
    {
        if (_levelCompletionShown) return;
        _levelCompletionShown = true;

        LevelProgress.SaveStars(CurrentLevelIndex, starsAwardedOnWin);
        LevelProgress.UnlockNextLevel(CurrentLevelIndex);

        // Re-resolve UIManager defensively before showing win panel
        if (uiManager == null)
            uiManager = UIManager.Instance ?? FindObjectOfType<UIManager>();

        if (uiManager != null)
            uiManager.ShowWinPanel();
        else
            Debug.LogError("GameManager.OnLevelComplete: UIManager not found!");
    }

    public void ReloadCurrentLevel() => LoadLevel(CurrentLevelIndex);

    public void LoadNextLevel()
    {
        int next = CurrentLevelIndex + 1;
        if (next >= levels.Length)
            SceneManager.LoadScene("MainMenu");
        else
            LoadLevel(next);
    }
}