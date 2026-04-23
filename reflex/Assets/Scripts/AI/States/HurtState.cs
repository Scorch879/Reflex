using UnityEngine;

public class HurtState : IEnemyState
{
    private EnemyController _enemy;
    private float _stunTimer;
    private const float StunDuration = 0.4f; // How long the enemy flinches

    public HurtState(EnemyController enemy)
    {
        _enemy = enemy;
    }

    public void OnEnter()
    {
        Debug.Log("Enemy hit! Entering HURT STATE.");
        _stunTimer = StunDuration;
        
        // Stop movement while hurt
        if (_enemy.agent != null && _enemy.agent.isActiveAndEnabled && _enemy.agent.isOnNavMesh)
        {
            _enemy.agent.isStopped = true;
        }
        
        // Visual feedback (flinch color flash)
        if (_enemy.spriteRenderer != null)
        {
            _enemy.spriteRenderer.color = new Color(1f, 0.5f, 0.5f); 
        }
        
        if (_enemy.animator != null)
        {
            _enemy.animator.SetBool("isHurt", true);
        }
    }

    public void Tick()
    {
        _stunTimer -= Time.deltaTime;
        if (_stunTimer <= 0)
        {
           
            // After flinching, get mad and chase the player!
            _enemy.ChangeState(new ChaseState(_enemy));
        }
    }

    public void OnExit()
    {
        if (_enemy.agent != null && _enemy.agent.isActiveAndEnabled && _enemy.agent.isOnNavMesh)
        {
            _enemy.agent.isStopped = false;
        }

        if (_enemy.animator != null)
        {
            _enemy.animator.SetBool("isHurt", false);
        }
    }
}