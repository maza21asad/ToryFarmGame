////using UnityEngine;
////using UnityEngine.EventSystems;
////using UnityEngine.UI;
////using System.Collections;

////namespace ToriSausages
////{
////    [RequireComponent(typeof(Image))]
////    public class CondimentBottle : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
////    {
////        [Header("Condiment Settings")]
////        [Tooltip("Use \"Ketchup\" or \"Mayo\".")]
////        public string condimentType = "Ketchup";

////        [Header("Pour Animation")]
////        public FrameAnimation pourAnimation;

////        [Header("Animation Overlay Transform")]
////        public float overlayWidth = 100f;
////        public float overlayHeight = 100f;
////        [Tooltip("Offset from the sausage centre.")]
////        public Vector2 overlayOffset = Vector2.zero;

////        [Header("Result Sprites  (applied after animation finishes)")]
////        public Sprite sausageResultSprite;
////        public Sprite sausageComboSprite;

////        [Header("Detection Radius")]
////        public float dropRadius = 120f;

////        // ── Private ───────────────────────────────────────────────────────────────

////        private Canvas _canvas;
////        private RectTransform _rect;
////        private Image _image;
////        private Sprite _bottleSprite;   // locked bottle sprite — never overwritten

////        private Vector3 _homeWorldPos;
////        private Vector2 _homeSizeDelta;
////        private Transform _homeParent;

////        // ── Unity ─────────────────────────────────────────────────────────────────

////        private void Awake()
////        {
////            _rect = GetComponent<RectTransform>();
////            _image = GetComponent<Image>();
////            _canvas = GetComponentInParent<Canvas>();
////        }

////        private void Start()
////        {
////            _homeWorldPos = _rect.position;
////            _homeSizeDelta = _rect.sizeDelta;
////            _homeParent = _rect.parent;

////            // Cache the bottle's own sprite once so we can always restore it.
////            _bottleSprite = _image != null ? _image.sprite : null;
////        }

////        // ── Drag ──────────────────────────────────────────────────────────────────

////        public void OnBeginDrag(PointerEventData eventData)
////        {
////            // Stop any AnimatorFrame on THIS object so it can't overwrite the sprite.
////            AnimatorFrame anim = GetComponent<AnimatorFrame>();
////            if (anim != null) anim.Stop();

////            // Restore the bottle's own sprite in case something changed it.
////            if (_image != null && _bottleSprite != null)
////                _image.sprite = _bottleSprite;

////            if (_canvas == null) _canvas = GetComponentInParent<Canvas>();
////            if (_canvas != null)
////            {
////                Vector3 wp = _rect.position;
////                _rect.SetParent(_canvas.transform, worldPositionStays: true);
////                _rect.position = wp;
////            }
////        }

////        public void OnDrag(PointerEventData eventData) => MoveToPointer(eventData);

////        public void OnEndDrag(PointerEventData eventData)
////        {
////            DraggableSausage target = FindEligibleSausageNear(eventData);

////            if (target != null)
////                StartCoroutine(ApplyCondimentWithAnimation(target));
////            else
////                Debug.Log("[CondimentBottle] No eligible sausage nearby — returning home.");

////            ReturnHome();
////        }

////        // ── Animation overlay ─────────────────────────────────────────────────────

////        private IEnumerator ApplyCondimentWithAnimation(DraggableSausage sausage)
////        {
////            string current = sausage.grillType;
////            string newType = GetResultType(current);
////            Sprite newSprite = GetResultSprite(current);

////            sausage.enabled = false;

////            // Create overlay as child of the sausage.
////            GameObject overlayGO = new GameObject("CondimentOverlay", typeof(RectTransform), typeof(Image));
////            overlayGO.transform.SetParent(sausage.transform, false);

////            RectTransform overlayRect = overlayGO.GetComponent<RectTransform>();
////            overlayRect.anchorMin = new Vector2(0.5f, 0.5f);
////            overlayRect.anchorMax = new Vector2(0.5f, 0.5f);
////            overlayRect.pivot = new Vector2(0.5f, 0.5f);
////            overlayRect.sizeDelta = new Vector2(overlayWidth, overlayHeight);
////            overlayRect.anchoredPosition = overlayOffset;
////            overlayRect.localScale = Vector3.one;

////            Image overlayImage = overlayGO.GetComponent<Image>();
////            overlayImage.raycastTarget = false;
////            overlayImage.color = Color.white;

////            // Play pour animation on the overlay only — never on the bottle.
////            if (pourAnimation != null && pourAnimation.frames != null && pourAnimation.frames.Length > 0)
////            {
////                overlayImage.sprite = pourAnimation.frames[0];
////                AnimatorFrame animator = overlayGO.AddComponent<AnimatorFrame>();
////                bool done = false;
////                animator.Play(pourAnimation, loop: false, onComplete: () => done = true);
////                yield return new WaitUntil(() => done);
////            }

////            Destroy(overlayGO);

////            Image sausageImage = sausage.GetComponent<Image>();
////            if (sausageImage != null && newSprite != null)
////                sausageImage.sprite = newSprite;

////            sausage.grillType = newType;
////            sausage.enabled = true;

////            Debug.Log($"[CondimentBottle] '{current}' → '{newType}'.");
////        }

////        // ── Logic helpers ─────────────────────────────────────────────────────────

////        private string GetResultType(string current)
////        {
////            if (condimentType == "Ketchup")
////                return current == "Sausage" ? "SausageKetchup" : "SausageKetchupMayo";
////            return current == "Sausage" ? "SausageMayo" : "SausageKetchupMayo";
////        }

////        private Sprite GetResultSprite(string current)
////        {
////            bool becomesCombo =
////                (condimentType == "Ketchup" && current == "SausageMayo") ||
////                (condimentType == "Mayo" && current == "SausageKetchup");
////            return becomesCombo ? sausageComboSprite : sausageResultSprite;
////        }

////        // ── Detection ─────────────────────────────────────────────────────────────

////        private DraggableSausage FindEligibleSausageNear(PointerEventData eventData)
////        {
////            DraggableSausage best = null;
////            float bestDist = dropRadius;
////            Camera uiCam = eventData.pressEventCamera;

////            foreach (DraggableSausage s in FindObjectsOfType<DraggableSausage>())
////            {
////                if (!s.isOnPlate) continue;
////                if (!CanReceiveCondiment(s.grillType)) continue;

////                RectTransform rt = s.GetComponent<RectTransform>();
////                if (rt == null) continue;

////                Vector2 screenPos = RectTransformUtility.WorldToScreenPoint(uiCam, rt.position);
////                float dist = Vector2.Distance(eventData.position, screenPos);
////                if (dist < bestDist) { bestDist = dist; best = s; }
////            }

////            return best;
////        }

