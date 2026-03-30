using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections;
using System.Collections.Generic;

public class WeaponManager : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private PlayerAnimation playerVisuals;
    public WeaponData currentWeaponData;
    public GameObject hitboxVisual;
    public LayerMask enemyLayer;
    private Coroutine hitboxCoroutine;
    [Header("Input")]
    private PlayerInput userInput;
    private InputAction attackAction;

    [Header("Combo & Cooldown State")]
    private int currentComboIndex = 0;
    private float lastAttackTime;

    void Start()
    {
        userInput = GetComponent<PlayerInput>();
        if (userInput != null)
        {
            // Update this string to match your Input Action Asset exactly
            attackAction = userInput.actions.FindAction("PlayerMovementAction/Attack");
            if (attackAction != null) attackAction.Enable();
        }

        // Initialize the animator with the current weapon's look
        if (currentWeaponData != null && currentWeaponData.weaponOverride != null)
        {
            playerVisuals.SwapWeaponAnimations(currentWeaponData.weaponOverride);
        }
    }

    void Update()
    {
        if (attackAction != null && attackAction.triggered)
        {
            if (CanAttack())
            {
                ExecuteAttack();
            }
        }
    }

    private bool CanAttack()
    {
        if (currentWeaponData == null) return false;

        // 1. Check if the cooldown (attackRate) has passed
        bool cooldownOver = Time.time >= lastAttackTime + currentWeaponData.attackRate;

        // 2. Check if a hitbox is currently active
        // If hitboxCoroutine is NOT null, it means HitboxRoutine is still running
        bool notCurrentlyAttacking = (hitboxCoroutine == null);

        return cooldownOver && notCurrentlyAttacking;
    }

    private void ExecuteAttack()
    {
        if (currentWeaponData == null || currentWeaponData.comboChain.Length == 0) return;

        // Reset combo if the player waited too long
        if (Time.time - lastAttackTime > currentWeaponData.comboResetTime)
        {
            currentComboIndex = 0;
        }

        AttackStep step = currentWeaponData.comboChain[currentComboIndex];

        // Play Visuals
        playerVisuals.PlayAttack(currentComboIndex);

        // START the routine (We don't stop the old one anymore because CanAttack blocks it)
        hitboxCoroutine = StartCoroutine(HitboxRoutine(step));

        lastAttackTime = Time.time;
        currentComboIndex = (currentComboIndex + 1) % currentWeaponData.comboChain.Length;
    }

    private void UpdateHitboxTransform(AttackStep step)
    {
        hitboxVisual.transform.localScale = new Vector3(step.attackWidth, step.verticalScale, step.attackRange);
        hitboxVisual.transform.localPosition = new Vector3(0, 0, step.attackRange / 2f);
    }

    private IEnumerator HitboxRoutine(AttackStep step)
    {
        // 1. STARTUP DELAY
        // This allows the "Wind-up" animation to play first
        yield return new WaitForSeconds(step.startupDelay);

        // 2. ACTIVATE HITBOX
        hitboxVisual.SetActive(true);
        UpdateHitboxTransform(step); // Ensure scale/pos are updated after the wait

        // 3. DAMAGE DETECTION
        HashSet<Collider> alreadyHit = new HashSet<Collider>();
        Vector3 center = hitboxVisual.transform.position;
        Vector3 halfExtents = hitboxVisual.transform.lossyScale / 2f;
        Quaternion orientation = hitboxVisual.transform.rotation;

        Collider[] hitEnemies = Physics.OverlapBox(center, halfExtents, orientation, enemyLayer);

        foreach (Collider enemy in hitEnemies)
        {
            if (!alreadyHit.Contains(enemy))
            {
                alreadyHit.Add(enemy);
                Debug.Log($"<color=red>HIT!</color> {enemy.name} for {step.attackDamage} damage.");
                // enemy.GetComponent<EnemyHealth>()?.TakeDamage(step.attackDamage);
            }
        }

        // 4. ACTIVE TIME
        // How long the "hit" stays in the world
        yield return new WaitForSeconds(step.activeTime);

        // 5. DEACTIVATE
        hitboxVisual.SetActive(false);
        hitboxCoroutine = null;
    }

    //this is for debugging purposes can remove or delete
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