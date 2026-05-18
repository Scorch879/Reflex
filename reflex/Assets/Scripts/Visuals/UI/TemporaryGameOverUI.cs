using System;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class TemporaryGameOverUI : MonoBehaviour
{
    private static TemporaryGameOverUI _instance;
    private const string DefaultLobbySceneName = "Lobby";

    [Header("Navigation")]
    [SerializeField] private string lobbySceneName = DefaultLobbySceneName;
    [SerializeField] private bool generateFreshRunOnReturn = true;

    private PlayerManager _observedPlayer;
    private TemporaryGameOverCanvasView _authoredCanvasView;
    private CanvasGroup _canvasGroup;
    private TextMeshProUGUI _detailsText;
    private Button _returnToLobbyButton;
    private bool _isShowing;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Bootstrap()
    {
        if (FindFirstObjectByType<TemporaryGameOverUI>() != null)
        {
            return;
        }

        GameObject managerObject = new GameObject("TemporaryGameOverUI");
        managerObject.AddComponent<TemporaryGameOverUI>();
    }

    private void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }

        _instance = this;
        DontDestroyOnLoad(gameObject);
        ResolveCanvasBindings();
    }

    private void OnEnable()
    {
        SceneManager.sceneLoaded += HandleSceneLoaded;
        TryBindPlayer();
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= HandleSceneLoaded;
        UnbindPlayer();
    }

    private void Update()
    {
        if (_observedPlayer == null)
        {
            TryBindPlayer();
        }
    }

    private void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (_isShowing)
        {
            return;
        }

        ResolveCanvasBindings();
        TryBindPlayer();
    }

    private void TryBindPlayer()
    {
        PlayerManager player = FindFirstObjectByType<PlayerManager>();
        if (player == null || _observedPlayer == player)
        {
            return;
        }

        UnbindPlayer();
        _observedPlayer = player;
        _observedPlayer.PlayerDied += HandlePlayerDied;
    }

    private void UnbindPlayer()
    {
        if (_observedPlayer == null)
        {
            return;
        }

        _observedPlayer.PlayerDied -= HandlePlayerDied;
        _observedPlayer = null;
    }

    private void HandlePlayerDied(PlayerManager deadPlayer)
    {
        ShowGameOver(deadPlayer);
    }

    private void ShowGameOver(PlayerManager deadPlayer)
    {
        if (_isShowing)
        {
            return;
        }

        ResolveCanvasBindings();
        if (_canvasGroup == null || _detailsText == null)
        {
            return;
        }

        RunRewardSummary summary = BuildFallbackSummary();
        if (RewardManager.Instance != null &&
            RewardManager.Instance.TryGetRunRewardSummary(out RunRewardSummary runSummary))
        {
            summary = runSummary;
        }

        int otherEssence = Mathf.Max(0, summary.totalEssenceEarned - summary.stageRewardEssence - summary.composureBonusEssence);
        _detailsText.text =
            "GAME OVER\n\n" +
            $"Runtime: {FormatRuntime(summary.runtimeSeconds)}\n" +
            $"Floor Cleared: {summary.floorReached}\n" +
            $"Stage Cleared: {summary.stageReached}\n" +
            $"Enemies Killed: {summary.enemiesDefeated}\n\n" +
            $"Total Soul Essence Earned: {summary.totalEssenceEarned}\n" +
            "Calculation:\n" +
            $"- Kill Essence: {summary.enemiesDefeated} x {summary.essencePerKill} = {summary.rawKillEssence}\n" +
            $"- Base Clear Essence: {summary.rawBaseEssence}\n" +
            $"- Floor Depth Essence: {summary.rawFloorEssence}\n" +
            $"- Raw Subtotal: {summary.rawEssenceBeforeMultipliers}\n" +
            $"- Effective Run Multiplier: x{summary.effectiveCombinedMultiplier:0.00}\n" +
            $"- Stage Reward Total: {summary.stageRewardEssence}\n" +
            $"- Composure Bonus: {summary.composureBonusEssence}\n" +
            $"- Other Bonus Sources: {otherEssence}";

        _canvasGroup.alpha = 1f;
        _canvasGroup.interactable = true;
        _canvasGroup.blocksRaycasts = true;
        if (_returnToLobbyButton != null)
        {
            _returnToLobbyButton.interactable = true;
        }

        _isShowing = true;
        Time.timeScale = 0f;
    }

    private RunRewardSummary BuildFallbackSummary()
    {
        RunRewardSummary fallback = new RunRewardSummary
        {
            runtimeSeconds = 0f,
            floorReached = 0,
            stageReached = 0,
            stagesCleared = 0,
            enemiesDefeated = 0,
            essencePerKill = 0,
            totalEssenceEarned = 0,
            stageRewardEssence = 0,
            composureBonusEssence = 0,
            rawBaseEssence = 0,
            rawKillEssence = 0,
            rawFloorEssence = 0,
            rawEssenceBeforeMultipliers = 0,
            effectiveCombinedMultiplier = 1f
        };

        if (LevelRunManager.HasInstance)
        {
            LevelClearContext context = LevelRunManager.Instance.LastClearContext;
            if (context.floorDepth > 0)
            {
                int stagesPerFloor = Mathf.Max(1, LevelRunManager.Instance.StagesPerFloor);
                fallback.floorReached = ((context.floorDepth - 1) / stagesPerFloor) + 1;
                fallback.stageReached = ((context.floorDepth - 1) % stagesPerFloor) + 1;
            }
        }

        return fallback;
    }

    private string FormatRuntime(float runtimeSeconds)
    {
        TimeSpan runTime = TimeSpan.FromSeconds(Mathf.Max(0f, runtimeSeconds));
        return runTime.TotalHours >= 1
            ? runTime.ToString(@"hh\:mm\:ss")
            : runTime.ToString(@"mm\:ss");
    }

    private void ResolveCanvasBindings()
    {
        if (TryBindAuthoredCanvas())
        {
            return;
        }

        BuildRuntimeUIIfNeeded();
    }

    private bool TryBindAuthoredCanvas()
    {
        if (_authoredCanvasView != null)
        {
            if (_authoredCanvasView.TryGetBindings(out CanvasGroup group, out TextMeshProUGUI details, out Button returnButton))
            {
                _canvasGroup = group;
                _detailsText = details;
                BindReturnToLobbyButton(returnButton);
                _authoredCanvasView.HideImmediate();
                return true;
            }
        }

        TemporaryGameOverCanvasView discoveredView = FindFirstObjectByType<TemporaryGameOverCanvasView>(FindObjectsInactive.Include);
        if (discoveredView == null)
        {
            return false;
        }

        if (!discoveredView.TryGetBindings(out CanvasGroup discoveredGroup, out TextMeshProUGUI discoveredDetails, out Button discoveredReturnButton))
        {
            Debug.LogWarning("TemporaryGameOverCanvasView found but missing CanvasGroup or details text reference.");
            return false;
        }

        _authoredCanvasView = discoveredView;
        _canvasGroup = discoveredGroup;
        _detailsText = discoveredDetails;
        BindReturnToLobbyButton(discoveredReturnButton);
        _authoredCanvasView.HideImmediate();
        return true;
    }

    private void BuildRuntimeUIIfNeeded()
    {
        if (_canvasGroup != null && _detailsText != null && _returnToLobbyButton != null)
        {
            return;
        }

        GameObject canvasObject = new GameObject(
            "Temporary Game Over Canvas",
            typeof(RectTransform),
            typeof(Canvas),
            typeof(CanvasScaler),
            typeof(GraphicRaycaster),
            typeof(CanvasGroup));

        canvasObject.transform.SetParent(transform, false);

        Canvas canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 2000;
        canvas.pixelPerfect = false;

        CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;

        _canvasGroup = canvasObject.GetComponent<CanvasGroup>();
        _canvasGroup.alpha = 0f;
        _canvasGroup.interactable = false;
        _canvasGroup.blocksRaycasts = false;

        RectTransform canvasRect = canvasObject.GetComponent<RectTransform>();
        canvasRect.anchorMin = Vector2.zero;
        canvasRect.anchorMax = Vector2.one;
        canvasRect.offsetMin = Vector2.zero;
        canvasRect.offsetMax = Vector2.zero;

        CreateImage("Dim", canvasRect, new Color(0f, 0f, 0f, 0.86f), Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, Vector2.one * 0.5f);
        RectTransform panel = CreateImage("Panel", canvasRect, new Color(0.08f, 0.08f, 0.1f, 0.98f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(1000f, 760f), Vector2.one * 0.5f);

        _detailsText = CreateText(
            "Details",
            panel,
            "GAME OVER",
            34f,
            FontStyles.Bold,
            new Color(0.98f, 0.98f, 0.98f, 1f),
            new Vector2(0.5f, 0.5f),
            new Vector2(0.5f, 0.5f),
            Vector2.zero,
            new Vector2(920f, 690f),
            new Vector2(0.5f, 0.5f),
            TextAlignmentOptions.TopLeft);

        RectTransform buttonRect = CreateImage(
            "Return To Lobby Button",
            panel,
            new Color(0.2f, 0.28f, 0.45f, 1f),
            new Vector2(0.5f, 0f),
            new Vector2(0.5f, 0f),
            new Vector2(0f, 58f),
            new Vector2(360f, 72f),
            new Vector2(0.5f, 0.5f));

        Button button = buttonRect.gameObject.AddComponent<Button>();
        ColorBlock colors = button.colors;
        colors.normalColor = Color.white;
        colors.highlightedColor = new Color(0.92f, 0.95f, 1f, 1f);
        colors.pressedColor = new Color(0.8f, 0.85f, 1f, 1f);
        button.colors = colors;

        CreateText(
            "Label",
            buttonRect,
            "Return To Lobby",
            30f,
            FontStyles.Bold,
            new Color(0.95f, 0.98f, 1f, 1f),
            new Vector2(0.5f, 0.5f),
            new Vector2(0.5f, 0.5f),
            Vector2.zero,
            new Vector2(340f, 56f),
            new Vector2(0.5f, 0.5f),
            TextAlignmentOptions.Center);

        BindReturnToLobbyButton(button);
    }

    private void BindReturnToLobbyButton(Button button)
    {
        if (_returnToLobbyButton != null)
        {
            _returnToLobbyButton.onClick.RemoveListener(HandleReturnToLobbyPressed);
        }

        _returnToLobbyButton = button;
        if (_returnToLobbyButton == null)
        {
            return;
        }

        _returnToLobbyButton.onClick.RemoveListener(HandleReturnToLobbyPressed);
        _returnToLobbyButton.onClick.AddListener(HandleReturnToLobbyPressed);
    }

    private void HandleReturnToLobbyPressed()
    {
        Time.timeScale = 1f;
        _isShowing = false;

        if (_canvasGroup != null)
        {
            _canvasGroup.alpha = 0f;
            _canvasGroup.interactable = false;
            _canvasGroup.blocksRaycasts = false;
        }

        PlayerManager player = _observedPlayer != null ? _observedPlayer : FindFirstObjectByType<PlayerManager>();
        if (player != null)
        {
            player.RespawnForRunStart();
        }

        if (generateFreshRunOnReturn && LevelRunManager.HasInstance)
        {
            LevelRunManager.Instance.GenerateNewRun();
        }

        string targetScene = string.IsNullOrWhiteSpace(lobbySceneName) ? DefaultLobbySceneName : lobbySceneName;
        SceneManager.LoadScene(targetScene, LoadSceneMode.Single);
    }

    private RectTransform CreateImage(
        string name,
        RectTransform parent,
        Color color,
        Vector2 anchorMin,
        Vector2 anchorMax,
        Vector2 anchoredPosition,
        Vector2 sizeDelta,
        Vector2 pivot)
    {
        GameObject imageObject = new GameObject(name, typeof(RectTransform), typeof(Image));
        imageObject.transform.SetParent(parent, false);
        RectTransform rect = imageObject.GetComponent<RectTransform>();
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.pivot = pivot;
        rect.anchoredPosition = anchoredPosition;
        rect.sizeDelta = sizeDelta;

        Image image = imageObject.GetComponent<Image>();
        image.color = color;
        return rect;
    }

    private TextMeshProUGUI CreateText(
        string name,
        RectTransform parent,
        string value,
        float fontSize,
        FontStyles fontStyle,
        Color color,
        Vector2 anchorMin,
        Vector2 anchorMax,
        Vector2 anchoredPosition,
        Vector2 sizeDelta,
        Vector2 pivot,
        TextAlignmentOptions alignment)
    {
        GameObject textObject = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
        textObject.transform.SetParent(parent, false);
        RectTransform rect = textObject.GetComponent<RectTransform>();
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.pivot = pivot;
        rect.anchoredPosition = anchoredPosition;
        rect.sizeDelta = sizeDelta;

        TextMeshProUGUI text = textObject.GetComponent<TextMeshProUGUI>();
        text.text = value;
        text.fontSize = fontSize;
        text.fontStyle = fontStyle;
        text.color = color;
        text.alignment = alignment;
        text.textWrappingMode = TextWrappingModes.Normal;
        text.font = TMP_Settings.defaultFontAsset;
        return text;
    }
}

