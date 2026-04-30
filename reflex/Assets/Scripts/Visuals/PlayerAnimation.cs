using System;
using UnityEngine;

public class PlayerAnimation : MonoBehaviour
{
    [SerializeField] private Animator playerAnim;
    [SerializeField] private PlayerMovementManagement playerMovement;
    [SerializeField] private PlayerManager playerManager;


    public string weapon = "";
    public int comboInd = 0;


    private void Awake()
    {
    }

    private void Update()
    {
        MoveState();
        UpdateAnimatorStates();
    }

    private void MoveState()
    {
        bool isRunning = playerMovement.moveInput.magnitude > 0.1f;
        playerManager.isRunning = isRunning;
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
        Debug.Log($"Attack {comboIndex}");
        playerAnim.Play($"Attack {comboIndex}", 0, 0f);
        if ( comboIndex>= playerManager.weaponData.comboChain.Length)
        {
            playerManager.currentComboIndex = 0;
        }
    }
    
    public void UpdateAnimatorStates()
    {
        playerAnim.SetBool("isRunning", playerManager.isRunning);
        playerAnim.SetFloat("comboTime", playerManager.comboTime);
    }
   
}