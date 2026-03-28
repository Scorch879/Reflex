using System;
using UnityEngine;

public class PlayerAnimation : MonoBehaviour
{
    [SerializeField] private Animator playerAnim;
    [SerializeField] private PlayerMovementManagement playerMovement;

    public string weapon = "";
    public int comboInd = 0;


    private void Awake()
    {
    }

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
    public void PlayAttack(int comboIndex, string weaponName)
    {
        if (playerAnim == null) return;

        playerAnim.Play($"{weaponName} {comboIndex}", 0, 0f);
    
        weapon = weaponName;
        comboInd = comboIndex;

        playerAnim.SetInteger("ComboIndex", comboIndex);
    }
    
    public void GoToIdle()
    {
        playerAnim.SetTrigger("goToIdle");
    }
   
}