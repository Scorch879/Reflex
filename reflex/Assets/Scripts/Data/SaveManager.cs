using UnityEngine;
using System.IO;
using System.Collections.Generic;

[System.Serializable]
public class SaveData
{
    public string equippedWeaponName;
    public int soulEssence;
    
    public int healthUpgradeLevel;
    public int damageUpgradeLevel;
    public int critUpgradeLevel;
}

public class SaveManager : MonoBehaviour
{
    public static SaveManager Instance { get; private set; }

    public SaveData currentSave;
    
    [Header("Available Weapons (For Reference)")]
    public List<WeaponData> availableWeapons;

    private string saveFilePath;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Bootstrap()
    {
        if (FindFirstObjectByType<SaveManager>() != null)
        {
            return;
        }

        GameObject saveManagerObject = new GameObject("SaveManager");
        saveManagerObject.AddComponent<SaveManager>();
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
        
        saveFilePath = Path.Combine(Application.persistentDataPath, "save_data.json");
        LoadGame();
    }

    public void SaveGame()
    {
        string json = JsonUtility.ToJson(currentSave, true);
        File.WriteAllText(saveFilePath, json);
        Debug.Log("Game Saved to " + saveFilePath);
    }

    public void LoadGame()
    {
        if (File.Exists(saveFilePath))
        {
            string json = File.ReadAllText(saveFilePath);
            currentSave = JsonUtility.FromJson<SaveData>(json);
            if (currentSave == null)
            {
                Debug.LogWarning("Save file exists but was unreadable. Creating a fresh save.");
                CreateDefaultSave();
                SaveGame();
                return;
            }

            Debug.Log("Game Loaded from " + saveFilePath);
        }
        else
        {
            CreateDefaultSave();
            SaveGame();
        }
    }

    private void CreateDefaultSave()
    {
        currentSave = new SaveData
        {
            equippedWeaponName = "", // Empty implies default
            soulEssence = 0,
            healthUpgradeLevel = 0,
            damageUpgradeLevel = 0,
            critUpgradeLevel = 0
        };
    }

    public void RefreshPlayerStats()
    {
        PlayerManager playerManager = FindFirstObjectByType<PlayerManager>();
        if (playerManager != null)
        {
            ApplyToPlayer(playerManager);
        }
    }

    public void ApplyToPlayer(PlayerManager playerManager)
    {
        playerManager.soulEssence = currentSave.soulEssence;

        // Apply weapon if it's found and differs from the current one
        if (!string.IsNullOrEmpty(currentSave.equippedWeaponName) && availableWeapons != null)
        {
            WeaponData weaponToEquip = availableWeapons.Find(w => w.weaponName == currentSave.equippedWeaponName);
            if (weaponToEquip != null)
            {
                WeaponManager weaponManager = playerManager.GetComponent<WeaponManager>();
                if (weaponManager != null)
                {
                    playerManager.weaponData = weaponToEquip;
                }
            }
        }
        
        // Let the UpgradeManager handle applying the permanent upgrade stats
        if (UpgradeManager.Instance != null)
        {
            UpgradeManager.Instance.ApplyUpgradesToPlayer();
        }
    }
    
    public void SetEquippedWeapon(string weaponName)
    {
        currentSave.equippedWeaponName = weaponName;
        SaveGame();
    }
}
