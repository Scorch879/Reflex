public interface IInteractable
{
    string GetInteractionText(); // New: Returns the string for the UI
    void Interact(PlayerManager player);
}