////        private bool CanReceiveCondiment(string grillType)
////        {
////            if (condimentType == "Ketchup")
////                return grillType == "Sausage" || grillType == "SausageMayo";
////            if (condimentType == "Mayo")
////                return grillType == "Sausage" || grillType == "SausageKetchup";
////            return false;
////        }

////        // ── Movement helpers ──────────────────────────────────────────────────────

////        private void ReturnHome()
////        {
////            _rect.SetParent(_homeParent, worldPositionStays: true);
////            _rect.position = _homeWorldPos;
////            _rect.sizeDelta = _homeSizeDelta;

////            // Always restore the bottle sprite on return.
////            if (_image != null && _bottleSprite != null)
////                _image.sprite = _bottleSprite;
////        }

////        private void MoveToPointer(PointerEventData eventData)
////        {
////            if (_canvas == null) _canvas = GetComponentInParent<Canvas>();
////            if (_canvas == null) return;

////            RectTransformUtility.ScreenPointToLocalPointInRectangle(
////                _canvas.GetComponent<RectTransform>(),
////                eventData.position,
////                eventData.pressEventCamera,
////                out Vector2 local);

////            _rect.localPosition = local;
////        }
////    }
////}

////using UnityEngine;
////using UnityEngine.EventSystems;
////using UnityEngine.UI;
////using System.Collections;

////namespace ToriSausages
////{
////    [RequireComponent(typeof(Image))]
////    public class CondimentBottle : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
////    {
////        [Header("Condiment Settings")]
////        [Tooltip("Use \"Ketchup\" or \"Mayo\".")]
////        public string condimentType = "Ketchup";

////        [Header("Pour Animation")]
////        public FrameAnimation pourAnimation;

////        [Header("Animation Overlay Transform")]
////        public float overlayWidth = 100f;
////        public float overlayHeight = 100f;
////        [Tooltip("Offset from the sausage centre.")]
////        public Vector2 overlayOffset = Vector2.zero;

////        [Header("Result Sprites  (applied after animation finishes)")]
////        public Sprite sausageResultSprite;
////        public Sprite sausageComboSprite;

////        [Header("Detection Radius")]
////        public float dropRadius = 120f;


////        // ── Private ───────────────────────────────────────────────────────────────

////        private Canvas _canvas;
////        private RectTransform _rect;
////        private Image _image;
////        private Sprite _bottleSprite;   // locked bottle sprite — never overwritten

////        private Vector3 _homeWorldPos;
////        private Vector2 _homeSizeDelta;
////        private Transform _homeParent;

////        // ── Unity ─────────────────────────────────────────────────────────────────

////        private void Awake()
////        {
////            _rect = GetComponent<RectTransform>();
////            _image = GetComponent<Image>();
////            _canvas = GetComponentInParent<Canvas>();
////        }

////        private void Start()
////        {
////            _homeWorldPos = _rect.position;
////            _homeSizeDelta = _rect.sizeDelta;
////            _homeParent = _rect.parent;

////            // Cache the bottle's own sprite once so we can always restore it.
////            _bottleSprite = _image != null ? _image.sprite : null;
////        }

////        // ── Drag ──────────────────────────────────────────────────────────────────

////        public void OnBeginDrag(PointerEventData eventData)
////        {
////            // Stop any AnimatorFrame on THIS object so it can't overwrite the sprite.
////            AnimatorFrame anim = GetComponent<AnimatorFrame>();
////            if (anim != null) anim.Stop();

////            // Restore the bottle's own sprite in case something changed it.
////            if (_image != null && _bottleSprite != null)
////                _image.sprite = _bottleSprite;

////            if (_canvas == null) _canvas = GetComponentInParent<Canvas>();
////            if (_canvas != null)
////            {
////                Vector3 wp = _rect.position;
////                _rect.SetParent(_canvas.transform, worldPositionStays: true);
////                _rect.position = wp;
////            }
////        }

////        public void OnDrag(PointerEventData eventData) => MoveToPointer(eventData);

////        public void OnEndDrag(PointerEventData eventData)
////        {
////            DraggableSausage target = FindEligibleSausageNear(eventData);

////            if (target != null)
////                StartCoroutine(ApplyCondimentWithAnimation(target));
////            else
////                Debug.Log("[CondimentBottle] No eligible sausage nearby — returning home.");

////            ReturnHome();
////        }

////        // ── Animation overlay ─────────────────────────────────────────────────────

////        private IEnumerator ApplyCondimentWithAnimation(DraggableSausage sausage)
////        {
////            string current = sausage.grillType;
////            string newType = GetResultType(current);
////            Sprite newSprite = GetResultSprite(current);

////            sausage.enabled = false;

////            // Create overlay as child of the sausage.
////            GameObject overlayGO = new GameObject("CondimentOverlay", typeof(RectTransform), typeof(Image));
////            overlayGO.transform.SetParent(sausage.transform, false);

////            RectTransform overlayRect = overlayGO.GetComponent<RectTransform>();
////            overlayRect.anchorMin = new Vector2(0.5f, 0.5f);
////            overlayRect.anchorMax = new Vector2(0.5f, 0.5f);
////            overlayRect.pivot = new Vector2(0.5f, 0.5f);
////            overlayRect.sizeDelta = new Vector2(overlayWidth, overlayHeight);
////            overlayRect.anchoredPosition = overlayOffset;
////            overlayRect.localScale = Vector3.one;

////            Image overlayImage = overlayGO.GetComponent<Image>();
////            overlayImage.raycastTarget = false;
////            overlayImage.color = Color.white;

////            // Play pour animation on the overlay only — never on the bottle.
////            if (pourAnimation != null && pourAnimation.frames != null && pourAnimation.frames.Length > 0)
////            {
////                overlayImage.sprite = pourAnimation.frames[0];
////                AnimatorFrame animator = overlayGO.AddComponent<AnimatorFrame>();
////                bool done = false;
////                animator.Play(pourAnimation, loop: false, onComplete: () => done = true);
////                yield return new WaitUntil(() => done);
////            }

////            Destroy(overlayGO);

////            Image sausageImage = sausage.GetComponent<Image>();
////            if (sausageImage != null && newSprite != null)
////                sausageImage.sprite = newSprite;

////            sausage.grillType = newType;
////            sausage.enabled = true;

////            Debug.Log($"[CondimentBottle] '{current}' → '{newType}'.");
////        }

////        // ── Logic helpers ─────────────────────────────────────────────────────────

////        private string GetResultType(string current)
////        {
////            if (condimentType == "Ketchup")
////                return current == "Sausage" ? "SausageKetchup" : "SausageKetchupMayo";
////            return current == "Sausage" ? "SausageMayo" : "SausageKetchupMayo";
////        }

