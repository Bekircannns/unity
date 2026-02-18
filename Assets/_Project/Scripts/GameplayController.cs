using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

public sealed class GameplayController : MonoBehaviour
{
    private enum ToolType
    {
        Brush,
        Spray,
        Scraper
    }

    [Header("Round")]
    [SerializeField] private float roundDurationSeconds = 35f;
    [SerializeField] private float winCleanPercent = 0.82f;

    [Header("Cleaning Grid")]
    [SerializeField] private int gridWidth = 120;
    [SerializeField] private int gridHeight = 120;

    [Header("Visuals")]
    [SerializeField] private Color dirtyColor = new Color(0.24f, 0.19f, 0.15f, 1f);
    [SerializeField] private Color cleanColor = new Color(0.87f, 0.91f, 0.95f, 1f);

    private ToolType selectedTool = ToolType.Brush;
    private Texture2D dirtTexture;
    private float[] dirtValues;
    private bool[] objectMask;
    private Color[] pixelCache;
    private float totalDirt;
    private float maxDirt;

    private int strokeCount;
    private float timeLeft;
    private float roundStartTime;
    private bool roundEnded;
    private bool pointerWasDown;
    private bool tutorialDismissed;

    private Canvas canvas;
    private Text timeText;
    private Text progressText;
    private Text strokesText;
    private Text toolHintText;
    private Text objectiveText;
    private RawImage cleanSurfaceImage;
    private RectTransform cleanSurfaceRect;
    private Image brushButtonImage;
    private Image sprayButtonImage;
    private Image scraperButtonImage;
    private RectTransform progressFillRect;
    private GameObject tutorialCard;

    private void Start()
    {
        gridWidth = Mathf.Max(32, gridWidth);
        gridHeight = Mathf.Max(32, gridHeight);
        winCleanPercent = Mathf.Clamp(winCleanPercent, 0.2f, 0.99f);

        dirtValues = new float[gridWidth * gridHeight];
        objectMask = new bool[dirtValues.Length];
        pixelCache = new Color[dirtValues.Length];

        BuildObjectMaskAndInitializeDirt();

        dirtTexture = new Texture2D(gridWidth, gridHeight, TextureFormat.RGBA32, false);
        dirtTexture.wrapMode = TextureWrapMode.Clamp;
        dirtTexture.filterMode = FilterMode.Bilinear;
        RebuildTexture();

        BuildUi();

        timeLeft = roundDurationSeconds;
        roundStartTime = Time.time;
        RefreshHud();
    }

    private void Update()
    {
        if (roundEnded)
        {
            return;
        }

        HandlePointerInput();

        if (GetCleanPercent() >= winCleanPercent)
        {
            EndRound(true);
            return;
        }

        timeLeft -= Time.deltaTime;
        if (timeLeft <= 0f)
        {
            timeLeft = 0f;
            EndRound(false);
            return;
        }

        RefreshHud();
    }

    private void OnDestroy()
    {
        if (dirtTexture != null)
        {
            Destroy(dirtTexture);
        }
    }

    private void BuildObjectMaskAndInitializeDirt()
    {
        maxDirt = 0f;

        for (var y = 0; y < gridHeight; y++)
        {
            for (var x = 0; x < gridWidth; x++)
            {
                var index = y * gridWidth + x;
                var nx = ((x + 0.5f) / gridWidth) * 2f - 1f;
                var ny = ((y + 0.5f) / gridHeight) * 2f - 1f;

                // Rounded "plate/object" silhouette so gameplay reads as object cleaning, not plain square.
                var ellipse = (nx * nx) / 0.78f + (ny * ny) / 0.92f <= 1f;
                var topCap = ny > 0.55f && Mathf.Abs(nx) < 0.22f;
                var inObject = ellipse || topCap;

                objectMask[index] = inObject;
                dirtValues[index] = inObject ? 1f : 0f;
                if (inObject)
                {
                    maxDirt += 1f;
                }
            }
        }

        totalDirt = maxDirt;
    }

