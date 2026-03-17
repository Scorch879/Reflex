using UnityEngine;

[System.Serializable]
public struct AttackStep
{
    public float attackRange;
    public float attackWidth;
    public float verticalScale;
    public float attackDamage;
    public float attackStunDuration;
    public float activeTime; // How long the red box stays visible
}

[CreateAssetMenu(fileName = "NewWeaponData", menuName = "Combat/Weapon Data")]
public class WeaponData : ScriptableObject
{
    public string weaponName;
    public float attackRate = 0.5f;

    public AnimatorOverrideController weaponOverride;

    [Header("Combo Settings")]
    public AttackStep[] comboChain; // Set size to 3 in the Inspector
    public float comboResetTime = 1.0f; 
}