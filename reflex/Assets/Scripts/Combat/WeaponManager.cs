using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections;
using System;
using Unity.VisualScripting;



public class WeaponManager : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private PlayerAnimation playerVisuals;
    [SerializeField] private PlayerManager playerManager;

    public GameObject hitboxVisual;
    public LayerMask enemyLayer;

    [Header("Input")]
    private InputAction attackAction;

    [Header("Combo & Cooldown State")]
    private float lastAttackTime;
    private bool startResetTime = false;

    void Start()
    {
        if (playerManager.playerInput != null)
        {
            // Update this string to match your Input Action Asset exactly
            attackAction = playerManager.playerInput.actions.FindAction("PlayerMovementAction/Attack");
            if (attackAction != null) attackAction.Enable();
        }

        // Initialize the animator with the current weapon's look
        if (playerManager.weaponData != null && playerManager.weaponData.weaponOverride != null)
        {
            playerVisuals.SwapWeaponAnimations(playerManager.weaponData.weaponOverride);
        }
    }

    public void EquipWeapon(WeaponData newData)
    {
        // 1. Update the data reference in PlayerManager
        playerManager.weaponData = newData;

        // 2. Reset combo state to prevent errors
        playerManager.currentComboIndex = 0;
        playerManager.canAttack = true;

        // 3. Update the animations visually
        if (newData.weaponOverride != null)
        {
            playerVisuals.SwapWeaponAnimations(newData.weaponOverride);
        }

        Debug.Log($"<color=cyan>Weapon Swapped to {newData.weaponName}!</color>");
    }

    void Update()
    {
        if (attackAction != null && attackAction.triggered)
        {
            if (playerManager.canAttack)
            {
                ExecuteAttack();
            }
        }
        UpdateTime();
    }


    private void UpdateTime()
    {
        // If the animation hasn't finished yet, don't count down
        if (!startResetTime) return;

        if (playerManager.comboTime <= 0)
        {
            if (playerManager.currentComboIndex > 0) { ResetComboTime(); }
            return;
        }

        playerManager.comboTime -= Time.deltaTime;
    }

    private void ResetComboTime()
    {
        playerManager.currentComboIndex = 0;
        playerManager.comboTime = 0;
        playerManager.canAttack = true;
        startResetTime = false;
    }
    // only use this in animation events
    private void CanAttackEvent()
    {
        if (playerManager.weaponData == null) return;
        playerManager.canAttack = !playerManager.canAttack;
    }

    // use this to call publicly
    public bool CanAttackLocal(bool attack)
    {
        if (playerManager.weaponData == null) return false;
        playerManager.canAttack = attack;
        return playerManager.canAttack;
    }


    private void ExecuteAttack()
    {
        // 1. Cache the reference for performance and readability
        WeaponData data = playerManager.weaponData;
        if (data == null || data.comboChain.Length == 0) return;

        // 2. Set State
        playerManager.canAttack = false;
        playerManager.isAttacking = true;
        startResetTime = false; // Pause the cooldown timer during the swing

        // 3. Logic: Increment and Wrap (Loop) the combo
        playerManager.currentComboIndex++;
        if (playerManager.currentComboIndex > data.comboChain.Length)
        {
            playerManager.currentComboIndex = 1; // Loop back to first attack
        }

        // 4. Get the specific step data (using cached 'data' and safe index)
        AttackStep step = data.comboChain[playerManager.currentComboIndex - 1];

        // 5. Execution
        playerManager.comboTime = data.comboResetTime;
        UpdateHitboxTransform(step);
        playerVisuals.PlayAttack(playerManager.currentComboIndex);

        lastAttackTime = Time.time;
    }

    private void UpdateHitboxTransform(AttackStep step)
    {
        hitboxVisual.transform.localScale = new Vector3(step.attackWidth, step.verticalScale, step.attackRange);
        hitboxVisual.transform.localPosition = new Vector3(0, 0, step.attackRange / 2f);
    }

    //Anim Event --| 
    //             v
    public void HitboxOn()
    {
        hitboxVisual.SetActive(true);
        Vector3 center = hitboxVisual.transform.position;
        Vector3 halfExtents = hitboxVisual.transform.lossyScale / 2f;
        Quaternion orientation = hitboxVisual.transform.rotation;

        Collider[] hitEnemies = Physics.OverlapBox(center, halfExtents, orientation, enemyLayer);
        AttackStep step = playerManager.weaponData.comboChain[playerManager.currentComboIndex - 1];
        float finalDamage = step.attackDamage * playerManager.TotalDamageMultiplier;
        if (UnityEngine.Random.value < playerManager.FinalCritChance)
        {
            finalDamage *= 2;
            Debug.Log("<color=red>CRIT!</color>");
        }

        foreach (Collider enemy in hitEnemies)
        {
            // Apply finalDamage to enemy logic here...
            Debug.Log(finalDamage);  
            // VAMPIRIC FOCUS (Heal on Hit)
            if (UnityEngine.Random.value < playerManager.cardVampChance)
            {
                playerManager.Heal(1);
                Debug.Log("<color=green>Healed 1 HP!</color>");
            }
        }

    }

    public void HitboxOff()
    {
        hitboxVisual.SetActive(false);
        playerManager.canAttack = true;

        StartResetTime();
    }


    public void StartResetTime()
    {
        // Base Reset Time + Card Bonus
        playerManager.comboTime = playerManager.weaponData.comboResetTime + playerManager.cardComboWindowBonus;
    }

    private void OnDrawGizmos()
    {
        if (hitboxVisual != null && hitboxVisual.activeInHierarchy)
        {
            Gizmos.color = Color.yellow;
            Gizmos.matrix = hitboxVisual.transform.localToWorldMatrix;
            // Draw a wireframe cube that matches the hitbox scale
            Gizmos.DrawWireCube(Vector3.zero, Vector3.one);
        }
    }
}