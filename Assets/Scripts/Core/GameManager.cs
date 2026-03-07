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

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    void Start()
    {
        // debug: load all level assets from Resources (regardless of inspector array)
        var loaded = Resources.LoadAll<LevelData>("Levels");
        string info = $"Resources.LoadAll found {loaded.Length} level(s): ";
        foreach (var l in loaded)
            info += $"{l.name}[{l.gridWidth}x{l.gridHeight}, boats={l.boats.Count}] ";
        Debug.Log(info);


        int startIndex = PlayerPrefs.GetInt("SelectedLevel", 0);
        LoadLevel(startIndex);
    }

    public void LoadLevel(int index)
    {
        if (levels == null || levels.Length == 0) { Debug.LogError("GameManager: no levels assigned!"); return; }
        index = Mathf.Clamp(index, 0, levels.Length - 1);
        CurrentLevelIndex = index;
        CurrentLevel      = levels[index];
        levelLoader.LoadLevel(CurrentLevel);
        uiManager.ShowGame(index + 1);
    }

    public void OnLevelComplete()
    {
        LevelProgress.SaveStars(CurrentLevelIndex, starsAwardedOnWin);
        LevelProgress.UnlockNextLevel(CurrentLevelIndex);
        uiManager.ShowWinPanel();
    }

    public void ReloadCurrentLevel()
    {
        LoadLevel(CurrentLevelIndex);
    }

    public void LoadNextLevel()
    {
        int next = CurrentLevelIndex + 1;
        if (next >= levels.Length)
            SceneManager.LoadScene("MainMenu");
        else
            LoadLevel(next);
    }
}