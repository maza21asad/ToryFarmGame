////using UnityEngine;
////using UnityEngine.UI;
////using System;
////using System.Collections;

////namespace ToriSausages
////{
////    /// <summary>
////    /// Attach to each customer's root Image GameObject (alongside AnimatorFrame).
////    ///
////    /// FULL FLOW
////    ///   ManagerCustomer → StartWalking() → walk anim + slide to targetPoint
////    ///   → idle anim + chat bubble (random sausage variants order)
////    ///   → player applies condiments, delivers sausages to mouth
////    ///   → all 3 slots done → bubble hides → wait → flip Y → walk back → hide
////    /// </summary>
////    public class ControllerCustomer : MonoBehaviour
////    {
////        [Header("Frame Animations")]
////        public FrameAnimation walkAnimation;
////        public FrameAnimation idleAnimation;
////        public FrameAnimation eatAnimation;

////        [Header("Movement")]
////        [Tooltip("RectTransform at the off-screen start position.")]
////        public RectTransform spawnPoint;
////        [Tooltip("RectTransform at the counter position.")]
////        public RectTransform targetPoint;
////        [Tooltip("Canvas-units per second.")]
////        public float walkSpeed = 400f;

////        [Header("Mouth Drop Zone")]
////        public RectTransform mouthPoint;
////        [Tooltip("Optional glow/arrow shown when a sausage hovers near the mouth.")]
////        public Image mouthHintImage;

////        [Header("Order Chat Bubble")]
////        [Tooltip("Set INACTIVE in the Editor.")]
////        public ChatheadOrder orderChathead;

////        [Header("Leave Delay")]
////        public float leaveDelay = 1f;

////        // ── Events / state ────────────────────────────────────────────────────────
////        public event Action OnArrived;
////        public bool HasArrived { get; private set; }

////        // ── Private ───────────────────────────────────────────────────────────────
////        private AnimatorFrame _anim;
////        private RectTransform _rect;
////        private RectTransform _canvasRect;
////        private Vector2 _spawnAnchor;
////        private Vector2 _targetAnchor;

////        // ── Unity ─────────────────────────────────────────────────────────────────

////        private void Awake()
////        {
////            _rect = GetComponent<RectTransform>();
////            _anim = GetComponent<AnimatorFrame>() ?? gameObject.AddComponent<AnimatorFrame>();
////            if (orderChathead != null) orderChathead.gameObject.SetActive(false);
////            if (mouthHintImage != null) mouthHintImage.enabled = false;
////        }

////        private void Start()
////        {
////            Canvas rootCanvas = GetComponentInParent<Canvas>();
////            if (rootCanvas != null) _canvasRect = rootCanvas.GetComponent<RectTransform>();

////            _spawnAnchor = WorldToCanvasAnchor(spawnPoint != null ? spawnPoint.position : transform.position);
////            _targetAnchor = WorldToCanvasAnchor(targetPoint != null ? targetPoint.position : transform.position);
////            _rect.anchoredPosition = _spawnAnchor;

////            ManagerCustomer.Instance?.Register(this);
////        }

////        private void OnDestroy() => ManagerCustomer.Instance?.Unregister(this);

////        private Vector2 WorldToCanvasAnchor(Vector3 worldPos)
////        {
////            if (_canvasRect == null) return new Vector2(worldPos.x, worldPos.y);
////            Canvas c = _canvasRect.GetComponent<Canvas>();
////            Camera uiCam = (c != null && c.renderMode != RenderMode.ScreenSpaceOverlay) ? c.worldCamera : null;
////            Vector2 screenPoint = RectTransformUtility.WorldToScreenPoint(uiCam, worldPos);
////            RectTransformUtility.ScreenPointToLocalPointInRectangle(_canvasRect, screenPoint, uiCam, out Vector2 local);
////            return local;
////        }

////        // ── Walk In ───────────────────────────────────────────────────────────────

////        public void StartWalking()
////        {
////            gameObject.SetActive(true);
////            _rect.anchoredPosition = _spawnAnchor;
////            FaceForward();
////            if (walkAnimation?.frames?.Length > 0) _anim.Play(walkAnimation, loop: true);
////            StartCoroutine(WalkRoutine(_targetAnchor, Arrived));
////        }

////        private IEnumerator WalkRoutine(Vector2 dest, Action onComplete)
////        {
////            while (Vector2.Distance(_rect.anchoredPosition, dest) > 1f)
////            {
////                _rect.anchoredPosition = Vector2.MoveTowards(_rect.anchoredPosition, dest, walkSpeed * Time.deltaTime);
////                yield return null;
////            }
////            _rect.anchoredPosition = dest;
////            onComplete?.Invoke();
////        }

