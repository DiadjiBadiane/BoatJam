using UnityEngine;

public static class LevelProgress
{
    private const string LevelsUnlockedKey = "LevelsUnlocked";
    private const string LevelStarsPrefix = "LevelStars_";

    public static int GetUnlockedLevelCount()
    {
        return Mathf.Max(1, PlayerPrefs.GetInt(LevelsUnlockedKey, 1));
    }

    public static bool IsLevelUnlocked(int levelIndex)
    {
        // levelIndex is zero-based, unlocked count is one-based.
        return levelIndex >= 0 && levelIndex < GetUnlockedLevelCount();
    }

    public static int GetStars(int levelIndex)
    {
        if (levelIndex < 0) return 0;
        return Mathf.Clamp(PlayerPrefs.GetInt(GetStarsKey(levelIndex), 0), 0, 3);
    }

    public static void SaveStars(int levelIndex, int starsEarned)
    {
        if (levelIndex < 0) return;

        int clampedStars = Mathf.Clamp(starsEarned, 0, 3);
        int existingStars = GetStars(levelIndex);
        if (clampedStars <= existingStars) return;

        PlayerPrefs.SetInt(GetStarsKey(levelIndex), clampedStars);
        PlayerPrefs.Save();
    }

    public static void UnlockNextLevel(int justCompletedLevelIndex)
    {
        if (justCompletedLevelIndex < 0) return;

        int currentUnlocked = GetUnlockedLevelCount();
        int desiredUnlocked = justCompletedLevelIndex + 2;
        if (desiredUnlocked <= currentUnlocked) return;

        PlayerPrefs.SetInt(LevelsUnlockedKey, desiredUnlocked);
        PlayerPrefs.Save();
    }

    private static string GetStarsKey(int levelIndex)
    {
        return $"{LevelStarsPrefix}{levelIndex}";
    }
}
