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

    private ToolType selectedTool = ToolType.Brush;
    private int currentLevelIndex;
    private RestoreLevelConfig currentLevelConfig;

    private Texture2D cleanTexture;
    private Texture2D dirtTexture;

    private float[] dirtValues;
    private bool[] objectMask;
    private Color[] cleanPixels;
    private Color[] dirtBasePixels;
    private Color[] dirtPixels;

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
    private RawImage cleanSurfaceImage;
    private RawImage dirtSurfaceImage;
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

        currentLevelIndex = Mathf.Clamp(GameRunState.CurrentLevelIndex, 0, GameRunState.HighestUnlockedLevelIndex);
        currentLevelIndex = Mathf.Clamp(currentLevelIndex, 0, RestoreLevelCatalog.Count - 1);
        GameRunState.SetCurrentLevel(currentLevelIndex);
        currentLevelConfig = RestoreLevelCatalog.GetByIndex(currentLevelIndex);
        roundDurationSeconds = currentLevelConfig.DurationSeconds;
        winCleanPercent = Mathf.Clamp(currentLevelConfig.TargetCleanPercent, 0.2f, 0.99f);

        InitializeObjectAndTextures(currentLevelConfig.ObjectStyle);
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
        if (cleanTexture != null)
        {
            Destroy(cleanTexture);
        }

        if (dirtTexture != null)
        {
            Destroy(dirtTexture);
        }
    }

    private void InitializeObjectAndTextures(RestoreObjectStyle objectStyle)
    {
        var count = gridWidth * gridHeight;
        dirtValues = new float[count];
        objectMask = new bool[count];
        cleanPixels = new Color[count];
        dirtBasePixels = new Color[count];
        dirtPixels = new Color[count];

        maxDirt = 0f;
        for (var y = 0; y < gridHeight; y++)
        {
            for (var x = 0; x < gridWidth; x++)
            {
                var index = y * gridWidth + x;
                var nx = ((x + 0.5f) / gridWidth) * 2f - 1f;
                var ny = ((y + 0.5f) / gridHeight) * 2f - 1f;

                var inObject = IsInsideObjectShape(nx, ny, objectStyle);
                objectMask[index] = inObject;

                if (inObject)
                {
                    dirtValues[index] = 1f;
                    maxDirt += 1f;
                    cleanPixels[index] = BuildCleanPixel(nx, ny, objectStyle);
                    dirtBasePixels[index] = BuildDirtBasePixel(x, y, objectStyle);
                }
                else
                {
                    dirtValues[index] = 0f;
                    cleanPixels[index] = new Color(0f, 0f, 0f, 0f);
                    dirtBasePixels[index] = new Color(0f, 0f, 0f, 0f);
                }
            }
        }

        totalDirt = maxDirt;

        cleanTexture = new Texture2D(gridWidth, gridHeight, TextureFormat.RGBA32, false);
        cleanTexture.wrapMode = TextureWrapMode.Clamp;
        cleanTexture.filterMode = FilterMode.Bilinear;
        cleanTexture.SetPixels(cleanPixels);
        cleanTexture.Apply(false, false);

        dirtTexture = new Texture2D(gridWidth, gridHeight, TextureFormat.RGBA32, false);
        dirtTexture.wrapMode = TextureWrapMode.Clamp;
        dirtTexture.filterMode = FilterMode.Bilinear;
        RebuildDirtTexture();
    }

    private static bool IsInsideObjectShape(float nx, float ny, RestoreObjectStyle objectStyle)
    {
        switch (objectStyle)
        {
            case RestoreObjectStyle.Plate:
            {
                var outer = (nx * nx) / 0.88f + (ny * ny) / 0.88f <= 1f;
                var innerHole = (nx * nx) / 0.24f + (ny * ny) / 0.24f <= 1f;
                var rim = outer && !innerHole;
                var center = (nx * nx) / 0.34f + (ny * ny) / 0.34f <= 1f;
                return rim || center;
            }
            case RestoreObjectStyle.Vase:
            {
                var top = Mathf.Abs(nx) < Mathf.Lerp(0.15f, 0.28f, Mathf.InverseLerp(0.2f, 0.95f, ny));
                var body = (nx * nx) / Mathf.Lerp(0.2f, 0.75f, Mathf.InverseLerp(-1f, 0.2f, ny)) + ((ny + 0.12f) * (ny + 0.12f)) / 0.9f <= 1f;
                return (ny > 0.2f && top) || body;
            }
            case RestoreObjectStyle.ToyDuck:
            {
                var body = ((nx + 0.08f) * (nx + 0.08f)) / 0.58f + ((ny + 0.06f) * (ny + 0.06f)) / 0.52f <= 1f;
                var head = ((nx - 0.36f) * (nx - 0.36f)) / 0.11f + ((ny + 0.32f) * (ny + 0.32f)) / 0.12f <= 1f;
                var beak = nx > 0.48f && nx < 0.72f && ny > 0.16f && ny < 0.35f;
                return body || head || beak;
            }
            case RestoreObjectStyle.RobotHead:
            {
                var head = Mathf.Abs(nx) < 0.62f && Mathf.Abs(ny) < 0.62f;
                var antenna = Mathf.Abs(nx) < 0.07f && ny > 0.62f && ny < 0.92f;
                var earLeft = nx < -0.62f && nx > -0.82f && Mathf.Abs(ny) < 0.18f;
                var earRight = nx > 0.62f && nx < 0.82f && Mathf.Abs(ny) < 0.18f;
                return head || antenna || earLeft || earRight;
            }
            default:
            {
                var body = (nx * nx) / 0.50f + ((ny + 0.05f) * (ny + 0.05f)) / 0.78f <= 1f;
                var neck = Mathf.Abs(nx) < 0.24f && ny > 0.28f && ny < 0.86f;
                var lip = Mathf.Abs(nx) < 0.34f && ny > 0.82f && ny < 0.96f;
                var handleEllipse = ((nx - 0.66f) * (nx - 0.66f)) / 0.08f + ((ny - 0.1f) * (ny - 0.1f)) / 0.18f <= 1f;
                var handleHole = ((nx - 0.66f) * (nx - 0.66f)) / 0.035f + ((ny - 0.1f) * (ny - 0.1f)) / 0.08f <= 1f;
                var handle = handleEllipse && !handleHole;
                return body || neck || lip || handle;
            }
        }
    }

    private static Color BuildCleanPixel(float nx, float ny, RestoreObjectStyle objectStyle)
    {
        Color topColor;
        Color bottomColor;
        switch (objectStyle)
        {
            case RestoreObjectStyle.Plate:
                topColor = new Color(0.95f, 0.94f, 0.89f, 1f);
                bottomColor = new Color(0.82f, 0.79f, 0.7f, 1f);
                break;
            case RestoreObjectStyle.Vase:
                topColor = new Color(0.85f, 0.98f, 0.87f, 1f);
                bottomColor = new Color(0.52f, 0.76f, 0.6f, 1f);
                break;
            case RestoreObjectStyle.ToyDuck:
                topColor = new Color(1f, 0.95f, 0.52f, 1f);
                bottomColor = new Color(0.95f, 0.73f, 0.18f, 1f);
                break;
            case RestoreObjectStyle.RobotHead:
                topColor = new Color(0.87f, 0.9f, 0.94f, 1f);
                bottomColor = new Color(0.55f, 0.6f, 0.68f, 1f);
                break;
            default:
                topColor = new Color(0.86f, 0.94f, 1f, 1f);
                bottomColor = new Color(0.56f, 0.72f, 0.9f, 1f);
                break;
        }

        var vertical = Mathf.InverseLerp(-1f, 1f, ny);
        var color = Color.Lerp(bottomColor, topColor, vertical);

        var radial = Mathf.Clamp01(1f - Mathf.Sqrt(Mathf.Max(0f, (nx * nx) / 0.9f + (ny * ny) / 1.05f)));
        color *= Mathf.Lerp(0.68f, 1.14f, radial);

        var highlight = Mathf.Exp(-((nx + 0.22f) * (nx + 0.22f) / 0.05f + (ny - 0.18f) * (ny - 0.18f) / 0.09f));
        color = Color.Lerp(color, new Color(0.98f, 0.995f, 1f, 1f), highlight * 0.36f);
        color.a = 1f;
        return color;
    }

    private static Color BuildDirtBasePixel(int x, int y, RestoreObjectStyle objectStyle)
    {
        var noiseA = Mathf.PerlinNoise((x + 13f) * 0.11f, (y + 47f) * 0.11f);
        var noiseB = Mathf.PerlinNoise((x + 5f) * 0.27f, (y + 17f) * 0.27f);

        Color dirtDark;
        Color dirtLight;
        switch (objectStyle)
        {
            case RestoreObjectStyle.Plate:
                dirtDark = new Color(0.18f, 0.14f, 0.1f, 1f);
                dirtLight = new Color(0.32f, 0.25f, 0.16f, 1f);
                break;
            case RestoreObjectStyle.Vase:
                dirtDark = new Color(0.1f, 0.14f, 0.09f, 1f);
                dirtLight = new Color(0.2f, 0.29f, 0.15f, 1f);
                break;
            case RestoreObjectStyle.ToyDuck:
                dirtDark = new Color(0.23f, 0.15f, 0.09f, 1f);
                dirtLight = new Color(0.39f, 0.25f, 0.12f, 1f);
                break;
            case RestoreObjectStyle.RobotHead:
                dirtDark = new Color(0.09f, 0.1f, 0.12f, 1f);
                dirtLight = new Color(0.2f, 0.22f, 0.26f, 1f);
                break;
            default:
                dirtDark = new Color(0.12f, 0.08f, 0.06f, 1f);
                dirtLight = new Color(0.24f, 0.16f, 0.11f, 1f);
                break;
        }

        var tone = Color.Lerp(dirtDark, dirtLight, noiseA);
        var alpha = Mathf.Lerp(0.76f, 1f, noiseB);
        tone.a = alpha;
        return tone;
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

        var objectiveText = RuntimeUiFactory.CreateText(
            canvas.transform,
            "ObjectiveText",
            $"{currentLevelConfig.Name} - clean at least {winCleanPercent * 100f:0}%",
            38,
            TextAnchor.MiddleCenter,
            new Color(0.93f, 0.96f, 1f, 1f));
        objectiveText.rectTransform.anchorMin = new Vector2(0.5f, 1f);
        objectiveText.rectTransform.anchorMax = new Vector2(0.5f, 1f);
        objectiveText.rectTransform.sizeDelta = new Vector2(980f, 60f);
        objectiveText.rectTransform.anchoredPosition = new Vector2(0f, -44f);

        var hudPanel = RuntimeUiFactory.CreatePanel(
            canvas.transform,
            "HudPanel",
            new Vector2(0f, 1f),
            new Vector2(0f, 1f),
            new Vector2(430f, 420f),
            new Vector2(235f, -250f),
            new Color(0.1f, 0.14f, 0.2f, 0.94f));

        var titleText = RuntimeUiFactory.CreateText(hudPanel, "Title", $"Level {currentLevelIndex + 1}", 40, TextAnchor.MiddleLeft, Color.white);
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

        var cleanLayerObject = new GameObject("CleanLayer", typeof(RectTransform), typeof(RawImage));
        cleanLayerObject.transform.SetParent(surfaceRoot.transform, false);
        var cleanLayerRect = cleanLayerObject.GetComponent<RectTransform>();
        cleanLayerRect.anchorMin = Vector2.zero;
        cleanLayerRect.anchorMax = Vector2.one;
        cleanLayerRect.offsetMin = new Vector2(12f, 12f);
        cleanLayerRect.offsetMax = new Vector2(-12f, -12f);
        cleanSurfaceImage = cleanLayerObject.GetComponent<RawImage>();
        cleanSurfaceImage.texture = cleanTexture;
        cleanSurfaceImage.raycastTarget = false;

        var dirtLayerObject = new GameObject("DirtLayer", typeof(RectTransform), typeof(RawImage));
        dirtLayerObject.transform.SetParent(surfaceRoot.transform, false);
        cleanSurfaceRect = dirtLayerObject.GetComponent<RectTransform>();
        cleanSurfaceRect.anchorMin = Vector2.zero;
        cleanSurfaceRect.anchorMax = Vector2.one;
        cleanSurfaceRect.offsetMin = new Vector2(12f, 12f);
        cleanSurfaceRect.offsetMax = new Vector2(-12f, -12f);
        dirtSurfaceImage = dirtLayerObject.GetComponent<RawImage>();
        dirtSurfaceImage.texture = dirtTexture;
        dirtSurfaceImage.raycastTarget = false;

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

        var tutorialText = RuntimeUiFactory.CreateText(tutorialCard.transform, "TutorialText", "Tap and drag on the dirty object to clean it.", 34, TextAnchor.MiddleCenter, new Color(0.9f, 0.95f, 1f, 1f));
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
            RebuildDirtTexture();
            RefreshHud();
        }
    }

    private void RebuildDirtTexture()
    {
        for (var i = 0; i < dirtPixels.Length; i++)
        {
            if (!objectMask[i])
            {
                dirtPixels[i] = new Color(0f, 0f, 0f, 0f);
                continue;
            }

            var baseColor = dirtBasePixels[i];
            dirtPixels[i] = new Color(baseColor.r, baseColor.g, baseColor.b, baseColor.a * dirtValues[i]);
        }

        dirtTexture.SetPixels(dirtPixels);
        dirtTexture.Apply(false, false);

        if (dirtSurfaceImage != null)
        {
            dirtSurfaceImage.texture = dirtTexture;
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
                radiusCells = 10;
                cleanPerSecond = 2.4f;
                break;
            case ToolType.Scraper:
                radiusCells = 5;
                cleanPerSecond = 4.3f;
                break;
            default:
                radiusCells = 7;
                cleanPerSecond = 3.2f;
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
        GameRunState.LastLevelIndex = currentLevelIndex;
        GameRunState.LastLevelName = currentLevelConfig.Name;
        GameRunState.LastRunWon = won;
        GameRunState.LastActions = strokeCount;
        GameRunState.LastDurationSeconds = Time.time - roundStartTime;
        GameRunState.LastCleanPercent = GetCleanPercent();

        if (won)
        {
            GameRunState.UnlockLevel(currentLevelIndex + 1);
        }

        SceneManager.LoadScene(SceneNames.Results);
    }
}