////        private void Arrived()
////        {
////            HasArrived = true;
////            FaceForward();
////            if (idleAnimation?.frames?.Length > 0) _anim.Play(idleAnimation, loop: true);
////            if (orderChathead != null) { orderChathead.gameObject.SetActive(true); orderChathead.RandomizeOrder(); }
////            OnArrived?.Invoke();
////            Debug.Log($"[ControllerCustomer] '{name}' arrived.");
////        }

////        // ── Order Fulfillment ─────────────────────────────────────────────────────

////        /// <summary>
////        /// Called by DraggableSausage on mouth-drop.
////        /// grillType may be "Sausage", "SausageKetchup", or "SausageMayo".
////        /// Returns true if matched (sausage is destroyed by caller).
////        /// </summary>
////        public bool TryFulfillOrder(string grillType)
////        {
////            if (orderChathead == null) return false;
////            bool matched = orderChathead.FulfillSlot(grillType);
////            if (matched) { PlayEatAnimation(); if (orderChathead.IsComplete()) OnAllFulfilled(); }
////            else Debug.Log($"[ControllerCustomer] '{name}' does not need '{grillType}' right now.");
////            return matched;
////        }

////        public void PlayEatAnimation()
////        {
////            if (eatAnimation?.frames?.Length > 0) _anim.Play(eatAnimation, loop: false, onComplete: ResumeIdle);
////            else ResumeIdle();
////        }

////        private void ResumeIdle()
////        {
////            if (idleAnimation?.frames?.Length > 0) _anim.Play(idleAnimation, loop: true);
////        }

////        public void SetMouthHint(bool active)
////        {
////            if (mouthHintImage != null) mouthHintImage.enabled = active;
////        }

////        // ── All Orders Done — leave sequence ──────────────────────────────────────

////        private void OnAllFulfilled()
////        {
////            Debug.Log($"[ControllerCustomer] '{name}' all orders done!");
////            HasArrived = false;
////            if (orderChathead != null) orderChathead.gameObject.SetActive(false);
////            if (mouthHintImage != null) mouthHintImage.enabled = false;
////            StartCoroutine(LeaveRoutine());
////        }

////        private IEnumerator LeaveRoutine()
////        {
////            yield return new WaitForSeconds(leaveDelay);
////            FaceBackward();
////            if (walkAnimation?.frames?.Length > 0) _anim.Play(walkAnimation, loop: true);
////            yield return StartCoroutine(WalkRoutine(_spawnAnchor, null));
////            gameObject.SetActive(false);
////        }

////        // ── Direction helpers ─────────────────────────────────────────────────────

////        private void FaceForward() => _rect.localEulerAngles = Vector3.zero;
////        private void FaceBackward() => _rect.localEulerAngles = new Vector3(0f, 180f, 0f);
////    }
////}


//using UnityEngine;
//using UnityEngine.UI;
//using System;
//using System.Collections;

//namespace ToriSausages
//{
//    /// <summary>
//    /// Attach to each customer's root Image GameObject (alongside AnimatorFrame).
//    ///
//    /// CHANGES
//    ///   • AssignOwnedPlate() — called by ManagerCustomer at spawn time.
//    ///   • TryFulfillOrder()  — now requires the sausage to have come from
//    ///                          THIS customer's ownedPlate (checked by caller via
//    ///                          the owningPlate field on DraggableSausage).
//    ///
//    /// FULL FLOW
//    ///   ManagerCustomer → AssignSlot() + AssignOwnedPlate() → StartWalking()
//    ///   → idle anim + chat bubble (random sausage order)
//    ///   → player cooks sausage → drops on customer's OWN plate → adds condiment
//    ///   → drags to mouth → all 3 slots done → customer leaves
//    /// </summary>
//    public class ControllerCustomer : MonoBehaviour
//    {
//        [Header("Frame Animations")]
//        public FrameAnimation walkAnimation;
//        public FrameAnimation idleAnimation;
//        public FrameAnimation eatAnimation;

//        [Header("Movement")]
//        [Tooltip("RectTransform at the off-screen start position.")]
//        public RectTransform spawnPoint;
//        [Tooltip("Counter position — assigned at runtime by ManagerCustomer.AssignSlot().")]
//        public RectTransform targetPoint;
//        [Tooltip("Canvas-units per second.")]
//        public float walkSpeed = 400f;

