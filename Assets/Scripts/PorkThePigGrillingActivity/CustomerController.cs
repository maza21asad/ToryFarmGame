////using UnityEngine;
////using UnityEngine.UI;
////using System;
////using System.Collections;

/////// <summary>
/////// Attach to each customer's root Image GameObject (alongside FrameAnimator).
///////
/////// owningPlate is assigned at runtime by CustomerManager — no Inspector wiring needed.
/////// StartWalking() receives the spawn and target RectTransforms from CustomerManager
/////// each time the customer is recycled into a slot.
///////
/////// FIX — "customer does not move visually"
///////   The old code resolved _canvasRect in Start(), but customers start SetActive(false),
///////   so Start() never ran before StartWalking() was called. WorldToCanvasAnchor()
///////   then returned raw world-space values as if they were canvas-local, placing the
///////   customer at a completely wrong anchoredPosition.
///////   Fix: resolve _canvasRect at the top of StartWalking() every time, so it is always
///////   valid regardless of whether Start() has run.
/////// </summary>
////public class CustomerController : MonoBehaviour
////{
////    // ── Inspector ─────────────────────────────────────────────────────────────

////    [Header("Frame Animations")]
////    public FrameAnimation walkAnimation;
////    public FrameAnimation idleAnimation;
////    public FrameAnimation eatAnimation;

////    [Tooltip("Canvas-units per second.")]
////    public float walkSpeed = 400f;

////    [Header("Mouth Drop Zone")]
////    public RectTransform mouthPoint;

////    [Tooltip("Optional Image shown when a cooked grill is hovering near the mouth.")]
////    public Image mouthHintImage;

////    [Header("Order Chat Bubble")]
////    [Tooltip("Set INACTIVE in the Editor — shown on arrival.")]
////    public OrderChathead orderChathead;

////    [Header("Leave Delay")]
////    [Tooltip("Seconds the customer waits after all orders are fulfilled before walking away.")]
////    public float leaveDelay = 1f;

////    [Header("Satisfaction Sound")]
////    [Tooltip("SFX key played when this customer's last order is fulfilled. Set to Satisfy1 / Satisfy2 / Satisfy3 per customer.")]
////    [SerializeField] private string satisfySound = "Satisfy1";

////    // Set by CustomerManager each spawn — NOT wired in the Inspector.
////    [HideInInspector] public PlateController owningPlate;

////    // ── Events ────────────────────────────────────────────────────────────────

////    public event Action OnArrived;
////    public event Action<CustomerController> OnLeft;

////    // ── Public state ──────────────────────────────────────────────────────────

////    public bool HasArrived { get; private set; }

////    // ── Private ───────────────────────────────────────────────────────────────

////    private FrameAnimator _anim;
////    private RectTransform _rect;
////    private RectTransform _canvasRect;

////    private Vector2 _spawnAnchor;
////    private Vector2 _targetAnchor;

////    // ── Unity ─────────────────────────────────────────────────────────────────

////    private void Awake()
////    {
////        _rect = GetComponent<RectTransform>();

////        _anim = GetComponent<FrameAnimator>();
////        if (_anim == null) _anim = gameObject.AddComponent<FrameAnimator>();

////        if (orderChathead != null)
////        {
////            orderChathead.gameObject.SetActive(false);

////            // Unity / DOTween can auto-add a CanvasGroup to the chathead at runtime.
////            // DOTween does this the first time DOFade is called on any UI object.
////            // That CanvasGroup defaults blocksRaycasts = true, which silently blocks
////            // clicks on the OK button even while the chathead is hidden — because a
////            // CanvasGroup affects raycasts even on inactive children of a Canvas.
////            //
////            // Fix: destroy the CanvasGroup entirely so it can never block input.
////            // We disable blocksRaycasts first as an immediate safeguard in case
////            // Destroy is deferred to end-of-frame.
////            CanvasGroup chatheadCG = orderChathead.GetComponent<CanvasGroup>();
////            if (chatheadCG != null)
////            {
////                chatheadCG.blocksRaycasts = false;
////                chatheadCG.interactable = false;
////                Destroy(chatheadCG);
////            }
////        }
////        if (mouthHintImage != null) mouthHintImage.enabled = false;
////    }

////    private void Start() { } // intentionally empty

////    // OnEnable fires every time SetActive(true) is called — unlike Start() which
////    // only fires once. CustomerManager.Start() calls SetActive(false) on all
////    // customers, consuming their Start(). When SpawnIntoSlot later calls
////    // SetActive(true), Start() is already spent, so without this fix customers
////    // never enter _active and GetNearestMouth always returns null.
////    private void OnEnable() => CustomerManager.Instance?.Register(this);
////    private void OnDisable() => CustomerManager.Instance?.Unregister(this);
////    private void OnDestroy() => CustomerManager.Instance?.Unregister(this);

////    // ── Walk In ───────────────────────────────────────────────────────────────

////    /// <summary>
////    /// Called by CustomerManager with the slot's spawn and target points.
////    /// FIX: _canvasRect is resolved HERE (not in Start) so it is always valid,
////    /// even when the customer was SetActive(false) and Start() never ran.
////    /// </summary>
////    public void StartWalking(RectTransform spawnPt, RectTransform targetPt)
////    {
////        // ── Resolve canvas rect every time ────────────────────────────────────
////        // We cannot rely on Start() because the customer may have been inactive.
////        Canvas rootCanvas = GetComponentInParent<Canvas>();
////        if (rootCanvas == null) rootCanvas = FindObjectOfType<Canvas>();
////        _canvasRect = rootCanvas != null ? rootCanvas.GetComponent<RectTransform>() : null;

