using UnityEngine;

public static class CameraDirectionLogic
{
    /// <summary>
    /// Converts raw 2D input into a 3D direction relative to the provided camera.
    /// </summary>
    public static Vector3 GetRelativeDirection(Vector2 input, Camera cam)
    {
        if (cam == null) return Vector3.zero;

        Vector3 forward = cam.transform.forward;
        Vector3 right = cam.transform.right;

        // Flatten the vectors so the player doesn't move vertically
        forward.y = 0;
        right.y = 0;
        
        forward.Normalize();
        right.Normalize();

        return (forward * input.y) + (right * input.x);
    }
}