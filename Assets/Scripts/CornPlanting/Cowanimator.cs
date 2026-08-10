using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Sprite-sheet animator for the Cow UI Image.
/// Supports three animation states: Idle, Walk (side), Walk Back (moving away).
/// CowController drives the state via SetDirection().
/// </summary>
[RequireComponent(typeof(Image))]
public class CowAnimator : MonoBehaviour
{
    // ── Inspector ──────────────────────────────────────────────────────────

    [Header("Idle Frames")]
    [Tooltip("Frames to play when the cow is standing still")]
    public Sprite[] idleFrames;

    [Header("Walk Frames  (side / forward)")]
    [Tooltip("Frames for walking left or right. Flipped automatically for left.")]
    public Sprite[] walkFrames;

    [Header("Back Walk Frames  (walking away)")]
    [Tooltip("Frames for walking away from the camera (upward on screen). " +
             "If empty, walk frames are used instead.")]
    public Sprite[] backWalkFrames;

    [Header("Speed")]
    [Tooltip("Frames per second for all animations")]
    public float framesPerSecond = 8f;

    // ── public state (read by CowController) ───────────────────────────────

    public enum MoveDirection { Idle, WalkSide, WalkBack }

    public bool FacingRight { get; private set; } = true;

    // ── private ────────────────────────────────────────────────────────────

    private Image _image;
    private MoveDirection _direction = MoveDirection.Idle;
    private int _frameIndex;
    private float _timer;

    void Awake()
    {
        _image = GetComponent<Image>();
    }

    void Update()
    {
        TickAnimation();
        ApplyFlip();
    }

    // ── public API ─────────────────────────────────────────────────────────

    /// <summary>
    /// Call this whenever movement state or direction changes.
    /// </summary>
    public void SetDirection(MoveDirection dir, bool facingRight = true)
    {
        bool changed = dir != _direction || facingRight != FacingRight;

        _direction = dir;
        FacingRight = facingRight;

        if (changed)
        {
            _frameIndex = 0;
            _timer = 0f;
            DisplayFrame(0);    // show first frame of new state instantly
        }
    }

    // ── animation tick ─────────────────────────────────────────────────────

    void TickAnimation()
    {
        Sprite[] frames = ActiveFrames();
        if (frames == null || frames.Length == 0) return;

        _timer += Time.deltaTime;
        float duration = 1f / Mathf.Max(framesPerSecond, 0.01f);
        if (_timer < duration) return;

        _timer -= duration;
        _frameIndex = (_frameIndex + 1) % frames.Length;
        DisplayFrame(_frameIndex);
    }

    void DisplayFrame(int index)
    {
        Sprite[] frames = ActiveFrames();
        if (frames == null || frames.Length == 0) return;
        _image.sprite = frames[Mathf.Clamp(index, 0, frames.Length - 1)];
    }

    // ── sprite flip ────────────────────────────────────────────────────────

    void ApplyFlip()
    {
        // Only flip for side walking; back walk and idle stay unflipped
        bool flip = (_direction == MoveDirection.WalkSide) && !FacingRight;
        Vector3 s = transform.localScale;
        s.x = flip ? -Mathf.Abs(s.x) : Mathf.Abs(s.x);
        transform.localScale = s;
    }

    // ── helpers ────────────────────────────────────────────────────────────

    Sprite[] ActiveFrames()
    {
        switch (_direction)
        {
            case MoveDirection.WalkBack:
                return (backWalkFrames != null && backWalkFrames.Length > 0)
                    ? backWalkFrames
                    : walkFrames;       // fallback to walk frames if back frames not assigned

            case MoveDirection.WalkSide:
                return walkFrames;

            default:
                return idleFrames;
        }
    }
}