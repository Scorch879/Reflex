using UnityEngine;
using System;

public class EnemyController : MonoBehaviour
{
    public static event Action<EnemyController> EnemyDefeated;

    private IEnemyState _currentState;
    public string enemyStateDislpay;

    [Header("Visuals")]
    public Animator animator;

    [Header("AI Vision")]
    public float visionRange = 8f;
    public float visionAngle = 90f;
    public float chaseLeashRange = 15f;
    [HideInInspector] public Vector3 lastKnownPlayerPosition;

    [Header("Detection Settings")]
    public LayerMask detectionLayers; // Set this in the Inspector

    [Header("AI Auto-Patrol")]
    public float patrolRadius = 10f;
    private Vector3 _homePosition;

    [Header("References")]
    public CharacterController controller;
    public Transform player;
    public SpriteRenderer spriteRenderer;
    public UnityEngine.AI.NavMeshAgent agent;
    public GameObject enemyHitbox;
    [HideInInspector] public Vector3 knockbackVelocity;

    [Header("Enemy Stats")]
    public EnemyData EnemyStatData; // The ScriptableObject blueprint
    // Runtime values (we keep these here so the Emotion Director can modify them safely)
    [HideInInspector] public float speed;
    [HideInInspector] public float maxHealth;
    [HideInInspector] public float attackDamage;
    [HideInInspector] public float attackRange;
    [HideInInspector] public float attackCooldown;
    [HideInInspector] public float currentHealth;


    private Vector3 _lastPosition;
    private float _stuckTimer;

    [Header("Emotion Adaptation")]
    public bool useEmotionDirector = true;

    [Header("Swarm Settings")]
    public string enemyType;
    [HideInInspector] public bool isElite = false;
    public PlayerEmotionState CurrentEmotionState { get; private set; }
    public EmotionDirectorDirective CurrentDirective { get; private set; }
    private float _baseSpeed;
    private float _baseAttackDamage;
    private float _baseAttackCooldown;
    private float _baseVisionRange;
    private bool _baseStatsCached;

    public Vector3 GetHomePosition() => _homePosition;

    void OnEnable()
    {
        EmotionDirector.DirectiveChanged += HandleDirectorDirectiveChanged;
    }

    void Start()
    {
        float floorHealthMultiplier = LevelRunManager.HasInstance ? LevelRunManager.Instance.CurrentFloorEnemyHealthMultiplier : 1f;
        float floorDamageMultiplier = LevelRunManager.HasInstance ? LevelRunManager.Instance.CurrentFloorEnemyDamageMultiplier : 1f;

        _homePosition = transform.position; // Remember where we started
        maxHealth = EnemyStatData.maxHealth * floorHealthMultiplier;
        currentHealth = maxHealth;
        speed = EnemyStatData.speed;
        attackDamage = EnemyStatData.attackDamage * floorDamageMultiplier;
        attackCooldown = EnemyStatData.attackCooldown;
        enemyHitbox.SetActive(false); // Ensure hitbox starts disabled
        PrintCurrentState();

        if (agent == null)
        {
            agent = GetComponent<UnityEngine.AI.NavMeshAgent>();
            agent.obstacleAvoidanceType = UnityEngine.AI.ObstacleAvoidanceType.NoObstacleAvoidance;
            if (agent == null)
            {
                Debug.LogWarning($"{name}: No NavMeshAgent found on enemy.");
            }
        }

        if (agent != null)
        {
            agent.updateRotation = false;
        }

        if (player == null)
        {
            GameObject playerObject = GameObject.FindGameObjectWithTag("Player");
            if (playerObject != null)
            {
                player = playerObject.transform;
            }
            else
            {
                Debug.LogWarning($"{name}: No GameObject found with tag 'Player'. Enemy will still patrol, but chasing will be disabled until the player is assigned.");
            }
        }

        if (spriteRenderer == null)
        {
            spriteRenderer = GetComponentInChildren<SpriteRenderer>();
        }

        if (animator == null)
        {
            animator = GetComponentInChildren<Animator>();
        }

        CacheBaseStats();
        ApplyDirectorDirective(EmotionDirector.Instance.CurrentDirective);
        SwarmManager.RegisterEnemy(enemyType, this);
        ChangeState(new IdleState(this));
    }

