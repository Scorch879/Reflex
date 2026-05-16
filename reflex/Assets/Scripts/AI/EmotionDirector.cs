using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

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
    public float aggressionBlend;
    public float confidence;
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
    [SerializeField, Range(0f, 1f)] private float profileUpdateLogBlendDelta = 0.1f;

    [Header("Adaptive Blend")]
    [SerializeField] private bool useContinuousBlend = true;
    [SerializeField, Range(0f, 1f)] private float confidenceBlendFloor = 0.3f;

    [Header("Calm Relief Rewards")]
    [SerializeField] private bool enableCalmReliefRewards = true;
    [SerializeField, Min(0)] private int maxCalmReliefCharges = 2;
    [SerializeField, Range(0f, 1f)] private float calmRewardScoreThreshold = 0.42f;
    [SerializeField, Min(0f)] private float calmRewardMaxDamageTaken = 14f;
    [SerializeField, Min(0)] private int calmRewardMaxDeaths = 0;
    [SerializeField, Min(0)] private int calmRewardMinAttacks = 4;
    [SerializeField, Range(0.1f, 2f)] private float calmReliefSpawnMultiplier = 0.82f;
    [SerializeField, Range(0.1f, 2f)] private float calmReliefEnemySpeedMultiplier = 0.92f;
    [SerializeField, Range(0.1f, 2f)] private float calmReliefEnemyAttackCooldownMultiplier = 1.15f;
    [SerializeField, Range(0.1f, 2f)] private float calmReliefEnemyVisionMultiplier = 0.9f;
    [SerializeField, Min(0f)] private float calmReliefAttackOpeningDelayBonus = 0.15f;

    [Header("World Tint")]
    [SerializeField] private bool applyWorldTint = true;
    [SerializeField, Range(0f, 1f)] private float ambientTintStrength = 0.2f;
    [SerializeField, Range(0f, 1f)] private float cameraTintStrength = 0.16f;

    public EmotionDirectorDirective CurrentDirective { get; private set; }
    public int PendingCalmReliefCharges => _pendingCalmReliefCharges;
    public bool IsCalmReliefActive => _calmReliefActive;
    private PlayerEmotionState _lastLoggedEmotion;
    private float _lastLoggedBlend = -1f;
    private bool _hasBaselineAmbientColor;
    private Color _baselineAmbientColor;
    private readonly Dictionary<int, Color> _baselineCameraColors = new Dictionary<int, Color>();
    private int _pendingCalmReliefCharges;
    private bool _calmReliefActive;

    private void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }

        _instance = this;
        DontDestroyOnLoad(gameObject);
        CacheVisualBaselines();
        RefreshDirective(EmotionEngine.Instance.CurrentEmotion, EmotionEngine.Instance.CurrentSnapshot, "startup");
    }

    private void OnEnable()
    {
        EmotionEngine.EmotionChanged += HandleEmotionChanged;
        EmotionEngine.EmotionProfileUpdated += HandleEmotionProfileUpdated;
        EmotionEngine.RoomStarted += HandleRoomStarted;
        EmotionEngine.RoomEvaluated += HandleRoomEvaluated;
        SceneManager.sceneLoaded += HandleSceneLoaded;
    }

    private void OnDisable()
    {
        EmotionEngine.EmotionChanged -= HandleEmotionChanged;
        EmotionEngine.EmotionProfileUpdated -= HandleEmotionProfileUpdated;
        EmotionEngine.RoomStarted -= HandleRoomStarted;
        EmotionEngine.RoomEvaluated -= HandleRoomEvaluated;
        SceneManager.sceneLoaded -= HandleSceneLoaded;
        RestoreVisualBaselines();
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
        RefreshDirective(emotionState, snapshot, "emotion changed");
    }

    private void HandleEmotionProfileUpdated(EmotionProfileSnapshot snapshot)
    {
        RefreshDirective(snapshot.state, snapshot, "profile updated");
    }

    private void HandleRoomEvaluated(EmotionRoomReport report)
    {
        _calmReliefActive = false;

        if (enableCalmReliefRewards && IsCalmRewardEligible(report))
        {
            _pendingCalmReliefCharges = Mathf.Min(maxCalmReliefCharges, _pendingCalmReliefCharges + 1);

            if (logDirectorDecisions)
            {
                Debug.Log($"Emotion Director: calm reward earned in room {report.roomNumber}. Charges: {_pendingCalmReliefCharges}.");
            }
        }

        RefreshDirective(report.emotionAfter, EmotionEngine.Instance.CurrentSnapshot, $"room {report.roomNumber} evaluated");
    }

    private void HandleRoomStarted(EmotionRoomStartReport report)
    {
        if (!enableCalmReliefRewards)
        {
            _calmReliefActive = false;
            return;
        }

        if (_pendingCalmReliefCharges > 0)
        {
            _pendingCalmReliefCharges--;
            _calmReliefActive = true;

            if (logDirectorDecisions)
            {
                Debug.Log($"Emotion Director: calm relief active for room {report.roomNumber}. Remaining charges: {_pendingCalmReliefCharges}.");
            }
        }
        else
        {
            _calmReliefActive = false;
        }

        RefreshDirective(report.emotionState, EmotionEngine.Instance.CurrentSnapshot, $"room {report.roomNumber} started");
    }

    private void RefreshDirective(PlayerEmotionState emotionState, EmotionProfileSnapshot snapshot, string reason)
    {
        CurrentDirective = BuildDirective(emotionState, snapshot);
        ApplyWorldTint(CurrentDirective);
        DirectiveChanged?.Invoke(CurrentDirective);

        if (logDirectorDecisions && ShouldLogDirective(reason, CurrentDirective))
        {
            Debug.Log($"Emotion Director: {CurrentDirective.strategy} because {reason}. blend={CurrentDirective.aggressionBlend:0.00}, confidence={CurrentDirective.confidence:0.00}. {CurrentDirective.explanation}");
            _lastLoggedEmotion = CurrentDirective.sourceEmotion;
            _lastLoggedBlend = CurrentDirective.aggressionBlend;
        }
    }

    private bool ShouldLogDirective(string reason, EmotionDirectorDirective directive)
    {
        if (!string.Equals(reason, "profile updated", StringComparison.Ordinal))
        {
            return true;
        }

        if (_lastLoggedBlend < 0f)
        {
            return true;
        }

        if (_lastLoggedEmotion != directive.sourceEmotion)
        {
            return true;
        }

        return Mathf.Abs(directive.aggressionBlend - _lastLoggedBlend) >= profileUpdateLogBlendDelta;
    }

    private EmotionDirectorDirective BuildDirective(PlayerEmotionState emotionState, EmotionProfileSnapshot snapshot)
    {
        float blend = ComputeAggressionBlend(snapshot);
        EmotionDirectorStrategy strategy = emotionState == PlayerEmotionState.Aggressive
            ? EmotionDirectorStrategy.AggressionContainment
            : EmotionDirectorStrategy.CalmPressure;

        string explanation = strategy == EmotionDirectorStrategy.AggressionContainment
            ? "Player is forceful, so the game applies containment pressure with safer enemy spacing and controlled tempo."
            : "Player is controlled, so enemies press faster to force higher commitment.";

        float spawnMultiplier = Mathf.Lerp(calmSpawnMultiplier, aggressiveSpawnMultiplier, blend);
        float enemySpeedMultiplier = Mathf.Lerp(calmEnemySpeedMultiplier, aggressiveEnemySpeedMultiplier, blend);
        float enemyAttackCooldownMultiplier = Mathf.Lerp(calmEnemyAttackCooldownMultiplier, aggressiveEnemyAttackCooldownMultiplier, blend);
        float enemyVisionMultiplier = Mathf.Lerp(calmEnemyVisionMultiplier, aggressiveEnemyVisionMultiplier, blend);
        float attackOpeningDelay = Mathf.Lerp(calmAttackOpeningDelay, aggressiveAttackOpeningDelay, blend);

        if (_calmReliefActive)
        {
            spawnMultiplier *= calmReliefSpawnMultiplier;
            enemySpeedMultiplier *= calmReliefEnemySpeedMultiplier;
            enemyAttackCooldownMultiplier *= calmReliefEnemyAttackCooldownMultiplier;
            enemyVisionMultiplier *= calmReliefEnemyVisionMultiplier;
            attackOpeningDelay += calmReliefAttackOpeningDelayBonus;
        }

        return new EmotionDirectorDirective
        {
            sourceEmotion = emotionState,
            strategy = strategy,
            aggressionBlend = blend,
            confidence = Mathf.Clamp01(snapshot.confidence),
            spawnMultiplier = Mathf.Max(0.2f, spawnMultiplier),
            enemySpeedMultiplier = Mathf.Max(0.2f, enemySpeedMultiplier),
            enemyAttackCooldownMultiplier = Mathf.Max(0.2f, enemyAttackCooldownMultiplier),
            enemyVisionMultiplier = Mathf.Max(0.2f, enemyVisionMultiplier),
            attackOpeningDelay = Mathf.Max(0f, attackOpeningDelay),
            chaseStandoffDistance = Mathf.Lerp(calmStandoffDistance, aggressiveStandoffDistance, blend),
            retreatDistance = Mathf.Lerp(calmRetreatDistance, aggressiveRetreatDistance, blend),
            worldTint = Color.Lerp(calmWorldTint, aggressiveWorldTint, blend),
            explanation = _calmReliefActive
                ? explanation + " Calm mastery reward active: lighter enemy pressure this room."
                : explanation
        };
    }

    private bool IsCalmRewardEligible(EmotionRoomReport report)
    {
        if (report.scoreAfter > calmRewardScoreThreshold)
        {
            return false;
        }

        if (report.damageTaken > calmRewardMaxDamageTaken)
        {
            return false;
        }

        if (report.deathCount > calmRewardMaxDeaths)
        {
            return false;
        }

        if (report.attacksPerformed < calmRewardMinAttacks)
        {
            return false;
        }

        return true;
    }

    private float ComputeAggressionBlend(EmotionProfileSnapshot snapshot)
    {
        float scoreBlend = Mathf.Clamp01(snapshot.aggressionScore);
        if (!useContinuousBlend)
        {
            return scoreBlend >= 0.5f ? 1f : 0f;
        }

        float confidenceInfluence = Mathf.Lerp(confidenceBlendFloor, 1f, Mathf.Clamp01(snapshot.confidence));
        return Mathf.Lerp(0.5f, scoreBlend, confidenceInfluence);
    }

    private void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        CacheVisualBaselines();
        ApplyWorldTint(CurrentDirective);
    }

    private void CacheVisualBaselines()
    {
        _baselineAmbientColor = RenderSettings.ambientLight;
        _hasBaselineAmbientColor = true;
        _baselineCameraColors.Clear();

        Camera[] cameras = Camera.allCameras;
        for (int cameraIndex = 0; cameraIndex < cameras.Length; cameraIndex++)
        {
            Camera camera = cameras[cameraIndex];
            if (camera != null)
            {
                _baselineCameraColors[camera.GetInstanceID()] = camera.backgroundColor;
            }
        }
    }

    private void RestoreVisualBaselines()
    {
        if (_hasBaselineAmbientColor)
        {
            RenderSettings.ambientLight = _baselineAmbientColor;
        }

        Camera[] cameras = Camera.allCameras;
        for (int cameraIndex = 0; cameraIndex < cameras.Length; cameraIndex++)
        {
            Camera camera = cameras[cameraIndex];
            if (camera == null)
            {
                continue;
            }

            if (_baselineCameraColors.TryGetValue(camera.GetInstanceID(), out Color baselineColor))
            {
                camera.backgroundColor = baselineColor;
            }
        }
    }

    private void ApplyWorldTint(EmotionDirectorDirective directive)
    {
        if (!applyWorldTint)
        {
            RestoreVisualBaselines();
            return;
        }

        if (!_hasBaselineAmbientColor)
        {
            CacheVisualBaselines();
        }

        RenderSettings.ambientLight = Color.Lerp(_baselineAmbientColor, directive.worldTint, ambientTintStrength);

        Camera[] cameras = Camera.allCameras;
        for (int cameraIndex = 0; cameraIndex < cameras.Length; cameraIndex++)
        {
            Camera camera = cameras[cameraIndex];
            if (camera == null)
            {
                continue;
            }

            int cameraId = camera.GetInstanceID();
            if (!_baselineCameraColors.TryGetValue(cameraId, out Color baselineColor))
            {
                baselineColor = camera.backgroundColor;
                _baselineCameraColors[cameraId] = baselineColor;
            }

            camera.backgroundColor = Color.Lerp(baselineColor, directive.worldTint, cameraTintStrength);
        }
    }
}
