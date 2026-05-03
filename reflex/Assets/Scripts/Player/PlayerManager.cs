using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerManager : MonoBehaviour
{
    [Header("State Flags")]
    public bool isRunning = false;
    public bool isAttacking = false;
    public bool isIdle = true;
    public bool isDead = false;
    public bool canAttack = true;
    public bool canGoToIdle = true;

    [Header("Combat & Combo")]
    public int currentComboIndex = 0;
    public float comboTime;
    public WeaponData weaponData;
    public PlayerInput playerInput;

    [Header("Data References")]
    public PlayerData stats; // Reference to your base ScriptableObject

    [Header("Permanent Upgrades (Essence)")]
    public float permanentAtkBonus = 0f;    // e.g., 0.1 for +10%
    public float permanentMaxHPBonus = 0f;
    public float permanentCritBonus = 0f;

    [Header("In-Run Card Buffs (Temporary)")]
    public float cardAtkBonus = 0f;
    public float cardCritChance = 0f;
    public float cardEssenceMult = 0f;      // e.g., 0.25 for +25%
    public float cardVampChance = 0f;
    public float cardComboWindowBonus = 0f; // Perfect Rhythm
    public float cardDashCDReduction = 0f;  // Fleet Foot CD
    public float cardDashDistanceBonus = 0f; // Fleet Foot Distance

    [Header("Runtime Health")]
    public float currentHealth;
    private float glassCannonHPModifier = 1f; // Used for the Glass Cannon card

    // --- ADDITIVE CALCULATIONS ---
    
    // Max Health: (Base + Permanent + Card) / Glass Cannon Penalty
    public float MaxHealth => (stats.baseMaxHealth + permanentMaxHPBonus) * glassCannonHPModifier;

    // Damage Multiplier: 1 + Base + Permanent + Card
    public float TotalDamageMultiplier => 1f + stats.baseDamageMultiplier + permanentAtkBonus + cardAtkBonus;

    // Crit Chance: Base (0.05) + Permanent + Card
    public float FinalCritChance => 0.05f + permanentCritBonus + cardCritChance;

    // Essence Multiplier: 1 (Base) + Card Bonus
    public float FinalEssenceMultiplier => 1f + cardEssenceMult;

    private void Start()
    {
        if (stats != null)
        {
            currentHealth = MaxHealth;
        }
    }

    private void Update()
    {
        CheckIfIdle();
        CheckIfAttacking();
        CheckComboTime();
    }

    // --- COMBAT LOGIC ---

    public void TakeDamage(float amount)
    {
        if (isDead) return;
        currentHealth -= amount;
        Debug.Log($"HP: {currentHealth}/{MaxHealth}");
        if (currentHealth <= 0) Die();
    }

    public void Heal(float amount)
    {
        currentHealth = Mathf.Min(currentHealth + amount, MaxHealth);
    }

    private void Die()
    {
        isDead = true;
        canAttack = false;
        Debug.Log("<color=red>Player is Dead</color>");
    }

    // --- STATE CHECKS ---

    private void CheckIfIdle()
    {
        // Player is idle only if not moving and not currently in an attack animation[cite: 2]
        isIdle = !isRunning && !isAttacking;
    }

    private void CheckIfAttacking()
    {
        // isAttacking is true as long as the attack "lock" is active
        isAttacking = !canAttack;
    }

    private void CheckComboTime()
    {
        if (comboTime > 0)
        {
            comboTime -= Time.deltaTime;
        }
        else
        {
            comboTime = 0;
            // If the timer runs out, the combo index resets to 0
            if (canAttack) currentComboIndex = 0;
        }
    }

    // --- CARD APPLICATION HELPER ---

    public void ApplyGlassCannon()
    {
        glassCannonHPModifier = 0.5f; // Halve health
        cardAtkBonus += 0.5f;        // Huge damage boost
        currentHealth = Mathf.Min(currentHealth, MaxHealth);
    }
}