////        // ── Convert world positions → canvas-local anchored positions ─────────
////        _spawnAnchor = WorldToCanvasAnchor(spawnPt != null ? spawnPt.position : transform.position);
////        _targetAnchor = WorldToCanvasAnchor(targetPt != null ? targetPt.position : transform.position);

////        // Teleport to spawn, then walk to target.
////        _rect.anchoredPosition = _spawnAnchor;
////        HasArrived = false;
////        FaceForward();

////        if (walkAnimation?.frames?.Length > 0)
////            _anim.Play(walkAnimation, loop: true);
////        SoundManager.Instance.PlaySFXLoop("Walking");

////        StartCoroutine(WalkRoutine(_targetAnchor, onComplete: Arrived));
////    }

////    private IEnumerator WalkRoutine(Vector2 destination, Action onComplete)
////    {
////        while (Vector2.Distance(_rect.anchoredPosition, destination) > 1f)
////        {
////            _rect.anchoredPosition = Vector2.MoveTowards(
////                _rect.anchoredPosition, destination, walkSpeed * Time.deltaTime);
////            yield return null;
////        }
////        _rect.anchoredPosition = destination;
////        SoundManager.Instance.StopSFXLoop();
////        onComplete?.Invoke();
////    }

////    private void Arrived()
////    {
////        HasArrived = true;
////        FaceForward();

////        if (idleAnimation?.frames?.Length > 0)
////            _anim.Play(idleAnimation, loop: true);

////        if (orderChathead != null)
////        {
////            orderChathead.gameObject.SetActive(true);
////            orderChathead.RandomizeOrder();
////        }

////        OnArrived?.Invoke();
////        Debug.Log($"[CustomerController] '{name}' arrived at counter (plate: {owningPlate?.plateLabel ?? "none"}).");
////    }

////    // ── Order Fulfillment ─────────────────────────────────────────────────────

////    /// <summary>
////    /// Called by DraggableGrill on mouth-drop.
////    /// Returns true only when:
////    ///   (a) the grill came from this customer's owning plate, AND
////    ///   (b) the grillType matches an open order slot.
////    /// Returns false otherwise — the grill snaps back to its plate.
////    /// </summary>
////    public bool TryFulfillOrder(string grillType, PlateController fromPlate)
////    {
////        if (orderChathead == null) return false;

////        // Reject food that came from the wrong plate.
////        if (owningPlate != null && fromPlate != owningPlate)
////        {
////            Debug.Log($"[CustomerController] '{name}' refuses food from '{fromPlate?.plateLabel}'" +
////                      $" — only accepts from '{owningPlate?.plateLabel}'.");
////            return false;
////        }

////        bool matched = orderChathead.FulfillSlot(grillType);

////        if (matched)
////        {
////            PlayEatAnimation();
////            if (orderChathead.IsComplete()) OnAllFulfilled();
////        }
////        else
////        {
////            Debug.Log($"[CustomerController] '{name}' does not need '{grillType}' right now.");
////        }

////        return matched;
////    }

////    // ── Animations ────────────────────────────────────────────────────────────

////    public void PlayEatAnimation()
////    {
////        SoundManager.Instance.PlaySFX("Eating");
////        if (eatAnimation?.frames?.Length > 0)
////            _anim.Play(eatAnimation, loop: false, onComplete: ResumeIdle);
////        else
////            ResumeIdle();
////    }

////    private void ResumeIdle()
////    {
////        if (idleAnimation?.frames?.Length > 0)
////            _anim.Play(idleAnimation, loop: true);
////    }

////    // ── Mouth Hint ────────────────────────────────────────────────────────────

////    public void SetMouthHint(bool active)
////    {
////        if (mouthHintImage != null) mouthHintImage.enabled = active;
////    }

////    // ── Leave ─────────────────────────────────────────────────────────────────

////    private void OnAllFulfilled()
////    {
////        Debug.Log($"[CustomerController] '{name}' — all orders fulfilled! Walking away.");
////        SoundManager.Instance.PlaySFX("PigLaugh");
////        SoundManager.Instance.PlaySFX(satisfySound);

////        if (orderChathead != null) orderChathead.gameObject.SetActive(false);
////        if (mouthHintImage != null) mouthHintImage.enabled = false;

////        StartCoroutine(LeaveRoutine());
////    }

////    private IEnumerator LeaveRoutine()
////    {
////        yield return new WaitForSeconds(leaveDelay);

////        HasArrived = false;
////        FaceBackward();

////        if (walkAnimation?.frames?.Length > 0)
////            _anim.Play(walkAnimation, loop: true);
////            SoundManager.Instance.PlaySFXLoop("Walking");

////        yield return StartCoroutine(WalkRoutine(_spawnAnchor, onComplete: null));

////        gameObject.SetActive(false);
////        owningPlate = null;

////        OnLeft?.Invoke(this);
////        OnLeft = null; // CustomerManager re-subscribes on each spawn
////    }

////    // ── Helpers ───────────────────────────────────────────────────────────────

