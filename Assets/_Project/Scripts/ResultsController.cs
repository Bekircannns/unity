using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public sealed class ResultsController : MonoBehaviour
{
    private Canvas canvas;
    private Text levelText;
    private Text statusText;
    private Text starsText;
    private Text coinText;
    private Text actionsText;
    private Text cleanedText;
    private Text durationText;
    private Button nextLevelButton;

    private void Start()
    {
        BuildUi();
        RefreshUi();
    }

    private void BuildUi()
    {
        if (canvas != null)
        {
            return;
        }

        RuntimeUiFactory.EnsureEventSystem();
        canvas = RuntimeUiFactory.CreateCanvas("ResultsCanvas");

        var background = RuntimeUiFactory.CreatePanel(
            canvas.transform,
            "Background",
            Vector2.zero,
            Vector2.one,
            Vector2.zero,
            Vector2.zero,
            new Color(0.05f, 0.07f, 0.11f, 1f),
            "results_background");

        background.offsetMin = Vector2.zero;
        background.offsetMax = Vector2.zero;

        var card = RuntimeUiFactory.CreatePanel(
            canvas.transform,
            "Card",
            new Vector2(0.5f, 0.5f),
            new Vector2(0.5f, 0.5f),
            new Vector2(760f, 820f),
            new Vector2(0f, 20f),
            new Color(0.1f, 0.14f, 0.2f, 0.92f),
            "results_card");

        var title = RuntimeUiFactory.CreateText(card, "Title", "Run Results", 66, TextAnchor.UpperCenter, new Color(0.93f, 0.95f, 1f, 1f));
        title.rectTransform.anchorMin = new Vector2(0f, 1f);
        title.rectTransform.anchorMax = new Vector2(1f, 1f);
        title.rectTransform.sizeDelta = new Vector2(0f, 110f);
        title.rectTransform.anchoredPosition = new Vector2(0f, -18f);

        levelText = RuntimeUiFactory.CreateText(card, "Level", string.Empty, 30, TextAnchor.MiddleCenter, new Color(0.78f, 0.86f, 0.97f, 1f));
        levelText.rectTransform.anchorMin = new Vector2(0.08f, 1f);
        levelText.rectTransform.anchorMax = new Vector2(0.92f, 1f);
        levelText.rectTransform.sizeDelta = new Vector2(0f, 42f);
        levelText.rectTransform.anchoredPosition = new Vector2(0f, -96f);

        statusText = RuntimeUiFactory.CreateText(card, "Status", string.Empty, 42, TextAnchor.MiddleCenter, Color.white);
        statusText.rectTransform.anchorMin = new Vector2(0.08f, 1f);
        statusText.rectTransform.anchorMax = new Vector2(0.92f, 1f);
        statusText.rectTransform.sizeDelta = new Vector2(0f, 56f);
        statusText.rectTransform.anchoredPosition = new Vector2(0f, -146f);

        starsText = RuntimeUiFactory.CreateText(card, "Stars", string.Empty, 34, TextAnchor.MiddleCenter, new Color(1f, 0.91f, 0.55f, 1f));
        starsText.rectTransform.anchorMin = new Vector2(0.08f, 1f);
        starsText.rectTransform.anchorMax = new Vector2(0.92f, 1f);
        starsText.rectTransform.sizeDelta = new Vector2(0f, 54f);
        starsText.rectTransform.anchoredPosition = new Vector2(0f, -204f);

        coinText = RuntimeUiFactory.CreateText(card, "Coins", string.Empty, 34, TextAnchor.MiddleCenter, new Color(0.87f, 0.94f, 1f, 1f));
        coinText.rectTransform.anchorMin = new Vector2(0.08f, 1f);
        coinText.rectTransform.anchorMax = new Vector2(0.92f, 1f);
        coinText.rectTransform.sizeDelta = new Vector2(0f, 54f);
        coinText.rectTransform.anchoredPosition = new Vector2(0f, -262f);

        actionsText = RuntimeUiFactory.CreateText(card, "Actions", string.Empty, 34, TextAnchor.MiddleCenter, new Color(0.84f, 0.9f, 1f, 1f));
        actionsText.rectTransform.anchorMin = new Vector2(0.08f, 1f);
        actionsText.rectTransform.anchorMax = new Vector2(0.92f, 1f);
        actionsText.rectTransform.sizeDelta = new Vector2(0f, 54f);
        actionsText.rectTransform.anchoredPosition = new Vector2(0f, -320f);

        cleanedText = RuntimeUiFactory.CreateText(card, "Cleaned", string.Empty, 34, TextAnchor.MiddleCenter, new Color(0.84f, 0.9f, 1f, 1f));
        cleanedText.rectTransform.anchorMin = new Vector2(0.08f, 1f);
        cleanedText.rectTransform.anchorMax = new Vector2(0.92f, 1f);
        cleanedText.rectTransform.sizeDelta = new Vector2(0f, 54f);
        cleanedText.rectTransform.anchoredPosition = new Vector2(0f, -376f);

        durationText = RuntimeUiFactory.CreateText(card, "Duration", string.Empty, 34, TextAnchor.MiddleCenter, new Color(0.84f, 0.9f, 1f, 1f));
        durationText.rectTransform.anchorMin = new Vector2(0.08f, 1f);
        durationText.rectTransform.anchorMax = new Vector2(0.92f, 1f);
        durationText.rectTransform.sizeDelta = new Vector2(0f, 54f);
        durationText.rectTransform.anchoredPosition = new Vector2(0f, -432f);

        var retryButton = RuntimeUiFactory.CreateButton(card, "RetryButton", "Retry Level", RetryLevel);
        var retryRect = retryButton.GetComponent<RectTransform>();
        retryRect.anchorMin = new Vector2(0.5f, 0f);
        retryRect.anchorMax = new Vector2(0.5f, 0f);
        retryRect.sizeDelta = new Vector2(520f, 86f);
        retryRect.anchoredPosition = new Vector2(0f, 206f);

        nextLevelButton = RuntimeUiFactory.CreateButton(card, "NextLevelButton", "Next Level", NextLevel);
        var nextRect = nextLevelButton.GetComponent<RectTransform>();
        nextRect.anchorMin = new Vector2(0.5f, 0f);
        nextRect.anchorMax = new Vector2(0.5f, 0f);
        nextRect.sizeDelta = new Vector2(520f, 86f);
        nextRect.anchoredPosition = new Vector2(0f, 112f);

        var menuButton = RuntimeUiFactory.CreateButton(card, "MenuButton", "Back To Menu", BackToMenu);
        var menuRect = menuButton.GetComponent<RectTransform>();
        menuRect.anchorMin = new Vector2(0.5f, 0f);
        menuRect.anchorMax = new Vector2(0.5f, 0f);
        menuRect.sizeDelta = new Vector2(520f, 80f);
        menuRect.anchoredPosition = new Vector2(0f, 26f);
    }

    private void RefreshUi()
    {
        levelText.text = GameRunState.LastLevelName;
        statusText.text = GameRunState.LastRunWon ? "Status: WIN" : "Status: FAIL";
        statusText.color = GameRunState.LastRunWon ? new Color(0.54f, 0.92f, 0.7f, 1f) : new Color(1f, 0.56f, 0.56f, 1f);
        starsText.text = $"Stars: {BuildStarsLine(GameRunState.LastRunStars)}";
        coinText.text = $"Coins: +{GameRunState.LastRunCoinReward}   (Total: {GameRunState.Coins})";
        actionsText.text = $"Actions: {GameRunState.LastActions}";
        cleanedText.text = $"Cleaned: {GameRunState.LastCleanPercent * 100f:0.0}%";
        durationText.text = $"Duration: {GameRunState.LastDurationSeconds:0.0}s";

        var hasNext = GameRunState.LastLevelIndex + 1 < RestoreLevelCatalog.Count;
        nextLevelButton.interactable = GameRunState.LastRunWon && hasNext;
    }

    private static void RetryLevel()
    {
        GameRunState.SetCurrentLevel(GameRunState.LastLevelIndex);
        SceneManager.LoadScene(SceneNames.Gameplay);
    }

    private static void NextLevel()
    {
        if (!GameRunState.LastRunWon)
        {
            return;
        }

        var nextLevelIndex = GameRunState.LastLevelIndex + 1;
        if (nextLevelIndex >= RestoreLevelCatalog.Count)
        {
            SceneManager.LoadScene(SceneNames.Menu);
            return;
        }

        GameRunState.UnlockLevel(nextLevelIndex);
        GameRunState.SetCurrentLevel(nextLevelIndex);
        SceneManager.LoadScene(SceneNames.Gameplay);
    }

    private static void BackToMenu()
    {
        SceneManager.LoadScene(SceneNames.Menu);
    }

    private static string BuildStarsLine(int stars)
    {
        var safeStars = Mathf.Clamp(stars, 0, 3);
        return new string('*', safeStars) + new string('-', 3 - safeStars);
    }
}