//        [Header("Mouth Drop Zone")]
//        public RectTransform mouthPoint;
//        [Tooltip("Optional glow/arrow shown when a sausage hovers near the mouth.")]
//        public Image mouthHintImage;

//        [Header("Order Chat Bubble")]
//        [Tooltip("Set INACTIVE in the Editor.")]
//        public ChatheadOrder orderChathead;

//        [Header("Leave Delay")]
//        public float leaveDelay = 1f;

//        // ── Runtime (set by ManagerCustomer) ──────────────────────────────────────

//        /// <summary>
//        /// The plate that belongs exclusively to this customer.
//        /// Only sausages from this plate will be accepted.
//        /// Set by ManagerCustomer.AssignOwnedPlate() before StartWalking().
//        /// </summary>
//        [HideInInspector] public ControllerPlate ownedPlate = null;

//        private int _assignedSlot = -1;

//        /// <summary>Called by ManagerCustomer before StartWalking().</summary>
//        public void AssignSlot(RectTransform slotPoint, int slotIndex)
//        {
//            targetPoint = slotPoint;
//            _assignedSlot = slotIndex;
//        }

//        /// <summary>Called by ManagerCustomer before StartWalking().</summary>
//        public void AssignOwnedPlate(ControllerPlate plate)
//        {
//            ownedPlate = plate;
//            Debug.Log($"[ControllerCustomer] '{name}' owns plate '{plate?.plateLabel}'.");
//        }

//        // ── Events / state ────────────────────────────────────────────────────────

//        public event Action OnArrived;
//        public bool HasArrived { get; private set; }

//        // ── Private ───────────────────────────────────────────────────────────────

//        private AnimatorFrame _anim;
//        private RectTransform _rect;
//        private RectTransform _canvasRect;
//        private Vector2 _spawnAnchor;
//        private Vector2 _targetAnchor;

//        // ── Unity ─────────────────────────────────────────────────────────────────

//        private void Awake()
//        {
//            _rect = GetComponent<RectTransform>();
//            _anim = GetComponent<AnimatorFrame>() ?? gameObject.AddComponent<AnimatorFrame>();
//            if (orderChathead != null) orderChathead.gameObject.SetActive(false);
//            if (mouthHintImage != null) mouthHintImage.enabled = false;
//        }

//        private void Start()
//        {
//            Canvas rootCanvas = GetComponentInParent<Canvas>();
//            if (rootCanvas != null) _canvasRect = rootCanvas.GetComponent<RectTransform>();

//            _spawnAnchor = WorldToCanvasAnchor(spawnPoint != null ? spawnPoint.position : transform.position);
//            _targetAnchor = WorldToCanvasAnchor(targetPoint != null ? targetPoint.position : transform.position);
//            _rect.anchoredPosition = _spawnAnchor;

//            ManagerCustomer.Instance?.Register(this);
//        }

//        private void OnDestroy() => ManagerCustomer.Instance?.Unregister(this);

//        private Vector2 WorldToCanvasAnchor(Vector3 worldPos)
//        {
//            if (_canvasRect == null) return new Vector2(worldPos.x, worldPos.y);
//            Canvas c = _canvasRect.GetComponent<Canvas>();
//            Camera cam = (c != null && c.renderMode != RenderMode.ScreenSpaceOverlay) ? c.worldCamera : null;
//            Vector2 sp = RectTransformUtility.WorldToScreenPoint(cam, worldPos);
//            RectTransformUtility.ScreenPointToLocalPointInRectangle(_canvasRect, sp, cam, out Vector2 local);
//            return local;
//        }

//        // ── Walk In ───────────────────────────────────────────────────────────────

//        public void StartWalking()
//        {
//            gameObject.SetActive(true);

//            // Recalculate target anchor in case AssignSlot set targetPoint after Start.
//            _targetAnchor = WorldToCanvasAnchor(targetPoint != null ? targetPoint.position : transform.position);

//            _rect.anchoredPosition = _spawnAnchor;
//            FaceForward();
//            if (walkAnimation?.frames?.Length > 0) _anim.Play(walkAnimation, loop: true);
//            StartCoroutine(WalkRoutine(_targetAnchor, Arrived));
//        }

//        private IEnumerator WalkRoutine(Vector2 dest, Action onComplete)
//        {
//            while (Vector2.Distance(_rect.anchoredPosition, dest) > 1f)
//            {
//                _rect.anchoredPosition = Vector2.MoveTowards(_rect.anchoredPosition, dest, walkSpeed * Time.deltaTime);
//                yield return null;
//            }
//            _rect.anchoredPosition = dest;
//            onComplete?.Invoke();
//        }

