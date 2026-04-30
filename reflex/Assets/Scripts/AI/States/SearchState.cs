using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class SearchState : IEnemyState
{
    private EnemyController _enemy;
    private float _searchTimer;
    private const float SearchDuration = 10f;

    private List<Vector3> _searchPoints = new List<Vector3>();
    private int _currentPointIndex;
    private bool _waitingAtPoint;
    private float _waitTimer;
    private const float WaitAtPointDuration = 1.2f;

    private Quaternion _initialScanRotation;
    private const float ScanAngle = 120f;
    private const float ScanSpeed = 2.2f;
    private const float SearchRadius = 4f;
    private const int SearchPointCount = 4;

    public SearchState(EnemyController enemy)
    {
        _enemy = enemy;
    }

    public void OnEnter()
    {
        _searchTimer = SearchDuration;
        _currentPointIndex = 0;
        _waitingAtPoint = false;
        _waitTimer = 0f;
        _searchPoints = BuildSearchPoints();

        Debug.Log("ENTERING SEARCH STATE");
        _enemy.spriteRenderer.color = Color.yellow;
        _enemy.HideLaser();

        MoveToNextSearchPoint();
    }

    public void Tick()
    {
        if (_enemy.CanSeePlayer())
        {
            _enemy.ChangeState(new ChaseState(_enemy));
            return;
        }

        _searchTimer -= Time.deltaTime;
        if (_searchTimer <= 0f)
        {
            Debug.Log("Search timer expired. Returning to patrol.");
            _enemy.ChangeState(new PatrolState(_enemy));
            return;
        }

        if (_waitingAtPoint)
        {
            _waitTimer -= Time.deltaTime;
            PerformScan();
            if (_waitTimer <= 0f)
            {
                _waitingAtPoint = false;
                MoveToNextSearchPoint();
            }
            return;
        }

        if (!_enemy.agent.pathPending && _enemy.agent.remainingDistance <= _enemy.agent.stoppingDistance)
        {
            BeginPointWait();
        }
    }

    private List<Vector3> BuildSearchPoints()
    {
        var points = new List<Vector3>();
        Vector3 center = _enemy.lastKnownPlayerPosition;

        if (center == Vector3.zero)
        {
            center = _enemy.transform.position;
        }

        for (int i = 0; i < SearchPointCount; i++)
        {
            float angle = i * (360f / SearchPointCount);
            float radius = SearchRadius + Random.Range(-1f, 1f);
            Vector3 direction = Quaternion.Euler(0, angle, 0) * Vector3.forward;
            Vector3 candidate = center + direction * radius;

            if (NavMesh.SamplePosition(candidate, out NavMeshHit hit, 2f, NavMesh.AllAreas))
            {
                points.Add(hit.position);
            }
        }

        if (points.Count == 0)
        {
            points.Add(_enemy.transform.position);
        }

        return points;
    }

    private void MoveToNextSearchPoint()
    {
        if (_searchPoints.Count == 0)
        {
            _enemy.ChangeState(new PatrolState(_enemy));
            return;
        }

        _enemy.agent.SetDestination(_searchPoints[_currentPointIndex]);
        _enemy.DrawLaser(_searchPoints[_currentPointIndex], false);

        _currentPointIndex = (_currentPointIndex + 1) % _searchPoints.Count;
    }

    private void BeginPointWait()
    {
        _waitingAtPoint = true;
        _waitTimer = WaitAtPointDuration;
        _initialScanRotation = _enemy.transform.rotation;
        _enemy.agent.ResetPath();
    }

    private void PerformScan()
    {
        float scanAngleOffset = Mathf.Sin(Time.time * ScanSpeed) * (ScanAngle / 2f);
        Quaternion scanRotation = Quaternion.AngleAxis(scanAngleOffset, Vector3.up);
        _enemy.transform.rotation = _initialScanRotation * scanRotation;
    }

    public void OnExit()
    {
        if (_enemy.spriteRenderer != null)
        {
            _enemy.spriteRenderer.color = Color.white;
        }
        _enemy.HideLaser();
    }
}