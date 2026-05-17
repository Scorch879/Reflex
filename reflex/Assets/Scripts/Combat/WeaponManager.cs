using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections;



public class WeaponManager : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private PlayerAnimation playerVisuals;
    [SerializeField] private PlayerManager playerManager;
    [SerializeField] private PlayerMovementManagement playerMovement;

    [SerializeField] private GameObject hitSparkPrefab; // spawned on successful hit, positioned at the hitbox's location with a slight random rotation for visual variety
    public GameObject hitboxVisual;
    public LayerMask enemyLayer;

    [Header("Attack Assist")]
    [SerializeField] private float attackAssistMaxStepDistance = 0.45f;
    [SerializeField] private float attackAssistReachPadding = 0.15f;
    [SerializeField] private float attackAssistSearchPadding = 0.6f;

    [Header("Input")]
    private InputAction attackAction;

    [Header("Combo & Cooldown State")]
    private float lastAttackTime;
    private bool startResetTime = false;
    private CharacterController playerController;

    void Start()
    {
        playerController = GetComponent<CharacterController>();
        playerMovement = GetComponent<PlayerMovementManagement>();

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

    private void ExecuteAttack()
    {
        // 1. Cache the reference for performance and readability
        WeaponData data = playerManager.weaponData;
        if (data == null || data.comboChain.Length == 0) return;

        int nextComboIndex = playerManager.currentComboIndex + 1;
        if (nextComboIndex > data.comboChain.Length)
        {
            nextComboIndex = 1; // Loop back to first attack
        }

        AttackStep step = data.comboChain[nextComboIndex - 1];

        ApplyAttackAssist(step);

        // 2. Set State
        playerManager.canAttack = false;
        playerManager.isAttacking = true;
        EmotionEngine.Instance.RecordAttackStarted();
        startResetTime = false; // Pause the cooldown timer during the swing

        // 3. Apply the prepared combo index
        playerManager.currentComboIndex = nextComboIndex;

        // 5. Execution
        // Note: We use the card window bonus we set up earlier!
        playerManager.comboTime = data.comboResetTime + playerManager.cardComboWindowBonus;

        UpdateHitboxTransform(step);
        playerVisuals.PlayAttack(playerManager.currentComboIndex);
        ApplyDashIn(step);

        lastAttackTime = Time.time;
    }

    // dash in when attacking using weaponData dashInvelocity and the player's character controller
    private void ApplyDashIn(AttackStep step)
    {
        if (playerController == null) return;

        Vector3 dashDirection = transform.forward;
        float dashDistance = step.dashInDistance;
        float dashVelocity = step.dashInVelocity;
        StartCoroutine(DashInCoroutine(dashDirection, dashDistance, dashVelocity));
    }

    private IEnumerator DashInCoroutine(Vector3 direction, float distance, float velocity)
    {
        float dashTime = distance / velocity;
        float elapsedTime = 0f;

        while (elapsedTime < dashTime)
        {
            playerController.Move(direction * velocity * Time.deltaTime);
            elapsedTime += Time.deltaTime;
            yield return null;
        }
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
        if (playerMovement.isDashing)
        {
            HitboxOff();
            return;
        }
        hitboxVisual.SetActive(true);
        Vector3 center = hitboxVisual.transform.position;
        Vector3 halfExtents = hitboxVisual.transform.lossyScale / 2f;
        Quaternion orientation = hitboxVisual.transform.rotation;

        Collider[] hitEnemies = Physics.OverlapBox(center, halfExtents, orientation, enemyLayer);
        AttackStep step = playerManager.weaponData.comboChain[playerManager.currentComboIndex - 1];
        CameraManager.Instance.StartCoroutine(CameraManager.Instance.ShakeCamera(step.cameraShakeIntensity, step.cameraShakeDuration));
        Debug.Log("Camera Shake Intensity: " + step.cameraShakeIntensity + ", Duration: " + step.cameraShakeDuration + ", Frequency: " + step.cameraShakeFrequency);
        float finalDamage = step.attackDamage * playerManager.TotalDamageMultiplier;
        float attackStunDuration = step.attackStunDuration;
        //chance for crit
        if (UnityEngine.Random.value < playerManager.FinalCritChance)
        {
            finalDamage *= 2;
            Debug.Log("<color=red>CRIT!</color>");
        }

        foreach (Collider enemy in hitEnemies)
        {
            // Apply finalDamage to enemy logic here...
            EnemyHurtbox enemyHurtbox = enemy.GetComponent<EnemyHurtbox>();
            if (enemyHurtbox != null)
            {
                enemyHurtbox.ReceiveDamage(finalDamage, attackStunDuration);
                EmotionEngine.Instance.RecordEnemyHit(finalDamage);
                // Spawn hit spark effect at the hitbox's position with a random rotation for visual variety
                // despawn after 1.5 seconds
                if (hitSparkPrefab != null)
                {
                    GameObject hitSpark = Instantiate(hitSparkPrefab, enemy.ClosestPoint(center), Quaternion.Euler(0, Random.Range(0, 360), 0));
                    Destroy(hitSpark, 1.5f);
                }
            }
            BossHurt bossHurt = enemy.GetComponent<BossHurt>();
            if (bossHurt != null)
            {
                bossHurt.HandleHurt(finalDamage);
                EmotionEngine.Instance.RecordEnemyHit(finalDamage);
                if (hitSparkPrefab != null)
                {
                    GameObject hitSpark = Instantiate(hitSparkPrefab, enemy.ClosestPoint(center), Quaternion.Euler(0, Random.Range(0, 360), 0));
                    Destroy(hitSpark, 1.5f);
                }
            }

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

    public void CancelAttackForDash()
    {
        if (!playerManager.isAttacking && playerManager.canAttack)
        {
            return;
        }

        if (hitboxVisual != null)
        {
            hitboxVisual.SetActive(false);
        }

        playerManager.canAttack = true;
        playerManager.isAttacking = false;
        playerManager.currentComboIndex = 0;
        playerManager.comboTime = 0f;
        startResetTime = false;

        if (playerVisuals != null)
        {
            playerVisuals.CancelAttackAnimation();
        }
    }

    private void ApplyAttackAssist(AttackStep step)
    {
        if (!TryGetAttackAssistTarget(step, out Collider targetCollider))
        {
            return;
        }

        Vector3 targetPosition = targetCollider.bounds.center;
        targetPosition.y = transform.position.y;

        Vector3 direction = targetPosition - transform.position;
        direction.y = 0f;

        if (direction.sqrMagnitude <= Mathf.Epsilon)
        {
            return;
        }

        Vector3 flatDirection = direction.normalized;
        transform.rotation = Quaternion.LookRotation(flatDirection);

        float stepDistance = GetAssistStepDistance(targetCollider, targetPosition, step);
        if (stepDistance <= 0f)
        {
            return;
        }

        Vector3 movement = flatDirection * stepDistance;
        if (playerController != null && playerController.enabled)
        {
            playerController.Move(movement);
        }
        else
        {
            transform.position += movement;
        }
    }

    private bool TryGetAttackAssistTarget(AttackStep step, out Collider targetCollider)
    {
        targetCollider = null;

        float searchRadius = step.attackRange + attackAssistMaxStepDistance + attackAssistSearchPadding;
        Collider[] candidates = enemyLayer.value != 0
            ? Physics.OverlapSphere(transform.position, searchRadius, enemyLayer, QueryTriggerInteraction.Collide)
            : Physics.OverlapSphere(transform.position, searchRadius, ~0, QueryTriggerInteraction.Collide);

        float bestScore = Mathf.Infinity;

        foreach (Collider candidate in candidates)
        {
            if (!candidate.CompareTag("Enemy") || candidate.GetComponent<EnemyHurtbox>() == null)
            {
                continue;
            }

            Vector3 targetPosition = candidate.bounds.center;
            targetPosition.y = transform.position.y;

            Vector3 toTarget = targetPosition - transform.position;
            toTarget.y = 0f;

            float centerDistance = toTarget.magnitude;
            if (centerDistance <= Mathf.Epsilon)
            {
                continue;
            }

            float surfaceDistance = Mathf.Max(0f, centerDistance - GetHorizontalBoundsRadius(candidate.bounds));
            float maxAssistedReach = step.attackRange + attackAssistMaxStepDistance;
            if (surfaceDistance > maxAssistedReach)
            {
                continue;
            }

            float facingScore = Vector3.Dot(transform.forward, toTarget / centerDistance);
            float score = surfaceDistance + ((1f - facingScore) * 0.25f);
            if (score < bestScore)
            {
                bestScore = score;
                targetCollider = candidate;
            }
        }

        return targetCollider != null;
    }

    private float GetAssistStepDistance(Collider targetCollider, Vector3 targetPosition, AttackStep step)
    {
        Vector3 toTarget = targetPosition - transform.position;
        toTarget.y = 0f;

        float centerDistance = toTarget.magnitude;
        float surfaceDistance = Mathf.Max(0f, centerDistance - GetHorizontalBoundsRadius(targetCollider.bounds));
        float desiredSurfaceDistance = Mathf.Max(0f, step.attackRange - attackAssistReachPadding);
        float reachGap = surfaceDistance - desiredSurfaceDistance;

        return Mathf.Clamp(reachGap, 0f, attackAssistMaxStepDistance);
    }

    private float GetHorizontalBoundsRadius(Bounds bounds)
    {
        return Mathf.Max(bounds.extents.x, bounds.extents.z);
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