////        private Sprite GetResultSprite(string current)
////        {
////            bool becomesCombo =
////                (condimentType == "Ketchup" && current == "SausageMayo") ||
////                (condimentType == "Mayo" && current == "SausageKetchup");
////            return becomesCombo ? sausageComboSprite : sausageResultSprite;
////        }

////        // ── Detection ─────────────────────────────────────────────────────────────

////        private DraggableSausage FindEligibleSausageNear(PointerEventData eventData)
////        {
////            DraggableSausage best = null;
////            float bestDist = dropRadius;
////            Camera uiCam = eventData.pressEventCamera;

////            foreach (DraggableSausage s in FindObjectsOfType<DraggableSausage>())
////            {
////                if (!s.isOnPlate) continue;
////                if (!CanReceiveCondiment(s.grillType)) continue;

////                RectTransform rt = s.GetComponent<RectTransform>();
////                if (rt == null) continue;

////                Vector2 screenPos = RectTransformUtility.WorldToScreenPoint(uiCam, rt.position);
////                float dist = Vector2.Distance(eventData.position, screenPos);
////                if (dist < bestDist) { bestDist = dist; best = s; }
////            }

////            return best;
////        }

////        private bool CanReceiveCondiment(string grillType)
////        {
////            if (condimentType == "Ketchup")
////                return grillType == "Sausage" || grillType == "SausageMayo";
////            if (condimentType == "Mayo")
////                return grillType == "Sausage" || grillType == "SausageKetchup";
////            return false;
////        }

////        // ── Movement helpers ──────────────────────────────────────────────────────

////        private void ReturnHome()
////        {
////            _rect.SetParent(_homeParent, worldPositionStays: true);
////            _rect.position = _homeWorldPos;
////            _rect.sizeDelta = _homeSizeDelta;

////            // Always restore the bottle sprite on return.
////            if (_image != null && _bottleSprite != null)
////                _image.sprite = _bottleSprite;
////        }

////        private void MoveToPointer(PointerEventData eventData)
////        {
////            if (_canvas == null) _canvas = GetComponentInParent<Canvas>();
////            if (_canvas == null) return;

////            RectTransformUtility.ScreenPointToLocalPointInRectangle(
////                _canvas.GetComponent<RectTransform>(),
////                eventData.position,
////                eventData.pressEventCamera,
////                out Vector2 local);

////            _rect.localPosition = local;
////        }
////    }
////}

//using UnityEngine;
//using UnityEngine.EventSystems;
//using UnityEngine.UI;
//using System.Collections;

//namespace ToriSausages
//{
//    /// <summary>
//    /// Attach to each condiment bottle Image (ketchup or mayo).
//    ///
//    /// DRAG BEHAVIOUR
//    ///   OnBeginDrag → bottle Image swaps to pourAnimation.frames[0]  (pour preview)
//    ///                 bottle follows the pointer WITHOUT jumping — grab point is preserved
//    ///   OnDrag      → bottle stays under the exact point where the player grabbed it
//    ///   OnEndDrag   → bottle sprite restores to the original bottle image
//    ///              → if a valid sausage is nearby:
//    ///                   overlay child plays the full pour animation on the sausage,
//    ///                   then sausage sprite + grillType are updated.
//    ///
//    /// SUPPORTED COMBINATIONS
//    ///   Ketchup bottle:
//    ///     "Sausage"        → "SausageKetchup"        (sausageResultSprite)
//    ///     "SausageMayo"    → "SausageKetchupMayo"     (sausageComboSprite)
//    ///
//    ///   Mayo bottle:
//    ///     "Sausage"        → "SausageMayo"            (sausageResultSprite)
//    ///     "SausageKetchup" → "SausageKetchupMayo"     (sausageComboSprite)
//    ///
//    /// SETUP — Ketchup bottle
//    ///   condimentType        = "Ketchup"
//    ///   pourAnimation        = ketchup pour frames + fps
//    ///   overlayWidth/Height  = size of the animation overlay on the sausage
//    ///   overlayOffset        = shift overlay relative to sausage centre
//    ///   sausageResultSprite  = sausage + ketchup sprite
//    ///   sausageComboSprite   = sausage + ketchup + mayo sprite
//    ///
//    /// SETUP — Mayo bottle
//    ///   condimentType        = "Mayo"
//    ///   pourAnimation        = mayo pour frames + fps
//    ///   sausageResultSprite  = sausage + mayo sprite
//    ///   sausageComboSprite   = sausage + ketchup + mayo sprite
//    /// </summary>
//    [RequireComponent(typeof(Image))]
//    public class CondimentBottle : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
//    {
//        [Header("Condiment Settings")]
//        [Tooltip("Use \"Ketchup\" or \"Mayo\".")]
//        public string condimentType = "Ketchup";

//        [Header("Pour Animation")]
//        [Tooltip("Frame 0 shows on the BOTTLE while dragging.\n" +
//                 "All frames play as an overlay on the SAUSAGE after drop.")]
//        public FrameAnimation pourAnimation;

//        [Header("Animation Overlay Transform  (on sausage after drop)")]
//        public float overlayWidth = 100f;
//        public float overlayHeight = 100f;
//        [Tooltip("Offset from the sausage centre.")]
//        public Vector2 overlayOffset = Vector2.zero;

//        [Header("Result Sprites  (applied after animation finishes)")]
//        [Tooltip("Sausage sprite after this single condiment is applied.")]
//        public Sprite sausageResultSprite;
//        [Tooltip("Sausage sprite when BOTH condiments have been applied (combo).")]
//        public Sprite sausageComboSprite;

//        [Header("Detection Radius")]
//        [Tooltip("Screen-pixel radius within which a drop counts as landing on a sausage.")]
//        public float dropRadius = 120f;

//        [Header("Drag Offset")]
//        [Tooltip("Position offset of the bottle relative to the cursor while dragging.")]
//        public Vector2 dragOffset = Vector2.zero;

//        // ── Private ───────────────────────────────────────────────────────────────

//        private Canvas _canvas;
//        private RectTransform _rect;
//        private Image _image;
//        private Sprite _bottleSprite;   // original bottle sprite — always restored

//        private Vector3 _homeWorldPos;
//        private Vector2 _homeSizeDelta;
//        private Transform _homeParent;

//        // Offset between the bottle pivot and the exact point where the player
//        // grabbed it. Applied every frame so the bottle never jumps on pickup.
//        private Vector2 _grabOffset;

//        // ── Unity ─────────────────────────────────────────────────────────────────

//        private void Awake()
//        {
//            _rect = GetComponent<RectTransform>();
//            _image = GetComponent<Image>();
//            _canvas = GetComponentInParent<Canvas>();
//        }

//        private void Start()
//        {
//            _homeWorldPos = _rect.position;
//            _homeSizeDelta = _rect.sizeDelta;
//            _homeParent = _rect.parent;