////    /// <summary>
////    /// Converts a world-space position to an anchoredPosition on the root Canvas.
////    /// Safe to call even when _canvasRect was just resolved above.
////    /// </summary>
////    private Vector2 WorldToCanvasAnchor(Vector3 worldPos)
////    {
////        if (_canvasRect == null)
////        {
////            // Fallback — shouldn't happen after the fix, but keeps us from crashing.
////            return new Vector2(worldPos.x, worldPos.y);
////        }

////        Camera uiCam = null;
////        Canvas c = _canvasRect.GetComponent<Canvas>();
////        if (c != null && c.renderMode != RenderMode.ScreenSpaceOverlay)
////            uiCam = c.worldCamera;

////        Vector2 screenPoint = RectTransformUtility.WorldToScreenPoint(uiCam, worldPos);

////        RectTransformUtility.ScreenPointToLocalPointInRectangle(
////            _canvasRect, screenPoint, uiCam, out Vector2 local);

////        return local;
////    }

////    private void FaceForward() => _rect.localEulerAngles = Vector3.zero;
////    private void FaceBackward() => _rect.localEulerAngles = new Vector3(0f, 180f, 0f);
////}


//using UnityEngine;
//using UnityEngine.UI;
//using System;
//using System.Collections;

//public class CustomerController : MonoBehaviour
//{
//    // ── Inspector ─────────────────────────────────────────────────────────────

//    [Header("Frame Animations")]
//    public FrameAnimation walkAnimation;
//    public FrameAnimation idleAnimation;
//    public FrameAnimation eatAnimation;

//    [Tooltip("Canvas-units per second.")]
//    public float walkSpeed = 400f;

//    [Header("Mouth Drop Zone")]
//    public RectTransform mouthPoint;

//    [Tooltip("Optional Image shown when a cooked grill is hovering near the mouth.")]
//    public Image mouthHintImage;

//    [Header("Order Chat Bubble")]
//    [Tooltip("Set INACTIVE in the Editor — shown on arrival.")]
//    public OrderChathead orderChathead;

//    [Header("Leave Delay")]
//    [Tooltip("Seconds the customer waits after all orders are fulfilled before walking away.")]
//    public float leaveDelay = 1f;

//    [Header("Satisfaction Sound")]
//    [Tooltip("SFX key played when this customer's last order is fulfilled. Set to Satisfy1 / Satisfy2 / Satisfy3 per customer.")]
//    [SerializeField] private string satisfySound = "Satisfy1";

//    // Set by CustomerManager each spawn — NOT wired in the Inspector.
//    [HideInInspector] public PlateController owningPlate;

//    // ── Events ────────────────────────────────────────────────────────────────

//    public event Action OnArrived;
//    public event Action<CustomerController> OnServed;
//    public event Action<CustomerController> OnLeft;

//    // ── Public state ──────────────────────────────────────────────────────────

//    public bool HasArrived { get; private set; }

//    // ── Private ───────────────────────────────────────────────────────────────

//    private FrameAnimator _anim;
//    private RectTransform _rect;
//    private RectTransform _canvasRect;

//    private Vector2 _spawnAnchor;
//    private Vector2 _targetAnchor;

//    // ── Unity ─────────────────────────────────────────────────────────────────

//    private void Awake()
//    {
//        _rect = GetComponent<RectTransform>();

//        _anim = GetComponent<FrameAnimator>();
//        if (_anim == null) _anim = gameObject.AddComponent<FrameAnimator>();

//        if (orderChathead != null)
//        {
//            orderChathead.gameObject.SetActive(false);

//            CanvasGroup chatheadCG = orderChathead.GetComponent<CanvasGroup>();
//            if (chatheadCG != null)
//            {
//                chatheadCG.blocksRaycasts = false;
//                chatheadCG.interactable = false;
//                Destroy(chatheadCG);
//            }
//        }
//        if (mouthHintImage != null) mouthHintImage.enabled = false;
//    }

//    private void Start() { }

//    private void OnEnable() => CustomerManager.Instance?.Register(this);
//    private void OnDisable() => CustomerManager.Instance?.Unregister(this);
//    private void OnDestroy() => CustomerManager.Instance?.Unregister(this);

//    // ── Walk In ───────────────────────────────────────────────────────────────

//    public void StartWalking(RectTransform spawnPt, RectTransform targetPt)
//    {
//        Canvas rootCanvas = GetComponentInParent<Canvas>();
//        if (rootCanvas == null) rootCanvas = FindObjectOfType<Canvas>();
//        _canvasRect = rootCanvas != null ? rootCanvas.GetComponent<RectTransform>() : null;

//        _spawnAnchor = WorldToCanvasAnchor(spawnPt != null ? spawnPt.position : transform.position);
//        _targetAnchor = WorldToCanvasAnchor(targetPt != null ? targetPt.position : transform.position);

//        _rect.anchoredPosition = _spawnAnchor;
//        HasArrived = false;
//        FaceForward();

//        if (walkAnimation?.frames?.Length > 0)
//            _anim.Play(walkAnimation, loop: true);

//        CustomerManager.Instance.OnCustomerStartWalk();
//        SoundManager.Instance.PlaySFXLoop("Walking");

//        StartCoroutine(WalkRoutine(_targetAnchor, onComplete: Arrived));
//    }

