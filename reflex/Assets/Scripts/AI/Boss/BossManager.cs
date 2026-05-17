using UnityEngine;

public class BossManager : MonoBehaviour
{
    [Header("Boss References")]
    public BossData bossData;
    public BossController bossController;

    [Header("Boss Settings")]
    public float maxHealth = 100f;
    public float currentHealth;
    public float attack1Damage = 15f;
    public float attack2Damage = 20f;
    public float laserDamage = 25f;
    public float stunDuration = 2f;

    private float _healthSinceLastHurtReaction;
    private float _hurtThreshold;

    public void Awake()
    {
        if (bossData != null)
        {
            maxHealth = bossData.maxHealth;
            attack1Damage = bossData.attack1Damage;
            attack2Damage = bossData.attack2Damage;
            laserDamage = bossData.laserDamage;
            stunDuration = bossData.stunDuration;
        }
    }

    public void Start()
    {
        currentHealth = maxHealth;
        // Calculate 25% of maximum health
        _hurtThreshold = maxHealth * 0.25f; 
        _healthSinceLastHurtReaction = 0f;
    }

    public void TakeDamage(float damage)
    {
        // Safety lock if already dead
        if (bossController.currentState == BossState.Defeated) return;

        currentHealth -= damage;
        _healthSinceLastHurtReaction += damage;
        
        Debug.Log($"Boss took {damage} damage, current health: {currentHealth}/{maxHealth}");

        // 1. Check for Defeat FIRST to prevent double animation triggers
        if (currentHealth <= 0)
        {
            currentHealth = 0;
            bossController.HandleDefeat();
            return; // Exit completely! Do not process hurt reactions if dead.
        }

        // 2. Only trigger a flinch animation if the damage threshold is met
        if (_healthSinceLastHurtReaction >= _hurtThreshold)
        {
            _healthSinceLastHurtReaction = 0f; // Reset threshold tracker
            bossController.HandleHurt();
        }
    }
}