using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerMovementManagement : MonoBehaviour
{
    [SerializeField] private DefaultMovementStats movementVariables;
    [SerializeField] private CharacterController playerController;
    [SerializeField] private new CinemachinePositionComposer camera;
    
    [Header("Movement Settings")]
    [SerializeField] private float rotationSpeed = 10f;
    
    public Vector2 moveInput { get; private set; }
    private Vector3 currentVelocity;
    private float verticalVelocity;
    private bool isSprinting;
    private bool isOnGround;

    private PlayerInput userInput;
    private InputAction moveAction;
    private InputAction jumpAction;
    private InputAction sprintAction;

    void Start()
    {
        userInput = GetComponent<PlayerInput>();
        
        // Initialize and Enable Actions
        moveAction = userInput.actions.FindAction("Move");
        jumpAction = userInput.actions.FindAction("Jump");
        sprintAction = userInput.actions.FindAction("Sprint");

        moveAction.Enable();
        jumpAction.Enable();
        sprintAction?.Enable();
    }

    void Update()
    {
        ReadInputs();
        MovePlayer();
        FOVChangeWhenRunning();
    }

    private void ReadInputs()
    {
        moveInput = moveAction.ReadValue<Vector2>();
        
        if (jumpAction.triggered)
        {
            Jump();
        }

        isSprinting = sprintAction.IsPressed();
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