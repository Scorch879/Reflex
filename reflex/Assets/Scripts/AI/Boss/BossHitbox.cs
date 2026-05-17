using UnityEngine;

public class BossHitbox : MonoBehaviour
{
    public int attackNumber = 1; // 1 for Attack1, 2 for Attack2, etc.
    public BossManager bossManager;

    private void OnTriggerEnter(Collider other)
    {
        if(other.CompareTag("Player"))
        {
            other.TryGetComponent(out PlayerManager playerManager);
            if (playerManager != null)
            {
                playerManager.TakeDamage(GetAttackDamageType(attackNumber));
            }
        }
    }

    private float GetAttackDamageType(int attackNumber)
    {
        switch (attackNumber)
        {
            case 1:
                return bossManager.attack1Damage;
            case 2:
                return bossManager.attack2Damage;
            case 3:
                return bossManager.laserDamage;
            default:
                return 0f;
        }
    }
}
