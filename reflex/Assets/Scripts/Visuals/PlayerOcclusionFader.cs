using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

[DisallowMultipleComponent]
public class PlayerOcclusionFader : MonoBehaviour
{
    private const string SilhouetteResourceName = "Player Occlusion Silhouette";
    private const string SilhouetteShaderName = "Hidden/Reflex/PlayerOcclusionSilhouette";
    private const string SilhouetteObjectPrefix = "OcclusionSilhouette";
    private const int RaycastHitBufferSize = 64;
    private static readonly int ColorProperty = Shader.PropertyToID("_Color");

    [Header("Line Of Sight")]
    [SerializeField] private Camera occlusionCamera;
    [SerializeField, Min(0f)] private float lineOfSightPadding = 0.18f;
    [SerializeField, Range(0.1f, 1f)] private float playerWidthSampleScale = 0.75f;
    [SerializeField, Range(0.1f, 1f)] private float playerHeightSampleScale = 0.35f;
    [SerializeField, Min(0f)] private float playerBackPadding = 0.15f;
    [SerializeField, Min(0f)] private float targetContactGraceDistance = 0.2f;
    [SerializeField, Range(1, 5)] private int requiredBlockedSamples = 1;
    [SerializeField] private bool requireCenterSampleBlocked = true;
    [SerializeField, Min(0.01f)] private float scanInterval = 0.08f;
    [SerializeField, Min(0.1f)] private float occluderRefreshInterval = 0.5f;

    [Header("Targets")]
    [SerializeField] private bool includePlayer = true;
    [SerializeField] private bool includeEnemies = true;
    [SerializeField] private string enemyTag = "Enemy";
    [SerializeField, Min(0.05f)] private float targetRefreshInterval = 0.5f;
    [SerializeField, Min(0.1f)] private float silhouettePartRefreshInterval = 1.25f;
    [SerializeField] private string[] ignoredVisualNameMarkers =
    {
        "Hitbox",
        "Hit Box",
        "Hurtbox",
        "Hurt Box",
        "Laser"
    };

    [Header("Silhouette")]
    [SerializeField] private Material silhouetteMaterial;
    [SerializeField] private Color silhouetteFillColor = new Color(0.02f, 0.025f, 0.035f, 0.62f);
    [SerializeField] private Color silhouetteOutlineColor = new Color(1f, 0.74f, 0.22f, 0.95f);
    [SerializeField, Min(1f)] private float outlineScale = 1.12f;
    [SerializeField, Min(0.1f)] private float silhouetteFadeSpeed = 7f;
    [SerializeField] private int silhouetteSortingOrderOffset = 250;

    [Header("Wall Filtering")]
    [SerializeField] private LayerMask occluderLayers = ~0;
    [SerializeField] private bool useDefaultLayerExclusions = true;
    [SerializeField] private string[] wallNameMarkers =
    {
        "Wall",
        "Corner",
        "Door"
    };
    [SerializeField] private string[] wallSegmentNames =
    {
        "Left",
        "Middle",
        "Right",
        "Single"
    };

    private readonly List<OcclusionTarget> silhouetteTargets = new List<OcclusionTarget>();
    private readonly Dictionary<Transform, OcclusionTarget> targetsByRoot = new Dictionary<Transform, OcclusionTarget>();
    private readonly HashSet<Transform> rootsSeenThisRefresh = new HashSet<Transform>();
    private readonly List<OccluderInfo> boundsOnlyOccluders = new List<OccluderInfo>();
    private readonly List<OccluderInfo> visibleBoundsOnlyOccluders = new List<OccluderInfo>();
    private readonly List<Collider> occluderColliderList = new List<Collider>();
    private readonly HashSet<Collider> occluderColliderLookup = new HashSet<Collider>();
    private readonly List<Collider> colliderScratch = new List<Collider>();
    private readonly Plane[] cameraFrustumPlanes = new Plane[6];
    private readonly Vector3[] targetPoints = new Vector3[5];
    private readonly RaycastHit[] raycastHits = new RaycastHit[RaycastHitBufferSize];

    private Material runtimeSilhouetteMaterial;
    private MaterialPropertyBlock fillPropertyBlock;
    private MaterialPropertyBlock outlinePropertyBlock;
    private bool ownsRuntimeSilhouetteMaterial;
    private float nextScanTime;
    private float nextTargetRefreshTime;
    private float nextOccluderRefreshTime;
    private bool occluderCacheValid;
    private int effectiveOccluderLayerMask;
    private Color lastSilhouetteFillColor;
    private Color lastSilhouetteOutlineColor;
    private float lastOutlineScale;
    private int lastSilhouetteSortingOrderOffset;
    private bool hasLastVisualSettings;
    private int playerLayer = -1;
    private int enemyLayer = -1;
    private int terrainLayer = -1;
    private int uiLayer = -1;
    private int ignoreRaycastLayer = -1;
    private int dashingPlayerLayer = -1;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void BootstrapOnSceneLoad()
    {
        SceneManager.sceneLoaded -= HandleSceneLoaded;
        SceneManager.sceneLoaded += HandleSceneLoaded;
        EnsureFaderOnPlayer();
    }

