using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace ToriSausages
{
    /// <summary>
    /// Attach to BOTH the raw sausage prefab root AND the cooked child object.
    ///
    /// THREE DRAG MODES
    /// ─────────────────────────────────────────────────────────────────────
    /// MODE A  isCooked=false
    ///   Raw sausage (SpawnerGrill) → dragged to stove slot.
    ///
    /// MODE B  isCooked=true, isOnPlate=false
    ///   Cooked sausage → dragged to a ControllerPlate slot.
    ///   ControllerPlate moves it to the canvas root and sets isOnPlate=true.
    ///
    /// MODE C  isCooked=true, isOnPlate=true
    ///   Plated sausage → dragged to the OWNER customer's mouthPoint.
    ///   Only accepted if owningPlate.ownerCustomer matches the target customer.
    ///   OR dragged to the ControllerDustbin to discard unwanted food.
    /// ─────────────────────────────────────────────────────────────────────
    ///
    /// CHANGES
    ///   • DropOnMouth() passes owningPlate to TryFulfillOrder() — customer
    ///     rejects food that did not come from their own plate.
    ///   • DropOnMouth() also checks for a dustbin drop so unwanted/wrong food
    ///     can be discarded without trapping it on the plate forever.
    ///   • DropOnPlate() likewise checks for dustbin — lets the player trash a
    ///     cooked sausage before it ever lands on a plate.
    ///   • OnBeginDrag() now blocks raw sausage (isCooked == false) drags while
    ///     ControllerStove.InputLocked is true (intro chathead popup active).
    /// </summary>
    public class DraggableSausage : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        // ── Set by SpawnerGrill / CookerGrill ─────────────────────────────────────
        [HideInInspector] public string grillType = "";
        [HideInInspector] public bool isCooked = false;
        [HideInInspector] public bool isOnPlate = false;

        /// <summary>The ControllerPlate currently holding this sausage.</summary>
        [HideInInspector] public ControllerPlate owningPlate = null;

        // ── Pulse (written by CookerGrill BEFORE SetActive) ───────────────────────
        [HideInInspector] public bool pendingPulse = false;
        [HideInInspector] public float pendingPulseSpeed = 3f;
        [HideInInspector] public float pendingPulseAmount = 0.07f;

        // ── Inspector ──────────────────────────────────────────────────────────────
        [Header("Alpha Hit-Test  (requires Read/Write Enabled on the texture)")]
        [Range(0f, 1f)]
        public float alphaThreshold = 0.1f;

        [Header("Drag Visual Override  (0,0 = keep prefab size/offset)")]
        public Vector2 draggingSize = Vector2.zero;
        public Vector2 draggingOffset = Vector2.zero;

        // ── Private ───────────────────────────────────────────────────────────────
        private Canvas _canvas;
        private RectTransform _rect;
        private CanvasGroup _cg;
        private Image _image;

        private Transform _preDragParent;
        private Vector3 _preDragWorldPos;
        private Vector2 _preDragSizeDelta;

        private ControllerStove _hoverStove;
        private int _hoverSlot = -1;

        private ControllerPlate _hoverPlate;
        private int _hoverPlateSlot = -1;

        private ControllerCustomer _hoverCustomer;

        /// <summary>Occupied sausage we're about to swap with, set during TrackPlateHint.</summary>
        private DraggableSausage _swapTarget;

        private Coroutine _pulseRoutine;

        // ── Unity ─────────────────────────────────────────────────────────────────

        private void Awake() => InitRefs();

        private void InitRefs()
        {
            if (_rect == null) _rect = GetComponent<RectTransform>();
            if (_cg == null) _cg = GetComponent<CanvasGroup>();
            if (_image == null) _image = GetComponent<Image>();

            if (_canvas == null) _canvas = GetComponentInParent<Canvas>();
            if (_canvas == null) _canvas = FindObjectOfType<Canvas>();

            TrySetAlphaHitTest();
            if (_cg != null) _cg.blocksRaycasts = false;
        }

        private void OnEnable()
        {
            if (pendingPulse)
            {
                pendingPulse = false;
                StartPulse(pendingPulseSpeed, pendingPulseAmount);
            }
        }

        private void TrySetAlphaHitTest()
        {
            if (_image == null || alphaThreshold <= 0f) return;
            Sprite s = _image.sprite;
            if (s != null && s.texture != null && s.texture.isReadable)
                _image.alphaHitTestMinimumThreshold = alphaThreshold;
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (_image == null) _image = GetComponent<Image>();
            TrySetAlphaHitTest();
        }
#endif

        // ── Pulse ─────────────────────────────────────────────────────────────────

        public void StartPulse(float speed = 3f, float amount = 0.07f)
        {
            StopPulse();
            _pulseRoutine = StartCoroutine(PulseRoutine(speed, amount));
        }

        public void StopPulse()
        {
            if (_pulseRoutine == null) return;
            StopCoroutine(_pulseRoutine);
            _pulseRoutine = null;
            if (_rect != null) _rect.localScale = Vector3.one;
        }

        private System.Collections.IEnumerator PulseRoutine(float speed, float amount)
        {
            float t = 0f;
            while (true)
            {
                t += Time.deltaTime * speed;
                float s = 1f + Mathf.Sin(t) * amount;
                if (_rect != null) _rect.localScale = new Vector3(s, s, 1f);
                yield return null;
            }
        }

        // ── Origin save / restore ─────────────────────────────────────────────────

        private void SaveOrigin()
        {
            if (_rect == null) { Debug.LogError("[DraggableSausage] SaveOrigin: _rect null."); return; }
            _preDragParent = _rect.parent;
            _preDragWorldPos = _rect.position;
            _preDragSizeDelta = _rect.sizeDelta;
        }

        public void ReturnToOrigin()
        {
            if (_rect == null) return;
            if (_preDragParent != null)
            {
                _rect.SetParent(_preDragParent, false);
                _rect.position = _preDragWorldPos;
                _rect.sizeDelta = _preDragSizeDelta;
            }
            _rect.localScale = Vector3.one;
            if (_cg != null) { _cg.blocksRaycasts = true; _cg.interactable = true; _cg.alpha = 1f; }
            if (_image != null) _image.raycastTarget = true;
            if (isCooked) StartPulse(pendingPulseSpeed, pendingPulseAmount);
            Debug.Log($"[DraggableSausage] '{grillType}' returned to origin.");
        }

        // ── SpawnerGrill entry point ───────────────────────────────────────────────

        public void BeginDragFromSpawner(PointerEventData eventData)
        {
            InitRefs();
            SaveOrigin();
            ApplyDragSize();
            if (_cg != null) _cg.blocksRaycasts = false;
            MoveToPointer(eventData);
        }

        // ── IBeginDragHandler ──────────────────────────────────────────────────────

        public void OnBeginDrag(PointerEventData eventData)
        {
            InitRefs();

            // Block raw sausage drags while the intro chathead popup is active
            // and waiting for the player to press OK.
            if (!isCooked && ControllerStove.InputLocked) return;

            StopPulse();
            SaveOrigin();
            GetComponent<BurnerSausage>()?.SetDragging(true);
            if (!isCooked) return;

            // Lift to canvas root so MoveToPointer's localPosition (computed in the
            // canvas's local space) is applied in the same space — otherwise a
            // sausage parented to a plate-slot anchor jumps by that anchor's offset.
            if (_canvas != null)
            {
                Vector3 worldPos = _rect.position;
                _rect.SetParent(_canvas.transform, worldPositionStays: true);
                _rect.position = worldPos;
            }

            ApplyDragSize();
            if (_cg != null) _cg.blocksRaycasts = false;
            MoveToPointer(eventData);
        }

        // ── IDragHandler ───────────────────────────────────────────────────────────

        public void OnDrag(PointerEventData eventData)
        {
            MoveToPointer(eventData);
            if (!isCooked) TrackStoveHint(eventData);
            else if (isOnPlate) { TrackMouthHint(eventData); TrackSwapHint(eventData); }
            else TrackPlateHint(eventData);
        }

        // ── IEndDragHandler ────────────────────────────────────────────────────────

        public void OnEndDrag(PointerEventData eventData)
        {
            if (_cg != null) _cg.blocksRaycasts = true;

            if (!isCooked)
            {
                GetComponent<BurnerSausage>()?.SetDragging(false);
                DropOnStove(eventData);
            }
            else if (isOnPlate)
            {
                DropOnMouth(eventData);
                ClearMouthHint();
            }
            else
            {
                DropOnPlate(eventData);
                ClearPlateHint();
            }
        }

        // ══ MODE A — raw sausage → stove ═════════════════════════════════════════

        private void DropOnStove(PointerEventData eventData)
        {
            _hoverStove?.HideAllHints();
            ControllerStove stove = FindTarget<ControllerStove>(eventData);
            if (stove != null && _hoverSlot >= 0) stove.TryPlaceGrill(this, _hoverSlot);
            else ReturnToOrigin();
            _hoverStove = null; _hoverSlot = -1;
        }

        private void TrackStoveHint(PointerEventData eventData)
        {
            ControllerStove stove = FindTarget<ControllerStove>(eventData);
            if (stove == null) { _hoverStove?.HideAllHints(); _hoverStove = null; _hoverSlot = -1; return; }
            _hoverStove = stove;
            int slot = stove.GetHoveredSlot(eventData.position, eventData.pressEventCamera);
            if (slot == _hoverSlot) return;
            _hoverSlot = slot;
            if (slot >= 0) stove.ShowSlotHint(slot, _image != null ? _image.sprite : null);
            else stove.HideAllHints();
        }

        // ══ MODE B — cooked sausage → plate (or dustbin) ══════════════════════════

        private void DropOnPlate(PointerEventData eventData)
        {
            // Allow discarding an unwanted cooked sausage into the dustbin directly.
            ControllerDustbin dustbin = FindObjectOfType<ControllerDustbin>();
            if (dustbin != null && dustbin.IsInsideTarget(eventData.position, eventData.pressEventCamera))
            {
                Debug.Log($"[DraggableSausage] '{grillType}' discarded to dustbin (pre-plate).");
                GetComponent<BurnerSausage>()?.SetDragging(false);
                dustbin.ReceiveCookedSausage(this);
                return;
            }

            if (_hoverPlate != null && _hoverPlateSlot >= 0)
            {
                bool placed = ManagerPlate.Instance != null
                    ? ManagerPlate.Instance.PlaceGrill(this, _hoverPlate, _hoverPlateSlot)
                    : _hoverPlate.TryReceiveGrill(this, _hoverPlateSlot);

                if (placed)
                {
                    BurnerSausage burner = GetComponent<BurnerSausage>();
                    if (burner != null) burner.enabled = false;
                }
                else
                {
                    GetComponent<BurnerSausage>()?.SetDragging(false);
                    ReturnToOrigin();
                }
            }
            else
            {
                GetComponent<BurnerSausage>()?.SetDragging(false);
                ReturnToOrigin();
            }
        }

        private void TrackPlateHint(PointerEventData eventData)
        {
            if (ManagerPlate.Instance == null) return;
            var (plate, slot) = ManagerPlate.Instance.GetNearestPlateAndSlot(
                eventData.position, eventData.pressEventCamera);

            if (plate == null || slot < 0)
            {
                if (_hoverPlate != null) { ManagerPlate.Instance.HideAllHints(); _hoverPlate = null; _hoverPlateSlot = -1; }
                return;
            }
            if (plate == _hoverPlate && slot == _hoverPlateSlot) return;
            _hoverPlate = plate; _hoverPlateSlot = slot;
            ManagerPlate.Instance.ShowHint(plate, slot, _image != null ? _image.sprite : null);
        }

        private void ClearPlateHint()
        {
            ManagerPlate.Instance?.HideAllHints();
            _hoverPlate = null; _hoverPlateSlot = -1;
            _swapTarget = null;
        }

        // ══ MODE C — plated sausage → customer mouth (or dustbin) ════════════════

        private void DropOnMouth(PointerEventData eventData)
        {
            // ── Swap check — player dropped onto another sausage on the same plate ──
            if (_swapTarget != null && owningPlate != null)
            {
                int mySlot = owningPlate.GetSlotIndexOf(this);
                int targetSlot = owningPlate.GetSlotIndexOf(_swapTarget);
                if (mySlot >= 0 && targetSlot >= 0)
                {
                    owningPlate.SwapSlots(mySlot, targetSlot);
                    _swapTarget = null;
                    ClearMouthHint();
                    return;
                }
            }
            _swapTarget = null;

            // ── Dustbin check — discard unwanted / wrong food ──────────────────────
            ControllerDustbin dustbin = FindObjectOfType<ControllerDustbin>();
            if (dustbin != null && dustbin.IsInsideTarget(eventData.position, eventData.pressEventCamera))
            {
                Debug.Log($"[DraggableSausage] '{grillType}' discarded to dustbin (from plate).");
                owningPlate?.FreeSlotOf(this);
                dustbin.ReceiveCookedSausage(this);
                return;
            }

            // ── Mouth drop ────────────────────────────────────────────────────────
            if (_hoverCustomer != null)
            {
                // Pass owningPlate so the customer can verify it came from their plate.
                bool accepted = _hoverCustomer.TryFulfillOrder(grillType, owningPlate);
                if (accepted)
                {
                    owningPlate?.FreeSlotOf(this);
                    CharacterCook.Instance?.PlayHappy();
                    ToriSausageCookTutorialManager.Instance?.NotifyFoodDeliveredToMouth();
                    Destroy(gameObject);
                    return;
                }

                // Rejected — either wrong food type or wrong plate.
                Debug.Log($"[DraggableSausage] '{grillType}' not accepted by '{_hoverCustomer.name}'. " +
                          $"Returning to plate.");
            }
            else
            {
                Debug.Log("[DraggableSausage] Missed all mouths — returning to plate.");
            }

            ReturnToOrigin();
            _hoverCustomer = null;
        }

        private void TrackMouthHint(PointerEventData eventData)
        {
            if (ManagerCustomer.Instance == null) return;
            var (customer, _) = ManagerCustomer.Instance.GetNearestMouth(
                eventData.position, eventData.pressEventCamera);
            if (customer == _hoverCustomer) return;
            _hoverCustomer?.SetMouthHint(false);
            _hoverCustomer = customer;
            _hoverCustomer?.SetMouthHint(true);
        }

        private void ClearMouthHint()
        {
            _hoverCustomer?.SetMouthHint(false);
            _hoverCustomer = null;
            _swapTarget = null;
        }

        // ── Swap hint (Mode C — plated sausage hovering over another plated sausage) ─

        private void TrackSwapHint(PointerEventData eventData)
        {
            if (owningPlate == null || ManagerPlate.Instance == null) return;

            var (swapTarget, _) = owningPlate.GetNearestOccupiedSlot(
                eventData.position, eventData.pressEventCamera,
                ManagerPlate.Instance.slotDetectionRadius, this);

            _swapTarget = swapTarget;
        }

        // ── Helpers ───────────────────────────────────────────────────────────────

        private void ApplyDragSize()
        {
            if (_rect != null && draggingSize != Vector2.zero) _rect.sizeDelta = draggingSize;
        }

        private void MoveToPointer(PointerEventData eventData)
        {
            if (_canvas == null)
            {
                _canvas = GetComponentInParent<Canvas>();
                if (_canvas == null) _canvas = FindObjectOfType<Canvas>();
            }
            if (_canvas == null || _rect == null) return;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                _canvas.GetComponent<RectTransform>(),
                eventData.position, eventData.pressEventCamera, out Vector2 local);
            _rect.localPosition = local + draggingOffset;
        }

        private T FindTarget<T>(PointerEventData eventData) where T : Component
        {
            var hits = new System.Collections.Generic.List<RaycastResult>();
            EventSystem.current.RaycastAll(eventData, hits);
            foreach (var hit in hits)
            {
                T t = hit.gameObject.GetComponentInParent<T>();
                if (t != null) return t;
            }
            return null;
        }
    }
}