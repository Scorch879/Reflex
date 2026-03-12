using UnityEngine;
using UnityEngine.InputSystem;

public class WeaponManager : MonoBehaviour
{
    [SerializeField] private Animator playerAnim;
    public BaseWeapon currentWeapon; 

    private PlayerInput userInput;
    private InputAction attackAction;

    void Start()
{
    userInput = GetComponent<PlayerInput>();
    
    if (userInput != null)
    {
        // We use the Map Name / Action Name format for precision
        attackAction = userInput.actions.FindAction("PlayerMovementAction/Attack");
        
        if (attackAction != null)
        {
            attackAction.Enable();
        }
    }
}

    void Update()
    {
        if (attackAction != null && attackAction.triggered && currentWeapon != null)
        {
            currentWeapon.PerformAttack(playerAnim);
        }
    }
}