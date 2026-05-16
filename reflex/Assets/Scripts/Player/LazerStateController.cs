using System;
using System.Collections.Generic;
using UnityEngine;

public class LazerStateController : MonoBehaviour
{
    public enum LazerCondition
    {
        ManualOnly,
        AfterSeconds,
        AfterAllWatchedEnemiesKilled,
        PlayerPassesThrough
    }

    public enum RendererToggleMode
    {
        ManualList,
        AllChildRenderers,
        ChildNameContains,
        MaterialNameContains,
        ChildOrMaterialNameContains
    }

    public enum PlayerPassTriggerMoment
    {
        OnEnter,
        OnExitAfterEnter
    }

    [Header("State")]
    public bool activeOnStart = true;
    public bool hideRenderersWhenOff = true;

    [Header("Visual Toggle")]
    public RendererToggleMode rendererToggleMode = RendererToggleMode.ChildNameContains;
    public string rendererNameContains = "Lazer";
    public string materialNameContains = "Glow";
    public bool refreshRenderersOnStateChange = true;

    [Header("References")]
    public DmgArea[] damageAreas;
    public LazerKnockback[] knockbackAreas;
    public Renderer[] renderersToToggle;

    [Header("Turn Off")]
    public LazerCondition turnOffWhen = LazerCondition.ManualOnly;
    public float turnOffDelay = 3f;

    [Header("Turn On")]
    public LazerCondition turnOnWhen = LazerCondition.ManualOnly;
    public float turnOnDelay = 3f;

    [Header("Enemy Condition")]
    public EnemyController[] enemiesToWatch;
    public bool searchSceneEnemiesWhenListEmpty = true;
    public float sceneEnemyRefreshInterval = 0.5f;

    [Header("Player Pass Condition")]
    public bool searchParentForPlayer = true;
    public PlayerPassTriggerMoment playerPassTriggerMoment = PlayerPassTriggerMoment.OnEnter;
    public bool usePassThroughTriggerColliders = true;
    public Collider[] passThroughTriggerColliders;
    public bool autoFindPassThroughTriggersWhenEmpty = true;
    public string passThroughTriggerNameContains = "Cube";
    public bool autoAddPassThroughTriggerComponents = true;
    public bool forcePassThroughCollidersToTrigger = true;

    public bool IsLazerActive { get; private set; }

    private EnemyController[] sceneEnemies;
    private bool hasFoundSceneEnemies;
    private bool playerWasInside;
    private bool playerPassedThrough;
    private bool turnOffConditionUsed;
    private bool turnOnConditionUsed;
    private float stateChangedTime;
    private float nextSceneEnemyRefreshTime;

    private void Awake()
    {
        AutoFillReferences();
        SetLazerActive(activeOnStart);
        ResetConditionTriggers();
    }

    private void Update()
    {
        if (IsLazerActive)
        {
            if (!turnOffConditionUsed && IsConditionMet(turnOffWhen, turnOffDelay))
            {
                turnOffConditionUsed = true;
                SetLazerActive(false);
            }
        }
        else if (!turnOnConditionUsed && IsConditionMet(turnOnWhen, turnOnDelay))
        {
            turnOnConditionUsed = true;
            SetLazerActive(true);
        }
    }

    public void TurnOn()
    {
        SetLazerActive(true);
    }

    public void TurnOff()
    {
        SetLazerActive(false);
    }

    public void SetLazerActive(bool active)
    {
        IsLazerActive = active;
        stateChangedTime = Time.time;

        AutoFillReferences();

        foreach (DmgArea damageArea in damageAreas)
        {
            if (damageArea != null)
            {
                damageArea.enabled = active;
            }
        }

        foreach (LazerKnockback knockbackArea in knockbackAreas)
        {
            if (knockbackArea != null)
            {
                knockbackArea.enabled = active;
            }
        }

        if (hideRenderersWhenOff)
        {
            foreach (Renderer rendererToToggle in renderersToToggle)
            {
                if (rendererToToggle != null)
                {
                    rendererToToggle.enabled = active;
                }
            }
        }
    }

