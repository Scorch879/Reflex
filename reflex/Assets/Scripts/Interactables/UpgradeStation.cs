using UnityEngine;

public class UpgradeStation : MonoBehaviour, IInteractable
{
    [SerializeField] private string interactPrompt = "[E] Open Upgrade Station";
    [SerializeField] private bool autoEnsureTriggerCollider = true;
    [SerializeField] private Vector3 interactionColliderCenter = new Vector3(0f, 1.15f, 0f);
    [SerializeField] private Vector3 interactionColliderSize = new Vector3(2f, 2.3f, 2f);

    private void Awake()
    {
        if (autoEnsureTriggerCollider)
        {
            EnsureInteractionCollider();
        }
    }

    private void OnValidate()
    {
        if (!autoEnsureTriggerCollider)
        {
            return;
        }

        EnsureInteractionCollider();
    }

    public string GetInteractionText()
    {
        return interactPrompt;
    }

    public void Interact(PlayerManager player)
    {
        if (UpgradeUIManager.Instance == null)
        {
            Debug.LogWarning("UpgradeUIManager instance is missing. Upgrade station cannot open UI.");
            return;
        }

        if (UpgradeUIManager.Instance.IsOpen)
        {
            UpgradeUIManager.Instance.CloseUI();
            return;
        }

        UpgradeUIManager.Instance.OpenUI(player);
    }

    private void EnsureInteractionCollider()
    {
        Collider existingCollider = GetComponent<Collider>();
        BoxCollider boxCollider = existingCollider as BoxCollider;
        if (boxCollider == null)
        {
            boxCollider = gameObject.AddComponent<BoxCollider>();
        }

        boxCollider.isTrigger = true;
        boxCollider.center = interactionColliderCenter;
        boxCollider.size = interactionColliderSize;
    }
}