    public void CheckIfStuck()
    {
        if (Vector3.Distance(transform.position, _lastPosition) < 0.1f)
        {
            _stuckTimer += Time.deltaTime;
            if (_stuckTimer > 2f) // If stuck for 2 seconds
            {
                _stuckTimer = 0;
                ChangeState(new IdleState(this)); // Force a reset
            }
        }
        else
        {
            _stuckTimer = 0;
            _lastPosition = transform.position;
        }
    }

    void Update()
    {
        // Rotate the transform to match the direction the NavMesh Agent is walking
        if (agent != null && agent.velocity.sqrMagnitude > 0.1f)
        {
            Vector3 direction = agent.velocity.normalized;
            // direction.y = 0; // Keep the enemy upright
            transform.rotation = Quaternion.LookRotation(direction);
            
        }

        if (animator != null)
        {
            bool moving = agent != null && agent.velocity.magnitude > 0.1f;
            animator.SetBool("isWalking", moving);

            bool grounded = controller != null ? controller.isGrounded : (agent != null && agent.isOnNavMesh);
            animator.SetBool("isGrounded", grounded);

            bool isAttacking = _currentState is AttackState;
            animator.SetBool("isAttacking", isAttacking);
        }

        _currentState?.Tick();
    }

    // debug purpouses
    public void PrintCurrentState()
    {
        enemyStateDislpay = _currentState != null ? _currentState.GetType().Name : "None";
    }


    // anim events
    public void HitboxOn()
    {
        if (enemyHitbox != null)
        {
            enemyHitbox.SetActive(true);
        }
    }

    public void HitboxOff()
    {
        if (enemyHitbox != null)
        {
            enemyHitbox.SetActive(false);
        }
    }

    public void AttackPlayer()
    {
        if (animator != null)
        {
            animator.Play("Attack");
            animator.SetBool("isAttacking", true);
            //Debug.Log("Attacking player");
        }

        if (player == null)
        {
            Debug.LogWarning($"{name}: AttackPlayer called but player is not assigned.");
            return;
        }
    }
    
    public void ChangeState(IEnemyState newState)
    {
        _currentState?.OnExit();
        _currentState = newState;
        _currentState.OnEnter();
        PrintCurrentState();
    }

    public bool CanSeePlayer()
    {
        if (player == null) return false;

        // Is player within vision range?
        float distanceToPlayer = Vector3.Distance(transform.position, player.position);
        if (distanceToPlayer > visionRange)
        {
            HideLaser();
            return false;
        }

        Vector3 eyePosition = transform.position + Vector3.up * 1f;
        Vector3 playerTargetPosition = player.position + Vector3.up * 1f; // Aim for the player's torso
        Vector3 directionToPlayer = (playerTargetPosition - eyePosition).normalized;

        // Is player within vision angle?
        float angleToPlayer = Vector3.Angle(transform.forward, directionToPlayer);
        if (angleToPlayer > visionAngle / 2)
        {
            HideLaser();
            return false;
        }

        // Is there a clear line of sight?
        if (Physics.Raycast(eyePosition, directionToPlayer, out RaycastHit hit, visionRange, detectionLayers))
        {
            bool isPlayer = hit.collider.CompareTag("Player");
            DrawLaser(hit.point, isPlayer); // Draw laser to whatever we hit

            if (isPlayer)
            {
                EmotionEngine.Instance.RecordEnemyEncounter(this);
                lastKnownPlayerPosition = player.position;
                return true;
            }
        }

        return false;
    }

    void OnDrawGizmosSelected()
    {
        // Vision Cone
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, visionRange);
        Vector3 eyePosition = transform.position + Vector3.up;
        Vector3 forward = transform.forward;
        Quaternion leftRayRotation = Quaternion.AngleAxis(-visionAngle / 2, Vector3.up);
        Quaternion rightRayRotation = Quaternion.AngleAxis(visionAngle / 2, Vector3.up);
        Vector3 leftRayDirection = leftRayRotation * forward;
        Vector3 rightRayDirection = rightRayRotation * forward;
        Gizmos.DrawRay(eyePosition, leftRayDirection * visionRange);
        Gizmos.DrawRay(eyePosition, rightRayDirection * visionRange);

