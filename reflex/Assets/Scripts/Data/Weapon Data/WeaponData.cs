using UnityEngine;

[System.Serializable]
public struct AttackStep
{
    public float attackRange;
    public float attackWidth;
    public float verticalScale;
    public float attackDamage;
    public float attackStunDuration;
    public float dashInDistance; // New field for dash-in distance
    public float dashInVelocity; // New field for dash-in velocity
    public float attackKnockbackForce; // New field for attack knockback force
    [Header("Camera Shake Settings")]
    public float cameraShakeIntensity; // New field for camera shake intensity
    public float cameraShakeDuration; // New field for camera shake duration
    public float cameraShakeFrequency; // New field for camera shake frequency
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

    [Header("Visuals")]
    public Sprite weaponIcon;
}