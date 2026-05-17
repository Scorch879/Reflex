using UnityEngine;

public class UpgradeManager : MonoBehaviour
{
    private enum UpgradeType
    {
        Health = 0,
        Damage = 1,
        Crit = 2
    }

    public static UpgradeManager Instance { get; private set; }

    [Header("Preferred Upgrade Tuning Asset")]
    public UpgradeSettings settings;

    [Header("Fallback Upgrade Tuning (Used when Settings is missing)")]
    [SerializeField, Min(1)] private int fallbackMaxUpgradeLevel = 10;
    [SerializeField, Min(0)] private int fallbackBaseUpgradeCost = 50;
    [SerializeField, Min(1f)] private float fallbackUpgradeCostMultiplier = 1.5f;
    [SerializeField] private float fallbackHealthPerLevel = 10f;
    [SerializeField] private float fallbackDamagePerLevel = 0.05f;
    [SerializeField] private float fallbackCritPerLevel = 0.01f;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Bootstrap()
    {
        if (FindFirstObjectByType<UpgradeManager>() != null)
        {
            return;
        }

        GameObject upgradeManagerObject = new GameObject("UpgradeManager");
        upgradeManagerObject.AddComponent<UpgradeManager>();
    }

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

    public bool TryUpgradeHealth() => TryUpgrade(UpgradeType.Health);

    public bool TryUpgradeDamage() => TryUpgrade(UpgradeType.Damage);

    public bool TryUpgradeCrit() => TryUpgrade(UpgradeType.Crit);

    private bool TryUpgrade(UpgradeType upgradeType)
    {
        if (SaveManager.Instance == null || SaveManager.Instance.currentSave == null)
        {
            Debug.LogWarning("SaveManager is not ready. Upgrade request ignored.");
            return false;
        }

        SaveData data = SaveManager.Instance.currentSave;
        int currentLevel = GetUpgradeLevel(data, upgradeType);
        int maxLevel = GetMaxUpgradeLevel();
        if (currentLevel >= maxLevel)
        {
            return false;
        }

        int cost = GetUpgradeCost(currentLevel);
        if (data.soulEssence < cost)
        {
            return false;
        }

        data.soulEssence -= cost;
        SetUpgradeLevel(data, upgradeType, currentLevel + 1);
        SaveManager.Instance.SaveGame();
        ApplyUpgradesToPlayer();
        return true;
    }

    public int GetMaxUpgradeLevel()
    {
        return settings != null
            ? Mathf.Max(1, settings.maxUpgradeLevel)
            : Mathf.Max(1, fallbackMaxUpgradeLevel);
    }

    public int GetUpgradeCost(int currentLevel)
    {
        if (settings != null)
        {
            return settings.GetUpgradeCost(currentLevel);
        }

        int safeLevel = Mathf.Max(0, currentLevel);
        return Mathf.RoundToInt(fallbackBaseUpgradeCost * Mathf.Pow(fallbackUpgradeCostMultiplier, safeLevel));
    }

    public float GetHealthPerLevel()
    {
        return settings != null ? settings.healthPerLevel : fallbackHealthPerLevel;
    }

    public float GetDamagePerLevel()
    {
        return settings != null ? settings.damagePerLevel : fallbackDamagePerLevel;
    }

    public float GetCritPerLevel()
    {
        return settings != null ? settings.critPerLevel : fallbackCritPerLevel;
    }

    private static int GetUpgradeLevel(SaveData data, UpgradeType upgradeType)
    {
        switch (upgradeType)
        {
            case UpgradeType.Health:
                return data.healthUpgradeLevel;
            case UpgradeType.Damage:
                return data.damageUpgradeLevel;
            case UpgradeType.Crit:
                return data.critUpgradeLevel;
            default:
                return 0;
        }
    }

    private static void SetUpgradeLevel(SaveData data, UpgradeType upgradeType, int newLevel)
    {
        switch (upgradeType)
        {
            case UpgradeType.Health:
                data.healthUpgradeLevel = newLevel;
                break;
            case UpgradeType.Damage:
                data.damageUpgradeLevel = newLevel;
                break;
            case UpgradeType.Crit:
                data.critUpgradeLevel = newLevel;
                break;
        }
    }

    public void ApplyUpgradesToPlayer()
    {
        if (SaveManager.Instance == null || SaveManager.Instance.currentSave == null)
        {
            return;
        }

        PlayerManager playerManager = FindFirstObjectByType<PlayerManager>();
        if (playerManager != null)
        {
            SaveData data = SaveManager.Instance.currentSave;
            playerManager.soulEssence = data.soulEssence;
            playerManager.permanentMaxHPBonus = data.healthUpgradeLevel * GetHealthPerLevel();
            playerManager.permanentAtkBonus = data.damageUpgradeLevel * GetDamagePerLevel();
            playerManager.permanentCritBonus = data.critUpgradeLevel * GetCritPerLevel();

            // Ensure current health stays valid after max-health upgrades or downgrades.
            if (playerManager.currentHealth > playerManager.MaxHealth)
            {
                playerManager.currentHealth = playerManager.MaxHealth;
            }

            if (InGameUIManager.Instance != null)
            {
                InGameUIManager.Instance.UpdateHealth(playerManager.currentHealth, playerManager.MaxHealth);
            }
        }
    }
}