//            // Cache the real bottle sprite now, before anything can change it.
//            _bottleSprite = _image != null ? _image.sprite : null;
//        }

//        // ── Drag ──────────────────────────────────────────────────────────────────

//        public void OnBeginDrag(PointerEventData eventData)
//        {
//            // Stop any stray AnimatorFrame on the bottle (safety net).
//            AnimatorFrame existingAnim = GetComponent<AnimatorFrame>();
//            if (existingAnim != null) existingAnim.Stop();

//            // Show pour animation frame 0 on the bottle while dragging.
//            if (_image != null && pourAnimation?.frames != null && pourAnimation.frames.Length > 0)
//                _image.sprite = pourAnimation.frames[0];

//            // Lift bottle to canvas root so it renders above everything.
//            if (_canvas == null) _canvas = GetComponentInParent<Canvas>();
//            if (_canvas != null)
//            {
//                Vector3 wp = _rect.position;
//                _rect.SetParent(_canvas.transform, worldPositionStays: true);
//                _rect.position = wp;
//            }

//            // Calculate grab offset AFTER reparenting so localPosition is in canvas space.
//            if (_canvas != null)
//            {
//                RectTransformUtility.ScreenPointToLocalPointInRectangle(
//                    _canvas.GetComponent<RectTransform>(),
//                    eventData.position,
//                    eventData.pressEventCamera,
//                    out Vector2 pointerLocal);
//                // Always pin to the top of the bottle so the player can see where they're pouring.
//                _grabOffset = dragOffset;
//            }
//            else
//            {
//                _grabOffset = Vector2.zero;
//            }
//        }

//        public void OnDrag(PointerEventData eventData) => MoveToPointer(eventData);

//        public void OnEndDrag(PointerEventData eventData)
//        {
//            // Restore bottle sprite and position BEFORE starting the sausage coroutine.
//            ReturnHome();

//            DraggableSausage target = FindEligibleSausageNear(eventData);

//            if (target != null)
//                StartCoroutine(ApplyCondimentWithAnimation(target));
//            else
//                Debug.Log("[CondimentBottle] No eligible sausage nearby — returning home.");
//        }

//        // ── Animation overlay on sausage ──────────────────────────────────────────

//        private IEnumerator ApplyCondimentWithAnimation(DraggableSausage sausage)
//        {
//            string current = sausage.grillType;
//            string newType = GetResultType(current);
//            Sprite newSprite = GetResultSprite(current);

//            // Block sausage dragging while animation plays.
//            sausage.enabled = false;

//            // ── Create overlay child on the sausage ───────────────────────────────
//            GameObject overlayGO = new GameObject("CondimentOverlay",
//                                                   typeof(RectTransform), typeof(Image));
//            overlayGO.transform.SetParent(sausage.transform, false);

//            RectTransform overlayRect = overlayGO.GetComponent<RectTransform>();
//            overlayRect.anchorMin = new Vector2(0.5f, 0.5f);
//            overlayRect.anchorMax = new Vector2(0.5f, 0.5f);
//            overlayRect.pivot = new Vector2(0.5f, 0.5f);
//            overlayRect.sizeDelta = new Vector2(overlayWidth, overlayHeight);
//            overlayRect.anchoredPosition = overlayOffset;
//            overlayRect.localScale = Vector3.one;

//            Image overlayImage = overlayGO.GetComponent<Image>();
//            overlayImage.raycastTarget = false;
//            overlayImage.color = Color.white;

//            // ── Play full pour animation on the overlay ───────────────────────────
//            if (pourAnimation != null && pourAnimation.frames != null &&
//                pourAnimation.frames.Length > 0)
//            {
//                overlayImage.sprite = pourAnimation.frames[0];
//                AnimatorFrame animator = overlayGO.AddComponent<AnimatorFrame>();
//                bool done = false;
//                animator.Play(pourAnimation, loop: false, onComplete: () => done = true);
//                yield return new WaitUntil(() => done);
//            }

//            // ── Cleanup & swap sausage sprite ─────────────────────────────────────
//            Destroy(overlayGO);

//            Image sausageImage = sausage.GetComponent<Image>();
//            if (sausageImage != null && newSprite != null)
//                sausageImage.sprite = newSprite;

//            sausage.grillType = newType;
//            sausage.enabled = true;

//            Debug.Log($"[CondimentBottle] '{current}' → '{newType}'.");
//        }

//        // ── Logic helpers ─────────────────────────────────────────────────────────

//        private string GetResultType(string current)
//        {
//            if (condimentType == "Ketchup")
//                return current == "Sausage" ? "SausageKetchup" : "SausageKetchupMayo";
//            // Mayo
//            return current == "Sausage" ? "SausageMayo" : "SausageKetchupMayo";
//        }

//        private Sprite GetResultSprite(string current)
//        {
//            bool becomesCombo =
//                (condimentType == "Ketchup" && current == "SausageMayo") ||
//                (condimentType == "Mayo" && current == "SausageKetchup");
//            return becomesCombo ? sausageComboSprite : sausageResultSprite;
//        }

//        // ── Detection ─────────────────────────────────────────────────────────────

//        private DraggableSausage FindEligibleSausageNear(PointerEventData eventData)
//        {
//            DraggableSausage best = null;
//            float bestDist = dropRadius;
//            Camera uiCam = eventData.pressEventCamera;

//            foreach (DraggableSausage s in FindObjectsOfType<DraggableSausage>())
//            {
//                if (!s.isOnPlate) continue;
//                if (!CanReceiveCondiment(s.grillType)) continue;

//                RectTransform rt = s.GetComponent<RectTransform>();
//                if (rt == null) continue;

//                Vector2 screenPos = RectTransformUtility.WorldToScreenPoint(uiCam, rt.position);
//                float dist = Vector2.Distance(eventData.position, screenPos);
//                if (dist < bestDist) { bestDist = dist; best = s; }
//            }

//            return best;
//        }

//        private bool CanReceiveCondiment(string grillType)
//        {
//            if (condimentType == "Ketchup")
//                return grillType == "Sausage" || grillType == "SausageMayo";
//            if (condimentType == "Mayo")
//                return grillType == "Sausage" || grillType == "SausageKetchup";
//            return false;
//        }

//        // ── Movement helpers ──────────────────────────────────────────────────────

//        private void ReturnHome()
//        {
//            _rect.SetParent(_homeParent, worldPositionStays: true);
//            _rect.position = _homeWorldPos;
//            _rect.sizeDelta = _homeSizeDelta;

//            // Always restore the real bottle sprite.
//            if (_image != null && _bottleSprite != null)
//                _image.sprite = _bottleSprite;
//        }

