using System;
using System.Collections.Generic;
using UnityEngine;

public enum PlayerEmotionState
{
    Calm,
    Aggressive
}

[Serializable]
public struct EmotionProfileSnapshot
{
    public PlayerEmotionState state;
    public float aggressionScore;
    public float recentAggressionScore;
    public float confidence;
    public float damagePressureScore;
    public float combatIntentScore;
    public float movementPressureScore;
    public float timePressureScore;
    public float damageTaken;
    public int deathCount;
    public int enemiesEncountered;
    public int attacksPerformed;
    public int enemyHits;
    public float effectiveEnemyHits;
    public float timeRunning;
    public float timeIdle;
    public float averageMovementSpeed;
    public int activeSpawnerCount;
    public float currentRoomTime;
    public float lastRoomClearTime;
}

[Serializable]
public struct EmotionRoomReport
{
    public int roomNumber;
    public PlayerEmotionState emotionBefore;
    public PlayerEmotionState emotionAfter;
    public float scoreBefore;
    public float scoreAfter;
    public float duration;
    public int spawnerCount;
    public int baseSpawnCount;
    public int adjustedSpawnCount;
    public float damageTaken;
    public int deathCount;
    public int enemiesEncountered;
    public int attacksPerformed;
    public int enemyHits;
    public float effectiveEnemyHits;
    public float timeRunning;
    public float timeIdle;
    public float averageMovementSpeed;
}

[Serializable]
public struct EmotionRoomStartReport
{
    public int roomNumber;
    public int activeSpawnerCount;
    public PlayerEmotionState emotionState;
    public float aggressionScore;
    public float confidence;
}

public class EmotionEngine : MonoBehaviour
{
    public static event Action<PlayerEmotionState, EmotionProfileSnapshot> EmotionChanged;
    public static event Action<EmotionProfileSnapshot> EmotionProfileUpdated;
    public static event Action<EmotionRoomStartReport> RoomStarted;
    public static event Action<EmotionRoomReport> RoomEvaluated;

    private sealed class ActiveRoomContributor
    {
        public string name;
        public int baseSpawnCount;
        public int adjustedSpawnCount;
    }

    private static EmotionEngine _instance;

    public static bool HasInstance => _instance != null;

    public static EmotionEngine Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = FindFirstObjectByType<EmotionEngine>();

