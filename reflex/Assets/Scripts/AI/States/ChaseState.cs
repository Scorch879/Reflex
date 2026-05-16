using UnityEngine;

public class ChaseState : IEnemyState
{
    private EnemyController _enemy;
    private float _lostSightTimer;
    private const float SightGracePeriod = 0.5f;

    // NEW: Each ant gets its own spot in the "Attack Ring"
    private Vector3 _personalOffset; 

    public ChaseState(EnemyController enemy)
    {
        _enemy = enemy;
    }

    public void OnEnter()
    {
        Debug.Log("ENTERED CHASE STATE!");
        if (_enemy.agent != null) _enemy.agent.speed = _enemy.speed;
        if (_enemy.spriteRenderer != null) _enemy.spriteRenderer.color = Color.darkRed;
        
        _lostSightTimer = 0f;

        // GENERATE OFFSET: Pick a random direction around the player.
        // We will multiply this by the attack range so they stop in a ring around you!
        Vector2 randomCircle = Random.insideUnitCircle.normalized;
        _personalOffset = new Vector3(randomCircle.x, 0, randomCircle.y);
    }

    public void Tick()
    {
        if (_enemy.player == null) return;

        // 1. UNIQUE TARGET: Instead of going to the exact center of the player, 
        // the ant goes to its personal spot just outside the player's hitbox!
        Vector3 surroundPosition = _enemy.player.position + (_personalOffset * (_enemy.attackRange - 0.2f));
        _enemy.agent.SetDestination(surroundPosition);
        
        _enemy.DrawLaser(_enemy.player.position, true); 

        // 2. LINE OF SIGHT CHECK (Your existing logic)
        Vector3 eyePosition = _enemy.transform.position + Vector3.up * 1f;
        Vector3 playerTargetPosition = _enemy.player.position + Vector3.up * 1f;
        Vector3 dirToPlayer = (playerTargetPosition - eyePosition).normalized;

        bool hasLineOfSight = false;
        
        if (Physics.Raycast(eyePosition, dirToPlayer, out RaycastHit hit, _enemy.chaseLeashRange, _enemy.detectionLayers))
        {
            if (hit.collider.CompareTag("Player"))
            {
                hasLineOfSight = true;
                _enemy.lastKnownPlayerPosition = _enemy.player.position;

                // Attack State Transition
                if (Vector3.Distance(_enemy.transform.position, _enemy.player.position) <= _enemy.attackRange)
                {
                    _enemy.ChangeState(new AttackState(_enemy));
                    return;
                }
            }
        }

        // 3. LOSS OF SIGHT TIMER
        if (hasLineOfSight)
        {
            _lostSightTimer = 0f;
        }
        else
        {
            _lostSightTimer += Time.deltaTime;
        }

        if (_lostSightTimer > SightGracePeriod)
        {
            _enemy.ChangeState(new SearchState(_enemy));
        }
    }

    public void OnExit()
    {
        _enemy.HideLaser();
    }
}