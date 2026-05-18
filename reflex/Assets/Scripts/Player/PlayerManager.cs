using System;
using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections.Generic;

public class PlayerManager : MonoBehaviour
{
    private const float MinimumMaxHealth = 1f;

    public event Action<int, int> SoulEssenceChanged;
    public event Action<PlayerManager> PlayerDied;

    [Header("State Flags")]
    public bool isRunning = false;
    public bool isAttacking = false;
    public bool isIdle = true;
    public bool isDead = false;
    public bool canAttack = true;
    public bool canGoToIdle = true;
    public bool isVulnerable = true;
    public bool isImmortal = false; // For testing purposes, can be toggled on/off

    [Header("Debug Controls")]
    [SerializeField] private Key immortalToggleKey = Key.Equals;

    [Header("Combat & Combo")]
    public int currentComboIndex = 0;
    public float comboTime;
    public WeaponData weaponData;
    public PlayerInput playerInput;
    public InputAction pauseAction;

    [Header("Data References")]
    public PlayerData stats; // Reference to your base ScriptableObject
    [SerializeField, Min(MinimumMaxHealth)] private float fallbackBaseMaxHealth = 100f;
    [SerializeField] private float fallbackBaseDamageMultiplier = 1f;
    [SerializeField, Min(0f)] private float fallbackInvulnerabilityDuration = 1f;

    [Header("Permanent Upgrades (Essence)")]
    public int soulEssence = 0;
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
    private float glassCannonDamageTakenMultiplier = 1f;
    private float glassCannonDamageDealtMultiplier = 1f;
    private bool hasInitializedHealthUI;


    // --- ADDITIVE CALCULATIONS ---
    
    // Max Health: Base + permanent upgrades
    public float MaxHealth => Mathf.Max(MinimumMaxHealth, BaseMaxHealth + permanentMaxHPBonus);

    // Damage Multiplier: additive base bonuses, then Glass Cannon rule multiplier
    public float TotalDamageMultiplier => (1f + BaseDamageMultiplier + permanentAtkBonus + cardAtkBonus) * glassCannonDamageDealtMultiplier;

    // Crit Chance: Base (0.05) + Permanent + Card
    public float FinalCritChance => 0.05f + permanentCritBonus + cardCritChance;

    // Essence Multiplier: 1 (Base) + Card Bonus
    public float FinalEssenceMultiplier => 1f + cardEssenceMult;

    private float BaseMaxHealth => stats != null ? Mathf.Max(MinimumMaxHealth, stats.baseMaxHealth) : fallbackBaseMaxHealth;

    private float BaseDamageMultiplier => stats != null ? stats.baseDamageMultiplier : fallbackBaseDamageMultiplier;

    private float InvulnerabilityDuration => stats != null ? Mathf.Max(0f, stats.invulnerabilityDuration) : fallbackInvulnerabilityDuration;

    private void Awake()
    {
        EnsureValidHealthState(true);
        ResolvePlayerInput();

        if (pauseAction == null)
        {
            Debug.LogError("Pause action not found in PlayerInput actions. Please check your Input Actions setup.");
        }

        UpdateWeaponIconUI();
    }

    private void Start()
    {
        if (SaveManager.Instance != null)
        {
            SaveManager.Instance.ApplyToPlayer(this);
        }

        RestoreHealthToMax();

        TryInitializeHealthUI();
    }

    private void Update()
    {
        HandleImmortalToggleInput();
        CheckIfIdle();
        CheckIfAttacking();
        CheckComboTime();
        TryInitializeHealthUI();
        //OnPause();
        if (InGameUIManager.Instance != null)
        {
            InGameUIManager.Instance.UpdateHPText(currentHealth, MaxHealth);
        }
    }

    // public void OnPause()
    // {
    //     if(pauseAction.WasPressedThisFrame())
    //     {
    //         Debug.Log("Pause button pressed. Toggling pause state.");
    //         PauseManager.Instance.TogglePause();
    //     }
    // }

    // --- COMBAT LOGIC ---

    private void HandleImmortalToggleInput()
    {
        if (Keyboard.current == null)
        {
            return;
        }

        if (Keyboard.current[immortalToggleKey].wasPressedThisFrame)
        {
            ToggleImmortal();
        }
    }

    public void ToggleImmortal()
    {
        isImmortal = !isImmortal;
        Debug.Log($"Player immortality {(isImmortal ? "enabled" : "disabled")}.");
    }