//    private IEnumerator WalkRoutine(Vector2 destination, Action onComplete)
//    {
//        while (Vector2.Distance(_rect.anchoredPosition, destination) > 1f)
//        {
//            _rect.anchoredPosition = Vector2.MoveTowards(
//                _rect.anchoredPosition, destination, walkSpeed * Time.deltaTime);
//            yield return null;
//        }
//        _rect.anchoredPosition = destination;

//        CustomerManager.Instance.OnCustomerStopWalk();
//        if (CustomerManager.Instance.WalkingCount == 0)
//            SoundManager.Instance.StopSFXLoop();

//        onComplete?.Invoke();
//    }

//    private void Arrived()
//    {
//        HasArrived = true;
//        FaceForward();

//        if (idleAnimation?.frames?.Length > 0)
//            _anim.Play(idleAnimation, loop: true);

//        if (orderChathead != null)
//        {
//            orderChathead.gameObject.SetActive(true);
//            orderChathead.RandomizeOrder();
//        }

//        OnArrived?.Invoke();
//        Debug.Log($"[CustomerController] '{name}' arrived at counter (plate: {owningPlate?.plateLabel ?? "none"}).");
//    }

//    // ── Order Fulfillment ─────────────────────────────────────────────────────

//    public bool TryFulfillOrder(string grillType, PlateController fromPlate)
//    {
//        if (orderChathead == null) return false;

//        if (owningPlate != null && fromPlate != owningPlate)
//        {
//            Debug.Log($"[CustomerController] '{name}' refuses food from '{fromPlate?.plateLabel}'" +
//                      $" — only accepts from '{owningPlate?.plateLabel}'.");
//            return false;
//        }

//        bool matched = orderChathead.FulfillSlot(grillType);

//        if (matched)
//        {
//            PlayEatAnimation();
//            if (orderChathead.IsComplete()) OnAllFulfilled();
//        }
//        else
//        {
//            Debug.Log($"[CustomerController] '{name}' does not need '{grillType}' right now.");
//        }

//        return matched;
//    }

//    // ── Animations ────────────────────────────────────────────────────────────

//    public void PlayEatAnimation()
//    {
//        SoundManager.Instance.PlaySFX("Eating");
//        if (eatAnimation?.frames?.Length > 0)
//            _anim.Play(eatAnimation, loop: false, onComplete: ResumeIdle);
//        else
//            ResumeIdle();
//    }

//    private void ResumeIdle()
//    {
//        if (idleAnimation?.frames?.Length > 0)
//            _anim.Play(idleAnimation, loop: true);
//    }

//    // ── Mouth Hint ────────────────────────────────────────────────────────────

//    public void SetMouthHint(bool active)
//    {
//        if (mouthHintImage != null) mouthHintImage.enabled = active;
//    }

//    // ── Leave ─────────────────────────────────────────────────────────────────

//    private void OnAllFulfilled()
//    {
//        Debug.Log($"[CustomerController] '{name}' — all orders fulfilled! Walking away.");
//        SoundManager.Instance.PlaySFX("PigLaugh");
//        SoundManager.Instance.PlaySFX(satisfySound);

//        if (orderChathead != null) orderChathead.gameObject.SetActive(false);
//        if (mouthHintImage != null) mouthHintImage.enabled = false;

//        // Notify manager immediately — counts toward the task even before the walk-off finishes.
//        OnServed?.Invoke(this);

//        StartCoroutine(LeaveRoutine());
//    }

//    private IEnumerator LeaveRoutine()
//    {
//        yield return new WaitForSeconds(leaveDelay);

//        HasArrived = false;
//        FaceBackward();

//        if (walkAnimation?.frames?.Length > 0)
//            _anim.Play(walkAnimation, loop: true);

//        CustomerManager.Instance.OnCustomerStartWalk();
//        SoundManager.Instance.PlaySFXLoop("Walking");

//        yield return StartCoroutine(WalkRoutine(_spawnAnchor, onComplete: null));

//        gameObject.SetActive(false);
//        owningPlate = null;

//        OnLeft?.Invoke(this);
//        OnLeft = null;
//    }

//    // ── Helpers ───────────────────────────────────────────────────────────────

//    private Vector2 WorldToCanvasAnchor(Vector3 worldPos)
//    {
//        if (_canvasRect == null)
//            return new Vector2(worldPos.x, worldPos.y);

//        Camera uiCam = null;
//        Canvas c = _canvasRect.GetComponent<Canvas>();
//        if (c != null && c.renderMode != RenderMode.ScreenSpaceOverlay)
//            uiCam = c.worldCamera;

//        Vector2 screenPoint = RectTransformUtility.WorldToScreenPoint(uiCam, worldPos);

//        RectTransformUtility.ScreenPointToLocalPointInRectangle(
//            _canvasRect, screenPoint, uiCam, out Vector2 local);

//        return local;
//    }

//    private void FaceForward() => _rect.localEulerAngles = Vector3.zero;
//    private void FaceBackward() => _rect.localEulerAngles = new Vector3(0f, 180f, 0f);


//}

//using UnityEngine;
//using UnityEngine.UI;
//using System;
//using System.Collections;

