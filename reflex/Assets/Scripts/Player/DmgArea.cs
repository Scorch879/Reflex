using System.Collections.Generic;
using UnityEngine;

public class DmgArea : MonoBehaviour
{
    [Header("Damage")]
    public float damagePerSecond = 20f;
    public float damageOnEnter;
    public float damageTickInterval = 0.5f;
    public bool ignorePlayerInvulnerability;

    [Header("Detection")]
    public bool searchParentForPlayer = true;

    [Header("Editor")]
    public bool showGizmos = true;
    public Color gizmoColor = new Color(1f, 0.12f, 0.02f, 0.3f);

    private readonly Dictionary<PlayerManager, float> nextDamageTimeByPlayer = new Dictionary<PlayerManager, float>();
    private readonly Dictionary<PlayerManager, float> nextDashDamageTimeByPlayer = new Dictionary<PlayerManager, float>();
    private readonly Dictionary<PlayerManager, float> suppressEntryDamageUntilByPlayer = new Dictionary<PlayerManager, float>();

    private void OnTriggerEnter(Collider other)
    {
        if (!TryGetPlayer(other, out PlayerManager player))
        {
            return;
        }

        if (ShouldSuppressEntryDamage(player))
        {
            nextDamageTimeByPlayer[player] = damageOnEnter > 0f ? Time.time + damageTickInterval : Time.time;
            return;
        }

        ApplyEntryDamage(player);
    }

    public bool TryApplyDashDamage(PlayerManager player)
    {
        if (!isActiveAndEnabled || player == null)
        {
            return false;
        }

        float cooldown = Mathf.Max(0.05f, damageTickInterval);
        if (nextDashDamageTimeByPlayer.TryGetValue(player, out float nextDashDamageTime) && Time.time < nextDashDamageTime)
        {
            return false;
        }

        bool appliedDamage = ApplyDashDamage(player);
        if (!appliedDamage)
        {
            return false;
        }

        nextDashDamageTimeByPlayer[player] = Time.time + cooldown;
        suppressEntryDamageUntilByPlayer[player] = Time.time + Mathf.Max(0.1f, Time.fixedDeltaTime * 3f);
        nextDamageTimeByPlayer[player] = Time.time + cooldown;
        return true;
    }

    private void ApplyEntryDamage(PlayerManager player)
    {
        bool appliedEntryDamage = damageOnEnter > 0f;
        if (appliedEntryDamage)
        {
            player.TakeDamage(damageOnEnter, ignorePlayerInvulnerability);
        }

        nextDamageTimeByPlayer[player] = appliedEntryDamage ? Time.time + damageTickInterval : Time.time;
    }

    private bool ApplyDashDamage(PlayerManager player)
    {
        if (damageOnEnter > 0f)
        {
            player.TakeDamage(damageOnEnter, ignorePlayerInvulnerability);
            return true;
        }

        if (damagePerSecond <= 0f)
        {
            return false;
        }

        float damageAmount = damageTickInterval > 0f
            ? damagePerSecond * damageTickInterval
            : damagePerSecond * Time.deltaTime;

        player.TakeDamage(damageAmount, ignorePlayerInvulnerability);
        return true;
    }

    private void OnTriggerStay(Collider other)
    {
        if (damagePerSecond <= 0f || !TryGetPlayer(other, out PlayerManager player))
        {
            return;
        }

        if (damageTickInterval <= 0f)
        {
            player.TakeDamage(damagePerSecond * Time.deltaTime, ignorePlayerInvulnerability);
            return;
        }

        float nextDamageTime = 0f;
        nextDamageTimeByPlayer.TryGetValue(player, out nextDamageTime);

        if (Time.time < nextDamageTime)
        {
            return;
        }

        player.TakeDamage(damagePerSecond * damageTickInterval, ignorePlayerInvulnerability);
        nextDamageTimeByPlayer[player] = Time.time + damageTickInterval;
    }

    private void OnTriggerExit(Collider other)
    {
        if (TryGetPlayer(other, out PlayerManager player))
        {
            nextDamageTimeByPlayer.Remove(player);
        }
    }

    private void OnDisable()
    {
        nextDamageTimeByPlayer.Clear();
        nextDashDamageTimeByPlayer.Clear();
        suppressEntryDamageUntilByPlayer.Clear();
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

    private bool ShouldSuppressEntryDamage(PlayerManager player)
    {
        if (!suppressEntryDamageUntilByPlayer.TryGetValue(player, out float suppressUntil))
        {
            return false;
        }

        if (Time.time < suppressUntil)
        {
            return true;
        }

        suppressEntryDamageUntilByPlayer.Remove(player);
        return false;
    }

    private void OnValidate()
    {
        damagePerSecond = Mathf.Max(0f, damagePerSecond);
        damageOnEnter = Mathf.Max(0f, damageOnEnter);
        damageTickInterval = Mathf.Max(0f, damageTickInterval);
    }

    private void OnDrawGizmosSelected()
    {
        if (!showGizmos)
        {
            return;
        }

        Collider damageCollider = GetComponent<Collider>();
        if (damageCollider == null)
        {
            return;
        }

        Gizmos.color = gizmoColor;
        Gizmos.matrix = transform.localToWorldMatrix;

        if (damageCollider is BoxCollider boxCollider)
        {
            Gizmos.DrawCube(boxCollider.center, boxCollider.size);
            Gizmos.color = new Color(gizmoColor.r, gizmoColor.g, gizmoColor.b, 0.9f);
            Gizmos.DrawWireCube(boxCollider.center, boxCollider.size);
        }
    }
}
