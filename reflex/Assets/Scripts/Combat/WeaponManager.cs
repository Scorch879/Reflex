using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections;

public class WeaponManager : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private PlayerAnimation playerVisuals;
    public WeaponData currentWeaponData;
    public GameObject hitboxVisual;
    public LayerMask enemyLayer;

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
        return Time.time >= lastAttackTime + currentWeaponData.attackRate;
    }

    private void ExecuteAttack()
    {
        if (currentWeaponData == null || currentWeaponData.comboChain.Length == 0) return;

        // Reset combo if player waited too long
        if (Time.time - lastAttackTime > currentWeaponData.comboResetTime)
        {
            currentComboIndex = 0;
        }

        AttackStep step = currentWeaponData.comboChain[currentComboIndex];

        // 1. Tell Visuals to play the specific animation for this combo hit
        playerVisuals.PlayAttack(currentComboIndex);

        // 2. Physical Hitbox Scaling
        UpdateHitboxTransform(step);

        // 3. Trigger the logic and damage check
        StopAllCoroutines();
        StartCoroutine(HitboxRoutine(step));

        // 4. Update State
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
        hitboxVisual.SetActive(true);

        // 1. Get the current World Position, Rotation, and Scale of the hitbox
        // We use lossyScale / 2 because OverlapBox expects "half-extents"
        Vector3 center = hitboxVisual.transform.position;
        Vector3 halfExtents = hitboxVisual.transform.lossyScale / 2f;
        Quaternion orientation = hitboxVisual.transform.rotation;

        // 2. Perform the Physics Check
        // This looks for anything on the 'Enemy' layer inside that yellow box
        Collider[] hitEnemies = Physics.OverlapBox(center, halfExtents, orientation, enemyLayer);

        // 3. Handle the Results
        if (hitEnemies.Length > 0)
        {
            foreach (Collider enemy in hitEnemies)
            {
                Debug.Log($"<color=red>HIT CONFIRMED:</color> Dealt {step.attackDamage} damage to {enemy.name}");

                // This is where you will eventually call enemy.TakeDamage()
            }
        }
        else
        {
            Debug.Log("<color=white>Attack missed.</color> No enemies found in range.");
        }

        // 4. Wait for the duration defined in your WeaponData, then hide
        yield return new WaitForSeconds(step.activeTime);
        hitboxVisual.SetActive(false);
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