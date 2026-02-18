using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

public sealed class GameplayController : MonoBehaviour
{
    private const int PercentPermilleScale = 1000;
    private const float WrongToolTimePenaltySeconds = 0.8f;

    private enum ToolType
    {
        Brush,
        Spray,
        Scraper
    }

    private enum DirtType : byte
    {
        Dust = 0,
        Rust = 1,
        Paint = 2
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
    private DirtType[] dirtTypes;
    private Color[] cleanPixels;
    private Color[] dirtBasePixels;
    private Color[] dirtPixels;

    private float totalDirt;
    private float maxDirt;

    private int strokeCount;
    private int comboStreak;
    private int bestCombo;
    private float timeLeft;
    private float roundStartTime;
    private bool roundEnded;
    private bool pointerWasDown;
    private bool tutorialDismissed;
    private float wrongToolHintTimer;

    private Canvas canvas;
    private Text timeText;
    private Text progressText;
    private Text strokesText;
    private Text comboText;
    private Text toolHintText;
    private Text dirtTypeText;
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

        if (wrongToolHintTimer > 0f)
        {
            wrongToolHintTimer -= Time.deltaTime;
        }

        HandlePointerInput();

        if (GetCleanPermille() >= GetTargetPermille())
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
        dirtTypes = new DirtType[count];
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
                    var dirtType = SelectDirtType(x, y);
                    dirtTypes[index] = dirtType;
                    dirtValues[index] = 1f;
                    maxDirt += 1f;
                    cleanPixels[index] = BuildCleanPixel(nx, ny, objectStyle);
                    dirtBasePixels[index] = BuildDirtBasePixel(x, y, objectStyle, dirtType);
                }
                else
                {
                    dirtTypes[index] = DirtType.Dust;
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

    private static Color BuildDirtBasePixel(int x, int y, RestoreObjectStyle objectStyle, DirtType dirtType)
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
        tone = ApplyDirtTypeTint(tone, dirtType, noiseB);
        var alpha = Mathf.Lerp(0.76f, 1f, noiseB);
        tone.a = alpha;
        return tone;
    }

    private DirtType SelectDirtType(int x, int y)
    {
        var noise = Mathf.PerlinNoise((x + (currentLevelIndex * 31)) * 0.09f, (y + (currentLevelIndex * 19)) * 0.09f);
        var rustThreshold = Mathf.Lerp(0.36f, 0.30f, currentLevelIndex / 4f);
        var paintThreshold = Mathf.Lerp(0.72f, 0.64f, currentLevelIndex / 4f);

        if (noise < rustThreshold)
        {
            return DirtType.Dust;
        }

        if (noise < paintThreshold)
        {
            return DirtType.Rust;
        }

        return DirtType.Paint;
    }

    private static Color ApplyDirtTypeTint(Color baseColor, DirtType dirtType, float noise)
    {
        Color tint;
        switch (dirtType)
        {
            case DirtType.Rust:
                tint = new Color(0.76f, 0.34f, 0.18f, 1f);
                break;
            case DirtType.Paint:
                tint = new Color(0.35f, 0.5f, 0.82f, 1f);
                break;
            default:
                tint = new Color(0.62f, 0.56f, 0.46f, 1f);
                break;
        }

        return Color.Lerp(baseColor, tint, Mathf.Lerp(0.58f, 0.85f, noise));
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
            new Color(0.05f, 0.07f, 0.11f, 1f),
            "gameplay_background");
        background.offsetMin = Vector2.zero;
        background.offsetMax = Vector2.zero;

        var objectiveText = RuntimeUiFactory.CreateText(
            canvas.transform,
            "ObjectiveText",
            $"{currentLevelConfig.Name} - clean at least {PermilleToPercentValue(GetTargetPermille()):0.0}%",
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
            new Color(0.1f, 0.14f, 0.2f, 0.94f),
            "gameplay_hud");

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

