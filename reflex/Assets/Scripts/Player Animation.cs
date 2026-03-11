using System;
using UnityEngine;

public class PlayerAnimation : MonoBehaviour
{
    [SerializeField] private Animator playerAnim;
    [SerializeField] private PlayerMovementManagement playerMovement;
   
    private void Update()
    {
        MoveState();
    }

   private void MoveState()
    {
        bool isRunning = playerMovement.moveInput.magnitude > 0.1f;
        playerAnim.SetBool("isRunning", isRunning);
    }

}