//        private void Arrived()
//        {
//            HasArrived = true;
//            FaceForward();
//            if (idleAnimation?.frames?.Length > 0) _anim.Play(idleAnimation, loop: true);
//            if (orderChathead != null) { orderChathead.gameObject.SetActive(true); orderChathead.RandomizeOrder(); }
//            OnArrived?.Invoke();
//            Debug.Log($"[ControllerCustomer] '{name}' arrived at slot {_assignedSlot}.");
//        }

//        // ── Order Fulfillment ─────────────────────────────────────────────────────

//        /// <summary>
//        /// Called by DraggableSausage on mouth-drop.
//        ///
//        /// The sausage MUST have come from this customer's ownedPlate.
//        /// If the plate check fails the sausage is returned and the player must
//        /// either trash it or deliver it to the correct customer.
//        ///
//        /// Returns true if the sausage matched an open order slot and was accepted.
//        /// </summary>
//        public bool TryFulfillOrder(string grillType, ControllerPlate fromPlate)
//        {
//            // ── Plate ownership check ──────────────────────────────────────────────
//            if (ownedPlate != null && fromPlate != ownedPlate)
//            {
//                Debug.Log($"[ControllerCustomer] '{name}' rejected '{grillType}' — " +
//                          $"came from plate '{fromPlate?.plateLabel}', expected '{ownedPlate.plateLabel}'.");
//                return false;
//            }

//            if (orderChathead == null) return false;

//            bool matched = orderChathead.FulfillSlot(grillType);
//            if (matched)
//            {
//                PlayEatAnimation();
//                if (orderChathead.IsComplete()) OnAllFulfilled();
//            }
//            else
//            {
//                Debug.Log($"[ControllerCustomer] '{name}' does not need '{grillType}' right now.");
//            }
//            return matched;
//        }

//        public void PlayEatAnimation()
//        {
//            if (eatAnimation?.frames?.Length > 0) _anim.Play(eatAnimation, loop: false, onComplete: ResumeIdle);
//            else ResumeIdle();
//        }

//        private void ResumeIdle()
//        {
//            if (idleAnimation?.frames?.Length > 0) _anim.Play(idleAnimation, loop: true);
//        }

//        public void SetMouthHint(bool active)
//        {
//            if (mouthHintImage != null) mouthHintImage.enabled = active;
//        }

//        // ── All Orders Done — leave sequence ──────────────────────────────────────

//        private void OnAllFulfilled()
//        {
//            Debug.Log($"[ControllerCustomer] '{name}' all orders done!");
//            HasArrived = false;
//            if (orderChathead != null) orderChathead.gameObject.SetActive(false);
//            if (mouthHintImage != null) mouthHintImage.enabled = false;
//            StartCoroutine(LeaveRoutine());
//        }

//        private IEnumerator LeaveRoutine()
//        {
//            yield return new WaitForSeconds(leaveDelay);
//            FaceBackward();
//            if (walkAnimation?.frames?.Length > 0) _anim.Play(walkAnimation, loop: true);
//            yield return StartCoroutine(WalkRoutine(_spawnAnchor, null));
//            gameObject.SetActive(false);

//            ownedPlate = null; // release plate ownership
//            ManagerCustomer.Instance?.NotifyLeft(this);
//        }

//        // ── Direction helpers ─────────────────────────────────────────────────────

//        private void FaceForward() => _rect.localEulerAngles = Vector3.zero;
//        private void FaceBackward() => _rect.localEulerAngles = new Vector3(0f, 180f, 0f);
//    }
//}


//using UnityEngine;
//using UnityEngine.UI;
//using System;
//using System.Collections;

//namespace ToriSausages
//{
//    /// <summary>
//    /// Attach to each customer's root Image GameObject (alongside AnimatorFrame).
//    ///
//    /// FULL FLOW
//    ///   ManagerCustomer → StartWalking() → walk anim + slide to targetPoint
//    ///   → idle anim + chat bubble (random sausage variants order)
//    ///   → player applies condiments, delivers sausages to mouth
//    ///   → all 3 slots done → bubble hides → wait → flip Y → walk back → hide
//    /// </summary>
//    public class ControllerCustomer : MonoBehaviour
//    {
//        [Header("Frame Animations")]
//        public FrameAnimation walkAnimation;
//        public FrameAnimation idleAnimation;
//        public FrameAnimation eatAnimation;

