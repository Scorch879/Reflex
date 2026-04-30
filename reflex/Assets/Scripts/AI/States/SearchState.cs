using UnityEngine;

public class SearchState : IEnemyState
{
    private EnemyController _enemy;
    private float _searchTimer;
    private const float SearchDuration = 10f;

    // Fields for the scanning behavior
    private bool _hasArrivedAtSearchPoint;
    private Quaternion _initialScanRotation;
    private const float ScanAngle = 120f; 
    private const float ScanSpeed = 1.2f;

    public SearchState(EnemyController enemy)
    {
        _enemy = enemy;
    }

    public void OnEnter()
    {
        Debug.Log("ENTERING SEARCH STATE");
        _enemy.agent.SetDestination(_enemy.lastKnownPlayerPosition);
        _enemy.spriteRenderer.color = Color.yellow; // Indicate searching
        _searchTimer = SearchDuration;
        _enemy.HideLaser();
        _hasArrivedAtSearchPoint = false;
    }

    public void Tick()
    {
        // 1. If we spot the player again, go back to chasing.
        if (_enemy.CanSeePlayer())
        {
            _enemy.ChangeState(new ChaseState(_enemy));
            return;
        }

        // 2. The main search timer is always counting down.
        _searchTimer -= Time.deltaTime;

        // 3. If the timer runs out, give up and return to patrolling.
        if (_searchTimer <= 0f)
        {
            Debug.Log("Search timer expired. Returning to patrol.");
            _enemy.ChangeState(new PatrolState(_enemy));
            return;
        }

        // 4. If we arrive at the destination and still have time, stop and wait.
        if (!_enemy.agent.pathPending && _enemy.agent.remainingDistance <= _enemy.agent.stoppingDistance)
        {
            // When we first arrive, stop the agent and capture the current rotation.
            if (!_hasArrivedAtSearchPoint)
            {
                _hasArrivedAtSearchPoint = true;
                _initialScanRotation = _enemy.transform.rotation;
                _enemy.agent.ResetPath();
            }

            // Now, perform the scanning behavior
            PerformScan();
        }
    }

    private void PerformScan()
    {
        // Use a sine wave for smooth back-and-forth rotation.
        float scanAngleOffset = Mathf.Sin(Time.time * ScanSpeed) * (ScanAngle / 2f);
        Quaternion scanRotation = Quaternion.AngleAxis(scanAngleOffset, Vector3.up);
        _enemy.transform.rotation = _initialScanRotation * scanRotation;
    }

    public void OnExit()
    {
        _enemy.spriteRenderer.color = Color.white; // Reset color
    }
}