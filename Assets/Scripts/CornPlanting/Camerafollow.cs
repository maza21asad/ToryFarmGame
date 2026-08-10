using UnityEngine;

/// <summary>
/// Moves the World RectTransform so the Cow always appears centred on screen.
/// Also handles pinch-to-zoom and drag-to-pan.
///
/// Drag-to-pan behaviour:
///   - Single-finger drag pans the camera (worldRoot shifts opposite to drag direction).
///   - After releasing, the camera stays panned.
///   - When the cow starts moving, camera-follow resumes automatically.
///   - Pan is clamped so the background never shows empty space beyond its edges.
///
/// Hierarchy required:
///   Canvas
///   └── World          ← assign to worldRoot
///       ├── BG         ← assign to bgRect  (used for pan clamping)
///       ├── WalkArea
///       └── Cow        ← assign to cow
/// </summary>
public class CameraFollow : MonoBehaviour
{
    [Header("References")]
    [Tooltip("The World RectTransform that holds BG, WalkArea, Cow, etc.")]
    public RectTransform worldRoot;

    [Tooltip("The Cow RectTransform (must be a child of worldRoot)")]
    public RectTransform cow;

    [Tooltip("The BG RectTransform — used to clamp how far the player can pan. " +
             "Must be a child of worldRoot.")]
    public RectTransform bgRect;

    [Header("Camera Follow")]
    [Tooltip("Follow smoothness. 1 = instant snap. Lower = smoother lag.")]
    [Range(0.01f, 1f)]
    public float smoothSpeed = 0.12f;

    [Tooltip("Optional offset to shift the focus point (e.g. Y=-60 to show more world above the cow)")]
    public Vector2 offset = Vector2.zero;

    [Header("Drag-to-Pan")]
    [Tooltip("How many pixels the pointer must move before it's treated as a drag (not a tap).")]
    public float dragThresholdPixels = 10f;

    [Header("Pinch Zoom")]
    [Tooltip("Minimum allowed scale of the World RectTransform.")]
    public float minZoom = 0.5f;

    [Tooltip("Maximum allowed scale of the World RectTransform.")]
    public float maxZoom = 2.0f;

    [Tooltip("How fast the zoom interpolates toward the target scale.")]
    [Range(0.01f, 1f)]
    public float zoomSmoothSpeed = 0.1f;

    [Tooltip("Pinch sensitivity. Higher = more zoom per pixel of finger movement.")]
    public float pinchSensitivity = 0.005f;

    // ── Private ────────────────────────────────────────────────
    private Vector2 _velocity;
    private Vector3 _baseScale;
    private float _currentZoom = 1f;
    private float _targetZoom = 1f;
    private float _lastPinchDistance;
    private bool _isPinching;

    // Drag-to-pan state
    private bool _isDragging;           // actively panning right now
    private bool _panMode;              // true = player has panned; suppress follow until cow moves
    private Vector2 _dragStartScreenPos;   // where the finger went down
    private Vector2 _lastDragScreenPos;    // previous frame's finger position

    // The cow's anchoredPosition at the moment the player started dragging —
    // used to detect when the cow has moved so we can resume follow.
    private Vector2 _cowPosAtPanStart;

    // Cached at Start — minimum zoom at which the BG still covers the full canvas.
    private float _minZoomFloor;

    // Button zoom — step counter (0 = default, max = MaxZoomSteps)
    private int _zoomSteps = 0;
    private const int MaxZoomSteps = 7;
    private float _defaultZoom;   // zoom level at game start; ZoomOut can never go below this

    private float ZoomStepSize => (maxZoom - _defaultZoom) / MaxZoomSteps;

    private float ComputeMinZoomFloor()
    {
        if (bgRect == null || worldRoot == null) return minZoom;

        Canvas canvas = worldRoot.GetComponentInParent<Canvas>();
        if (canvas == null) return minZoom;

        RectTransform canvasRect = canvas.GetComponent<RectTransform>();
        Vector2 canvasSize = canvasRect.rect.size;
        // _baseScale is guaranteed set before this is called
        Vector2 bgSize = bgRect.rect.size * _baseScale.x;

        if (bgSize.x <= 0f || bgSize.y <= 0f) return minZoom;

        float fitX = canvasSize.x / bgSize.x;
        float fitY = canvasSize.y / bgSize.y;
        return Mathf.Max(minZoom, fitX, fitY);
    }

