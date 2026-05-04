using UnityEngine;

public class EnemyHurtbox : MonoBehaviour
{
    [Header("References")]
    public EnemyController enemyController;

    private void Start()
    {
        // Attempt to grab the controller from this object or its parents if unassigned
        if (enemyController == null)
        {
            enemyController = GetComponentInParent<EnemyController>();
        }
    }

    /// <summary>
    /// Call this method from your player's weapon/attack script when it intersects with this collider.
    /// </summary>
    public void ReceiveDamage(float damageAmount)
    {
        if (enemyController != null)
        {
            enemyController.TakeDamage(damageAmount);
        }
        else
        {
            Debug.LogWarning("EnemyHurtbox doesn't have an EnemyController assigned!");
        }
    }
}