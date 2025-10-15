using UnityEngine;
using System;

[RequireComponent(typeof(PlayerMotor))]
public class PlayerGroundPound : MonoBehaviour
{
    #region Events

    public event Action OnGroundPoundLand;
    
    #endregion

    #region Settings

    [Header("Settings")]
    [SerializeField] private bool canGroundPound = true;
    [SerializeField] private float groundPoundForce = -30f;
    
    #endregion

    #region Internal State

    private PlayerMotor motor;
    private Animator animator;
    private readonly int groundPoundHash = Animator.StringToHash("GroundPound");
    private bool hasLandedFromPound;
    
    #endregion

    private void Awake()
    {
        motor = GetComponent<PlayerMotor>();
        animator = GetComponent<Animator>();
    }

    /// <summary>
    /// Initiates ground pound sequence when conditions are met:
    /// - Player is allowed to ground pound
    /// - Player is airborne (not grounded)
    /// </summary>
    public void TryGroundPound()
    {
        if (canGroundPound && !motor.IsGrounded)
        {
            // Enter ground pounding state
            motor.SetState(PlayerMotor.PlayerState.GroundPounding);
            
            // Trigger animation event
            animator.SetTrigger(groundPoundHash);
            
            // Reset landing flag for event triggering
            hasLandedFromPound = false;
            
            // Reset horizontal momentum
            motor.SetHorizontalVelocity(Vector3.zero);
            
            // Apply downward force for ground pound
            motor.SetVerticalVelocity(groundPoundForce);
        }
    }

    /// <summary>
    /// Detects landing during ground pound sequence
    /// - Checks if player is grounded
    /// - Triggers landing event exactly once per sequence
    /// </summary>
    private void Update()
    {
        // Only process when in GroundPounding state, grounded, and not yet registered landing
        if (motor.CurrentState == PlayerMotor.PlayerState.GroundPounding && 
            motor.IsGrounded && 
            !hasLandedFromPound)
        {
            // Mark landing has occurred
            hasLandedFromPound = true;
            
            // Notify listeners of landing event
            OnGroundPoundLand?.Invoke();
        }
    }

    /// <summary>
    /// Called by animation event handler after ground pound completes
    /// Resets motor state to default Idle state
    /// </summary>
    public void EndGroundPound()
    {
        motor.SetState(PlayerMotor.PlayerState.Idle);
    }
}