///// <summary>
///// Attach to each customer's root Image GameObject (alongside FrameAnimator).
/////
///// owningPlate is assigned at runtime by CustomerManager — no Inspector wiring needed.
///// StartWalking() receives the spawn and target RectTransforms from CustomerManager
///// each time the customer is recycled into a slot.
/////
///// FIX — "customer does not move visually"
/////   The old code resolved _canvasRect in Start(), but customers start SetActive(false),
/////   so Start() never ran before StartWalking() was called. WorldToCanvasAnchor()
/////   then returned raw world-space values as if they were canvas-local, placing the
/////   customer at a completely wrong anchoredPosition.
/////   Fix: resolve _canvasRect at the top of StartWalking() every time, so it is always
/////   valid regardless of whether Start() has run.
///// </summary>
//public class CustomerController : MonoBehaviour
//{
//    // ── Inspector ─────────────────────────────────────────────────────────────

//    [Header("Frame Animations")]
//    public FrameAnimation walkAnimation;
//    public FrameAnimation idleAnimation;
//    public FrameAnimation eatAnimation;

//    [Tooltip("Canvas-units per second.")]
//    public float walkSpeed = 400f;

//    [Header("Mouth Drop Zone")]
//    public RectTransform mouthPoint;

//    [Tooltip("Optional Image shown when a cooked grill is hovering near the mouth.")]
//    public Image mouthHintImage;

//    [Header("Order Chat Bubble")]
//    [Tooltip("Set INACTIVE in the Editor — shown on arrival.")]
//    public OrderChathead orderChathead;

//    [Header("Leave Delay")]
//    [Tooltip("Seconds the customer waits after all orders are fulfilled before walking away.")]
//    public float leaveDelay = 1f;

//    [Header("Satisfaction Sound")]
//    [Tooltip("SFX key played when this customer's last order is fulfilled. Set to Satisfy1 / Satisfy2 / Satisfy3 per customer.")]
//    [SerializeField] private string satisfySound = "Satisfy1";

//    // Set by CustomerManager each spawn — NOT wired in the Inspector.
//    [HideInInspector] public PlateController owningPlate;

//    // ── Events ────────────────────────────────────────────────────────────────

//    public event Action OnArrived;
//    public event Action<CustomerController> OnLeft;

//    // ── Public state ──────────────────────────────────────────────────────────

//    public bool HasArrived { get; private set; }

//    // ── Private ───────────────────────────────────────────────────────────────

//    private FrameAnimator _anim;
//    private RectTransform _rect;
//    private RectTransform _canvasRect;

//    private Vector2 _spawnAnchor;
//    private Vector2 _targetAnchor;

//    // ── Unity ─────────────────────────────────────────────────────────────────

//    private void Awake()
//    {
//        _rect = GetComponent<RectTransform>();

//        _anim = GetComponent<FrameAnimator>();
//        if (_anim == null) _anim = gameObject.AddComponent<FrameAnimator>();

//        if (orderChathead != null)
//        {
//            orderChathead.gameObject.SetActive(false);

//            // Unity / DOTween can auto-add a CanvasGroup to the chathead at runtime.
//            // DOTween does this the first time DOFade is called on any UI object.
//            // That CanvasGroup defaults blocksRaycasts = true, which silently blocks
//            // clicks on the OK button even while the chathead is hidden — because a
//            // CanvasGroup affects raycasts even on inactive children of a Canvas.
//            //
//            // Fix: destroy the CanvasGroup entirely so it can never block input.
//            // We disable blocksRaycasts first as an immediate safeguard in case
//            // Destroy is deferred to end-of-frame.
//            CanvasGroup chatheadCG = orderChathead.GetComponent<CanvasGroup>();
//            if (chatheadCG != null)
//            {
//                chatheadCG.blocksRaycasts = false;
//                chatheadCG.interactable = false;
//                Destroy(chatheadCG);
//            }
//        }
//        if (mouthHintImage != null) mouthHintImage.enabled = false;
//    }

//    private void Start() { } // intentionally empty

//    // OnEnable fires every time SetActive(true) is called — unlike Start() which
//    // only fires once. CustomerManager.Start() calls SetActive(false) on all
//    // customers, consuming their Start(). When SpawnIntoSlot later calls
//    // SetActive(true), Start() is already spent, so without this fix customers
//    // never enter _active and GetNearestMouth always returns null.
//    private void OnEnable() => CustomerManager.Instance?.Register(this);
//    private void OnDisable() => CustomerManager.Instance?.Unregister(this);
//    private void OnDestroy() => CustomerManager.Instance?.Unregister(this);

//    // ── Walk In ───────────────────────────────────────────────────────────────

//    /// <summary>
//    /// Called by CustomerManager with the slot's spawn and target points.
//    /// FIX: _canvasRect is resolved HERE (not in Start) so it is always valid,
//    /// even when the customer was SetActive(false) and Start() never ran.
//    /// </summary>
//    public void StartWalking(RectTransform spawnPt, RectTransform targetPt)
//    {
//        // ── Resolve canvas rect every time ────────────────────────────────────
//        // We cannot rely on Start() because the customer may have been inactive.
//        Canvas rootCanvas = GetComponentInParent<Canvas>();
//        if (rootCanvas == null) rootCanvas = FindObjectOfType<Canvas>();
//        _canvasRect = rootCanvas != null ? rootCanvas.GetComponent<RectTransform>() : null;

//        // ── Convert world positions → canvas-local anchored positions ─────────
//        _spawnAnchor = WorldToCanvasAnchor(spawnPt != null ? spawnPt.position : transform.position);
//        _targetAnchor = WorldToCanvasAnchor(targetPt != null ? targetPt.position : transform.position);

