using UnityEngine;

public class BossHurt : MonoBehaviour
{
    public BossController bossController;
    public BossManager bossManager;

    public void HandleHurt(float damage)
    {
        if (bossController.currentState == BossState.Defeated) return;

        bossManager.TakeDamage(damage);
    }
}
