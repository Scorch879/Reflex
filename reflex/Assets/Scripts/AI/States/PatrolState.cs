using UnityEngine;
using UnityEngine.AI;

public class PatrolState : IEnemyState
{
    private EnemyController _enemy;

    public PatrolState(EnemyController enemy) => _enemy = enemy;

    public void OnEnter()
    {
        _enemy.spriteRenderer.color = Color.white;
        
        Vector3 randomDirection = Random.insideUnitSphere * _enemy.patrolRadius;
        randomDirection += _enemy.GetHomePosition();

        NavMeshHit hit;
        // Find the closest valid spot on the NavMesh within the radius
        if (NavMesh.SamplePosition(randomDirection, out hit, _enemy.patrolRadius, 1))
        {
            _enemy.agent.SetDestination(hit.position);
        }
    }

    public void Tick()
    {
        // 1. Check if we can see the player
        if (_enemy.CanSeePlayer())
        {
            _enemy.ChangeState(new ChaseState(_enemy));
            return;
        }

        // 2. If we've reached our patrol destination, go back to idle to pick a new one.
        if (!_enemy.agent.pathPending && _enemy.agent.remainingDistance <= _enemy.agent.stoppingDistance)
        {
            if (!_enemy.agent.hasPath || _enemy.agent.velocity.sqrMagnitude < 0.1f)
            {
                _enemy.ChangeState(new IdleState(_enemy));
            }
        }
    }

    public void OnExit() { }
}