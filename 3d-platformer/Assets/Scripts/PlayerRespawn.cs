using System;
using UnityEngine;

public class PlayerRespawn : MonoBehaviour
{
    public Transform respawnPoint;
    public float fallThreshold = -15f;

    private CharacterController characterController;

    private void Awake()
    {
        characterController = GetComponent<CharacterController>();
    }

    void Update()
    {
        if (transform.position.y < fallThreshold)
        {
            RespawnPlayer();
        }
    }

    private void RespawnPlayer()
    {
        if (characterController != null)
        {
            characterController.enabled = false;
        }

        transform.position = respawnPoint.position;
        
        if (characterController != null)
        {
            characterController.enabled = true;
        }

    }
}