//        [Header("Movement")]
//        [Tooltip("RectTransform at the off-screen start position.")]
//        public RectTransform spawnPoint;
//        [Tooltip("RectTransform at the counter position.")]
//        public RectTransform targetPoint;
//        [Tooltip("Canvas-units per second.")]
//        public float walkSpeed = 400f;

//        [Header("Mouth Drop Zone")]
//        public RectTransform mouthPoint;
//        [Tooltip("Optional glow/arrow shown when a sausage hovers near the mouth.")]
//        public Image mouthHintImage;

//        [Header("Order Chat Bubble")]
//        [Tooltip("Set INACTIVE in the Editor.")]
//        public ChatheadOrder orderChathead;

//        [Header("Leave Delay")]
//        public float leaveDelay = 1f;

//        // ── Events / state ────────────────────────────────────────────────────────
//        public event Action OnArrived;
//        public bool HasArrived { get; private set; }

//        // ── Private ───────────────────────────────────────────────────────────────
//        private AnimatorFrame _anim;
//        private RectTransform _rect;
//        private RectTransform _canvasRect;
//        private Vector2 _spawnAnchor;
//        private Vector2 _targetAnchor;

//        // ── Unity ─────────────────────────────────────────────────────────────────

//        private void Awake()
//        {
//            _rect = GetComponent<RectTransform>();
//            _anim = GetComponent<AnimatorFrame>() ?? gameObject.AddComponent<AnimatorFrame>();
//            if (orderChathead != null) orderChathead.gameObject.SetActive(false);
//            if (mouthHintImage != null) mouthHintImage.enabled = false;
//        }

//        private void Start()
//        {
//            Canvas rootCanvas = GetComponentInParent<Canvas>();
//            if (rootCanvas != null) _canvasRect = rootCanvas.GetComponent<RectTransform>();

//            _spawnAnchor = WorldToCanvasAnchor(spawnPoint != null ? spawnPoint.position : transform.position);
//            _targetAnchor = WorldToCanvasAnchor(targetPoint != null ? targetPoint.position : transform.position);
//            _rect.anchoredPosition = _spawnAnchor;

//            ManagerCustomer.Instance?.Register(this);
//        }

//        private void OnDestroy() => ManagerCustomer.Instance?.Unregister(this);

//        private Vector2 WorldToCanvasAnchor(Vector3 worldPos)
//        {
//            if (_canvasRect == null) return new Vector2(worldPos.x, worldPos.y);
//            Canvas c = _canvasRect.GetComponent<Canvas>();
//            Camera uiCam = (c != null && c.renderMode != RenderMode.ScreenSpaceOverlay) ? c.worldCamera : null;
//            Vector2 screenPoint = RectTransformUtility.WorldToScreenPoint(uiCam, worldPos);
//            RectTransformUtility.ScreenPointToLocalPointInRectangle(_canvasRect, screenPoint, uiCam, out Vector2 local);
//            return local;
//        }

//        // ── Walk In ───────────────────────────────────────────────────────────────

//        public void StartWalking()
//        {
//            gameObject.SetActive(true);
//            _rect.anchoredPosition = _spawnAnchor;
//            FaceForward();
//            if (walkAnimation?.frames?.Length > 0) _anim.Play(walkAnimation, loop: true);
//            StartCoroutine(WalkRoutine(_targetAnchor, Arrived));
//        }

//        private IEnumerator WalkRoutine(Vector2 dest, Action onComplete)
//        {
//            while (Vector2.Distance(_rect.anchoredPosition, dest) > 1f)
//            {
//                _rect.anchoredPosition = Vector2.MoveTowards(_rect.anchoredPosition, dest, walkSpeed * Time.deltaTime);
//                yield return null;
//            }
//            _rect.anchoredPosition = dest;
//            onComplete?.Invoke();
//        }

//        private void Arrived()
//        {
//            HasArrived = true;
//            FaceForward();
//            if (idleAnimation?.frames?.Length > 0) _anim.Play(idleAnimation, loop: true);
//            if (orderChathead != null) { orderChathead.gameObject.SetActive(true); orderChathead.RandomizeOrder(); }
//            OnArrived?.Invoke();
//            Debug.Log($"[ControllerCustomer] '{name}' arrived.");
//        }

//        // ── Order Fulfillment ─────────────────────────────────────────────────────

