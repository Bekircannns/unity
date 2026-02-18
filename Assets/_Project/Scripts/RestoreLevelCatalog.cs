public enum RestoreObjectStyle
{
    Jug,
    Plate,
    Vase,
    ToyDuck,
    RobotHead
}

public readonly struct RestoreLevelConfig
{
    public RestoreLevelConfig(string name, float durationSeconds, float targetCleanPercent, RestoreObjectStyle objectStyle)
    {
        Name = name;
        DurationSeconds = durationSeconds;
        TargetCleanPercent = targetCleanPercent;
        ObjectStyle = objectStyle;
    }

    public string Name { get; }
    public float DurationSeconds { get; }
    public float TargetCleanPercent { get; }
    public RestoreObjectStyle ObjectStyle { get; }
}

public static class RestoreLevelCatalog
{
    private static readonly RestoreLevelConfig[] Levels =
    {
        new RestoreLevelConfig("Level 1 - Old Jug", 40f, 0.74f, RestoreObjectStyle.Jug),
        new RestoreLevelConfig("Level 2 - Dinner Plate", 40f, 0.75f, RestoreObjectStyle.Plate),
        new RestoreLevelConfig("Level 3 - Vintage Vase", 38f, 0.78f, RestoreObjectStyle.Vase),
        new RestoreLevelConfig("Level 4 - Duck Toy", 36f, 0.8f, RestoreObjectStyle.ToyDuck),
        new RestoreLevelConfig("Level 5 - Robot Head", 35f, 0.82f, RestoreObjectStyle.RobotHead)
    };

    public static int Count => Levels.Length;

    public static RestoreLevelConfig GetByIndex(int levelIndex)
    {
        var safeIndex = levelIndex;
        if (safeIndex < 0)
        {
            safeIndex = 0;
        }
        else if (safeIndex >= Levels.Length)
        {
            safeIndex = Levels.Length - 1;
        }

        return Levels[safeIndex];
    }
}