    private void BuildUi()
    {
        if (canvas != null)
        {
            return;
        }

        RuntimeUiFactory.EnsureEventSystem();
        canvas = RuntimeUiFactory.CreateCanvas("GameplayCanvas");

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

        objectiveText = RuntimeUiFactory.CreateText(
            canvas.transform,
            "ObjectiveText",
            "Clean to 82% before time ends",
            38,
            TextAnchor.MiddleCenter,
            new Color(0.93f, 0.96f, 1f, 1f));
        objectiveText.rectTransform.anchorMin = new Vector2(0.5f, 1f);
        objectiveText.rectTransform.anchorMax = new Vector2(0.5f, 1f);
        objectiveText.rectTransform.sizeDelta = new Vector2(950f, 60f);
        objectiveText.rectTransform.anchoredPosition = new Vector2(0f, -44f);

        var hudPanel = RuntimeUiFactory.CreatePanel(
            canvas.transform,
            "HudPanel",
            new Vector2(0f, 1f),
            new Vector2(0f, 1f),
            new Vector2(430f, 420f),
            new Vector2(235f, -250f),
            new Color(0.1f, 0.14f, 0.2f, 0.94f));

        var titleText = RuntimeUiFactory.CreateText(hudPanel, "Title", "Restore Run", 40, TextAnchor.MiddleLeft, Color.white);
        titleText.rectTransform.anchorMin = new Vector2(0f, 1f);
        titleText.rectTransform.anchorMax = new Vector2(1f, 1f);
        titleText.rectTransform.sizeDelta = new Vector2(-36f, 54f);
        titleText.rectTransform.anchoredPosition = new Vector2(14f, -28f);

        timeText = RuntimeUiFactory.CreateText(hudPanel, "TimeText", string.Empty, 30, TextAnchor.MiddleLeft, new Color(0.88f, 0.92f, 1f, 1f));
        timeText.rectTransform.anchorMin = new Vector2(0f, 1f);
        timeText.rectTransform.anchorMax = new Vector2(1f, 1f);
        timeText.rectTransform.sizeDelta = new Vector2(-36f, 44f);
        timeText.rectTransform.anchoredPosition = new Vector2(14f, -78f);

        progressText = RuntimeUiFactory.CreateText(hudPanel, "ProgressText", string.Empty, 30, TextAnchor.MiddleLeft, new Color(0.88f, 0.92f, 1f, 1f));
        progressText.rectTransform.anchorMin = new Vector2(0f, 1f);
        progressText.rectTransform.anchorMax = new Vector2(1f, 1f);
        progressText.rectTransform.sizeDelta = new Vector2(-36f, 44f);
        progressText.rectTransform.anchoredPosition = new Vector2(14f, -124f);

        strokesText = RuntimeUiFactory.CreateText(hudPanel, "StrokesText", string.Empty, 30, TextAnchor.MiddleLeft, new Color(0.88f, 0.92f, 1f, 1f));
        strokesText.rectTransform.anchorMin = new Vector2(0f, 1f);
        strokesText.rectTransform.anchorMax = new Vector2(1f, 1f);
        strokesText.rectTransform.sizeDelta = new Vector2(-36f, 44f);
        strokesText.rectTransform.anchoredPosition = new Vector2(14f, -170f);

        var progressBarBg = RuntimeUiFactory.CreatePanel(
            hudPanel,
            "ProgressBarBg",
            new Vector2(0f, 1f),
            new Vector2(1f, 1f),
            new Vector2(-40f, 22f),
            new Vector2(0f, -214f),
            new Color(0.22f, 0.28f, 0.36f, 1f));

        var progressFill = RuntimeUiFactory.CreatePanel(
            progressBarBg,
            "ProgressFill",
            new Vector2(0f, 0f),
            new Vector2(0f, 1f),
            Vector2.zero,
            Vector2.zero,
            new Color(0.34f, 0.79f, 0.56f, 1f));
        progressFill.offsetMin = Vector2.zero;
        progressFill.offsetMax = Vector2.zero;
        progressFillRect = progressFill;

        var toolsLabel = RuntimeUiFactory.CreateText(hudPanel, "ToolsLabel", "Tools", 28, TextAnchor.MiddleLeft, new Color(0.76f, 0.83f, 0.95f, 1f));
        toolsLabel.rectTransform.anchorMin = new Vector2(0f, 1f);
        toolsLabel.rectTransform.anchorMax = new Vector2(1f, 1f);
        toolsLabel.rectTransform.sizeDelta = new Vector2(-36f, 40f);
        toolsLabel.rectTransform.anchoredPosition = new Vector2(14f, -258f);

        var brushButton = RuntimeUiFactory.CreateButton(hudPanel, "BrushButton", "Brush", () => SelectTool(ToolType.Brush));
        var brushRect = brushButton.GetComponent<RectTransform>();
        brushRect.anchorMin = new Vector2(0f, 1f);
        brushRect.anchorMax = new Vector2(0f, 1f);
        brushRect.sizeDelta = new Vector2(118f, 54f);
        brushRect.anchoredPosition = new Vector2(74f, -308f);
        brushButtonImage = brushButton.GetComponent<Image>();
        brushButton.GetComponentInChildren<Text>().fontSize = 22;

        var sprayButton = RuntimeUiFactory.CreateButton(hudPanel, "SprayButton", "Spray", () => SelectTool(ToolType.Spray));
        var sprayRect = sprayButton.GetComponent<RectTransform>();
        sprayRect.anchorMin = new Vector2(0f, 1f);
        sprayRect.anchorMax = new Vector2(0f, 1f);
        sprayRect.sizeDelta = new Vector2(118f, 54f);
        sprayRect.anchoredPosition = new Vector2(214f, -308f);
        sprayButtonImage = sprayButton.GetComponent<Image>();
        sprayButton.GetComponentInChildren<Text>().fontSize = 22;

        var scraperButton = RuntimeUiFactory.CreateButton(hudPanel, "ScraperButton", "Scraper", () => SelectTool(ToolType.Scraper));
        var scraperRect = scraperButton.GetComponent<RectTransform>();
        scraperRect.anchorMin = new Vector2(0f, 1f);
        scraperRect.anchorMax = new Vector2(0f, 1f);
        scraperRect.sizeDelta = new Vector2(118f, 54f);
        scraperRect.anchoredPosition = new Vector2(354f, -308f);
        scraperButtonImage = scraperButton.GetComponent<Image>();
        scraperButton.GetComponentInChildren<Text>().fontSize = 22;

        var surfaceRoot = new GameObject("CleanSurfaceFrame", typeof(RectTransform), typeof(Image));
        surfaceRoot.transform.SetParent(canvas.transform, false);
        var frameRect = surfaceRoot.GetComponent<RectTransform>();
        frameRect.anchorMin = new Vector2(0.5f, 0.5f);
        frameRect.anchorMax = new Vector2(0.5f, 0.5f);
        frameRect.sizeDelta = new Vector2(760f, 760f);
        frameRect.anchoredPosition = new Vector2(80f, 80f);

        var frameImage = surfaceRoot.GetComponent<Image>();
        frameImage.color = new Color(0.16f, 0.2f, 0.27f, 1f);

        var surfaceImageObject = new GameObject("CleanSurfaceImage", typeof(RectTransform), typeof(RawImage));
        surfaceImageObject.transform.SetParent(surfaceRoot.transform, false);
        cleanSurfaceRect = surfaceImageObject.GetComponent<RectTransform>();
        cleanSurfaceRect.anchorMin = Vector2.zero;
        cleanSurfaceRect.anchorMax = Vector2.one;
        cleanSurfaceRect.offsetMin = new Vector2(12f, 12f);
        cleanSurfaceRect.offsetMax = new Vector2(-12f, -12f);

        cleanSurfaceImage = surfaceImageObject.GetComponent<RawImage>();
        cleanSurfaceImage.texture = dirtTexture;
        cleanSurfaceImage.uvRect = new Rect(0f, 0f, 1f, 1f);
        cleanSurfaceImage.color = Color.white;
        cleanSurfaceImage.raycastTarget = false;

        toolHintText = RuntimeUiFactory.CreateText(canvas.transform, "Hint", string.Empty, 30, TextAnchor.MiddleCenter, new Color(0.8f, 0.87f, 1f, 1f));
        toolHintText.rectTransform.anchorMin = new Vector2(0.5f, 0f);
        toolHintText.rectTransform.anchorMax = new Vector2(0.5f, 0f);
        toolHintText.rectTransform.sizeDelta = new Vector2(900f, 44f);
        toolHintText.rectTransform.anchoredPosition = new Vector2(80f, 530f);

        tutorialCard = RuntimeUiFactory.CreatePanel(
            canvas.transform,
            "TutorialCard",
            new Vector2(0.5f, 0f),
            new Vector2(0.5f, 0f),
            new Vector2(760f, 180f),
            new Vector2(80f, 360f),
            new Color(0.06f, 0.12f, 0.19f, 0.95f)).gameObject;

        var tutorialText = RuntimeUiFactory.CreateText(tutorialCard.transform, "TutorialText", "Tap and drag on the object to remove dirt.", 34, TextAnchor.MiddleCenter, new Color(0.9f, 0.95f, 1f, 1f));
        tutorialText.rectTransform.anchorMin = Vector2.zero;
        tutorialText.rectTransform.anchorMax = Vector2.one;
        tutorialText.rectTransform.offsetMin = new Vector2(20f, 20f);
        tutorialText.rectTransform.offsetMax = new Vector2(-20f, -20f);

        RefreshToolVisuals();
    }