//        private void MoveToPointer(PointerEventData eventData)
//        {
//            if (_canvas == null) _canvas = GetComponentInParent<Canvas>();
//            if (_canvas == null) return;

//            RectTransformUtility.ScreenPointToLocalPointInRectangle(
//                _canvas.GetComponent<RectTransform>(),
//                eventData.position,
//                eventData.pressEventCamera,
//                out Vector2 local);

//            // Add the grab offset so the bottle stays under the exact grab point,
//            // not snapped to the pointer with its pivot.
//            _rect.localPosition = local + _grabOffset;
//        }
//    }
//}

//using UnityEngine;
//using UnityEngine.EventSystems;
//using UnityEngine.UI;
//using System.Collections;

//namespace ToriSausages
//{
//    [RequireComponent(typeof(Image))]
//    public class CondimentBottle : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
//    {
//        [Header("Condiment Settings")]
//        [Tooltip("Use \"Ketchup\" or \"Mayo\".")]
//        public string condimentType = "Ketchup";

//        [Header("Pour Animation")]
//        public FrameAnimation pourAnimation;

//        [Header("Animation Overlay Transform")]
//        public float overlayWidth = 100f;
//        public float overlayHeight = 100f;
//        [Tooltip("Offset from the sausage centre.")]
//        public Vector2 overlayOffset = Vector2.zero;

//        [Header("Result Sprites  (applied after animation finishes)")]
//        public Sprite sausageResultSprite;
//        public Sprite sausageComboSprite;

//        [Header("Detection Radius")]
//        public float dropRadius = 120f;

//        // ── Private ───────────────────────────────────────────────────────────────

//        private Canvas _canvas;
//        private RectTransform _rect;
//        private Image _image;
//        private Sprite _bottleSprite;   // locked bottle sprite — never overwritten

//        private Vector3 _homeWorldPos;
//        private Vector2 _homeSizeDelta;
//        private Transform _homeParent;

//        // ── Unity ─────────────────────────────────────────────────────────────────

//        private void Awake()
//        {
//            _rect = GetComponent<RectTransform>();
//            _image = GetComponent<Image>();
//            _canvas = GetComponentInParent<Canvas>();
//        }

//        private void Start()
//        {
//            _homeWorldPos = _rect.position;
//            _homeSizeDelta = _rect.sizeDelta;
//            _homeParent = _rect.parent;

//            // Cache the bottle's own sprite once so we can always restore it.
//            _bottleSprite = _image != null ? _image.sprite : null;
//        }

//        // ── Drag ──────────────────────────────────────────────────────────────────

//        public void OnBeginDrag(PointerEventData eventData)
//        {
//            // Stop any AnimatorFrame on THIS object so it can't overwrite the sprite.
//            AnimatorFrame anim = GetComponent<AnimatorFrame>();
//            if (anim != null) anim.Stop();

//            // Restore the bottle's own sprite in case something changed it.
//            if (_image != null && _bottleSprite != null)
//                _image.sprite = _bottleSprite;

//            if (_canvas == null) _canvas = GetComponentInParent<Canvas>();
//            if (_canvas != null)
//            {
//                Vector3 wp = _rect.position;
//                _rect.SetParent(_canvas.transform, worldPositionStays: true);
//                _rect.position = wp;
//            }
//        }

//        public void OnDrag(PointerEventData eventData) => MoveToPointer(eventData);

//        public void OnEndDrag(PointerEventData eventData)
//        {
//            DraggableSausage target = FindEligibleSausageNear(eventData);

//            if (target != null)
//                StartCoroutine(ApplyCondimentWithAnimation(target));
//            else
//                Debug.Log("[CondimentBottle] No eligible sausage nearby — returning home.");

//            ReturnHome();
//        }

//        // ── Animation overlay ─────────────────────────────────────────────────────

//        private IEnumerator ApplyCondimentWithAnimation(DraggableSausage sausage)
//        {
//            string current = sausage.grillType;
//            string newType = GetResultType(current);
//            Sprite newSprite = GetResultSprite(current);

//            sausage.enabled = false;

//            // Create overlay as child of the sausage.
//            GameObject overlayGO = new GameObject("CondimentOverlay", typeof(RectTransform), typeof(Image));
//            overlayGO.transform.SetParent(sausage.transform, false);

//            RectTransform overlayRect = overlayGO.GetComponent<RectTransform>();
//            overlayRect.anchorMin = new Vector2(0.5f, 0.5f);
//            overlayRect.anchorMax = new Vector2(0.5f, 0.5f);
//            overlayRect.pivot = new Vector2(0.5f, 0.5f);
//            overlayRect.sizeDelta = new Vector2(overlayWidth, overlayHeight);
//            overlayRect.anchoredPosition = overlayOffset;
//            overlayRect.localScale = Vector3.one;

//            Image overlayImage = overlayGO.GetComponent<Image>();
//            overlayImage.raycastTarget = false;
//            overlayImage.color = Color.white;

//            // Play pour animation on the overlay only — never on the bottle.
//            if (pourAnimation != null && pourAnimation.frames != null && pourAnimation.frames.Length > 0)
//            {
//                overlayImage.sprite = pourAnimation.frames[0];
//                AnimatorFrame animator = overlayGO.AddComponent<AnimatorFrame>();
//                bool done = false;
//                animator.Play(pourAnimation, loop: false, onComplete: () => done = true);
//                yield return new WaitUntil(() => done);
//            }

//            Destroy(overlayGO);

//            Image sausageImage = sausage.GetComponent<Image>();
//            if (sausageImage != null && newSprite != null)
//                sausageImage.sprite = newSprite;

//            sausage.grillType = newType;
//            sausage.enabled = true;

//            Debug.Log($"[CondimentBottle] '{current}' → '{newType}'.");
//        }

//        // ── Logic helpers ─────────────────────────────────────────────────────────

//        private string GetResultType(string current)
//        {
//            if (condimentType == "Ketchup")
//                return current == "Sausage" ? "SausageKetchup" : "SausageKetchupMayo";
//            return current == "Sausage" ? "SausageMayo" : "SausageKetchupMayo";
//        }

//        private Sprite GetResultSprite(string current)
//        {
//            bool becomesCombo =
//                (condimentType == "Ketchup" && current == "SausageMayo") ||
//                (condimentType == "Mayo" && current == "SausageKetchup");
//            return becomesCombo ? sausageComboSprite : sausageResultSprite;
//        }

//        // ── Detection ─────────────────────────────────────────────────────────────

//        private DraggableSausage FindEligibleSausageNear(PointerEventData eventData)
//        {
//            DraggableSausage best = null;
//            float bestDist = dropRadius;
//            Camera uiCam = eventData.pressEventCamera;

