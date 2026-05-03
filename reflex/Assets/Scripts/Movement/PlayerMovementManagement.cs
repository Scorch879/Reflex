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

    [Header("Movement Settings")]
    [SerializeField] private float rotationSpeed = 10f;

    public Vector2 moveInput { get; private set; }
    private Vector3 currentVelocity;
    private float verticalVelocity;
    private bool isSprinting;
    private bool isOnGround;
    private bool isDashing = false;
    private float lastDashTime;
    private PlayerInput userInput;
    private InputAction moveAction;
    private InputAction dashAction;
    private InputAction sprintAction;

    void Start()
    {

        userInput = GetComponent<PlayerInput>();

        // Initialize and Enable Actions
        moveAction = userInput.actions.FindAction("Move");
        dashAction = userInput.actions.FindAction("Dash");
        sprintAction = userInput.actions.FindAction("Sprint");

        moveAction.Enable();
        dashAction.Enable();
        sprintAction?.Enable();
    }

    void Update()
    {
        if (isDashing) return;
        if (playerManager.isAttacking)
        {
            currentVelocity = Vector3.zero;
            return;
        }
        ReadInputs();
        MovePlayer();
        FOVChangeWhenRunning();
    }

    private void ReadInputs()
    {
        moveInput = moveAction.ReadValue<Vector2>();

        if (dashAction.triggered && CanDash())
        {
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
        isDashing = true;
        lastDashTime = Time.time;

        Vector3 dashDir = CameraDirectionLogic.GetRelativeDirection(moveInput, Camera.main);
        if (dashDir.magnitude < 0.1f) dashDir = transform.forward;

        float startTime = Time.time;
        // Add the distance bonus to the base speed[cite: 1, 2]
        float totalDashSpeed = movementVariables.dashSpeed + playerManager.cardDashDistanceBonus;

        while (Time.time < startTime + movementVariables.dashDuration)
        {
            playerController.Move(dashDir * totalDashSpeed * Time.deltaTime);
            yield return null;
        }

        isDashing = false;
        currentVelocity = dashDir * GetCurrentSpeed();
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