using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public sealed class ResultsController : MonoBehaviour
{
    private Canvas canvas;
    private Text statusText;
    private Text actionsText;
    private Text cleanedText;
    private Text durationText;

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
            new Color(0.05f, 0.07f, 0.11f, 1f));

        background.offsetMin = Vector2.zero;
        background.offsetMax = Vector2.zero;

        var card = RuntimeUiFactory.CreatePanel(
            canvas.transform,
            "Card",
            new Vector2(0.5f, 0.5f),
            new Vector2(0.5f, 0.5f),
            new Vector2(760f, 760f),
            new Vector2(0f, 20f),
            new Color(0.1f, 0.14f, 0.2f, 0.92f));

        var title = RuntimeUiFactory.CreateText(card, "Title", "Run Results", 66, TextAnchor.UpperCenter, new Color(0.93f, 0.95f, 1f, 1f));
        title.rectTransform.anchorMin = new Vector2(0f, 1f);
        title.rectTransform.anchorMax = new Vector2(1f, 1f);
        title.rectTransform.sizeDelta = new Vector2(0f, 110f);
        title.rectTransform.anchoredPosition = new Vector2(0f, -18f);

        statusText = RuntimeUiFactory.CreateText(card, "Status", string.Empty, 42, TextAnchor.MiddleCenter, Color.white);
        statusText.rectTransform.anchorMin = new Vector2(0.08f, 1f);
        statusText.rectTransform.anchorMax = new Vector2(0.92f, 1f);
        statusText.rectTransform.sizeDelta = new Vector2(0f, 56f);
        statusText.rectTransform.anchoredPosition = new Vector2(0f, -132f);

        actionsText = RuntimeUiFactory.CreateText(card, "Actions", string.Empty, 34, TextAnchor.MiddleCenter, new Color(0.84f, 0.9f, 1f, 1f));
        actionsText.rectTransform.anchorMin = new Vector2(0.08f, 1f);
        actionsText.rectTransform.anchorMax = new Vector2(0.92f, 1f);
        actionsText.rectTransform.sizeDelta = new Vector2(0f, 54f);
        actionsText.rectTransform.anchoredPosition = new Vector2(0f, -194f);

        cleanedText = RuntimeUiFactory.CreateText(card, "Cleaned", string.Empty, 34, TextAnchor.MiddleCenter, new Color(0.84f, 0.9f, 1f, 1f));
        cleanedText.rectTransform.anchorMin = new Vector2(0.08f, 1f);
        cleanedText.rectTransform.anchorMax = new Vector2(0.92f, 1f);
        cleanedText.rectTransform.sizeDelta = new Vector2(0f, 54f);
        cleanedText.rectTransform.anchoredPosition = new Vector2(0f, -252f);

        durationText = RuntimeUiFactory.CreateText(card, "Duration", string.Empty, 34, TextAnchor.MiddleCenter, new Color(0.84f, 0.9f, 1f, 1f));
        durationText.rectTransform.anchorMin = new Vector2(0.08f, 1f);
        durationText.rectTransform.anchorMax = new Vector2(0.92f, 1f);
        durationText.rectTransform.sizeDelta = new Vector2(0f, 54f);
        durationText.rectTransform.anchoredPosition = new Vector2(0f, -310f);

        var playAgainButton = RuntimeUiFactory.CreateButton(card, "PlayAgainButton", "Play Again", PlayAgain);
        var playAgainRect = playAgainButton.GetComponent<RectTransform>();
        playAgainRect.anchorMin = new Vector2(0.5f, 0f);
        playAgainRect.anchorMax = new Vector2(0.5f, 0f);
        playAgainRect.sizeDelta = new Vector2(520f, 92f);
        playAgainRect.anchoredPosition = new Vector2(0f, 176f);

        var menuButton = RuntimeUiFactory.CreateButton(card, "MenuButton", "Back To Menu", BackToMenu);
        var menuRect = menuButton.GetComponent<RectTransform>();
        menuRect.anchorMin = new Vector2(0.5f, 0f);
        menuRect.anchorMax = new Vector2(0.5f, 0f);
        menuRect.sizeDelta = new Vector2(520f, 86f);
        menuRect.anchoredPosition = new Vector2(0f, 70f);
    }

    private void RefreshUi()
    {
        statusText.text = GameRunState.LastRunWon ? "Status: WIN" : "Status: FAIL";
        statusText.color = GameRunState.LastRunWon ? new Color(0.54f, 0.92f, 0.7f, 1f) : new Color(1f, 0.56f, 0.56f, 1f);
        actionsText.text = $"Actions: {GameRunState.LastActions}";
        cleanedText.text = $"Cleaned: {GameRunState.LastCleanPercent * 100f:0.0}%";
        durationText.text = $"Duration: {GameRunState.LastDurationSeconds:0.0}s";
    }

    private static void PlayAgain()
    {
        SceneManager.LoadScene(SceneNames.Gameplay);
    }

    private static void BackToMenu()
    {
        SceneManager.LoadScene(SceneNames.Menu);
    }
}
