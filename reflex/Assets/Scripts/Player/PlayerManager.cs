using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerManager : MonoBehaviour
{
    public bool isRunning = false;
    public bool isAttacking = false;
    public bool isIdle = true;
    public int comboCount = 0;
    public int currentComboIndex = 0;
    public bool canAttack = true;
    public float comboTime;
    public bool canGoToIdle = true;

    public PlayerInput playerInput;
    public WeaponData weaponData;

    private void Update()
    {
        CheckIfIdle();
        CheckIfAttacking();
        CheckComboTime();
    }

    private void CheckIfIdle()
    {
        if(isRunning || isAttacking)
        {
            isIdle = false;
        }
        else
        {
            isIdle = true;
        }
    }

    private void CheckComboTime()
    {
        if(comboTime < 0)
        {
            comboTime = 0;
        }
    }

    private void CheckIfAttacking()
    {
        isAttacking = !canAttack;
    }
}
