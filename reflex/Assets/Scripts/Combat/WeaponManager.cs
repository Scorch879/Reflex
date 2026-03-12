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
        // Using "Attack" to match your common setup
        attackAction = userInput.actions.FindAction("Attack");
        attackAction?.Enable();
    }

    void Update()
    {
        if (attackAction != null && attackAction.triggered && currentWeapon != null)
        {
            currentWeapon.PerformAttack(playerAnim);
        }
    }
}