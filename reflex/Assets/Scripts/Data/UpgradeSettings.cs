using UnityEngine;

[CreateAssetMenu(fileName = "NewUpgradeSettings", menuName = "Reflex/Upgrade Settings")]
public class UpgradeSettings : ScriptableObject
{
    [Header("Upgrade Limits")]
    public int maxUpgradeLevel = 10;
    
    [Header("Cost Settings")]
    public int baseUpgradeCost = 50;
    [Tooltip("The cost of the next upgrade is: baseUpgradeCost * (upgradeCostMultiplier ^ currentLevel)")]
    public float upgradeCostMultiplier = 1.5f;

    [Header("Upgrade Values Per Level")]
    public float healthPerLevel = 10f;
    public float damagePerLevel = 0.05f; // +5% Base Damage
    public float critPerLevel = 0.01f;   // +1% Crit Rate

    public int GetUpgradeCost(int currentLevel)
    {
        return Mathf.RoundToInt(baseUpgradeCost * Mathf.Pow(upgradeCostMultiplier, currentLevel));
    }
}