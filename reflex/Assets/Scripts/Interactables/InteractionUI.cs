using UnityEngine;
using TMPro;

public class InteractionUI : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI promptText;
    [SerializeField] private GameObject visualRoot;
    [SerializeField] private Vector3 offset = new Vector3(0, 1.5f, 0); // Height above the object

    public void Show(string text, Vector3 worldPosition)
    {
        if (visualRoot != null) visualRoot.SetActive(true);
        if (promptText != null) promptText.text = text;

        // Move the UI to the object's position plus an offset so it floats above it
        transform.position = worldPosition + offset;
        
        // Optional: Make the UI always face the camera
        transform.LookAt(transform.position + Camera.main.transform.rotation * Vector3.forward,
                         Camera.main.transform.rotation * Vector3.up);
    }

    public void Hide()
    {
        if (visualRoot != null) visualRoot.SetActive(false);
    }
}