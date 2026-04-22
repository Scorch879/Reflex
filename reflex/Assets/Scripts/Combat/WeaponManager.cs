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
        if (playerManager.weaponData == null) return;
        playerManager.canAttack = false;
        playerManager.isAttacking = true;

        startResetTime = false;
        playerManager.currentComboIndex++;

        if (playerManager.currentComboIndex > playerManager.weaponData.comboChain.Length)
        {
            playerManager.currentComboIndex = playerManager.weaponData.comboChain.Length;
        }

        AttackStep step = playerManager.weaponData.comboChain[playerManager.currentComboIndex - 1];
        playerManager.comboTime = playerManager.weaponData.comboResetTime;


        // 2. Physical Hitbox Scaling
        UpdateHitboxTransform(step);
        // Play Visuals
        playerVisuals.PlayAttack(playerManager.currentComboIndex);

        // START the routine (We don't stop the old one anymore because CanAttack blocks it)
        //hitboxCoroutine = StartCoroutine(HitboxRoutine(step));
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
        AttackStep step = playerManager.weaponData.comboChain[playerManager.currentComboIndex];
        float finalDamage = step.attackDamage * playerManager.TotalDamageMultiplier;
        foreach (Collider enemy in hitEnemies)
        {
            // Now you can pass finalDamage to your enemy script
            Debug.Log($"Hit {enemy.name} for {finalDamage} damage!");
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
        startResetTime = true;
        playerManager.comboTime = playerManager.weaponData.comboResetTime;
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