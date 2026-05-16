using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections;
using System.Collections.Generic;

public class PlayerMovementManagement : MonoBehaviour
{
    [SerializeField] private DefaultMovementStats movementVariables;
    [SerializeField] private CharacterController playerController;
    [SerializeField] private new CinemachinePositionComposer camera;
    [SerializeField] private PlayerManager playerManager;
    [SerializeField] private WeaponManager weaponManager;

    [Header("Movement Settings")]
    [SerializeField] private float rotationSpeed = 10f;
    [SerializeField] private LayerMask dashCollisionMask = ~0; // Ensure this excludes the Player layer
    [SerializeField] private float dashCollisionBuffer = 0.05f;

    [Header("Hazard Detection")]
    [SerializeField] private LayerMask dashHazardMask = ~0;
    [SerializeField] private float dashHazardCheckBuffer = 0.1f;
    
    [Header("VFX")]
    public TrailRenderer dashTrail;

    public Vector2 moveInput { get; private set; }
    private Vector3 currentVelocity;
    private float verticalVelocity;
    private bool isSprinting;
    private bool isOnGround;
    public bool isDashing = false;
    public bool isKnockedBack { get; private set; }
    private float lastDashTime;
    private Coroutine knockbackRoutine;
    private float nextHazardKnockbackTime;
    private readonly Collider[] dashHazardResults = new Collider[16];
    private readonly List<Collider> ignoredDashColliders = new List<Collider>();
    private readonly HashSet<Collider> ignoredDashColliderSet = new HashSet<Collider>();
    private int activeDashOriginalLayer = -1;
    private PlayerInput userInput;
    private InputAction moveAction;
    private InputAction dashAction;
    private InputAction sprintAction;

    private void Awake()
    {
        int dashingPlayerLayer = LayerMask.NameToLayer("DashingPlayer");
        int enemyLayer = LayerMask.NameToLayer("Enemy");

        if (dashingPlayerLayer >= 0 && enemyLayer >= 0)
        {
            Physics.IgnoreLayerCollision(dashingPlayerLayer, enemyLayer, true);
        }
    }

    private void OnDisable()
    {
        RestoreDashPassThroughCollisions();

        if (activeDashOriginalLayer >= 0)
        {
            gameObject.layer = activeDashOriginalLayer;
            activeDashOriginalLayer = -1;
        }
    }

    void Start()
    {

        userInput = GetComponent<PlayerInput>();
        if (weaponManager == null)
        {
            weaponManager = GetComponent<WeaponManager>();
        }

        // Initialize and Enable Actions
        moveAction = userInput.actions.FindAction("Move");
        dashAction = userInput.actions.FindAction("Dash");
        sprintAction = userInput.actions.FindAction("Sprint");

        moveAction.Enable();
        dashAction.Enable();
        sprintAction?.Enable();
        dashTrail.emitting = false;
    }

    void Update()
    {
        if (isDashing || isKnockedBack) return;

        ReadInputs();
        if (isDashing || isKnockedBack) return;

        if (playerManager.isAttacking)
        {
            currentVelocity = Vector3.zero;
            return;
        }

        MovePlayer();
        FOVChangeWhenRunning();
    }

    private void ReadInputs()
    {
        moveInput = moveAction.ReadValue<Vector2>();

        if (dashAction.triggered && CanDash())
        {
            if (playerManager.isAttacking && weaponManager != null)
            {
                weaponManager.CancelAttackForDash();
            }

            StartCoroutine(PerformDash());
        }

    }
    private bool CanDash()
    {
        // Subtract the reduction bonus from the base cooldown
        float actualCD = Mathf.Max(0.2f, movementVariables.dashCooldown - playerManager.cardDashCDReduction);
        return !isDashing && Time.time >= lastDashTime + actualCD;
    }
    private IEnumerator PerformDash()
    {
        weaponManager?.HitboxOff();
        isDashing = true;
        lastDashTime = Time.time;
        int originalLayer = gameObject.layer;
        activeDashOriginalLayer = originalLayer;
        int dashingPlayerLayer = LayerMask.NameToLayer("DashingPlayer");
        if (dashingPlayerLayer >= 0)
        {
            gameObject.layer = dashingPlayerLayer;
        }

        if (dashTrail != null) dashTrail.emitting = true;

        Vector3 dashDir = CameraDirectionLogic.GetRelativeDirection(moveInput, Camera.main);
        if (dashDir.magnitude < 0.1f) dashDir = transform.forward;

        float totalDashSpeed = movementVariables.dashSpeed + playerManager.cardDashDistanceBonus;
        float dashDuration = movementVariables.dashDuration;
        float totalDashDistance = totalDashSpeed * dashDuration;
        PrepareDashPassThroughCollisions(dashDir, totalDashDistance + dashCollisionBuffer);

        float startTime = Time.time;
        while (Time.time < startTime + dashDuration && !isKnockedBack)
        {
            Vector3 dashStep = dashDir * totalDashSpeed * Time.deltaTime;

            if (TryTriggerDashHazard(dashDir, dashStep.magnitude + dashHazardCheckBuffer))
            {
                break;
            }

            PrepareDashPassThroughCollisions(dashDir, dashStep.magnitude + dashCollisionBuffer);
            CollisionFlags collisionFlags = MoveDashStep(dashStep);
            if ((collisionFlags & CollisionFlags.Sides) != 0) break;

            yield return null;
        }

        if (dashTrail != null) dashTrail.emitting = false;
        RestoreDashPassThroughCollisions();
        gameObject.layer = originalLayer;
        activeDashOriginalLayer = -1;
        isDashing = false;
        currentVelocity = isKnockedBack ? Vector3.zero : dashDir * GetCurrentSpeed();
    }

