using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public class PlayerOcclusionFader : MonoBehaviour
{
    private const string SilhouetteResourceName = "Player Occlusion Silhouette";
    private const string SilhouetteShaderName = "Hidden/Reflex/PlayerOcclusionSilhouette";
    private const string SilhouetteObjectPrefix = "OcclusionSilhouette";
    private static readonly int ColorProperty = Shader.PropertyToID("_Color");

    [Header("Line Of Sight")]
    [SerializeField] private Camera occlusionCamera;
    [SerializeField, Min(0f)] private float lineOfSightPadding = 0.18f;
    [SerializeField, Range(0.1f, 1f)] private float playerWidthSampleScale = 0.75f;
    [SerializeField, Range(0.1f, 1f)] private float playerHeightSampleScale = 0.35f;
    [SerializeField, Min(0f)] private float playerBackPadding = 0.15f;
    [SerializeField, Min(0.01f)] private float scanInterval = 0.08f;

    [Header("Targets")]
    [SerializeField] private bool includePlayer = true;
    [SerializeField] private bool includeEnemies = true;
    [SerializeField] private string enemyTag = "Enemy";
    [SerializeField, Min(0.05f)] private float targetRefreshInterval = 0.5f;
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
    private readonly List<Renderer> wallRenderers = new List<Renderer>();
    private readonly List<Collider> occluderColliders = new List<Collider>();
    private readonly Plane[] cameraFrustumPlanes = new Plane[6];
    private readonly Vector3[] targetPoints = new Vector3[5];

    private Material runtimeSilhouetteMaterial;
    private MaterialPropertyBlock fillPropertyBlock;
    private MaterialPropertyBlock outlinePropertyBlock;
    private bool ownsRuntimeSilhouetteMaterial;
    private float nextScanTime;
    private float nextTargetRefreshTime;
    private int playerLayer = -1;
    private int enemyLayer = -1;
    private int terrainLayer = -1;
    private int uiLayer = -1;
    private int ignoreRaycastLayer = -1;
    private int dashingPlayerLayer = -1;

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
        RefreshWallRenderers();

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
                target.RefreshParts(this, runtimeSilhouetteMaterial);
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

    private void RefreshWallRenderers()
    {
        wallRenderers.Clear();
        Renderer[] renderers = FindObjectsByType<Renderer>(FindObjectsSortMode.None);

        for (int i = 0; i < renderers.Length; i++)
        {
            Renderer renderer = renderers[i];
            if (IsEligibleWallOccluder(renderer))
            {
                wallRenderers.Add(renderer);
            }
        }
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

    private bool IsTargetBlockedByWall(OcclusionTarget target, Camera activeCamera)
    {
        if (!target.HasRoot ||
            !target.TryGetBounds(out Bounds targetBounds) ||
            !GeometryUtility.TestPlanesAABB(cameraFrustumPlanes, targetBounds))
        {
            return false;
        }

        BuildTargetPoints(targetBounds, activeCamera);

        for (int i = 0; i < wallRenderers.Count; i++)
        {
            Renderer wallRenderer = wallRenderers[i];
            if (wallRenderer == null ||
                wallRenderer.transform.IsChildOf(target.Root) ||
                !GeometryUtility.TestPlanesAABB(cameraFrustumPlanes, wallRenderer.bounds) ||
                !RendererBlocksTargetLineOfSight(wallRenderer, activeCamera))
            {
                continue;
            }

            return true;
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

    private bool RendererBlocksTargetLineOfSight(Renderer renderer, Camera activeCamera)
    {
        Bounds paddedBounds = renderer.bounds;
        paddedBounds.Expand(lineOfSightPadding * 2f);
        bool hasPreciseCollider = TryCollectBlockingColliders(renderer);

        Vector3 cameraPosition = activeCamera.transform.position;

        for (int i = 0; i < targetPoints.Length; i++)
        {
            Vector3 toTarget = targetPoints[i] - cameraPosition;
            float targetDistance = toTarget.magnitude;

            if (targetDistance <= Mathf.Epsilon)
            {
                continue;
            }

            Ray cameraRay = new Ray(cameraPosition, toTarget / targetDistance);
            float maxDistance = targetDistance - playerBackPadding;
            if (maxDistance <= 0f ||
                !paddedBounds.IntersectRay(cameraRay, out float hitDistance) ||
                hitDistance >= maxDistance)
            {
                continue;
            }

            if (!hasPreciseCollider || CollidersBlockRay(cameraRay, maxDistance))
            {
                return true;
            }
        }

        return false;
    }

    private bool TryCollectBlockingColliders(Renderer renderer)
    {
        occluderColliders.Clear();
        renderer.GetComponents(occluderColliders);

        for (int i = occluderColliders.Count - 1; i >= 0; i--)
        {
            Collider occluderCollider = occluderColliders[i];
            if (occluderCollider == null ||
                !occluderCollider.enabled ||
                !occluderCollider.gameObject.activeInHierarchy)
            {
                occluderColliders.RemoveAt(i);
            }
        }

        return occluderColliders.Count > 0;
    }

    private bool CollidersBlockRay(Ray ray, float maxDistance)
    {
        for (int i = 0; i < occluderColliders.Count; i++)
        {
            if (occluderColliders[i].Raycast(ray, out RaycastHit hit, maxDistance))
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
        for (int i = 0; i < silhouetteTargets.Count; i++)
        {
            OcclusionTarget target = silhouetteTargets[i];
            Color fillColor = silhouetteFillColor;
            Color outlineColor = silhouetteOutlineColor;
            fillColor.a *= target.Amount;
            outlineColor.a *= target.Amount;
            fillPropertyBlock.SetColor(ColorProperty, fillColor);
            outlinePropertyBlock.SetColor(ColorProperty, outlineColor);

            target.SyncVisuals(
                outlineScale,
                silhouetteSortingOrderOffset,
                fillPropertyBlock,
                outlinePropertyBlock);
        }
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

    private sealed class OcclusionTarget
    {
        private readonly Transform root;
        private readonly CharacterController characterController;
        private readonly List<SilhouettePart> parts = new List<SilhouettePart>();
        private readonly HashSet<Renderer> knownSources = new HashSet<Renderer>();

        public Transform Root => root;
        public bool HasRoot => root != null && root.gameObject.activeInHierarchy;
        public bool IsOccluded { get; set; }
        public float Amount { get; private set; }

        public OcclusionTarget(Transform root)
        {
            this.root = root;
            characterController = root.GetComponent<CharacterController>();
        }

        public void RefreshParts(PlayerOcclusionFader owner, Material material)
        {
            RemoveMissingParts();

            if (!HasRoot || material == null)
            {
                return;
            }

            Renderer[] renderers = root.GetComponentsInChildren<Renderer>(true);
            for (int i = 0; i < renderers.Length; i++)
            {
                Renderer source = renderers[i];
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
            }
        }

        public void Step(float deltaTime, float fadeSpeed)
        {
            float targetAmount = IsOccluded ? 1f : 0f;
            Amount = Mathf.MoveTowards(Amount, targetAmount, fadeSpeed * deltaTime);
        }

        public void HideImmediately()
        {
            IsOccluded = false;
            Amount = 0f;
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
            MaterialPropertyBlock outlinePropertyBlock)
        {
            for (int i = 0; i < parts.Count; i++)
            {
                parts[i].Sync(
                    Amount,
                    outlineScale,
                    sortingOrderOffset,
                    fillPropertyBlock,
                    outlinePropertyBlock);
            }
        }

        public void Destroy()
        {
            for (int i = 0; i < parts.Count; i++)
            {
                parts[i].Destroy();
            }

            parts.Clear();
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
            MaterialPropertyBlock outlinePropertyBlock)
        {
            if (root == null)
            {
                return;
            }

            bool visible = amount > 0.001f && IsSourceVisible();
            root.SetActive(visible);
            if (!visible)
            {
                return;
            }

            if (sourceSpriteRenderer != null)
            {
                CopySpriteState(sourceSpriteRenderer, outlineSpriteRenderer, sortingOrderOffset, outlinePropertyBlock);
                CopySpriteState(sourceSpriteRenderer, fillSpriteRenderer, sortingOrderOffset + 1, fillPropertyBlock);
            }
            else if (sourceSkinnedMeshRenderer != null)
            {
                CopySkinnedMeshState(sourceSkinnedMeshRenderer, outlineSkinnedMeshRenderer, sortingOrderOffset, outlinePropertyBlock);
                CopySkinnedMeshState(sourceSkinnedMeshRenderer, fillSkinnedMeshRenderer, sortingOrderOffset + 1, fillPropertyBlock);
            }
            else
            {
                CopyMeshState(source, sourceMeshFilter, outlineMeshFilter, outlineRenderer, sortingOrderOffset, outlinePropertyBlock);
                CopyMeshState(source, sourceMeshFilter, fillMeshFilter, fillRenderer, sortingOrderOffset + 1, fillPropertyBlock);
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
            MaterialPropertyBlock propertyBlock)
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
            target.SetPropertyBlock(propertyBlock);
        }

        private void CopyMeshState(
            Renderer source,
            MeshFilter sourceFilter,
            MeshFilter targetFilter,
            Renderer target,
            int sortingOrderOffset,
            MaterialPropertyBlock propertyBlock)
        {
            targetFilter.sharedMesh = sourceFilter.sharedMesh;
            CopyRendererState(source, target, sortingOrderOffset, propertyBlock, sourceFilter.sharedMesh);
        }

        private void CopySkinnedMeshState(
            SkinnedMeshRenderer source,
            SkinnedMeshRenderer target,
            int sortingOrderOffset,
            MaterialPropertyBlock propertyBlock)
        {
            CopySkinnedMeshData(source, target);
            CopyRendererState(source, target, sortingOrderOffset, propertyBlock, source.sharedMesh);
        }

        private void CopyRendererState(
            Renderer source,
            Renderer target,
            int sortingOrderOffset,
            MaterialPropertyBlock propertyBlock,
            Mesh mesh)
        {
            target.sortingLayerID = source.sortingLayerID;
            target.sortingOrder = source.sortingOrder + sortingOrderOffset;
            target.renderingLayerMask = source.renderingLayerMask;
            EnsureMaterialSlots(source, target, material, mesh != null ? mesh.subMeshCount : 1);
            target.SetPropertyBlock(propertyBlock);
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