    private void SelectTool(ToolType tool)
    {
        selectedTool = tool;
        RefreshToolVisuals();
    }

    private void RefreshHud()
    {
        var cleanPercent = GetCleanPercent();
        timeText.text = $"Time: {timeLeft:0.0}s";
        progressText.text = $"Cleaned: {cleanPercent * 100f:0.0}% / {winCleanPercent * 100f:0.0}%";
        strokesText.text = $"Strokes: {strokeCount}";
        toolHintText.text = GetToolHint(selectedTool);

        if (progressFillRect != null)
        {
            progressFillRect.anchorMax = new Vector2(Mathf.Clamp01(cleanPercent), 1f);
        }
    }

    private void RefreshToolVisuals()
    {
        var selectedColor = new Color(0.2f, 0.44f, 0.76f, 1f);
        var normalColor = new Color(0.11f, 0.16f, 0.25f, 0.95f);

        brushButtonImage.color = selectedTool == ToolType.Brush ? selectedColor : normalColor;
        sprayButtonImage.color = selectedTool == ToolType.Spray ? selectedColor : normalColor;
        scraperButtonImage.color = selectedTool == ToolType.Scraper ? selectedColor : normalColor;
    }

    private string GetToolHint(ToolType tool)
    {
        switch (tool)
        {
            case ToolType.Spray:
                return "Spray: wide area, lower power";
            case ToolType.Scraper:
                return "Scraper: small area, high power";
            default:
                return "Brush: balanced area and power";
        }
    }

