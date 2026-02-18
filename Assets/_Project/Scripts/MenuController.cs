using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public sealed class MenuController : MonoBehaviour
{
    private Canvas canvas;
    private Text progressText;
    private Text coinsText;
    private Text starsText;
    private Text brushPowerText;
    private Button continueButton;
    private Button upgradeBrushButton;
    private Text upgradeBrushLabel;
    private Button[] levelButtons;
    private Text[] levelButtonLabels;

    private void Start()
    {
        GameRunState.LoadProgress();
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
        canvas = RuntimeUiFactory.CreateCanvas("MenuCanvas");

        var background = RuntimeUiFactory.CreatePanel(
            canvas.transform,
            "Background",
            Vector2.zero,
            Vector2.one,
            Vector2.zero,
            Vector2.zero,
            new Color(0.05f, 0.07f, 0.11f, 1f),
            "menu_background");

        background.offsetMin = Vector2.zero;
        background.offsetMax = Vector2.zero;

        var card = RuntimeUiFactory.CreatePanel(
            canvas.transform,
            "Card",
            new Vector2(0.5f, 0.5f),
            new Vector2(0.5f, 0.5f),
            new Vector2(900f, 1500f),
            new Vector2(0f, 0f),
            new Color(0.1f, 0.14f, 0.2f, 0.92f),
            "menu_card");

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

        var logoSprite = Resources.Load<Sprite>("UI/logo_puzzle");
        if (logoSprite != null)
        {
            var logoImage = RuntimeUiFactory.CreateImage(card, "Logo", logoSprite, Color.white);
            var logoRect = logoImage.rectTransform;
            logoRect.anchorMin = new Vector2(1f, 1f);
            logoRect.anchorMax = new Vector2(1f, 1f);
            logoRect.sizeDelta = new Vector2(136f, 136f);
            logoRect.anchoredPosition = new Vector2(-90f, -86f);
        }

        progressText = RuntimeUiFactory.CreateText(card, "Progress", string.Empty, 28, TextAnchor.UpperCenter, new Color(0.79f, 0.86f, 0.97f, 1f));
        progressText.rectTransform.anchorMin = new Vector2(0f, 1f);
        progressText.rectTransform.anchorMax = new Vector2(1f, 1f);
        progressText.rectTransform.sizeDelta = new Vector2(-40f, 48f);
        progressText.rectTransform.anchoredPosition = new Vector2(0f, -186f);

        coinsText = RuntimeUiFactory.CreateText(card, "Coins", string.Empty, 30, TextAnchor.UpperCenter, new Color(0.95f, 0.95f, 0.8f, 1f));
        coinsText.rectTransform.anchorMin = new Vector2(0f, 1f);
        coinsText.rectTransform.anchorMax = new Vector2(1f, 1f);
        coinsText.rectTransform.sizeDelta = new Vector2(-40f, 46f);
        coinsText.rectTransform.anchoredPosition = new Vector2(0f, -234f);

        starsText = RuntimeUiFactory.CreateText(card, "Stars", string.Empty, 30, TextAnchor.UpperCenter, new Color(0.87f, 0.94f, 1f, 1f));
        starsText.rectTransform.anchorMin = new Vector2(0f, 1f);
        starsText.rectTransform.anchorMax = new Vector2(1f, 1f);
        starsText.rectTransform.sizeDelta = new Vector2(-40f, 46f);
        starsText.rectTransform.anchoredPosition = new Vector2(0f, -278f);

        brushPowerText = RuntimeUiFactory.CreateText(card, "BrushPower", string.Empty, 28, TextAnchor.UpperCenter, new Color(0.78f, 0.86f, 0.97f, 1f));
        brushPowerText.rectTransform.anchorMin = new Vector2(0f, 1f);
        brushPowerText.rectTransform.anchorMax = new Vector2(1f, 1f);
        brushPowerText.rectTransform.sizeDelta = new Vector2(-40f, 44f);
        brushPowerText.rectTransform.anchoredPosition = new Vector2(0f, -322f);

        upgradeBrushButton = RuntimeUiFactory.CreateButton(card, "UpgradeBrushButton", "Upgrade Brush", UpgradeBrush);
        var upgradeRect = upgradeBrushButton.GetComponent<RectTransform>();
        upgradeRect.anchorMin = new Vector2(0.5f, 1f);
        upgradeRect.anchorMax = new Vector2(0.5f, 1f);
        upgradeRect.sizeDelta = new Vector2(620f, 82f);
        upgradeRect.anchoredPosition = new Vector2(0f, -386f);
        upgradeBrushLabel = upgradeBrushButton.GetComponentInChildren<Text>();
        upgradeBrushLabel.fontSize = 28;

        var levelTitle = RuntimeUiFactory.CreateText(card, "LevelTitle", "Level Select", 40, TextAnchor.UpperCenter, Color.white);
        levelTitle.rectTransform.anchorMin = new Vector2(0f, 1f);
        levelTitle.rectTransform.anchorMax = new Vector2(1f, 1f);
        levelTitle.rectTransform.sizeDelta = new Vector2(-40f, 54f);
        levelTitle.rectTransform.anchoredPosition = new Vector2(0f, -486f);

        levelButtons = new Button[RestoreLevelCatalog.Count];
        levelButtonLabels = new Text[RestoreLevelCatalog.Count];

        for (var i = 0; i < RestoreLevelCatalog.Count; i++)
        {
            var row = i / 2;
            var col = i % 2;
            var x = col == 0 ? -182f : 182f;
            if (i == RestoreLevelCatalog.Count - 1 && RestoreLevelCatalog.Count % 2 == 1)
            {
                x = 0f;
            }

            var y = -574f - (row * 104f);
            CreateLevelButton(card, i, new Vector2(x, y));
        }

        var startButton = RuntimeUiFactory.CreateButton(card, "StartButton", "Start New Run", StartNewRun);
        var startRect = startButton.GetComponent<RectTransform>();
        startRect.anchorMin = new Vector2(0.5f, 0f);
        startRect.anchorMax = new Vector2(0.5f, 0f);
        startRect.sizeDelta = new Vector2(620f, 94f);
        startRect.anchoredPosition = new Vector2(0f, 230f);

        continueButton = RuntimeUiFactory.CreateButton(card, "ContinueButton", "Continue", ContinueRun);
        var continueRect = continueButton.GetComponent<RectTransform>();
        continueRect.anchorMin = new Vector2(0.5f, 0f);
        continueRect.anchorMax = new Vector2(0.5f, 0f);
        continueRect.sizeDelta = new Vector2(620f, 86f);
        continueRect.anchoredPosition = new Vector2(0f, 132f);

        var quitButton = RuntimeUiFactory.CreateButton(card, "QuitButton", "Quit", QuitGame);
        var quitRect = quitButton.GetComponent<RectTransform>();
        quitRect.anchorMin = new Vector2(0.5f, 0f);
        quitRect.anchorMax = new Vector2(0.5f, 0f);
        quitRect.sizeDelta = new Vector2(620f, 80f);
        quitRect.anchoredPosition = new Vector2(0f, 42f);
    }

    private void RefreshUi()
    {
        var unlockedCount = GameRunState.HighestUnlockedLevelIndex + 1;
        if (unlockedCount < 1)
        {
            unlockedCount = 1;
        }

        progressText.text = $"Unlocked: {unlockedCount}/{RestoreLevelCatalog.Count}";
        coinsText.text = $"Coins: {GameRunState.Coins}";
        starsText.text = $"Stars: {GameRunState.GetTotalStars()}/{RestoreLevelCatalog.Count * 3}";
        brushPowerText.text = $"Brush Power Bonus: +{GameRunState.GetBrushPowerBonusPercent()}%";

        if (GameRunState.BrushPowerLevel >= GameRunState.GetMaxBrushPowerLevel())
        {
            upgradeBrushLabel.text = "Upgrade Brush (MAX)";
            upgradeBrushButton.interactable = false;
        }
        else
        {
            var cost = GameRunState.GetBrushUpgradeCost();
            upgradeBrushLabel.text = $"Upgrade Brush ({cost} coins)";
            upgradeBrushButton.interactable = GameRunState.CanUpgradeBrush();
        }

        continueButton.interactable = GameRunState.HighestUnlockedLevelIndex > 0;

        for (var i = 0; i < levelButtons.Length; i++)
        {
            var unlocked = i <= GameRunState.HighestUnlockedLevelIndex;
            levelButtons[i].interactable = unlocked;

            if (unlocked)
            {
                levelButtonLabels[i].text = $"Level {i + 1} [{BuildStarsText(GameRunState.GetLevelStars(i))}]";
            }
            else
            {
                levelButtonLabels[i].text = $"Level {i + 1} [LOCKED]";
            }
        }
    }

    private static void StartNewRun()
    {
        GameRunState.SetCurrentLevel(0);
        SceneManager.LoadScene(SceneNames.Gameplay);
    }

    private static void ContinueRun()
    {
        GameRunState.SetCurrentLevel(GameRunState.HighestUnlockedLevelIndex);
        SceneManager.LoadScene(SceneNames.Gameplay);
    }

    private static void QuitGame()
    {
        Application.Quit();
    }

    private void UpgradeBrush()
    {
        if (!GameRunState.TryUpgradeBrush())
        {
            return;
        }

        RefreshUi();
    }

    private void CreateLevelButton(Transform parent, int levelIndex, Vector2 anchoredPos)
    {
        var button = RuntimeUiFactory.CreateButton(parent, $"LevelButton_{levelIndex}", $"Level {levelIndex + 1}", () => SelectLevel(levelIndex));
        var rect = button.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 1f);
        rect.anchorMax = new Vector2(0.5f, 1f);
        rect.sizeDelta = new Vector2(340f, 84f);
        rect.anchoredPosition = anchoredPos;

        var label = button.GetComponentInChildren<Text>();
        label.fontSize = 24;

        levelButtons[levelIndex] = button;
        levelButtonLabels[levelIndex] = label;
    }

    private static void SelectLevel(int levelIndex)
    {
        if (levelIndex > GameRunState.HighestUnlockedLevelIndex)
        {
            return;
        }

        GameRunState.SetCurrentLevel(levelIndex);
        SceneManager.LoadScene(SceneNames.Gameplay);
    }

    private static string BuildStarsText(int stars)
    {
        var safeStars = Mathf.Clamp(stars, 0, 3);
        return new string('*', safeStars) + new string('-', 3 - safeStars);
    }

}
