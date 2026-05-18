using System.Collections.Generic;
using UnityEngine;

public class EnemySpawner : MonoBehaviour
{
    [System.Serializable]
    private struct EnemySpawnOption
    {
        public string label;
        public GameObject prefab;
        [Min(0f)] public float weight;
        public bool exclusiveWave;
    }

    [Header("Spawner Settings")]
    public GameObject enemyPrefab; // The blueprint to spawn
    public int spawnCount = 3; // How many enemies to spawn per wave
    public float spawnRadius = 5f; // How far from the spawner to place them
    public float spawnHeight = 0f; // Height offset for spawning enemies
    public float respawnDelay = 3f; // How long to wait after the entire wave is gone

    [Header("Enemy Type Selection")]
    [SerializeField] private bool randomizeEnemyTypes = true;
    [SerializeField] private EnemySpawnOption[] enemySpawnOptions;
    [SerializeField, Min(1)] private int exclusiveWaveBaseSpawnCount = 1;
    [SerializeField, Min(1)] private int exclusiveWaveFloorStep = 3;
    [SerializeField, Min(1)] private int exclusiveWaveSpawnCountCap = 4;
    [SerializeField] private bool logSpawnSelections = true;

    [Header("Emotion Spawn Scaling")]
    public bool useEmotionSpawnCount = true;
    public bool useEmotionSpawnRate = true;
    public bool useContinuousEmotionRespawnRate = true;
    [Min(0.01f)] public float calmRespawnDelayMultiplier = 1.25f;
    [Min(0.01f)] public float aggressiveRespawnDelayMultiplier = 0.65f;
    [Range(0f, 1f)] public float respawnRateConfidenceFloor = 0.3f;
    [Min(0f)] public float minimumRespawnDelay = 0.25f;
    public bool logEmotionSpawnRate = true;

    [Header("Wave Sequencing")]
    [SerializeField] private bool enableAdditionalWaves = true;
    [SerializeField, Min(1)] private int maxWavesPerRoom = 3;
    [SerializeField, Range(0f, 1f)] private float additionalWaveChanceFloorOne = 0.12f;
    [SerializeField, Range(0f, 1f)] private float additionalWaveChancePerFloor = 0.06f;
    [SerializeField, Range(0f, 1f)] private float maxAdditionalWaveChance = 0.75f;
    [SerializeField] private bool logWaveRolls = true;

    private readonly List<GameObject> _currentEnemies = new List<GameObject>();
    private float _timer;
    private bool _waveClearHandled;
    private bool _waitingForRoomToClear;
    private bool _hasSpawnedWave;
    private bool _hasUpcomingWave;
    private bool _roomClearReported;
    private int _wavesSpawned;

    public bool HasSpawnedWave => _hasSpawnedWave;
    public bool HasUpcomingWave => _hasUpcomingWave;
    public int WavesSpawned => _wavesSpawned;
    public int AliveEnemyCount
    {
        get
        {
            RemoveDestroyedEnemies();
            return _currentEnemies.Count;
        }
    }

    void Start()
    {
        SpawnWave();
    }

    void Update()
    {
        RemoveDestroyedEnemies();

        if (_currentEnemies.Count == 0)
        {
            if (!_waveClearHandled)
            {
                ResolveCurrentWaveEnd();
            }

            if (_waitingForRoomToClear)
            {
                if (EmotionEngine.Instance.IsRoomActive)
                {
                    return;
                }

                _waitingForRoomToClear = false;
            }

            if (_hasUpcomingWave)
            {
                _timer -= Time.deltaTime;
                if (_timer <= 0f)
                {
                    SpawnWave();
                }
            }
        }
    }

    private void RemoveDestroyedEnemies()
    {
        for (int i = _currentEnemies.Count - 1; i >= 0; i--)
        {
            if (_currentEnemies[i] == null)
            {
                _currentEnemies.RemoveAt(i);
            }
        }
    }

