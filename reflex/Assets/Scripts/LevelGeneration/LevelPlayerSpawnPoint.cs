using UnityEngine;

public class LevelPlayerSpawnPoint : MonoBehaviour
{
    [SerializeField] private string spawnId = "Default";
    [SerializeField] private bool defaultSpawn = true;
    [SerializeField] private int priority;

    public string SpawnId => spawnId;
    public bool DefaultSpawn => defaultSpawn;
    public int Priority => priority;
}
