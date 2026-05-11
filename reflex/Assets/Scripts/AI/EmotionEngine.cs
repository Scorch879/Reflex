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
    public float damageTaken;
    public int deathCount;
    public int enemiesEncountered;
    public int attacksPerformed;
    public int enemyHits;
    public float timeRunning;
    public float timeIdle;
    public float averageMovementSpeed;
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
    public int baseSpawnCount;
    public int adjustedSpawnCount;
    public float damageTaken;
    public int deathCount;
    public int enemiesEncountered;
    public int attacksPerformed;
    public int enemyHits;
    public float timeRunning;
    public float timeIdle;
    public float averageMovementSpeed;
}

public class EmotionEngine : MonoBehaviour
{
    public static event Action<PlayerEmotionState, EmotionProfileSnapshot> EmotionChanged;
    public static event Action<EmotionRoomReport> RoomEvaluated;

    private static EmotionEngine _instance;

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
    [SerializeField] private float evaluationInterval = 1f;
    [SerializeField] private bool logEmotionChanges = true;

    [Header("Expected Values")]
    [SerializeField] private float expectedDamageTaken = 50f;
    [SerializeField] private float expectedEnemyEncounters = 8f;
    [SerializeField] private float expectedAttacks = 20f;
    [SerializeField] private float expectedAverageMovementSpeed = 5f;
    [SerializeField] private float expectedRoomClearTime = 120f;
    [SerializeField] private float expectedDeaths = 2f;

    [Header("Adaptive Spawning")]
    [SerializeField] private float aggressiveSpawnMultiplier = 1.35f;
    [SerializeField] private float calmSpawnMultiplier = 0.85f;

    [Header("Debug")]
    [SerializeField] private bool createDebugHud = true;

    public PlayerEmotionState CurrentEmotion { get; private set; }
    public float AggressionScore { get; private set; }
    public EmotionProfileSnapshot CurrentSnapshot => BuildSnapshot();
    public EmotionRoomReport LastRoomReport { get; private set; }
    public bool IsRoomActive => _roomTimerRunning;

    private readonly HashSet<int> _encounteredEnemyIds = new HashSet<int>();
    private float _damageTaken;
    private int _deathCount;
    private int _attacksPerformed;
    private int _enemyHits;
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
    private EmotionProfileSnapshot _roomStartSnapshot;
    private PlayerEmotionState _roomStartEmotion;
    private float _roomStartScore;
    private float _roomStartMovementSpeedTotal;
    private int _roomStartMovementSamples;

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
        _evaluationTimer = evaluationInterval;

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

    public void BeginRoom(int baseSpawnCount, int adjustedSpawnCount)
    {
        _currentRoomTime = 0f;
        _roomTimerRunning = true;
        _currentRoomBaseSpawnCount = baseSpawnCount;
        _currentRoomAdjustedSpawnCount = adjustedSpawnCount;
        _roomStartSnapshot = BuildSnapshot();
        _roomStartEmotion = CurrentEmotion;
        _roomStartScore = AggressionScore;
        _roomStartMovementSpeedTotal = _movementSpeedTotal;
        _roomStartMovementSamples = _movementSamples;
    }

