using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public sealed class MenuController : MonoBehaviour
{
    private Canvas canvas;

    private void Start()
    {
        BuildUi();
    }

    private void BuildUi()
    {
        if (canvas != null)
        {
            return;
        }

        RuntimeUiFactory.EnsureEventSystem();
        canvas = RuntimeUiFactory.CreateCanvas("MenuCanvas");

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
            new Vector2(760f, 620f),
            new Vector2(0f, 40f),
            new Color(0.1f, 0.14f, 0.2f, 0.92f));

        var title = RuntimeUiFactory.CreateText(card, "Title", "Restore Rush", 74, TextAnchor.UpperCenter, new Color(0.93f, 0.95f, 1f, 1f));
        title.rectTransform.anchorMin = new Vector2(0f, 1f);
        title.rectTransform.anchorMax = new Vector2(1f, 1f);
        title.rectTransform.sizeDelta = new Vector2(0f, 120f);
        title.rectTransform.anchoredPosition = new Vector2(0f, -26f);

        var subtitle = RuntimeUiFactory.CreateText(card, "Subtitle", "Satisfying restore puzzle prototype", 32, TextAnchor.UpperCenter, new Color(0.7f, 0.78f, 0.9f, 1f));
        subtitle.rectTransform.anchorMin = new Vector2(0f, 1f);
        subtitle.rectTransform.anchorMax = new Vector2(1f, 1f);
        subtitle.rectTransform.sizeDelta = new Vector2(-48f, 70f);
        subtitle.rectTransform.anchoredPosition = new Vector2(0f, -120f);

        var startButton = RuntimeUiFactory.CreateButton(card, "StartButton", "Start Game", StartGame);
        var startRect = startButton.GetComponent<RectTransform>();
        startRect.anchorMin = new Vector2(0.5f, 0f);
        startRect.anchorMax = new Vector2(0.5f, 0f);
        startRect.sizeDelta = new Vector2(520f, 94f);
        startRect.anchoredPosition = new Vector2(0f, 190f);

        var quitButton = RuntimeUiFactory.CreateButton(card, "QuitButton", "Quit", QuitGame);
        var quitRect = quitButton.GetComponent<RectTransform>();
        quitRect.anchorMin = new Vector2(0.5f, 0f);
        quitRect.anchorMax = new Vector2(0.5f, 0f);
        quitRect.sizeDelta = new Vector2(520f, 84f);
        quitRect.anchoredPosition = new Vector2(0f, 78f);
    }

    private static void StartGame()
    {
        SceneManager.LoadScene(SceneNames.Gameplay);
    }

    private static void QuitGame()
    {
        Application.Quit();
    }
}
