using UnityEngine;

/// <summary>
/// Attach to the Cow UI GameObject.
/// Cow must be a child of the World RectTransform (same as WalkArea and BG).
///
/// Hierarchy:
///   Canvas
///   └── World
///       ├── BG
///       ├── WalkArea
///       └── Cow   ← here
/// </summary>
[RequireComponent(typeof(CowAnimator))]
public class CowController : MonoBehaviour
{
    [Header("Movement")]
    public float moveSpeed = 200f;
    public float arrivalThreshold = 6f;

    [Header("Walk Area")]
    [Tooltip("Assign your WalkArea RectTransform. Must be a sibling of the Cow (child of World).")]
    public RectTransform walkArea;

    [Header("References")]
    [Tooltip("Auto-found at startup if left empty")]
    public Canvas canvas;

    // ── private ────────────────────────────────────────────────────────────
    private RectTransform _rect;
    private CowAnimator _anim;
    private Vector2 _target;
    private bool _moving;

    void Awake()
    {
        _rect = GetComponent<RectTransform>();
        _anim = GetComponent<CowAnimator>();
        _target = _rect.anchoredPosition;

        if (canvas == null)
            canvas = GetComponentInParent<Canvas>();
    }

    void Update()
    {
        if (!_moving) return;

        Vector2 current = _rect.anchoredPosition;

        if (Vector2.Distance(current, _target) <= arrivalThreshold)
        {
            _rect.anchoredPosition = _target;
            _moving = false;
            _anim.SetDirection(CowAnimator.MoveDirection.Idle, _anim.FacingRight);
            return;
        }

        // Move one step and clamp — cow can never leave the walk area
        Vector2 next = Vector2.MoveTowards(current, _target, moveSpeed * Time.deltaTime);
        _rect.anchoredPosition = Clamp(next);
    }

    // ── public API (called by CowClickArea) ────────────────────────────────

    public void WalkTo(Vector2 screenPos)
    {
        // Cow's parent = World. Convert the screen click into World local space.
        RectTransform parent = _rect.parent as RectTransform;
        if (parent == null) return;

        Camera cam = canvas.renderMode == RenderMode.ScreenSpaceOverlay
            ? null : canvas.worldCamera;

        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(
                parent, screenPos, cam, out Vector2 worldLocal)) return;

        // Clamp the target so it cannot be outside the walk area from the start
        _target = Clamp(worldLocal);
        _moving = true;

        Vector2 delta = _target - _rect.anchoredPosition;
        bool goingUp = delta.y > Mathf.Abs(delta.x);
        bool goingRight = delta.x >= 0f;

        _anim.SetDirection(
            goingUp ? CowAnimator.MoveDirection.WalkBack
                    : CowAnimator.MoveDirection.WalkSide,
            goingRight);

        SoundManager.Instance.PlaySFX("Cornwalk");
    }

    // ── clamping ───────────────────────────────────────────────────────────

    /// <summary>
    /// Clamps a position (in World/parent local space) inside the WalkArea.
    /// Because Cow, WalkArea, and BG are all children of World, their
    /// anchoredPositions share the same coordinate space — no conversion needed.
    /// </summary>
    Vector2 Clamp(Vector2 position)
    {
        if (walkArea == null) return position;

        // GetWorldCorners → convert each corner to the shared parent (World) local space
        RectTransform parent = _rect.parent as RectTransform;
        Vector3[] corners = new Vector3[4];
        walkArea.GetWorldCorners(corners);

        float minX = float.MaxValue, maxX = float.MinValue;
        float minY = float.MaxValue, maxY = float.MinValue;

        foreach (Vector3 c in corners)
        {
            Vector3 local = parent.InverseTransformPoint(c);
            if (local.x < minX) minX = local.x;
            if (local.x > maxX) maxX = local.x;
            if (local.y < minY) minY = local.y;
            if (local.y > maxY) maxY = local.y;
        }

        return new Vector2(
            Mathf.Clamp(position.x, minX, maxX),
            Mathf.Clamp(position.y, minY, maxY));
    }

    // ── gizmo ──────────────────────────────────────────────────────────────

    void OnDrawGizmosSelected()
    {
        if (walkArea == null) return;
        Gizmos.color = Color.green;
        Vector3[] c = new Vector3[4];
        walkArea.GetWorldCorners(c);
        for (int i = 0; i < 4; i++)
            Gizmos.DrawLine(c[i], c[(i + 1) % 4]);
    }

    public void ForceIdle()
    {
        _moving = false;
        _anim.SetDirection(CowAnimator.MoveDirection.Idle, _anim.FacingRight);
    }
}