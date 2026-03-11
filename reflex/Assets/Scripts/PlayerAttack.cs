using System;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerAttack : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Animator playerAnim;

    private PlayerInput userInput;
    private InputAction attackAction;
    private bool isAttacking;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        userInput = GetComponent<PlayerInput>();
        attackAction = userInput.actions.FindAction("Attack");
        attackAction?.Enable();
    }

    // Update is called once per frame
    void Update()
    {
        if (attackAction != null && attackAction.triggered)
        {
            PerformAttack();
        }
    }

    private void PerformAttack()
    {
        Debug.Log("Attack triggered!");
        
        if (playerAnim != null)
        {
            // Make sure you have a Trigger parameter named "attack" in your Animator Controller
            playerAnim.SetTrigger("attack");
        }

        // TODO: Add damage logic here (e.g., Physics.OverlapSphere to detect enemies)
    }
}
