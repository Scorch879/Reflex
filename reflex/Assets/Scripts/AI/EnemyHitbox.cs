using Unity.VisualScripting;
using UnityEngine;

public class EnemyHitbox : MonoBehaviour
{
    [SerializeField] private EnemyController enemyController;
    
    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            // Assuming the player has a method to take damage
            PlayerManager playerManager = other.GetComponent<PlayerManager>();
            if (playerManager != null)
            {
                playerManager.TakeDamage(enemyController.attackDamage);
                enemyController.HitboxOff(); // Turn off the hitbox after hitting
            }
        }
    }
}