    public void RecordRoomCleared()
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
            Debug.Log($"Room {_roomsCleared} evaluated: {LastRoomReport.emotionBefore} -> {LastRoomReport.emotionAfter} ({LastRoomReport.scoreBefore:0.00} -> {LastRoomReport.scoreAfter:0.00})");
        }

        _currentRoomTime = 0f;
    }

    public void RecordDamageTaken(float amount)
    {
        if (amount <= 0f)
        {
            return;
        }

        _damageTaken += amount;
        EvaluateEmotion(false);
    }

    public void RecordDeath()
    {
        _deathCount++;
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
    }

    public void RecordEnemyHit(float damage)
    {
        if (damage <= 0f)
        {
            return;
        }

        _enemyHits++;
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
        _timeRunning = 0f;
        _timeIdle = 0f;
        _movementSpeedTotal = 0f;
        _movementSamples = 0;
        _currentRoomTime = 0f;
        _lastRoomClearTime = 0f;
        _roomTimerRunning = false;
        _roomsCleared = 0;
        _currentRoomBaseSpawnCount = 0;
        _currentRoomAdjustedSpawnCount = 0;
        LastRoomReport = default;
        CurrentEmotion = startingEmotion;
        AggressionScore = startingEmotion == PlayerEmotionState.Aggressive ? aggressiveThreshold : calmThreshold;
        EmotionChanged?.Invoke(CurrentEmotion, BuildSnapshot());
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
            baseSpawnCount = _currentRoomBaseSpawnCount,
            adjustedSpawnCount = _currentRoomAdjustedSpawnCount,
            damageTaken = _damageTaken - _roomStartSnapshot.damageTaken,
            deathCount = _deathCount - _roomStartSnapshot.deathCount,
            enemiesEncountered = _encounteredEnemyIds.Count - _roomStartSnapshot.enemiesEncountered,
            attacksPerformed = _attacksPerformed - _roomStartSnapshot.attacksPerformed,
            enemyHits = _enemyHits - _roomStartSnapshot.enemyHits,
            timeRunning = _timeRunning - _roomStartSnapshot.timeRunning,
            timeIdle = _timeIdle - _roomStartSnapshot.timeIdle,
            averageMovementSpeed = roomAverageSpeed
        };
    }

    private void EvaluateEmotion(bool forceImmediate)
    {
        float targetScore = CalculateAggressionScore();
        AggressionScore = forceImmediate ? targetScore : Mathf.Lerp(AggressionScore, targetScore, scoreSmoothing);

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
        EmotionProfileSnapshot snapshot = BuildSnapshot();
        EmotionChanged?.Invoke(CurrentEmotion, snapshot);

        if (logEmotionChanges)
        {
            Debug.Log($"Emotion profile changed to {CurrentEmotion} ({AggressionScore:0.00})");
        }
    }

    private float CalculateAggressionScore()
    {
        float damageScore = SafeRatio(_damageTaken, expectedDamageTaken);
        float encounterScore = SafeRatio(_encounteredEnemyIds.Count, expectedEnemyEncounters);
        float attackScore = SafeRatio(_attacksPerformed, expectedAttacks);
        float hitScore = _attacksPerformed <= 0 ? 0f : Mathf.Clamp01((float)_enemyHits / _attacksPerformed);
        float movementScore = CalculateMovementScore();
        float roomTimeScore = SafeRatio(GetRoomTimeForScoring(), expectedRoomClearTime);
        float deathScore = SafeRatio(_deathCount, expectedDeaths);

        float weightedScore =
            damageScore * 0.24f +
            encounterScore * 0.18f +
            attackScore * 0.16f +
            hitScore * 0.08f +
            movementScore * 0.14f +
            roomTimeScore * 0.14f +
            deathScore * 0.06f;

        return Mathf.Clamp01(weightedScore);
    }

    private float CalculateMovementScore()
    {
        float averageSpeed = GetAverageMovementSpeed();
        float speedScore = SafeRatio(averageSpeed, expectedAverageMovementSpeed);
        float trackedTime = Mathf.Max(0.01f, _timeRunning + _timeIdle);
        float runningRatio = Mathf.Clamp01(_timeRunning / trackedTime);
        float idleRatio = Mathf.Clamp01(_timeIdle / trackedTime);

        return Mathf.Clamp01((speedScore * 0.5f) + (runningRatio * 0.35f) + ((1f - idleRatio) * 0.15f));
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

    private EmotionProfileSnapshot BuildSnapshot()
    {
        return new EmotionProfileSnapshot
        {
            state = CurrentEmotion,
            aggressionScore = AggressionScore,
            damageTaken = _damageTaken,
            deathCount = _deathCount,
            enemiesEncountered = _encounteredEnemyIds.Count,
            attacksPerformed = _attacksPerformed,
            enemyHits = _enemyHits,
            timeRunning = _timeRunning,
            timeIdle = _timeIdle,
            averageMovementSpeed = GetAverageMovementSpeed(),
            currentRoomTime = _currentRoomTime,
            lastRoomClearTime = _lastRoomClearTime
        };
    }
}
