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
        _enemy.FlipSprite(_enemy.agent.velocity.x);

        // 1. PATH CHECK (Check if we reached destination)
        if (!_enemy.agent.pathPending && _enemy.agent.remainingDistance <= _enemy.agent.stoppingDistance)
        {
            if (!_enemy.agent.hasPath || _enemy.agent.velocity.sqrMagnitude < 0.1f)
            {
                _enemy.ChangeState(new IdleState(_enemy));
            }
        }

        // 2. DETECTION BLOCK (Only one block needed!)
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
    }

    public void OnExit() { }
}