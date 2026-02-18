using UnityEngine;

public static class GameRunState
{
    private const string HighestUnlockedLevelKey = "restore_rush_highest_unlocked_level";

    public static int CurrentLevelIndex;
    public static int HighestUnlockedLevelIndex;
    public static int LastLevelIndex;
    public static string LastLevelName;
    public static bool LastRunWon;
    public static int LastActions;
    public static float LastDurationSeconds;
    public static float LastCleanPercent;

    public static void LoadProgress()
    {
        HighestUnlockedLevelIndex = ClampLevel(PlayerPrefs.GetInt(HighestUnlockedLevelKey, 0));
        CurrentLevelIndex = Mathf.Clamp(CurrentLevelIndex, 0, HighestUnlockedLevelIndex);
    }

    public static void SaveProgress()
    {
        PlayerPrefs.SetInt(HighestUnlockedLevelKey, ClampLevel(HighestUnlockedLevelIndex));
        PlayerPrefs.Save();
    }

    public static void SetCurrentLevel(int levelIndex)
    {
        CurrentLevelIndex = Mathf.Clamp(ClampLevel(levelIndex), 0, HighestUnlockedLevelIndex);
    }

    public static void UnlockLevel(int levelIndex)
    {
        var safeLevel = ClampLevel(levelIndex);
        if (safeLevel <= HighestUnlockedLevelIndex)
        {
            return;
        }

        HighestUnlockedLevelIndex = safeLevel;
        SaveProgress();
    }

    private static int ClampLevel(int levelIndex)
    {
        return Mathf.Clamp(levelIndex, 0, Mathf.Max(0, RestoreLevelCatalog.Count - 1));
    }
}
