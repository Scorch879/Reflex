using UnityEngine;

public class UpgradeManager : MonoBehaviour
{
    public static UpgradeManager Instance { get; private set; }

    public UpgradeSettings settings;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    public bool TryUpgradeHealth()
    {
        if (settings == null) 
        {
            Debug.LogWarning("UpgradeSettings is missing on UpgradeManager!");
            return false;
        }

        SaveData data = SaveManager.Instance.currentSave;
        
        if (data.healthUpgradeLevel >= settings.maxUpgradeLevel) return false;
        
        int cost = settings.GetUpgradeCost(data.healthUpgradeLevel);
        if (data.soulEssence >= cost)
        {
            data.soulEssence -= cost;
            data.healthUpgradeLevel++;
            SaveManager.Instance.SaveGame();
            ApplyUpgradesToPlayer();
            return true;
        }
        return false;
    }
    
    public bool TryUpgradeDamage()
    {
        if (settings == null) 
        {
            Debug.LogWarning("UpgradeSettings is missing on UpgradeManager!");
            return false;
        }

        SaveData data = SaveManager.Instance.currentSave;
        
        if (data.damageUpgradeLevel >= settings.maxUpgradeLevel) return false;
        
        int cost = settings.GetUpgradeCost(data.damageUpgradeLevel);
        if (data.soulEssence >= cost)
        {
            data.soulEssence -= cost;
            data.damageUpgradeLevel++;
            SaveManager.Instance.SaveGame();
            ApplyUpgradesToPlayer();
            return true;
        }
        return false;
    }

    public bool TryUpgradeCrit()
    {
        if (settings == null) 
        {
            Debug.LogWarning("UpgradeSettings is missing on UpgradeManager!");
            return false;
        }

        SaveData data = SaveManager.Instance.currentSave;
        
        if (data.critUpgradeLevel >= settings.maxUpgradeLevel) return false;
        
        int cost = settings.GetUpgradeCost(data.critUpgradeLevel);
        if (data.soulEssence >= cost)
        {
            data.soulEssence -= cost;
            data.critUpgradeLevel++;
            SaveManager.Instance.SaveGame();
            ApplyUpgradesToPlayer();
            return true;
        }
        return false;
    }

    public void ApplyUpgradesToPlayer()
    {
        PlayerManager playerManager = FindFirstObjectByType<PlayerManager>();
        if (playerManager != null && settings != null)
        {
            SaveData data = SaveManager.Instance.currentSave;
            playerManager.permanentMaxHPBonus = data.healthUpgradeLevel * settings.healthPerLevel;
            playerManager.permanentAtkBonus = data.damageUpgradeLevel * settings.damagePerLevel;
            playerManager.permanentCritBonus = data.critUpgradeLevel * settings.critPerLevel;
            
            // Ensure UI updates properly if needed, cap current health to new max
            if (playerManager.currentHealth > playerManager.MaxHealth)
            {
                playerManager.currentHealth = playerManager.MaxHealth;
            }
        }
    }
}