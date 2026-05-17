using System;
using System.Collections.Generic;
using System.Linq;
using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;
using UnityEngine.EventSystems;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem.UI;
#endif

[Serializable]
public struct LevelRewardContext
{
    public int nodeId;
    public int floorDepth;
    public int levelNumber;
    public int stageNumber;
    public string sceneName;
    public int kills;
    public int soulEssenceAwarded;
    public int baseEssenceComponent;
    public int killEssenceComponent;
    public int floorEssenceComponent;
    public int rawEssenceBeforeMultipliers;
    public float levelMultiplier;
    public float playerEssenceMultiplier;
    public float combinedEssenceMultiplier;

    public string LevelStageLabel => levelNumber + "-" + stageNumber;
}

public struct RunRewardSummary
{
    public float runtimeSeconds;
    public int floorReached;
    public int stageReached;
    public int stagesCleared;
    public int enemiesDefeated;
    public int essencePerKill;
    public int totalEssenceEarned;
    public int stageRewardEssence;
    public int composureBonusEssence;
    public int rawBaseEssence;
    public int rawKillEssence;
    public int rawFloorEssence;
    public int rawEssenceBeforeMultipliers;
    public float effectiveCombinedMultiplier;
}

public enum StageCardRewardTriggerMode
{
    AnyStageClear = 0,
    CombatRoomClear = 1,
    CalmCombatRoomClear = 2
}

public class RewardManager : MonoBehaviour
{
    private static RewardManager _instance;
    private const float TimeScaleEpsilon = 0.0001f;

    [Header("References")]
    [SerializeField] private PlayerManager playerManager;
    [SerializeField] private CanvasGroup rewardCanvasGroup; // For fading in/out the reward UI
    [SerializeField] private BuffCardUI[] cardUI; // 3 UI slots for the buff cards
    [Header("Settings")]
    [SerializeField] private float fadeInDuration = 0.5f;
    [SerializeField] private float fadeOutDuration = 0.5f;
    [SerializeField] private bool openCardRewardsOnLevelClear = true;
    [SerializeField] private StageCardRewardTriggerMode stageCardRewardTrigger = StageCardRewardTriggerMode.AnyStageClear;

    [Header("Card Pool")]
    [SerializeField] private BuffCardData[] allAvailableCards;

    [Header("Soul Essence Rewards")]
    [SerializeField, Min(1)] private int stagesPerLevel = 5;
    [SerializeField, Min(0)] private int baseEssencePerClear = 5;
    [SerializeField, Min(0)] private int essencePerKill = 2;
    [SerializeField, Min(0)] private int essencePerFloor = 1;
    [SerializeField, Min(0f)] private float levelRewardMultiplierStep = 0.1f;

    [Header("Composure Rewards")]
    [SerializeField] private bool awardComposureEssenceBonus = true;
    [SerializeField, Min(0)] private int composureBonusBaseEssence = 4;
    [SerializeField, Min(0)] private int composureBonusMaxEssence = 12;
    [SerializeField, Range(0f, 1f)] private float composureBonusScoreThreshold = 0.42f;
    [SerializeField, Min(0f)] private float composureBonusMaxDamageTaken = 14f;
    [SerializeField, Min(0)] private int composureBonusMaxDeaths = 0;
    [SerializeField, Min(0)] private int composureBonusMinAttacks = 4;
    [SerializeField] private Color composureMessageColor = new Color(0.55f, 1f, 0.75f);

    public event Action<LevelRewardContext> LevelRewardGranted;

    public static RewardManager Instance => _instance;

    public LevelRewardContext LastRewardContext { get; private set; }

    private sealed class ActiveCardBuff
    {
        public ActiveCardBuff(BuffCardData sourceCard, int remainingStageClears)
        {
            card = sourceCard;
            stagesRemaining = remainingStageClears;
        }

        public BuffCardData card;
        public int stagesRemaining; // -1 means whole run.
    }

    private int _killsThisLevel;
    private readonly List<ActiveCardBuff> _activeCardBuffs = new List<ActiveCardBuff>();
    private readonly HashSet<BuffCardData> _selectedSpecialCards = new HashSet<BuffCardData>();
    private readonly HashSet<BuffCardData> _blockedCards = new HashSet<BuffCardData>();
    private bool _rewardScreenOpen;
    private bool _usingRuntimeRewardUI;
    private Button[] _runtimeCardButtons;
    private TextMeshProUGUI[] _runtimeCardNameTexts;
    private TextMeshProUGUI[] _runtimeCardDescriptionTexts;
    private BuffCardData[] _runtimeChoices;
    private readonly List<BuffCardData> _runtimeGeneratedCards = new List<BuffCardData>();
    private Coroutine _fadeInCoroutine;
    private Coroutine _fadeOutCoroutine;
    private PlayerManager _subscribedPlayerManager;
    private bool _runSummaryInitialized;
    private float _runStartRealtime;
    private int _runTotalEnemiesDefeated;
    private int _runTotalStagesCleared;
    private int _runLastClearedFloor;
    private int _runLastClearedStage;
    private int _runBaseEssenceComponentTotal;
    private int _runKillEssenceComponentTotal;
    private int _runFloorEssenceComponentTotal;
    private int _runRawEssenceBeforeMultiplierTotal;
    private int _runStageRewardEssenceTotal;
    private int _runComposureBonusEssenceTotal;
    private int _runTotalEssenceEarned;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Bootstrap()
    {
        if (FindFirstObjectByType<RewardManager>() != null)
        {
            return;
        }

        GameObject rewardManagerObject = new GameObject("RewardManager");
        rewardManagerObject.AddComponent<RewardManager>();
    }