//        // Teleport to spawn, then walk to target.
//        _rect.anchoredPosition = _spawnAnchor;
//        HasArrived = false;
//        FaceForward();

//        if (walkAnimation?.frames?.Length > 0)
//            _anim.Play(walkAnimation, loop: true);
//        SoundManager.Instance.PlaySFXLoop("Walking");

//        StartCoroutine(WalkRoutine(_targetAnchor, onComplete: Arrived));
//    }

//    private IEnumerator WalkRoutine(Vector2 destination, Action onComplete)
//    {
//        while (Vector2.Distance(_rect.anchoredPosition, destination) > 1f)
//        {
//            _rect.anchoredPosition = Vector2.MoveTowards(
//                _rect.anchoredPosition, destination, walkSpeed * Time.deltaTime);
//            yield return null;
//        }
//        _rect.anchoredPosition = destination;
//        SoundManager.Instance.StopSFXLoop();
//        onComplete?.Invoke();
//    }

//    private void Arrived()
//    {
//        HasArrived = true;
//        FaceForward();

//        if (idleAnimation?.frames?.Length > 0)
//            _anim.Play(idleAnimation, loop: true);

//        if (orderChathead != null)
//        {
//            orderChathead.gameObject.SetActive(true);
//            orderChathead.RandomizeOrder();
//        }

//        OnArrived?.Invoke();
//        Debug.Log($"[CustomerController] '{name}' arrived at counter (plate: {owningPlate?.plateLabel ?? "none"}).");
//    }

//    // ── Order Fulfillment ─────────────────────────────────────────────────────

//    /// <summary>
//    /// Called by DraggableGrill on mouth-drop.
//    /// Returns true only when:
//    ///   (a) the grill came from this customer's owning plate, AND
//    ///   (b) the grillType matches an open order slot.
//    /// Returns false otherwise — the grill snaps back to its plate.
//    /// </summary>
//    public bool TryFulfillOrder(string grillType, PlateController fromPlate)
//    {
//        if (orderChathead == null) return false;

//        // Reject food that came from the wrong plate.
//        if (owningPlate != null && fromPlate != owningPlate)
//        {
//            Debug.Log($"[CustomerController] '{name}' refuses food from '{fromPlate?.plateLabel}'" +
//                      $" — only accepts from '{owningPlate?.plateLabel}'.");
//            return false;
//        }

//        bool matched = orderChathead.FulfillSlot(grillType);

//        if (matched)
//        {
//            PlayEatAnimation();
//            if (orderChathead.IsComplete()) OnAllFulfilled();
//        }
//        else
//        {
//            Debug.Log($"[CustomerController] '{name}' does not need '{grillType}' right now.");
//        }

//        return matched;
//    }

//    // ── Animations ────────────────────────────────────────────────────────────

//    public void PlayEatAnimation()
//    {
//        SoundManager.Instance.PlaySFX("Eating");
//        if (eatAnimation?.frames?.Length > 0)
//            _anim.Play(eatAnimation, loop: false, onComplete: ResumeIdle);
//        else
//            ResumeIdle();
//    }

//    private void ResumeIdle()
//    {
//        if (idleAnimation?.frames?.Length > 0)
//            _anim.Play(idleAnimation, loop: true);
//    }

//    // ── Mouth Hint ────────────────────────────────────────────────────────────

//    public void SetMouthHint(bool active)
//    {
//        if (mouthHintImage != null) mouthHintImage.enabled = active;
//    }

//    // ── Leave ─────────────────────────────────────────────────────────────────

//    private void OnAllFulfilled()
//    {
//        Debug.Log($"[CustomerController] '{name}' — all orders fulfilled! Walking away.");
//        SoundManager.Instance.PlaySFX("PigLaugh");
//        SoundManager.Instance.PlaySFX(satisfySound);

//        if (orderChathead != null) orderChathead.gameObject.SetActive(false);
//        if (mouthHintImage != null) mouthHintImage.enabled = false;

//        StartCoroutine(LeaveRoutine());
//    }

//    private IEnumerator LeaveRoutine()
//    {
//        yield return new WaitForSeconds(leaveDelay);

//        HasArrived = false;
//        FaceBackward();

//        if (walkAnimation?.frames?.Length > 0)
//            _anim.Play(walkAnimation, loop: true);
//            SoundManager.Instance.PlaySFXLoop("Walking");

//        yield return StartCoroutine(WalkRoutine(_spawnAnchor, onComplete: null));

//        gameObject.SetActive(false);
//        owningPlate = null;

//        OnLeft?.Invoke(this);
//        OnLeft = null; // CustomerManager re-subscribes on each spawn
//    }

//    // ── Helpers ───────────────────────────────────────────────────────────────

//    /// <summary>
//    /// Converts a world-space position to an anchoredPosition on the root Canvas.
//    /// Safe to call even when _canvasRect was just resolved above.
//    /// </summary>
//    private Vector2 WorldToCanvasAnchor(Vector3 worldPos)
//    {
//        if (_canvasRect == null)
//        {
//            // Fallback — shouldn't happen after the fix, but keeps us from crashing.
//            return new Vector2(worldPos.x, worldPos.y);
//        }

//        Camera uiCam = null;
//        Canvas c = _canvasRect.GetComponent<Canvas>();
//        if (c != null && c.renderMode != RenderMode.ScreenSpaceOverlay)
//            uiCam = c.worldCamera;

