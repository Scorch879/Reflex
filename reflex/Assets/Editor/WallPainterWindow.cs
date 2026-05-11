using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public class WallPainterWindow : EditorWindow
{
    private const string HelpText = "Left mouse drag in Scene view to paint. Hold Shift while dragging to erase.";

    [SerializeField] private GameObject wallAsset;
    [SerializeField] private Transform parent;
    [SerializeField] private LayerMask paintSurfaceMask = ~0;
    [SerializeField] private float gridSize = 1f;
    [SerializeField] private float yOffset;
    [SerializeField] private Vector3 rotationOffset;
    [SerializeField] private Vector3 scale = Vector3.one;
    [SerializeField] private bool alignToSurfaceNormal;
    [SerializeField] private bool useGroundPlaneWhenNoSurface = true;
    [SerializeField] private float groundPlaneY;

    private readonly HashSet<Vector3Int> paintedCells = new HashSet<Vector3Int>();
    private bool paintingEnabled;
    private Vector3 previewPosition;
    private Vector3 previewNormal = Vector3.up;
    private bool hasPreview;

    [MenuItem("Tools/Reflex/Wall Painter")]
    public static void Open()
    {
        GetWindow<WallPainterWindow>("Wall Painter");
    }

    private void OnEnable()
    {
        SceneView.duringSceneGui += OnSceneGUI;
    }

    private void OnDisable()
    {
        SceneView.duringSceneGui -= OnSceneGUI;
    }

    private void OnGUI()
    {
        EditorGUILayout.LabelField("Wall Painter", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox(HelpText, MessageType.Info);

        wallAsset = (GameObject)EditorGUILayout.ObjectField("Wall Model/Prefab", wallAsset, typeof(GameObject), false);
        parent = (Transform)EditorGUILayout.ObjectField("Parent", parent, typeof(Transform), true);
        paintSurfaceMask = LayerMaskField("Paint Surface Mask", paintSurfaceMask);

        EditorGUILayout.Space(6f);
        gridSize = Mathf.Max(0.01f, EditorGUILayout.FloatField("Grid Size", gridSize));
        yOffset = EditorGUILayout.FloatField("Y Offset", yOffset);
        rotationOffset = EditorGUILayout.Vector3Field("Rotation Offset", rotationOffset);
        scale = EditorGUILayout.Vector3Field("Scale", scale);
        alignToSurfaceNormal = EditorGUILayout.Toggle("Align To Surface Normal", alignToSurfaceNormal);
        useGroundPlaneWhenNoSurface = EditorGUILayout.Toggle("Use Ground Plane Fallback", useGroundPlaneWhenNoSurface);

        using (new EditorGUI.DisabledScope(!useGroundPlaneWhenNoSurface))
        {
            groundPlaneY = EditorGUILayout.FloatField("Ground Plane Y", groundPlaneY);
        }

        EditorGUILayout.Space(6f);
        using (new EditorGUI.DisabledScope(wallAsset == null))
        {
            string buttonText = paintingEnabled ? "Stop Painting" : "Start Painting";
            if (GUILayout.Button(buttonText, GUILayout.Height(28f)))
            {
                paintingEnabled = !paintingEnabled;
                SceneView.RepaintAll();
            }
        }

        if (GUILayout.Button("Clear Brush Memory"))
        {
            paintedCells.Clear();
        }

        if (wallAsset == null)
        {
            EditorGUILayout.HelpBox("Assign an FBX/model or prefab first. Your Big Wall FBX can go here.", MessageType.Warning);
        }
    }

    private void OnSceneGUI(SceneView sceneView)
    {
        if (!paintingEnabled || wallAsset == null)
        {
            return;
        }

        Event currentEvent = Event.current;
        HandleUtility.AddDefaultControl(GUIUtility.GetControlID(FocusType.Passive));

        if (TryGetPaintPoint(currentEvent.mousePosition, out Vector3 point, out Vector3 normal))
        {
            previewPosition = SnapToGrid(point) + Vector3.up * yOffset;
            previewNormal = normal;
            hasPreview = true;
            DrawPreview();
        }
        else
        {
            hasPreview = false;
        }

        bool isPaintEvent = currentEvent.type == EventType.MouseDown || currentEvent.type == EventType.MouseDrag;
        if (isPaintEvent && currentEvent.button == 0 && hasPreview)
        {
            if (currentEvent.shift)
            {
                EraseAt(previewPosition);
            }
            else
            {
                PaintAt(previewPosition, previewNormal);
            }

            currentEvent.Use();
        }

        sceneView.Repaint();
    }

    private bool TryGetPaintPoint(Vector2 mousePosition, out Vector3 point, out Vector3 normal)
    {
        Ray ray = HandleUtility.GUIPointToWorldRay(mousePosition);

        if (Physics.Raycast(ray, out RaycastHit hit, 1000f, paintSurfaceMask))
        {
            point = hit.point;
            normal = hit.normal;
            return true;
        }

        if (useGroundPlaneWhenNoSurface)
        {
            Plane plane = new Plane(Vector3.up, new Vector3(0f, groundPlaneY, 0f));
            if (plane.Raycast(ray, out float enter))
            {
                point = ray.GetPoint(enter);
                normal = Vector3.up;
                return true;
            }
        }

        point = Vector3.zero;
        normal = Vector3.up;
        return false;
    }

    private void PaintAt(Vector3 position, Vector3 normal)
    {
        Vector3Int cell = GetCell(position);
        if (!paintedCells.Add(cell))
        {
            return;
        }

        GameObject instance = InstantiateWallAsset();
        Undo.RegisterCreatedObjectUndo(instance, "Paint Wall");

        instance.transform.SetParent(parent, true);
        instance.transform.position = position;
        instance.transform.rotation = GetRotation(normal);
        instance.transform.localScale = scale;
        instance.name = wallAsset.name;
    }

    private GameObject InstantiateWallAsset()
    {
        GameObject prefabInstance = PrefabUtility.InstantiatePrefab(wallAsset) as GameObject;
        if (prefabInstance != null)
        {
            return prefabInstance;
        }

        return Instantiate(wallAsset);
    }

    private void EraseAt(Vector3 position)
    {
        Vector3Int cell = GetCell(position);
        Vector3 center = CellToWorld(cell);
        float radius = gridSize * 0.45f;
        GameObject closest = null;
        float closestDistance = float.MaxValue;

        foreach (GameObject candidate in GameObject.FindObjectsByType<GameObject>(FindObjectsSortMode.None))
        {
            if (!candidate.scene.IsValid() || candidate == null)
            {
                continue;
            }

            if (candidate.name != wallAsset.name && candidate.name != $"{wallAsset.name}(Clone)")
            {
                continue;
            }

            float distance = Vector3.Distance(candidate.transform.position, center);
            if (distance <= radius && distance < closestDistance)
            {
                closest = candidate;
                closestDistance = distance;
            }
        }

        if (closest != null)
        {
            paintedCells.Remove(cell);
            Undo.DestroyObjectImmediate(closest);
        }
    }

    private Vector3 SnapToGrid(Vector3 point)
    {
        return new Vector3(
            Mathf.Round(point.x / gridSize) * gridSize,
            Mathf.Round(point.y / gridSize) * gridSize,
            Mathf.Round(point.z / gridSize) * gridSize
        );
    }

    private Vector3Int GetCell(Vector3 point)
    {
        return new Vector3Int(
            Mathf.RoundToInt(point.x / gridSize),
            Mathf.RoundToInt(point.y / gridSize),
            Mathf.RoundToInt(point.z / gridSize)
        );
    }

    private Vector3 CellToWorld(Vector3Int cell)
    {
        return new Vector3(cell.x * gridSize, cell.y * gridSize, cell.z * gridSize);
    }

    private Quaternion GetRotation(Vector3 normal)
    {
        Quaternion baseRotation = alignToSurfaceNormal ? Quaternion.FromToRotation(Vector3.up, normal) : Quaternion.identity;
        return baseRotation * Quaternion.Euler(rotationOffset);
    }

    private void DrawPreview()
    {
        Handles.color = new Color(0.2f, 0.8f, 1f, 0.9f);
        Handles.DrawWireCube(previewPosition, Vector3.one * gridSize);
        Handles.ArrowHandleCap(0, previewPosition, Quaternion.LookRotation(previewNormal), gridSize * 0.5f, EventType.Repaint);
    }

    private LayerMask LayerMaskField(string label, LayerMask selected)
    {
        List<string> layerNames = new List<string>();
        List<int> layerNumbers = new List<int>();

        for (int i = 0; i < 32; i++)
        {
            string layerName = LayerMask.LayerToName(i);
            if (!string.IsNullOrEmpty(layerName))
            {
                layerNames.Add(layerName);
                layerNumbers.Add(i);
            }
        }

        int maskWithoutEmpty = 0;
        for (int i = 0; i < layerNumbers.Count; i++)
        {
            if (((1 << layerNumbers[i]) & selected.value) != 0)
            {
                maskWithoutEmpty |= 1 << i;
            }
        }

        maskWithoutEmpty = EditorGUILayout.MaskField(label, maskWithoutEmpty, layerNames.ToArray());

        int mask = 0;
        for (int i = 0; i < layerNumbers.Count; i++)
        {
            if ((maskWithoutEmpty & (1 << i)) != 0)
            {
                mask |= 1 << layerNumbers[i];
            }
        }

        selected.value = mask;
        return selected;
    }
}
