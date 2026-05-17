using UnityEngine;

public class UpgradeStation : MonoBehaviour, IInteractable
{
    [SerializeField] private string interactPrompt = "[E] Open Upgrade Station";

    public string GetInteractionText()
    {
        return interactPrompt;
    }

    public void Interact(PlayerManager player)
    {
        if (UpgradeUIManager.Instance != null)
        {
            // Toggle UI based on whether it is currently active or not
            if (UpgradeUIManager.Instance.upgradePanel.activeSelf)
            {
                UpgradeUIManager.Instance.CloseUI();
            }
            else
            {
                UpgradeUIManager.Instance.OpenUI(player);
            }
        }
        else
        {
            Debug.LogWarning("UpgradeUIManager Instance is missing in the scene!");
        }
    }
}