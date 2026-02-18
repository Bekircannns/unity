using UnityEngine;

public static class GameRunState
{
    private const string HighestUnlockedLevelKey = "restore_rush_highest_unlocked_level";
    private const string CoinsKey = "restore_rush_coins";
    private const string BrushPowerLevelKey = "restore_rush_brush_power_level";
    private const string LevelStarsKeyPrefix = "restore_rush_level_stars_";
    private const int BrushUpgradeStepPercent = 5;
    private const int MaxBrushPowerLevel = 8;

    public static int CurrentLevelIndex;
    public static int HighestUnlockedLevelIndex;
    public static int Coins;
    public static int BrushPowerLevel;
    public static int LastLevelIndex;
    public static string LastLevelName;
    public static bool LastRunWon;
    public static int LastActions;
    public static float LastDurationSeconds;
    public static float LastCleanPercent;
    public static int LastRunStars;
    public static int LastRunCoinReward;
    public static int LastBestCombo;
    public static int LastComboBonusCoins;

    private static int[] starsByLevel = new int[0];

    public static void LoadProgress()
    {
        HighestUnlockedLevelIndex = ClampLevel(PlayerPrefs.GetInt(HighestUnlockedLevelKey, 0));
        Coins = Mathf.Max(0, PlayerPrefs.GetInt(CoinsKey, 0));
        BrushPowerLevel = Mathf.Clamp(PlayerPrefs.GetInt(BrushPowerLevelKey, 0), 0, MaxBrushPowerLevel);

        EnsureStarsCache();
        for (var i = 0; i < starsByLevel.Length; i++)
        {
            starsByLevel[i] = Mathf.Clamp(PlayerPrefs.GetInt(GetLevelStarsKey(i), 0), 0, 3);
        }

        CurrentLevelIndex = Mathf.Clamp(CurrentLevelIndex, 0, HighestUnlockedLevelIndex);
    }

    public static void SaveProgress()
    {
        PlayerPrefs.SetInt(HighestUnlockedLevelKey, ClampLevel(HighestUnlockedLevelIndex));
        PlayerPrefs.SetInt(CoinsKey, Mathf.Max(0, Coins));
        PlayerPrefs.SetInt(BrushPowerLevelKey, Mathf.Clamp(BrushPowerLevel, 0, MaxBrushPowerLevel));

        EnsureStarsCache();
        for (var i = 0; i < starsByLevel.Length; i++)
        {
            PlayerPrefs.SetInt(GetLevelStarsKey(i), Mathf.Clamp(starsByLevel[i], 0, 3));
        }

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

    public static int GetLevelStars(int levelIndex)
    {
        EnsureStarsCache();
        var safeLevel = ClampLevel(levelIndex);
        return starsByLevel[safeLevel];
    }

    public static int GetTotalStars()
    {
        EnsureStarsCache();
        var total = 0;
        for (var i = 0; i < starsByLevel.Length; i++)
        {
            total += starsByLevel[i];
        }

        return total;
    }

    public static void UpdateLevelStars(int levelIndex, int stars)
    {
        EnsureStarsCache();
        var safeLevel = ClampLevel(levelIndex);
        var safeStars = Mathf.Clamp(stars, 0, 3);
        if (safeStars <= starsByLevel[safeLevel])
        {
            return;
        }

        starsByLevel[safeLevel] = safeStars;
        SaveProgress();
    }

    public static void AddCoins(int amount)
    {
        if (amount <= 0)
        {
            return;
        }

        Coins += amount;
        SaveProgress();
    }

    public static float GetBrushPowerMultiplier()
    {
        return 1f + (BrushPowerLevel * (BrushUpgradeStepPercent / 100f));
    }

    public static int GetBrushPowerBonusPercent()
    {
        return BrushPowerLevel * BrushUpgradeStepPercent;
    }

    public static bool CanUpgradeBrush()
    {
        if (BrushPowerLevel >= MaxBrushPowerLevel)
        {
            return false;
        }

        return Coins >= GetBrushUpgradeCost();
    }

    public static int GetBrushUpgradeCost()
    {
        return 120 + (BrushPowerLevel * 80);
    }

    public static bool TryUpgradeBrush()
    {
        if (BrushPowerLevel >= MaxBrushPowerLevel)
        {
            return false;
        }

        var cost = GetBrushUpgradeCost();
        if (Coins < cost)
        {
            return false;
        }

        Coins -= cost;
        BrushPowerLevel++;
        SaveProgress();
        return true;
    }

    public static int GetMaxBrushPowerLevel()
    {
        return MaxBrushPowerLevel;
    }

    private static void EnsureStarsCache()
    {
        if (starsByLevel.Length == RestoreLevelCatalog.Count)
        {
            return;
        }

        var old = starsByLevel;
        starsByLevel = new int[RestoreLevelCatalog.Count];
        var copyCount = Mathf.Min(old.Length, starsByLevel.Length);
        for (var i = 0; i < copyCount; i++)
        {
            starsByLevel[i] = Mathf.Clamp(old[i], 0, 3);
        }
    }

    private static string GetLevelStarsKey(int levelIndex)
    {
        return $"{LevelStarsKeyPrefix}{levelIndex}";
    }

    private static int ClampLevel(int levelIndex)
    {
        return Mathf.Clamp(levelIndex, 0, Mathf.Max(0, RestoreLevelCatalog.Count - 1));
    }
}
