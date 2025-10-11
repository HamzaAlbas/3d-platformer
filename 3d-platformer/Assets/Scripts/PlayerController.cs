using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(CharacterController))]
public class PlayerController : MonoBehaviour
{
    [Header("Movement")]
    public float moveSpeed = 5f;
    public Transform cameraTransform;
    public float rotationSpeed = 10f;
    
    [Header("Jump")]
    public float jumpHeight = 2f;
    public float gravity = -20f;

    //Components
    private CharacterController controller;
    private PlayerInput playerInput;

    //Input
    private InputAction moveAction;
    private Vector2 moveInput;
    private InputAction jumpAction;
    
    //Movement
    private Vector3 lastMovementDirection;
    private Vector3 velocity;
    private bool isGrounded;
    
    //State Machine
    public enum PlayerState
    {
        Idle,
        Moving,
        Jumping,
        Falling
    }
    
    [Header("Debug")]
    [SerializeField]private PlayerState currentState = PlayerState.Idle;
    
    private void Awake()
    {
        controller = GetComponent<CharacterController>();
        playerInput = GetComponent<PlayerInput>();

        moveAction = playerInput.actions["Move"];
        jumpAction = playerInput.actions["Jump"];
    }

    private void OnEnable()
    {
        moveAction.Enable();
        jumpAction.Enable();
    }

    private void OnDisable()
    {
        moveAction.Disable();
        jumpAction.Disable();
    }

    private void Update()
    {
        CheckGroundStatus();
        HandleInput();
        UpdateState();
        HandleState();
        ApplyGravity();
    }

    private void HandleInput()
    {
        moveInput = moveAction.ReadValue<Vector2>();
    }

    private void HandleMovement()
    {
        if (moveInput.magnitude < 0.1f)
        {
            return;
        }

        Vector3 movementDirection = GetCameraRelativeMovement();
        
        controller.Move(movementDirection * moveSpeed * Time.deltaTime);

        lastMovementDirection = movementDirection.normalized;
    }

    private Vector3 GetCameraRelativeMovement()
    {
        if (cameraTransform == null)
        {
            return new Vector3(moveInput.x, 0f, moveInput.y);
        }

        Vector3 cameraForward = cameraTransform.forward;
        Vector3 cameraRight = cameraTransform.right;

        cameraForward.y = 0f;
        cameraRight.y = 0f;
        cameraForward.Normalize();
        cameraRight.Normalize();
        
        Vector3 movementDirection = cameraRight * moveInput.x + cameraForward * moveInput.y;
        return movementDirection;
    }

    private void HandleRotation()
    {
        if (lastMovementDirection == Vector3.zero)
        {
            return;
        }
        
        Quaternion targetRotation = Quaternion.LookRotation(lastMovementDirection, Vector3.up);
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
    }

    private void UpdateState()
    {
        switch (currentState)
        {
            case PlayerState.Idle:
                if (jumpAction.triggered && isGrounded)
                {
                    PerformJump();
                    currentState = PlayerState.Jumping;
                }
                else if (!isGrounded && velocity.y < 0)
                {
                    currentState = PlayerState.Falling;
                }
                else if (moveInput.magnitude > 0.1f)
                {
                    currentState = PlayerState.Moving;
                }
                break;
            
            case PlayerState.Moving:
                if (jumpAction.triggered && isGrounded)
                {
                    PerformJump();
                    currentState = PlayerState.Jumping;
                }
                else if (!isGrounded && velocity.y < 0)
                {
                    currentState = PlayerState.Falling;
                }
                else if (moveInput.magnitude <= 0.1f)
                {
                    currentState = PlayerState.Idle;
                }
                break;
            case PlayerState.Jumping:
                if (velocity.y < 0)
                {
                    currentState = PlayerState.Falling;
                }
                break;
            case PlayerState.Falling:
                if (moveInput.magnitude > 0.1f)
                {
                    currentState = PlayerState.Moving;
                }
                else
                {
                    currentState = PlayerState.Idle;
                }
                break;
        }
    }

    private void HandleState()
    {
        switch (currentState)
        {
            case PlayerState.Idle:
                HandleIdle();
                break;
            case PlayerState.Moving:
                HandleMovement();
                HandleRotation();
                break;
            case PlayerState.Jumping:
                HandleJumping();
                break;
            case PlayerState.Falling:
                HandleFalling();
                break;
        }
    }

    private void HandleIdle()
    {
        
    }

    private void HandleJumping()
    {
        if (moveInput.magnitude > 0.1f)
        {
            HandleMovement();
            HandleRotation();
        }
    }

    private void HandleFalling()
    {
        if (moveInput.magnitude > 0.1f)
        {
            HandleMovement();
            HandleRotation();
        }
    }

    private void CheckGroundStatus()
    {
        isGrounded = controller.isGrounded;

        if (isGrounded && velocity.y < 0)
        {
            velocity.y = -2f;
        }
    }

    private void ApplyGravity()
    {
        velocity.y += gravity * Time.deltaTime;
        controller.Move(velocity * Time.deltaTime);
    }

    private void PerformJump()
    {
        velocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);
    }
}
