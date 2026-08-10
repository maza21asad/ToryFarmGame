//using UnityEngine;
//using UnityEngine.UI;
//using System.Collections;

///// <summary>
///// Attach to the COOKED grill prefab (same GameObject as DraggableGrill).
///// Works for BOTH RedMeat and Shrimp — grillType is read from DraggableGrill.
/////
///// TIMING
/////   OnEnable         → 5 s grace timer starts (grill just cooked)
/////   Grace ends       → 5 s burn animation plays (overlay on top of grill)
/////   Burn anim ends   → last frame of burn animation is HELD; cooked sprite hidden
/////   DraggableGrill disabled → DraggableBurnedGrill enabled → player drags to dustbin
/////
/////   If the player picks up the grill, the timer pauses. On put-down it resumes.
/////   If the grill is dropped on the dustbin (via DraggableGrill.TryDropOnDustbin)
/////   BurnerGrill.enabled is set to false before any of this fires.
/////
///// PREFAB SETUP
/////   On the cooked grill GameObject add:
/////     BurnerGrill            (this script)
/////     DraggableBurnedGrill   (disabled by default — auto-enabled when burned)
///// </summary>
//public class BurnerGrill : MonoBehaviour
//{
//    [Header("Burn Timer")]
//    [Tooltip("Seconds after the grill finishes cooking before the burn animation starts.\n" +
//             "Set this to 5 s to match design.")]
//    public float burnDuration = 5f;

//    [Header("Burn Animation  (plays once when burning starts — ~5 s)")]
//    public FrameAnimation burnAnimation;

//    [Header("Animation Overlay Size and Offset")]
//    public float overlayWidth  = 100f;
//    public float overlayHeight = 100f;
//    public Vector2 overlayOffset = Vector2.zero;

//    // ── Private ───────────────────────────────────────────────────────────────

//    private float _timer;
//    private bool  _burning   = false;
//    private bool  _isDragged = false;
//    private Image _image;

//    // ── Unity ─────────────────────────────────────────────────────────────────

//    private void Awake()
//    {
//        _image = GetComponent<Image>();

//        // DraggableBurnedGrill starts disabled — BurnerGrill enables it after burning.
//        DraggableBurnedGrill db = GetComponent<DraggableBurnedGrill>();
//        if (db != null) db.enabled = false;
//    }

//    private void OnEnable()
//    {
//        _timer   = burnDuration;
//        _burning = false;
//    }

//    private void Update()
//    {
//        if (_burning || _isDragged) return;

//        _timer -= Time.deltaTime;
//        if (_timer <= 0f)
//            StartCoroutine(BurnRoutine());
//    }

//    // ── Called by DraggableGrill to pause / resume the timer ──────────────────

//    public void SetDragging(bool dragging) => _isDragged = dragging;

//    // ── Burn sequence ─────────────────────────────────────────────────────────

//    private IEnumerator BurnRoutine()
//    {
//        _burning = true;

//        // Stop normal grill dragging immediately.
//        DraggableGrill dg = GetComponent<DraggableGrill>();
//        if (dg != null) dg.enabled = false;

//        // Hide the cooked sprite while the burn animation plays.
//        if (_image != null) _image.enabled = false;

//        // ── Build an overlay child that plays the burn animation ──────────────
//        Image overlayImage = null;
//        FrameAnimator animator = null;

//        if (burnAnimation != null && burnAnimation.frames != null && burnAnimation.frames.Length > 0)
//        {
//            GameObject overlayGO = new GameObject("BurnOverlay",
//                typeof(RectTransform), typeof(Image));
//            overlayGO.transform.SetParent(transform, false);

//            RectTransform overlayRect = overlayGO.GetComponent<RectTransform>();
//            overlayRect.anchorMin        = new Vector2(0.5f, 0.5f);
//            overlayRect.anchorMax        = new Vector2(0.5f, 0.5f);
//            overlayRect.pivot            = new Vector2(0.5f, 0.5f);
//            overlayRect.sizeDelta        = new Vector2(overlayWidth, overlayHeight);
//            overlayRect.anchoredPosition = overlayOffset;
//            overlayRect.localScale       = Vector3.one;

//            overlayImage = overlayGO.GetComponent<Image>();
//            overlayImage.raycastTarget = false;
//            overlayImage.color         = Color.white;
//            overlayImage.sprite        = burnAnimation.frames[0];

//            // Play once — FrameAnimator holds the last frame after finishing.
//            animator = overlayGO.AddComponent<FrameAnimator>();
//            bool done = false;
//            animator.Play(burnAnimation, loop: false, onComplete: () => done = true);
//            yield return new WaitUntil(() => done);