    private void SpawnWave()
    {
        EnemySpawnOption selectedSpawnOption = SelectSpawnOption();
        GameObject selectedEnemyPrefab = selectedSpawnOption.prefab;

        if (selectedEnemyPrefab == null)
        {
            Debug.LogError($"{name}: EnemySpawner has no valid enemy prefab configured.");
            return;
        }

        _wavesSpawned++;
        _hasSpawnedWave = true;
        _currentEnemies.Clear();
        _waveClearHandled = false;
        _waitingForRoomToClear = false;
        _hasUpcomingWave = false;
        _roomClearReported = false;

        int adjustedSpawnCount = GetEmotionAdjustedSpawnCount();
        int finalSpawnCount = GetSpawnCountForSelectedOption(selectedSpawnOption, adjustedSpawnCount);
        EmotionEngine.Instance.BeginRoom(this, spawnCount, finalSpawnCount);

        for (int i = 0; i < finalSpawnCount; i++)
        {
            Vector3 offset = Random.insideUnitSphere * spawnRadius;
            offset.y = spawnHeight;  // Use the configurable spawn height
            Vector3 spawnPosition = transform.position + offset;
            GameObject enemy = Instantiate(selectedEnemyPrefab, spawnPosition, transform.rotation);
            SpawnedEnemyHitboxAutoBinder.Configure(enemy);
            _currentEnemies.Add(enemy);
        }

        _timer = GetEmotionAdjustedRespawnDelay();
        string selectedEnemyName = GetSpawnOptionLabel(selectedSpawnOption);
        int currentFloor = LevelRunManager.HasInstance ? Mathf.Max(1, LevelRunManager.Instance.CurrentFloor) : 1;

        if (logSpawnSelections)
        {
            string waveMode = selectedSpawnOption.exclusiveWave ? "exclusive" : "standard";
            Debug.Log($"<color=#9AE66E>{name}: spawned {finalSpawnCount}x {selectedEnemyName} ({waveMode}, floor {currentFloor})</color>");
        }

        Debug.Log($"<color=green>SPAWNED WAVE OF {finalSpawnCount} ENEMIES ({EmotionDirector.Instance.CurrentDirective.strategy}); active spawners: {EmotionEngine.Instance.ActiveSpawnerCount}</color>");
    }

    private void ResolveCurrentWaveEnd()
    {
        _waveClearHandled = true;

        if (TryScheduleAdditionalWave())
        {
            return;
        }

        EmotionEngine.Instance.RecordRoomCleared(this);
        _roomClearReported = true;
        _waitingForRoomToClear = true;
    }

    private bool TryScheduleAdditionalWave()
    {
        if (!enableAdditionalWaves)
        {
            return false;
        }

        int maxAllowedWaves = Mathf.Max(1, maxWavesPerRoom);
        if (_wavesSpawned >= maxAllowedWaves)
        {
            return false;
        }

        float chance = GetAdditionalWaveChanceForCurrentFloor();
        float roll = Random.value;
        bool queueAnotherWave = roll < chance;

        if (logWaveRolls)
        {
            int floor = LevelRunManager.HasInstance ? Mathf.Max(1, LevelRunManager.Instance.CurrentFloor) : 1;
            string outcome = queueAnotherWave ? "queue next wave" : "end room";
            Debug.Log($"<color=orange>{name}: wave {_wavesSpawned}/{maxAllowedWaves}, floor {floor}, roll {roll:0.00} vs chance {chance:0.00} -> {outcome}</color>");
        }

        if (!queueAnotherWave)
        {
            return false;
        }

        _hasUpcomingWave = true;
        _timer = GetEmotionAdjustedRespawnDelay();

        if (logEmotionSpawnRate)
        {
            Debug.Log($"<color=cyan>{name}: next wave in {_timer:0.00}s ({EmotionEngine.Instance.CurrentEmotion})</color>");
        }

        return true;
    }

