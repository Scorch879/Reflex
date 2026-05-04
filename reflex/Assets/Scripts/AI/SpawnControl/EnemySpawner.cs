using System.Collections.Generic;
using UnityEngine;

public class EnemySpawner : MonoBehaviour
{
    [Header("Spawner Settings")]
    public GameObject enemyPrefab; // The blueprint to spawn
    public int spawnCount = 3; // How many enemies to spawn per wave
    public float spawnRadius = 5f; // How far from the spawner to place them
    public float spawnHeight = 0f; // Height offset for spawning enemies
    public float respawnDelay = 3f; // How long to wait after the entire wave is gone

    private readonly List<GameObject> _currentEnemies = new List<GameObject>();
    private float _timer;

    void Start()
    {
        SpawnWave();
    }

    void Update()
    {
        RemoveDestroyedEnemies();

        if (_currentEnemies.Count == 0)
        {
            _timer -= Time.deltaTime;
            if (_timer <= 0f)
            {
                SpawnWave();
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
        _currentEnemies.Clear();

        for (int i = 0; i < spawnCount; i++)
        {
            Vector3 offset = Random.insideUnitSphere * spawnRadius;
            offset.y = spawnHeight;  // Use the configurable spawn height
            Vector3 spawnPosition = transform.position + offset;
            GameObject enemy = Instantiate(enemyPrefab, spawnPosition, transform.rotation);
            _currentEnemies.Add(enemy);
        }

        _timer = respawnDelay;
        Debug.Log($"<color=green>SPAWNED WAVE OF {spawnCount} ENEMIES</color>");
    }
}