using UnityEngine;

public class BossController : MonoBehaviour
{
    [Header("Boss References")]
    public BossData bossData;
    public BossManager bossManager;
    public BossState currentState;
    public Animator animator;
    public GameObject leftArmHitbox;
    public GameObject rightArmHitbox;
    public GameObject leftLaserHitbox;
    public GameObject rightLaserHitbox;

    [Header("Boss Hurtboxes")]
    public GameObject headHurtbox;
    public GameObject leftArmHurtbox;
    public GameObject rightArmHurtbox;
    public GameObject leftChestHurtbox;
    public GameObject rightChestHurtbox;

    public float animationSpeed = 1f;
    public float minAttackInterval = 1f;
    public float maxAttackInterval = 3f;
    private int _attackCounter = 0;
    public float gracePeriodFromHurt = 1f; 
    private float _nextAttackTime;
    private float _stunEndTime;

    public void Awake()
    {   
        if (leftArmHitbox != null) leftArmHitbox.SetActive(false);
        if (rightArmHitbox != null) rightArmHitbox.SetActive(false);
        if (leftLaserHitbox != null) leftLaserHitbox.SetActive(false);
        if (rightLaserHitbox != null) rightLaserHitbox.SetActive(false);
        
        if (bossManager != null)
        {
            bossData = bossManager.bossData;
        }
    }

    private void Start()
    {
        if (bossData != null)
        {
            animationSpeed = bossData.animationSpeed;
            minAttackInterval = bossData.minAttackInterval;
            maxAttackInterval = bossData.maxAttackInterval;
        }
        currentState = BossState.Idle;
        _nextAttackTime = Time.time + 3f;
    }

    private void Update()
    {
        // If the boss is dead, halt all AI behavior choices entirely
        if (currentState == BossState.Defeated) return;

        HandleStateBehavior();
        UpdateAnimatorParameters();
    }

    private void HandleStateBehavior()
    { 
        switch (currentState)
        {
            case BossState.Idle:
                if (Time.time >= _nextAttackTime)
                {
                    InitiateAttackSequence();
                }
                break;

            case BossState.Attacking:
                break;

            case BossState.Stunned:
                if (Time.time >= _stunEndTime)
                {
                    currentState = BossState.Idle;
                    _nextAttackTime = Time.time + Random.Range(minAttackInterval, maxAttackInterval);
                    UpdateAnimatorParameters();
                }
                break;
                
            case BossState.Hurt:
                break;
        }
    }

    public void UpdateAnimatorParameters()
    {
        // Safeguard to preserve death state visual consistency
        if (animator == null || currentState == BossState.Defeated) return;
        
        animator.SetFloat("animationSpeed", animationSpeed);
        animator.SetBool("isAttacking", currentState == BossState.Attacking);
        animator.SetBool("isStunned", currentState == BossState.Stunned);
        animator.SetBool("isHurt", currentState == BossState.Hurt);
    }

    public void HandleHurt()
    {
        if (currentState == BossState.Defeated) return;
        
        // Cancel any pending ProceedToIdle calls to prevent state-machine confusion
        CancelInvoke(nameof(ProceedToIdle));

        if (currentState == BossState.Stunned)
        {
            animator.Play("Stun Hurt", 0, 0f);
        }
        else
        {
            currentState = BossState.Hurt;
            animator.Play("Hurt", 0, 0f);
        }
        
        UpdateAnimatorParameters();
        DisableAllHitboxes();
        
        Invoke(nameof(ProceedToIdle), gracePeriodFromHurt);
    }

    public void HandleDefeat()
    {
        // Cancel any queued recovery loops
        CancelInvoke(nameof(ProceedToIdle));
        
        currentState = BossState.Defeated;
        DisableAllHitboxes();
        DisableAllHurtboxes();

        if (animator != null)
        {
            // Reset parameter weights cleanly
            animator.SetBool("isAttacking", false);
            animator.SetBool("isStunned", false);
            animator.SetBool("isHurt", false);
            
            // Force play death clip instantly
            animator.Play("Death", 0, 0f);
        }
    }

    private void InitiateAttackSequence()
    {
        currentState = BossState.Attacking;
        _attackCounter++;

        if (_attackCounter <= 2)
        {
            PlayPhysicalAttack();
        }
        else
        {
            if (Random.value <= 0.4f)
            {
                PlayLaserAttack();
            }
            else
            {
                PlayPhysicalAttack();
            }
        }
    }

    private void PlayPhysicalAttack()
    {
        string attackName = Random.value > 0.5f ? "Attack 1" : "Attack 2";
        ExecuteAttack(attackName);
    }

    private void PlayLaserAttack()
    {
        string attackName = Random.value > 0.5f ? "Attack 3" : "Attack 4";
        ExecuteAttack(attackName);
    }

    private void ExecuteAttack(string triggerName)
    {
        animator.Play(triggerName, 0, 0f);
    }

    public void TriggerStun()
    {
        if (currentState == BossState.Defeated) return;

        CancelInvoke(nameof(ProceedToIdle));
        currentState = BossState.Stunned;
        _stunEndTime = Time.time + bossManager.stunDuration;
        
        animator.Play("Stunned", 0, 0f); 
        UpdateAnimatorParameters();
        DisableAllHitboxes();
    }

    public void OnAttackComplete()
    {
        if (currentState == BossState.Defeated) return;
        _nextAttackTime = Time.time + Random.Range(minAttackInterval, maxAttackInterval);
        currentState = BossState.Idle;
    }
    
    public void ProceedToIdle()
    {
        if (currentState == BossState.Defeated) return;
        currentState = BossState.Idle;
    }

    private void DisableAllHitboxes()
    {
        SetLeftArmHitbox(false);
        SetRightArmHitbox(false);
        SetLeftLaserHitbox(false);
        SetRightLaserHitbox(false);
    }

    private void DisableAllHurtboxes()
    {
        SetHeadHurtbox(false);
        SetLeftArmHurtbox(false);
        SetRightArmHurtbox(false);
        SetLeftChestHurtbox(false);
        SetRightChestHurtbox(false);
    }
    
    // Hitbox Toggles
    public void SetLeftArmHitbox(bool active) { if (leftArmHitbox != null) leftArmHitbox.SetActive(active); }
    public void SetRightArmHitbox(bool active) { if (rightArmHitbox != null) rightArmHitbox.SetActive(active); }
    public void SetLeftLaserHitbox(bool active) { if (leftLaserHitbox != null) leftLaserHitbox.SetActive(active); }
    public void SetRightLaserHitbox(bool active) { if (rightLaserHitbox != null) rightLaserHitbox.SetActive(active); }

    // Hurtbox Toggles
    public void SetHeadHurtbox(bool active) { if (headHurtbox != null) headHurtbox.SetActive(active); }
    public void SetLeftArmHurtbox(bool active) { if (leftArmHurtbox != null) leftArmHurtbox.SetActive(active); }
    public void SetRightArmHurtbox(bool active) { if (rightArmHurtbox != null) rightArmHurtbox.SetActive(active); }
    public void SetLeftChestHurtbox(bool active) { if (leftChestHurtbox != null) leftChestHurtbox.SetActive(active); }
    public void SetRightChestHurtbox(bool active) { if (rightChestHurtbox != null) rightChestHurtbox.SetActive(active); }
}