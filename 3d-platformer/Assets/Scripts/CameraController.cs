using System;
using UnityEngine;
using UnityEngine.InputSystem;

public class CameraController : MonoBehaviour
{
    //References
    public Transform target;
    public float distance = 5f;
    public float cameraHeight = 2f;
    public float sensitivity = 100f;
    public float positionSmoothTime = 0.001f;
    public Vector2 pitchMinMax = new(-20, 70);
    
    //Private variables
    private float yaw;
    private float pitch;
    private Vector3 currentVelocity;
    
    //Input System
    private PlayerInput playerInput;
    private InputAction lookAction;

    private void Awake()
    {
        if (target != null)
        {
            playerInput = target.GetComponent<PlayerInput>();
        }

        lookAction = playerInput.actions["Look"];
    }

    private void OnEnable()
    {
        lookAction.Enable();
        
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    private void OnDisable()
    {
        lookAction.Disable();
        
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    private void LateUpdate()
    {
        if (target == null) return;
        
        Vector2 lookInput = lookAction.ReadValue<Vector2>();

        yaw += lookInput.x * sensitivity * Time.deltaTime;
        pitch -= lookInput.y * sensitivity * Time.deltaTime;

        pitch = Mathf.Clamp(pitch, pitchMinMax.x, pitchMinMax.y);
        
        Quaternion desiredRotation = Quaternion.Euler(pitch, yaw, 0);

        Vector3 direction = Vector3.forward;
        Vector3 targetPosition = new Vector3(target.position.x, cameraHeight, target.position.z) -
                                 (desiredRotation * direction * distance);

        transform.position =
            Vector3.SmoothDamp(transform.position, targetPosition, ref currentVelocity, positionSmoothTime);

        transform.rotation = desiredRotation;
    }
}
