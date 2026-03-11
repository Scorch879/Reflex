using UnityEngine;

// This must inherit from BaseWeapon
public class WhipWeapon : BaseWeapon
{
    // This is the specific logic for the Whip
    public override void PerformAttack(Animator playerAnim)
    {
        // 1. Check if the cooldown has passed (Logic is in BaseWeapon)
        if (!CanAttack()) return;

        // 2. Log the attack to the console for testing
        // Note: weaponData.weaponName comes from your ScriptableObject asset
        Debug.Log($"<color=green>Combat:</color> Successfully attacked with {weaponData.weaponName}!");
        
        // 3. Trigger the animation
        if (playerAnim != null)
        {
            playerAnim.SetTrigger("attack"); 
        }

        // 4. Update the timestamp so we can't spam clicks
        lastAttackTime = Time.time;
    }
}