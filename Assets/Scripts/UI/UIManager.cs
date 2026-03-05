// Assets/Scripts/UI/UIManager.cs
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class UIManager : MonoBehaviour
{
    [Header("UI References")]
    public TMPro.TextMeshProUGUI levelLabel;
    public GameObject winPanel;
    public GameObject pausePanel;

    private GameManager gameManager;

    void Start()
    {
        gameManager = FindObjectOfType<GameManager>();
        if (winPanel != null)   winPanel.SetActive(false);
        if (pausePanel != null) pausePanel.SetActive(false);
        Debug.Log("UIManager started");
    }

    // =========================
    // SHOW GAME HUD
    // =========================
    public void ShowGame(int levelNumber)
    {
        if (winPanel != null)   winPanel.SetActive(false);
        if (pausePanel != null) pausePanel.SetActive(false);
        if (levelLabel != null) levelLabel.text = $"Level {levelNumber}";
        Time.timeScale = 1f;
    }

    // =========================
    // SHOW WIN PANEL
    // =========================
    public void ShowWinPanel()
    {
        if (winPanel != null) winPanel.SetActive(true);
        if (pausePanel != null) pausePanel.SetActive(false);
        
        // Ensure the button is interactable
        var button = winPanel.GetComponentInChildren<Button>();
        if (button != null)
        {
            button.interactable = true;
        }
    }

    // =========================
    // PAUSE
    // =========================
    public void TogglePause()
    {
        if (pausePanel == null) { Debug.LogError("UIManager: pausePanel not assigned!"); return; }
        bool isPaused = !pausePanel.activeSelf;
        pausePanel.SetActive(isPaused);
        Time.timeScale = isPaused ? 0f : 1f;
        Debug.Log($"Pause toggled: {isPaused}");
    }

    // =========================
    // RESTART LEVEL
    // =========================
    public void RestartLevel()
    {
        Time.timeScale = 1f;
        gameManager.ReloadCurrentLevel();
    }

    // =========================
    // NEXT LEVEL
    // =========================
    public void NextLevel()
    {
        Time.timeScale = 1f;
        gameManager.LoadNextLevel();
        Debug.Log("Next level clicked");
    }

    // =========================
    // GO HOME
    // =========================
    public void GoHome()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene("MainMenu");
        Debug.Log("Go home clicked");
    }
}