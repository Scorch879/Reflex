using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerManager : MonoBehaviour
{
    public bool isRunning = false;
    public bool isAttacking = false;
    public bool isIdle = true;
    public int comboCount = 0;
    public int currentComboIndex = 0;
    public bool canAttack = true;
    public float comboTime;
    public bool canGoToIdle = true;
    public PlayerInput playerInput;
    public WeaponData weaponData;

    [Header("Data Reference")]
    public PlayerData stats; // Drag your PlayerData asset here

    [Header("Runtime Stats")]
    public float currentHealth;
    public float bonusMaxHealth = 0f;
    public float damageMultiplierModifier = 0f;
    public bool isDead = false;

    // Properties to calculate final values on the fly
    public float MaxHealth => stats.baseMaxHealth + bonusMaxHealth;
    public float TotalDamageMultiplier => stats.baseDamageMultiplier + damageMultiplierModifier;
    private void Start()
    {
        if (stats != null)
        {
            currentHealth = MaxHealth;
        }
    }

    public void TakeDamage(float amount)
    {
        if (isDead) return;
        currentHealth -= amount;
        Debug.Log($"HP: {currentHealth}/{MaxHealth}");
        if (currentHealth <= 0) Die();
    }

    private void Die()
    {
        isDead = true;
        canAttack = false;
        Debug.Log("Player is Dead");
    }   
    private void Update()
    {
        CheckIfIdle();
        CheckIfAttacking();
        CheckComboTime();
    }

    private void CheckIfIdle()
    {
        if (isRunning || isAttacking)
        {
            isIdle = false;
        }
        else
        {
            isIdle = true;
        }
    }

    private void CheckComboTime()
    {
        if (comboTime < 0)
        {
            comboTime = 0;
        }
    }

    private void CheckIfAttacking()
    {
        isAttacking = !canAttack;
    }
}