        comboText = RuntimeUiFactory.CreateText(hudPanel, "ComboText", string.Empty, 28, TextAnchor.MiddleLeft, new Color(1f, 0.88f, 0.45f, 1f));
        comboText.rectTransform.anchorMin = new Vector2(0f, 1f);
        comboText.rectTransform.anchorMax = new Vector2(1f, 1f);
        comboText.rectTransform.sizeDelta = new Vector2(-36f, 40f);
        comboText.rectTransform.anchoredPosition = new Vector2(14f, -212f);

        var progressBarBg = RuntimeUiFactory.CreatePanel(
            hudPanel,
            "ProgressBarBg",
            new Vector2(0f, 1f),
            new Vector2(1f, 1f),
            new Vector2(-40f, 22f),
            new Vector2(0f, -252f),
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

        var ruleText = RuntimeUiFactory.CreateText(canvas.transform, "Rules", "Dust=Brush | Rust=Scraper | Paint=Spray", 26, TextAnchor.MiddleCenter, new Color(0.72f, 0.82f, 0.95f, 1f));
        ruleText.rectTransform.anchorMin = new Vector2(0.5f, 0f);
        ruleText.rectTransform.anchorMax = new Vector2(0.5f, 0f);
        ruleText.rectTransform.sizeDelta = new Vector2(980f, 40f);
        ruleText.rectTransform.anchoredPosition = new Vector2(80f, 490f);

        dirtTypeText = RuntimeUiFactory.CreateText(canvas.transform, "DirtTypeHint", "Under Cursor: -", 24, TextAnchor.MiddleCenter, new Color(0.92f, 0.96f, 1f, 1f));
        dirtTypeText.rectTransform.anchorMin = new Vector2(0.5f, 0f);
        dirtTypeText.rectTransform.anchorMax = new Vector2(0.5f, 0f);
        dirtTypeText.rectTransform.sizeDelta = new Vector2(980f, 36f);
        dirtTypeText.rectTransform.anchoredPosition = new Vector2(80f, 454f);

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
        var cleanPermille = GetCleanPermille();
        var targetPermille = GetTargetPermille();
        timeText.text = $"Time: {timeLeft:0.0}s";
        progressText.text = $"Cleaned: {PermilleToPercentValue(cleanPermille):0.0}% / {PermilleToPercentValue(targetPermille):0.0}%";
        strokesText.text = $"Strokes: {strokeCount}";
        comboText.text = $"Combo: x{Mathf.Max(0, comboStreak)}";
        toolHintText.text = GetToolHint(selectedTool);

        if (progressFillRect != null)
        {
            progressFillRect.anchorMax = new Vector2(Mathf.Clamp01(cleanPermille / (float)PercentPermilleScale), 1f);
        }
    }

    private void RefreshToolVisuals()
    {
        var selectedColor = new Color(0.18f, 0.58f, 0.86f, 1f);
        var normalColor = new Color(0.11f, 0.16f, 0.25f, 0.95f);

        brushButtonImage.color = selectedTool == ToolType.Brush ? selectedColor : normalColor;
        sprayButtonImage.color = selectedTool == ToolType.Spray ? selectedColor : normalColor;
        scraperButtonImage.color = selectedTool == ToolType.Scraper ? selectedColor : normalColor;
    }

    private string GetToolHint(ToolType tool)
    {
        if (wrongToolHintTimer > 0f)
        {
            return "Wrong tool used: -0.8s";
        }

        switch (tool)
        {
            case ToolType.Spray:
                return "Spray: strongest on PAINT";
            case ToolType.Scraper:
                return "Scraper: strongest on RUST";
            default:
                return "Brush: strongest on DUST";
        }
    }

    private void HandlePointerInput()
    {
        if (!TryGetPointer(out var pointerScreen, out var pointerDown, out var pointerBegan))
        {
            pointerWasDown = false;
            if (dirtTypeText != null)
            {
                dirtTypeText.text = "Under Cursor: -";
            }
            return;
        }

        var pointerInside = RectTransformUtility.RectangleContainsScreenPoint(cleanSurfaceRect, pointerScreen, null);
        if (pointerBegan && pointerInside)
        {
            strokeCount++;
            ProcessStrokeStart(pointerScreen);
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
            ProcessStrokeStart(pointerScreen);
            RefreshHud();
        }

        UpdateDirtTypeHint(pointerInside, pointerScreen);

        if (pointerDown && pointerInside)
        {
            ApplyCleaning(pointerScreen);
        }

        pointerWasDown = pointerDown;
    }