//        /// <summary>
//        /// Called by DraggableSausage on mouth-drop.
//        /// grillType may be "Sausage", "SausageKetchup", or "SausageMayo".
//        /// Returns true if matched (sausage is destroyed by caller).
//        /// </summary>
//        public bool TryFulfillOrder(string grillType)
//        {
//            if (orderChathead == null) return false;
//            bool matched = orderChathead.FulfillSlot(grillType);
//            if (matched) { PlayEatAnimation(); if (orderChathead.IsComplete()) OnAllFulfilled(); }
//            else Debug.Log($"[ControllerCustomer] '{name}' does not need '{grillType}' right now.");
//            return matched;
//        }

//        public void PlayEatAnimation()
//        {
//            if (eatAnimation?.frames?.Length > 0) _anim.Play(eatAnimation, loop: false, onComplete: ResumeIdle);
//            else ResumeIdle();
//        }

//        private void ResumeIdle()
//        {
//            if (idleAnimation?.frames?.Length > 0) _anim.Play(idleAnimation, loop: true);
//        }

//        public void SetMouthHint(bool active)
//        {
//            if (mouthHintImage != null) mouthHintImage.enabled = active;
//        }

//        // ── All Orders Done — leave sequence ──────────────────────────────────────

//        private void OnAllFulfilled()
//        {
//            Debug.Log($"[ControllerCustomer] '{name}' all orders done!");
//            HasArrived = false;
//            if (orderChathead != null) orderChathead.gameObject.SetActive(false);
//            if (mouthHintImage != null) mouthHintImage.enabled = false;
//            StartCoroutine(LeaveRoutine());
//        }

//        private IEnumerator LeaveRoutine()
//        {
//            yield return new WaitForSeconds(leaveDelay);
//            FaceBackward();
//            if (walkAnimation?.frames?.Length > 0) _anim.Play(walkAnimation, loop: true);
//            yield return StartCoroutine(WalkRoutine(_spawnAnchor, null));
//            gameObject.SetActive(false);
//        }

//        // ── Direction helpers ─────────────────────────────────────────────────────

//        private void FaceForward() => _rect.localEulerAngles = Vector3.zero;
//        private void FaceBackward() => _rect.localEulerAngles = new Vector3(0f, 180f, 0f);
//    }
//}


using UnityEngine;
using UnityEngine.UI;
using System;
using System.Collections;

namespace ToriSausages
{
    /// <summary>
    /// Attach to each customer's root Image GameObject (alongside AnimatorFrame).
    ///
    /// CHANGES
    ///   • AssignOwnedPlate() — called by ManagerCustomer at spawn time.
    ///   • TryFulfillOrder()  — now requires the sausage to have come from
    ///                          THIS customer's ownedPlate (checked by caller via
    ///                          the owningPlate field on DraggableSausage).
    ///
    /// FULL FLOW
    ///   ManagerCustomer → AssignSlot() + AssignOwnedPlate() → StartWalking()
    ///   → idle anim + chat bubble (random sausage order)
    ///   → player cooks sausage → drops on customer's OWN plate → adds condiment
    ///   → drags to mouth → all 3 slots done → customer leaves
    /// </summary>
    public class ControllerCustomer : MonoBehaviour
    {
        [Header("Frame Animations")]
        public FrameAnimation walkAnimation;
        public FrameAnimation idleAnimation;
        public FrameAnimation eatAnimation;

        [Header("Movement")]
        [Tooltip("RectTransform at the off-screen start position.")]
        public RectTransform spawnPoint;
        [Tooltip("Counter position — assigned at runtime by ManagerCustomer.AssignSlot().")]
        public RectTransform targetPoint;
        [Tooltip("Canvas-units per second.")]
        public float walkSpeed = 400f;

        [Header("Mouth Drop Zone")]
        public RectTransform mouthPoint;
        [Tooltip("Optional glow/arrow shown when a sausage hovers near the mouth.")]
        public Image mouthHintImage;

        [Header("Order Chat Bubble")]
        [Tooltip("Set INACTIVE in the Editor.")]
        public ChatheadOrder orderChathead;

        [Header("Leave Delay")]
        public float leaveDelay = 1f;

        [Header("SFX")]
        [Tooltip("SFX key played when all this customer's orders are fulfilled. E.g. Satisfy1 / Satisfy2 / Satisfy3.")]
        [SerializeField] private string satisfySound = "Satisfy1";

        // Own AudioSource so each customer's walking loop is independent.
        private AudioSource _walkLoopSource;