    private float GetAdditionalWaveChanceForCurrentFloor()
    {
        int floor = LevelRunManager.HasInstance ? Mathf.Max(1, LevelRunManager.Instance.CurrentFloor) : 1;
        float chance = additionalWaveChanceFloorOne + ((floor - 1) * additionalWaveChancePerFloor);
        return Mathf.Clamp(chance, 0f, Mathf.Clamp01(maxAdditionalWaveChance));
    }

    private EnemySpawnOption SelectSpawnOption()
    {
        EnemySpawnOption fallbackOption = CreateFallbackSpawnOption();
        if (!randomizeEnemyTypes || enemySpawnOptions == null || enemySpawnOptions.Length == 0)
        {
            return fallbackOption;
        }

        float totalWeight = 0f;
        EnemySpawnOption lastValidOption = fallbackOption;
        bool hasValidOption = false;

        for (int i = 0; i < enemySpawnOptions.Length; i++)
        {
            EnemySpawnOption option = enemySpawnOptions[i];
            if (option.prefab == null || option.weight <= 0f)
            {
                continue;
            }

            totalWeight += option.weight;
            lastValidOption = option;
            hasValidOption = true;
        }

        if (!hasValidOption || totalWeight <= 0f)
        {
            return fallbackOption;
        }

        float roll = Random.value * totalWeight;
        for (int i = 0; i < enemySpawnOptions.Length; i++)
        {
            EnemySpawnOption option = enemySpawnOptions[i];
            if (option.prefab == null || option.weight <= 0f)
            {
                continue;
            }

            roll -= option.weight;
            if (roll <= 0f)
            {
                return option;
            }
        }

        return lastValidOption;
    }

    private EnemySpawnOption CreateFallbackSpawnOption()
    {
        return new EnemySpawnOption
        {
            label = enemyPrefab != null ? enemyPrefab.name : "Unassigned",
            prefab = enemyPrefab,
            weight = 1f,
            exclusiveWave = false
        };
    }

    private int GetSpawnCountForSelectedOption(EnemySpawnOption selectedOption, int adjustedSpawnCount)
    {
        int clampedAdjustedCount = Mathf.Max(1, adjustedSpawnCount);
        if (!selectedOption.exclusiveWave)
        {
            return clampedAdjustedCount;
        }

        int currentFloor = LevelRunManager.HasInstance ? Mathf.Max(1, LevelRunManager.Instance.CurrentFloor) : 1;
        int floorStep = Mathf.Max(1, exclusiveWaveFloorStep);
        int maxExclusiveCount = Mathf.Max(exclusiveWaveBaseSpawnCount, exclusiveWaveSpawnCountCap);
        int floorBonus = (currentFloor - 1) / floorStep;
        int exclusiveCount = Mathf.Clamp(exclusiveWaveBaseSpawnCount + floorBonus, 1, maxExclusiveCount);

        return Mathf.Clamp(exclusiveCount, 1, clampedAdjustedCount);
    }

    private string GetSpawnOptionLabel(EnemySpawnOption option)
    {
        if (!string.IsNullOrWhiteSpace(option.label))
        {
            return option.label.Trim();
        }

        return option.prefab != null ? option.prefab.name : "Unknown Enemy";
    }

    private int GetEmotionAdjustedSpawnCount()
    {
        int baseCount;
        if (!useEmotionSpawnCount)
        {
            baseCount = spawnCount;
        }
        else
        {
            baseCount = EmotionDirector.Instance.GetRecommendedSpawnCount(spawnCount);
        }

        float floorMultiplier = LevelRunManager.HasInstance ? LevelRunManager.Instance.CurrentFloorSpawnMultiplier : 1f;
        return Mathf.Max(1, Mathf.CeilToInt(baseCount * floorMultiplier));
    }

