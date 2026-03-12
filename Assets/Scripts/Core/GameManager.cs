// Assets/Scripts/Core/GameManager.cs
using System.Collections;
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

    [Header("Auto-Advance")]
    [Tooltip("Seconds to wait between each auto-step when the hero path is clear")]
    [SerializeField] float autoAdvanceStepDelay = 0.18f;

    public LevelData CurrentLevel      { get; private set; }
    public int       CurrentLevelIndex { get; private set; }

    bool _levelCompletionShown;
    int  _framesSinceLoad;
    bool _autoAdvanceRunning;

    // ── Lifecycle ─────────────────────────────────────────────────────────────

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Debug.LogWarning($"[GameManager] Duplicate — destroying {gameObject.name} in scene '{gameObject.scene.name}'");
            Destroy(gameObject);
            return;
        }
        Instance = this;
        Debug.Log($"[GameManager] Awake in scene '{gameObject.scene.name}'");
    }

    void Start()
    {
        uiManager = UIManager.Instance ?? FindObjectOfType<UIManager>();

        var loaded = Resources.LoadAll<LevelData>("Levels");
        string info = $"Resources.LoadAll found {loaded.Length} level(s): ";
        foreach (var l in loaded)
            info += $"{l.name}[{l.gridWidth}x{l.gridHeight}, boats={l.boats.Count}] ";
        Debug.Log(info);

        int startIndex = PlayerPrefs.GetInt("SelectedLevel", 0);
        LoadLevel(startIndex);
    }

    void OnEnable()  => BoatMovement.OnAnyBoatMoved += OnAnyBoatMoved;
    void OnDisable() => BoatMovement.OnAnyBoatMoved -= OnAnyBoatMoved;

    // ── Auto-advance trigger ──────────────────────────────────────────────────

    void OnAnyBoatMoved(BoatMovement movedBoat)
    {
        // Don't start a second coroutine if one is already running
        if (_autoAdvanceRunning || _levelCompletionShown) return;

        // Wait until the moving boat has finished its animation, then check
        StartCoroutine(CheckAndAutoAdvance());
    }

    IEnumerator CheckAndAutoAdvance()
    {
        _autoAdvanceRunning = true;

        // Wait for all boats to finish their current animation
        yield return new WaitUntil(() => !AnyBoatMoving());

        if (_levelCompletionShown) { _autoAdvanceRunning = false; yield break; }

        BoatMovement hero = FindHero();
        if (hero == null) { _autoAdvanceRunning = false; yield break; }

        if (!GridManager.Instance.IsHeroPathClear(hero))
        {
            _autoAdvanceRunning = false;
            yield break;
        }

        Debug.Log("[GameManager] Hero path is clear — auto-advancing!");

        // Step the hero forward until it escapes
        Vector2Int exitDir = CurrentLevel.exitOnRight ? Vector2Int.right : Vector2Int.left;

        while (!_levelCompletionShown)
        {
            // Wait for the previous step animation to finish
            yield return new WaitUntil(() => !hero.IsMoving);

            if (_levelCompletionShown) break;

            // Has the hero already escaped?
            if (GridManager.Instance.HasHeroEscaped(hero)) break;

            // Small delay between steps so it feels smooth, not instant
            yield return new WaitForSeconds(autoAdvanceStepDelay);

            hero.TryMove(exitDir);

            // Give it at least one frame to start moving
            yield return null;
        }

        _autoAdvanceRunning = false;
    }

    BoatMovement FindHero()
    {
        foreach (var b in FindObjectsOfType<BoatMovement>())
            if (b != null && b.isHero) return b;
        return null;
    }

    bool AnyBoatMoving()
    {
        foreach (var b in FindObjectsOfType<BoatMovement>())
            if (b != null && b.IsMoving) return true;
        return false;
    }

    // ── Existing Update (kept as safety net for edge-cases) ───────────────────

    void Update()
    {
        if (_levelCompletionShown || CurrentLevel == null) return;

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
        _framesSinceLoad      = 0;
        _autoAdvanceRunning   = false;

        StopAllCoroutines();

        levelLoader.LoadLevel(CurrentLevel);

        if (uiManager == null)
            uiManager = UIManager.Instance ?? FindObjectOfType<UIManager>();
        uiManager?.ShowGame(index + 1);

        StartCoroutine(FitCameraWhenReady(CurrentLevel.gridWidth, CurrentLevel.gridHeight));
    }

    // ── Find fitter only within THIS GameManager's scene ─────────────────────

    ResponsiveCameraFitter FindFitterInMyScene()
    {
        foreach (var root in gameObject.scene.GetRootGameObjects())
        {
            var f = root.GetComponentInChildren<ResponsiveCameraFitter>(true);
            if (f != null)
            {
                Debug.Log($"[GameManager] Found ResponsiveCameraFitter on '{f.gameObject.name}' (active={f.gameObject.activeInHierarchy})");
                return f;
            }
        }
        Debug.LogError($"[GameManager] ResponsiveCameraFitter not found anywhere in scene '{gameObject.scene.name}'!");
        return null;
    }

    IEnumerator FitCameraWhenReady(int expectedW, int expectedH)
    {
        Debug.Log($"[GameManager] Waiting for grid {expectedW}x{expectedH} in scene '{gameObject.scene.name}'");

        int attempts = 0;
        const int maxAttempts = 60;

        while (attempts < maxAttempts)
        {
            yield return null;
            attempts++;

            if (GridManager.Instance == null) continue;

            int gw = GridManager.Instance.width;
            int gh = GridManager.Instance.height;
            if (gw != expectedW || gh != expectedH) continue;

            // Grid dimensions match — find and fire the fitter
            var fitter = FindFitterInMyScene();
            if (fitter == null) yield break;

            if (!fitter.gameObject.activeInHierarchy)
                fitter.gameObject.SetActive(true);

            fitter.FitNow();
            Debug.Log($"[GameManager] FitNow() called after {attempts} frame(s)");
            yield break;
        }

        Debug.LogWarning("[GameManager] Timeout — forcing fit");
        var f = FindFitterInMyScene();
        if (f != null) f.FitNow();
    }

    public void OnLevelComplete()
    {
        if (_levelCompletionShown) return;
        _levelCompletionShown = true;

        LevelProgress.SaveStars(CurrentLevelIndex, starsAwardedOnWin);
        LevelProgress.UnlockNextLevel(CurrentLevelIndex);

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