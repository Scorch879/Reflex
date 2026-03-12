using UnityEngine;

public abstract class BaseWeapon : MonoBehaviour
{
    public WeaponData weaponData;
    protected float lastAttackTime;

    public abstract void PerformAttack(Animator playerAnim);

    protected bool CanAttack()
    {
        return Time.time >= lastAttackTime + (weaponData != null ? weaponData.attackRate : 0.5f);
    }
}