using System;
using UnityEngine;

[RequireComponent(typeof(PlayerMotor))]
public class PlayerJump : MonoBehaviour
{
    #region Events and Configurations

    public event Action OnJump;
    public event Action OnDoubleJump;

    [Header("Jump Settings")]
    public float MoveSpeed = 5f;
    public float SprintSpeed = 8f;
    [SerializeField] private float jumpHeight = 2f;
    [SerializeField, Range(0f, 2f)] private float airMomentumMultiplier = 0.8f;
    [SerializeField] private bool canDoubleJump = true;
    
    #endregion

    #region Internal State
    
    private PlayerMotor motor;
    private Animator animator;
    private readonly int doubleJumpHash = Animator.StringToHash("DoubleJump");
    private bool hasDoubleJumped;
    private float gravity;
    
    #endregion

    private void Awake()
    {
        motor = GetComponent<PlayerMotor>();
        gravity = motor.gravity;
        animator = GetComponent<Animator>();
    }

    /// <summary>
    /// Handles jump attempt based on current state:
    /// - Grounded: perform standard jump
    /// - Airborne + double jump enabled: perform double jump
    /// </summary>
    public void TryJump()
    {
        if (motor.IsGrounded)
        {
            PerformJump();
        }
        else if (canDoubleJump && !hasDoubleJumped)
        {
            PerformDoubleJump();
        }
    }

    /// <summary>
    /// Calculates and applies vertical force for standard ground jump
    /// Also applies horizontal momentum reduction using airMomentumMultiplier
    /// </summary>
    private void PerformJump()
    {
        float jumpForce = Mathf.Sqrt(jumpHeight * -2f * gravity);
        motor.SetVerticalVelocity(jumpForce);
        motor.SetHorizontalVelocity(motor.HorizontalVelocity * airMomentumMultiplier);
        OnJump?.Invoke();
    }

    /// <summary>
    /// Handles double jump execution with special animation trigger
    /// </summary>
    private void PerformDoubleJump()
    {
        hasDoubleJumped = true;
        float jumpForce = Mathf.Sqrt(jumpHeight * -2f * gravity);
        motor.SetVerticalVelocity(jumpForce);
        animator.SetTrigger(doubleJumpHash);
        OnDoubleJump?.Invoke();
    }

    /// <summary>
    /// Resets double jump capability when player lands on ground
    /// </summary>
    private void Update()
    {
        if (motor.IsGrounded)
        {
            hasDoubleJumped = false;
        }
    }
}