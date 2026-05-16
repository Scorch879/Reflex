using System;
using System.Collections.Generic;
using System.Linq;
using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

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
    public float levelMultiplier;

    public string LevelStageLabel => levelNumber + "-" + stageNumber;
}

public class RewardManager : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private PlayerManager playerManager;
    [SerializeField] private CanvasGroup rewardCanvasGroup; // For fading in/out the reward UI
    [SerializeField] private BuffCardUI[] cardUI; // 3 UI slots for the buff cards
    [Header("Settings")]
    [SerializeField] private float fadeInDuration = 0.5f;
    [SerializeField] private float fadeOutDuration = 0.5f;
    [SerializeField] private bool openCardRewardsOnLevelClear = true;

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

    public LevelRewardContext LastRewardContext { get; private set; }

    private int _killsThisLevel;
    private bool _rewardScreenOpen;

    private void OnEnable()
    {
        LevelRunManager.LevelEntered += HandleLevelEntered;
        LevelRunManager.LevelCleared += HandleLevelCleared;
        EnemyController.EnemyDefeated += HandleEnemyDefeated;
        EmotionEngine.RoomEvaluated += HandleRoomEvaluated;
    }

    private void OnDisable()
    {
        LevelRunManager.LevelEntered -= HandleLevelEntered;
        LevelRunManager.LevelCleared -= HandleLevelCleared;
        EnemyController.EnemyDefeated -= HandleEnemyDefeated;
        EmotionEngine.RoomEvaluated -= HandleRoomEvaluated;
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
        if (_rewardScreenOpen || rewardCanvasGroup == null || cardUI == null || cardUI.Length == 0)
        {
            return;
        }

        _rewardScreenOpen = true;
        StartCoroutine(FadeInUI());

        foreach (var card in cardUI)
        {
            if (card != null)
            {
                card.ClearBuffText();
            }
        }

        // Pick 3 unique random cards
        var choices = allAvailableCards != null
            ? allAvailableCards.Where(card => card != null).OrderBy(x => UnityEngine.Random.value).Take(cardUI.Length).ToList()
            : new List<BuffCardData>();

        // assign each card to a socket
        for (int i = 0; i < choices.Count; i++)
        {
            if (cardUI[i] != null)
            {
                cardUI[i].Setup(choices[i]);
            }
        }
    }

    private IEnumerator FadeInUI()
    {
        float duration = fadeInDuration;
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
    }

    private IEnumerator FadeOutUI()
    {
        float duration = fadeOutDuration;
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
            StartCoroutine(FadeOutUI());
            return;
        }

        // Apply the additive bonuses to PlayerManager
        playerManager.cardAtkBonus += card.atkBonus;
        playerManager.cardCritChance += card.critBonus;
        playerManager.cardEssenceMult += card.essenceBonus;
        playerManager.cardVampChance += card.vampiricBonus;
        playerManager.cardComboWindowBonus += card.comboWindowBonus;

        // Fleet Foot (Dash) bonuses[cite: 1]
        playerManager.cardDashCDReduction += card.dashCDReduction;
        playerManager.cardDashDistanceBonus += card.dashDistanceBonus;

        if (card.isGlassCannon) playerManager.ApplyGlassCannon();

        StartCoroutine(FadeOutUI());
    }

    private void HandleLevelEntered(int nodeId, int floorDepth, string sceneName)
    {
        _killsThisLevel = 0;
    }

    private void HandleEnemyDefeated(EnemyController enemy)
    {
        _killsThisLevel++;
    }

    private void HandleLevelCleared(int nodeId, int floorDepth, string sceneName)
    {
        if (floorDepth <= 0)
        {
            return;
        }

        EnsurePlayerManager();

        LevelRewardContext context = BuildRewardContext(nodeId, floorDepth, sceneName);
        LastRewardContext = context;

        if (playerManager != null)
        {
            playerManager.AddSoulEssence(context.soulEssenceAwarded);
        }

        LevelRewardGranted?.Invoke(context);

        if (openCardRewardsOnLevelClear)
        {
            OpenRewardScreen();
        }
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

        playerManager.AddSoulEssence(bonus);

        if (InGameUIManager.Instance != null)
        {
            InGameUIManager.Instance.ShowStatusMessage($"+{bonus} Soul Essence (Composure)", composureMessageColor);
        }

        Debug.Log($"Composure reward: +{bonus} Soul Essence (room {report.roomNumber}, score {report.scoreAfter:0.00}, damage {report.damageTaken:0.0}).");
    }

    private LevelRewardContext BuildRewardContext(int nodeId, int floorDepth, string sceneName)
    {
        int levelNumber = ((floorDepth - 1) / stagesPerLevel) + 1;
        int stageNumber = ((floorDepth - 1) % stagesPerLevel) + 1;
        float levelMultiplier = 1f + ((levelNumber - 1) * levelRewardMultiplierStep);
        int rawEssence = baseEssencePerClear + (_killsThisLevel * essencePerKill) + (floorDepth * essencePerFloor);
        int essenceAwarded = Mathf.Max(0, Mathf.RoundToInt(rawEssence * levelMultiplier * GetPlayerEssenceMultiplier()));

        return new LevelRewardContext
        {
            nodeId = nodeId,
            floorDepth = floorDepth,
            levelNumber = levelNumber,
            stageNumber = stageNumber,
            sceneName = sceneName,
            kills = _killsThisLevel,
            soulEssenceAwarded = essenceAwarded,
            levelMultiplier = levelMultiplier
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

    private void EnsurePlayerManager()
    {
        if (playerManager == null)
        {
            playerManager = FindFirstObjectByType<PlayerManager>();
        }
    }
}