    public void ResetConditionTriggers()
    {
        turnOffConditionUsed = false;
        turnOnConditionUsed = false;
        playerPassedThrough = false;
        playerWasInside = false;
        stateChangedTime = Time.time;
    }

    public void NotifyPlayerEnteredPassThroughTrigger(PlayerManager player)
    {
        if (player == null)
        {
            return;
        }

        playerWasInside = true;

        if (playerPassTriggerMoment == PlayerPassTriggerMoment.OnEnter)
        {
            playerPassedThrough = true;
        }
    }

    public void NotifyPlayerExitedPassThroughTrigger(PlayerManager player)
    {
        if (player == null)
        {
            return;
        }

        if (playerPassTriggerMoment == PlayerPassTriggerMoment.OnExitAfterEnter && !playerWasInside)
        {
            return;
        }

        playerWasInside = false;
        playerPassedThrough = true;
    }

    public void NotifyPlayerPassedThrough(PlayerManager player)
    {
        if (player == null)
        {
            return;
        }

        playerWasInside = false;
        playerPassedThrough = true;
    }

    private bool IsConditionMet(LazerCondition condition, float delay)
    {
        switch (condition)
        {
            case LazerCondition.AfterSeconds:
                return Time.time >= stateChangedTime + delay;
            case LazerCondition.AfterAllWatchedEnemiesKilled:
                return AreAllWatchedEnemiesKilled();
            case LazerCondition.PlayerPassesThrough:
                if (!playerPassedThrough)
                {
                    return false;
                }

                playerPassedThrough = false;
                return true;
            default:
                return false;
        }
    }

    private bool AreAllWatchedEnemiesKilled()
    {
        EnemyController[] watchedEnemies = GetWatchedEnemies();
        if (watchedEnemies == null || watchedEnemies.Length == 0)
        {
            return hasFoundSceneEnemies;
        }

        foreach (EnemyController enemy in watchedEnemies)
        {
            if (enemy != null && enemy.isActiveAndEnabled && enemy.currentHealth > 0f)
            {
                return false;
            }
        }

        return true;
    }

