using UnityEngine;

public class IdleState : IEnemyState
{
    private EnemyController _enemy;

    private float _idleTimer;
    private float _waitDuration = 2f; // Wait for 2 seconds

    // This constructor connects the state to your specific enemy
    public IdleState(EnemyController enemy)
    {
        _enemy = enemy;
    }

    public void OnEnter()
    {
        _idleTimer = _waitDuration;

        if (_enemy.spriteRenderer != null)
        {
            _enemy.spriteRenderer.color = Color.white;
        }
        
        Debug.Log("Enemy is resting...");
    }

    public void Tick()
    {
        // 1. Count down the timer
        _idleTimer -= Time.deltaTime;

        // 2. Check if we can see the player
        if (_enemy.CanSeePlayer())
        {
            _enemy.ChangeState(new ChaseState(_enemy));
            return;
        }

        // 3. If time is up, go back to Patrolling
        if (_idleTimer <= 0)
        {
            _enemy.ChangeState(new PatrolState(_enemy));
        }
    }

    public void OnExit()
    {
        Debug.Log("Enemy leaving IDLE state.");
    }
}