    private void HandlePointerInput()
    {
        if (!TryGetPointer(out var pointerScreen, out var pointerDown, out var pointerBegan))
        {
            pointerWasDown = false;
            return;
        }

        var pointerInside = RectTransformUtility.RectangleContainsScreenPoint(cleanSurfaceRect, pointerScreen, null);
        if (pointerBegan && pointerInside)
        {
            strokeCount++;
            if (!tutorialDismissed)
            {
                tutorialDismissed = true;
                if (tutorialCard != null)
                {
                    tutorialCard.SetActive(false);
                }
            }

            RefreshHud();
        }
        else if (pointerDown && !pointerWasDown && pointerInside)
        {
            strokeCount++;
            RefreshHud();
        }

        if (pointerDown && pointerInside)
        {
            ApplyCleaning(pointerScreen);
        }

        pointerWasDown = pointerDown;
    }

    private void ApplyCleaning(Vector2 pointerScreen)
    {
        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(cleanSurfaceRect, pointerScreen, null, out var localPoint))
        {
            return;
        }

        var rect = cleanSurfaceRect.rect;
        var localX = Mathf.InverseLerp(rect.xMin, rect.xMax, localPoint.x);
        var localY = Mathf.InverseLerp(rect.yMin, rect.yMax, localPoint.y);
        var centerX = Mathf.RoundToInt(localX * (gridWidth - 1));
        var centerY = Mathf.RoundToInt(localY * (gridHeight - 1));

        GetToolSettings(out var radiusCells, out var cleanPerSecond);
        var cleanPower = cleanPerSecond * Time.deltaTime;
        if (cleanPower <= 0f)
        {
            return;
        }

        var radiusSquared = radiusCells * radiusCells;
        var minX = Mathf.Max(0, centerX - radiusCells);
        var maxX = Mathf.Min(gridWidth - 1, centerX + radiusCells);
        var minY = Mathf.Max(0, centerY - radiusCells);
        var maxY = Mathf.Min(gridHeight - 1, centerY + radiusCells);