//            // ── Hold last frame: keep the overlay alive (do NOT Destroy it). ─
//            // The overlay's Image still shows the final burn frame.
//            // Disable the FrameAnimator so it won't interfere.
//            animator.enabled = false;

//            // overlayGO intentionally NOT destroyed — last frame stays visible.
//        }

//        // ── Free the plate slot so it doesn't stay locked ────────────────────
//        if (dg != null && dg.owningPlate != null)
//            dg.owningPlate.FreeSlotOf(dg);

//        // ── Enable burned dragging ────────────────────────────────────────────
//        DraggableBurnedGrill db = GetComponent<DraggableBurnedGrill>();
//        if (db != null) db.enabled = true;

//        Debug.Log($"[BurnerGrill] '{name}' burned — last burn frame held.");
//    }
//}


//using UnityEngine;
//using UnityEngine.UI;
//using System.Collections;

///// <summary>
///// Attach to the COOKED grill prefab (same GameObject as DraggableGrill).
///// Works for BOTH RedMeat and Shrimp — grillType is read from DraggableGrill.
/////
///// TIMING
/////   OnEnable         → 5 s grace timer starts (grill just cooked)
/////   Grace ends       → 5 s burn animation plays (overlay on top of grill)
/////   Burn anim ends   → last frame of burn animation is HELD; cooked sprite hidden
/////   DraggableGrill disabled → DraggableBurnedGrill enabled → player drags to dustbin
/////
/////   If the player picks up the grill, the timer pauses. On put-down it resumes.
/////   If the grill is dropped on the dustbin (via DraggableGrill.TryDropOnDustbin)
/////   BurnerGrill.enabled is set to false before any of this fires.
/////
///// PREFAB SETUP
/////   On the cooked grill GameObject add:
/////     BurnerGrill            (this script)
/////     DraggableBurnedGrill   (disabled by default — auto-enabled when burned)
///// </summary>
//public class BurnerGrill : MonoBehaviour
//{
//    [Header("Burn Timer")]
//    [Tooltip("Seconds after the grill finishes cooking before the burn animation starts.\n" +
//             "Set this to 5 s to match design.")]
//    public float burnDuration = 5f;

//    [Header("Burn Animation  (plays once when burning starts — ~5 s)")]
//    public FrameAnimation burnAnimation;

//    [Header("Animation Overlay Size and Offset")]
//    public float overlayWidth  = 100f;
//    public float overlayHeight = 100f;
//    public Vector2 overlayOffset = Vector2.zero;

//    // ── Private ───────────────────────────────────────────────────────────────

//    private float _timer;
//    private bool  _burning   = false;
//    private bool  _isDragged = false;
//    private Image _image;

//    // ── Unity ─────────────────────────────────────────────────────────────────

//    private void Awake()
//    {
//        _image = GetComponent<Image>();

//        // DraggableBurnedGrill starts disabled — BurnerGrill enables it after burning.
//        DraggableBurnedGrill db = GetComponent<DraggableBurnedGrill>();
//        if (db != null) db.enabled = false;
//    }

//    private void OnEnable()
//    {
//        _timer   = burnDuration;
//        _burning = false;
//    }

//    private void Update()
//    {
//        if (_burning || _isDragged) return;

//        _timer -= Time.deltaTime;
//        if (_timer <= 0f)
//            StartCoroutine(BurnRoutine());
//    }

//    // ── Called by DraggableGrill to pause / resume the timer ──────────────────

//    public void SetDragging(bool dragging) => _isDragged = dragging;

//    // ── Burn sequence ─────────────────────────────────────────────────────────

//    private IEnumerator BurnRoutine()
//    {
//        _burning = true;

//        // Stop normal grill dragging immediately.
//        DraggableGrill dg = GetComponent<DraggableGrill>();
//        if (dg != null) dg.enabled = false;

//        // Hide the cooked sprite while the burn animation plays.
//        if (_image != null) _image.enabled = false;

//        // ── Build an overlay child that plays the burn animation ──────────────
//        Image overlayImage = null;
//        FrameAnimator animator = null;

//        if (burnAnimation != null && burnAnimation.frames != null && burnAnimation.frames.Length > 0)
//        {
//            GameObject overlayGO = new GameObject("BurnOverlay",
//                typeof(RectTransform), typeof(Image));
//            overlayGO.transform.SetParent(transform, false);

//            RectTransform overlayRect = overlayGO.GetComponent<RectTransform>();
//            overlayRect.anchorMin        = new Vector2(0.5f, 0.5f);
//            overlayRect.anchorMax        = new Vector2(0.5f, 0.5f);
//            overlayRect.pivot            = new Vector2(0.5f, 0.5f);
//            overlayRect.sizeDelta        = new Vector2(overlayWidth, overlayHeight);
//            overlayRect.anchoredPosition = overlayOffset;
//            overlayRect.localScale       = Vector3.one;

