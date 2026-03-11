using UnityEngine;

public abstract class BaseWeapon : MonoBehaviour
{
    // You need this so the weapon knows its stats!
    public WeaponData weaponData; 
    protected float lastAttackTime;

    public abstract void PerformAttack(Animator playerAnim);

    // Add this helper so all weapons can share cooldown logic
    protected bool CanAttack()
    {
        return Time.time >= lastAttackTime + (weaponData != null ? weaponData.attackRate : 0.5f);
    }
}