//        Vector2 screenPoint = RectTransformUtility.WorldToScreenPoint(uiCam, worldPos);

//        RectTransformUtility.ScreenPointToLocalPointInRectangle(
//            _canvasRect, screenPoint, uiCam, out Vector2 local);

//        return local;
//    }

//    private void FaceForward() => _rect.localEulerAngles = Vector3.zero;
//    private void FaceBackward() => _rect.localEulerAngles = new Vector3(0f, 180f, 0f);
//}


using UnityEngine;
using UnityEngine.UI;
using System;
using System.Collections;

public class CustomerController : MonoBehaviour
{
    // ── Inspector ─────────────────────────────────────────────────────────────

    [Header("Frame Animations")]
    public FrameAnimation walkAnimation;
    public FrameAnimation idleAnimation;
    public FrameAnimation eatAnimation;

    [Tooltip("Canvas-units per second.")]
    public float walkSpeed = 400f;

    [Header("Mouth Drop Zone")]
    public RectTransform mouthPoint;

    [Tooltip("Optional Image shown when a cooked grill is hovering near the mouth.")]
    public Image mouthHintImage;

    [Header("Order Chat Bubble")]
    [Tooltip("Set INACTIVE in the Editor — shown on arrival.")]
    public OrderChathead orderChathead;

    [Header("Leave Delay")]
    [Tooltip("Seconds the customer waits after all orders are fulfilled before walking away.")]
    public float leaveDelay = 1f;

    [Header("Satisfaction Sound")]
    [Tooltip("SFX key played when this customer's last order is fulfilled. Set to Satisfy1 / Satisfy2 / Satisfy3 per customer.")]
    [SerializeField] private string satisfySound = "Satisfy1";

    // Set by CustomerManager each spawn — NOT wired in the Inspector.
    [HideInInspector] public PlateController owningPlate;

    // ── Events ────────────────────────────────────────────────────────────────

    public event Action OnArrived;
    public event Action<CustomerController> OnServed;
    public event Action<CustomerController> OnLeft;

    // ── Public state ──────────────────────────────────────────────────────────

    public bool HasArrived { get; private set; }

    // ── Private ───────────────────────────────────────────────────────────────

    private FrameAnimator _anim;
    private RectTransform _rect;
    private RectTransform _canvasRect;
    private RectTransform _parentRect;

    private Vector2 _spawnAnchor;
    private Vector2 _targetAnchor;

    // ── Unity ─────────────────────────────────────────────────────────────────

    private void Awake()
    {
        _rect = GetComponent<RectTransform>();

        _anim = GetComponent<FrameAnimator>();
        if (_anim == null) _anim = gameObject.AddComponent<FrameAnimator>();

        if (orderChathead != null)
        {
            orderChathead.gameObject.SetActive(false);

            CanvasGroup chatheadCG = orderChathead.GetComponent<CanvasGroup>();
            if (chatheadCG != null)
            {
                chatheadCG.blocksRaycasts = false;
                chatheadCG.interactable = false;
                Destroy(chatheadCG);
            }
        }
        if (mouthHintImage != null) mouthHintImage.enabled = false;
    }

    private void Start() { }

    private void OnEnable() => CustomerManager.Instance?.Register(this);
    private void OnDisable() => CustomerManager.Instance?.Unregister(this);
    private void OnDestroy() => CustomerManager.Instance?.Unregister(this);

    // ── Walk In ───────────────────────────────────────────────────────────────

    public void StartWalking(RectTransform spawnPt, RectTransform targetPt)
    {
        Canvas rootCanvas = GetComponentInParent<Canvas>();
        if (rootCanvas == null) rootCanvas = FindObjectOfType<Canvas>();
        _canvasRect = rootCanvas != null ? rootCanvas.GetComponent<RectTransform>() : null;

        // Anchored positions are always relative to the immediate parent
        // (Characters), not the root Canvas — resolve that here every time
        // in case this customer is ever reparented.
        _parentRect = _rect.parent as RectTransform;

        _spawnAnchor = WorldToAnchoredPosition(spawnPt != null ? spawnPt.position : transform.position);
        _targetAnchor = WorldToAnchoredPosition(targetPt != null ? targetPt.position : transform.position);

        _rect.anchoredPosition = _spawnAnchor;
        HasArrived = false;
        FaceForward();

        if (walkAnimation?.frames?.Length > 0)
            _anim.Play(walkAnimation, loop: true);

        CustomerManager.Instance.OnCustomerStartWalk();
        SoundManager.Instance.PlaySFXLoop("Walking");

        StartCoroutine(WalkRoutine(_targetAnchor, onComplete: Arrived));
    }

    private IEnumerator WalkRoutine(Vector2 destination, Action onComplete)
    {
        while (Vector2.Distance(_rect.anchoredPosition, destination) > 1f)
        {
            _rect.anchoredPosition = Vector2.MoveTowards(
                _rect.anchoredPosition, destination, walkSpeed * Time.deltaTime);
            yield return null;
        }
        _rect.anchoredPosition = destination;

        CustomerManager.Instance.OnCustomerStopWalk();
        if (CustomerManager.Instance.WalkingCount == 0)
            SoundManager.Instance.StopSFXLoop();

        onComplete?.Invoke();
    }