        // ── Runtime (set by ManagerCustomer) ──────────────────────────────────────

        /// <summary>
        /// The plate that belongs exclusively to this customer.
        /// Only sausages from this plate will be accepted.
        /// Set by ManagerCustomer.AssignOwnedPlate() before StartWalking().
        /// </summary>
        [HideInInspector] public ControllerPlate ownedPlate = null;

        private int _assignedSlot = -1;

        /// <summary>Called by ManagerCustomer before StartWalking().</summary>
        public void AssignSlot(RectTransform slotPoint, int slotIndex)
        {
            targetPoint = slotPoint;
            _assignedSlot = slotIndex;
        }

        /// <summary>Called by ManagerCustomer before StartWalking().</summary>
        public void AssignOwnedPlate(ControllerPlate plate)
        {
            ownedPlate = plate;
            Debug.Log($"[ControllerCustomer] '{name}' owns plate '{plate?.plateLabel}'.");
        }

        // ── Events / state ────────────────────────────────────────────────────────

        public event Action OnArrived;
        public bool HasArrived { get; private set; }

        // ── Private ───────────────────────────────────────────────────────────────

        private AnimatorFrame _anim;
        private RectTransform _rect;
        private RectTransform _canvasRect;
        private Vector2 _spawnAnchor;
        private Vector2 _targetAnchor;

        // ── Unity ─────────────────────────────────────────────────────────────────

        private void Awake()
        {
            _rect = GetComponent<RectTransform>();
            _anim = GetComponent<AnimatorFrame>() ?? gameObject.AddComponent<AnimatorFrame>();
            if (orderChathead != null) orderChathead.gameObject.SetActive(false);
            if (mouthHintImage != null) mouthHintImage.enabled = false;

            // Dedicated loop source — independent per customer so two can walk simultaneously.
            _walkLoopSource = gameObject.AddComponent<AudioSource>();
            _walkLoopSource.loop = true;
            _walkLoopSource.playOnAwake = false;
        }

        private void Start()
        {
            Canvas rootCanvas = GetComponentInParent<Canvas>();
            if (rootCanvas != null) _canvasRect = rootCanvas.GetComponent<RectTransform>();

            _spawnAnchor = WorldToCanvasAnchor(spawnPoint != null ? spawnPoint.position : transform.position);
            _targetAnchor = WorldToCanvasAnchor(targetPoint != null ? targetPoint.position : transform.position);
            _rect.anchoredPosition = _spawnAnchor;

            ManagerCustomer.Instance?.Register(this);
        }

        private void OnDestroy() => ManagerCustomer.Instance?.Unregister(this);

        private Vector2 WorldToCanvasAnchor(Vector3 worldPos)
        {
            if (_canvasRect == null) return new Vector2(worldPos.x, worldPos.y);
            Canvas c = _canvasRect.GetComponent<Canvas>();
            Camera cam = (c != null && c.renderMode != RenderMode.ScreenSpaceOverlay) ? c.worldCamera : null;
            Vector2 sp = RectTransformUtility.WorldToScreenPoint(cam, worldPos);
            RectTransformUtility.ScreenPointToLocalPointInRectangle(_canvasRect, sp, cam, out Vector2 local);
            return local;
        }

        // ── Walk In ───────────────────────────────────────────────────────────────

        public void StartWalking()
        {
            gameObject.SetActive(true);

            // Recalculate both anchors fresh so stale baked values never cause a teleport.
            _spawnAnchor = WorldToCanvasAnchor(spawnPoint != null ? spawnPoint.position : transform.position);
            _targetAnchor = WorldToCanvasAnchor(targetPoint != null ? targetPoint.position : transform.position);

            _rect.anchoredPosition = _spawnAnchor;
            FaceForward();
            if (walkAnimation?.frames?.Length > 0) _anim.Play(walkAnimation, loop: true);
            PlayWalkSFX();
            StartCoroutine(WalkRoutine(_targetAnchor, Arrived));
        }

        private IEnumerator WalkRoutine(Vector2 dest, Action onComplete)
        {
            while (Vector2.Distance(_rect.anchoredPosition, dest) > 1f)
            {
                _rect.anchoredPosition = Vector2.MoveTowards(_rect.anchoredPosition, dest, walkSpeed * Time.deltaTime);
                yield return null;
            }
            _rect.anchoredPosition = dest;
            StopWalkSFX();
            onComplete?.Invoke();
        }