    public void ApplyKnockback(Vector3 direction, float distance, float duration)
    {
        TryStartKnockback(direction, distance, duration);
    }

    public bool TryApplyHazardKnockback(Vector3 direction, float distance, float duration, float cooldown, bool ignoreCooldown = false)
    {
        if (isKnockedBack || (!ignoreCooldown && Time.time < nextHazardKnockbackTime))
        {
            return false;
        }

        if (!TryStartKnockback(direction, distance, duration))
        {
            return false;
        }

        nextHazardKnockbackTime = Time.time + Mathf.Max(cooldown, duration);
        return true;
    }

    private bool TryStartKnockback(Vector3 direction, float distance, float duration)
    {
        if (!isActiveAndEnabled || playerController == null)
        {
            return false;
        }

        direction.y = 0f;
        if (direction.sqrMagnitude <= 0.0001f)
        {
            return false;
        }

        if (knockbackRoutine != null)
        {
            StopCoroutine(knockbackRoutine);
        }

        knockbackRoutine = StartCoroutine(KnockbackCoroutine(direction.normalized, distance, duration));
        return true;
    }

    private IEnumerator KnockbackCoroutine(Vector3 direction, float distance, float duration)
    {
        isKnockedBack = true;
        currentVelocity = Vector3.zero;

        float elapsed = 0f;
        float safeDuration = Mathf.Max(0.01f, duration);
        float speed = Mathf.Max(0f, distance) / safeDuration;

        while (elapsed < safeDuration)
        {
            float remainingTime = safeDuration - elapsed;
            float stepTime = Mathf.Min(Time.deltaTime, remainingTime);
            playerController.Move(direction * speed * stepTime);
            elapsed += stepTime;
            yield return null;
        }

        isKnockedBack = false;
        knockbackRoutine = null;
    }

    private CollisionFlags MoveDashStep(Vector3 movement)
    {
        if (movement.sqrMagnitude <= Mathf.Epsilon) return CollisionFlags.None;
        Vector3 direction = movement.normalized;
        float distance = movement.magnitude;

        if (TryGetDashBlockedDistance(direction, distance, out float blockedDistance))
        {
            float safeDistance = Mathf.Max(0f, blockedDistance - dashCollisionBuffer);
            if (safeDistance > 0f) playerController.Move(direction * safeDistance);
            return CollisionFlags.Sides;
        }
        return playerController.Move(movement);
    }

    private bool TryGetDashBlockedDistance(Vector3 direction, float distance, out float blockedDistance)
    {
        blockedDistance = 0f;
        Vector3 center = transform.TransformPoint(playerController.center);
        float radius = Mathf.Max(0.01f, playerController.radius);
        float halfHeight = Mathf.Max(radius, playerController.height * 0.5f);
        Vector3 bottom = center - Vector3.up * (halfHeight - radius);
        Vector3 top = center + Vector3.up * (halfHeight - radius);

        RaycastHit[] hits = Physics.CapsuleCastAll(bottom, top, radius, direction, distance + dashCollisionBuffer, GetDashCollisionMask(), QueryTriggerInteraction.Ignore);
        float closest = Mathf.Infinity;

        foreach (var hit in hits)
        {
            if (ShouldIgnoreDashCollision(hit.collider)) continue;
            if (hit.distance < closest) closest = hit.distance;
        }

        if (float.IsInfinity(closest)) return false;
        blockedDistance = closest;
        return true;
    }

