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
        _enemy.spriteRenderer.color = Color.white;
        Debug.Log("Enemy is resting...");
    }

    public void Tick()
    {
        // 1. Count down the timer
        _idleTimer -= Time.deltaTime;

        // 2. Check for Player (Always priority!)
        float distance = Vector3.Distance(_enemy.transform.position, _enemy.player.position);

        if (distance < 8f)
        {
            Vector3 dirToPlayer = (_enemy.player.position - _enemy.transform.position).normalized;

            // Use transform.right/left if billboarding, or transform.forward if you added the rotation fix
            Vector3 lookDir = _enemy.spriteRenderer.flipX ? Vector3.left : Vector3.right;

            float angle = Vector3.Angle(lookDir, dirToPlayer);

            if (angle < 60f)
            {
                // We only want to hit the Player and the Environment (Default)
                int mask = LayerMask.GetMask("Default", "Player");

                // We shoot the ray slightly higher (Vector3.up * 1f) to ensure we hit the chest/head
                Vector3 eyeLevel = _enemy.transform.position + Vector3.up * 1f;

                if (Physics.Raycast(eyeLevel, dirToPlayer, out RaycastHit hit, 8f, mask))
                {
                    // THIS IS THE KEY: Check the console to see what the AI is actually hitting
                    Debug.Log("AI eyes hitting: " + hit.collider.name + " on layer: " + LayerMask.LayerToName(hit.collider.gameObject.layer));

                    bool isPlayer = hit.collider.CompareTag("Player");
                    _enemy.DrawLaser(hit.point, isPlayer);

                    if (isPlayer)
                    {
                        _enemy.ChangeState(new ChaseState(_enemy));
                    }
                }
            }
            else
            {
                _enemy.HideLaser();
            }
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