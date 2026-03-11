using UnityEngine;
using UnityEngine.U2D.Animation;

public class PerspectiveChanger : MonoBehaviour
{
    [Tooltip("The camera that the sprite will face.")]
    public Camera cameraObj;

    [Tooltip("Sprites for 8 directions, ordered clockwise starting from Front:\n0: Front, 1: Front-Right, 2: Right, 3: Back-Right, 4: Back, 5: Back-Left, 6: Left, 7: Front-Left")]
    public SpriteLibraryAsset[] spriteSwapLib;

    [Tooltip("The SpriteRenderer to change sprites on.")]
    public SpriteLibrary spriteLib; 

    [Tooltip("The transform that defines the object's orientation (which way is 'front'). If not set, the parent transform will be used.")]
    public Transform orientationSource;
    public int dirIndexDisplay;
    private readonly string[] directionNames = { "Front", "Front-Right", "Right", "Back-Right", "Back", "Back-Left", "Left", "Front-Left" };

    private void LateUpdate()
    {
        // Always perform billboarding if the camera is set
        if(cameraObj == null) return;
        UpdatePerspective();
    }

    private void UpdatePerspective()
    {
        Transform source = orientationSource != null ? orientationSource : transform.parent;
        if (source == null) return;

        Vector3 forwardDir = source.forward;
        forwardDir.y = 0;

        Vector3 billboardForward = transform.forward;
        billboardForward.y = 0;

        float angle = Vector3.SignedAngle(forwardDir, -billboardForward, Vector3.up);

        int index = Mathf.RoundToInt(angle / 45f);
        
        // Wrap index to 0-7 range
        index = (index % 8 + 8) % 8;

        if (index != dirIndexDisplay)
        {
            Debug.Log($"<color=orange>Perspective:</color> {directionNames[index]} (Angle: {angle:F1}°)");
            dirIndexDisplay = index;
        }
        
        {
            if (spriteSwapLib.Length > index && spriteSwapLib[index] != null)
            {
                spriteLib.spriteLibraryAsset = spriteSwapLib[index];
            }
        }
    }
}