    // ──────────────────────────────────────────────────────────
    void Start()
    {
        _baseScale = worldRoot != null ? worldRoot.localScale : Vector3.one;
        _minZoomFloor = ComputeMinZoomFloor();   // must come after _baseScale is set
        _targetZoom = Mathf.Clamp(1f, _minZoomFloor, maxZoom);
        _defaultZoom = _targetZoom;   // remember the default so ZoomOut never goes past it
        _currentZoom = _targetZoom;
        if (worldRoot != null)
            worldRoot.localScale = _baseScale * _currentZoom;

        // --- DEBUG: remove once zoom is working ---
        Canvas dbgCanvas = worldRoot != null ? worldRoot.GetComponentInParent<Canvas>() : null;
        RectTransform dbgCanvasRect = dbgCanvas != null ? dbgCanvas.GetComponent<RectTransform>() : null;
        Vector2 dbgCanvasSize = dbgCanvasRect != null ? dbgCanvasRect.rect.size : Vector2.zero;
        Vector2 dbgBgSize = bgRect != null ? bgRect.rect.size : Vector2.zero;
        Debug.Log($"[CameraFollow] _baseScale={_baseScale}  bgRect.size={dbgBgSize}  canvasSize={dbgCanvasSize}  _minZoomFloor={_minZoomFloor}  minZoom={minZoom}  maxZoom={maxZoom}  _targetZoom={_targetZoom}");
        // --- END DEBUG ---
    }

    // ──────────────────────────────────────────────────────────
    void LateUpdate()
    {
        HandlePinchZoom();
        ApplyZoom();
        HandleDragPan();

        // Resume camera-follow automatically once the cow moves away from where it
        // was when panning started.
        if (_panMode && cow != null)
        {
            float cowMoveDist = Vector2.Distance(cow.anchoredPosition, _cowPosAtPanStart);
            if (cowMoveDist > 4f)          // small threshold to avoid floating-point noise
            {
                _panMode = false;
                _velocity = Vector2.zero;  // reset smoothdamp so follow snaps cleanly
            }
        }

        if (!_panMode)
            ApplyCameraFollow();
    }

    // ──────────────────────────────────────────────────────────
    // Drag-to-Pan
    // ──────────────────────────────────────────────────────────
    void HandleDragPan()
    {
        // ── Unified input: prefer touch, fall back to mouse ──
        bool inputDown = false;
        bool inputHeld = false;
        bool inputUp = false;
        Vector2 inputPos = Vector2.zero;

        if (Input.touchCount == 1)
        {
            Touch t = Input.GetTouch(0);
            inputPos = t.position;
            inputDown = t.phase == TouchPhase.Began;
            inputHeld = t.phase == TouchPhase.Moved || t.phase == TouchPhase.Stationary;
            inputUp = t.phase == TouchPhase.Ended || t.phase == TouchPhase.Canceled;
        }
        else if (Input.touchCount == 0)
        {
            inputPos = Input.mousePosition;
            inputDown = Input.GetMouseButtonDown(0);
            inputHeld = Input.GetMouseButton(0);
            inputUp = Input.GetMouseButtonUp(0);
        }
        else
        {
            // Two or more fingers — this is a pinch, not a pan
            if (_isDragging)
                EndDrag();
            return;
        }

        if (inputDown)
        {
            _dragStartScreenPos = inputPos;
            _lastDragScreenPos = inputPos;
            _isDragging = false;   // not confirmed as drag yet
        }
        else if (inputHeld)
        {
            float movedPixels = Vector2.Distance(inputPos, _dragStartScreenPos);

            if (!_isDragging && movedPixels >= dragThresholdPixels)
            {
                // Threshold crossed — this is a drag
                _isDragging = true;
                _panMode = true;
                _cowPosAtPanStart = cow != null ? cow.anchoredPosition : Vector2.zero;
                _lastDragScreenPos = inputPos;
                NotifyCowClickAreaDragStarted();
            }

            if (_isDragging && worldRoot != null)
            {
                Vector2 screenDelta = inputPos - _lastDragScreenPos;
                _lastDragScreenPos = inputPos;

                // Screen-space delta → worldRoot local space delta.
                // We divide by the canvas scaler's reference resolution ratio
                // so panning feels 1:1 with finger movement regardless of screen DPI.
                Canvas canvas = worldRoot.GetComponentInParent<Canvas>();
                float scaleFactor = canvas != null ? canvas.scaleFactor : 1f;
                Vector2 worldDelta = screenDelta / scaleFactor;

                worldRoot.anchoredPosition = ClampPan(worldRoot.anchoredPosition + worldDelta);
            }
        }
        else if (inputUp)
        {
            EndDrag();
        }
    }

    void EndDrag()
    {
        _isDragging = false;
        // _panMode stays true until the cow moves
    }

    // Tell CowClickArea not to fire a WalkTo on the upcoming pointer-up
    void NotifyCowClickAreaDragStarted()
    {
        CowClickArea[] areas = FindObjectsByType<CowClickArea>(FindObjectsSortMode.None);
        foreach (var a in areas)
            a.NotifyDragStarted();
    }

