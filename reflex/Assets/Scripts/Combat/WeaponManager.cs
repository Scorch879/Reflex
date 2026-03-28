using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections;
using System;

public class WeaponManager : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private PlayerAnimation playerVisuals;
    public WeaponData currentWeaponData;
    public GameObject hitboxVisual;
    public LayerMask enemyLayer;

    [Header("Input")]
    private PlayerInput userInput;
    private InputAction attackAction;

    [Header("Combo & Cooldown State")]
    private int currentComboIndex = 0;
    private float lastAttackTime;
    [SerializeField] private float comboTime = 0f;
    private bool startResetTime = false;


    [SerializeField] private bool canAttack = true;
    [SerializeField] private bool toIdle = false;

    void Start()
    {
        userInput = GetComponent<PlayerInput>();
        if (userInput != null)
        {
            // Update this string to match your Input Action Asset exactly
            attackAction = userInput.actions.FindAction("PlayerMovementAction/Attack");
            if (attackAction != null) attackAction.Enable();
        }

        // Initialize the animator with the current weapon's look
        if (currentWeaponData != null && currentWeaponData.weaponOverride != null)
        {
            playerVisuals.SwapWeaponAnimations(currentWeaponData.weaponOverride);
        }
    }

    void Update()
    {
        if (attackAction != null && attackAction.triggered)
        {
            if (canAttack)
            {
                ExecuteAttack();
            }
        }
        UpdateTime();   
    }


    private void UpdateTime()
    {
        // If the animation hasn't finished yet, don't count down
        if(!startResetTime) return;

        if(comboTime <= 0)
        {   
            if(currentComboIndex > 0) { ResetComboTime(); }
            return;
        }

        comboTime -= Time.deltaTime;
    }

    private void ResetComboTime()
    {
        toIdle = true;
        currentComboIndex = 0;
        comboTime = currentWeaponData.comboResetTime;
        playerVisuals.GoToIdle();
        canAttack = true;
        startResetTime = false;
    }
    // only use this in animation events
    private void CanAttackEvent()
    {
        if (currentWeaponData == null) return;
        canAttack = !canAttack;
        toIdle = false;
    }

    // use this to call publicly
    public bool CanAttackLocal(bool attack)
    {
        if (currentWeaponData == null) return false;
        canAttack = attack;
        return canAttack;
    }
    

    private void ExecuteAttack()
    {
        if (currentWeaponData == null || currentWeaponData.comboChain.Length == 0) return;
        startResetTime = false;
        currentComboIndex++;
        if(currentComboIndex > currentWeaponData.comboChain.Length)
        {
            currentComboIndex = currentWeaponData.comboChain.Length;
        }
        AttackStep step = currentWeaponData.comboChain[currentComboIndex-1];
        comboTime = currentWeaponData.comboResetTime;


        // 1. Tell Visuals to play the specific animation for this combo hit
        playerVisuals.PlayAttack(currentComboIndex-1, currentWeaponData.weaponName);

        // 2. Physical Hitbox Scaling
        UpdateHitboxTransform(step);
        lastAttackTime = Time.time;
        
    }

    private void UpdateHitboxTransform(AttackStep step)
    {
        hitboxVisual.transform.localScale = new Vector3(step.attackWidth, step.verticalScale, step.attackRange);
        hitboxVisual.transform.localPosition = new Vector3(0, 0, step.attackRange / 2f);
    }

    //Anim Event --| 
    //             v
    public void HitboxOn()
    {
        hitboxVisual.SetActive(true);

        // 1. Get the current World Position, Rotation, and Scale of the hitbox
        // We use lossyScale / 2 because OverlapBox expects "half-extents"
        Vector3 center = hitboxVisual.transform.position;
        Vector3 halfExtents = hitboxVisual.transform.lossyScale / 2f;
        Quaternion orientation = hitboxVisual.transform.rotation;

        // 2. Perform the Physics Check
        // This looks for anything on the 'Enemy' layer inside that yellow box
        Collider[] hitEnemies = Physics.OverlapBox(center, halfExtents, orientation, enemyLayer);

        // 3. Handle the Results
        if (hitEnemies.Length > 0)
        {
            foreach (Collider enemy in hitEnemies)
            {
                Debug.Log($"<color=red>HIT CONFIRMED:</color> Dealt damage to {enemy.name}");

                // This is where you will eventually call enemy.TakeDamage()
            }
        }
        else
        {
            Debug.Log("<color=white>Attack missed.</color> No enemies found in range.");
        }
    }

    public void HitboxOff()
    {
        hitboxVisual.SetActive(false);
    }

    public void StartResetTime()
    {
        // This allows the timer to start ticking in Update()
        startResetTime = true; 
        
        // Set the initial duration from your WeaponData
        comboTime = currentWeaponData.comboResetTime;
    }

//this is for debugging purposes can remove or delete
    private void OnDrawGizmos()
    {
        if (hitboxVisual != null && hitboxVisual.activeInHierarchy)
        {
            Gizmos.color = Color.yellow;
            Gizmos.matrix = hitboxVisual.transform.localToWorldMatrix;
            // Draw a wireframe cube that matches the hitbox scale
            Gizmos.DrawWireCube(Vector3.zero, Vector3.one);
        }
    }
}