    private bool TryTriggerDashHazard(Vector3 incomingDirection, float castDistance)
    {
        if (playerManager == null || playerController == null)
        {
            return false;
        }

        if (TryTriggerOverlappingHazard(incomingDirection))
        {
            return true;
        }

        if (castDistance <= 0f || incomingDirection.sqrMagnitude <= 0.0001f)
        {
            return false;
        }

        GetControllerCapsuleAt(transform.position, out Vector3 bottom, out Vector3 top, out float radius);
        RaycastHit[] hits = Physics.CapsuleCastAll(bottom, top, radius, incomingDirection.normalized, castDistance, dashHazardMask, QueryTriggerInteraction.Collide);
        LazerKnockback closestHazard = null;
        DmgArea closestDamageArea = null;
        float closestDistance = Mathf.Infinity;
        float closestDamageDistance = Mathf.Infinity;

        foreach (RaycastHit hit in hits)
        {
            if (hit.collider == null || !hit.collider.isTrigger || IsOwnCollider(hit.collider))
            {
                continue;
            }

            DmgArea damageArea = hit.collider.GetComponentInParent<DmgArea>();
            if (damageArea != null && hit.distance < closestDamageDistance)
            {
                closestDamageArea = damageArea;
                closestDamageDistance = hit.distance;
            }

            LazerKnockback hazard = hit.collider.GetComponentInParent<LazerKnockback>();
            if (hazard == null || hit.distance >= closestDistance)
            {
                continue;
            }

            closestHazard = hazard;
            closestDistance = hit.distance;
        }

        closestDamageArea?.TryApplyDashDamage(playerManager);
        return closestHazard != null && closestHazard.TryKnockback(playerManager, incomingDirection, true);
    }

    private bool TryTriggerOverlappingHazard(Vector3 incomingDirection)
    {
        GetControllerCapsuleAt(transform.position, out Vector3 bottom, out Vector3 top, out float radius);
        int hitCount = Physics.OverlapCapsuleNonAlloc(bottom, top, radius, dashHazardResults, dashHazardMask, QueryTriggerInteraction.Collide);
        LazerKnockback closestHazard = null;
        DmgArea closestDamageArea = null;
        float closestDistance = Mathf.Infinity;
        float closestDamageDistance = Mathf.Infinity;
        Vector3 center = transform.TransformPoint(playerController.center);

        for (int i = 0; i < hitCount; i++)
        {
            Collider hit = dashHazardResults[i];
            dashHazardResults[i] = null;

            if (hit == null || !hit.isTrigger || IsOwnCollider(hit))
            {
                continue;
            }

            DmgArea damageArea = hit.GetComponentInParent<DmgArea>();
            float distance = (hit.ClosestPoint(center) - center).sqrMagnitude;

            if (damageArea != null && distance < closestDamageDistance)
            {
                closestDamageArea = damageArea;
                closestDamageDistance = distance;
            }

            LazerKnockback hazard = hit.GetComponentInParent<LazerKnockback>();
            if (hazard == null)
            {
                continue;
            }

            if (distance >= closestDistance)
            {
                continue;
            }

            closestHazard = hazard;
            closestDistance = distance;
        }

        closestDamageArea?.TryApplyDashDamage(playerManager);
        return closestHazard != null && closestHazard.TryKnockback(playerManager, incomingDirection, true);
    }

    private void GetControllerCapsuleAt(Vector3 position, out Vector3 bottom, out Vector3 top, out float radius)
    {
        Vector3 center = position + transform.TransformVector(playerController.center);
        radius = Mathf.Max(0.01f, playerController.radius);
        float halfHeight = Mathf.Max(radius, playerController.height * 0.5f);
        float verticalOffset = halfHeight - radius;

        bottom = center - Vector3.up * verticalOffset;
        top = center + Vector3.up * verticalOffset;
    }

    private bool IsOwnCollider(Collider hit)
    {
        return hit.transform == transform || hit.transform.IsChildOf(transform);
    }

    private int GetDashCollisionMask()
    {
        int mask = dashCollisionMask.value;
        RemoveLayerFromMask(ref mask, gameObject.layer);
        RemoveLayerFromMask(ref mask, LayerMask.NameToLayer("Player"));
        RemoveLayerFromMask(ref mask, LayerMask.NameToLayer("DashingPlayer"));
        RemoveLayerFromMask(ref mask, LayerMask.NameToLayer("Enemy"));
        return mask;
    }

    private static void RemoveLayerFromMask(ref int mask, int layer)
    {
        if (layer < 0) return;
        mask &= ~(1 << layer);
    }