//            foreach (DraggableSausage s in FindObjectsOfType<DraggableSausage>())
//            {
//                if (!s.isOnPlate) continue;
//                if (!CanReceiveCondiment(s.grillType)) continue;

//                RectTransform rt = s.GetComponent<RectTransform>();
//                if (rt == null) continue;

//                Vector2 screenPos = RectTransformUtility.WorldToScreenPoint(uiCam, rt.position);
//                float dist = Vector2.Distance(eventData.position, screenPos);
//                if (dist < bestDist) { bestDist = dist; best = s; }
//            }

//            return best;
//        }

//        private bool CanReceiveCondiment(string grillType)
//        {
//            if (condimentType == "Ketchup")
//                return grillType == "Sausage" || grillType == "SausageMayo";
//            if (condimentType == "Mayo")
//                return grillType == "Sausage" || grillType == "SausageKetchup";
//            return false;
//        }

//        // ── Movement helpers ──────────────────────────────────────────────────────

//        private void ReturnHome()
//        {
//            _rect.SetParent(_homeParent, worldPositionStays: true);
//            _rect.position = _homeWorldPos;
//            _rect.sizeDelta = _homeSizeDelta;

//            // Always restore the bottle sprite on return.
//            if (_image != null && _bottleSprite != null)
//                _image.sprite = _bottleSprite;
//        }

//        private void MoveToPointer(PointerEventData eventData)
//        {
//            if (_canvas == null) _canvas = GetComponentInParent<Canvas>();
//            if (_canvas == null) return;

//            RectTransformUtility.ScreenPointToLocalPointInRectangle(
//                _canvas.GetComponent<RectTransform>(),
//                eventData.position,
//                eventData.pressEventCamera,
//                out Vector2 local);

//            _rect.localPosition = local;
//        }
//    }
//}

//using UnityEngine;
//using UnityEngine.EventSystems;
//using UnityEngine.UI;
//using System.Collections;

//namespace ToriSausages
//{
//    [RequireComponent(typeof(Image))]
//    public class CondimentBottle : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
//    {
//        [Header("Condiment Settings")]
//        [Tooltip("Use \"Ketchup\" or \"Mayo\".")]
//        public string condimentType = "Ketchup";

//        [Header("Pour Animation")]
//        public FrameAnimation pourAnimation;

//        [Header("Animation Overlay Transform")]
//        public float overlayWidth = 100f;
//        public float overlayHeight = 100f;
//        [Tooltip("Offset from the sausage centre.")]
//        public Vector2 overlayOffset = Vector2.zero;

//        [Header("Result Sprites  (applied after animation finishes)")]
//        public Sprite sausageResultSprite;
//        public Sprite sausageComboSprite;

//        [Header("Detection Radius")]
//        public float dropRadius = 120f;


//        // ── Private ───────────────────────────────────────────────────────────────

//        private Canvas _canvas;
//        private RectTransform _rect;
//        private Image _image;
//        private Sprite _bottleSprite;   // locked bottle sprite — never overwritten

//        private Vector3 _homeWorldPos;
//        private Vector2 _homeSizeDelta;
//        private Transform _homeParent;

//        // ── Unity ─────────────────────────────────────────────────────────────────

//        private void Awake()
//        {
//            _rect = GetComponent<RectTransform>();
//            _image = GetComponent<Image>();
//            _canvas = GetComponentInParent<Canvas>();
//        }

//        private void Start()
//        {
//            _homeWorldPos = _rect.position;
//            _homeSizeDelta = _rect.sizeDelta;
//            _homeParent = _rect.parent;

//            // Cache the bottle's own sprite once so we can always restore it.
//            _bottleSprite = _image != null ? _image.sprite : null;
//        }

//        // ── Drag ──────────────────────────────────────────────────────────────────

//        public void OnBeginDrag(PointerEventData eventData)
//        {
//            // Stop any AnimatorFrame on THIS object so it can't overwrite the sprite.
//            AnimatorFrame anim = GetComponent<AnimatorFrame>();
//            if (anim != null) anim.Stop();

//            // Restore the bottle's own sprite in case something changed it.
//            if (_image != null && _bottleSprite != null)
//                _image.sprite = _bottleSprite;

//            if (_canvas == null) _canvas = GetComponentInParent<Canvas>();
//            if (_canvas != null)
//            {
//                Vector3 wp = _rect.position;
//                _rect.SetParent(_canvas.transform, worldPositionStays: true);
//                _rect.position = wp;
//            }
//        }

//        public void OnDrag(PointerEventData eventData) => MoveToPointer(eventData);

//        public void OnEndDrag(PointerEventData eventData)
//        {
//            DraggableSausage target = FindEligibleSausageNear(eventData);

//            if (target != null)
//                StartCoroutine(ApplyCondimentWithAnimation(target));
//            else
//                Debug.Log("[CondimentBottle] No eligible sausage nearby — returning home.");

//            ReturnHome();
//        }

//        // ── Animation overlay ─────────────────────────────────────────────────────

//        private IEnumerator ApplyCondimentWithAnimation(DraggableSausage sausage)
//        {
//            string current = sausage.grillType;
//            string newType = GetResultType(current);
//            Sprite newSprite = GetResultSprite(current);

//            sausage.enabled = false;

//            // Create overlay as child of the sausage.
//            GameObject overlayGO = new GameObject("CondimentOverlay", typeof(RectTransform), typeof(Image));
//            overlayGO.transform.SetParent(sausage.transform, false);

//            RectTransform overlayRect = overlayGO.GetComponent<RectTransform>();
//            overlayRect.anchorMin = new Vector2(0.5f, 0.5f);
//            overlayRect.anchorMax = new Vector2(0.5f, 0.5f);
//            overlayRect.pivot = new Vector2(0.5f, 0.5f);
//            overlayRect.sizeDelta = new Vector2(overlayWidth, overlayHeight);
//            overlayRect.anchoredPosition = overlayOffset;
//            overlayRect.localScale = Vector3.one;

//            Image overlayImage = overlayGO.GetComponent<Image>();
//            overlayImage.raycastTarget = false;
//            overlayImage.color = Color.white;

//            // Play pour animation on the overlay only — never on the bottle.
//            if (pourAnimation != null && pourAnimation.frames != null && pourAnimation.frames.Length > 0)
//            {
//                overlayImage.sprite = pourAnimation.frames[0];
//                AnimatorFrame animator = overlayGO.AddComponent<AnimatorFrame>();
//                bool done = false;
//                animator.Play(pourAnimation, loop: false, onComplete: () => done = true);
//                yield return new WaitUntil(() => done);
//            }

//            Destroy(overlayGO);

//            Image sausageImage = sausage.GetComponent<Image>();
//            if (sausageImage != null && newSprite != null)
//                sausageImage.sprite = newSprite;

