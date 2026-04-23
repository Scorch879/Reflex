using UnityEngine;

public class EnemyController : MonoBehaviour
{
    private IEnemyState _currentState;

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

    [Header("Settings")]
    public float speed = 3f;
    public float maxHealth = 100f;
    public float currentHealth;
    public float attackRange = 2f;
    public float attackCooldown = 1.5f;
    private float _verticalVelocity;
    private Vector3 _lastPosition;
    private float _stuckTimer;

    public Vector3 GetHomePosition() => _homePosition;

    void Start()
    {

        _homePosition = transform.position; // Remember where we started
        currentHealth = maxHealth;

        if (agent == null) agent = GetComponent<UnityEngine.AI.NavMeshAgent>();

        agent.updateRotation = false;

        if (player == null)
        {
            player = GameObject.FindGameObjectWithTag("Player").transform;
        }

        // This finds the SpriteRenderer component on the same object or its children
        if (spriteRenderer == null)
        {
            spriteRenderer = GetComponentInChildren<SpriteRenderer>();
        }

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
        if (player == null) return;

        // Rotate the transform to match the direction the NavMesh Agent is walking
        if (agent.velocity.sqrMagnitude > 0.1f)
        {
            Vector3 direction = agent.velocity.normalized;
            direction.y = 0; // Keep the enemy upright
            transform.rotation = Quaternion.LookRotation(direction);
        }

        if (animator != null)
        {
            // If speed is greater than 0.1, moving is true. Otherwise, false.
            bool moving = agent.velocity.magnitude > 0.1f;
            animator.SetBool("isWalking", moving);

            // Track grounded state using CharacterController if available, fallback to NavMeshAgent
            bool grounded = controller != null ? controller.isGrounded : agent.isOnNavMesh;
            animator.SetBool("isGrounded", grounded);
        }

        _currentState?.Tick();
    }


    public void AttackPlayer()
    {
        // 1. Tell the Animator to play the bite/attack animation
        if (animator != null)
        {
            animator.Play("Attack");
        }

        // 2. Calculate the "Hitbox" (a mathematical sphere right in front of the Ant's mouth)
        // The '1f' pushes it 1 unit forward. The Vector3.up raises it to chest/head height.
        Vector3 directionToPlayer = (player.position - transform.position).normalized;
        Vector3 hitBoxCenter = transform.position + (directionToPlayer * 1f) + (Vector3.up * 1f);

        // 3. Generate the invisible Hitbox sphere and grab any colliders it touches
        // The '1f' at the end is the size of the bite radius.
        Collider[] hitObjects = Physics.OverlapSphere(hitBoxCenter, 1f);

        // 4. Loop through everything we hit and check if it's the Player
        foreach (Collider hit in hitObjects)
        {
            if (hit.CompareTag("Player"))
            {
                // The Ant's teeth connected with the player's Capsule Collider!
                Debug.Log("<color=orange>ANT BIT THE PLAYER!</color>");

                // NOTE: Once you build a PlayerHealth script, you will trigger the damage here like this:
                // PlayerHealth pHealth = hit.GetComponent<PlayerHealth>();
                // if (pHealth != null) pHealth.TakeDamage(20f);
            }
        }
    }
    
    public void ChangeState(IEnemyState newState)
    {
        _currentState?.OnExit();
        _currentState = newState;
        _currentState.OnEnter();
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


    public void TakeDamage(float amount)
    {
        // Don't take further damage or change states if already dead
        if (currentHealth <= 0) return;

        currentHealth -= amount;

        if (currentHealth <= 0)
        {
            ChangeState(new DeathState(this));
        }
        else
        {
            ChangeState(new HurtState(this));
        }
    }

}