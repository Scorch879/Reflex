using UnityEngine;

public class AttackState : IEnemyState
{
    private EnemyController _enemy;
    private float _attackTimer;
    private bool _hasFinishedAttacking;

    public AttackState(EnemyController enemy)
    {
        _enemy = enemy;
    }

    public void OnEnter()
    {
       Debug.Log("ENTERED ATTACK STATE");
        _enemy.agent.isStopped = true; 
        
        // Trigger the animation
        if (_enemy.animator != null) 
            _enemy.animator.SetBool("isAttacking", true);

        // Set to 0 so the very first bite happens instantly
        _attackTimer = 0f;
    }

    public void Tick()
    {
        // 1. Keep looking at player while standing still
        Vector3 dirToPlayer = (_enemy.player.position - _enemy.transform.position).normalized;
        dirToPlayer.y = 0; 
        _enemy.transform.rotation = Quaternion.LookRotation(dirToPlayer);

        // 2. Handle the swing timer
        _attackTimer -= Time.deltaTime;
        if (_attackTimer <= 0)
        {
            // Trigger the actual Hitbox code we wrote in EnemyController!
            _enemy.AttackPlayer(); 
            _attackTimer = _enemy.attackCooldown; // Reset timer for the next bite
        }

        // 3. Only go back to Chase if the player runs away
        float distance = Vector3.Distance(_enemy.transform.position, _enemy.player.position);
        if (distance > _enemy.attackRange)
        {
            _enemy.ChangeState(new ChaseState(_enemy));
        }
    }

    public void OnExit()
    {
       _enemy.agent.isStopped = false; 
        
        // Turn off the attack animation
        if (_enemy.animator != null) 
            _enemy.animator.SetBool("isAttacking", false);
    }
}