    private void ApplyCleaning(Vector2 pointerScreen)
    {
        if (!TryGetGridCoordinates(pointerScreen, out var centerX, out var centerY))
        {
            return;
        }

        GetToolSettings(out var radiusCells, out var cleanPerSecond);
        var comboBoost = 1f + (Mathf.Clamp(comboStreak - 1, 0, 10) * 0.05f);
        var cleanPower = cleanPerSecond * comboBoost * Time.deltaTime;
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

                var effectiveness = GetToolEffectiveness(selectedTool, dirtTypes[index]);
                var falloff = 1f - (Mathf.Sqrt(distSquared) / radiusCells);
                var newDirt = Mathf.Max(0f, oldDirt - (cleanPower * effectiveness * Mathf.Max(0.2f, falloff)));
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

    private void ProcessStrokeStart(Vector2 pointerScreen)
    {
        if (!TryGetGridCoordinates(pointerScreen, out var centerX, out var centerY))
        {
            return;
        }

        var index = centerY * gridWidth + centerX;
        if (index < 0 || index >= dirtValues.Length || !objectMask[index])
        {
            return;
        }

        if (dirtValues[index] <= 0.03f)
        {
            return;
        }

        if (GetToolEffectiveness(selectedTool, dirtTypes[index]) >= 1f)
        {
            comboStreak = Mathf.Min(comboStreak + 1, 20);
            bestCombo = Mathf.Max(bestCombo, comboStreak);
            return;
        }

        comboStreak = 0;
        timeLeft = Mathf.Max(0f, timeLeft - WrongToolTimePenaltySeconds);
        wrongToolHintTimer = 1.15f;
    }

    private void UpdateDirtTypeHint(bool pointerInside, Vector2 pointerScreen)
    {
        if (dirtTypeText == null)
        {
            return;
        }

        if (!pointerInside || !TryGetGridCoordinates(pointerScreen, out var centerX, out var centerY))
        {
            dirtTypeText.text = "Under Cursor: -";
            return;
        }

        var index = centerY * gridWidth + centerX;
        if (index < 0 || index >= dirtTypes.Length || !objectMask[index] || dirtValues[index] <= 0.03f)
        {
            dirtTypeText.text = "Under Cursor: clean area";
            return;
        }

        var dirtType = dirtTypes[index];
        var bestTool = GetBestTool(dirtType);
        var multiplier = GetToolEffectiveness(selectedTool, dirtType);
        var state = selectedTool == bestTool ? "correct" : "wrong";
        dirtTypeText.text = $"Under Cursor: {GetDirtTypeLabel(dirtType)} | Best: {bestTool} | Current: {selectedTool} ({state} x{multiplier:0.00})";
    }

    private bool TryGetGridCoordinates(Vector2 pointerScreen, out int centerX, out int centerY)
    {
        centerX = 0;
        centerY = 0;
        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(cleanSurfaceRect, pointerScreen, null, out var localPoint))
        {
            return false;
        }

        var rect = cleanSurfaceRect.rect;
        var localX = Mathf.InverseLerp(rect.xMin, rect.xMax, localPoint.x);
        var localY = Mathf.InverseLerp(rect.yMin, rect.yMax, localPoint.y);
        centerX = Mathf.Clamp(Mathf.RoundToInt(localX * (gridWidth - 1)), 0, gridWidth - 1);
        centerY = Mathf.Clamp(Mathf.RoundToInt(localY * (gridHeight - 1)), 0, gridHeight - 1);
        return true;
    }