//            overlayImage = overlayGO.GetComponent<Image>();
//            overlayImage.raycastTarget = false;
//            overlayImage.color         = Color.white;
//            overlayImage.sprite        = burnAnimation.frames[0];

//            // Play once — FrameAnimator holds the last frame after finishing.
//            animator = overlayGO.AddComponent<FrameAnimator>();
//            bool done = false;
//            animator.Play(burnAnimation, loop: false, onComplete: () => done = true);
//            yield return new WaitUntil(() => done);

//            // ── Hold last frame: keep the overlay alive (do NOT Destroy it). ─
//            // The overlay's Image still shows the final burn frame.
//            // Disable the FrameAnimator so it won't interfere.
//            animator.enabled = false;

//            // overlayGO intentionally NOT destroyed — last frame stays visible.
//        }

//        // ── Free the plate slot so it doesn't stay locked ────────────────────
//        if (dg != null && dg.owningPlate != null)
//            dg.owningPlate.FreeSlotOf(dg);

//        // ── Enable burned dragging ────────────────────────────────────────────
//        DraggableBurnedGrill db = GetComponent<DraggableBurnedGrill>();
//        if (db != null) db.enabled = true;

//        Debug.Log($"[BurnerGrill] '{name}' burned — last burn frame held.");
//    }
//}


using UnityEngine;
using UnityEngine.UI;
using System.Collections;

/// <summary>
/// Attach to the COOKED grill prefab (same GameObject as DraggableGrill).
/// Works for BOTH RedMeat and Shrimp — grillType is read from DraggableGrill.
///
/// TIMING
///   OnEnable         → burnDuration (5 s) grace timer starts (grill just cooked)
///   Grace ends       → 5 s burn animation plays (overlay on top of grill)
///   Burn anim ends   → last frame of burn animation is HELD; cooked sprite hidden
///   DraggableGrill disabled → DraggableBurnedGrill enabled → player drags to dustbin
///
///   If the player picks up the grill, the timer pauses. On put-down it resumes.
///   If the grill is dropped on the dustbin (via DraggableGrill.TryDropOnDustbin)
///   BurnerGrill.enabled is set to false before any of this fires.
///
/// PREFAB SETUP
///   On the cooked grill GameObject add:
///     BurnerGrill            (this script)
///     DraggableBurnedGrill   (disabled by default — auto-enabled when burned)
///
/// FIX — "burned grills cannot be dragged from the stove"
///   Root cause: after BurnRoutine() finished, the grill was still deep inside the
///   stove's slot anchor hierarchy. DraggableBurnedGrill.OnBeginDrag tries to lift
///   the object to _canvas.transform, but if _canvas was null or the object's
///   CanvasGroup.blocksRaycasts was false it would never receive pointer events.
///   Fix: at the END of BurnRoutine(), before enabling DraggableBurnedGrill, we
///   (a) re-enable the CanvasGroup so raycasts reach it, and
///   (b) lift the grill to the canvas root (same technique GrillCooker uses),
///   so pointer events always work regardless of stove hierarchy depth.
/// </summary>
public class BurnerGrill : MonoBehaviour
{
    [Header("Burn Timer")]
    [Tooltip("Seconds after the grill finishes cooking before the burn animation starts.\n" +
             "Set this to 5 s to match design.")]
    public float burnDuration = 5f;

    [Header("Burn Animation  (plays once when burning starts — ~5 s)")]
    public FrameAnimation burnAnimation;

    [Header("Animation Overlay Size and Offset")]
    public float overlayWidth = 100f;
    public float overlayHeight = 100f;
    public Vector2 overlayOffset = Vector2.zero;

    // ── Private ───────────────────────────────────────────────────────────────

    private float _timer;
    private bool _burning = false;
    private bool _isDragged = false;
    private Image _image;

    // ── Unity ─────────────────────────────────────────────────────────────────

    private void Awake()
    {
        _image = GetComponent<Image>();

        // DraggableBurnedGrill starts disabled — BurnerGrill enables it after burning.
        DraggableBurnedGrill db = GetComponent<DraggableBurnedGrill>();
        if (db != null) db.enabled = false;
    }

    private void OnEnable()
    {
        _timer = burnDuration;
        _burning = false;
    }

    private void Update()
    {
        if (_burning || _isDragged) return;

        _timer -= Time.deltaTime;
        if (_timer <= 0f)
            StartCoroutine(BurnRoutine());
    }

    // ── Called by DraggableGrill to pause / resume the timer ──────────────────

