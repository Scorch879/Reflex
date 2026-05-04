using UnityEngine;
using UnityEngine.AI;

public class PatrolState : IEnemyState
{
    private EnemyController _enemy;

    public PatrolState(EnemyController enemy) => _enemy = enemy;

    public void OnEnter()
    {
        if (_enemy.spriteRenderer != null)
        {
            _enemy.spriteRenderer.color = Color.white;
        }

        EnemyController elite = SwarmManager.GetElite(_enemy.enemyType);
        if (elite != null && elite != _enemy && SwarmManager.GetAllEnemies(_enemy.enemyType).Count > 1)
        {
            // Swarm around the elite
            Vector3 offset = Random.insideUnitSphere * 5f; // 5 units radius around elite
            offset.y = 0;
            Vector3 target = elite.transform.position + offset;
            NavMeshHit hit;
            if (NavMesh.SamplePosition(target, out hit, 5f, 1))
            {
                _enemy.agent.SetDestination(hit.position);
            }
        }
        else
        {
            // Original random patrol
            Vector3 randomDirection = Random.insideUnitSphere * _enemy.patrolRadius;
            randomDirection += _enemy.GetHomePosition();
            NavMeshHit hit;
            if (NavMesh.SamplePosition(randomDirection, out hit, _enemy.patrolRadius, 1))
            {
                _enemy.agent.SetDestination(hit.position);
            }
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