        var changed = false;
        for (var y = minY; y <= maxY; y++)
        {
            for (var x = minX; x <= maxX; x++)
            {
                var dx = x - centerX;
                var dy = y - centerY;
                var distSquared = dx * dx + dy * dy;
                if (distSquared > radiusSquared)
                {
                    continue;
                }

                var index = y * gridWidth + x;
                if (!objectMask[index])
                {
                    continue;
                }

                var oldDirt = dirtValues[index];
                if (oldDirt <= 0f)
                {
                    continue;
                }

                var falloff = 1f - (Mathf.Sqrt(distSquared) / radiusCells);
                var newDirt = Mathf.Max(0f, oldDirt - (cleanPower * Mathf.Max(0.2f, falloff)));
                if (newDirt >= oldDirt)
                {
                    continue;
                }

                dirtValues[index] = newDirt;
                totalDirt -= oldDirt - newDirt;
                changed = true;
            }
        }

        if (changed)
        {
            totalDirt = Mathf.Clamp(totalDirt, 0f, maxDirt);
            RebuildTexture();
            RefreshHud();
        }
    }

    private void RebuildTexture()
    {
        for (var i = 0; i < dirtValues.Length; i++)
        {
            if (!objectMask[i])
            {
                pixelCache[i] = new Color(0f, 0f, 0f, 0f);
                continue;
            }

            pixelCache[i] = Color.Lerp(cleanColor, dirtyColor, dirtValues[i]);
        }

        dirtTexture.SetPixels(pixelCache);
        dirtTexture.Apply(false, false);

        if (cleanSurfaceImage != null)
        {
            cleanSurfaceImage.texture = dirtTexture;
        }
    }

    private float GetCleanPercent()
    {
        if (maxDirt <= 0f)
        {
            return 0f;
        }

        return 1f - (totalDirt / maxDirt);
    }

    private void GetToolSettings(out int radiusCells, out float cleanPerSecond)
    {
        switch (selectedTool)
        {
            case ToolType.Spray:
                radiusCells = 9;
                cleanPerSecond = 0.9f;
                break;
            case ToolType.Scraper:
                radiusCells = 4;
                cleanPerSecond = 2.3f;
                break;
            default:
                radiusCells = 6;
                cleanPerSecond = 1.4f;
                break;
        }
    }

    private bool TryGetPointer(out Vector2 pointerScreen, out bool pointerDown, out bool pointerBegan)
    {
#if ENABLE_INPUT_SYSTEM
        pointerScreen = default;
        pointerDown = false;
        pointerBegan = false;

        var touch = Touchscreen.current;
        if (touch != null && touch.primaryTouch.press.isPressed)
        {
            pointerScreen = touch.primaryTouch.position.ReadValue();
            pointerDown = touch.primaryTouch.press.isPressed;
            pointerBegan = touch.primaryTouch.press.wasPressedThisFrame;
            return true;
        }

        var mouse = Mouse.current;
        if (mouse != null)
        {
            pointerScreen = mouse.position.ReadValue();
            pointerDown = mouse.leftButton.isPressed;
            pointerBegan = mouse.leftButton.wasPressedThisFrame;
            return true;
        }

        return false;
#else
        if (Input.touchCount > 0)
        {
            var touch = Input.GetTouch(0);
            pointerScreen = touch.position;
            pointerDown = touch.phase == TouchPhase.Began ||
                          touch.phase == TouchPhase.Moved ||
                          touch.phase == TouchPhase.Stationary;
            pointerBegan = touch.phase == TouchPhase.Began;
            return true;
        }

        pointerScreen = Input.mousePosition;
        pointerDown = Input.GetMouseButton(0);
        pointerBegan = Input.GetMouseButtonDown(0);
        return true;
#endif
    }

    private void EndRound(bool won)
    {
        if (roundEnded)
        {
            return;
        }

        roundEnded = true;
        GameRunState.LastRunWon = won;
        GameRunState.LastActions = strokeCount;
        GameRunState.LastDurationSeconds = Time.time - roundStartTime;
        GameRunState.LastCleanPercent = GetCleanPercent();
        SceneManager.LoadScene(SceneNames.Results);
    }
}