[DisallowMultipleComponent]
public class TemporaryGameOverCanvasView : MonoBehaviour
{
    [Header("Required References")]
    [SerializeField] private CanvasGroup canvasGroup;
    [SerializeField] private TextMeshProUGUI detailsText;
    [SerializeField] private Button returnToLobbyButton;

    [Header("Runtime Behavior")]
    [SerializeField] private bool hideOnAwake = true;

    private void Awake()
    {
        if (hideOnAwake)
        {
            HideImmediate();
        }
    }

    public bool TryGetBindings(out CanvasGroup group, out TextMeshProUGUI details, out Button returnButton)
    {
        group = canvasGroup;
        details = detailsText;
        returnButton = returnToLobbyButton;
        return group != null && details != null;
    }

    public void HideImmediate()
    {
        if (canvasGroup == null)
        {
            return;
        }

        canvasGroup.alpha = 0f;
        canvasGroup.interactable = false;
        canvasGroup.blocksRaycasts = false;
    }

    private void Reset()
    {
        if (canvasGroup == null)
        {
            canvasGroup = GetComponent<CanvasGroup>();
        }

        if (detailsText == null)
        {
            detailsText = GetComponentInChildren<TextMeshProUGUI>(true);
        }

        if (returnToLobbyButton == null)
        {
            returnToLobbyButton = GetComponentInChildren<Button>(true);
        }
    }
}
