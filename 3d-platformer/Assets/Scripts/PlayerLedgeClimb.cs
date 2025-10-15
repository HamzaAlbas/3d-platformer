using UnityEngine;
using System.Collections;

[RequireComponent(typeof(PlayerMotor), typeof(CharacterController))]
public class PlayerLedgeClimb : MonoBehaviour
{
    #region Settings

    [Header("Settings")]
    [SerializeField] private float ledgeClimbDuration = 1.0f;
    
    #endregion

    #region Internal State

    private PlayerMotor motor;
    private Animator animator;
    private readonly int ledgeClimbHash = Animator.StringToHash("LedgeClimb");
    private CharacterController controller;
    
    #endregion

    private void Awake()
    {
        motor = GetComponent<PlayerMotor>();
        controller = GetComponent<CharacterController>();
        animator = GetComponent<Animator>();
    }

    /// <summary>
    /// Handles triggering when entering a ledge collider
    /// Processes ledge position and rotation if conditions are met
    /// </summary>
    private void OnTriggerEnter(Collider other)
    {
        // Skip processing if movement is locked or player is already grounded
        if (motor.IsMovementLocked || motor.IsGrounded) return;

        // Check if the collider is a valid ledge trigger
        if (other.TryGetComponent<LedgeTrigger>(out LedgeTrigger ledge))
        {
            Transform ledgeTransform = ledge.transform;

            // Calculate position vector from ledge center to player
            Vector3 playerPos = transform.position;
            Vector3 playerToLedgeCenter = playerPos - ledgeTransform.position;

            // Determine horizontal alignment along ledge's right axis
            float dot = Vector3.Dot(playerToLedgeCenter, ledgeTransform.right);
            float ledgeWidth = ledgeTransform.localScale.x / 2f;
            dot = Mathf.Clamp(dot, -ledgeWidth, ledgeWidth);

            // Find the closest point on ledge surface
            Vector3 closestPointOnLedge = ledgeTransform.position + ledgeTransform.right * dot;

            // Calculate final target position (including climb offset)
            Vector3 finalTargetPosition = closestPointOnLedge + ledgeTransform.TransformDirection(ledge.climbUpOffset);
            Quaternion finalTargetRotation = Quaternion.LookRotation(ledgeTransform.forward);

            // Begin climbing sequence
            StartCoroutine(PerformLedgeClimb(finalTargetPosition, finalTargetRotation));
        }
    }

    /// <summary>
    /// Handles physical movement during ledge climb animation
    /// - Disables physics during climb
    /// - Interpolates position and rotation to target
    /// - Finalizes climb when completed
    /// </summary>
    private IEnumerator PerformLedgeClimb(Vector3 targetPos, Quaternion targetRot)
    {
        // Enable climbing state and trigger animation
        motor.SetState(PlayerMotor.PlayerState.LedgeClimbing);
        animator.SetTrigger(ledgeClimbHash);

        Vector3 startPos = transform.position;
        float elapsedTime = 0f;
        controller.enabled = false; // Temporarily disable physics during climb

        // Smoothly transition to target position/rotation
        while (elapsedTime < ledgeClimbDuration)
        {
            transform.position = Vector3.Lerp(startPos, targetPos, elapsedTime / ledgeClimbDuration);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, elapsedTime / ledgeClimbDuration);
            elapsedTime += Time.deltaTime;
            yield return null;
        }

        // Ensure exact target position/rotation at completion
        transform.position = targetPos;
        transform.rotation = targetRot;
        EndLedgeClimb(); // Finalize climb sequence
    }

    /// <summary>
    /// Resets movement constraints and returns to idle state
    /// </summary>
    public void EndLedgeClimb()
    {
        controller.enabled = true; // Re-enable physics
        motor.SetHorizontalVelocity(Vector3.zero); // Reset horizontal movement
        motor.SetVerticalVelocity(0); // Reset vertical movement
        motor.SetState(PlayerMotor.PlayerState.Idle); // Return to default state
    }
}