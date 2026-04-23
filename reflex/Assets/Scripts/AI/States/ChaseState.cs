using UnityEngine;

public class ChaseState : IEnemyState
{
    private EnemyController _enemy;
    private float _lostSightTimer; // Timer to track how long the player has been out of sight
    private const float SightGracePeriod = 0.5f; // How long to wait before giving up the chase


    public ChaseState(EnemyController enemy)
    {
        _enemy = enemy;
    }

    public void OnEnter()
    {
        Debug.Log("ENTERED CHASE STATE!");
        _enemy.agent.speed = _enemy.speed;

        if (_enemy.spriteRenderer != null)
        {
            _enemy.spriteRenderer.color = Color.darkRed;
        }
        
        _lostSightTimer = 0f;
    }

    public void Tick()
    {
        // 1. Always target the player's current position while chasing
        _enemy.agent.SetDestination(_enemy.player.position);
        _enemy.DrawLaser(_enemy.player.position, true); // Show we are locked on

        // 2. Check if we still have a line of sight to the player
        Vector3 eyePosition = _enemy.transform.position + Vector3.up * 1f; // From enemy's eyes
        Vector3 playerTargetPosition = _enemy.player.position + Vector3.up * 1f; // To player's torso
        Vector3 dirToPlayer = (playerTargetPosition - eyePosition).normalized;

        bool hasLineOfSight = false;
        // Use the chaseLeashRange for the raycast distance
        if (Physics.Raycast(eyePosition, dirToPlayer, out RaycastHit hit, _enemy.chaseLeashRange, _enemy.detectionLayers))
        {
            if (hit.collider.CompareTag("Player"))
            {
                hasLineOfSight = true;
                // Continuously update last known position while we can see them
                _enemy.lastKnownPlayerPosition = _enemy.player.position;

                // Transition to Attack State if close enough
                if (Vector3.Distance(_enemy.transform.position, _enemy.player.position) <= _enemy.attackRange)
                {
                    _enemy.ChangeState(new AttackState(_enemy));
                    return;
                }
            }
        }

        // 3. If we have line of sight, reset the timer. Otherwise, start counting.
        if (hasLineOfSight)
        {
            _lostSightTimer = 0f;
        }
        else
        {
            _lostSightTimer += Time.deltaTime;
        }

        // 4. If we have lost sight for longer than the grace period, go to Search state
        if (_lostSightTimer > SightGracePeriod)
        {
            _enemy.ChangeState(new SearchState(_enemy));
        }
    }

    public void OnExit()
    {
        _enemy.HideLaser();
    }
}