using System;
using UnityEngine;

public enum EmotionDirectorStrategy
{
    CalmPressure,
    AggressionContainment
}

[Serializable]
public struct EmotionDirectorDirective
{
    public PlayerEmotionState sourceEmotion;
    public EmotionDirectorStrategy strategy;
    public float spawnMultiplier;
    public float enemySpeedMultiplier;
    public float enemyAttackCooldownMultiplier;
    public float enemyVisionMultiplier;
    public float attackOpeningDelay;
    public float chaseStandoffDistance;
    public float retreatDistance;
    public Color worldTint;
    public string explanation;
}

public class EmotionDirector : MonoBehaviour
{
    public static event Action<EmotionDirectorDirective> DirectiveChanged;

    private static EmotionDirector _instance;

    public static EmotionDirector Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = FindFirstObjectByType<EmotionDirector>();

                if (_instance == null)
                {
                    GameObject directorObject = new GameObject("Emotion Director");
                    _instance = directorObject.AddComponent<EmotionDirector>();
                }
            }

            return _instance;
        }
    }

    [Header("Calm Player Response")]
    [SerializeField] private float calmSpawnMultiplier = 1f;
    [SerializeField] private float calmEnemySpeedMultiplier = 1.2f;
    [SerializeField] private float calmEnemyAttackCooldownMultiplier = 0.75f;
    [SerializeField] private float calmEnemyVisionMultiplier = 0.95f;
    [SerializeField] private float calmAttackOpeningDelay = 0f;
    [SerializeField] private float calmStandoffDistance = 0f;
    [SerializeField] private float calmRetreatDistance = 0f;
    [SerializeField] private Color calmWorldTint = new Color(0.45f, 0.7f, 1f);

    [Header("Aggressive Player Response")]
    [SerializeField] private float aggressiveSpawnMultiplier = 1.35f;
    [SerializeField] private float aggressiveEnemySpeedMultiplier = 0.9f;
    [SerializeField] private float aggressiveEnemyAttackCooldownMultiplier = 1.25f;
    [SerializeField] private float aggressiveEnemyVisionMultiplier = 1.15f;
    [SerializeField] private float aggressiveAttackOpeningDelay = 0.35f;
    [SerializeField] private float aggressiveStandoffDistance = 4f;
    [SerializeField] private float aggressiveRetreatDistance = 2.25f;
    [SerializeField] private Color aggressiveWorldTint = new Color(1f, 0.45f, 0.35f);

    [Header("Debug")]
    [SerializeField] private bool logDirectorDecisions = true;

    public EmotionDirectorDirective CurrentDirective { get; private set; }

    private void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }

        _instance = this;
        DontDestroyOnLoad(gameObject);
        RefreshDirective(EmotionEngine.Instance.CurrentEmotion, "startup");
    }

    private void OnEnable()
    {
        EmotionEngine.EmotionChanged += HandleEmotionChanged;
        EmotionEngine.RoomEvaluated += HandleRoomEvaluated;
    }

    private void OnDisable()
    {
        EmotionEngine.EmotionChanged -= HandleEmotionChanged;
        EmotionEngine.RoomEvaluated -= HandleRoomEvaluated;
    }

    private void OnDestroy()
    {
        if (_instance == this)
        {
            _instance = null;
        }
    }

    public int GetRecommendedSpawnCount(int baseSpawnCount)
    {
        if (baseSpawnCount <= 0)
        {
            return 0;
        }

        return Mathf.Max(1, Mathf.CeilToInt(baseSpawnCount * CurrentDirective.spawnMultiplier));
    }

    private void HandleEmotionChanged(PlayerEmotionState emotionState, EmotionProfileSnapshot snapshot)
    {
        RefreshDirective(emotionState, "emotion changed");
    }

    private void HandleRoomEvaluated(EmotionRoomReport report)
    {
        RefreshDirective(report.emotionAfter, $"room {report.roomNumber} evaluated");
    }

    private void RefreshDirective(PlayerEmotionState emotionState, string reason)
    {
        CurrentDirective = BuildDirective(emotionState);
        DirectiveChanged?.Invoke(CurrentDirective);

        if (logDirectorDecisions)
        {
            Debug.Log($"Emotion Director: {CurrentDirective.strategy} because {reason}. {CurrentDirective.explanation}");
        }
    }

    private EmotionDirectorDirective BuildDirective(PlayerEmotionState emotionState)
    {
        if (emotionState == PlayerEmotionState.Aggressive)
        {
            return new EmotionDirectorDirective
            {
                sourceEmotion = emotionState,
                strategy = EmotionDirectorStrategy.AggressionContainment,
                spawnMultiplier = aggressiveSpawnMultiplier,
                enemySpeedMultiplier = aggressiveEnemySpeedMultiplier,
                enemyAttackCooldownMultiplier = aggressiveEnemyAttackCooldownMultiplier,
                enemyVisionMultiplier = aggressiveEnemyVisionMultiplier,
                attackOpeningDelay = aggressiveAttackOpeningDelay,
                chaseStandoffDistance = aggressiveStandoffDistance,
                retreatDistance = aggressiveRetreatDistance,
                worldTint = aggressiveWorldTint,
                explanation = "Player is forceful, so the game adds combat pressure while enemies hesitate, hold distance, and punish reckless approaches."
            };
        }

        return new EmotionDirectorDirective
        {
            sourceEmotion = emotionState,
            strategy = EmotionDirectorStrategy.CalmPressure,
            spawnMultiplier = calmSpawnMultiplier,
            enemySpeedMultiplier = calmEnemySpeedMultiplier,
            enemyAttackCooldownMultiplier = calmEnemyAttackCooldownMultiplier,
            enemyVisionMultiplier = calmEnemyVisionMultiplier,
            attackOpeningDelay = calmAttackOpeningDelay,
            chaseStandoffDistance = calmStandoffDistance,
            retreatDistance = calmRetreatDistance,
            worldTint = calmWorldTint,
            explanation = "Player is controlled, so enemies rush more directly to force a reaction."
        };
    }
}