        // Chase Leash Range
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, chaseLeashRange);

        // Last Known Position
        if (lastKnownPlayerPosition != Vector3.zero)
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawSphere(lastKnownPlayerPosition, 0.5f);
        }
    }

    public LineRenderer laserLine; // Drag the LineRenderer component here
    public void DrawLaser(Vector3 targetPos, bool canSee)
    {
        if (laserLine == null) return;

        laserLine.enabled = true;
        laserLine.SetPosition(0, transform.position + Vector3.up * 0.5f); // Eye level
        laserLine.SetPosition(1, targetPos);

        // Change color: Red if blocked, Green if spotting you
        laserLine.startColor = canSee ? Color.green : Color.red;
        laserLine.endColor = canSee ? Color.green : Color.red;
    }

    public void HideLaser()
    {
        if (laserLine != null) laserLine.enabled = false;
    }

    public Vector3 GetDirectorChaseDestination(Vector3 playerPosition)
    {
        if (!useEmotionDirector || CurrentDirective.strategy != EmotionDirectorStrategy.AggressionContainment)
        {
            return playerPosition;
        }

        Vector3 enemyPosition = transform.position;
        float distanceToPlayer = Vector3.Distance(enemyPosition, playerPosition);

        if (distanceToPlayer < CurrentDirective.retreatDistance)
        {
            Vector3 awayFromPlayer = (enemyPosition - playerPosition).normalized;
            if (awayFromPlayer.sqrMagnitude < 0.01f)
            {
                awayFromPlayer = -transform.forward;
            }

            Vector3 retreatTarget = enemyPosition + awayFromPlayer * CurrentDirective.retreatDistance;
            if (UnityEngine.AI.NavMesh.SamplePosition(retreatTarget, out UnityEngine.AI.NavMeshHit hit, 3f, UnityEngine.AI.NavMesh.AllAreas))
            {
                return hit.position;
            }

            return retreatTarget;
        }

        if (distanceToPlayer < CurrentDirective.chaseStandoffDistance && distanceToPlayer > attackRange)
        {
            return enemyPosition;
        }

        return playerPosition;
    }

    public float GetDirectorAttackOpeningDelay()
    {
        if (!useEmotionDirector)
        {
            return 0f;
        }

        return CurrentDirective.attackOpeningDelay;
    }

    private void HandleDirectorDirectiveChanged(EmotionDirectorDirective directive)
    {
        ApplyDirectorDirective(directive);
    }

    private void CacheBaseStats()
    {
        if (_baseStatsCached)
        {
            return;
        }

        _baseSpeed = speed;
        _baseAttackCooldown = attackCooldown;
        _baseVisionRange = visionRange;
        _baseAttackDamage = attackDamage;
        _baseStatsCached = true;
    }

    private void ApplyDirectorDirective(EmotionDirectorDirective directive)
    {
        CacheBaseStats();
        CurrentDirective = directive;
        CurrentEmotionState = directive.sourceEmotion;

        if (useEmotionDirector)
        {
            speed = _baseSpeed * directive.enemySpeedMultiplier;
            attackCooldown = _baseAttackCooldown * directive.enemyAttackCooldownMultiplier;
            visionRange = _baseVisionRange * directive.enemyVisionMultiplier;
            attackDamage = _baseAttackDamage; //Jorho please update this to also include attack damage
        }

        if (agent != null)
        {
            agent.speed = speed;
        }
    }


    public void TakeDamage(float amount, float attackStunDuration, Vector3 knockbackForce)
    {
        // Don't take further damage or change states if already dead
        Debug.Log($"<color=yellow> CurrentHP : {currentHealth}");
        if (currentHealth <= 0) return;

        currentHealth -= amount;

        if (currentHealth <= 0)
        {
            SwarmManager.UnregisterEnemy(enemyType, this);
            EnemyDefeated?.Invoke(this);
            ChangeState(new DeathState(this));
            animator.Play("Hurt");
        }
        else
        {
            // Store the incoming knockback so HurtState can execute the physics
            this.knockbackVelocity = knockbackForce;
            ChangeState(new HurtState(this, attackStunDuration));
        }
    }

    void OnDisable()
    {
        EmotionDirector.DirectiveChanged -= HandleDirectorDirectiveChanged;
        SwarmManager.UnregisterEnemy(enemyType, this);
    }

}
