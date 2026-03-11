using UnityEngine;

public class ChaseState : IEnemyState
{
    private EnemyController _enemy;

    public ChaseState(EnemyController enemy)
    {
        _enemy = enemy;
    }

    public void OnEnter()
    {
        Debug.Log("ENTERED CHASE STATE!");
        _enemy.agent.speed = _enemy.speed;
        _enemy.spriteRenderer.color = Color.red; // Turn angry!
    }

    public void Tick()
    {
        // 1. Tell the agent to run to the player
        _enemy.agent.SetDestination(_enemy.player.position);

        // 2. Face the correct direction
        _enemy.FlipSprite(_enemy.agent.velocity.x);

        // 3. Keep drawing the laser so we know it's locked on
        _enemy.DrawLaser(_enemy.player.position, true);

        // 4. If the player runs far away, give up and go back to Idle
        // Note: Make this number LARGER than your detection radius (8) so it doesn't instantly give up
        float distance = Vector3.Distance(_enemy.transform.position, _enemy.player.position);
        if (distance > 12f)
        {
            Debug.Log("Lost the player!");
            _enemy.ChangeState(new IdleState(_enemy));
        }
    }

    public void OnExit()
    {
        _enemy.HideLaser();
    }
}