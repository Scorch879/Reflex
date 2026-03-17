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

    /// <summary>
    /// Swaps the animation clips to match the currently equipped weapon.
    /// Called by WeaponManager at the start of a run.
    /// </summary>
    public void SwapWeaponAnimations(AnimatorOverrideController newOverride)
    {
        if (newOverride != null && playerAnim != null)
        {
            playerAnim.runtimeAnimatorController = newOverride;
        }
    }

    /// <summary>
    /// Tells the animator to play the attack and sets the combo index.
    /// Called by WeaponManager when the player clicks.
    /// </summary>
    public void PlayAttack(int comboIndex)
    {
        if (playerAnim == null) return;

        // Set the combo hit number (0, 1, or 2)
        playerAnim.SetInteger("ComboIndex", comboIndex);
        
        // Fire the trigger to start the transition
        playerAnim.SetTrigger("Attack");
    }
}