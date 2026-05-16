using System.Collections.Generic;
using UnityEngine;

public class LazerKnockback : MonoBehaviour
{
    [Header("Knockback")]
    public float knockbackDistance = 4f;
    public float knockbackDuration = 0.18f;
    public float knockbackCooldown = 0.35f;
    public bool useIncomingDirection = true;
    public bool fallbackAwayFromLazer = true;
    public float minimumIncomingSpeed = 0.1f;

    [Header("Detection")]
    public bool searchParentForPlayer = true;

    private readonly Dictionary<PlayerManager, float> fallbackNextKnockbackTimeByPlayer = new Dictionary<PlayerManager, float>();

    private void OnTriggerEnter(Collider other)
    {
        TryKnockback(other);
    }

    private void OnTriggerStay(Collider other)
    {
        TryKnockback(other);
    }

    private void OnDisable()
    {
        fallbackNextKnockbackTimeByPlayer.Clear();
    }

    private void TryKnockback(Collider other)
    {
        if (!TryGetPlayer(other, out PlayerManager player))
        {
            return;
        }

        TryKnockback(player, Vector3.zero);
    }

    public bool TryKnockback(PlayerManager player, Vector3 incomingDirection, bool ignoreCooldown = false)
    {
        if (!isActiveAndEnabled || player == null)
        {
            return false;
        }

        Vector3 direction = GetKnockbackDirection(player, incomingDirection);
        PlayerMovementManagement movement = player.GetComponent<PlayerMovementManagement>();

        if (movement != null)
        {
            return movement.TryApplyHazardKnockback(direction, knockbackDistance, knockbackDuration, knockbackCooldown, ignoreCooldown);
        }

        if (!ignoreCooldown && fallbackNextKnockbackTimeByPlayer.TryGetValue(player, out float nextTime) && Time.time < nextTime)
        {
            return false;
        }

        if (player.TryGetComponent(out CharacterController controller))
        {
            controller.Move(direction * knockbackDistance);
            fallbackNextKnockbackTimeByPlayer[player] = Time.time + knockbackCooldown;
            return true;
        }

        return false;
    }

    private bool TryGetPlayer(Collider other, out PlayerManager player)
    {
        if (other.TryGetComponent(out player))
        {
            return true;
        }

        if (searchParentForPlayer)
        {
            player = other.GetComponentInParent<PlayerManager>();
            return player != null;
        }

        return false;
    }

    private Vector3 GetKnockbackDirection(PlayerManager player, Vector3 incomingDirection)
    {
        incomingDirection.y = 0f;
        if (useIncomingDirection && incomingDirection.sqrMagnitude >= minimumIncomingSpeed * minimumIncomingSpeed)
        {
            return -incomingDirection.normalized;
        }

        if (useIncomingDirection && player.TryGetComponent(out CharacterController controller))
        {
            Vector3 incoming = controller.velocity;
            incoming.y = 0f;

            if (incoming.sqrMagnitude >= minimumIncomingSpeed * minimumIncomingSpeed)
            {
                return -incoming.normalized;
            }
        }

        if (fallbackAwayFromLazer)
        {
            Vector3 awayFromLazer = player.transform.position - transform.position;
            awayFromLazer.y = 0f;

            if (awayFromLazer.sqrMagnitude > 0.0001f)
            {
                return awayFromLazer.normalized;
            }
        }

        Vector3 backward = -player.transform.forward;
        backward.y = 0f;
        return backward.sqrMagnitude > 0.0001f ? backward.normalized : Vector3.back;
    }

    private void OnValidate()
    {
        knockbackDistance = Mathf.Max(0f, knockbackDistance);
        knockbackDuration = Mathf.Max(0.01f, knockbackDuration);
        knockbackCooldown = Mathf.Max(0f, knockbackCooldown);
        minimumIncomingSpeed = Mathf.Max(0f, minimumIncomingSpeed);
    }
}