    private void Arrived()
    {
        HasArrived = true;
        FaceForward();

        if (idleAnimation?.frames?.Length > 0)
            _anim.Play(idleAnimation, loop: true);

        if (orderChathead != null)
        {
            orderChathead.gameObject.SetActive(true);
            orderChathead.RandomizeOrder();
        }

        OnArrived?.Invoke();
        Debug.Log($"[CustomerController] '{name}' arrived at counter (plate: {owningPlate?.plateLabel ?? "none"}).");
    }

    // ── Order Fulfillment ─────────────────────────────────────────────────────

    public bool TryFulfillOrder(string grillType, PlateController fromPlate)
    {
        if (orderChathead == null) return false;

        if (owningPlate != null && fromPlate != owningPlate)
        {
            Debug.Log($"[CustomerController] '{name}' refuses food from '{fromPlate?.plateLabel}'" +
                      $" — only accepts from '{owningPlate?.plateLabel}'.");
            return false;
        }

        bool matched = orderChathead.FulfillSlot(grillType);

        if (matched)
        {
            PlayEatAnimation();
            if (orderChathead.IsComplete()) OnAllFulfilled();
        }
        else
        {
            Debug.Log($"[CustomerController] '{name}' does not need '{grillType}' right now.");
        }

        return matched;
    }

    // ── Animations ────────────────────────────────────────────────────────────

    public void PlayEatAnimation()
    {
        SoundManager.Instance.PlaySFX("Eating");
        if (eatAnimation?.frames?.Length > 0)
            _anim.Play(eatAnimation, loop: false, onComplete: ResumeIdle);
        else
            ResumeIdle();
    }

    private void ResumeIdle()
    {
        if (idleAnimation?.frames?.Length > 0)
            _anim.Play(idleAnimation, loop: true);
    }

    // ── Mouth Hint ────────────────────────────────────────────────────────────

    public void SetMouthHint(bool active)
    {
        if (mouthHintImage != null) mouthHintImage.enabled = active;
    }

    // ── Leave ─────────────────────────────────────────────────────────────────

    private void OnAllFulfilled()
    {
        Debug.Log($"[CustomerController] '{name}' — all orders fulfilled! Walking away.");
        SoundManager.Instance.PlaySFX("PigLaugh");
        SoundManager.Instance.PlaySFX(satisfySound);

        if (orderChathead != null) orderChathead.gameObject.SetActive(false);
        if (mouthHintImage != null) mouthHintImage.enabled = false;

        // Notify manager immediately — counts toward the task even before the walk-off finishes.
        OnServed?.Invoke(this);

        StartCoroutine(LeaveRoutine());
    }

    private IEnumerator LeaveRoutine()
    {
        yield return new WaitForSeconds(leaveDelay);

        HasArrived = false;
        FaceBackward();

        if (walkAnimation?.frames?.Length > 0)
            _anim.Play(walkAnimation, loop: true);

        CustomerManager.Instance.OnCustomerStartWalk();
        SoundManager.Instance.PlaySFXLoop("Walking");

        yield return StartCoroutine(WalkRoutine(_spawnAnchor, onComplete: null));

        gameObject.SetActive(false);
        owningPlate = null;

        OnLeft?.Invoke(this);
        OnLeft = null;
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    /// <summary>
    /// Converts a world position into this object's anchoredPosition space,
    /// resolved against its IMMEDIATE PARENT (Characters), not the root
    /// Canvas, and corrected for this object's own anchorMin/anchorMax.
    ///
    /// ScreenPointToLocalPointInRectangle returns a point relative to the
    /// parent rect's PIVOT. anchoredPosition, however, is relative to the
    /// ANCHOR REFERENCE point (which for a bottom-left anchored object like
    /// Tori/Pig/Horse is the parent's bottom-left corner, not its pivot/
    /// center). Skipping that correction is what caused customers to spawn
    /// and walk to the wrong place even though SpawnPoint/TargetPoint
    /// (anchored middle-center) looked correct in the Inspector.
    /// </summary>
    private Vector2 WorldToAnchoredPosition(Vector3 worldPos)
    {
        if (_parentRect == null)
            return new Vector2(worldPos.x, worldPos.y);

        Camera uiCam = null;
        Canvas c = _canvasRect != null ? _canvasRect.GetComponent<Canvas>() : null;
        if (c != null && c.renderMode != RenderMode.ScreenSpaceOverlay)
            uiCam = c.worldCamera;

        Vector2 screenPoint = RectTransformUtility.WorldToScreenPoint(uiCam, worldPos);

        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            _parentRect, screenPoint, uiCam, out Vector2 localPoint);

        // Where this object's anchor reference point sits inside the parent's
        // own local space (same space ScreenPointToLocalPointInRectangle
        // just gave us back).
        Rect r = _parentRect.rect;
        Vector2 anchorMin = _rect.anchorMin;
        Vector2 anchorMax = _rect.anchorMax;
        Vector2 anchorRef = new Vector2(
            r.x + r.width * ((anchorMin.x + anchorMax.x) * 0.5f),
            r.y + r.height * ((anchorMin.y + anchorMax.y) * 0.5f));

        return localPoint - anchorRef;
    }

    private void FaceForward() => _rect.localEulerAngles = Vector3.zero;
    private void FaceBackward() => _rect.localEulerAngles = new Vector3(0f, 180f, 0f);


}