    private EnemyController[] GetWatchedEnemies()
    {
        if (enemiesToWatch != null && enemiesToWatch.Length > 0)
        {
            return enemiesToWatch;
        }

        if (!searchSceneEnemiesWhenListEmpty)
        {
            return null;
        }

        if (Time.time >= nextSceneEnemyRefreshTime)
        {
            sceneEnemies = FindObjectsByType<EnemyController>(FindObjectsSortMode.None);
            hasFoundSceneEnemies |= sceneEnemies.Length > 0;
            nextSceneEnemyRefreshTime = Time.time + sceneEnemyRefreshInterval;
        }

        return sceneEnemies;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (TryGetPlayer(other, out PlayerManager player))
        {
            NotifyPlayerEnteredPassThroughTrigger(player);
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (TryGetPlayer(other, out PlayerManager player))
        {
            NotifyPlayerExitedPassThroughTrigger(player);
        }
    }

    public bool TryGetPlayer(Collider other, out PlayerManager player)
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

    private void AutoFillReferences()
    {
        if (damageAreas == null || damageAreas.Length == 0)
        {
            damageAreas = GetComponentsInChildren<DmgArea>(includeInactive: true);
        }

        if (knockbackAreas == null || knockbackAreas.Length == 0)
        {
            knockbackAreas = GetComponentsInChildren<LazerKnockback>(includeInactive: true);
        }

        if (renderersToToggle == null || renderersToToggle.Length == 0)
        {
            RefreshRendererToggleList();
        }
        else if (refreshRenderersOnStateChange && rendererToggleMode != RendererToggleMode.ManualList)
        {
            RefreshRendererToggleList();
        }

        if (usePassThroughTriggerColliders)
        {
            if ((passThroughTriggerColliders == null || passThroughTriggerColliders.Length == 0) && autoFindPassThroughTriggersWhenEmpty)
            {
                RefreshPassThroughTriggerColliders();
            }

            ConfigurePassThroughTriggerColliders();
        }
    }

    [ContextMenu("Refresh Renderer Toggle List")]
    public void RefreshRendererToggleList()
    {
        if (rendererToggleMode == RendererToggleMode.ManualList)
        {
            return;
        }

        Renderer[] childRenderers = GetComponentsInChildren<Renderer>(includeInactive: true);
        List<Renderer> matchingRenderers = new List<Renderer>();

        foreach (Renderer childRenderer in childRenderers)
        {
            if (childRenderer != null && ShouldToggleRenderer(childRenderer))
            {
                matchingRenderers.Add(childRenderer);
            }
        }

        renderersToToggle = matchingRenderers.ToArray();
    }

    [ContextMenu("Refresh Pass Through Trigger Colliders")]
    public void RefreshPassThroughTriggerColliders()
    {
        Collider[] childColliders = GetComponentsInChildren<Collider>(includeInactive: true);
        List<Collider> matchingColliders = new List<Collider>();

        foreach (Collider childCollider in childColliders)
        {
            if (childCollider == null || string.IsNullOrWhiteSpace(passThroughTriggerNameContains))
            {
                continue;
            }

            if (ContainsIgnoreCase(childCollider.gameObject.name, passThroughTriggerNameContains))
            {
                matchingColliders.Add(childCollider);
            }
        }

        passThroughTriggerColliders = matchingColliders.ToArray();
    }

    private void ConfigurePassThroughTriggerColliders()
    {
        if (passThroughTriggerColliders == null)
        {
            return;
        }

        foreach (Collider passThroughCollider in passThroughTriggerColliders)
        {
            if (passThroughCollider == null)
            {
                continue;
            }

            if (forcePassThroughCollidersToTrigger)
            {
                passThroughCollider.isTrigger = true;
            }

            LazerPassThroughTrigger passThroughTrigger = passThroughCollider.GetComponent<LazerPassThroughTrigger>();
            if (passThroughTrigger == null && autoAddPassThroughTriggerComponents)
            {
                passThroughTrigger = passThroughCollider.gameObject.AddComponent<LazerPassThroughTrigger>();
            }

            if (passThroughTrigger != null)
            {
                passThroughTrigger.Configure(this);
            }
        }
    }

    private bool ShouldToggleRenderer(Renderer rendererToCheck)
    {
        switch (rendererToggleMode)
        {
            case RendererToggleMode.AllChildRenderers:
                return true;
            case RendererToggleMode.ChildNameContains:
                return ContainsIgnoreCase(rendererToCheck.gameObject.name, rendererNameContains);
            case RendererToggleMode.MaterialNameContains:
                return HasMatchingMaterial(rendererToCheck, materialNameContains);
            case RendererToggleMode.ChildOrMaterialNameContains:
                return ContainsIgnoreCase(rendererToCheck.gameObject.name, rendererNameContains) ||
                       HasMatchingMaterial(rendererToCheck, materialNameContains);
            default:
                return false;
        }
    }

    private static bool HasMatchingMaterial(Renderer rendererToCheck, string materialFilter)
    {
        Material[] sharedMaterials = rendererToCheck.sharedMaterials;

        foreach (Material material in sharedMaterials)
        {
            if (material != null && ContainsIgnoreCase(material.name, materialFilter))
            {
                return true;
            }
        }

        return false;
    }

    private static bool ContainsIgnoreCase(string value, string filter)
    {
        return !string.IsNullOrWhiteSpace(filter) &&
               !string.IsNullOrEmpty(value) &&
               value.IndexOf(filter, StringComparison.OrdinalIgnoreCase) >= 0;
    }

    private void OnValidate()
    {
        turnOffDelay = Mathf.Max(0f, turnOffDelay);
        turnOnDelay = Mathf.Max(0f, turnOnDelay);
        sceneEnemyRefreshInterval = Mathf.Max(0.1f, sceneEnemyRefreshInterval);
    }
}