//            sausage.grillType = newType;
//            sausage.enabled = true;

//            Debug.Log($"[CondimentBottle] '{current}' → '{newType}'.");
//        }

//        // ── Logic helpers ─────────────────────────────────────────────────────────

//        private string GetResultType(string current)
//        {
//            if (condimentType == "Ketchup")
//                return current == "Sausage" ? "SausageKetchup" : "SausageKetchupMayo";
//            return current == "Sausage" ? "SausageMayo" : "SausageKetchupMayo";
//        }

//        private Sprite GetResultSprite(string current)
//        {
//            bool becomesCombo =
//                (condimentType == "Ketchup" && current == "SausageMayo") ||
//                (condimentType == "Mayo" && current == "SausageKetchup");
//            return becomesCombo ? sausageComboSprite : sausageResultSprite;
//        }

//        // ── Detection ─────────────────────────────────────────────────────────────

//        private DraggableSausage FindEligibleSausageNear(PointerEventData eventData)
//        {
//            DraggableSausage best = null;
//            float bestDist = dropRadius;
//            Camera uiCam = eventData.pressEventCamera;

//            foreach (DraggableSausage s in FindObjectsOfType<DraggableSausage>())
//            {
//                if (!s.isOnPlate) continue;
//                if (!CanReceiveCondiment(s.grillType)) continue;

//                RectTransform rt = s.GetComponent<RectTransform>();
//                if (rt == null) continue;

//                Vector2 screenPos = RectTransformUtility.WorldToScreenPoint(uiCam, rt.position);
//                float dist = Vector2.Distance(eventData.position, screenPos);
//                if (dist < bestDist) { bestDist = dist; best = s; }
//            }

//            return best;
//        }

//        private bool CanReceiveCondiment(string grillType)
//        {
//            if (condimentType == "Ketchup")
//                return grillType == "Sausage" || grillType == "SausageMayo";
//            if (condimentType == "Mayo")
//                return grillType == "Sausage" || grillType == "SausageKetchup";
//            return false;
//        }

//        // ── Movement helpers ──────────────────────────────────────────────────────

//        private void ReturnHome()
//        {
//            _rect.SetParent(_homeParent, worldPositionStays: true);
//            _rect.position = _homeWorldPos;
//            _rect.sizeDelta = _homeSizeDelta;

//            // Always restore the bottle sprite on return.
//            if (_image != null && _bottleSprite != null)
//                _image.sprite = _bottleSprite;
//        }

//        private void MoveToPointer(PointerEventData eventData)
//        {
//            if (_canvas == null) _canvas = GetComponentInParent<Canvas>();
//            if (_canvas == null) return;

//            RectTransformUtility.ScreenPointToLocalPointInRectangle(
//                _canvas.GetComponent<RectTransform>(),
//                eventData.position,
//                eventData.pressEventCamera,
//                out Vector2 local);

//            _rect.localPosition = local;
//        }
//    }
//}

using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using System.Collections;

namespace ToriSausages
{
    /// <summary>
    /// Attach to each condiment bottle Image (ketchup or mayo).
    ///
    /// DRAG BEHAVIOUR
    ///   OnBeginDrag → bottle Image swaps to pourAnimation.frames[0]  (pour preview)
    ///                 bottle follows the pointer WITHOUT jumping — grab point is preserved
    ///   OnDrag      → bottle stays under the exact point where the player grabbed it
    ///   OnEndDrag   → bottle sprite restores to the original bottle image
    ///              → if a valid sausage is nearby:
    ///                   overlay child plays the full pour animation on the sausage,
    ///                   then sausage sprite + grillType are updated.
    ///
    /// SUPPORTED COMBINATIONS
    ///   Ketchup bottle:
    ///     "Sausage"        → "SausageKetchup"        (sausageResultSprite)
    ///     "SausageMayo"    → "SausageKetchupMayo"     (sausageComboSprite)
    ///
    ///   Mayo bottle:
    ///     "Sausage"        → "SausageMayo"            (sausageResultSprite)
    ///     "SausageKetchup" → "SausageKetchupMayo"     (sausageComboSprite)
    ///
    /// SETUP — Ketchup bottle
    ///   condimentType        = "Ketchup"
    ///   pourAnimation        = ketchup pour frames + fps
    ///   overlayWidth/Height  = size of the animation overlay on the sausage
    ///   overlayOffset        = shift overlay relative to sausage centre
    ///   sausageResultSprite  = sausage + ketchup sprite
    ///   sausageComboSprite   = sausage + ketchup + mayo sprite
    ///
    /// SETUP — Mayo bottle
    ///   condimentType        = "Mayo"
    ///   pourAnimation        = mayo pour frames + fps
    ///   sausageResultSprite  = sausage + mayo sprite
    ///   sausageComboSprite   = sausage + ketchup + mayo sprite
    /// </summary>
    [RequireComponent(typeof(Image))]
    public class CondimentBottle : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        [Header("Condiment Settings")]
        [Tooltip("Use \"Ketchup\" or \"Mayo\".")]
        public string condimentType = "Ketchup";

        [Header("Pour Animation")]
        [Tooltip("Frame 0 shows on the BOTTLE while dragging.\n" +
                 "All frames play as an overlay on the SAUSAGE after drop.")]
        public FrameAnimation pourAnimation;

        [Header("Animation Overlay Transform  (on sausage after drop)")]
        public float overlayWidth = 100f;
        public float overlayHeight = 100f;
        [Tooltip("Offset from the sausage centre.")]
        public Vector2 overlayOffset = Vector2.zero;

        [Header("Result Sprites  (applied after animation finishes)")]
        [Tooltip("Sausage sprite after this single condiment is applied.")]
        public Sprite sausageResultSprite;
        [Tooltip("Sausage sprite when BOTH condiments have been applied (combo).")]
        public Sprite sausageComboSprite;

        [Header("Detection Radius")]
        [Tooltip("Screen-pixel radius within which a drop counts as landing on a sausage.")]
        public float dropRadius = 120f;

        [Header("SFX")]
        [Tooltip("SFX key played while the pour animation plays. E.g. CondimentPour.")]
        public string pourSFXKey = "Squeez";

        [Header("Drag Offset")]
        [Tooltip("Position offset of the bottle relative to the cursor while dragging.")]
        public Vector2 dragOffset = Vector2.zero;

        // ── Private ───────────────────────────────────────────────────────────────

        private Canvas _canvas;
        private RectTransform _rect;
        private Image _image;
        private Sprite _bottleSprite;   // original bottle sprite — always restored

        private Vector3 _homeWorldPos;
        private Vector2 _homeSizeDelta;
        private Transform _homeParent;

        // Offset between the bottle pivot and the exact point where the player
        // grabbed it. Applied every frame so the bottle never jumps on pickup.
        private Vector2 _grabOffset;

