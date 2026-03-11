using UnityEngine;
using UnityEngine.InputSystem;

public class WeaponManager : MonoBehaviour
{
    [SerializeField] private Animator playerAnim;
    [SerializeField] private BaseWeapon currentWeapon;

    private PlayerInput userInput;
    private InputAction attackAction;

    void Start()
    {
        userInput = GetComponent<PlayerInput>();

        // Check if userInput exists first
        if (userInput != null)
        {
            attackAction = userInput.actions.FindAction("Attack");

            if (attackAction != null)
            {
                attackAction.Enable();
            }
            else
            {
                Debug.LogError("WeaponManager: Could not find an action named 'Attack'! Check your Input Action Asset.");
            }
        }
        else
        {
            Debug.LogError("WeaponManager: No PlayerInput component found on this GameObject!");
        }
    }

    void Update()
    {
        if (attackAction != null && attackAction.triggered)
        {
            // ADD THIS LINE
            Debug.Log("<color=cyan>Input:</color> Left Click detected in WeaponManager");

            if (currentWeapon != null)
            {
                currentWeapon.PerformAttack(playerAnim);
            }
            else
            {
                // Helpful warning for the editor
                Debug.LogWarning("Left Click pressed, but no Weapon is assigned to the WeaponManager!");
            }
        }
    }
    // Call this when selecting a weapon before the run starts
    public void EquipWeapon(BaseWeapon weaponPrefab)
    {
        // Logic to instantiate the weapon and parent it to your hand
        currentWeapon = weaponPrefab;
    }
}