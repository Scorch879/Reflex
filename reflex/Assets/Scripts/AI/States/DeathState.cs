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
        Debug.Log("<color=PURPLE>Enemy Defeated! ENTERING DEATH STATE</color>");
        if (_enemy.spriteRenderer != null)
        {
            if (_enemy.agent.isActiveAndEnabled && _enemy.agent.isOnNavMesh)
                _enemy.agent.isStopped = true;
            _enemy.agent.enabled = false;
        }

        // 2. Disable colliders so the player can walk over the corpse
        if (_enemy.TryGetComponent(out Collider collider))
        {
            collider.enabled = false;
        }

        // 3. Play the exact death animation
        if (_enemy.animator != null)
        {
            // Based on your earlier screenshot, the gray box is named "Death Back"
            _enemy.animator.Play("Death Back");
        }

        // 4. Destroy the GameObject after 3 seconds to free up memory
        // Make sure the time matches how long your animation takes to fall to the floor
        Object.Destroy(_enemy.gameObject, 3f);

    }

    public void Tick() { /* Do nothing, dead things don't tick */ }
    public void OnExit() { }
}