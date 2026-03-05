using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.InputSystem;

public class PlayerMovementManagement : MonoBehaviour
{
    [SerializeField] private DefaultMovementStats movementVariables;
    [SerializeField] private CharacterController playerController;
    [SerializeField] private new CinemachinePositionComposer camera;
    [SerializeField] private float verticalVelocityOffset = 0;

    /// <summary>
    /// Current velocity of the player, which will be updated based on player input and used to move the character controller. This variable will be modified in the MovePlayer method to create smooth acceleration and deceleration when the player starts or stops moving.
    /// </summary>
    private Vector3 currentVelocity;
    private Vector3 moveInput;



    /// <summary>
    /// Input Actions for player movement, jump, and sprint. These actions are defined in the Input System and will be used to read player inputs for movement, jumping, and sprinting.
    /// </summary>
    private InputAction moveAction;
    private InputAction jumpAction;
    private InputAction sprintAction;



    /// <summary>
    /// Reference to the PlayerInput component, which is used to access the input actions defined in the Input System. This variable will be initialized in the Start method and used to read player inputs in the ReadInputs method.
    /// </summary>
    private PlayerInput userInput;



    /// <summary>
    /// Vertical velocity of the player, which will be updated based on gravity and jumping. This variable will be modified in the Jump method when the player jumps and in the MovePlayer method to apply gravity when the player is in the air.
    /// </summary>
    private float verticalVelocity;
    private bool isSprinting;



    /// <summary>
    /// Temporary variables for acceleration and deceleration, which will be set based on whether the player is grounded or in the air. These variables will be used in the MovePlayer method to apply different acceleration and deceleration values depending on the player's state (grounded or airborne).
    /// </summary>
    float tempAccel;
    float tempDecel;
    bool isOnGround;


    private void Jump()
    {
        if (playerController.isGrounded)
        {
            verticalVelocity = Mathf.Sqrt(movementVariables.JumpHeight * 2f * movementVariables.gravity);
        }
    }

    private float GetCurrentSpeed()
    {
        if (sprintAction != null && sprintAction.IsPressed())
        {
            return movementVariables.sprintSpeed;
        }
        return movementVariables.movementSpeed;


    }

    private void MovePlayer()
    {
        Vector3 cameraForward = Camera.main.transform.forward;
        Vector3 cameraRight = Camera.main.transform.right;

        cameraForward.y = 0;
        cameraRight.y = 0;
        cameraForward.Normalize();
        cameraRight.Normalize();

        isOnGround = playerController.isGrounded;

        Vector3 moveDirection = (cameraForward * moveInput.y) + (cameraRight * moveInput.x);

        Vector3 targetVelocity = moveDirection * GetCurrentSpeed();

        if (isOnGround)
        {
            tempAccel = movementVariables.acceleration;
            tempDecel = movementVariables.deceleration;
        }
        else
        {
            tempAccel = movementVariables.airAcceleration;
            tempDecel = movementVariables.airDeceleration;
        }


        if (moveDirection.magnitude > 0.1f)
        {
            currentVelocity = Vector3.MoveTowards(currentVelocity, targetVelocity, tempAccel * Time.deltaTime);
        }
        else
        {
            currentVelocity = Vector3.MoveTowards(currentVelocity, Vector3.zero, tempDecel * Time.deltaTime);
        }

        if (playerController.isGrounded && verticalVelocity < 0)
        {

            if (verticalVelocityOffset > 0)
            {
                float temp = verticalVelocityOffset * -1f;
                verticalVelocityOffset = temp;
            }

            verticalVelocity = verticalVelocityOffset;

        }
        else
        {
            verticalVelocity -= movementVariables.gravity * Time.deltaTime;
        }

        Vector3 finalVelocity = currentVelocity + (Vector3.up * verticalVelocity);
        playerController.Move(finalVelocity * Time.deltaTime);
    }

    private void ReadInputs()
    {
        moveInput = moveAction.ReadValue<Vector2>();
        if (jumpAction.triggered)
        {
            Jump();
        }

        isSprinting = sprintAction.IsPressed();
        if (isSprinting)
        {
            Debug.Log("Sprinting");
        }
    }

    private void FOVChangeWhenRunning()
    {
        if (isSprinting)
        {
            camera.DeadZoneDepth = movementVariables.deadZone; // Adjust this value to increase/decrease the FOV change
            Debug.Log("FOV Change: " + camera.DeadZoneDepth);
        }
        else
        {
            camera.DeadZoneDepth = 0.0f;
            Debug.Log("FOV Reset: " + camera.DeadZoneDepth);
        }
    }

    // Start is called once
    // {} before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        userInput = GetComponent<PlayerInput>();
        moveAction = userInput.actions.FindAction("Move");
        jumpAction = userInput.actions.FindAction("Jump");
        sprintAction = userInput.actions.FindAction("Sprint");
        moveAction.Enable();
        jumpAction.Enable();
        sprintAction?.Enable();
    }

    // Update is called once per frame
    void Update()
    {
        //Every input detection of user input it will auto move in each frame
        ReadInputs();
        MovePlayer();
        FOVChangeWhenRunning();
    }
}