    public void SetDragging(bool dragging) => _isDragged = dragging;

    // ── Burn sequence ─────────────────────────────────────────────────────────

    private IEnumerator BurnRoutine()
    {
        _burning = true;

        // Stop normal grill dragging immediately.
        DraggableGrill dg = GetComponent<DraggableGrill>();
        if (dg != null) dg.enabled = false;

        // ── Build an overlay child that plays the burn animation ──────────────
        // The overlay renders ON TOP of the cooked sprite (higher sibling index).
        // We do NOT hide _image here — it stays visible underneath the overlay,
        // which is intentional (the flame plays over the food visual).
        // After the animation finishes we hide the cooked sprite and keep only
        // the last burn frame visible.

        if (burnAnimation != null && burnAnimation.frames != null && burnAnimation.frames.Length > 0)
        {
            GameObject overlayGO = new GameObject("BurnOverlay",
                typeof(RectTransform), typeof(Image));
            overlayGO.transform.SetParent(transform, false);

            RectTransform overlayRect = overlayGO.GetComponent<RectTransform>();
            overlayRect.anchorMin = new Vector2(0.5f, 0.5f);
            overlayRect.anchorMax = new Vector2(0.5f, 0.5f);
            overlayRect.pivot = new Vector2(0.5f, 0.5f);
            overlayRect.sizeDelta = new Vector2(overlayWidth, overlayHeight);
            overlayRect.anchoredPosition = overlayOffset;
            overlayRect.localScale = Vector3.one;

            Image overlayImage = overlayGO.GetComponent<Image>();
            overlayImage.raycastTarget = false;
            overlayImage.color = Color.white;
            overlayImage.sprite = burnAnimation.frames[0];

            // Calculate FPS so the animation spans exactly burnDuration seconds.
            // e.g. 6 frames over 10 s = 0.6 fps; 5 frames over 8 s = 0.625 fps.
            float syncedFps = burnAnimation.frames.Length / burnDuration;

            FrameAnimator animator = overlayGO.AddComponent<FrameAnimator>();
            bool done = false;
            animator.Play(burnAnimation.frames, syncedFps, loop: false, onComplete: () => done = true);
            yield return new WaitUntil(() => done);

            // Animation finished — stop animating, hold last frame.
            animator.enabled = false;
            // overlayGO intentionally NOT destroyed — last frame stays visible.

            // Now hide the cooked sprite so only the burn frame shows.
            if (_image != null) _image.enabled = false;
        }
        else
        {
            // No animation assigned — just hide the cooked sprite immediately
            // so the player can see something changed (solid black fallback).
            if (_image != null)
            {
                _image.color = new Color(0.2f, 0.1f, 0f, 1f); // dark burnt colour
                _image.enabled = true;
            }
            Debug.LogWarning($"[BurnerGrill] '{name}': burnAnimation not assigned — using colour fallback.");
        }

        // ── Free the plate slot so it doesn't stay locked ────────────────────
        if (dg != null && dg.owningPlate != null)
            dg.owningPlate.FreeSlotOf(dg);

        // ── Lift burned grill to canvas root so DraggableBurnedGrill ─────────
        //    can always receive pointer events.
        Canvas rootCanvas = GetComponentInParent<Canvas>();
        if (rootCanvas == null) rootCanvas = FindObjectOfType<Canvas>();

        if (rootCanvas != null)
        {
            RectTransform myRect = GetComponent<RectTransform>();
            Vector3 worldPos = myRect.position;

            myRect.SetParent(rootCanvas.transform, worldPositionStays: true);
            myRect.position = worldPos;
        }

        // ── Re-enable the root Image as a raycast surface ─────────────────────
        // Even though it's visually hidden (disabled above after animation),
        // the RectTransform needs a live Graphic for pointer events to land.
        // We use an invisible but raycast-enabled Image on the root.
        if (_image != null)
        {
            _image.enabled = true;
            _image.raycastTarget = true;
            _image.color = Color.clear; // invisible — BurnOverlay child shows the visual
        }

        // ── Re-enable CanvasGroup so pointer events reach the object ─────────
        CanvasGroup cg = GetComponent<CanvasGroup>();
        if (cg != null)
        {
            cg.blocksRaycasts = true;
            cg.interactable = true;
            cg.alpha = 1f;
        }

        // ── Enable burned dragging ────────────────────────────────────────────
        DraggableBurnedGrill db = GetComponent<DraggableBurnedGrill>();
        if (db != null) db.enabled = true;

        PigGrillTutorialManager.Instance?.NotifyFoodBurned();
        Debug.Log($"[BurnerGrill] '{name}' burned — last burn frame held. Ready to drag to dustbin.");
    }
}