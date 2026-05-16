using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerInteraction : MonoBehaviour
{
    [SerializeField] private PlayerManager playerManager;
    [SerializeField] private InteractionUI uiElement; // Drag your UI script here
    [SerializeField] private float interactRange = 2.5f;

    private IInteractable currentInteractable;
    private InputAction interactAction;

    void Start()
    {
        if (playerManager == null)
        {
            playerManager = GetComponent<PlayerManager>();
        }

        if (playerManager == null || playerManager.playerInput == null)
        {
            enabled = false;
            return;
        }

        interactAction = playerManager.playerInput.actions.FindAction("Interact");
        interactAction?.Enable();
    }

    void Update()
    {
        FindBestInteractable();

        if (currentInteractable != null && interactAction != null && interactAction.triggered)
        {
            currentInteractable.Interact(playerManager);
        }
    }

    private void FindBestInteractable()
    {
        Collider[] colliders = Physics.OverlapSphere(transform.position, interactRange);
        IInteractable closest = null;
        float minDistance = float.MaxValue;
        Transform closestTransform = null;

        foreach (var col in colliders)
        {
            IInteractable interactable;
            if (!col.TryGetComponent(out interactable))
            {
                interactable = col.GetComponentInParent<IInteractable>();
            }

            if (interactable == null)
            {
                continue;
            }

            Component interactableComponent = interactable as Component;
            Transform promptTransform = interactableComponent != null ? interactableComponent.transform : col.transform;
            float dist = Vector3.Distance(transform.position, promptTransform.position);

            if (dist < minDistance)
            {
                minDistance = dist;
                closest = interactable;
                closestTransform = promptTransform;
            }
        }

        if (closest != null && closestTransform != null)
        {
            currentInteractable = closest;

            if (uiElement != null)
            {
                uiElement.Show(closest.GetInteractionText(), closestTransform.position);
            }
        }
        else
        {
            currentInteractable = null;

            if (uiElement != null)
            {
                uiElement.Hide();
            }
        }
    }
}
