using UnityEngine;

[CreateAssetMenu(fileName = "NewMovementStats", menuName = "ScriptableObjects/MovementStats")]
public class DefaultMovementStats : ScriptableObject
{
    public float movementSpeed = 5f;
    public float sprintSpeed = 10f;
    public float JumpHeight = 1.2f;
    public float gravity = 15f;
    public float acceleration = 50f;
    public float deceleration = 60f;
    public float airAcceleration = 5f;
    public float airDeceleration = 2f;
    public float deadZone = 0.1f;
    public float dashSpeed = 20f;
    public float dashDuration = 0.2f;
    public float dashCooldown = 1.0f;
}