    // ──────────────────────────────────────────────────────────
    // Clamp worldRoot.anchoredPosition so the BG fills the screen
    // ──────────────────────────────────────────────────────────
    Vector2 ClampPan(Vector2 desired)
    {
        if (bgRect == null || worldRoot == null) return desired;

        // The canvas size (in canvas units) — this is the "viewport"
        Canvas canvas = worldRoot.GetComponentInParent<Canvas>();
        if (canvas == null) return desired;

        RectTransform canvasRect = canvas.GetComponent<RectTransform>();
        Vector2 canvasSize = canvasRect.rect.size;

        // BG size in canvas units, accounting for current world scale
        Vector2 bgSize = bgRect.rect.size * worldRoot.localScale.x;

        // Half-extents of how far the world root can shift before the BG edge shows
        float halfW = Mathf.Max(0f, (bgSize.x - canvasSize.x) * 0.5f);
        float halfH = Mathf.Max(0f, (bgSize.y - canvasSize.y) * 0.5f);

        // bgRect pivot/anchor offsets relative to worldRoot origin
        Vector2 bgAnchoredPos = bgRect.anchoredPosition * worldRoot.localScale.x;

        float minX = -bgAnchoredPos.x - halfW;
        float maxX = -bgAnchoredPos.x + halfW;
        float minY = -bgAnchoredPos.y - halfH;
        float maxY = -bgAnchoredPos.y + halfH;

        return new Vector2(
            Mathf.Clamp(desired.x, minX, maxX),
            Mathf.Clamp(desired.y, minY, maxY));
    }

    // ──────────────────────────────────────────────────────────
    // Pinch-to-zoom
    // ──────────────────────────────────────────────────────────
    void HandlePinchZoom()
    {
        if (Input.touchCount != 2)
        {
            _isPinching = false;
            return;
        }

        Touch t0 = Input.GetTouch(0);
        Touch t1 = Input.GetTouch(1);

        float currentDistance = Vector2.Distance(t0.position, t1.position);

        if (!_isPinching)
        {
            _lastPinchDistance = currentDistance;
            _isPinching = true;
            return;
        }

        float delta = currentDistance - _lastPinchDistance;
        _lastPinchDistance = currentDistance;

        _targetZoom += delta * pinchSensitivity;
        _targetZoom = Mathf.Clamp(_targetZoom, _minZoomFloor, maxZoom);
    }

    // ──────────────────────────────────────────────────────────
    void ApplyZoom()
    {
        if (worldRoot == null) return;

        float t = 1f - Mathf.Exp(-zoomSmoothSpeed * 30f * Time.deltaTime);
        _currentZoom = Mathf.Lerp(_currentZoom, _targetZoom, t);

        if (float.IsNaN(_currentZoom)) return;

        worldRoot.localScale = _baseScale * _currentZoom;
    }

    // ──────────────────────────────────────────────────────────
    // Camera follow — only active when not in pan mode
    // ──────────────────────────────────────────────────────────
    void ApplyCameraFollow()
    {
        if (worldRoot == null || cow == null) return;

        Vector2 desired = -(cow.anchoredPosition * worldRoot.localScale.x) + offset;
        desired = ClampPan(desired);

        worldRoot.anchoredPosition = Vector2.SmoothDamp(
            worldRoot.anchoredPosition,
            desired,
            ref _velocity,
            smoothSpeed);
    }

    // ──────────────────────────────────────────────────────────
    // Button zoom  (step counter: 0 = default, max = MaxZoomSteps = 3)
    //
    //  _zoomSteps  │  CanZoomIn  │  CanZoomOut
    //  ────────────┼─────────────┼────────────
    //       0      │     ✓       │      ✗       ← default view
    //       1      │     ✓       │      ✓
    //       2      │     ✓       │      ✓
    //       3      │     ✗       │      ✓
    // ──────────────────────────────────────────────────────────
    public void ZoomInButton()
    {
        if (_zoomSteps >= MaxZoomSteps) return;
        _zoomSteps++;
        _targetZoom = _defaultZoom + _zoomSteps * ZoomStepSize;
        Debug.Log($"[CameraFollow] ZoomIn  → steps={_zoomSteps}  _targetZoom={_targetZoom}");
    }

    public void ZoomOutButton()
    {
        if (_zoomSteps <= 0) return;   // already at default — cannot zoom out further
        _zoomSteps--;
        _targetZoom = _defaultZoom + _zoomSteps * ZoomStepSize;
        Debug.Log($"[CameraFollow] ZoomOut → steps={_zoomSteps}  _targetZoom={_targetZoom}");
    }

    public void ResetZoom()
    {
        _zoomSteps = 0;
        _targetZoom = _defaultZoom;
    }

    /// <summary>Returns true when the Zoom In button should be interactable.</summary>
    public bool CanZoomIn() => _zoomSteps < MaxZoomSteps;

    /// <summary>Returns true when the Zoom Out button should be interactable.</summary>
    public bool CanZoomOut() => _zoomSteps > 0;

    /// <summary>
    /// Call this to force the camera back into follow mode (e.g. when the game starts).
    /// </summary>
    public void ResumeFollow()
    {
        _panMode = false;
        _isDragging = false;
        _velocity = Vector2.zero;
    }
}