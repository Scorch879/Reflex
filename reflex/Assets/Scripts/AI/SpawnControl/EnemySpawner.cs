using UnityEngine;

public class EnemySpawner : MonoBehaviour
{
    [Header("Spawner Settings")]
    public GameObject enemyPrefab; // The blueprint to spawn
    public float respawnDelay = 3f; // How long to wait after the enemy dies

    private GameObject _currentEnemy;
    private float _timer;

    void Start()
    {
        // Spawn the very first enemy when the game starts
        SpawnEnemy();
    }

    void Update()
    {
        // If the current enemy is dead (deleted by the DeathState)
        if (_currentEnemy == null)
        {
            _timer -= Time.deltaTime;

            // When the timer hits 0, respawn!
            if (_timer <= 0)
            {
                SpawnEnemy();
            }
        }
    }

    void SpawnEnemy()
    {
        // Create a new enemy at this spawner's exact position and rotation
        _currentEnemy = Instantiate(enemyPrefab, transform.position, transform.rotation);
        
        // Reset the timer for the next death
        _timer = respawnDelay; 
        
        Debug.Log("<color=green>SPAWNING NEW ENEMY!</color>");
    }
}