        // ── Unity ─────────────────────────────────────────────────────────────────

        private void Awake()
        {
            _rect = GetComponent<RectTransform>();
            _image = GetComponent<Image>();
            _canvas = GetComponentInParent<Canvas>();
        }

        private void Start()
        {
            _homeWorldPos = _rect.position;
            _homeSizeDelta = _rect.sizeDelta;
            _homeParent = _rect.parent;

            // Cache the real bottle sprite now, before anything can change it.
            _bottleSprite = _image != null ? _image.sprite : null;
        }

        // ── Drag ──────────────────────────────────────────────────────────────────

        public void OnBeginDrag(PointerEventData eventData)
        {
            // Stop any stray AnimatorFrame on the bottle (safety net).
            AnimatorFrame existingAnim = GetComponent<AnimatorFrame>();
            if (existingAnim != null) existingAnim.Stop();

            // Show pour animation frame 0 on the bottle while dragging.
            if (_image != null && pourAnimation?.frames != null && pourAnimation.frames.Length > 0)
                _image.sprite = pourAnimation.frames[0];

            // Lift bottle to canvas root so it renders above everything.
            if (_canvas == null) _canvas = GetComponentInParent<Canvas>();
            if (_canvas != null)
            {
                Vector3 wp = _rect.position;
                _rect.SetParent(_canvas.transform, worldPositionStays: true);
                _rect.position = wp;
            }

            // Calculate grab offset AFTER reparenting so localPosition is in canvas space.
            if (_canvas != null)
            {
                RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    _canvas.GetComponent<RectTransform>(),
                    eventData.position,
                    eventData.pressEventCamera,
                    out Vector2 pointerLocal);
                // Always pin to the top of the bottle so the player can see where they're pouring.
                _grabOffset = dragOffset;
            }
            else
            {
                _grabOffset = Vector2.zero;
            }
        }

        public void OnDrag(PointerEventData eventData) => MoveToPointer(eventData);

        public void OnEndDrag(PointerEventData eventData)
        {
            // Restore bottle sprite and position BEFORE starting the sausage coroutine.
            ReturnHome();

            DraggableSausage target = FindEligibleSausageNear(eventData);

            if (target != null)
                StartCoroutine(ApplyCondimentWithAnimation(target));
            else
                Debug.Log("[CondimentBottle] No eligible sausage nearby — returning home.");
        }

        // ── Animation overlay on sausage ──────────────────────────────────────────

        private IEnumerator ApplyCondimentWithAnimation(DraggableSausage sausage)
        {
            string current = sausage.grillType;
            string newType = GetResultType(current);
            Sprite newSprite = GetResultSprite(current);

            // Block sausage dragging while animation plays.
            sausage.enabled = false;

            // ── Create overlay child on the sausage ───────────────────────────────
            GameObject overlayGO = new GameObject("CondimentOverlay",
                                                   typeof(RectTransform), typeof(Image));
            overlayGO.transform.SetParent(sausage.transform, false);

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

            // ── Play full pour animation on the overlay ───────────────────────────
            if (pourAnimation != null && pourAnimation.frames != null &&
                pourAnimation.frames.Length > 0)
            {
                overlayImage.sprite = pourAnimation.frames[0];
                AnimatorFrame animator = overlayGO.AddComponent<AnimatorFrame>();
                bool done = false;
                animator.Play(pourAnimation, loop: false, onComplete: () => done = true);
                if (!string.IsNullOrEmpty(pourSFXKey))
                    SoundManager.Instance?.PlaySFX(pourSFXKey);
                yield return new WaitUntil(() => done);
            }

            // ── Cleanup & swap sausage sprite ─────────────────────────────────────
            Destroy(overlayGO);

            Image sausageImage = sausage.GetComponent<Image>();
            if (sausageImage != null && newSprite != null)
                sausageImage.sprite = newSprite;

            sausage.grillType = newType;
            sausage.enabled = true;

            ToriSausageCookTutorialManager.Instance?.NotifyCondimentApplied();
            Debug.Log($"[CondimentBottle] '{current}' → '{newType}'.");
        }

        // ── Logic helpers ─────────────────────────────────────────────────────────

        private string GetResultType(string current)
        {
            if (condimentType == "Ketchup")
                return current == "Sausage" ? "SausageKetchup" : "SausageKetchupMayo";
            // Mayo
            return current == "Sausage" ? "SausageMayo" : "SausageKetchupMayo";
        }

        private Sprite GetResultSprite(string current)
        {
            bool becomesCombo =
                (condimentType == "Ketchup" && current == "SausageMayo") ||
                (condimentType == "Mayo" && current == "SausageKetchup");
            return becomesCombo ? sausageComboSprite : sausageResultSprite;
        }

        // ── Detection ─────────────────────────────────────────────────────────────

        private DraggableSausage FindEligibleSausageNear(PointerEventData eventData)
        {
            DraggableSausage best = null;
            float bestDist = dropRadius;
            Camera uiCam = eventData.pressEventCamera;

            foreach (DraggableSausage s in FindObjectsOfType<DraggableSausage>())
            {
                if (!s.isOnPlate) continue;
                if (!CanReceiveCondiment(s.grillType)) continue;

                RectTransform rt = s.GetComponent<RectTransform>();
                if (rt == null) continue;

                Vector2 screenPos = RectTransformUtility.WorldToScreenPoint(uiCam, rt.position);
                float dist = Vector2.Distance(eventData.position, screenPos);
                if (dist < bestDist) { bestDist = dist; best = s; }
            }

            return best;
        }

        private bool CanReceiveCondiment(string grillType)
        {
            if (condimentType == "Ketchup")
                return grillType == "Sausage" || grillType == "SausageMayo";
            if (condimentType == "Mayo")
                return grillType == "Sausage" || grillType == "SausageKetchup";
            return false;
        }

        // ── Movement helpers ──────────────────────────────────────────────────────

        private void ReturnHome()
        {
            _rect.SetParent(_homeParent, worldPositionStays: true);
            _rect.position = _homeWorldPos;
            _rect.sizeDelta = _homeSizeDelta;

            // Always restore the real bottle sprite.
            if (_image != null && _bottleSprite != null)
                _image.sprite = _bottleSprite;
        }

        private void MoveToPointer(PointerEventData eventData)
        {
            if (_canvas == null) _canvas = GetComponentInParent<Canvas>();
            if (_canvas == null) return;

            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                _canvas.GetComponent<RectTransform>(),
                eventData.position,
                eventData.pressEventCamera,
                out Vector2 local);

            // Add the grab offset so the bottle stays under the exact grab point,
            // not snapped to the pointer with its pivot.
            _rect.localPosition = local + _grabOffset;
        }
    }
}