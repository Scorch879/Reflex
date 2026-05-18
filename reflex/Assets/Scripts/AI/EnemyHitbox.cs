using UnityEngine;

public class EnemyHitbox : MonoBehaviour
{
    [SerializeField] private EnemyController enemyController;
    private bool _loggedMissingController;

    private void Awake()
    {
        ResolveControllerReference();
    }

    private void OnEnable()
    {
        ResolveControllerReference();
    }
    
    private void OnTriggerEnter(Collider other)
    {
        TryDamagePlayer(other);
    }

    private void OnTriggerStay(Collider other)
    {
        TryDamagePlayer(other);
    }

    private void TryDamagePlayer(Collider other)
    {
        ResolveControllerReference();
        if (enemyController == null)
        {
            if (!_loggedMissingController)
            {
                Debug.LogWarning($"{name}: EnemyHitbox has no EnemyController reference.");
                _loggedMissingController = true;
            }

            return;
        }

        PlayerManager playerManager = other.GetComponentInParent<PlayerManager>();
        if (playerManager != null && (other.CompareTag("Player") || playerManager.CompareTag("Player")))
        {
            playerManager.TakeDamage(enemyController.attackDamage);
            enemyController.HitboxOff();
        }
    }

    private void ResolveControllerReference()
    {
        if (enemyController == null)
        {
            enemyController = GetComponentInParent<EnemyController>();
        }
    }
}
