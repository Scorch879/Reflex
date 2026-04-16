using UnityEngine;

public class AttackState : IEnemyState
{
    private EnemyController _enemy;
    private float _attackTimer;

    public AttackState(EnemyController enemy)
    {
        _enemy = enemy;
    }

    public void OnEnter()
    {
        Debug.Log("ENTERED ATTACK STATE");
        // Stop moving to attack
        _enemy.agent.isStopped = true;
        _enemy.spriteRenderer.color = Color.magenta; // Indicate attacking visually

        _attackTimer = _enemy.attackCooldown;

        if (_enemy.animator != null)
        {
            _enemy.animator.SetBool("isAttacking", true);
        }
    }

    public void Tick()
    {
        _attackTimer -= Time.deltaTime;

        if (_attackTimer <= 0)
        {
            // Attack finished, go back to chasing
            _enemy.ChangeState(new ChaseState(_enemy));
        }
    }

    public void OnExit()
    {
        // 1. Unfreeze the legs
        _enemy.agent.isStopped = false;

        // 2. Turn OFF the attack animation so it doesn't get stuck sliding around in a punch pose!
        if (_enemy.animator != null)
        {
            _enemy.animator.SetBool("isAttacking", false);
        }
    }
}