    private bool ShouldIgnoreDashCollision(Collider hit)
    {
        if (hit == null || IsOwnCollider(hit))
        {
            return true;
        }

        return IsEnemyCollider(hit);
    }

    private static bool IsEnemyCollider(Collider hit)
    {
        return hit.CompareTag("Enemy") || hit.GetComponentInParent<EnemyController>() != null;
    }

    private void PrepareDashPassThroughCollisions(Vector3 direction, float distance)
    {
        if (playerController == null)
        {
            return;
        }

        GetControllerCapsuleAt(transform.position, out Vector3 bottom, out Vector3 top, out float radius);
        Collider[] overlappingHits = Physics.OverlapCapsule(bottom, top, radius, ~0, QueryTriggerInteraction.Collide);
        for (int i = 0; i < overlappingHits.Length; i++)
        {
            TryIgnoreDashCollider(overlappingHits[i]);
        }

        if (distance <= 0f || direction.sqrMagnitude <= 0.0001f)
        {
            return;
        }

        RaycastHit[] pathHits = Physics.CapsuleCastAll(bottom, top, radius, direction.normalized, distance, ~0, QueryTriggerInteraction.Collide);
        for (int i = 0; i < pathHits.Length; i++)
        {
            TryIgnoreDashCollider(pathHits[i].collider);
        }
    }

    private void TryIgnoreDashCollider(Collider hit)
    {
        if (hit == null || IsOwnCollider(hit) || !IsEnemyCollider(hit) || !ignoredDashColliderSet.Add(hit))
        {
            return;
        }

        Physics.IgnoreCollision(playerController, hit, true);
        ignoredDashColliders.Add(hit);
    }

    private void RestoreDashPassThroughCollisions()
    {
        for (int i = 0; i < ignoredDashColliders.Count; i++)
        {
            Collider ignoredCollider = ignoredDashColliders[i];
            if (ignoredCollider != null && playerController != null)
            {
                Physics.IgnoreCollision(playerController, ignoredCollider, false);
            }
        }

        ignoredDashColliders.Clear();
        ignoredDashColliderSet.Clear();
    }

    private void MovePlayer()
    {
        // 1. Get the direction relative to your 2.5D camera view using your static logic
        Vector3 moveDirection = CameraDirectionLogic.GetRelativeDirection(moveInput, Camera.main);
        Vector3 targetVelocity = moveDirection * GetCurrentSpeed();

        isOnGround = playerController.isGrounded;

        // 2. Pick acceleration/deceleration based on ground state
        float accel = isOnGround ? movementVariables.acceleration : movementVariables.airAcceleration;
        float decel = isOnGround ? movementVariables.deceleration : movementVariables.airDeceleration;

        // 3. Handle movement and rotation
        if (moveDirection.magnitude > 0.1f)
        {
            currentVelocity = Vector3.MoveTowards(currentVelocity, targetVelocity, accel * Time.deltaTime);
            RotateTowards(moveDirection);
        }
        else
        {
            currentVelocity = Vector3.MoveTowards(currentVelocity, Vector3.zero, decel * Time.deltaTime);
        }

        // 4. Apply vertical forces
        ApplyGravity();

        // 5. Final Move
        Vector3 finalVelocity = currentVelocity + (Vector3.up * verticalVelocity);
        playerController.Move(finalVelocity * Time.deltaTime);

        bool isMoving = moveDirection.magnitude > 0.1f;
        bool isIdle = !isMoving && !playerManager.isAttacking && !isDashing;
        EmotionEngine.Instance.RecordMovement(currentVelocity.magnitude, isMoving, isIdle);
    }

    private void RotateTowards(Vector3 direction)
    {
        Quaternion targetRotation = Quaternion.LookRotation(direction);
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
    }

    private void Jump()
    {
        if (playerController.isGrounded)
        {
            verticalVelocity = Mathf.Sqrt(movementVariables.JumpHeight * 2f * movementVariables.gravity);
        }
    }

    private void ApplyGravity()
    {
        if (playerController.isGrounded && verticalVelocity < 0)
        {
            verticalVelocity = -2f; // Snap to ground
        }
        else
        {
            verticalVelocity -= movementVariables.gravity * Time.deltaTime;
        }
    }

    private float GetCurrentSpeed()
    {
        return isSprinting ? movementVariables.sprintSpeed : movementVariables.movementSpeed;
    }

    private void FOVChangeWhenRunning()
    {
        camera.DeadZoneDepth = isSprinting ? movementVariables.deadZone : 0.0f;
    }
}
