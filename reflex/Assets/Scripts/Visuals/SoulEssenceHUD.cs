using UnityEngine;
using TMPro; // Assuming you are using TextMeshPro for text

public class SoulEssenceHUD : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI essenceText;
    private PlayerManager playerManager;

    private void Start()
    {
        // We find the player in the scene
        playerManager = FindFirstObjectByType<PlayerManager>();

        if (playerManager != null)
        {
            // Subscribe to the event so it updates instantly when picking up essence
            playerManager.SoulEssenceChanged += UpdateEssenceUI;
            
            // Set initial value
            UpdateEssenceText(playerManager.soulEssence);
        }
        else if (SaveManager.Instance != null)
        {
            UpdateEssenceText(SaveManager.Instance.currentSave.soulEssence);
        }
    }

    private void OnDestroy()
    {
        if (playerManager != null)
        {
            playerManager.SoulEssenceChanged -= UpdateEssenceUI;
        }
    }

    private void UpdateEssenceUI(int totalEssence, int amountAdded)
    {
        UpdateEssenceText(totalEssence);
    }

    public void UpdateEssenceText(int amount)
    {
        if (essenceText != null)
        {
            essenceText.text = "Essence: " + amount.ToString();
        }
    }

    // Call this if the player spends essence so the UI refreshes
    public void RefreshHUD()
    {
        if (playerManager != null)
        {
            UpdateEssenceText(playerManager.soulEssence);
        }
        else if (SaveManager.Instance != null)
        {
            UpdateEssenceText(SaveManager.Instance.currentSave.soulEssence);
        }
    }
}