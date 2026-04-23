using UnityEngine;

[CreateAssetMenu(fileName = "NewPlayerData", menuName = "Stats/Player Data")]
public class PlayerData : ScriptableObject
{
    [Header("Base Vitality")]
    public float baseMaxHealth = 100f;
    
    [Header("Base Offense")]
    public float baseDamageMultiplier = 1.0f; // 1.0 = 100% damage
    
    [Header("Base Movement")]
    public float baseMoveSpeed = 5.0f;
}