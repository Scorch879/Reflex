using System;
using UnityEngine;

public class PlayerAnimation : MonoBehaviour
{
    [SerializeField] private Animator playerAnim;
    [SerializeField] private PlayerMovementManagement playerMovement;
    private bool isRunning = false;
    private bool lastState = false;

    private void Update()
    {
        MoveState();
    }

    private void MoveState()
    {
        isRunning = playerMovement.moveInput.magnitude > 0.1f;
        if (isRunning && !lastState)
        {
            playerAnim.SetTrigger("run");
            lastState = true;
        }
        else if(!isRunning && lastState)
        {
            playerAnim.SetTrigger("runToIdle");
            isRunning = false;
            lastState = false;
        }
    }   
}
