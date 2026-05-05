using UnityEngine;
using UnityEngine.U2D.Animation;

/// <summary>
/// Drives the big enemy's sprite "perspective" (front/back/side/diagonal)
/// from its look direction relative to the main camera.
/// 
/// Expected SpriteLibrary setup (Category: "Idle"):
/// - Front, Back, Side, DiagonalFront, DiagonalBack
/// </summary>
public sealed class BigEnemyPerspective : MonoBehaviour
{
    [Header("Sprite")]
    [SerializeField] private SpriteResolver resolver;
    [SerializeField] private SpriteRenderer spriteRenderer;

    [Header("Sprite Library Names")]
    [SerializeField] private string category = "Idle";

    [SerializeField] private string frontLabel = "Front";
    [SerializeField] private string backLabel = "Back";
    [SerializeField] private string sideLabel = "Side";
    [SerializeField] private string diagonalFrontLabel = "DiagonalFront";
    [SerializeField] private string diagonalBackLabel = "DiagonalBack";

    [Header("Tuning")]
    [Tooltip("If true, uses transform.forward as the look direction. If false, uses a target transform when set.")]
    [SerializeField] private bool useTransformForward = true;

    [Tooltip("Optional: if set and useTransformForward is false, we look toward this target.")]
    [SerializeField] private Transform lookTarget;

    [Tooltip("Degrees. Below this angle to camera view direction => Front/Back.")]
    [Range(0f, 90f)]
    [SerializeField] private float frontBackAngle = 25f;

    [Tooltip("Degrees. Near perpendicular to camera view direction => Side.")]
    [Range(0f, 90f)]
    [SerializeField] private float sideAngleFromPerpendicular = 20f;

    private Camera _cam;
    private string _lastLabel;
    private bool _lastFlipX;

    private void Reset()
    {
        resolver = GetComponentInChildren<SpriteResolver>();
        spriteRenderer = GetComponentInChildren<SpriteRenderer>();
    }

    private void Awake()
    {
        if (resolver == null) resolver = GetComponentInChildren<SpriteResolver>();
        if (spriteRenderer == null) spriteRenderer = GetComponentInChildren<SpriteRenderer>();
    }

    private void LateUpdate()
    {
        if (resolver == null || spriteRenderer == null) return;

        if (_cam == null) _cam = Camera.main;
        if (_cam == null) return;

        Vector3 facing = GetFacingDirectionXZ();
        if (facing.sqrMagnitude < 0.0001f) return;

        // Camera "view direction" on the ground plane (direction from camera into scene).
        Vector3 viewDir = -_cam.transform.forward;
        viewDir.y = 0f;
        viewDir = viewDir.sqrMagnitude > 0.0001f ? viewDir.normalized : Vector3.forward;

        Vector3 camRight = _cam.transform.right;
        camRight.y = 0f;
        camRight = camRight.sqrMagnitude > 0.0001f ? camRight.normalized : Vector3.right;

        float angleToView = Vector3.Angle(facing, viewDir); // 0 => facing toward camera (front)
        float angleToPerp = Mathf.Abs(90f - angleToView);

        string label;
        if (angleToView <= frontBackAngle)
        {
            label = frontLabel;
        }
        else if (angleToView >= 180f - frontBackAngle)
        {
            label = backLabel;
        }
        else if (angleToPerp <= sideAngleFromPerpendicular)
        {
            label = sideLabel;
        }
        else
        {
            // Diagonal: choose whether it's closer to front or back half-space.
            float towardCamera = Vector3.Dot(facing, viewDir);
            label = towardCamera >= 0f ? diagonalFrontLabel : diagonalBackLabel;
        }

        // Left/right is handled via horizontal flip relative to camera right axis.
        bool flipX = Vector3.Dot(facing, camRight) < 0f;

        if (_lastLabel != label)
        {
            resolver.SetCategoryAndLabel(category, label);
            _lastLabel = label;
        }

        if (_lastFlipX != flipX)
        {
            spriteRenderer.flipX = flipX;
            _lastFlipX = flipX;
        }
    }

    private Vector3 GetFacingDirectionXZ()
    {
        Vector3 dir;

        if (!useTransformForward && lookTarget != null)
        {
            dir = lookTarget.position - transform.position;
        }
        else
        {
            dir = transform.forward;
        }

        dir.y = 0f;
        return dir.normalized;
    }
}

