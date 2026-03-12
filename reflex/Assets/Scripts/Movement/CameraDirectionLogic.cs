using UnityEngine;

public static class CameraDirectionLogic
{
    /// <summary>
    /// Converts raw 2D input into a 3D direction relative to the provided camera.
    /// This ensures that "Up" on the screen always moves the player "Forward".
    /// </summary>
    public static Vector3 GetRelativeDirection(Vector2 input, Camera cam)
    {
        // Safety check to prevent errors if the camera is missing
        if (cam == null) return Vector3.zero;

        // Get the camera's forward and right vectors
        Vector3 forward = cam.transform.forward;
        Vector3 right = cam.transform.right;

        // Flatten the vectors on the Y-axis so the player doesn't 
        // accidentally move up/down into the air or floor.
        forward.y = 0;
        right.y = 0;
        
        // Normalize the vectors so diagonal movement isn't faster
        forward.Normalize();
        right.Normalize();

        // Calculate and return the final 3D direction
        return (forward * input.y) + (right * input.x);
    }
}