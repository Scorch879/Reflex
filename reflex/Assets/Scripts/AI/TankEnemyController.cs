using UnityEngine;

public class NewEmptyCSharpScript : EnemyController
{
    [Header("Tank Enemy Settings")]
    public float tankHealth = 500f;

    void Awake()
    {
        this.speed = 1.5f; // Tank enemies are slower
        this.visionRange = 12f; // Tank enemies have a shorter vision range
    }
}