    private float GetEmotionAdjustedRespawnDelay()
    {
        if (!useEmotionSpawnRate)
        {
            return Mathf.Max(minimumRespawnDelay, respawnDelay);
        }

        float multiplier;
        if (useContinuousEmotionRespawnRate)
        {
            EmotionEngine engine = EmotionEngine.Instance;
            float confidenceInfluence = Mathf.Lerp(respawnRateConfidenceFloor, 1f, engine.Confidence);
            float blend = Mathf.Lerp(0.5f, engine.AggressionScore, confidenceInfluence);
            multiplier = Mathf.Lerp(calmRespawnDelayMultiplier, aggressiveRespawnDelayMultiplier, blend);
        }
        else
        {
            multiplier = EmotionEngine.Instance.CurrentEmotion == PlayerEmotionState.Aggressive
                ? aggressiveRespawnDelayMultiplier
                : calmRespawnDelayMultiplier;
        }

        float floorMultiplier = LevelRunManager.HasInstance ? LevelRunManager.Instance.CurrentFloorRespawnDelayMultiplier : 1f;
        return Mathf.Max(minimumRespawnDelay, respawnDelay * multiplier * floorMultiplier);
    }

    private void OnDisable()
    {
        if (_hasSpawnedWave && !_roomClearReported && EmotionEngine.HasInstance)
        {
            EmotionEngine.Instance.RecordRoomCleared(this);
            _roomClearReported = true;
        }
    }

    private static class SpawnedEnemyHitboxAutoBinder
    {
        private const string EnemyTag = "Enemy";

        private static readonly string[] AttackHitboxNames =
        {
            "Hit Box",
            "Hitbox"
        };

        private static readonly string[] HurtboxNames =
        {
            "Hurt Box",
            "Hurtbox"
        };

        public static void Configure(GameObject spawnedEnemy)
        {
            if (spawnedEnemy == null)
            {
                return;
            }

            EnemyController enemyController = spawnedEnemy.GetComponent<EnemyController>();
            if (enemyController != null)
            {
                GameObject attackHitboxObject = enemyController.enemyHitbox;
                if (attackHitboxObject == null)
                {
                    attackHitboxObject = FindAttackHitbox(spawnedEnemy.transform);
                }

                if (attackHitboxObject != null)
                {
                    enemyController.enemyHitbox = attackHitboxObject;
                    enemyController.enemyHitbox.SetActive(false);
                }
            }

            GameObject hurtboxObject = FindHurtbox(spawnedEnemy.transform);
            if (hurtboxObject != null)
            {
                hurtboxObject.tag = EnemyTag;
            }
        }

        private static GameObject FindAttackHitbox(Transform root)
        {
            EnemyHitbox enemyHitbox = root.GetComponentInChildren<EnemyHitbox>(true);
            if (enemyHitbox != null)
            {
                return enemyHitbox.gameObject;
            }

            return FindByNames(root, AttackHitboxNames);
        }

        private static GameObject FindHurtbox(Transform root)
        {
            EnemyHurtbox enemyHurtbox = root.GetComponentInChildren<EnemyHurtbox>(true);
            if (enemyHurtbox != null)
            {
                return enemyHurtbox.gameObject;
            }

            return FindByNames(root, HurtboxNames);
        }

        private static GameObject FindByNames(Transform root, string[] candidateNames)
        {
            for (int i = 0; i < candidateNames.Length; i++)
            {
                Transform candidate = FindChildRecursive(root, candidateNames[i]);
                if (candidate != null)
                {
                    return candidate.gameObject;
                }
            }

            return null;
        }

        private static Transform FindChildRecursive(Transform root, string targetName)
        {
            if (root.name == targetName)
            {
                return root;
            }

            for (int i = 0; i < root.childCount; i++)
            {
                Transform child = root.GetChild(i);
                Transform found = FindChildRecursive(child, targetName);
                if (found != null)
                {
                    return found;
                }
            }

            return null;
        }
    }
}
