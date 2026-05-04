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
        if (_enemy.player == null)
        {
            _enemy.ChangeState(new SearchState(_enemy));
            return;
        }

        // 1. Determine where to move while chasing
        bool hasLineOfSight = false;
        Vector3 eyePosition = _enemy.transform.position + Vector3.up * 1f; // From enemy's eyes
        Vector3 playerTargetPosition = _enemy.player.position + Vector3.up * 1f; // To player's torso
        Vector3 dirToPlayer = (playerTargetPosition - eyePosition).normalized;

        if (Physics.Raycast(eyePosition, dirToPlayer, out RaycastHit hit, _enemy.chaseLeashRange, _enemy.detectionLayers))
        {
            if (hit.collider.CompareTag("Player"))
            {
                hasLineOfSight = true;
                _enemy.lastKnownPlayerPosition = _enemy.player.position;
            }
        }

        if (hasLineOfSight)
        {
            _lostSightTimer = 0f;
            _enemy.agent.SetDestination(_enemy.player.position);
            _enemy.DrawLaser(_enemy.player.position, true); // Show we are locked on

            if (Vector3.Distance(_enemy.transform.position, _enemy.player.position) <= _enemy.attackRange)
            {
                _enemy.ChangeState(new AttackState(_enemy));
                return;
            }
        }
        else
        {
            _lostSightTimer += Time.deltaTime;

            if (_enemy.lastKnownPlayerPosition != Vector3.zero)
            {
                _enemy.agent.SetDestination(_enemy.lastKnownPlayerPosition);
                _enemy.DrawLaser(_enemy.lastKnownPlayerPosition, false);
            }
        }

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