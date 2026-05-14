using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections;

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
    
    [Header("VFX")]
    public TrailRenderer dashTrail;

    public Vector2 moveInput { get; private set; }
    private Vector3 currentVelocity;
    private float verticalVelocity;
    private bool isSprinting;
    private bool isOnGround;
    public bool isDashing = false;
    private float lastDashTime;
    private PlayerInput userInput;
    private InputAction moveAction;
    private InputAction dashAction;
    private InputAction sprintAction;

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
        if (isDashing) return;

        ReadInputs();
        if (isDashing) return;

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

        isSprinting = sprintAction.IsPressed();
    }
    private bool CanDash()
    {
        // Subtract the reduction bonus from the base cooldown
        float actualCD = Mathf.Max(0.2f, movementVariables.dashCooldown - playerManager.cardDashCDReduction);
        return !isDashing && Time.time >= lastDashTime + actualCD;
    }
    private IEnumerator PerformDash()
    {
        weaponManager.HitboxOff();
        isDashing = true;
        lastDashTime = Time.time;
        int originalLayer = gameObject.layer;
        gameObject.layer = LayerMask.NameToLayer("DashingPlayer");

        if (dashTrail != null) dashTrail.emitting = true;

        Vector3 dashDir = CameraDirectionLogic.GetRelativeDirection(moveInput, Camera.main);
        if (dashDir.magnitude < 0.1f) dashDir = transform.forward;

        float totalDashSpeed = movementVariables.dashSpeed + playerManager.cardDashDistanceBonus;
        float dashDuration = movementVariables.dashDuration;
        float totalDashDistance = totalDashSpeed * dashDuration;

        // PHASE LOGIC: Check if the end position is clear.
        // If it is clear, we phase through everything in between.
        // If it's blocked, the obstacle is "thicker" than our dash, so we collide normally.
        bool canPhase = !IsDestinationBlocked(dashDir, totalDashDistance);

        float startTime = Time.time;
        while (Time.time < startTime + dashDuration)
        {
            Vector3 dashStep = dashDir * totalDashSpeed * Time.deltaTime;
            
            if (canPhase)
            {
                // Rely on the Physics Matrix (DashingPlayer vs Obstacles) to glide through.
                playerController.Move(dashStep);
            }
            else
            {
                // Manual collision check to stop in front of thick obstacles.
                CollisionFlags collisionFlags = MoveDashStep(dashStep);
                if ((collisionFlags & CollisionFlags.Sides) != 0) break;
            }

            yield return null;
        }

        if (dashTrail != null) dashTrail.emitting = false;
        gameObject.layer = originalLayer;
        isDashing = false;
        currentVelocity = dashDir * GetCurrentSpeed();
    }

    private bool IsDestinationBlocked(Vector3 direction, float distance)
    {
        Vector3 targetPos = transform.position + direction * distance;
        Vector3 center = targetPos + playerController.center;

        float radius = playerController.radius;
        float halfHeight = playerController.height * 0.5f;
        float offset = Mathf.Max(0, halfHeight - radius);
        
        // Offset the bottom slightly to avoid hitting the floor.
        float verticalBuffer = playerController.stepOffset;
        Vector3 top = center + Vector3.up * offset;
        Vector3 bottom = center - Vector3.up * (offset - verticalBuffer);
        
        Collider[] hits = Physics.OverlapCapsule(bottom, top, radius * 0.9f, dashCollisionMask, QueryTriggerInteraction.Ignore);
        
        foreach (var hit in hits)
        {
            if (hit.transform != transform && !hit.transform.IsChildOf(transform)) return true;
        }
        return false;
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

        RaycastHit[] hits = Physics.CapsuleCastAll(bottom, top, radius, direction, distance + dashCollisionBuffer, dashCollisionMask, QueryTriggerInteraction.Ignore);
        float closest = Mathf.Infinity;

        foreach (var hit in hits)
        {
            if (hit.collider.transform == transform || hit.collider.transform.IsChildOf(transform)) continue;
            if (hit.distance < closest) closest = hit.distance;
        }

        if (float.IsInfinity(closest)) return false;
        blockedDistance = closest;
        return true;
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
