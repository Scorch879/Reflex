using UnityEngine;

public class WhipWeapon : BaseWeapon
{
    /// <summary>
    /// This method is called by the WeaponManager when the left-click is detected.
    /// It handles the timing and animation triggers for the whip.
    /// </summary>
    public override void PerformAttack(Animator playerAnim)
    {
        // 1. Check if the weapon is ready to fire based on the attackRate in WeaponData
        if (!CanAttack()) return;

        // 2. Log to the console to verify the logic is working
        Debug.Log($"<color=green>Combat:</color> Successfully swung the {weaponData.weaponName}!");
        
        // 3. Trigger the animation in the Animator Controller
        if (playerAnim != null)
        {
            // Ensure you have a Trigger parameter named "attack" in your Animator
            playerAnim.SetTrigger("attack"); 
        }

        // 4. Update the timestamp to start the cooldown timer
        lastAttackTime = Time.time;
    }
}