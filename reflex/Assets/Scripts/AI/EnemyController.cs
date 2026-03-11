using UnityEngine;

public class EnemyController : MonoBehaviour 
{
    private IEnemyState _currentState;
   
    [Header("Visuals")]
    public Animator animator;

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
    private float _verticalVelocity;
    private Vector3 _lastPosition;
    private float _stuckTimer;

    public Vector3 GetHomePosition() => _homePosition;

    void Start() {

        _homePosition = transform.position; // Remember where we started

        if (agent == null) agent = GetComponent<UnityEngine.AI.NavMeshAgent>();

        agent.updateRotation = false;
        
        if (player == null) {
            player = GameObject.FindGameObjectWithTag("Player").transform;
        }

        // This finds the SpriteRenderer component on the same object or its children
        if (spriteRenderer == null) {
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

    public void FlipSprite(float horizontalDirection)
    {
        if (horizontalDirection > 0.1f) spriteRenderer.flipX = false;
        else if (horizontalDirection < -0.1f) spriteRenderer.flipX = true;
    }

    void Update() {
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
            animator.SetFloat("Speed", agent.velocity.magnitude);
        }

        _currentState?.Tick();
    }

    public void ChangeState(IEnemyState newState) {
        _currentState?.OnExit();
        _currentState = newState;
        _currentState.OnEnter();
    }

    void OnDrawGizmos()
    {
        if (player != null) {
            float distance = Vector3.Distance(transform.position, player.position);
            Vector3 direction = (player.position - transform.position).normalized;
            
            // Check if path is clear
            bool clearShot = false;
            if (Physics.Raycast(transform.position, direction, out RaycastHit hit, 8f))
            {
                if (hit.collider.CompareTag("Player")) clearShot = true;
            }

            // Green if he sees you, Red if you are hidden
            Gizmos.color = clearShot ? Color.green : Color.red;
            Gizmos.DrawLine(transform.position, player.position);
        }

        // Draw the Chase Range in Red
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, 8f);

        // Draw a line to the player if they are assigned
        if (player != null)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawLine(transform.position, player.position);
        }

        Vector3 groundPos = new Vector3(transform.position.x, 0, transform.position.z);
        Gizmos.DrawWireSphere(groundPos, 8f);
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


    
}