    public void TakeDamage(float amount, bool ignoreInvulnerability = false)
    {
        if (isDead || isImmortal) return;
        if (!ignoreInvulnerability && !isVulnerable) return;
        EnsureValidHealthState(true);

        float finalIncomingDamage = amount * glassCannonDamageTakenMultiplier;
        currentHealth -= finalIncomingDamage;
        EmotionEngine.Instance.RecordDamageTaken(finalIncomingDamage);
        Debug.Log($"HP: {currentHealth}/{MaxHealth}");

        if (currentHealth <= 0)
        {
            currentHealth = 0;
            Die();
        }
        else if (!ignoreInvulnerability)
        {
            isVulnerable = false; // Start invulnerability timer
            float invulnerabilityDuration = InvulnerabilityDuration;
            if (invulnerabilityDuration > 0f)
            {
                Invoke(nameof(ResetVulnerability), invulnerabilityDuration);
            }
            else
            {
                ResetVulnerability();
            }
        }
        if (InGameUIManager.Instance != null)
        {
            InGameUIManager.Instance.UpdateHealth(currentHealth, MaxHealth);
        }
    }

    private void ResetVulnerability()
    {
        isVulnerable = true;
    }

    public void Heal(float amount)
    {
        EnsureValidHealthState(true);
        currentHealth = Mathf.Clamp(currentHealth + Mathf.Max(0f, amount), 0f, MaxHealth);

        if (InGameUIManager.Instance != null)
        {
            InGameUIManager.Instance.UpdateHealth(currentHealth, MaxHealth);
        }
    }

    public void AddSoulEssence(int amount)
    {
        int safeAmount = Mathf.Max(0, amount);
        if (safeAmount <= 0)
        {
            return;
        }

        if (SaveManager.Instance != null)
        {
            SaveManager.Instance.currentSave.soulEssence += safeAmount;
            SaveManager.Instance.SaveGame();
            soulEssence = SaveManager.Instance.currentSave.soulEssence;
        }
        else
        {
            soulEssence += safeAmount;
        }

        SoulEssenceChanged?.Invoke(soulEssence, safeAmount);
    }

    public void ResetTemporaryRunState()
    {
        RespawnForRunStart();
    }

    public void RespawnForRunStart()
    {
        ClearTemporaryCardBuffs();

        RestoreHealthToMax();

        CancelInvoke(nameof(ResetVulnerability));
        isDead = false;
        isRunning = false;
        isAttacking = false;
        isIdle = true;
        canAttack = true;
        canGoToIdle = true;
        isVulnerable = true;
        currentComboIndex = 0;
        comboTime = 0f;

        if (InGameUIManager.Instance != null)
        {
            InGameUIManager.Instance.SetHealthImmediate(currentHealth, MaxHealth);
        }
    }

    public void ClearTemporaryCardBuffs()
    {
        cardAtkBonus = 0f;
        cardCritChance = 0f;
        cardEssenceMult = 0f;
        cardVampChance = 0f;
        cardComboWindowBonus = 0f;
        cardDashCDReduction = 0f;
        cardDashDistanceBonus = 0f;
        glassCannonDamageTakenMultiplier = 1f;
        glassCannonDamageDealtMultiplier = 1f;
    }

    private void Die()
    {
        if (isDead)
        {
            return;
        }

        isDead = true;
        canAttack = false;
        EmotionEngine.Instance.RecordDeath();
        PlayerDied?.Invoke(this);
        if (InGameUIManager.Instance != null)
        {
            InGameUIManager.Instance.ShowGameOver(this);
        }

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
        glassCannonDamageTakenMultiplier = 2f;
        glassCannonDamageDealtMultiplier = 2f;
    }

    public void RestoreHealthToMax()
    {
        currentHealth = MaxHealth;
    }

    public void EnsureValidHealthState(bool restoreEmptyHealth)
    {
        float maxHealth = MaxHealth;
        if (restoreEmptyHealth && !isDead && currentHealth <= 0f)
        {
            currentHealth = maxHealth;
            return;
        }

        currentHealth = Mathf.Clamp(currentHealth, 0f, maxHealth);
    }

    private void TryInitializeHealthUI()
    {
        if (hasInitializedHealthUI || InGameUIManager.Instance == null)
        {
            return;
        }

        InGameUIManager.Instance.SetHealthImmediate(currentHealth, MaxHealth);
        hasInitializedHealthUI = true;
    }

    private void ResolvePlayerInput()
    {
        if (playerInput == null)
        {
            TryGetComponent(out playerInput);
        }

        pauseAction = playerInput != null && playerInput.actions != null
            ? playerInput.actions.FindAction("Pause")
            : null;
    }

    private void UpdateWeaponIconUI()
    {
        if (InGameUIManager.Instance == null)
        {
            return;
        }

        InGameUIManager.Instance.UpdateWeaponIcon(weaponData != null ? weaponData.weaponIcon : null);
    }
}
