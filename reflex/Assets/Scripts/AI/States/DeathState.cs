using UnityEngine;

public class DeathState : IEnemyState
{
    private EnemyController _enemy;

    public DeathState(EnemyController enemy)
    {
        _enemy = enemy;
    }

    public void OnEnter()
    {
        Debug.Log("Enemy Defeated!");
        if (_enemy.spriteRenderer != null)
        {
            _enemy.spriteRenderer.color = Color.black; // Show death visually
        }
        
        // Stop the NavMeshAgent
        if (_enemy.agent != null)
        {
            if (_enemy.agent.isActiveAndEnabled && _enemy.agent.isOnNavMesh)
                _enemy.agent.isStopped = true;
            _enemy.agent.enabled = false;
        }

        // Disable colliders so the player doesn't bump into a dead enemy
        if (_enemy.TryGetComponent(out Collider collider)) collider.enabled = false;

        // TODO: Play death animation or destroy object after a delay
    }

    public void Tick() { /* Do nothing, dead things don't tick */ }
    public void OnExit() { }
}