                if (_instance == null)
                {
                    GameObject emotionEngineObject = new GameObject("Emotion Engine");
                    _instance = emotionEngineObject.AddComponent<EmotionEngine>();
                }
            }

            return _instance;
        }
    }

    [Header("Emotion State")]
    [SerializeField] private PlayerEmotionState startingEmotion = PlayerEmotionState.Calm;
    [SerializeField, Range(0f, 1f)] private float aggressiveThreshold = 0.58f;
    [SerializeField, Range(0f, 1f)] private float calmThreshold = 0.42f;
    [SerializeField, Range(0f, 1f)] private float scoreSmoothing = 0.35f;
    [SerializeField, Range(0f, 1f)] private float aggressionRiseSmoothing = 0.2f;
    [SerializeField, Range(0f, 1f)] private float aggressionFallSmoothing = 0.55f;
    [SerializeField] private float evaluationInterval = 1f;
    [SerializeField] private bool logEmotionChanges = true;

    [Header("Aggression Tempo")]
    [SerializeField, Min(0f)] private float calmDecayDelay = 0.9f;
    [SerializeField, Range(0f, 0.25f)] private float calmDecayPerSecond = 0.07f;
    [SerializeField, Range(0.1f, 1f)] private float attackIntentScale = 0.75f;
    [SerializeField, Range(0.1f, 1f)] private float hitIntentScale = 0.7f;

    [Header("Expected Values")]
    [SerializeField] private float expectedDamageTaken = 50f;
    [SerializeField] private float expectedEnemyEncounters = 8f;
    [SerializeField] private float expectedAttacks = 20f;
    [SerializeField] private float expectedAverageMovementSpeed = 5f;
    [SerializeField] private float expectedRoomClearTime = 120f;
    [SerializeField] private float expectedDeaths = 2f;

    [Header("Recent Behavior Tuning")]
    [SerializeField, Range(0f, 1f)] private float recentBehaviorWeight = 0.6f;
    [SerializeField] private float expectedRoomDamageTaken = 20f;
    [SerializeField] private float expectedRoomEnemyEncounters = 4f;
    [SerializeField] private float expectedRoomAttacks = 10f;
    [SerializeField] private float expectedRoomMovementSpeed = 4f;
    [SerializeField] private float expectedRoomDeaths = 1f;
    [SerializeField] private float minimumEvidenceForChange = 0.25f;

    [Header("Adaptive Spawning")]
    [SerializeField] private float aggressiveSpawnMultiplier = 1.35f;
    [SerializeField] private float calmSpawnMultiplier = 0.85f;

    [Header("Aggression Anti-Spike")]
    [SerializeField] private bool useMultiHitDiminishingReturns = true;
    [SerializeField, Min(0.05f)] private float multiHitBurstWindow = 0.45f;
    [SerializeField, Min(0f)] private float additionalHitFalloff = 0.85f;
    [SerializeField, Min(0.2f)] private float maxEffectiveHitsPerAttack = 1.6f;

    [Header("Debug")]
    [SerializeField] private bool createDebugHud = true;

    public PlayerEmotionState CurrentEmotion { get; private set; }
    public float AggressionScore { get; private set; }
    public float RecentAggressionScore { get; private set; }
    public float Confidence { get; private set; }
    public EmotionProfileSnapshot CurrentSnapshot => BuildSnapshot();
    public EmotionRoomReport LastRoomReport { get; private set; }
    public bool IsRoomActive => _activeRoomContributors.Count > 0;
    public int ActiveSpawnerCount => _activeRoomContributors.Count;

    private readonly HashSet<int> _encounteredEnemyIds = new HashSet<int>();
    private readonly Dictionary<int, ActiveRoomContributor> _activeRoomContributors = new Dictionary<int, ActiveRoomContributor>();
    private float _damageTaken;
    private int _deathCount;
    private int _attacksPerformed;
    private int _enemyHits;
    private float _effectiveEnemyHits;
    private float _timeRunning;
    private float _timeIdle;
    private float _movementSpeedTotal;
    private int _movementSamples;
    private float _currentRoomTime;
    private float _lastRoomClearTime;
    private bool _roomTimerRunning;
    private float _evaluationTimer;
    private int _roomsCleared;
    private int _currentRoomBaseSpawnCount;
    private int _currentRoomAdjustedSpawnCount;
    private int _currentRoomSpawnerCount;
    private int _nextAnonymousRoomId = -1;
    private EmotionProfileSnapshot _roomStartSnapshot;
    private PlayerEmotionState _roomStartEmotion;
    private float _roomStartScore;
    private float _roomStartMovementSpeedTotal;
    private int _roomStartMovementSamples;
    private float _damagePressureScore;
    private float _combatIntentScore;
    private float _movementPressureScore;
    private float _timePressureScore;
    private float _lastCombatIntentTime;
    private float _lastEmotionEvaluationTime;
    private int _hitsInCurrentAttack;
    private float _effectiveHitsInCurrentAttack;
    private float _lastAttackStartedTime;
    private float _lastEnemyHitTime;

    private void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }

        _instance = this;
        DontDestroyOnLoad(gameObject);

        CurrentEmotion = startingEmotion;
        AggressionScore = startingEmotion == PlayerEmotionState.Aggressive ? aggressiveThreshold : calmThreshold;
        RecentAggressionScore = AggressionScore;
        _evaluationTimer = evaluationInterval;
        _lastCombatIntentTime = Time.time;
        _lastEmotionEvaluationTime = Time.time;

        if (createDebugHud)
        {
            EmotionDebugHUD.EnsureExists();
        }

        _ = EmotionDirector.Instance;
    }

    private void Update()
    {
        if (_roomTimerRunning)
        {
            _currentRoomTime += Time.deltaTime;
        }

        _evaluationTimer -= Time.deltaTime;
        if (_evaluationTimer <= 0f)
        {
            _evaluationTimer = evaluationInterval;
            EvaluateEmotion(false);
        }
    }

    private void OnDestroy()
    {
        if (_instance == this)
        {
            _instance = null;
        }
    }

    public void BeginRoom()
    {
        BeginRoom(0, 0);
    }

    public int BeginRoom(int baseSpawnCount, int adjustedSpawnCount)
    {
        return BeginRoom(CreateAnonymousRoomId(), "Room", baseSpawnCount, adjustedSpawnCount);
    }

    public int BeginRoom(UnityEngine.Object source, int baseSpawnCount, int adjustedSpawnCount)
    {
        if (source == null)
        {
            return BeginRoom(baseSpawnCount, adjustedSpawnCount);
        }

        return BeginRoom(source.GetInstanceID(), source.name, baseSpawnCount, adjustedSpawnCount);
    }

    public void RecordRoomCleared()
    {
        if (_activeRoomContributors.Count == 0)
        {
            return;
        }

        if (_activeRoomContributors.Count > 1)
        {
            Debug.LogWarning("EmotionEngine.RecordRoomCleared() was called without a source while multiple spawners are active. Use RecordRoomCleared(source) so the correct wave can be cleared.");
            return;
        }

        int onlySourceId = 0;
        foreach (int sourceId in _activeRoomContributors.Keys)
        {
            onlySourceId = sourceId;
            break;
        }

        RecordRoomCleared(onlySourceId);
    }

    public void RecordRoomCleared(UnityEngine.Object source)
    {
        if (source == null)
        {
            RecordRoomCleared();
            return;
        }

        RecordRoomCleared(source.GetInstanceID());
    }

    public void RecordDamageTaken(float amount)
    {
        if (amount <= 0f)
        {
            return;
        }

        _damageTaken += amount;
        _lastCombatIntentTime = Time.time;
        EvaluateEmotion(false);
    }

    public void RecordDeath()
    {
        _deathCount++;
        _lastCombatIntentTime = Time.time;
        EvaluateEmotion(true);
    }

    public void RecordEnemyEncounter(EnemyController enemy)
    {
        if (enemy == null)
        {
            return;
        }

        if (_encounteredEnemyIds.Add(enemy.GetInstanceID()))
        {
            EvaluateEmotion(false);
        }
    }

    public void RecordAttackStarted()
    {
        _attacksPerformed++;
        _lastCombatIntentTime = Time.time;
        _lastAttackStartedTime = Time.time;
        _hitsInCurrentAttack = 0;
        _effectiveHitsInCurrentAttack = 0f;
        EvaluateEmotion(false);
    }

    public void RecordEnemyHit(float damage)
    {
        if (damage <= 0f)
        {
            return;
        }

        _enemyHits++;
        _lastCombatIntentTime = Time.time;
        _effectiveEnemyHits += CalculateEffectiveHitContribution();
        _lastEnemyHitTime = Time.time;
        EvaluateEmotion(false);
    }

    public void RecordMovement(float speed, bool isMoving, bool isIdle)
    {
        float deltaTime = Time.deltaTime;

        if (isMoving)
        {
            _timeRunning += deltaTime;
        }

        if (isIdle)
        {
            _timeIdle += deltaTime;
        }

        _movementSpeedTotal += Mathf.Max(0f, speed);
        _movementSamples++;
    }

    public int GetRecommendedSpawnCount(int baseSpawnCount)
    {
        if (baseSpawnCount <= 0)
        {
            return 0;
        }

        if (CurrentEmotion == PlayerEmotionState.Aggressive)
        {
            return Mathf.Max(1, Mathf.CeilToInt(baseSpawnCount * aggressiveSpawnMultiplier));
        }

        return Mathf.Max(1, Mathf.RoundToInt(baseSpawnCount * calmSpawnMultiplier));
    }

    public void ResetProfile()
    {
        _encounteredEnemyIds.Clear();
        _damageTaken = 0f;
        _deathCount = 0;
        _attacksPerformed = 0;
        _enemyHits = 0;
        _effectiveEnemyHits = 0f;
        _timeRunning = 0f;
        _timeIdle = 0f;
        _movementSpeedTotal = 0f;
        _movementSamples = 0;
        _currentRoomTime = 0f;
        _lastRoomClearTime = 0f;
        _roomTimerRunning = false;
        _activeRoomContributors.Clear();
        _roomsCleared = 0;
        _currentRoomBaseSpawnCount = 0;
        _currentRoomAdjustedSpawnCount = 0;
        _currentRoomSpawnerCount = 0;
        LastRoomReport = default;
        CurrentEmotion = startingEmotion;
        AggressionScore = startingEmotion == PlayerEmotionState.Aggressive ? aggressiveThreshold : calmThreshold;
        RecentAggressionScore = AggressionScore;
        Confidence = 0f;
        _damagePressureScore = 0f;
        _combatIntentScore = 0f;
        _movementPressureScore = 0f;
        _timePressureScore = 0f;
        _lastCombatIntentTime = Time.time;
        _lastEmotionEvaluationTime = Time.time;
        _hitsInCurrentAttack = 0;
        _effectiveHitsInCurrentAttack = 0f;
        _lastAttackStartedTime = 0f;
        _lastEnemyHitTime = 0f;
        EmotionChanged?.Invoke(CurrentEmotion, BuildSnapshot());
    }

    private int BeginRoom(int sourceId, string sourceName, int baseSpawnCount, int adjustedSpawnCount)
    {
        if (_activeRoomContributors.Count == 0)
        {
            StartRoomWindow();
        }

        int sanitizedBaseCount = Mathf.Max(0, baseSpawnCount);
        int sanitizedAdjustedCount = Mathf.Max(0, adjustedSpawnCount);

        if (_activeRoomContributors.TryGetValue(sourceId, out ActiveRoomContributor existingContributor))
        {
            _currentRoomBaseSpawnCount -= existingContributor.baseSpawnCount;
            _currentRoomAdjustedSpawnCount -= existingContributor.adjustedSpawnCount;
        }
        else
        {
            _currentRoomSpawnerCount++;
        }

        _activeRoomContributors[sourceId] = new ActiveRoomContributor
        {
            name = string.IsNullOrWhiteSpace(sourceName) ? "Spawner" : sourceName,
            baseSpawnCount = sanitizedBaseCount,
            adjustedSpawnCount = sanitizedAdjustedCount
        };

        _currentRoomBaseSpawnCount += sanitizedBaseCount;
        _currentRoomAdjustedSpawnCount += sanitizedAdjustedCount;

        return sourceId;
    }

    private void StartRoomWindow()
    {
        _currentRoomTime = 0f;
        _roomTimerRunning = true;
        _currentRoomBaseSpawnCount = 0;
        _currentRoomAdjustedSpawnCount = 0;
        _currentRoomSpawnerCount = 0;
        _roomStartSnapshot = BuildSnapshot();
        _roomStartEmotion = CurrentEmotion;
        _roomStartScore = AggressionScore;
        _roomStartMovementSpeedTotal = _movementSpeedTotal;
        _roomStartMovementSamples = _movementSamples;

        RoomStarted?.Invoke(new EmotionRoomStartReport
        {
            roomNumber = _roomsCleared + 1,
            activeSpawnerCount = _activeRoomContributors.Count,
            emotionState = CurrentEmotion,
            aggressionScore = AggressionScore,
            confidence = Confidence
        });
    }

    private void RecordRoomCleared(int sourceId)
    {
        if (!_activeRoomContributors.Remove(sourceId))
        {
            return;
        }

        if (_activeRoomContributors.Count > 0)
        {
            return;
        }

        CompleteRoomWindow();
    }

    private void CompleteRoomWindow()
    {
        if (!_roomTimerRunning)
        {
            return;
        }

        float clearDuration = _currentRoomTime;
        _lastRoomClearTime = clearDuration;
        _roomTimerRunning = false;
        EvaluateEmotion(true);

        _roomsCleared++;
        LastRoomReport = BuildRoomReport(clearDuration);
        RoomEvaluated?.Invoke(LastRoomReport);

        if (logEmotionChanges)
        {
            Debug.Log($"Room {_roomsCleared} evaluated across {_currentRoomSpawnerCount} spawner(s): {LastRoomReport.emotionBefore} -> {LastRoomReport.emotionAfter} ({LastRoomReport.scoreBefore:0.00} -> {LastRoomReport.scoreAfter:0.00})");
        }

        _currentRoomTime = 0f;
        _currentRoomBaseSpawnCount = 0;
        _currentRoomAdjustedSpawnCount = 0;
        _currentRoomSpawnerCount = 0;
    }

    private int CreateAnonymousRoomId()
    {
        return _nextAnonymousRoomId--;
    }

    private EmotionRoomReport BuildRoomReport(float clearDuration)
    {
        float roomMovementSpeedTotal = _movementSpeedTotal - _roomStartMovementSpeedTotal;
        int roomMovementSamples = _movementSamples - _roomStartMovementSamples;
        float roomAverageSpeed = roomMovementSamples > 0 ? roomMovementSpeedTotal / roomMovementSamples : 0f;

        return new EmotionRoomReport
        {
            roomNumber = _roomsCleared,
            emotionBefore = _roomStartEmotion,
            emotionAfter = CurrentEmotion,
            scoreBefore = _roomStartScore,
            scoreAfter = AggressionScore,
            duration = clearDuration,
            spawnerCount = _currentRoomSpawnerCount,
            baseSpawnCount = _currentRoomBaseSpawnCount,
            adjustedSpawnCount = _currentRoomAdjustedSpawnCount,
            damageTaken = _damageTaken - _roomStartSnapshot.damageTaken,
            deathCount = _deathCount - _roomStartSnapshot.deathCount,
            enemiesEncountered = _encounteredEnemyIds.Count - _roomStartSnapshot.enemiesEncountered,
            attacksPerformed = _attacksPerformed - _roomStartSnapshot.attacksPerformed,
            enemyHits = _enemyHits - _roomStartSnapshot.enemyHits,
            effectiveEnemyHits = _effectiveEnemyHits - _roomStartSnapshot.effectiveEnemyHits,
            timeRunning = _timeRunning - _roomStartSnapshot.timeRunning,
            timeIdle = _timeIdle - _roomStartSnapshot.timeIdle,
            averageMovementSpeed = roomAverageSpeed
        };
    }

    private void EvaluateEmotion(bool forceImmediate)
    {
        float now = Time.time;
        float elapsedSinceLastEvaluation = Mathf.Max(0f, now - _lastEmotionEvaluationTime);
        _lastEmotionEvaluationTime = now;

        float targetScore = CalculateAggressionScore();
        targetScore = ApplyPassiveCalmDecay(targetScore, elapsedSinceLastEvaluation, now);

        if (forceImmediate)
        {
            AggressionScore = targetScore;
        }
        else
        {
            float directionalSmoothing = targetScore >= AggressionScore ? aggressionRiseSmoothing : aggressionFallSmoothing;
            float smoothing = directionalSmoothing > 0f ? directionalSmoothing : scoreSmoothing;
            AggressionScore = Mathf.Lerp(AggressionScore, targetScore, smoothing);
        }

        EmotionProfileSnapshot snapshot = BuildSnapshot();
        EmotionProfileUpdated?.Invoke(snapshot);

        if (Confidence < minimumEvidenceForChange)
        {
            return;
        }

        PlayerEmotionState nextEmotion = CurrentEmotion;
        if (AggressionScore >= aggressiveThreshold)
        {
            nextEmotion = PlayerEmotionState.Aggressive;
        }
        else if (AggressionScore <= calmThreshold)
        {
            nextEmotion = PlayerEmotionState.Calm;
        }

        if (nextEmotion == CurrentEmotion)
        {
            return;
        }

        CurrentEmotion = nextEmotion;
        EmotionChanged?.Invoke(CurrentEmotion, snapshot);

        if (logEmotionChanges)
        {
            Debug.Log($"Emotion profile changed to {CurrentEmotion} ({AggressionScore:0.00})");
        }
    }

    private float CalculateAggressionScore()
    {
        float lifetimeScore = CalculateWeightedScore(
            _damageTaken,
            _deathCount,
            _encounteredEnemyIds.Count,
            _attacksPerformed,
            _effectiveEnemyHits,
            GetAverageMovementSpeed(),
            _timeRunning,
            _timeIdle,
            GetRoomTimeForScoring(),
            expectedDamageTaken,
            expectedDeaths,
            expectedEnemyEncounters,
            expectedAttacks,
            expectedAverageMovementSpeed,
            expectedRoomClearTime);

        EmotionProfileSnapshot recentSnapshot = GetRecentBehaviorSnapshot();
        RecentAggressionScore = CalculateWeightedScore(
            recentSnapshot.damageTaken,
            recentSnapshot.deathCount,
            recentSnapshot.enemiesEncountered,
            recentSnapshot.attacksPerformed,
            recentSnapshot.effectiveEnemyHits,
            recentSnapshot.averageMovementSpeed,
            recentSnapshot.timeRunning,
            recentSnapshot.timeIdle,
            GetRecentRoomTimeForScoring(recentSnapshot),
            expectedRoomDamageTaken,
            expectedRoomDeaths,
            expectedRoomEnemyEncounters,
            expectedRoomAttacks,
            expectedRoomMovementSpeed,
            expectedRoomClearTime);

        Confidence = CalculateConfidence(recentSnapshot);
        float effectiveRecentWeight = recentBehaviorWeight * Confidence;
        return Mathf.Clamp01(Mathf.Lerp(lifetimeScore, RecentAggressionScore, effectiveRecentWeight));
    }

    private float CalculateWeightedScore(
        float damageTaken,
        int deathCount,
        int enemiesEncountered,
        int attacksPerformed,
        float effectiveEnemyHits,
        float averageMovementSpeed,
        float timeRunning,
        float timeIdle,
        float roomTime,
        float expectedDamage,
        float expectedDeathCount,
        float expectedEncounters,
        float expectedAttackCount,
        float expectedMovementSpeed,
        float expectedClearTime)
    {
        float damageScore = SafeRatio(damageTaken, expectedDamage);
        float encounterScore = SafeRatio(enemiesEncountered, expectedEncounters);
        float attackScore = Mathf.Clamp01(SafeRatio(attacksPerformed, expectedAttackCount) * attackIntentScale);
        float rawHitScore = attacksPerformed <= 0 ? 0f : Mathf.Clamp01(effectiveEnemyHits / attacksPerformed);
        float hitScore = Mathf.Clamp01(rawHitScore * hitIntentScale);
        float movementScore = CalculateMovementScore(averageMovementSpeed, timeRunning, timeIdle, expectedMovementSpeed);
        float roomTimeScore = SafeRatio(roomTime, expectedClearTime);
        float deathScore = SafeRatio(deathCount, expectedDeathCount);

        _damagePressureScore = Mathf.Clamp01((damageScore * 0.75f) + (deathScore * 0.25f));
        _combatIntentScore = Mathf.Clamp01((encounterScore * 0.4f) + (attackScore * 0.4f) + (hitScore * 0.2f));
        _movementPressureScore = movementScore;
        _timePressureScore = roomTimeScore;

        float weightedScore =
            damageScore * 0.22f +
            encounterScore * 0.16f +
            attackScore * 0.18f +
            hitScore * 0.08f +
            movementScore * 0.14f +
            roomTimeScore * 0.12f +
            deathScore * 0.10f;

        return Mathf.Clamp01(weightedScore);
    }

    private float CalculateMovementScore(float averageSpeed, float timeRunning, float timeIdle, float expectedMovementSpeed)
    {
        float speedScore = SafeRatio(averageSpeed, expectedMovementSpeed);
        float trackedTime = Mathf.Max(0.01f, timeRunning + timeIdle);
        float runningRatio = Mathf.Clamp01(timeRunning / trackedTime);
        float idleRatio = Mathf.Clamp01(timeIdle / trackedTime);

        return Mathf.Clamp01((speedScore * 0.5f) + (runningRatio * 0.35f) + ((1f - idleRatio) * 0.15f));
    }

    private EmotionProfileSnapshot GetRecentBehaviorSnapshot()
    {
        if (_roomTimerRunning)
        {
            return BuildRoomDeltaSnapshot(_currentRoomTime);
        }

        if (LastRoomReport.roomNumber > 0)
        {
            return BuildSnapshotFromRoomReport(LastRoomReport);
        }

        return BuildSnapshot();
    }

    private EmotionProfileSnapshot BuildRoomDeltaSnapshot(float roomTime)
    {
        float roomMovementSpeedTotal = _movementSpeedTotal - _roomStartMovementSpeedTotal;
        int roomMovementSamples = _movementSamples - _roomStartMovementSamples;

        return new EmotionProfileSnapshot
        {
            state = CurrentEmotion,
            aggressionScore = AggressionScore,
            damageTaken = _damageTaken - _roomStartSnapshot.damageTaken,
            deathCount = _deathCount - _roomStartSnapshot.deathCount,
            enemiesEncountered = _encounteredEnemyIds.Count - _roomStartSnapshot.enemiesEncountered,
            attacksPerformed = _attacksPerformed - _roomStartSnapshot.attacksPerformed,
            enemyHits = _enemyHits - _roomStartSnapshot.enemyHits,
            effectiveEnemyHits = _effectiveEnemyHits - _roomStartSnapshot.effectiveEnemyHits,
            timeRunning = _timeRunning - _roomStartSnapshot.timeRunning,
            timeIdle = _timeIdle - _roomStartSnapshot.timeIdle,
            averageMovementSpeed = roomMovementSamples > 0 ? roomMovementSpeedTotal / roomMovementSamples : 0f,
            activeSpawnerCount = ActiveSpawnerCount,
            currentRoomTime = roomTime,
            lastRoomClearTime = _lastRoomClearTime
        };
    }

    private EmotionProfileSnapshot BuildSnapshotFromRoomReport(EmotionRoomReport report)
    {
        return new EmotionProfileSnapshot
        {
            state = report.emotionAfter,
            aggressionScore = report.scoreAfter,
            damageTaken = report.damageTaken,
            deathCount = report.deathCount,
            enemiesEncountered = report.enemiesEncountered,
            attacksPerformed = report.attacksPerformed,
            enemyHits = report.enemyHits,
            effectiveEnemyHits = report.effectiveEnemyHits,
            timeRunning = report.timeRunning,
            timeIdle = report.timeIdle,
            averageMovementSpeed = report.averageMovementSpeed,
            activeSpawnerCount = ActiveSpawnerCount,
            currentRoomTime = 0f,
            lastRoomClearTime = report.duration
        };
    }

    private float GetRecentRoomTimeForScoring(EmotionProfileSnapshot recentSnapshot)
    {
        return recentSnapshot.currentRoomTime > 0f ? recentSnapshot.currentRoomTime : recentSnapshot.lastRoomClearTime;
    }

    private float CalculateConfidence(EmotionProfileSnapshot recentSnapshot)
    {
        float actionEvidence = SafeRatio(recentSnapshot.attacksPerformed + recentSnapshot.effectiveEnemyHits, expectedRoomAttacks);
        float encounterEvidence = SafeRatio(recentSnapshot.enemiesEncountered, expectedRoomEnemyEncounters);
        float damageEvidence = SafeRatio(recentSnapshot.damageTaken, expectedRoomDamageTaken);
        float movementEvidence = SafeRatio(recentSnapshot.timeRunning + recentSnapshot.timeIdle, 20f);
        float roomEvidence = recentSnapshot.lastRoomClearTime > 0f ? 1f : SafeRatio(recentSnapshot.currentRoomTime, 30f);

        return Mathf.Clamp01(
            actionEvidence * 0.25f +
            encounterEvidence * 0.2f +
            damageEvidence * 0.2f +
            movementEvidence * 0.2f +
            roomEvidence * 0.15f);
    }

    private float GetRoomTimeForScoring()
    {
        if (_lastRoomClearTime > 0f)
        {
            return _lastRoomClearTime;
        }

        return _currentRoomTime;
    }

    private float GetAverageMovementSpeed()
    {
        if (_movementSamples <= 0)
        {
            return 0f;
        }

        return _movementSpeedTotal / _movementSamples;
    }

    private float SafeRatio(float value, float expectedValue)
    {
        if (expectedValue <= 0f)
        {
            return 0f;
        }

        return Mathf.Clamp01(value / expectedValue);
    }

    private float ApplyPassiveCalmDecay(float targetScore, float elapsedSinceLastEvaluation, float now)
    {
        if (calmDecayPerSecond <= 0f)
        {
            return targetScore;
        }

        if (now - _lastCombatIntentTime <= calmDecayDelay)
        {
            return targetScore;
        }

        float decayAmount = calmDecayPerSecond * elapsedSinceLastEvaluation;
        return Mathf.Clamp01(targetScore - decayAmount);
    }

    private float CalculateEffectiveHitContribution()
    {
        if (!useMultiHitDiminishingReturns)
        {
            return 1f;
        }

        float burstWindow = Mathf.Max(0.05f, multiHitBurstWindow);
        bool hasRecentAttackContext = _lastAttackStartedTime > 0f && (Time.time - _lastAttackStartedTime) <= burstWindow;
        bool inBurstWindow = _lastEnemyHitTime > 0f && (Time.time - _lastEnemyHitTime) <= burstWindow;

        if (!hasRecentAttackContext && !inBurstWindow)
        {
            _hitsInCurrentAttack = 0;
            _effectiveHitsInCurrentAttack = 0f;
        }

        _hitsInCurrentAttack++;
        float hitWeight = GetHitWeight(_hitsInCurrentAttack);
        float remainingAttackBudget = Mathf.Max(0f, maxEffectiveHitsPerAttack - _effectiveHitsInCurrentAttack);
        float contribution = Mathf.Min(hitWeight, remainingAttackBudget);
        _effectiveHitsInCurrentAttack += contribution;
        return contribution;
    }

    private float GetHitWeight(int hitIndex)
    {
        if (hitIndex <= 1)
        {
            return 1f;
        }

        float falloff = Mathf.Max(0f, additionalHitFalloff);
        return 1f / (1f + ((hitIndex - 1) * falloff));
    }

    private EmotionProfileSnapshot BuildSnapshot()
    {
        return new EmotionProfileSnapshot
        {
            state = CurrentEmotion,
            aggressionScore = AggressionScore,
            recentAggressionScore = RecentAggressionScore,
            confidence = Confidence,
            damagePressureScore = _damagePressureScore,
            combatIntentScore = _combatIntentScore,
            movementPressureScore = _movementPressureScore,
            timePressureScore = _timePressureScore,
            damageTaken = _damageTaken,
            deathCount = _deathCount,
            enemiesEncountered = _encounteredEnemyIds.Count,
            attacksPerformed = _attacksPerformed,
            enemyHits = _enemyHits,
            effectiveEnemyHits = _effectiveEnemyHits,
            timeRunning = _timeRunning,
            timeIdle = _timeIdle,
            averageMovementSpeed = GetAverageMovementSpeed(),
            activeSpawnerCount = ActiveSpawnerCount,
            currentRoomTime = _currentRoomTime,
            lastRoomClearTime = _lastRoomClearTime
        };
    }
}
