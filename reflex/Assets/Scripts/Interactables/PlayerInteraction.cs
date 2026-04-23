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
        interactAction = playerManager.playerInput.actions.FindAction("Interact");
        interactAction?.Enable();
    }

    void Update()
    {
        FindBestInteractable();

        if (currentInteractable != null && interactAction.triggered)
        {
            currentInteractable.Interact(playerManager);
        }
    }

    private void FindBestInteractable()
{
    Collider[] colliders = Physics.OverlapSphere(transform.position, interactRange);
    IInteractable closest = null;
    float minDistance = float.MaxValue;
    GameObject closestObj = null; // Track the physical object too

    foreach (var col in colliders)
    {
        if (col.TryGetComponent<IInteractable>(out IInteractable interactable))
        {
            float dist = Vector3.Distance(transform.position, col.transform.position);
            if (dist < minDistance)
            {
                minDistance = dist;
                closest = interactable;
                closestObj = col.gameObject; // Store the object reference
            }
        }
    }

    if (closest != null && closestObj != null)
    {
        currentInteractable = closest;
        // Pass the text AND the position of the object
        uiElement.Show(closest.GetInteractionText(), closestObj.transform.position); 
    }
    else
    {
        currentInteractable = null;
        uiElement.Hide();
    }
}
}