    private static void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        EnsureFaderOnPlayer();
    }

    private static void EnsureFaderOnPlayer()
    {
        if (FindFirstObjectByType<PlayerOcclusionFader>() != null)
        {
            return;
        }

        GameObject playerObject;
        try
        {
            playerObject = GameObject.FindGameObjectWithTag("Player");
        }
        catch (UnityException)
        {
            return;
        }

        if (playerObject != null && playerObject.GetComponent<PlayerOcclusionFader>() == null)
        {
            playerObject.AddComponent<PlayerOcclusionFader>();
        }
    }

    private void Awake()
    {
        fillPropertyBlock = new MaterialPropertyBlock();
        outlinePropertyBlock = new MaterialPropertyBlock();

        playerLayer = LayerMask.NameToLayer("Player");
        enemyLayer = LayerMask.NameToLayer("Enemy");
        terrainLayer = LayerMask.NameToLayer("Terrain");
        uiLayer = LayerMask.NameToLayer("UI");
        ignoreRaycastLayer = LayerMask.NameToLayer("Ignore Raycast");
        dashingPlayerLayer = LayerMask.NameToLayer("DashingPlayer");

        ResolveSilhouetteMaterial();
    }

    private void Start()
    {
        RefreshTargets();
    }

    private void OnDisable()
    {
        for (int i = 0; i < silhouetteTargets.Count; i++)
        {
            silhouetteTargets[i].HideImmediately();
        }

        UpdateSilhouetteVisuals();
    }

    private void OnDestroy()
    {
        for (int i = 0; i < silhouetteTargets.Count; i++)
        {
            silhouetteTargets[i].Destroy();
        }

        silhouetteTargets.Clear();
        targetsByRoot.Clear();

        if (ownsRuntimeSilhouetteMaterial && runtimeSilhouetteMaterial != null)
        {
            if (Application.isPlaying)
            {
                Destroy(runtimeSilhouetteMaterial);
            }
            else
            {
                DestroyImmediate(runtimeSilhouetteMaterial);
            }
        }
    }

    private void Update()
    {
        if (Time.time >= nextScanTime)
        {
            nextScanTime = Time.time + scanInterval;
            RefreshOcclusionStates();
        }

        for (int i = 0; i < silhouetteTargets.Count; i++)
        {
            silhouetteTargets[i].Step(Time.deltaTime, silhouetteFadeSpeed);
        }
    }

    private void LateUpdate()
    {
        if (Time.time >= nextTargetRefreshTime)
        {
            nextTargetRefreshTime = Time.time + targetRefreshInterval;
            RefreshTargets();
        }

        UpdateSilhouetteVisuals();
    }

    private void RefreshOcclusionStates()
    {
        Camera activeCamera = ResolveCamera();
        if (activeCamera == null)
        {
            SetAllTargetsOccluded(false);
            return;
        }

        GeometryUtility.CalculateFrustumPlanes(activeCamera, cameraFrustumPlanes);
        RefreshOccludersIfNeeded();
        RefreshVisibleBoundsOnlyOccluders();

        for (int i = 0; i < silhouetteTargets.Count; i++)
        {
            OcclusionTarget target = silhouetteTargets[i];
            target.IsOccluded = IsTargetBlockedByWall(target, activeCamera);
        }
    }

    private void SetAllTargetsOccluded(bool isOccluded)
    {
        for (int i = 0; i < silhouetteTargets.Count; i++)
        {
            silhouetteTargets[i].IsOccluded = isOccluded;
        }
    }

    private Camera ResolveCamera()
    {
        if (occlusionCamera != null && occlusionCamera.isActiveAndEnabled)
        {
            return occlusionCamera;
        }

        occlusionCamera = Camera.main;
        return occlusionCamera;
    }

    private void RefreshTargets()
    {
        if (runtimeSilhouetteMaterial == null)
        {
            ResolveSilhouetteMaterial();
            if (runtimeSilhouetteMaterial == null)
            {
                return;
            }
        }

        rootsSeenThisRefresh.Clear();

        if (includePlayer)
        {
            AddTargetRoot(transform);
        }

        if (includeEnemies)
        {
            AddEnemyTargets();
        }

        for (int i = silhouetteTargets.Count - 1; i >= 0; i--)
        {
            OcclusionTarget target = silhouetteTargets[i];
            Transform root = target.Root;
            if (root != null && rootsSeenThisRefresh.Contains(root))
            {
                target.RefreshParts(this, runtimeSilhouetteMaterial, Time.time, silhouettePartRefreshInterval);
                continue;
            }

            target.Destroy();
            silhouetteTargets.RemoveAt(i);

            if (root != null)
            {
                targetsByRoot.Remove(root);
            }
        }
    }

    private void AddEnemyTargets()
    {
        EnemyController[] enemyControllers = FindObjectsByType<EnemyController>(FindObjectsSortMode.None);
        for (int i = 0; i < enemyControllers.Length; i++)
        {
            AddTargetRoot(enemyControllers[i].transform);
        }

        if (string.IsNullOrWhiteSpace(enemyTag))
        {
            return;
        }

        try
        {
            GameObject[] taggedEnemies = GameObject.FindGameObjectsWithTag(enemyTag);
            for (int i = 0; i < taggedEnemies.Length; i++)
            {
                AddTargetRoot(ResolveEnemyRoot(taggedEnemies[i].transform));
            }
        }
        catch (UnityException exception)
        {
            Debug.LogWarning($"{nameof(PlayerOcclusionFader)} could not scan enemy tag '{enemyTag}': {exception.Message}");
            enemyTag = string.Empty;
        }
    }

    private Transform ResolveEnemyRoot(Transform candidate)
    {
        EnemyController enemyController = candidate.GetComponentInParent<EnemyController>();
        return enemyController != null ? enemyController.transform : candidate;
    }

    private void AddTargetRoot(Transform root)
    {
        if (root == null || !root.gameObject.activeInHierarchy || IsSilhouetteProxy(root))
        {
            return;
        }

        rootsSeenThisRefresh.Add(root);

        if (targetsByRoot.ContainsKey(root))
        {
            return;
        }

        OcclusionTarget target = new OcclusionTarget(root);
        targetsByRoot.Add(root, target);
        silhouetteTargets.Add(target);
    }

    private bool IsEligibleWallOccluder(Renderer renderer)
    {
        if (renderer == null || !renderer.enabled || !renderer.gameObject.activeInHierarchy)
        {
            return false;
        }

        if (IsSilhouetteProxy(renderer.transform) ||
            renderer is SpriteRenderer ||
            (!(renderer is MeshRenderer) && !(renderer is SkinnedMeshRenderer)))
        {
            return false;
        }

        int layerMask = 1 << renderer.gameObject.layer;
        if ((occluderLayers.value & layerMask) == 0 || IsDefaultExcludedLayer(renderer.gameObject.layer))
        {
            return false;
        }

        if (HasTargetParent(renderer.transform))
        {
            return false;
        }

        return HasWallName(renderer.transform);
    }

    private void RefreshOccludersIfNeeded()
    {
        if (occluderCacheValid && Time.time < nextOccluderRefreshTime)
        {
            return;
        }

        nextOccluderRefreshTime = Time.time + Mathf.Max(0.1f, occluderRefreshInterval);
        RefreshOccluderCache();
    }

    private void RefreshOccluderCache()
    {
        boundsOnlyOccluders.Clear();
        visibleBoundsOnlyOccluders.Clear();
        occluderColliderList.Clear();
        occluderColliderLookup.Clear();
        effectiveOccluderLayerMask = BuildEffectiveOccluderLayerMask();

        Renderer[] renderers = FindObjectsByType<Renderer>(FindObjectsSortMode.None);
        for (int i = 0; i < renderers.Length; i++)
        {
            Renderer renderer = renderers[i];
            if (!IsEligibleWallOccluder(renderer))
            {
                continue;
            }

            bool hasBlockingCollider = TryCacheBlockingColliders(renderer);
            if (!hasBlockingCollider)
            {
                boundsOnlyOccluders.Add(new OccluderInfo(renderer));
            }
        }

        colliderScratch.Clear();
        occluderCacheValid = true;
    }

    private bool TryCacheBlockingColliders(Renderer renderer)
    {
        bool hasBlockingCollider = false;
        colliderScratch.Clear();
        renderer.GetComponents<Collider>(colliderScratch);

        for (int i = 0; i < colliderScratch.Count; i++)
        {
            Collider occluderCollider = colliderScratch[i];
            if (occluderCollider == null ||
                !occluderCollider.enabled ||
                !occluderCollider.gameObject.activeInHierarchy)
            {
                continue;
            }

            occluderColliderList.Add(occluderCollider);
            occluderColliderLookup.Add(occluderCollider);
            hasBlockingCollider = true;
        }

        return hasBlockingCollider;
    }

    private void RefreshVisibleBoundsOnlyOccluders()
    {
        visibleBoundsOnlyOccluders.Clear();

        for (int i = boundsOnlyOccluders.Count - 1; i >= 0; i--)
        {
            OccluderInfo occluder = boundsOnlyOccluders[i];
            if (!occluder.IsUsable)
            {
                boundsOnlyOccluders.RemoveAt(i);
                occluderCacheValid = false;
                continue;
            }

            occluder.RefreshPaddedBounds(lineOfSightPadding);
            if (GeometryUtility.TestPlanesAABB(cameraFrustumPlanes, occluder.PaddedBounds))
            {
                visibleBoundsOnlyOccluders.Add(occluder);
            }
        }
    }

    private int BuildEffectiveOccluderLayerMask()
    {
        int layerMask = occluderLayers.value;
        if (!useDefaultLayerExclusions)
        {
            return layerMask;
        }

        RemoveLayerFromMask(ref layerMask, playerLayer);
        RemoveLayerFromMask(ref layerMask, enemyLayer);
        RemoveLayerFromMask(ref layerMask, terrainLayer);
        RemoveLayerFromMask(ref layerMask, uiLayer);
        RemoveLayerFromMask(ref layerMask, ignoreRaycastLayer);
        RemoveLayerFromMask(ref layerMask, dashingPlayerLayer);
        return layerMask;
    }

    private static void RemoveLayerFromMask(ref int layerMask, int layer)
    {
        if (layer >= 0)
        {
            layerMask &= ~(1 << layer);
        }
    }

    private bool IsTargetBlockedByWall(OcclusionTarget target, Camera activeCamera)
    {
        if (!target.HasRoot ||
            !target.TryGetBounds(out Bounds targetBounds) ||
            !GeometryUtility.TestPlanesAABB(cameraFrustumPlanes, targetBounds))
        {
            return false;
        }

        BuildTargetPoints(targetBounds, activeCamera);
        Vector3 cameraPosition = activeCamera.transform.position;
        Transform targetRoot = target.Root;
        int blockedSampleCount = 0;
        int requiredSamples = Mathf.Clamp(requiredBlockedSamples, 1, targetPoints.Length);
        bool centerSampleBlocked = false;

        for (int i = 0; i < targetPoints.Length; i++)
        {
            if (!TryBuildLineOfSightRay(cameraPosition, targetPoints[i], targetBounds, out Ray cameraRay, out float maxDistance))
            {
                if (i == 0 && requireCenterSampleBlocked)
                {
                    return false;
                }

                continue;
            }

            bool sampleBlocked =
                ColliderOccluderBlocksRay(targetRoot, cameraRay, maxDistance) ||
                BoundsOnlyOccluderBlocksRay(targetRoot, cameraRay, maxDistance);

            if (i == 0)
            {
                centerSampleBlocked = sampleBlocked;
                if (requireCenterSampleBlocked && !centerSampleBlocked)
                {
                    return false;
                }
            }

            if (!sampleBlocked)
            {
                int remainingSamples = targetPoints.Length - i - 1;
                if (blockedSampleCount + remainingSamples < requiredSamples)
                {
                    return false;
                }

                continue;
            }

            blockedSampleCount++;
            if ((!requireCenterSampleBlocked || centerSampleBlocked) && blockedSampleCount >= requiredSamples)
            {
                return true;
            }
        }

        return false;
    }

    private void BuildTargetPoints(Bounds targetBounds, Camera activeCamera)
    {
        Vector3 cameraRight = activeCamera.transform.right;
        Vector3 boundsExtents = targetBounds.extents;
        float projectedHalfWidth =
            Mathf.Abs(cameraRight.x) * boundsExtents.x +
            Mathf.Abs(cameraRight.y) * boundsExtents.y +
            Mathf.Abs(cameraRight.z) * boundsExtents.z;

        float halfWidth = Mathf.Max(0.1f, projectedHalfWidth * playerWidthSampleScale);
        float halfHeight = Mathf.Max(0.1f, boundsExtents.y * playerHeightSampleScale);
        Vector3 center = targetBounds.center;
        Vector3 horizontalOffset = cameraRight * halfWidth;
        Vector3 verticalOffset = Vector3.up * halfHeight;

        targetPoints[0] = center;
        targetPoints[1] = center + horizontalOffset;
        targetPoints[2] = center - horizontalOffset;
        targetPoints[3] = center + verticalOffset;
        targetPoints[4] = center - verticalOffset;
    }

    private bool TryBuildLineOfSightRay(
        Vector3 cameraPosition,
        Vector3 targetPoint,
        Bounds targetBounds,
        out Ray cameraRay,
        out float maxDistance)
    {
        Vector3 toTarget = targetPoint - cameraPosition;
        float targetDistance = toTarget.magnitude;

        if (targetDistance <= Mathf.Epsilon)
        {
            maxDistance = 0f;
            cameraRay = default;
            return false;
        }

        cameraRay = new Ray(cameraPosition, toTarget / targetDistance);
        maxDistance = targetDistance - playerBackPadding;

        if (targetBounds.IntersectRay(cameraRay, out float targetEntryDistance))
        {
            maxDistance = Mathf.Min(maxDistance, targetEntryDistance - targetContactGraceDistance);
        }

        return maxDistance > 0f;
    }

    private bool ColliderOccluderBlocksRay(Transform targetRoot, Ray ray, float maxDistance)
    {
        if (occluderColliderList.Count == 0 || effectiveOccluderLayerMask == 0)
        {
            return false;
        }

        int hitCount = Physics.RaycastNonAlloc(
            ray,
            raycastHits,
            maxDistance,
            effectiveOccluderLayerMask,
            QueryTriggerInteraction.Collide);

        for (int i = 0; i < hitCount; i++)
        {
            Collider hitCollider = raycastHits[i].collider;
            if (IsCachedOccluderCollider(hitCollider, targetRoot))
            {
                return true;
            }
        }

        return hitCount >= raycastHits.Length && CachedCollidersBlockRay(targetRoot, ray, maxDistance);
    }

    private bool CachedCollidersBlockRay(Transform targetRoot, Ray ray, float maxDistance)
    {
        for (int i = 0; i < occluderColliderList.Count; i++)
        {
            Collider occluderCollider = occluderColliderList[i];
            if (!IsCachedOccluderCollider(occluderCollider, targetRoot))
            {
                continue;
            }

            if (occluderCollider.Raycast(ray, out RaycastHit hit, maxDistance))
            {
                return true;
            }
        }

        return false;
    }

    private bool IsCachedOccluderCollider(Collider occluderCollider, Transform targetRoot)
    {
        return occluderCollider != null &&
               occluderCollider.enabled &&
               occluderCollider.gameObject.activeInHierarchy &&
               occluderColliderLookup.Contains(occluderCollider) &&
               (targetRoot == null || !occluderCollider.transform.IsChildOf(targetRoot));
    }

    private bool BoundsOnlyOccluderBlocksRay(Transform targetRoot, Ray ray, float maxDistance)
    {
        for (int i = 0; i < visibleBoundsOnlyOccluders.Count; i++)
        {
            OccluderInfo occluder = visibleBoundsOnlyOccluders[i];
            if (targetRoot != null && occluder.Transform.IsChildOf(targetRoot))
            {
                continue;
            }

            if (occluder.PaddedBounds.IntersectRay(ray, out float hitDistance) && hitDistance < maxDistance)
            {
                return true;
            }
        }

        return false;
    }

    private bool IsDefaultExcludedLayer(int layer)
    {
        if (!useDefaultLayerExclusions)
        {
            return false;
        }

        return layer == playerLayer ||
               layer == enemyLayer ||
               layer == terrainLayer ||
               layer == uiLayer ||
               layer == ignoreRaycastLayer ||
               layer == dashingPlayerLayer;
    }

    private bool HasTargetParent(Transform candidate)
    {
        for (Transform current = candidate; current != null; current = current.parent)
        {
            if (current == transform || current.CompareTag("Player") || current.CompareTag("Enemy"))
            {
                return true;
            }

            int layer = current.gameObject.layer;
            if (layer == playerLayer || layer == enemyLayer || layer == dashingPlayerLayer)
            {
                return true;
            }
        }

        return false;
    }

    private bool HasWallName(Transform candidate)
    {
        for (Transform current = candidate; current != null; current = current.parent)
        {
            if (NameMatchesAnyMarker(current.name, wallNameMarkers) ||
                NameEqualsAnyMarker(current.name, wallSegmentNames))
            {
                return true;
            }
        }

        return false;
    }

    private bool IsEligibleSilhouetteSource(Renderer source, Transform targetRoot)
    {
        if (source == null ||
            targetRoot == null ||
            !source.transform.IsChildOf(targetRoot) ||
            IsSilhouetteProxy(source.transform) ||
            NameMatchesAnyMarker(source.transform, ignoredVisualNameMarkers))
        {
            return false;
        }

        if (source is SpriteRenderer)
        {
            return true;
        }

        if (source is MeshRenderer)
        {
            return source.TryGetComponent(out MeshFilter meshFilter) && meshFilter.sharedMesh != null;
        }

        return source is SkinnedMeshRenderer skinnedMeshRenderer && skinnedMeshRenderer.sharedMesh != null;
    }

    private bool IsSilhouetteProxy(Transform candidate)
    {
        for (Transform current = candidate; current != null; current = current.parent)
        {
            if (current.name.StartsWith(SilhouetteObjectPrefix, System.StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }

    private bool NameMatchesAnyMarker(Transform candidate, string[] markers)
    {
        for (Transform current = candidate; current != null; current = current.parent)
        {
            if (NameMatchesAnyMarker(current.name, markers))
            {
                return true;
            }
        }

        return false;
    }

    private static bool NameMatchesAnyMarker(string candidateName, string[] markers)
    {
        if (markers == null)
        {
            return false;
        }

        for (int i = 0; i < markers.Length; i++)
        {
            string marker = markers[i];
            if (!string.IsNullOrWhiteSpace(marker) &&
                candidateName.IndexOf(marker, System.StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return true;
            }
        }

        return false;
    }

    private static bool NameEqualsAnyMarker(string candidateName, string[] markers)
    {
        if (markers == null)
        {
            return false;
        }

        string normalizedName = NormalizeObjectName(candidateName);
        for (int i = 0; i < markers.Length; i++)
        {
            string marker = markers[i];
            if (!string.IsNullOrWhiteSpace(marker) &&
                string.Equals(normalizedName, marker.Trim(), System.StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }

    private static string NormalizeObjectName(string objectName)
    {
        const string cloneSuffix = "(Clone)";
        string normalizedName = objectName.Trim();
        if (normalizedName.EndsWith(cloneSuffix, System.StringComparison.OrdinalIgnoreCase))
        {
            normalizedName = normalizedName.Substring(0, normalizedName.Length - cloneSuffix.Length).Trim();
        }

        return normalizedName;
    }

    private void ResolveSilhouetteMaterial()
    {
        runtimeSilhouetteMaterial = silhouetteMaterial != null
            ? silhouetteMaterial
            : Resources.Load<Material>(SilhouetteResourceName);

        if (runtimeSilhouetteMaterial != null)
        {
            return;
        }

        Shader silhouetteShader = Shader.Find(SilhouetteShaderName);
        if (silhouetteShader == null)
        {
            silhouetteShader = Shader.Find("Sprites/Default");
        }

        if (silhouetteShader == null)
        {
            Debug.LogWarning($"{nameof(PlayerOcclusionFader)} could not find a silhouette shader.");
            return;
        }

        runtimeSilhouetteMaterial = new Material(silhouetteShader)
        {
            name = "Runtime Player Occlusion Silhouette",
            hideFlags = HideFlags.DontSave
        };
        ownsRuntimeSilhouetteMaterial = true;
    }

    private void UpdateSilhouetteVisuals()
    {
        bool forceVisualSettingsSync = HaveSilhouetteVisualSettingsChanged();

        for (int i = 0; i < silhouetteTargets.Count; i++)
        {
            OcclusionTarget target = silhouetteTargets[i];
            if (!target.NeedsVisualSync && (!forceVisualSettingsSync || target.Amount <= 0.001f))
            {
                continue;
            }

            bool wasVisualDirty = target.ConsumeVisualDirty();
            bool updateMaterialProperties = forceVisualSettingsSync || wasVisualDirty;
            if (updateMaterialProperties)
            {
                Color fillColor = silhouetteFillColor;
                Color outlineColor = silhouetteOutlineColor;
                fillColor.a *= target.Amount;
                outlineColor.a *= target.Amount;
                fillPropertyBlock.SetColor(ColorProperty, fillColor);
                outlinePropertyBlock.SetColor(ColorProperty, outlineColor);
            }

            target.SyncVisuals(
                outlineScale,
                silhouetteSortingOrderOffset,
                fillPropertyBlock,
                outlinePropertyBlock,
                updateMaterialProperties);
        }

        RememberSilhouetteVisualSettings();
    }

    private bool HaveSilhouetteVisualSettingsChanged()
    {
        if (!hasLastVisualSettings)
        {
            return true;
        }

        return silhouetteFillColor != lastSilhouetteFillColor ||
               silhouetteOutlineColor != lastSilhouetteOutlineColor ||
               !Mathf.Approximately(outlineScale, lastOutlineScale) ||
               silhouetteSortingOrderOffset != lastSilhouetteSortingOrderOffset;
    }

    private void RememberSilhouetteVisualSettings()
    {
        lastSilhouetteFillColor = silhouetteFillColor;
        lastSilhouetteOutlineColor = silhouetteOutlineColor;
        lastOutlineScale = outlineScale;
        lastSilhouetteSortingOrderOffset = silhouetteSortingOrderOffset;
        hasLastVisualSettings = true;
    }

    private void OnDrawGizmosSelected()
    {
        Camera activeCamera = occlusionCamera != null ? occlusionCamera : Camera.main;
        if (activeCamera == null)
        {
            return;
        }

        if (!targetsByRoot.TryGetValue(transform, out OcclusionTarget target) ||
            !target.TryGetBounds(out Bounds targetBounds))
        {
            return;
        }

        BuildTargetPoints(targetBounds, activeCamera);
        Gizmos.color = new Color(0.25f, 0.8f, 1f, 0.35f);

        for (int i = 0; i < targetPoints.Length; i++)
        {
            Gizmos.DrawLine(activeCamera.transform.position, targetPoints[i]);
        }
    }

    private sealed class OccluderInfo
    {
        private readonly Renderer renderer;
        private readonly Transform transform;

        public Transform Transform => transform;
        public Bounds PaddedBounds { get; private set; }

        public bool IsUsable =>
            renderer != null &&
            renderer.enabled &&
            renderer.gameObject.activeInHierarchy;

        public OccluderInfo(Renderer renderer)
        {
            this.renderer = renderer;
            transform = renderer.transform;
        }

        public void RefreshPaddedBounds(float padding)
        {
            Bounds paddedBounds = renderer.bounds;
            paddedBounds.Expand(padding * 2f);
            PaddedBounds = paddedBounds;
        }
    }

    private sealed class OcclusionTarget
    {
        private readonly Transform root;
        private readonly CharacterController characterController;
        private readonly List<SilhouettePart> parts = new List<SilhouettePart>();
        private readonly List<Renderer> rendererScratch = new List<Renderer>();
        private readonly HashSet<Renderer> knownSources = new HashSet<Renderer>();
        private float nextPartRefreshTime;
        private bool visualDirty = true;

        public Transform Root => root;
        public bool HasRoot => root != null && root.gameObject.activeInHierarchy;
        public bool IsOccluded { get; set; }
        public float Amount { get; private set; }
        public bool NeedsVisualSync => visualDirty || Amount > 0.001f;

        public OcclusionTarget(Transform root)
        {
            this.root = root;
            characterController = root.GetComponent<CharacterController>();
        }

        public void RefreshParts(
            PlayerOcclusionFader owner,
            Material material,
            float currentTime,
            float partRefreshInterval)
        {
            RemoveMissingParts();

            if (!HasRoot || material == null)
            {
                return;
            }

            if (parts.Count > 0 && currentTime < nextPartRefreshTime)
            {
                return;
            }

            nextPartRefreshTime = currentTime + Mathf.Max(0.1f, partRefreshInterval);
            rendererScratch.Clear();
            root.GetComponentsInChildren<Renderer>(true, rendererScratch);

            for (int i = 0; i < rendererScratch.Count; i++)
            {
                Renderer source = rendererScratch[i];
                if (!owner.IsEligibleSilhouetteSource(source, root) || knownSources.Contains(source))
                {
                    continue;
                }

                SilhouettePart part = SilhouettePart.Create(source, material);
                if (part == null)
                {
                    continue;
                }

                parts.Add(part);
                knownSources.Add(source);
                visualDirty = true;
            }

            rendererScratch.Clear();
        }

        public void Step(float deltaTime, float fadeSpeed)
        {
            if (!IsOccluded && Amount <= 0f)
            {
                return;
            }

            float targetAmount = IsOccluded ? 1f : 0f;
            float previousAmount = Amount;
            Amount = Mathf.MoveTowards(Amount, targetAmount, fadeSpeed * deltaTime);

            if (!Mathf.Approximately(previousAmount, Amount))
            {
                visualDirty = true;
            }
        }

        public void HideImmediately()
        {
            IsOccluded = false;
            Amount = 0f;
            visualDirty = true;
        }

        public bool ConsumeVisualDirty()
        {
            bool wasDirty = visualDirty;
            visualDirty = false;
            return wasDirty;
        }

        public bool TryGetBounds(out Bounds bounds)
        {
            bool hasBounds = false;
            bounds = default;

            for (int i = 0; i < parts.Count; i++)
            {
                if (!parts[i].TryGetSourceBounds(out Bounds sourceBounds))
                {
                    continue;
                }

                if (!hasBounds)
                {
                    bounds = sourceBounds;
                    hasBounds = true;
                }
                else
                {
                    bounds.Encapsulate(sourceBounds);
                }
            }

            if (!hasBounds && characterController != null && characterController.enabled)
            {
                bounds = characterController.bounds;
                hasBounds = true;
            }

            return hasBounds;
        }

        public void SyncVisuals(
            float outlineScale,
            int sortingOrderOffset,
            MaterialPropertyBlock fillPropertyBlock,
            MaterialPropertyBlock outlinePropertyBlock,
            bool updateMaterialProperties)
        {
            for (int i = 0; i < parts.Count; i++)
            {
                parts[i].Sync(
                    Amount,
                    outlineScale,
                    sortingOrderOffset,
                    fillPropertyBlock,
                    outlinePropertyBlock,
                    updateMaterialProperties);
            }
        }

        public void Destroy()
        {
            for (int i = 0; i < parts.Count; i++)
            {
                parts[i].Destroy();
            }

            parts.Clear();
            rendererScratch.Clear();
            knownSources.Clear();
        }

        private void RemoveMissingParts()
        {
            for (int i = parts.Count - 1; i >= 0; i--)
            {
                SilhouettePart part = parts[i];
                if (part.HasSource)
                {
                    continue;
                }

                knownSources.Remove(part.Source);
                part.Destroy();
                parts.RemoveAt(i);
                visualDirty = true;
            }
        }
    }

    private sealed class SilhouettePart
    {
        private readonly Renderer source;
        private readonly Material material;
        private readonly GameObject root;
        private readonly Renderer outlineRenderer;
        private readonly Renderer fillRenderer;
        private readonly SpriteRenderer sourceSpriteRenderer;
        private readonly SpriteRenderer outlineSpriteRenderer;
        private readonly SpriteRenderer fillSpriteRenderer;
        private readonly MeshFilter sourceMeshFilter;
        private readonly MeshFilter outlineMeshFilter;
        private readonly MeshFilter fillMeshFilter;
        private readonly SkinnedMeshRenderer sourceSkinnedMeshRenderer;
        private readonly SkinnedMeshRenderer outlineSkinnedMeshRenderer;
        private readonly SkinnedMeshRenderer fillSkinnedMeshRenderer;

        public Renderer Source => source;
        public bool HasSource => source != null;

        private SilhouettePart(
            Renderer source,
            Material material,
            GameObject root,
            Renderer outlineRenderer,
            Renderer fillRenderer,
            SpriteRenderer sourceSpriteRenderer = null,
            SpriteRenderer outlineSpriteRenderer = null,
            SpriteRenderer fillSpriteRenderer = null,
            MeshFilter sourceMeshFilter = null,
            MeshFilter outlineMeshFilter = null,
            MeshFilter fillMeshFilter = null,
            SkinnedMeshRenderer sourceSkinnedMeshRenderer = null,
            SkinnedMeshRenderer outlineSkinnedMeshRenderer = null,
            SkinnedMeshRenderer fillSkinnedMeshRenderer = null)
        {
            this.source = source;
            this.material = material;
            this.root = root;
            this.outlineRenderer = outlineRenderer;
            this.fillRenderer = fillRenderer;
            this.sourceSpriteRenderer = sourceSpriteRenderer;
            this.outlineSpriteRenderer = outlineSpriteRenderer;
            this.fillSpriteRenderer = fillSpriteRenderer;
            this.sourceMeshFilter = sourceMeshFilter;
            this.outlineMeshFilter = outlineMeshFilter;
            this.fillMeshFilter = fillMeshFilter;
            this.sourceSkinnedMeshRenderer = sourceSkinnedMeshRenderer;
            this.outlineSkinnedMeshRenderer = outlineSkinnedMeshRenderer;
            this.fillSkinnedMeshRenderer = fillSkinnedMeshRenderer;
        }

        public static SilhouettePart Create(Renderer source, Material material)
        {
            if (source is SpriteRenderer spriteRenderer)
            {
                return CreateSpritePart(spriteRenderer, material);
            }

            if (source is SkinnedMeshRenderer skinnedMeshRenderer)
            {
                return CreateSkinnedMeshPart(skinnedMeshRenderer, material);
            }

            if (source is MeshRenderer meshRenderer &&
                source.TryGetComponent(out MeshFilter meshFilter) &&
                meshFilter.sharedMesh != null)
            {
                return CreateMeshPart(meshRenderer, meshFilter, material);
            }

            return null;
        }

        public bool TryGetSourceBounds(out Bounds bounds)
        {
            bounds = default;
            if (!IsSourceVisible())
            {
                return false;
            }

            bounds = source.bounds;
            return true;
        }

        public void Sync(
            float amount,
            float outlineScale,
            int sortingOrderOffset,
            MaterialPropertyBlock fillPropertyBlock,
            MaterialPropertyBlock outlinePropertyBlock,
            bool updateMaterialProperties)
        {
            bool visible = amount > 0.001f && IsSourceVisible();
            if (root.activeSelf != visible)
            {
                root.SetActive(visible);
            }

            if (!visible)
            {
                return;
            }

            if (sourceSpriteRenderer != null)
            {
                CopySpriteState(
                    sourceSpriteRenderer,
                    outlineSpriteRenderer,
                    sortingOrderOffset,
                    outlinePropertyBlock,
                    updateMaterialProperties);
                CopySpriteState(
                    sourceSpriteRenderer,
                    fillSpriteRenderer,
                    sortingOrderOffset + 1,
                    fillPropertyBlock,
                    updateMaterialProperties);
            }
            else if (sourceSkinnedMeshRenderer != null)
            {
                CopySkinnedMeshState(
                    sourceSkinnedMeshRenderer,
                    outlineSkinnedMeshRenderer,
                    sortingOrderOffset,
                    outlinePropertyBlock,
                    updateMaterialProperties);
                CopySkinnedMeshState(
                    sourceSkinnedMeshRenderer,
                    fillSkinnedMeshRenderer,
                    sortingOrderOffset + 1,
                    fillPropertyBlock,
                    updateMaterialProperties);
            }
            else
            {
                CopyMeshState(
                    source,
                    sourceMeshFilter,
                    outlineMeshFilter,
                    outlineRenderer,
                    sortingOrderOffset,
                    outlinePropertyBlock,
                    updateMaterialProperties);
                CopyMeshState(
                    source,
                    sourceMeshFilter,
                    fillMeshFilter,
                    fillRenderer,
                    sortingOrderOffset + 1,
                    fillPropertyBlock,
                    updateMaterialProperties);
            }

            outlineRenderer.transform.localScale = Vector3.one * outlineScale;
            fillRenderer.transform.localScale = Vector3.one;
        }

        public void Destroy()
        {
            if (root == null)
            {
                return;
            }

            if (Application.isPlaying)
            {
                Object.Destroy(root);
            }
            else
            {
                Object.DestroyImmediate(root);
            }
        }

        private static SilhouettePart CreateSpritePart(SpriteRenderer source, Material material)
        {
            GameObject root = CreateRoot(source.transform);
            SpriteRenderer outlineRenderer = CreateSpriteRenderer("OcclusionSilhouette Outline", root.transform, source, material);
            SpriteRenderer fillRenderer = CreateSpriteRenderer("OcclusionSilhouette Fill", root.transform, source, material);
            root.SetActive(false);

            return new SilhouettePart(
                source,
                material,
                root,
                outlineRenderer,
                fillRenderer,
                source,
                outlineRenderer,
                fillRenderer);
        }

        private static SilhouettePart CreateMeshPart(MeshRenderer source, MeshFilter sourceMeshFilter, Material material)
        {
            GameObject root = CreateRoot(source.transform);
            MeshRenderer outlineRenderer = CreateMeshRenderer(
                "OcclusionSilhouette Outline",
                root.transform,
                source,
                sourceMeshFilter.sharedMesh,
                material,
                out MeshFilter outlineMeshFilter);
            MeshRenderer fillRenderer = CreateMeshRenderer(
                "OcclusionSilhouette Fill",
                root.transform,
                source,
                sourceMeshFilter.sharedMesh,
                material,
                out MeshFilter fillMeshFilter);
            root.SetActive(false);

            return new SilhouettePart(
                source,
                material,
                root,
                outlineRenderer,
                fillRenderer,
                sourceMeshFilter: sourceMeshFilter,
                outlineMeshFilter: outlineMeshFilter,
                fillMeshFilter: fillMeshFilter);
        }

        private static SilhouettePart CreateSkinnedMeshPart(SkinnedMeshRenderer source, Material material)
        {
            GameObject root = CreateRoot(source.transform);
            SkinnedMeshRenderer outlineRenderer = CreateSkinnedMeshRenderer("OcclusionSilhouette Outline", root.transform, source, material);
            SkinnedMeshRenderer fillRenderer = CreateSkinnedMeshRenderer("OcclusionSilhouette Fill", root.transform, source, material);
            root.SetActive(false);

            return new SilhouettePart(
                source,
                material,
                root,
                outlineRenderer,
                fillRenderer,
                sourceSkinnedMeshRenderer: source,
                outlineSkinnedMeshRenderer: outlineRenderer,
                fillSkinnedMeshRenderer: fillRenderer);
        }

        private bool IsSourceVisible()
        {
            if (source == null || !source.enabled || !source.gameObject.activeInHierarchy)
            {
                return false;
            }

            if (sourceSpriteRenderer != null)
            {
                return sourceSpriteRenderer.sprite != null;
            }

            if (sourceSkinnedMeshRenderer != null)
            {
                return sourceSkinnedMeshRenderer.sharedMesh != null;
            }

            return sourceMeshFilter != null && sourceMeshFilter.sharedMesh != null;
        }

        private static GameObject CreateRoot(Transform source)
        {
            GameObject root = new GameObject($"{SilhouetteObjectPrefix} ({source.name})")
            {
                hideFlags = HideFlags.DontSave,
                layer = source.gameObject.layer
            };
            root.transform.SetParent(source, false);
            return root;
        }

        private static SpriteRenderer CreateSpriteRenderer(string name, Transform parent, SpriteRenderer source, Material material)
        {
            GameObject rendererObject = CreateRendererObject(name, parent, source.gameObject.layer);
            SpriteRenderer renderer = rendererObject.AddComponent<SpriteRenderer>();
            renderer.sharedMaterial = material;
            ConfigureRenderer(renderer);
            return renderer;
        }

        private static MeshRenderer CreateMeshRenderer(
            string name,
            Transform parent,
            Renderer source,
            Mesh mesh,
            Material material,
            out MeshFilter meshFilter)
        {
            GameObject rendererObject = CreateRendererObject(name, parent, source.gameObject.layer);
            meshFilter = rendererObject.AddComponent<MeshFilter>();
            meshFilter.sharedMesh = mesh;
            MeshRenderer renderer = rendererObject.AddComponent<MeshRenderer>();
            ConfigureRenderer(renderer);
            EnsureMaterialSlots(source, renderer, material, mesh != null ? mesh.subMeshCount : 1);
            return renderer;
        }

        private static SkinnedMeshRenderer CreateSkinnedMeshRenderer(
            string name,
            Transform parent,
            SkinnedMeshRenderer source,
            Material material)
        {
            GameObject rendererObject = CreateRendererObject(name, parent, source.gameObject.layer);
            SkinnedMeshRenderer renderer = rendererObject.AddComponent<SkinnedMeshRenderer>();
            ConfigureRenderer(renderer);
            CopySkinnedMeshData(source, renderer);
            EnsureMaterialSlots(source, renderer, material, source.sharedMesh != null ? source.sharedMesh.subMeshCount : 1);
            return renderer;
        }

        private static GameObject CreateRendererObject(string name, Transform parent, int layer)
        {
            GameObject rendererObject = new GameObject(name)
            {
                hideFlags = HideFlags.DontSave,
                layer = layer
            };
            rendererObject.transform.SetParent(parent, false);
            return rendererObject;
        }

        private static void ConfigureRenderer(Renderer renderer)
        {
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            renderer.receiveShadows = false;
        }

        private static void CopySpriteState(
            SpriteRenderer source,
            SpriteRenderer target,
            int sortingOrderOffset,
            MaterialPropertyBlock propertyBlock,
            bool updateMaterialProperties)
        {
            target.sprite = source.sprite;
            target.flipX = source.flipX;
            target.flipY = source.flipY;
            target.drawMode = source.drawMode;
            target.size = source.size;
            target.tileMode = source.tileMode;
            target.maskInteraction = SpriteMaskInteraction.None;
            target.sortingLayerID = source.sortingLayerID;
            target.sortingOrder = source.sortingOrder + sortingOrderOffset;
            target.color = new Color(1f, 1f, 1f, source.color.a);
            if (updateMaterialProperties)
            {
                target.SetPropertyBlock(propertyBlock);
            }
        }

        private void CopyMeshState(
            Renderer source,
            MeshFilter sourceFilter,
            MeshFilter targetFilter,
            Renderer target,
            int sortingOrderOffset,
            MaterialPropertyBlock propertyBlock,
            bool updateMaterialProperties)
        {
            Mesh sourceMesh = sourceFilter.sharedMesh;
            bool meshChanged = targetFilter.sharedMesh != sourceMesh;
            if (meshChanged)
            {
                targetFilter.sharedMesh = sourceMesh;
            }

            CopyRendererState(source, target, sortingOrderOffset, propertyBlock, sourceMesh, updateMaterialProperties, meshChanged);
        }

        private void CopySkinnedMeshState(
            SkinnedMeshRenderer source,
            SkinnedMeshRenderer target,
            int sortingOrderOffset,
            MaterialPropertyBlock propertyBlock,
            bool updateMaterialProperties)
        {
            Mesh sourceMesh = source.sharedMesh;
            bool meshOrRigChanged = target.sharedMesh != sourceMesh || target.rootBone != source.rootBone;
            if (meshOrRigChanged)
            {
                CopySkinnedMeshData(source, target);
            }
            else
            {
                target.localBounds = source.localBounds;
                target.updateWhenOffscreen = source.updateWhenOffscreen;
                target.quality = source.quality;
            }

            CopyRendererState(source, target, sortingOrderOffset, propertyBlock, sourceMesh, updateMaterialProperties, meshOrRigChanged);
        }

        private void CopyRendererState(
            Renderer source,
            Renderer target,
            int sortingOrderOffset,
            MaterialPropertyBlock propertyBlock,
            Mesh mesh,
            bool updateMaterialProperties,
            bool updateMaterialSlots)
        {
            if (target.sortingLayerID != source.sortingLayerID)
            {
                target.sortingLayerID = source.sortingLayerID;
            }

            int targetSortingOrder = source.sortingOrder + sortingOrderOffset;
            if (target.sortingOrder != targetSortingOrder)
            {
                target.sortingOrder = targetSortingOrder;
            }

            if (target.renderingLayerMask != source.renderingLayerMask)
            {
                target.renderingLayerMask = source.renderingLayerMask;
            }

            if (updateMaterialSlots)
            {
                EnsureMaterialSlots(source, target, material, mesh != null ? mesh.subMeshCount : 1);
            }

            if (updateMaterialProperties)
            {
                target.SetPropertyBlock(propertyBlock);
            }
        }

        private static void CopySkinnedMeshData(SkinnedMeshRenderer source, SkinnedMeshRenderer target)
        {
            target.sharedMesh = source.sharedMesh;
            target.rootBone = source.rootBone;
            target.bones = source.bones;
            target.localBounds = source.localBounds;
            target.updateWhenOffscreen = source.updateWhenOffscreen;
            target.quality = source.quality;
        }

        private static void EnsureMaterialSlots(Renderer source, Renderer target, Material material, int meshSubMeshCount)
        {
            int sourceMaterialCount = source.sharedMaterials != null ? source.sharedMaterials.Length : 0;
            int materialCount = Mathf.Max(1, sourceMaterialCount, meshSubMeshCount);
            Material[] currentMaterials = target.sharedMaterials;

            if (currentMaterials != null && currentMaterials.Length == materialCount)
            {
                bool hasExpectedMaterials = true;
                for (int i = 0; i < currentMaterials.Length; i++)
                {
                    if (currentMaterials[i] != material)
                    {
                        hasExpectedMaterials = false;
                        break;
                    }
                }

                if (hasExpectedMaterials)
                {
                    return;
                }
            }

            Material[] silhouetteMaterials = new Material[materialCount];
            for (int i = 0; i < silhouetteMaterials.Length; i++)
            {
                silhouetteMaterials[i] = material;
            }

            target.sharedMaterials = silhouetteMaterials;
        }
    }
}
