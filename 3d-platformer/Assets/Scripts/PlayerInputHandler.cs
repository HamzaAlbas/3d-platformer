using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerInputHandler : MonoBehaviour
{
    [Header("Dependencies")]
    [SerializeField] private Transform cameraTransform;

    // Components
    private PlayerMotor motor;
    private PlayerJump jump;
    private PlayerDash dash;
    private PlayerGroundPound groundPound;

    // Input Actions
    private InputAction moveAction, jumpAction, sprintAction, dashAction, groundPoundAction;

    private void Awake()
    {
        motor = GetComponent<PlayerMotor>();
        jump = GetComponent<PlayerJump>();
        dash = GetComponent<PlayerDash>();
        groundPound = GetComponent<PlayerGroundPound>();

        var playerInput = GetComponent<PlayerInput>();
        moveAction = playerInput.actions["Move"];
        sprintAction = playerInput.actions["Sprint"];
        jumpAction = playerInput.actions["Jump"];
        dashAction = playerInput.actions["Dash"];
        groundPoundAction = playerInput.actions["GroundPound"];
    }

    private void OnEnable()
    {
        moveAction.Enable(); sprintAction.Enable(); jumpAction.Enable();
        dashAction.Enable(); groundPoundAction.Enable();
    }

    private void OnDisable()
    {
        moveAction.Disable(); sprintAction.Disable(); jumpAction.Disable();
        dashAction.Disable(); groundPoundAction.Disable();
    }
    
    private void Update()
    {
        if (motor.IsMovementLocked) return;

        // Movement & Rotation
        Vector2 moveInput = moveAction.ReadValue<Vector2>();
        Vector3 moveDirection = GetCameraRelativeMovement(moveInput);

        float speed;
        if (motor.IsGrounded && sprintAction.IsPressed())
        {
             speed = jump.SprintSpeed;
        }
        else
        {
            speed = jump.MoveSpeed;
        }
        
        motor.SetHorizontalVelocity(moveDirection * speed);
        motor.RotatePlayer(moveDirection);

        // Abilities
        if (jumpAction.triggered) jump.TryJump();
        if (dashAction.triggered) dash.TryDash(moveDirection);
        if (groundPoundAction.triggered) groundPound.TryGroundPound();
    }

    private Vector3 GetCameraRelativeMovement(Vector2 moveInput)
    {
        if (moveInput.magnitude < 0.1f) return Vector3.zero;

        Vector3 forward = cameraTransform.forward;
        Vector3 right = cameraTransform.right;
        forward.y = 0; right.y = 0;
        forward.Normalize(); right.Normalize();
        return (right * moveInput.x + forward * moveInput.y).normalized;
    }
}