        private void Arrived()
        {
            HasArrived = true;
            FaceForward();
            if (idleAnimation?.frames?.Length > 0) _anim.Play(idleAnimation, loop: true);
            if (orderChathead != null) { orderChathead.gameObject.SetActive(true); orderChathead.RandomizeOrder(); }
            OnArrived?.Invoke();
            Debug.Log($"[ControllerCustomer] '{name}' arrived at slot {_assignedSlot}.");
        }

        // ── Order Fulfillment ─────────────────────────────────────────────────────

        /// <summary>
        /// Called by DraggableSausage on mouth-drop.
        ///
        /// The sausage MUST have come from this customer's ownedPlate.
        /// If the plate check fails the sausage is returned and the player must
        /// either trash it or deliver it to the correct customer.
        ///
        /// Returns true if the sausage matched an open order slot and was accepted.
        /// </summary>
        public bool TryFulfillOrder(string grillType, ControllerPlate fromPlate)
        {
            // ── Plate ownership check ──────────────────────────────────────────────
            if (ownedPlate != null && fromPlate != ownedPlate)
            {
                Debug.Log($"[ControllerCustomer] '{name}' rejected '{grillType}' — " +
                          $"came from plate '{fromPlate?.plateLabel}', expected '{ownedPlate.plateLabel}'.");
                return false;
            }

            if (orderChathead == null) return false;

            bool matched = orderChathead.FulfillSlot(grillType);
            if (matched)
            {
                PlayEatAnimation();
                if (orderChathead.IsComplete()) OnAllFulfilled();
            }
            else
            {
                Debug.Log($"[ControllerCustomer] '{name}' does not need '{grillType}' right now.");
            }
            return matched;
        }

        public void PlayEatAnimation()
        {
            SoundManager.Instance?.PlaySFX("Eating");
            if (eatAnimation?.frames?.Length > 0) _anim.Play(eatAnimation, loop: false, onComplete: ResumeIdle);
            else ResumeIdle();
        }

        private void ResumeIdle()
        {
            if (idleAnimation?.frames?.Length > 0) _anim.Play(idleAnimation, loop: true);
        }

        public void SetMouthHint(bool active)
        {
            if (mouthHintImage != null) mouthHintImage.enabled = active;
        }

        // ── All Orders Done — leave sequence ──────────────────────────────────────

        private void OnAllFulfilled()
        {
            Debug.Log($"[ControllerCustomer] '{name}' all orders done!");
            HasArrived = false;
            if (orderChathead != null) orderChathead.gameObject.SetActive(false);
            if (mouthHintImage != null) mouthHintImage.enabled = false;

            SoundManager.Instance?.PlaySFX(satisfySound);
            SoundManager.Instance?.PlaySFX("PigLaugh");

            // Count as fed immediately when all 3 food items are eaten.
            ManagerCustomer.Instance?.NotifyServed(this);

            StartCoroutine(LeaveRoutine());
        }

        private IEnumerator LeaveRoutine()
        {
            yield return new WaitForSeconds(leaveDelay);

            // Recalculate spawn anchor so the walk-back goes to the correct off-screen point.
            _spawnAnchor = WorldToCanvasAnchor(spawnPoint != null ? spawnPoint.position : transform.position);

            FaceBackward();
            if (walkAnimation?.frames?.Length > 0) _anim.Play(walkAnimation, loop: true);
            PlayWalkSFX();
            yield return StartCoroutine(WalkRoutine(_spawnAnchor, null));
            gameObject.SetActive(false);

            ownedPlate = null; // release plate ownership
            ManagerCustomer.Instance?.NotifyLeft(this);
        }

        // ── Direction helpers ─────────────────────────────────────────────────────

        // ── SFX helpers ───────────────────────────────────────────────────────────

        private void PlayWalkSFX()
        {
            if (SoundManager.Instance == null || _walkLoopSource == null) return;
            AudioClip clip = SoundManager.Instance.GetSFXClip("Walking");
            if (clip == null) return;
            _walkLoopSource.clip = clip;
            _walkLoopSource.volume = SoundManager.Instance.GetSFXVolume("Walking");
            _walkLoopSource.pitch = SoundManager.Instance.GetSFXPitch("Walking");
            _walkLoopSource.loop = true;
            _walkLoopSource.Play();
        }

        private void StopWalkSFX()
        {
            if (_walkLoopSource != null) _walkLoopSource.Stop();
        }

        private void FaceForward() => _rect.localEulerAngles = Vector3.zero;
        private void FaceBackward() => _rect.localEulerAngles = new Vector3(0f, 180f, 0f);
    }
}