    private void Awake()
    {
        if (_instance != null && _instance != this)
        {
            int existingScore = _instance.GetConfigurationScore();
            int currentScore = GetConfigurationScore();

            if (currentScore > existingScore)
            {
                Destroy(_instance.gameObject);
                _instance = this;
            }
            else
            {
                Destroy(gameObject);
                return;
            }
        }

        _instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void OnEnable()
    {
        EnsureRewardUIAndCardPoolReady();
        EnsurePlayerManager();
        BindPlayerEvents();
        LevelRunManager.LevelEntered += HandleLevelEntered;
        LevelRunManager.LevelClearedDetailed += HandleLevelCleared;
        EnemyController.EnemyDefeated += HandleEnemyDefeated;
        EmotionEngine.RoomEvaluated += HandleRoomEvaluated;
        SceneManager.sceneUnloaded += HandleSceneUnloaded;
    }

    private void OnDisable()
    {
        LevelRunManager.LevelEntered -= HandleLevelEntered;
        LevelRunManager.LevelClearedDetailed -= HandleLevelCleared;
        EnemyController.EnemyDefeated -= HandleEnemyDefeated;
        EmotionEngine.RoomEvaluated -= HandleRoomEvaluated;
        SceneManager.sceneUnloaded -= HandleSceneUnloaded;
        UnbindPlayerEvents();
        ForceCloseRewardScreenImmediate();
    }

    private void OnDestroy()
    {
        ForceCloseRewardScreenImmediate();

        if (_instance == this)
        {
            _instance = null;
        }

        for (int i = 0; i < _runtimeGeneratedCards.Count; i++)
        {
            if (_runtimeGeneratedCards[i] != null)
            {
                Destroy(_runtimeGeneratedCards[i]);
            }
        }

        _runtimeGeneratedCards.Clear();
    }

    void Update()
    {
        // For testing: Press 'K' to simulate clearing a floor
        if (Keyboard.current != null && Keyboard.current.kKey.wasPressedThisFrame)
        {
            OpenRewardScreen();
        }
    }

    public void OpenRewardScreen()
    {
        OpenRewardScreen(false);
    }

    private void OpenRewardScreen(bool calmClear)
    {
        if (_rewardScreenOpen)
        {
            return;
        }

        if (!EnsureRewardUIAndCardPoolReady())
        {
            Debug.LogWarning("RewardManager could not open reward UI because no UI/card pool is available.");
            return;
        }

        EnsureEventSystem();

        int choiceCapacity = _usingRuntimeRewardUI
            ? (_runtimeCardButtons != null ? _runtimeCardButtons.Length : 0)
            : (cardUI != null ? cardUI.Length : 0);

        if (choiceCapacity <= 0)
        {
            return;
        }

        List<BuffCardData> choices = BuildWeightedCardChoices(choiceCapacity, calmClear);
        if (choices.Count <= 0)
        {
            Debug.LogWarning("RewardManager skipped reward UI because no buff cards were available.");
            return;
        }

        _rewardScreenOpen = true;
        StartFadeIn();

        if (!_usingRuntimeRewardUI)
        {
            foreach (BuffCardUI card in cardUI)
            {
                if (card != null)
                {
                    card.ClearBuffText();
                }
            }
        }

        if (_usingRuntimeRewardUI)
        {
            AssignRuntimeChoices(choices);
            return;
        }

        // assign each card to a socket
        for (int i = 0; i < cardUI.Length; i++)
        {
            BuffCardData choice = i < choices.Count ? choices[i] : null;
            if (cardUI[i] != null && choice != null)
            {
                cardUI[i].Setup(choice);
            }
        }
    }

    private IEnumerator FadeInUI()
    {
        if (rewardCanvasGroup == null)
        {
            ForceCloseRewardScreenImmediate();
            yield break;
        }

        float duration = Mathf.Max(TimeScaleEpsilon, fadeInDuration);
        float elapsed = 0f;

        rewardCanvasGroup.interactable = true;
        rewardCanvasGroup.blocksRaycasts = true;

        while (elapsed < duration)
        {
            rewardCanvasGroup.alpha = Mathf.Lerp(0f, 1f, elapsed / duration);
            Time.timeScale = Mathf.Lerp(1f, 0f, elapsed / duration);
            elapsed += Time.unscaledDeltaTime;
            yield return null;
        }
        rewardCanvasGroup.alpha = 1f;
        Time.timeScale = 0f;
        _fadeInCoroutine = null;
    }

    private IEnumerator FadeOutUI()
    {
        if (rewardCanvasGroup == null)
        {
            ForceCloseRewardScreenImmediate();
            yield break;
        }

        float duration = Mathf.Max(0f, fadeOutDuration);
        float elapsed = 0f;

        rewardCanvasGroup.interactable = false;
        rewardCanvasGroup.blocksRaycasts = false;

        Time.timeScale = 1f;

        while (elapsed < duration)
        {
            rewardCanvasGroup.alpha = Mathf.Lerp(1f, 0f, elapsed / duration);
            elapsed += Time.unscaledDeltaTime;
            yield return null;
        }
        rewardCanvasGroup.alpha = 0f;
        _rewardScreenOpen = false;
        _fadeOutCoroutine = null;
    }

    public void SelectCard(BuffCardData card)
    {
        if (card == null)
        {
            return;
        }

        EnsurePlayerManager();

        if (playerManager == null)
        {
            StartFadeOut();
            return;
        }

        if (!IsCardSelectable(card))
        {
            StartFadeOut();
            return;
        }

        int duration = card.buffDurationStages > 0 ? card.buffDurationStages : -1;
        _activeCardBuffs.Add(new ActiveCardBuff(card, duration));

        if (card.isSpecialCard)
        {
            _selectedSpecialCards.Add(card);
            RegisterCardBlocks(card);
        }

        ReapplyActiveCardBuffsToPlayer();

        StartFadeOut();
    }

    private void HandleLevelEntered(int nodeId, int floorDepth, string sceneName)
    {
        _killsThisLevel = 0;
        EnsurePlayerManager();
        BindPlayerEvents();

        if (nodeId > 0)
        {
            if (_runSummaryInitialized && floorDepth == 1 && _runTotalStagesCleared > 0)
            {
                ResetRunStateForFreshStart();
            }

            EnsureRunSummaryInitialized();
            ReapplyActiveCardBuffsToPlayer();
        }
    }

    private void HandleEnemyDefeated(EnemyController enemy)
    {
        _killsThisLevel++;
        if (_runSummaryInitialized)
        {
            _runTotalEnemiesDefeated++;
        }
    }

    private void HandleLevelCleared(LevelClearContext clearContext)
    {
        int nodeId = clearContext.nodeId;
        int floorDepth = clearContext.floorDepth;
        string sceneName = clearContext.sceneName;

        if (floorDepth <= 0)
        {
            return;
        }

        EnsureRunSummaryInitialized();
        _runTotalStagesCleared++;
        UpdateLastClearedProgress(floorDepth);

        bool calmClear = clearContext.hasRoomReport && clearContext.roomReport.emotionAfter == PlayerEmotionState.Calm;
        bool shouldGrantReward = ShouldGrantStageCardReward(clearContext, calmClear);

        if (shouldGrantReward)
        {
            EnsurePlayerManager();
            BindPlayerEvents();

            LevelRewardContext context = BuildRewardContext(nodeId, floorDepth, sceneName);
            LastRewardContext = context;
            AddContextToRunSummary(context);

            if (playerManager != null)
            {
                playerManager.AddSoulEssence(context.soulEssenceAwarded);
            }

            LevelRewardGranted?.Invoke(context);

            if (openCardRewardsOnLevelClear)
            {
                OpenRewardScreen(calmClear);
            }
        }

        ConsumeStageLimitedCardDurations();
    }

    private void HandleRoomEvaluated(EmotionRoomReport report)
    {
        if (!awardComposureEssenceBonus || !IsComposureBonusEligible(report))
        {
            return;
        }

        EnsurePlayerManager();
        if (playerManager == null)
        {
            return;
        }

        int bonus = CalculateComposureBonus(report);
        if (bonus <= 0)
        {
            return;
        }

        EnsureRunSummaryInitialized();
        playerManager.AddSoulEssence(bonus);
        _runComposureBonusEssenceTotal += bonus;

        if (InGameUIManager.Instance != null)
        {
            InGameUIManager.Instance.ShowStatusMessage($"+{bonus} Soul Essence (Composure)", composureMessageColor);
        }

        Debug.Log($"Composure reward: +{bonus} Soul Essence (room {report.roomNumber}, score {report.scoreAfter:0.00}, damage {report.damageTaken:0.0}).");
    }

    private LevelRewardContext BuildRewardContext(int nodeId, int floorDepth, string sceneName)
    {
        int configuredStagesPerLevel = stagesPerLevel;
        if (LevelRunManager.HasInstance)
        {
            configuredStagesPerLevel = Mathf.Max(1, LevelRunManager.Instance.StagesPerFloor);
        }

        int levelNumber = ((floorDepth - 1) / configuredStagesPerLevel) + 1;
        int stageNumber = ((floorDepth - 1) % configuredStagesPerLevel) + 1;
        float levelMultiplier = 1f + ((levelNumber - 1) * levelRewardMultiplierStep);
        int baseEssenceComponent = baseEssencePerClear;
        int killEssenceComponent = _killsThisLevel * essencePerKill;
        int floorEssenceComponent = floorDepth * essencePerFloor;
        int rawEssence = baseEssenceComponent + killEssenceComponent + floorEssenceComponent;
        float playerEssenceMultiplier = GetPlayerEssenceMultiplier();
        float combinedMultiplier = levelMultiplier * playerEssenceMultiplier;
        int essenceAwarded = Mathf.Max(0, Mathf.RoundToInt(rawEssence * combinedMultiplier));

        return new LevelRewardContext
        {
            nodeId = nodeId,
            floorDepth = floorDepth,
            levelNumber = levelNumber,
            stageNumber = stageNumber,
            sceneName = sceneName,
            kills = _killsThisLevel,
            soulEssenceAwarded = essenceAwarded,
            baseEssenceComponent = baseEssenceComponent,
            killEssenceComponent = killEssenceComponent,
            floorEssenceComponent = floorEssenceComponent,
            rawEssenceBeforeMultipliers = rawEssence,
            levelMultiplier = levelMultiplier,
            playerEssenceMultiplier = playerEssenceMultiplier,
            combinedEssenceMultiplier = combinedMultiplier
        };
    }

    private float GetPlayerEssenceMultiplier()
    {
        return playerManager != null ? Mathf.Max(0f, playerManager.FinalEssenceMultiplier) : 1f;
    }

    private bool IsComposureBonusEligible(EmotionRoomReport report)
    {
        if (report.scoreAfter > composureBonusScoreThreshold)
        {
            return false;
        }

        if (report.damageTaken > composureBonusMaxDamageTaken)
        {
            return false;
        }

        if (report.deathCount > composureBonusMaxDeaths)
        {
            return false;
        }

        if (report.attacksPerformed < composureBonusMinAttacks)
        {
            return false;
        }

        return true;
    }

    private int CalculateComposureBonus(EmotionRoomReport report)
    {
        float scoreQuality = 1f - Mathf.Clamp01(report.scoreAfter);
        float damageQuality = 1f - Mathf.Clamp01(report.damageTaken / Mathf.Max(1f, composureBonusMaxDamageTaken));
        float quality = Mathf.Clamp01((scoreQuality * 0.65f) + (damageQuality * 0.35f));
        int scaledBonus = Mathf.RoundToInt(composureBonusBaseEssence * (1f + quality));
        return Mathf.Clamp(scaledBonus, 0, composureBonusMaxEssence);
    }

    private int GetConfigurationScore()
    {
        int score = 0;
        if (HasSerializedRewardUIBindings())
        {
            score += 10;
        }

        if (allAvailableCards != null && allAvailableCards.Any(card => card != null))
        {
            score += 5;
        }

        if (_usingRuntimeRewardUI)
        {
            score += 2;
        }

        return score;
    }

    private bool HasSerializedRewardUIBindings()
    {
        return rewardCanvasGroup != null &&
               cardUI != null &&
               cardUI.Length > 0 &&
               cardUI.Any(slot => slot != null);
    }

    private bool EnsureRewardUIAndCardPoolReady()
    {
        EnsureRuntimeCardPoolFallback();
        ApplyDefaultCardRulesIfMissing();

        if (!HasSerializedRewardUIBindings())
        {
            BuildRuntimeRewardUIIfNeeded();
        }
        else
        {
            _usingRuntimeRewardUI = false;
        }

        bool hasAnyCardPool = allAvailableCards != null && allAvailableCards.Any(card => card != null);
        bool hasAnyUi = HasSerializedRewardUIBindings() || HasRuntimeRewardUIBindings();
        return hasAnyUi && hasAnyCardPool;
    }

    private bool HasRuntimeRewardUIBindings()
    {
        return _usingRuntimeRewardUI &&
               rewardCanvasGroup != null &&
               _runtimeCardButtons != null &&
               _runtimeCardButtons.Length > 0;
    }

    private void EnsureRuntimeCardPoolFallback()
    {
        if (allAvailableCards != null && allAvailableCards.Any(card => card != null))
        {
            return;
        }

        if (_runtimeGeneratedCards.Count == 0)
        {
            BuildRuntimeCardPool();
        }

        allAvailableCards = _runtimeGeneratedCards.ToArray();
    }

    private void BuildRuntimeCardPool()
    {
        BuffCardData bruteForce = CreateRuntimeCard("Brute Force I", "Increase your strength by 10%.", 24, 0, atkBonus: 0.1f);
        BuffCardData precision = CreateRuntimeCard("Precision I", "Increase your crit chance by 1%.", 22, 0, critBonus: 0.01f);
        BuffCardData fleetFoot = CreateRuntimeCard("Fleet foot", "Sacrifice dash distance for a shorter dash cooldown.", 12, 1, dashCdReduction: 0.5f, dashDistanceBonus: -15f, isSpecialCard: true);
        BuffCardData glassCannon = CreateRuntimeCard("Glass Cannon", "Double damage taken and dealt.", 6, 0, isGlassCannon: true);
        BuffCardData momentumRhythm = CreateRuntimeCard("Momentum Rhythm", "Extend combo timing to keep pressure between attacks.", 18, 2, comboWindowBonus: 0.3f);
        BuffCardData soulSiphon = CreateRuntimeCard("Soul Siphon I", "Small chance to lifesteal from hits this stage.", 14, 2, vampiricBonus: 0.12f);
        BuffCardData essenceSurge = CreateRuntimeCard("Essence Surge I", "Increase Soul Essence gains for the next stage.", 13, 4, essenceBonus: 0.2f);
        BuffCardData kineticFocus = CreateRuntimeCard("Kinetic Focus", "Boost crit chance and combo control for this stage.", 16, 1, critBonus: 0.03f, comboWindowBonus: 0.15f);
        BuffCardData windrunner = CreateRuntimeCard("Windrunner", "Faster dash cooldown with improved dash reach.", 12, 2, dashCdReduction: 0.25f, dashDistanceBonus: 6f, isSpecialCard: true);
        BuffCardData berserkerTempo = CreateRuntimeCard("Berserker Tempo", "A rare burst of damage and crit for one stage.", 7, 0, atkBonus: 0.14f, critBonus: 0.02f, durationStages: 1);

        fleetFoot.blockedCards = new[] { windrunner };
        windrunner.blockedCards = new[] { fleetFoot };

        _runtimeGeneratedCards.Add(bruteForce);
        _runtimeGeneratedCards.Add(precision);
        _runtimeGeneratedCards.Add(fleetFoot);
        _runtimeGeneratedCards.Add(glassCannon);
        _runtimeGeneratedCards.Add(momentumRhythm);
        _runtimeGeneratedCards.Add(soulSiphon);
        _runtimeGeneratedCards.Add(essenceSurge);
        _runtimeGeneratedCards.Add(kineticFocus);
        _runtimeGeneratedCards.Add(windrunner);
        _runtimeGeneratedCards.Add(berserkerTempo);
    }

    private BuffCardData CreateRuntimeCard(
        string name,
        string description,
        int obtainWeight,
        int calmBonusWeight,
        float atkBonus = 0f,
        float critBonus = 0f,
        float comboWindowBonus = 0f,
        float dashCdReduction = 0f,
        float dashDistanceBonus = 0f,
        float essenceBonus = 0f,
        float vampiricBonus = 0f,
        int durationStages = 0,
        bool isSpecialCard = false,
        bool isGlassCannon = false)
    {
        BuffCardData card = ScriptableObject.CreateInstance<BuffCardData>();
        card.hideFlags = HideFlags.HideAndDontSave;
        card.cardName = name;
        card.description = description;
        card.obtainWeight = obtainWeight;
        card.calmStateBonusWeight = calmBonusWeight;
        card.atkBonus = atkBonus;
        card.critBonus = critBonus;
        card.comboWindowBonus = comboWindowBonus;
        card.dashCDReduction = dashCdReduction;
        card.dashDistanceBonus = dashDistanceBonus;
        card.essenceBonus = essenceBonus;
        card.vampiricBonus = vampiricBonus;
        card.buffDurationStages = Mathf.Max(0, durationStages);
        card.isSpecialCard = isSpecialCard;
        card.isGlassCannon = isGlassCannon;
        return card;
    }

    private void BuildRuntimeRewardUIIfNeeded()
    {
        if (HasRuntimeRewardUIBindings())
        {
            return;
        }

        GameObject canvasObject = new GameObject(
            "Reward Canvas (Runtime)",
            typeof(RectTransform),
            typeof(Canvas),
            typeof(CanvasScaler),
            typeof(GraphicRaycaster),
            typeof(CanvasGroup));

        canvasObject.transform.SetParent(transform, false);

        Canvas canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 800;
        canvas.pixelPerfect = false;

        CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;

        CanvasGroup canvasGroup = canvasObject.GetComponent<CanvasGroup>();
        canvasGroup.alpha = 0f;
        canvasGroup.interactable = false;
        canvasGroup.blocksRaycasts = false;
        rewardCanvasGroup = canvasGroup;

        RectTransform canvasRect = canvasObject.GetComponent<RectTransform>();
        canvasRect.anchorMin = Vector2.zero;
        canvasRect.anchorMax = Vector2.one;
        canvasRect.offsetMin = Vector2.zero;
        canvasRect.offsetMax = Vector2.zero;

        CreateImage("Dim", canvasRect, new Color(0f, 0f, 0f, 0.78f), Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, Vector2.one * 0.5f);
        RectTransform panel = CreateImage("Panel", canvasRect, new Color(0.09f, 0.11f, 0.18f, 0.98f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(1460f, 760f), Vector2.one * 0.5f);

        CreateText("Title", panel, "Select Buff!", 68f, FontStyles.Bold, new Color(0.95f, 0.97f, 1f, 1f),
            new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -84f), new Vector2(900f, 90f), new Vector2(0.5f, 0.5f), TextAlignmentOptions.Center);
        CreateText("Hint", panel, "Tap a card to claim a buff for this run", 34f, FontStyles.Normal, new Color(0.82f, 0.88f, 1f, 0.95f),
            new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 56f), new Vector2(1200f, 60f), new Vector2(0.5f, 0.5f), TextAlignmentOptions.Center);

        _runtimeCardButtons = new Button[3];
        _runtimeCardNameTexts = new TextMeshProUGUI[3];
        _runtimeCardDescriptionTexts = new TextMeshProUGUI[3];
        _runtimeChoices = new BuffCardData[3];

        float[] xOffsets = { -430f, 0f, 430f };
        for (int i = 0; i < _runtimeCardButtons.Length; i++)
        {
            RectTransform card = CreateImage("Card " + (i + 1), panel, new Color(0.13f, 0.16f, 0.25f, 1f),
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(xOffsets[i], -12f), new Vector2(360f, 500f), Vector2.one * 0.5f);
            Image cardImage = card.GetComponent<Image>();
            cardImage.raycastTarget = true;

            Button button = card.gameObject.AddComponent<Button>();
            ColorBlock buttonColors = button.colors;
            buttonColors.normalColor = Color.white;
            buttonColors.highlightedColor = new Color(0.92f, 0.95f, 1f, 1f);
            buttonColors.pressedColor = new Color(0.8f, 0.85f, 1f, 1f);
            button.colors = buttonColors;
            int capturedIndex = i;
            button.onClick.AddListener(() => OnRuntimeCardChoiceSelected(capturedIndex));

            _runtimeCardButtons[i] = button;
            _runtimeCardNameTexts[i] = CreateText("Name", card, "", 34f, FontStyles.Bold, Color.white,
                new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -44f), new Vector2(320f, 60f), new Vector2(0.5f, 0.5f), TextAlignmentOptions.Center);
            _runtimeCardDescriptionTexts[i] = CreateText("Description", card, "", 25f, FontStyles.Normal, new Color(0.9f, 0.93f, 1f, 1f),
                new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 146f), new Vector2(320f, 240f), new Vector2(0.5f, 0.5f), TextAlignmentOptions.TopLeft);
        }

        _usingRuntimeRewardUI = true;
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

    private void AssignRuntimeChoices(List<BuffCardData> choices)
    {
        if (!HasRuntimeRewardUIBindings() || _runtimeChoices == null)
        {
            return;
        }

        for (int i = 0; i < _runtimeCardButtons.Length; i++)
        {
            BuffCardData choice = i < choices.Count ? choices[i] : null;
            _runtimeChoices[i] = choice;

            if (_runtimeCardButtons[i] != null)
            {
                _runtimeCardButtons[i].interactable = choice != null;
            }

            if (_runtimeCardNameTexts[i] != null)
            {
                _runtimeCardNameTexts[i].text = choice != null ? choice.cardName : "Locked";
            }

            if (_runtimeCardDescriptionTexts[i] != null)
            {
                _runtimeCardDescriptionTexts[i].text = choice != null ? choice.description : "No reward available for this slot.";
            }
        }
    }

    private void OnRuntimeCardChoiceSelected(int index)
    {
        if (_runtimeChoices == null || index < 0 || index >= _runtimeChoices.Length)
        {
            return;
        }

        BuffCardData card = _runtimeChoices[index];
        if (card == null)
        {
            return;
        }

        SelectCard(card);
    }

    private bool ShouldGrantStageCardReward(LevelClearContext clearContext, bool calmClear)
    {
        if (clearContext.nodeId <= 0)
        {
            return false;
        }

        switch (stageCardRewardTrigger)
        {
            case StageCardRewardTriggerMode.AnyStageClear:
                return true;
            case StageCardRewardTriggerMode.CombatRoomClear:
                return clearContext.reason == LevelClearReason.RoomEvaluated;
            case StageCardRewardTriggerMode.CalmCombatRoomClear:
                return clearContext.reason == LevelClearReason.RoomEvaluated && calmClear;
            default:
                return clearContext.reason == LevelClearReason.RoomEvaluated;
        }
    }

    private List<BuffCardData> BuildWeightedCardChoices(int maxChoices, bool calmClear)
    {
        List<BuffCardData> choices = new List<BuffCardData>();
        if (allAvailableCards == null || allAvailableCards.Length == 0 || maxChoices <= 0)
        {
            return choices;
        }

        List<BuffCardData> pool = allAvailableCards
            .Where(card => IsCardSelectable(card))
            .Distinct()
            .ToList();

        while (choices.Count < maxChoices && pool.Count > 0)
        {
            BuffCardData selected = PickWeightedCard(pool, calmClear);
            if (selected == null)
            {
                break;
            }

            choices.Add(selected);
            pool.Remove(selected);
        }

        return choices;
    }

    private BuffCardData PickWeightedCard(List<BuffCardData> pool, bool calmClear)
    {
        if (pool == null || pool.Count == 0)
        {
            return null;
        }

        int totalWeight = 0;
        for (int i = 0; i < pool.Count; i++)
        {
            totalWeight += GetCardObtainWeight(pool[i], calmClear);
        }

        if (totalWeight <= 0)
        {
            int fallbackIndex = UnityEngine.Random.Range(0, pool.Count);
            return pool[fallbackIndex];
        }

        int roll = UnityEngine.Random.Range(0, totalWeight);
        for (int i = 0; i < pool.Count; i++)
        {
            BuffCardData candidate = pool[i];
            roll -= GetCardObtainWeight(candidate, calmClear);
            if (roll < 0)
            {
                return candidate;
            }
        }

        return pool[pool.Count - 1];
    }

    private int GetCardObtainWeight(BuffCardData card, bool calmClear)
    {
        if (card == null)
        {
            return 0;
        }

        int baseWeight = Mathf.Max(0, card.obtainWeight);
        if (baseWeight == 0)
        {
            baseWeight = 1;
        }

        if (!calmClear)
        {
            return baseWeight;
        }

        return baseWeight + Mathf.Max(0, card.calmStateBonusWeight);
    }

    private bool IsCardSelectable(BuffCardData card)
    {
        if (card == null)
        {
            return false;
        }

        if (_blockedCards.Contains(card))
        {
            return false;
        }

        return !card.isSpecialCard || !_selectedSpecialCards.Contains(card);
    }

    private void RegisterCardBlocks(BuffCardData sourceCard)
    {
        if (sourceCard == null || sourceCard.blockedCards == null)
        {
            return;
        }

        for (int i = 0; i < sourceCard.blockedCards.Length; i++)
        {
            BuffCardData blockedCard = sourceCard.blockedCards[i];
            if (blockedCard != null)
            {
                _blockedCards.Add(blockedCard);
            }
        }
    }

    private void ReapplyActiveCardBuffsToPlayer()
    {
        EnsurePlayerManager();
        if (playerManager == null)
        {
            return;
        }

        playerManager.ClearTemporaryCardBuffs();

        for (int i = 0; i < _activeCardBuffs.Count; i++)
        {
            ActiveCardBuff activeBuff = _activeCardBuffs[i];
            if (activeBuff == null || activeBuff.card == null)
            {
                continue;
            }

            BuffCardData card = activeBuff.card;
            playerManager.cardAtkBonus += card.atkBonus;
            playerManager.cardCritChance += card.critBonus;
            playerManager.cardEssenceMult += card.essenceBonus;
            playerManager.cardVampChance += card.vampiricBonus;
            playerManager.cardComboWindowBonus += card.comboWindowBonus;
            playerManager.cardDashCDReduction += card.dashCDReduction;
            playerManager.cardDashDistanceBonus += card.dashDistanceBonus;

            if (card.isGlassCannon)
            {
                playerManager.ApplyGlassCannon();
            }
        }
    }

    private void ConsumeStageLimitedCardDurations()
    {
        bool removedAnyCard = false;

        for (int i = _activeCardBuffs.Count - 1; i >= 0; i--)
        {
            ActiveCardBuff activeBuff = _activeCardBuffs[i];
            if (activeBuff == null || activeBuff.card == null)
            {
                _activeCardBuffs.RemoveAt(i);
                removedAnyCard = true;
                continue;
            }

            if (activeBuff.stagesRemaining < 0)
            {
                continue;
            }

            activeBuff.stagesRemaining--;
            if (activeBuff.stagesRemaining <= 0)
            {
                _activeCardBuffs.RemoveAt(i);
                removedAnyCard = true;
            }
        }

        if (removedAnyCard)
        {
            ReapplyActiveCardBuffsToPlayer();
        }
    }

    private void AddContextToRunSummary(LevelRewardContext context)
    {
        _runBaseEssenceComponentTotal += Mathf.Max(0, context.baseEssenceComponent);
        _runKillEssenceComponentTotal += Mathf.Max(0, context.killEssenceComponent);
        _runFloorEssenceComponentTotal += Mathf.Max(0, context.floorEssenceComponent);
        _runRawEssenceBeforeMultiplierTotal += Mathf.Max(0, context.rawEssenceBeforeMultipliers);
        _runStageRewardEssenceTotal += Mathf.Max(0, context.soulEssenceAwarded);
    }

    private void UpdateLastClearedProgress(int floorDepth)
    {
        int configuredStagesPerLevel = stagesPerLevel;
        if (LevelRunManager.HasInstance)
        {
            configuredStagesPerLevel = Mathf.Max(1, LevelRunManager.Instance.StagesPerFloor);
        }

        _runLastClearedFloor = ((floorDepth - 1) / Mathf.Max(1, configuredStagesPerLevel)) + 1;
        _runLastClearedStage = ((floorDepth - 1) % Mathf.Max(1, configuredStagesPerLevel)) + 1;
    }

    private void EnsureRunSummaryInitialized()
    {
        if (_runSummaryInitialized)
        {
            return;
        }

        ResetRunSummaryState();
        _runSummaryInitialized = true;
        _runStartRealtime = Time.realtimeSinceStartup;
    }

    private void ResetRunSummaryState()
    {
        _runTotalEnemiesDefeated = 0;
        _runTotalStagesCleared = 0;
        _runLastClearedFloor = 0;
        _runLastClearedStage = 0;
        _runBaseEssenceComponentTotal = 0;
        _runKillEssenceComponentTotal = 0;
        _runFloorEssenceComponentTotal = 0;
        _runRawEssenceBeforeMultiplierTotal = 0;
        _runStageRewardEssenceTotal = 0;
        _runComposureBonusEssenceTotal = 0;
        _runTotalEssenceEarned = 0;
    }

    private void ResetRunStateForFreshStart()
    {
        _activeCardBuffs.Clear();
        _selectedSpecialCards.Clear();
        _blockedCards.Clear();
        ResetRunSummaryState();
        _runStartRealtime = Time.realtimeSinceStartup;
        ReapplyActiveCardBuffsToPlayer();
    }

    private void ApplyDefaultCardRulesIfMissing()
    {
        if (allAvailableCards == null || allAvailableCards.Length == 0)
        {
            return;
        }

        BuffCardData fleetFoot = TryGetCardByName("Fleet foot");
        BuffCardData windrunner = TryGetCardByName("Windrunner");
        BuffCardData berserkerTempo = TryGetCardByName("Berserker Tempo");

        EnsureMutualBlockPair(fleetFoot, windrunner);

        if (berserkerTempo != null && berserkerTempo.buffDurationStages <= 0)
        {
            berserkerTempo.buffDurationStages = 1;
        }
    }

    private BuffCardData TryGetCardByName(string cardName)
    {
        if (allAvailableCards == null || string.IsNullOrWhiteSpace(cardName))
        {
            return null;
        }

        for (int i = 0; i < allAvailableCards.Length; i++)
        {
            BuffCardData candidate = allAvailableCards[i];
            if (candidate != null && string.Equals(candidate.cardName, cardName, StringComparison.OrdinalIgnoreCase))
            {
                return candidate;
            }
        }

        return null;
    }

    private void EnsureMutualBlockPair(BuffCardData first, BuffCardData second)
    {
        if (first == null || second == null)
        {
            return;
        }

        first.isSpecialCard = true;
        second.isSpecialCard = true;
        first.blockedCards = EnsureCardInBlockList(first.blockedCards, second);
        second.blockedCards = EnsureCardInBlockList(second.blockedCards, first);
    }

    private BuffCardData[] EnsureCardInBlockList(BuffCardData[] existingList, BuffCardData cardToAdd)
    {
        if (cardToAdd == null)
        {
            return existingList ?? Array.Empty<BuffCardData>();
        }

        if (existingList != null)
        {
            for (int i = 0; i < existingList.Length; i++)
            {
                if (existingList[i] == cardToAdd)
                {
                    return existingList;
                }
            }
        }

        List<BuffCardData> combined = existingList != null
            ? new List<BuffCardData>(existingList.Where(card => card != null))
            : new List<BuffCardData>();
        combined.Add(cardToAdd);
        return combined.ToArray();
    }

    private void BindPlayerEvents()
    {
        EnsurePlayerManager();
        if (playerManager == null || _subscribedPlayerManager == playerManager)
        {
            return;
        }

        UnbindPlayerEvents();
        _subscribedPlayerManager = playerManager;
        _subscribedPlayerManager.SoulEssenceChanged += HandleSoulEssenceChanged;
    }

    private void UnbindPlayerEvents()
    {
        if (_subscribedPlayerManager == null)
        {
            return;
        }

        _subscribedPlayerManager.SoulEssenceChanged -= HandleSoulEssenceChanged;
        _subscribedPlayerManager = null;
    }

    private void HandleSoulEssenceChanged(int totalEssence, int amountAdded)
    {
        if (!_runSummaryInitialized)
        {
            return;
        }

        _runTotalEssenceEarned += Mathf.Max(0, amountAdded);
    }

    private void EnsurePlayerManager()
    {
        if (playerManager == null)
        {
            playerManager = FindFirstObjectByType<PlayerManager>();
        }
    }

    public bool TryGetRunRewardSummary(out RunRewardSummary summary)
    {
        if (!_runSummaryInitialized)
        {
            summary = default;
            return false;
        }

        float runtimeSeconds = Mathf.Max(0f, Time.realtimeSinceStartup - _runStartRealtime);
        float effectiveMultiplier = _runRawEssenceBeforeMultiplierTotal > 0
            ? (float)_runStageRewardEssenceTotal / _runRawEssenceBeforeMultiplierTotal
            : 1f;

        summary = new RunRewardSummary
        {
            runtimeSeconds = runtimeSeconds,
            floorReached = _runLastClearedFloor,
            stageReached = _runLastClearedStage,
            stagesCleared = _runTotalStagesCleared,
            enemiesDefeated = _runTotalEnemiesDefeated,
            essencePerKill = essencePerKill,
            totalEssenceEarned = _runTotalEssenceEarned,
            stageRewardEssence = _runStageRewardEssenceTotal,
            composureBonusEssence = _runComposureBonusEssenceTotal,
            rawBaseEssence = _runBaseEssenceComponentTotal,
            rawKillEssence = _runKillEssenceComponentTotal,
            rawFloorEssence = _runFloorEssenceComponentTotal,
            rawEssenceBeforeMultipliers = _runRawEssenceBeforeMultiplierTotal,
            effectiveCombinedMultiplier = effectiveMultiplier
        };

        return true;
    }

    private void StartFadeIn()
    {
        if (_fadeOutCoroutine != null)
        {
            StopCoroutine(_fadeOutCoroutine);
            _fadeOutCoroutine = null;
        }

        if (_fadeInCoroutine != null)
        {
            StopCoroutine(_fadeInCoroutine);
        }

        _fadeInCoroutine = StartCoroutine(FadeInUI());
    }

    private void StartFadeOut()
    {
        if (_fadeInCoroutine != null)
        {
            StopCoroutine(_fadeInCoroutine);
            _fadeInCoroutine = null;
        }

        if (_fadeOutCoroutine != null)
        {
            StopCoroutine(_fadeOutCoroutine);
        }

        _fadeOutCoroutine = StartCoroutine(FadeOutUI());
    }

    private void ForceCloseRewardScreenImmediate()
    {
        if (_fadeInCoroutine != null)
        {
            StopCoroutine(_fadeInCoroutine);
            _fadeInCoroutine = null;
        }

        if (_fadeOutCoroutine != null)
        {
            StopCoroutine(_fadeOutCoroutine);
            _fadeOutCoroutine = null;
        }

        if (rewardCanvasGroup != null)
        {
            rewardCanvasGroup.alpha = 0f;
            rewardCanvasGroup.interactable = false;
            rewardCanvasGroup.blocksRaycasts = false;
        }

        Time.timeScale = 1f;
        _rewardScreenOpen = false;
    }

    private void HandleSceneUnloaded(Scene scene)
    {
        // Reward UI should never leak across scene transitions.
        ForceCloseRewardScreenImmediate();
    }

    private void EnsureEventSystem()
    {
        EventSystem existing = FindFirstObjectByType<EventSystem>(FindObjectsInactive.Include);
        if (existing != null)
        {
            return;
        }

        GameObject eventSystemObject = new GameObject("EventSystem (Runtime)");
        eventSystemObject.transform.SetParent(transform, false);
        eventSystemObject.AddComponent<EventSystem>();

#if ENABLE_INPUT_SYSTEM
        eventSystemObject.AddComponent<InputSystemUIInputModule>();
#else
        eventSystemObject.AddComponent<StandaloneInputModule>();
#endif
    }
}
