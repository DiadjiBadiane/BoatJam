// Assets/Scripts/Core/GameManager.cs
using System.Collections;
using System;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    const int FixedHeroCol = 1;
    const int FixedHeroRow = 4;
    const int FixedExitRow = 4;
    const bool FixedExitOnRight = true;

    public static GameManager Instance { get; private set; }

    [Header("References")]
    public LevelLoader levelLoader;
    public UIManager   uiManager;

    [Header("Levels")]
    [Tooltip("Drag all LevelData assets here in order")]
    public LevelData[] levels;

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

        EnsureLevelsLoadedFromResources();

        int startIndex = PlayerPrefs.GetInt("SelectedLevel", 0);
        LoadLevel(startIndex);
    }

    void EnsureLevelsLoadedFromResources()
    {
        var loaded = Resources.LoadAll<LevelData>("Levels");
        if (loaded == null || loaded.Length == 0)
            return;

        Array.Sort(loaded, (a, b) => GetLevelOrder(a).CompareTo(GetLevelOrder(b)));
        levels = loaded;

        string info = $"[GameManager] Loaded {levels.Length} level(s) from Resources: ";
        for (int i = 0; i < levels.Length; i++)
            info += $"{levels[i].name}[{levels[i].gridWidth}x{levels[i].gridHeight}, boats={levels[i].boats.Count}] ";
        Debug.Log(info);
    }

    int GetLevelOrder(LevelData level)
    {
        if (level == null || string.IsNullOrEmpty(level.name)) return int.MaxValue;

        string s = level.name;
        int value = 0;
        bool foundDigit = false;
        for (int i = 0; i < s.Length; i++)
        {
            if (char.IsDigit(s[i]))
            {
                foundDigit = true;
                value = value * 10 + (s[i] - '0');
            }
            else if (foundDigit)
            {
                break;
            }
        }

        return foundDigit ? value : int.MaxValue;
    }

    void OnEnable()  => BoatMovement.OnAnyBoatMoved += OnAnyBoatMoved;
    void OnDisable() => BoatMovement.OnAnyBoatMoved -= OnAnyBoatMoved;

    // ── Auto-advance trigger ──────────────────────────────────────────────────

    void OnAnyBoatMoved(BoatMovement movedBoat)
    {
        if (_autoAdvanceRunning || _levelCompletionShown) return;
        StartCoroutine(CheckAndAutoAdvance());
    }

    IEnumerator CheckAndAutoAdvance()
    {
        _autoAdvanceRunning = true;

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

        Vector2Int exitDir = CurrentLevel.exitOnRight ? Vector2Int.right : Vector2Int.left;

        while (!_levelCompletionShown)
        {
            yield return new WaitUntil(() => !hero.IsMoving);

            if (_levelCompletionShown) break;
            if (GridManager.Instance.HasHeroEscaped(hero)) break;

            yield return new WaitForSeconds(autoAdvanceStepDelay);

            hero.AutoMove(exitDir);
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

    // ── Update (safety net) ───────────────────────────────────────────────────

    void Update()
    {
        if (_levelCompletionShown || CurrentLevel == null || GridManager.Instance == null) return;

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
        if (hero == null || GridManager.Instance == null) return false;

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
        NormalizeLevelLayout(CurrentLevel);
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

    void NormalizeLevelLayout(LevelData level)
    {
        if (level == null)
            return;

        level.exitOnRight = FixedExitOnRight;
        level.exitRow = Mathf.Clamp(FixedExitRow, 0, Mathf.Max(0, level.gridHeight - 1));

        if (level.boats == null)
            level.boats = new System.Collections.Generic.List<BoatData>();

        BoatData hero = null;
        for (int i = 0; i < level.boats.Count; i++)
        {
            if (level.boats[i] != null && level.boats[i].isHero)
            {
                hero = level.boats[i];
                break;
            }
        }

        if (hero == null)
        {
            hero = new BoatData { id = "hero", isHero = true };
            level.boats.Insert(0, hero);
        }

        hero.id = "hero";
        hero.isHero = true;
        hero.isHorizontal = true;
        hero.size = 2;
        hero.width = 1;
        hero.col = Mathf.Clamp(FixedHeroCol, 0, Mathf.Max(0, level.gridWidth - hero.size));
        hero.row = Mathf.Clamp(FixedHeroRow, 0, Mathf.Max(0, level.gridHeight - hero.width));

        var occupied = new System.Collections.Generic.HashSet<Vector2Int>();
        StampBoatCells(hero, occupied);

        for (int i = 0; i < level.boats.Count; i++)
        {
            var boat = level.boats[i];
            if (boat == null || boat.isHero)
                continue;

            boat.size = Mathf.Max(1, boat.size);
            boat.width = Mathf.Max(1, boat.width);

            int spanX = boat.isHorizontal ? boat.size : boat.width;
            int spanY = boat.isHorizontal ? boat.width : boat.size;
            boat.col = Mathf.Clamp(boat.col, 0, Mathf.Max(0, level.gridWidth - spanX));
            boat.row = Mathf.Clamp(boat.row, 0, Mathf.Max(0, level.gridHeight - spanY));

            if (!CanPlaceBoat(boat, occupied, level.gridWidth, level.gridHeight))
                RepositionBoat(boat, occupied, level.gridWidth, level.gridHeight);

            StampBoatCells(boat, occupied);
        }
    }

    static bool CanPlaceBoat(BoatData boat, System.Collections.Generic.HashSet<Vector2Int> occupied, int gridWidth, int gridHeight)
    {
        int spanX = boat.isHorizontal ? boat.size : boat.width;
        int spanY = boat.isHorizontal ? boat.width : boat.size;

        if (boat.col < 0 || boat.row < 0 || boat.col + spanX > gridWidth || boat.row + spanY > gridHeight)
            return false;

        for (int dx = 0; dx < spanX; dx++)
        for (int dy = 0; dy < spanY; dy++)
            if (occupied.Contains(new Vector2Int(boat.col + dx, boat.row + dy)))
                return false;

        return true;
    }

    static void RepositionBoat(BoatData boat, System.Collections.Generic.HashSet<Vector2Int> occupied, int gridWidth, int gridHeight)
    {
        int spanX = boat.isHorizontal ? boat.size : boat.width;
        int spanY = boat.isHorizontal ? boat.width : boat.size;

        for (int row = 0; row <= gridHeight - spanY; row++)
        {
            for (int col = 0; col <= gridWidth - spanX; col++)
            {
                boat.col = col;
                boat.row = row;
                if (CanPlaceBoat(boat, occupied, gridWidth, gridHeight))
                    return;
            }
        }
    }

    static void StampBoatCells(BoatData boat, System.Collections.Generic.HashSet<Vector2Int> occupied)
    {
        int spanX = boat.isHorizontal ? boat.size : boat.width;
        int spanY = boat.isHorizontal ? boat.width : boat.size;

        for (int dx = 0; dx < spanX; dx++)
        for (int dy = 0; dy < spanY; dy++)
            occupied.Add(new Vector2Int(boat.col + dx, boat.row + dy));
    }

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

    // ── Win / completion ──────────────────────────────────────────────────────

    public void OnLevelComplete()
    {
        if (_levelCompletionShown) return;
        _levelCompletionShown = true;

        // Ask UIManager for the live move count, then let LevelData decide stars
        int moves = uiManager != null ? uiManager.GetMoveCount() : int.MaxValue;
        int stars  = CurrentLevel != null ? CurrentLevel.CalculateStars(moves) : 1;

        LevelProgress.SaveStars(CurrentLevelIndex, stars);
        LevelProgress.UnlockNextLevel(CurrentLevelIndex);

        if (uiManager == null)
            uiManager = UIManager.Instance ?? FindObjectOfType<UIManager>();

        if (uiManager != null)
            uiManager.ShowWinPanel(stars);
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