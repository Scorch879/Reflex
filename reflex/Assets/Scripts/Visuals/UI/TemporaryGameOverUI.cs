using System;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class TemporaryGameOverUI : MonoBehaviour
{
    private static TemporaryGameOverUI _instance;

    private PlayerManager _observedPlayer;
    private CanvasGroup _canvasGroup;
    private TextMeshProUGUI _detailsText;
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
        BuildRuntimeUIIfNeeded();
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

        BuildRuntimeUIIfNeeded();
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

    private void BuildRuntimeUIIfNeeded()
    {
        if (_canvasGroup != null && _detailsText != null)
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