    private static float GetToolEffectiveness(ToolType tool, DirtType dirtType)
    {
        if (tool == ToolType.Brush && dirtType == DirtType.Dust)
        {
            return 1.8f;
        }

        if (tool == ToolType.Scraper && dirtType == DirtType.Rust)
        {
            return 1.9f;
        }

        if (tool == ToolType.Spray && dirtType == DirtType.Paint)
        {
            return 1.7f;
        }

        if (tool == ToolType.Brush && dirtType == DirtType.Paint)
        {
            return 0.55f;
        }

        if (tool == ToolType.Scraper && dirtType == DirtType.Dust)
        {
            return 0.55f;
        }

        if (tool == ToolType.Spray && dirtType == DirtType.Rust)
        {
            return 0.55f;
        }

        return 0.22f;
    }

    private static ToolType GetBestTool(DirtType dirtType)
    {
        switch (dirtType)
        {
            case DirtType.Rust:
                return ToolType.Scraper;
            case DirtType.Paint:
                return ToolType.Spray;
            default:
                return ToolType.Brush;
        }
    }

    private static string GetDirtTypeLabel(DirtType dirtType)
    {
        switch (dirtType)
        {
            case DirtType.Rust:
                return "Rust";
            case DirtType.Paint:
                return "Paint";
            default:
                return "Dust";
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

    private int GetCleanPermille()
    {
        return Mathf.Clamp(Mathf.RoundToInt(GetCleanPercent() * PercentPermilleScale), 0, PercentPermilleScale);
    }

    private int GetTargetPermille()
    {
        return Mathf.Clamp(Mathf.RoundToInt(winCleanPercent * PercentPermilleScale), 0, PercentPermilleScale);
    }

    private static float PermilleToPercentValue(int permille)
    {
        return permille / 10f;
    }

    private void GetToolSettings(out int radiusCells, out float cleanPerSecond)
    {
        switch (selectedTool)
        {
            case ToolType.Spray:
                radiusCells = 10;
                cleanPerSecond = 2.8f;
                break;
            case ToolType.Scraper:
                radiusCells = 5;
                cleanPerSecond = 4.8f;
                break;
            default:
                radiusCells = 7;
                cleanPerSecond = 3.6f * GameRunState.GetBrushPowerMultiplier();
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
        var finalCleanPercent = GetCleanPermille() / (float)PercentPermilleScale;
        var starsEarned = 0;
        var coinReward = 0;
        var comboBonusCoins = 0;
        GameRunState.LastLevelIndex = currentLevelIndex;
        GameRunState.LastLevelName = currentLevelConfig.Name;
        GameRunState.LastRunWon = won;
        GameRunState.LastActions = strokeCount;
        GameRunState.LastDurationSeconds = Time.time - roundStartTime;
        GameRunState.LastCleanPercent = finalCleanPercent;

        if (won)
        {
            starsEarned = CalculateStars(finalCleanPercent, currentLevelConfig.TargetCleanPercent);
            comboBonusCoins = CalculateComboBonusCoins(bestCombo);
            coinReward = CalculateCoinReward(starsEarned) + comboBonusCoins;
            GameRunState.UpdateLevelStars(currentLevelIndex, starsEarned);
            GameRunState.AddCoins(coinReward);
            GameRunState.UnlockLevel(currentLevelIndex + 1);
        }

        GameRunState.LastRunStars = starsEarned;
        GameRunState.LastBestCombo = bestCombo;
        GameRunState.LastComboBonusCoins = comboBonusCoins;
        GameRunState.LastRunCoinReward = coinReward;
        SceneManager.LoadScene(SceneNames.Results);
    }

    private int CalculateStars(float cleanPercent, float targetPercent)
    {
        if (cleanPercent < targetPercent)
        {
            return 0;
        }

        if (cleanPercent >= targetPercent + 0.15f)
        {
            return 3;
        }

        if (cleanPercent >= targetPercent + 0.07f)
        {
            return 2;
        }

        return 1;
    }

    private int CalculateCoinReward(int starsEarned)
    {
        if (starsEarned <= 0)
        {
            return 0;
        }

        return 40 + (currentLevelIndex * 12) + (starsEarned * 20);
    }

    private int CalculateComboBonusCoins(int bestComboInRun)
    {
        if (bestComboInRun < 3)
        {
            return 0;
        }

        return (bestComboInRun - 2) * 8;
    }
}
