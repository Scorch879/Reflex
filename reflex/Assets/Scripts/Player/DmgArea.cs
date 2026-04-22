using UnityEngine;

public class DmgArea : MonoBehaviour
{
    public float damagePerSecond = 20f;

    private void OnTriggerStay(Collider other)
    {
        // Check if the thing entering the zone has a PlayerManager
        if (other.TryGetComponent<PlayerManager>(out PlayerManager player))
        {
            // Use Time.deltaTime so the damage scales with time
            player.TakeDamage(damagePerSecond * Time.deltaTime);
        }
    }
}