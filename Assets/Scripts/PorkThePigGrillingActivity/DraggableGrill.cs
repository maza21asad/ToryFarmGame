//using UnityEngine;
//using UnityEngine.EventSystems;
//using UnityEngine.UI;

///// <summary>
///// Attach to BOTH the raw grill prefab root AND the cooked child object.
/////
///// THREE DRAG MODES
///// ────────────────────────────────────────────────────────────────────
///// MODE A  isCooked=false
/////   Raw grill spawned by GrillSpawner → dragged to stove.
/////
///// MODE B  isCooked=true, isOnPlate=false
/////   Cooked grill → dragged to a PlateController slot.
/////   PlateController.SnapGrill() places it at the canvas root (same world pos)
/////   and sets isOnPlate=true.
/////
///// MODE C  isCooked=true, isOnPlate=true
/////   Grill already sitting at canvas root (placed there by PlateController).
/////   Player drags it to a customer's mouthPoint.
/////   SUCCESS → eat anim + pig happy + Destroy.
/////   FAIL    → ReturnToOrigin() snaps it back to its plate position.
///// ────────────────────────────────────────────────────────────────────
///// </summary>
//public class DraggableGrill : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
//{
//    // ── Set by GrillSpawner / GrillCooker ────────────────────────────────────
//    [HideInInspector] public string grillType = "";
//    [HideInInspector] public bool isCooked = false;

//    /// <summary>Set true by PlateController after snapping onto a plate.</summary>
//    [HideInInspector] public bool isOnPlate = false;

//    /// <summary>
//    /// The PlateController that currently holds this grill.
//    /// Set by PlateController.SnapGrill(). Cleared and freed on successful delivery.
//    /// </summary>
//    [HideInInspector] public PlateController owningPlate = null;

//    // ── Pulse (written by GrillCooker BEFORE SetActive) ──────────────────────
//    [HideInInspector] public bool pendingPulse = false;
//    [HideInInspector] public float pendingPulseSpeed = 3f;
//    [HideInInspector] public float pendingPulseAmount = 0.07f;

//    // ── Inspector ──────────────────────────────────────────────────────────────
//    [Header("Alpha Hit-Test  (requires Read/Write Enabled on the texture)")]
//    [Range(0f, 1f)]
//    public float alphaThreshold = 0.1f;

//    [Header("Drag Visual Override  (0,0 = keep prefab size/offset)")]
//    public Vector2 draggingSize = Vector2.zero;
//    public Vector2 draggingOffset = Vector2.zero;

//    // ── Private ───────────────────────────────────────────────────────────────
//    private Canvas _canvas;
//    private RectTransform _rect;
//    private CanvasGroup _cg;
//    private Image _image;

//    // Saved at drag-start; restored on failed drop.
//    private Transform _preDragParent;
//    private Vector3 _preDragWorldPos;
//    private Vector2 _preDragSizeDelta;

//    // Mode A hover
//    private StoveController _hoverStove;
//    private int _hoverSlot = -1;

//    // Mode B hover (distance-based, no raycasts)
//    private PlateController _hoverPlate;
//    private int _hoverPlateSlot = -1;

//    // Mode C hover
//    private CustomerController _hoverCustomer;

//    private Coroutine _pulseRoutine;

//    // ── Unity ─────────────────────────────────────────────────────────────────

//    private void Awake()
//    {
//        _rect = GetComponent<RectTransform>();
//        _cg = GetComponent<CanvasGroup>();
//        _canvas = GetComponentInParent<Canvas>();
//        _image = GetComponent<Image>();

//        TrySetAlphaHitTest();

//        // Off by default — GrillCooker turns it on after cooking.
//        if (_cg != null) _cg.blocksRaycasts = false;
//    }

//    private void OnEnable()
//    {
//        if (pendingPulse)
//        {
//            pendingPulse = false;
//            StartPulse(pendingPulseSpeed, pendingPulseAmount);
//        }
//    }

//    private void TrySetAlphaHitTest()
//    {
//        if (_image == null || alphaThreshold <= 0f) return;
//        Sprite s = _image.sprite;
//        if (s != null && s.texture != null && s.texture.isReadable)
//            _image.alphaHitTestMinimumThreshold = alphaThreshold;
//    }

//#if UNITY_EDITOR
//    private void OnValidate()
//    {
//        if (_image == null) _image = GetComponent<Image>();
//        TrySetAlphaHitTest();
//    }
//#endif

//    // ── Pulse ─────────────────────────────────────────────────────────────────

//    public void StartPulse(float speed = 3f, float amount = 0.07f)
//    {
//        StopPulse();
//        _pulseRoutine = StartCoroutine(PulseRoutine(speed, amount));
//    }

//    public void StopPulse()
//    {
//        if (_pulseRoutine == null) return;
//        StopCoroutine(_pulseRoutine);
//        _pulseRoutine = null;
//        if (_rect != null) _rect.localScale = Vector3.one;
//    }

//    private System.Collections.IEnumerator PulseRoutine(float speed, float amount)
//    {
//        float t = 0f;
//        while (true)
//        {
//            t += Time.deltaTime * speed;
//            float s = 1f + Mathf.Sin(t) * amount;
//            if (_rect != null) _rect.localScale = new Vector3(s, s, 1f);
//            yield return null;
//        }
//    }

//    // ── Origin save / restore ─────────────────────────────────────────────────

//    private void SaveOrigin()
//    {
//        _preDragParent = _rect.parent;
//        _preDragWorldPos = _rect.position;
//        _preDragSizeDelta = _rect.sizeDelta;
//    }

//    /// <summary>
//    /// Returns the grill to its exact pre-drag state.
//    /// For Mode C this snaps it back to its plate position at the canvas root.
//    /// </summary>
//    public void ReturnToOrigin()
//    {
//        if (_preDragParent != null)
//        {
//            _rect.SetParent(_preDragParent, false);
//            _rect.position = _preDragWorldPos;
//            _rect.sizeDelta = _preDragSizeDelta;
//        }

//        _rect.localScale = Vector3.one;

//        if (_cg != null)
//        {
//            _cg.blocksRaycasts = true;
//            _cg.interactable = true;
//            _cg.alpha = 1f;
//        }

//        if (_image != null)
//            _image.raycastTarget = true;

//        if (isCooked)
//            StartPulse(pendingPulseSpeed, pendingPulseAmount);

//        Debug.Log($"[DraggableGrill] '{grillType}' returned to origin.");
//    }

//    // ── GrillSpawner entry point (raw grills only) ────────────────────────────

//    public void BeginDragFromSpawner(PointerEventData eventData)
//    {
//        SaveOrigin();
//        RefreshCanvas();
//        ApplyDragSize();
//        if (_cg != null) _cg.blocksRaycasts = false;
//        MoveToPointer(eventData);
//    }

//    // ── IBeginDragHandler ──────────────────────────────────────────────────────

//    public void OnBeginDrag(PointerEventData eventData)
//    {
//        StopPulse();
//        SaveOrigin();

//        // Pause the burn timer while the player is holding the grill.
//        GetComponent<BurnerGrill>()?.SetDragging(true);

//        // Raw grills are controlled entirely by GrillSpawner.
//        if (!isCooked) return;

//        // Mode B & C: cooked grill picked up.
//        // The grill is already at the canvas root (placed there by PlateController
//        // in Mode C, or by GrillCooker in Mode B), so no reparenting needed here.
//        RefreshCanvas();
//        ApplyDragSize();
//        if (_cg != null) _cg.blocksRaycasts = false;
//        MoveToPointer(eventData);
//    }

//    // ── IDragHandler ───────────────────────────────────────────────────────────

//    public void OnDrag(PointerEventData eventData)
//    {
//        MoveToPointer(eventData);

//        if (!isCooked)
//            TrackStoveHint(eventData);      // Mode A
//        else if (isOnPlate)
//            TrackMouthHint(eventData);      // Mode C
//        else
//            TrackPlateHint(eventData);      // Mode B
//    }

//    // ── IEndDragHandler ────────────────────────────────────────────────────────

//    //public void OnEndDrag(PointerEventData eventData)
//    //{
//    //    if (_cg != null) _cg.blocksRaycasts = true;

//    //    // Resume burn timer now that the player has let go.
//    //    GetComponent<BurnerGrill>()?.SetDragging(false);

//    //    if (!isCooked)
//    //    {
//    //        DropOnStove(eventData);
//    //    }
//    //    else if (isOnPlate)
//    //    {
//    //        DropOnMouth();
//    //        ClearMouthHint();
//    //    }
//    //    else
//    //    {
//    //        DropOnPlate();
//    //        ClearPlateHint();
//    //    }
//    //}

//    public void OnEndDrag(PointerEventData eventData)
//    {
//        if (_cg != null) _cg.blocksRaycasts = true;

//        if (!isCooked)
//        {
//            GetComponent<BurnerGrill>()?.SetDragging(false); // raw grill — resume timer
//            DropOnStove(eventData);
//        }
//        else if (isOnPlate)
//        {
//            // Mode C — plated cooked grill. Check dustbin first, then customer mouth.
//            ClearMouthHint();
//            if (TryDropOnDustbin(eventData))
//                return;
//            DropOnMouth();
//        }
//        else
//        {
//            // Mode B — freshly cooked, not yet plated. Check dustbin first, then plate.
//            ClearPlateHint();
//            if (TryDropOnDustbin(eventData))
//                return;
//            DropOnPlate();   // cooked grill — DropOnPlate handles the burn timer itself
//        }
//    }

//    // ── Dustbin drop (cooked or burned) ───────────────────────────────────────

//    /// <summary>
//    /// Tries to drop this cooked/burned grill on the dustbin.
//    /// Returns true and destroys self if the drop lands on the dustbin.
//    /// Returns false so the caller can fall through to the normal logic.
//    /// </summary>
//    private bool TryDropOnDustbin(PointerEventData eventData)
//    {
//        DustbinController dustbin = FindObjectOfType<DustbinController>();
//        if (dustbin == null) return false;

//        if (!dustbin.IsInsideTarget(eventData.position, eventData.pressEventCamera))
//            return false;

//        // Free the plate slot if this grill was on a plate.
//        owningPlate?.FreeSlotOf(this);

//        // Stop the burn timer.
//        BurnerGrill burner = GetComponent<BurnerGrill>();
//        if (burner != null) burner.enabled = false;

//        // Log and update dustbin UI.
//        dustbin.ReceiveCookedGrill(grillType);

//        Destroy(gameObject);
//        return true;
//    }

//    // ══ MODE A — raw grill → stove ════════════════════════════════════════════

//    private void DropOnStove(PointerEventData eventData)
//    {
//        _hoverStove?.HideAllHints();
//        StoveController stove = FindTarget<StoveController>(eventData);

//        if (stove != null && _hoverSlot >= 0)
//            stove.TryPlaceGrill(this, _hoverSlot);
//        else
//            ReturnToOrigin();

//        _hoverStove = null;
//        _hoverSlot = -1;
//    }

//    private void TrackStoveHint(PointerEventData eventData)
//    {
//        StoveController stove = FindTarget<StoveController>(eventData);

//        if (stove == null)
//        {
//            _hoverStove?.HideAllHints();
//            _hoverStove = null;
//            _hoverSlot = -1;
//            return;
//        }

//        _hoverStove = stove;
//        int slot = stove.GetHoveredSlot(eventData.position, eventData.pressEventCamera);

//        if (slot == _hoverSlot) return;
//        _hoverSlot = slot;

//        if (slot >= 0)
//            stove.ShowSlotHint(slot, _image != null ? _image.sprite : null);
//        else
//            stove.HideAllHints();
//    }

//    // ══ MODE B — cooked grill → plate ════════════════════════════════════════

//    private void DropOnPlate()
//    {
//        if (_hoverPlate != null && _hoverPlateSlot >= 0)
//        {
//            bool placed = PlateManager.Instance != null
//                ? PlateManager.Instance.PlaceGrill(this, _hoverPlate, _hoverPlateSlot)
//                : _hoverPlate.TryReceiveGrill(this, _hoverPlateSlot);

//            if (placed)
//            {
//                // Grill is now on the plate — stop the burn timer permanently
//                BurnerGrill burner = GetComponent<BurnerGrill>();
//                if (burner != null) burner.enabled = false;
//            }
//            else
//            {
//                GetComponent<BurnerGrill>()?.SetDragging(false); // failed — resume timer
//                ReturnToOrigin();
//            }
//        }
//        else
//        {
//            GetComponent<BurnerGrill>()?.SetDragging(false); // missed — resume timer
//            ReturnToOrigin();
//        }
//    }

//    private void TrackPlateHint(PointerEventData eventData)
//    {
//        if (PlateManager.Instance == null) return;

//        var (plate, slot) = PlateManager.Instance.GetNearestPlateAndSlot(
//            eventData.position, eventData.pressEventCamera);

//        if (plate == null || slot < 0)
//        {
//            if (_hoverPlate != null)
//            {
//                PlateManager.Instance.HideAllHints();
//                _hoverPlate = null;
//                _hoverPlateSlot = -1;
//            }
//            return;
//        }

//        if (plate == _hoverPlate && slot == _hoverPlateSlot) return;
//        _hoverPlate = plate;
//        _hoverPlateSlot = slot;
//        PlateManager.Instance.ShowHint(plate, slot, _image != null ? _image.sprite : null);
//    }

//    private void ClearPlateHint()
//    {
//        PlateManager.Instance?.HideAllHints();
//        _hoverPlate = null;
//        _hoverPlateSlot = -1;
//    }

//    // ══ MODE C — plated grill → customer mouth ════════════════════════════════

//    /// <summary>
//    /// SUCCESS  → grill type matches an open order slot:
//    ///   eat animation plays, pig plays happy, grill is Destroyed.
//    /// FAILURE  → grill type doesn't match, or pointer missed all mouths:
//    ///   ReturnToOrigin() snaps it back to its plate position.
//    /// </summary>
//    private void DropOnMouth()
//    {
//        if (_hoverCustomer != null)
//        {
//            bool accepted = _hoverCustomer.TryFulfillOrder(grillType, owningPlate);
//            if (accepted)
//            {
//                // Free the plate slot so it can be reused immediately.
//                owningPlate?.FreeSlotOf(this);

//                CookCharacter.Instance?.PlayHappy();
//                Destroy(gameObject);
//                return;
//            }
//            Debug.Log($"[DraggableGrill] '{grillType}' not accepted — returning to plate.");
//        }
//        else
//        {
//            Debug.Log("[DraggableGrill] Missed all mouths — returning to plate.");
//        }

//        ReturnToOrigin();
//        _hoverCustomer = null;
//    }

//    private void TrackMouthHint(PointerEventData eventData)
//    {
//        if (CustomerManager.Instance == null) return;

//        var (customer, _) = CustomerManager.Instance.GetNearestMouth(
//            eventData.position, eventData.pressEventCamera);

//        if (customer == _hoverCustomer) return;

//        _hoverCustomer?.SetMouthHint(false);
//        _hoverCustomer = customer;
//        _hoverCustomer?.SetMouthHint(true);
//    }

//    private void ClearMouthHint()
//    {
//        _hoverCustomer?.SetMouthHint(false);
//        _hoverCustomer = null;
//    }

//    // ── Helpers ───────────────────────────────────────────────────────────────

//    private void ApplyDragSize()
//    {
//        if (_rect != null && draggingSize != Vector2.zero)
//            _rect.sizeDelta = draggingSize;
//    }

//    private void RefreshCanvas()
//    {
//        _canvas = GetComponentInParent<Canvas>();
//        if (_canvas == null)
//            _canvas = FindObjectOfType<Canvas>();
//    }

//    private void MoveToPointer(PointerEventData eventData)
//    {
//        if (_canvas == null) RefreshCanvas();
//        if (_canvas == null) return;

//        RectTransformUtility.ScreenPointToLocalPointInRectangle(
//            _canvas.GetComponent<RectTransform>(),
//            eventData.position,
//            eventData.pressEventCamera,
//            out Vector2 local);

//        _rect.localPosition = local + draggingOffset;
//    }

//    private T FindTarget<T>(PointerEventData eventData) where T : Component
//    {
//        var hits = new System.Collections.Generic.List<RaycastResult>();
//        EventSystem.current.RaycastAll(eventData, hits);
//        foreach (var hit in hits)
//        {
//            T t = hit.gameObject.GetComponentInParent<T>();
//            if (t != null) return t;
//        }
//        return null;
//    }
//}

//using UnityEngine;
//using UnityEngine.EventSystems;
//using UnityEngine.UI;

///// <summary>
///// Attach to BOTH the raw grill prefab root AND the cooked child object.
/////
///// THREE DRAG MODES
///// ────────────────────────────────────────────────────────────────────
///// MODE A  isCooked=false
/////   Raw grill spawned by GrillSpawner → dragged to stove.
/////
///// MODE B  isCooked=true, isOnPlate=false
/////   Cooked grill → dragged to a PlateController slot.
/////   PlateController.SnapGrill() places it at the canvas root (same world pos)
/////   and sets isOnPlate=true.
/////
///// MODE C  isCooked=true, isOnPlate=true
/////   Grill already sitting at canvas root (placed there by PlateController).
/////   Player drags it to a customer's mouthPoint.
/////   SUCCESS → eat anim + pig happy + Destroy.
/////   FAIL    → ReturnToOrigin() snaps it back to its plate position.
///// ────────────────────────────────────────────────────────────────────
///// </summary>
//public class DraggableGrill : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
//{
//    // ── Set by GrillSpawner / GrillCooker ────────────────────────────────────
//    [HideInInspector] public string grillType = "";
//    [HideInInspector] public bool isCooked = false;

//    /// <summary>Set true by PlateController after snapping onto a plate.</summary>
//    [HideInInspector] public bool isOnPlate = false;

//    /// <summary>
//    /// The PlateController that currently holds this grill.
//    /// Set by PlateController.SnapGrill(). Cleared and freed on successful delivery.
//    /// </summary>
//    [HideInInspector] public PlateController owningPlate = null;

//    // ── Pulse (written by GrillCooker BEFORE SetActive) ──────────────────────
//    [HideInInspector] public bool pendingPulse = false;
//    [HideInInspector] public float pendingPulseSpeed = 3f;
//    [HideInInspector] public float pendingPulseAmount = 0.07f;

//    // ── Inspector ──────────────────────────────────────────────────────────────
//    [Header("Alpha Hit-Test  (requires Read/Write Enabled on the texture)")]
//    [Range(0f, 1f)]
//    public float alphaThreshold = 0.1f;

//    [Header("Drag Visual Override  (0,0 = keep prefab size/offset)")]
//    public Vector2 draggingSize = Vector2.zero;
//    public Vector2 draggingOffset = Vector2.zero;

//    // ── Private ───────────────────────────────────────────────────────────────
//    private Canvas _canvas;
//    private RectTransform _rect;
//    private CanvasGroup _cg;
//    private Image _image;

//    // Saved at drag-start; restored on failed drop.
//    private Transform _preDragParent;
//    private Vector3 _preDragWorldPos;
//    private Vector2 _preDragSizeDelta;

//    // Mode A hover
//    private StoveController _hoverStove;
//    private int _hoverSlot = -1;

//    // Mode B hover (distance-based, no raycasts)
//    private PlateController _hoverPlate;
//    private int _hoverPlateSlot = -1;

//    // Mode C hover
//    private CustomerController _hoverCustomer;

//    private Coroutine _pulseRoutine;

//    // ── Unity ─────────────────────────────────────────────────────────────────

//    private void Awake()
//    {
//        _rect = GetComponent<RectTransform>();
//        _cg = GetComponent<CanvasGroup>();
//        _canvas = GetComponentInParent<Canvas>();
//        _image = GetComponent<Image>();

//        TrySetAlphaHitTest();

//        // Off by default — GrillCooker turns it on after cooking.
//        if (_cg != null) _cg.blocksRaycasts = false;
//    }

//    private void OnEnable()
//    {
//        if (pendingPulse)
//        {
//            pendingPulse = false;
//            StartPulse(pendingPulseSpeed, pendingPulseAmount);
//        }
//    }

//    private void TrySetAlphaHitTest()
//    {
//        if (_image == null || alphaThreshold <= 0f) return;
//        Sprite s = _image.sprite;
//        if (s != null && s.texture != null && s.texture.isReadable)
//            _image.alphaHitTestMinimumThreshold = alphaThreshold;
//    }

//#if UNITY_EDITOR
//    private void OnValidate()
//    {
//        if (_image == null) _image = GetComponent<Image>();
//        TrySetAlphaHitTest();
//    }
//#endif

//    // ── Pulse ─────────────────────────────────────────────────────────────────

//    public void StartPulse(float speed = 3f, float amount = 0.07f)
//    {
//        StopPulse();
//        _pulseRoutine = StartCoroutine(PulseRoutine(speed, amount));
//    }

//    public void StopPulse()
//    {
//        if (_pulseRoutine == null) return;
//        StopCoroutine(_pulseRoutine);
//        _pulseRoutine = null;
//        if (_rect != null) _rect.localScale = Vector3.one;
//    }

//    private System.Collections.IEnumerator PulseRoutine(float speed, float amount)
//    {
//        float t = 0f;
//        while (true)
//        {
//            t += Time.deltaTime * speed;
//            float s = 1f + Mathf.Sin(t) * amount;
//            if (_rect != null) _rect.localScale = new Vector3(s, s, 1f);
//            yield return null;
//        }
//    }

//    // ── Origin save / restore ─────────────────────────────────────────────────

//    private void SaveOrigin()
//    {
//        _preDragParent = _rect.parent;
//        _preDragWorldPos = _rect.position;
//        _preDragSizeDelta = _rect.sizeDelta;
//    }

//    /// <summary>
//    /// Returns the grill to its exact pre-drag state.
//    /// For Mode C this snaps it back to its plate position at the canvas root.
//    /// </summary>
//    public void ReturnToOrigin()
//    {
//        if (_preDragParent != null)
//        {
//            _rect.SetParent(_preDragParent, false);
//            _rect.position = _preDragWorldPos;
//            _rect.sizeDelta = _preDragSizeDelta;
//        }

//        _rect.localScale = Vector3.one;

//        if (_cg != null)
//        {
//            _cg.blocksRaycasts = true;
//            _cg.interactable = true;
//            _cg.alpha = 1f;
//        }

//        if (_image != null)
//            _image.raycastTarget = true;

//        if (isCooked)
//            StartPulse(pendingPulseSpeed, pendingPulseAmount);

//        Debug.Log($"[DraggableGrill] '{grillType}' returned to origin.");
//    }

//    // ── GrillSpawner entry point (raw grills only) ────────────────────────────

//    public void BeginDragFromSpawner(PointerEventData eventData)
//    {
//        SaveOrigin();
//        RefreshCanvas();
//        ApplyDragSize();
//        if (_cg != null) _cg.blocksRaycasts = false;
//        MoveToPointer(eventData);
//    }

//    // ── IBeginDragHandler ──────────────────────────────────────────────────────

//    public void OnBeginDrag(PointerEventData eventData)
//    {
//        StopPulse();
//        SaveOrigin();

//        // Pause the burn timer while the player is holding the grill.
//        GetComponent<BurnerGrill>()?.SetDragging(true);

//        // Raw grills are controlled entirely by GrillSpawner.
//        if (!isCooked) return;

//        // Mode B & C: cooked grill picked up.
//        // The grill is already at the canvas root (placed there by PlateController
//        // in Mode C, or by GrillCooker in Mode B), so no reparenting needed here.
//        RefreshCanvas();
//        ApplyDragSize();
//        if (_cg != null) _cg.blocksRaycasts = false;
//        MoveToPointer(eventData);
//    }

//    // ── IDragHandler ───────────────────────────────────────────────────────────

//    public void OnDrag(PointerEventData eventData)
//    {
//        MoveToPointer(eventData);

//        if (!isCooked)
//            TrackStoveHint(eventData);      // Mode A
//        else if (isOnPlate)
//            TrackMouthHint(eventData);      // Mode C
//        else
//            TrackPlateHint(eventData);      // Mode B
//    }

//    // ── IEndDragHandler ────────────────────────────────────────────────────────

//    //public void OnEndDrag(PointerEventData eventData)
//    //{
//    //    if (_cg != null) _cg.blocksRaycasts = true;

//    //    // Resume burn timer now that the player has let go.
//    //    GetComponent<BurnerGrill>()?.SetDragging(false);

//    //    if (!isCooked)
//    //    {
//    //        DropOnStove(eventData);
//    //    }
//    //    else if (isOnPlate)
//    //    {
//    //        DropOnMouth();
//    //        ClearMouthHint();
//    //    }
//    //    else
//    //    {
//    //        DropOnPlate();
//    //        ClearPlateHint();
//    //    }
//    //}

//    public void OnEndDrag(PointerEventData eventData)
//    {
//        if (_cg != null) _cg.blocksRaycasts = true;

//        if (!isCooked)
//        {
//            GetComponent<BurnerGrill>()?.SetDragging(false); // raw grill — resume timer
//            DropOnStove(eventData);
//        }
//        else if (isOnPlate)
//        {
//            // Mode C — plated cooked grill. Check dustbin first, then customer mouth.
//            ClearMouthHint();
//            if (TryDropOnDustbin(eventData))
//                return;
//            DropOnMouth();
//        }
//        else
//        {
//            // Mode B — freshly cooked, not yet plated. Check dustbin first, then plate.
//            ClearPlateHint();
//            if (TryDropOnDustbin(eventData))
//                return;
//            DropOnPlate();   // cooked grill — DropOnPlate handles the burn timer itself
//        }
//    }

//    // ── Dustbin drop (cooked or burned) ───────────────────────────────────────

//    /// <summary>
//    /// Tries to drop this cooked/burned grill on the dustbin.
//    /// Returns true and destroys self if the drop lands on the dustbin.
//    /// Returns false so the caller can fall through to the normal logic.
//    /// </summary>
//    private bool TryDropOnDustbin(PointerEventData eventData)
//    {
//        DustbinController dustbin = FindObjectOfType<DustbinController>();
//        if (dustbin == null) return false;

//        if (!dustbin.IsInsideTarget(eventData.position, eventData.pressEventCamera))
//            return false;

//        // Free the plate slot if this grill was on a plate.
//        owningPlate?.FreeSlotOf(this);

//        // Stop the burn timer.
//        BurnerGrill burner = GetComponent<BurnerGrill>();
//        if (burner != null) burner.enabled = false;

//        // Log and update dustbin UI.
//        dustbin.ReceiveCookedGrill(grillType);

//        Destroy(gameObject);
//        return true;
//    }

//    // ══ MODE A — raw grill → stove ════════════════════════════════════════════

//    private void DropOnStove(PointerEventData eventData)
//    {
//        _hoverStove?.HideAllHints();
//        StoveController stove = FindTarget<StoveController>(eventData);

//        if (stove != null && _hoverSlot >= 0)
//            stove.TryPlaceGrill(this, _hoverSlot);
//        else
//            ReturnToOrigin();

//        _hoverStove = null;
//        _hoverSlot = -1;
//    }

//    private void TrackStoveHint(PointerEventData eventData)
//    {
//        StoveController stove = FindTarget<StoveController>(eventData);

//        if (stove == null)
//        {
//            _hoverStove?.HideAllHints();
//            _hoverStove = null;
//            _hoverSlot = -1;
//            return;
//        }

//        _hoverStove = stove;
//        int slot = stove.GetHoveredSlot(eventData.position, eventData.pressEventCamera);

//        if (slot == _hoverSlot) return;
//        _hoverSlot = slot;

//        if (slot >= 0)
//            stove.ShowSlotHint(slot, _image != null ? _image.sprite : null);
//        else
//            stove.HideAllHints();
//    }

//    // ══ MODE B — cooked grill → plate ════════════════════════════════════════

//    private void DropOnPlate()
//    {
//        if (_hoverPlate != null && _hoverPlateSlot >= 0)
//        {
//            bool placed = PlateManager.Instance != null
//                ? PlateManager.Instance.PlaceGrill(this, _hoverPlate, _hoverPlateSlot)
//                : _hoverPlate.TryReceiveGrill(this, _hoverPlateSlot);

//            if (placed)
//            {
//                // Grill is now on the plate — stop the burn timer permanently
//                BurnerGrill burner = GetComponent<BurnerGrill>();
//                if (burner != null) burner.enabled = false;
//            }
//            else
//            {
//                GetComponent<BurnerGrill>()?.SetDragging(false); // failed — resume timer
//                ReturnToOrigin();
//            }
//        }
//        else
//        {
//            GetComponent<BurnerGrill>()?.SetDragging(false); // missed — resume timer
//            ReturnToOrigin();
//        }
//    }

//    private void TrackPlateHint(PointerEventData eventData)
//    {
//        if (PlateManager.Instance == null) return;

//        var (plate, slot) = PlateManager.Instance.GetNearestPlateAndSlot(
//            eventData.position, eventData.pressEventCamera);

//        if (plate == null || slot < 0)
//        {
//            if (_hoverPlate != null)
//            {
//                PlateManager.Instance.HideAllHints();
//                _hoverPlate = null;
//                _hoverPlateSlot = -1;
//            }
//            return;
//        }

//        if (plate == _hoverPlate && slot == _hoverPlateSlot) return;
//        _hoverPlate = plate;
//        _hoverPlateSlot = slot;
//        PlateManager.Instance.ShowHint(plate, slot, _image != null ? _image.sprite : null);
//    }

//    private void ClearPlateHint()
//    {
//        PlateManager.Instance?.HideAllHints();
//        _hoverPlate = null;
//        _hoverPlateSlot = -1;
//    }

//    // ══ MODE C — plated grill → customer mouth ════════════════════════════════

//    /// <summary>
//    /// SUCCESS  → grill type matches an open order slot:
//    ///   eat animation plays, pig plays happy, grill is Destroyed.
//    /// FAILURE  → grill type doesn't match, or pointer missed all mouths:
//    ///   ReturnToOrigin() snaps it back to its plate position.
//    /// </summary>
//    private void DropOnMouth()
//    {
//        if (_hoverCustomer != null)
//        {
//            bool accepted = _hoverCustomer.TryFulfillOrder(grillType, owningPlate);
//            if (accepted)
//            {
//                // Free the plate slot so it can be reused immediately.
//                owningPlate?.FreeSlotOf(this);

//                CookCharacter.Instance?.PlayHappy();
//                Destroy(gameObject);
//                return;
//            }
//            Debug.Log($"[DraggableGrill] '{grillType}' not accepted — returning to plate.");
//        }
//        else
//        {
//            Debug.Log("[DraggableGrill] Missed all mouths — returning to plate.");
//        }

//        ReturnToOrigin();
//        _hoverCustomer = null;
//    }

//    private void TrackMouthHint(PointerEventData eventData)
//    {
//        if (CustomerManager.Instance == null) return;

//        var (customer, _) = CustomerManager.Instance.GetNearestMouth(
//            eventData.position, eventData.pressEventCamera);

//        if (customer == _hoverCustomer) return;

//        _hoverCustomer?.SetMouthHint(false);
//        _hoverCustomer = customer;
//        _hoverCustomer?.SetMouthHint(true);
//    }

//    private void ClearMouthHint()
//    {
//        _hoverCustomer?.SetMouthHint(false);
//        _hoverCustomer = null;
//    }

//    // ── Helpers ───────────────────────────────────────────────────────────────

//    private void ApplyDragSize()
//    {
//        if (_rect != null && draggingSize != Vector2.zero)
//            _rect.sizeDelta = draggingSize;
//    }

//    private void RefreshCanvas()
//    {
//        _canvas = GetComponentInParent<Canvas>();
//        if (_canvas == null)
//            _canvas = FindObjectOfType<Canvas>();
//    }

//    private void MoveToPointer(PointerEventData eventData)
//    {
//        if (_canvas == null) RefreshCanvas();
//        if (_canvas == null) return;

//        RectTransformUtility.ScreenPointToLocalPointInRectangle(
//            _canvas.GetComponent<RectTransform>(),
//            eventData.position,
//            eventData.pressEventCamera,
//            out Vector2 local);

//        _rect.localPosition = local + draggingOffset;
//    }

//    private T FindTarget<T>(PointerEventData eventData) where T : Component
//    {
//        var hits = new System.Collections.Generic.List<RaycastResult>();
//        EventSystem.current.RaycastAll(eventData, hits);
//        foreach (var hit in hits)
//        {
//            T t = hit.gameObject.GetComponentInParent<T>();
//            if (t != null) return t;
//        }
//        return null;
//    }
//}

using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// Attach to BOTH the raw grill prefab root AND the cooked child object.
///
/// THREE DRAG MODES
/// ────────────────────────────────────────────────────────────────────────────
/// MODE A  isCooked=false
///   Raw grill spawned by GrillSpawner → dragged to stove.
///
/// MODE B  isCooked=true, isOnPlate=false
///   Freshly cooked grill sitting at canvas root → dragged onto a PlateController slot.
///   On success: PlateController.SnapGrill() moves it to the slot world position
///   (still at canvas root) and sets isOnPlate=true.
///   On miss or failed drop: stays where it is (no snap-back to stove).
///
/// MODE C  isCooked=true, isOnPlate=true
///   Grill already plated at canvas root → dragged to a customer's mouthPoint.
///   SUCCESS → eat anim + pig happy + Destroy.
///   FAIL    → ReturnToOrigin() snaps it back to its plate position.
/// ────────────────────────────────────────────────────────────────────────────
///
/// FIX — "grill snaps back to stove instead of staying on plate"
///   Root cause: the cooked child GameObject is a NEW object (set active by
///   GrillCooker); SaveOrigin() had never been called on it, so _preDragParent
///   was null and _preDragWorldPos was (0,0,0) — world origin inside the stove
///   hierarchy. ReturnToOrigin() then teleported it there.
///   Fix: OnEnable() saves origin immediately after GrillCooker activates and
///   positions the object. A Mode B miss no longer calls ReturnToOrigin() — it
///   simply leaves the grill at its current canvas-root position so the player
///   can try again.
///
/// FIX — "cooked grills auto-picked up by plates"
///   Root cause: PlateManager.GetNearestPlateAndSlot() was called on every drag
///   frame (TrackPlateHint) and could record a hover over a plate that happens
///   to be geometrically close, even when the player didn't intend to drop.
///   Fix: tracking still works the same way (it only shows hints), but DropOnPlate()
///   now requires _hoverPlate to be non-null AND within detection radius at the
///   moment of release. Missing all plates now leaves the grill in place rather
///   than snapping it to origin.
/// </summary>
public class DraggableGrill : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    // ── Set by GrillSpawner / GrillCooker ────────────────────────────────────

    [HideInInspector] public string grillType = "";
    [HideInInspector] public bool isCooked = false;

    /// <summary>Set true by PlateController.SnapGrill() after snapping onto a plate.</summary>
    [HideInInspector] public bool isOnPlate = false;

    /// <summary>
    /// The PlateController that currently holds this grill.
    /// Set by PlateController.SnapGrill(). Cleared and freed on successful delivery.
    /// </summary>
    [HideInInspector] public PlateController owningPlate = null;

    // ── Pulse (written by GrillCooker BEFORE SetActive) ──────────────────────

    [HideInInspector] public bool pendingPulse = false;
    [HideInInspector] public float pendingPulseSpeed = 3f;
    [HideInInspector] public float pendingPulseAmount = 0.07f;

    // ── Inspector ─────────────────────────────────────────────────────────────

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

    // Saved at drag-start (or OnEnable for cooked objects); restored on failed Mode C drop.
    private Transform _preDragParent;
    private Vector3 _preDragWorldPos;
    private Vector2 _preDragSizeDelta;

    // Mode A hover
    private StoveController _hoverStove;
    private int _hoverSlot = -1;

    // Mode B hover (distance-based, no raycasts)
    private PlateController _hoverPlate;
    private int _hoverPlateSlot = -1;

    // Mode C hover
    private CustomerController _hoverCustomer;

    private Coroutine _pulseRoutine;

    // Last known pointer position — used by DropOnPlate last-chance check.
    private Vector2 _lastPointerScreenPos;
    private Camera _lastPointerCamera;

    // ── Unity ─────────────────────────────────────────────────────────────────

    private void Awake()
    {
        _rect = GetComponent<RectTransform>();
        _cg = GetComponent<CanvasGroup>();
        _canvas = GetComponentInParent<Canvas>();
        _image = GetComponent<Image>();

        TrySetAlphaHitTest();

        // Off by default — GrillCooker turns it on after cooking.
        if (_cg != null) _cg.blocksRaycasts = false;
    }

    private void OnEnable()
    {
        // ── FIX: save origin the moment GrillCooker activates this object ────
        // GrillCooker sets pendingPulse = true BEFORE calling SetActive, so we
        // can use it as a reliable signal that this is a freshly-cooked object
        // that needs its origin captured right now.
        if (pendingPulse)
        {
            // Snapshot current position BEFORE starting the pulse so ReturnToOrigin
            // has valid data even if the player drags before the first frame.
            SaveOrigin();

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
        _preDragParent = _rect.parent;
        _preDragWorldPos = _rect.position;
        _preDragSizeDelta = _rect.sizeDelta;
    }

    /// <summary>
    /// Returns the grill to its exact pre-drag state.
    /// For Mode C: snaps back to the plate position at canvas root.
    /// Only called when _preDragParent is valid (cooked grill placed by
    /// PlateController.SnapGrill, or a raw grill spawned by GrillSpawner).
    /// </summary>
    public void ReturnToOrigin()
    {
        if (_preDragParent != null)
        {
            _rect.SetParent(_preDragParent, false);
            _rect.position = _preDragWorldPos;
            _rect.sizeDelta = _preDragSizeDelta;
        }

        _rect.localScale = Vector3.one;

        if (_cg != null)
        {
            _cg.blocksRaycasts = true;
            _cg.interactable = true;
            _cg.alpha = 1f;
        }

        if (_image != null) _image.raycastTarget = true;

        // Resume pulse.
        // NOTE: do NOT touch BurnerGrill here. If this grill is returning to its
        // plate position (Mode C failed delivery), BurnerGrill is already disabled
        // by PlateController.SnapGrill() and should stay that way.
        // If it is a raw grill returning to spawn, BurnerGrill doesn't exist.
        if (isCooked) StartPulse(pendingPulseSpeed, pendingPulseAmount);

        Debug.Log($"[DraggableGrill] '{grillType}' returned to origin.");
    }

    // ── GrillSpawner entry point (raw grills only) ────────────────────────────

    public void BeginDragFromSpawner(PointerEventData eventData)
    {
        SaveOrigin();
        RefreshCanvas();
        ApplyDragSize();
        if (_cg != null) _cg.blocksRaycasts = false;
        MoveToPointer(eventData);
    }

    // ── IBeginDragHandler ─────────────────────────────────────────────────────

    public void OnBeginDrag(PointerEventData eventData)
    {
        StopPulse();
        SaveOrigin(); // always refresh snapshot when player picks up

        // Pause the burn timer while the player holds the grill.
        GetComponent<BurnerGrill>()?.SetDragging(true);

        // Raw grills are controlled entirely by GrillSpawner — no extra work here.
        if (!isCooked) return;

        // Mode B & C: cooked grill picked up from a stove/plate slot anchor.
        RefreshCanvas();

        // Lift to canvas root so MoveToPointer's localPosition (computed in the
        // canvas's local space) is applied in the same space — otherwise a grill
        // parented to a stove/plate slot anchor jumps by that anchor's offset.
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

    // ── IDragHandler ──────────────────────────────────────────────────────────

    public void OnDrag(PointerEventData eventData)
    {
        MoveToPointer(eventData);

        if (!isCooked)
            TrackStoveHint(eventData);   // Mode A
        else if (isOnPlate)
        {
            TrackMouthHint(eventData);   // Mode C — mouth delivery
            // Only track plate hints when NOT hovering a mouth; otherwise the
            // nearest plate slot (often the middle one) intercepts mouth drops.
            if (_hoverCustomer == null)
                TrackPlateHint(eventData);   // Mode C — plate-to-plate swap
            else
                ClearPlateHint();            // clear stale plate hint while near a mouth
        }
        else
            TrackPlateHint(eventData);   // Mode B
    }

    // ── IEndDragHandler ───────────────────────────────────────────────────────

    public void OnEndDrag(PointerEventData eventData)
    {
        if (_cg != null) _cg.blocksRaycasts = true;

        if (!isCooked)
        {
            // Mode A — raw grill → stove
            GetComponent<BurnerGrill>()?.SetDragging(false);
            DropOnStove(eventData);
        }
        else if (isOnPlate)
        {
            // Mode C — plated cooked grill.
            // Priority: plate swap > dustbin > customer mouth > snap back.
            CustomerController dropTarget = _hoverCustomer;
            ClearMouthHint();

            // 1. Did the player drop onto a plate slot (move or swap)?
            //    Skip if a customer mouth was targeted — mouth always wins.
            if (dropTarget == null && TryDropOnPlateFromPlate()) return;

            // 2. Dustbin discard.
            if (TryDropOnDustbin(eventData)) return;

            // 3. Customer mouth delivery.
            ClearPlateHint();
            DropOnMouth(dropTarget);
        }
        else
        {
            // Mode B — freshly cooked, not yet plated → plate slot (or dustbin)
            ClearPlateHint();
            if (TryDropOnDustbin(eventData)) return;
            DropOnPlate(); // burn timer handled inside DropOnPlate
        }
    }

    // ── Dustbin drop (cooked or burned) ───────────────────────────────────────

    /// <summary>
    /// Tries to drop this grill on the dustbin.
    /// Returns true (and destroys self) when successful.
    /// Returns false so the caller falls through to normal logic.
    /// </summary>
    private bool TryDropOnDustbin(PointerEventData eventData)
    {
        DustbinController dustbin = FindObjectOfType<DustbinController>();
        if (dustbin == null) return false;

        if (!dustbin.IsInsideTarget(eventData.position, eventData.pressEventCamera))
            return false;

        // Free the plate slot if this grill was on a plate.
        owningPlate?.FreeSlotOf(this);

        // Disable the burn timer.
        BurnerGrill burner = GetComponent<BurnerGrill>();
        if (burner != null) burner.enabled = false;

        dustbin.ReceiveCookedGrill(grillType);
        Destroy(gameObject);
        return true;
    }

    // ══ MODE A — raw grill → stove ════════════════════════════════════════════

    private void DropOnStove(PointerEventData eventData)
    {
        _hoverStove?.HideAllHints();
        StoveController stove = FindTarget<StoveController>(eventData);

        if (stove != null && _hoverSlot >= 0)
            stove.TryPlaceGrill(this, _hoverSlot);
        else
            ReturnToOrigin();

        _hoverStove = null;
        _hoverSlot = -1;
    }

    private void TrackStoveHint(PointerEventData eventData)
    {
        StoveController stove = FindTarget<StoveController>(eventData);

        if (stove == null)
        {
            _hoverStove?.HideAllHints();
            _hoverStove = null;
            _hoverSlot = -1;
            return;
        }

        _hoverStove = stove;
        int slot = stove.GetHoveredSlot(eventData.position, eventData.pressEventCamera);

        if (slot == _hoverSlot) return;
        _hoverSlot = slot;

        if (slot >= 0)
            stove.ShowSlotHint(slot, _image != null ? _image.sprite : null);
        else
            stove.HideAllHints();
    }

    // ══ MODE B — cooked grill → plate ════════════════════════════════════════

    private void DropOnPlate()
    {
        // If TrackPlateHint didn't record a hover (e.g. player moved very fast),
        // do one final proximity check at the drop position with a generous radius.
        if ((_hoverPlate == null || _hoverPlateSlot < 0) && PlateManager.Instance != null)
        {
            // Last-chance catch using a doubled radius — free slots only.
            float savedRadius = PlateManager.Instance.slotDetectionRadius;
            PlateManager.Instance.slotDetectionRadius = savedRadius * 2f;
            var (fallbackPlate, fallbackSlot) = PlateManager.Instance.GetNearestPlateAndSlot(
                _lastPointerScreenPos, _lastPointerCamera);
            PlateManager.Instance.slotDetectionRadius = savedRadius;

            if (fallbackPlate != null && fallbackSlot >= 0)
            {
                _hoverPlate = fallbackPlate;
                _hoverPlateSlot = fallbackSlot;
            }
        }

        if (_hoverPlate != null && _hoverPlateSlot >= 0)
        {
            bool placed = PlateManager.Instance != null
                ? PlateManager.Instance.PlaceGrill(this, _hoverPlate, _hoverPlateSlot)
                : _hoverPlate.TryReceiveGrill(this, _hoverPlateSlot);

            if (placed)
            {
                Debug.Log($"[DraggableGrill] '{grillType}' placed on plate.");
            }
            else
            {
                // Slot is full — snap back.
                GetComponent<BurnerGrill>()?.SetDragging(false);
                ReturnToOrigin();
                Debug.Log($"[DraggableGrill] '{grillType}' slot is full — snapped back.");
            }
        }
        else
        {
            // Missed all plates — snap back to where the cooked grill came from.
            GetComponent<BurnerGrill>()?.SetDragging(false);
            ReturnToOrigin();
            Debug.Log($"[DraggableGrill] '{grillType}' missed all plate slots — snapped back.");
        }

        _hoverPlate = null;
        _hoverPlateSlot = -1;
    }

    private void TrackPlateHint(PointerEventData eventData)
    {
        if (PlateManager.Instance == null) return;

        // Only hint free slots — occupied slots are not a valid drop target.
        var (plate, slot) = PlateManager.Instance.GetNearestPlateAndSlot(
            eventData.position, eventData.pressEventCamera);

        if (plate == null || slot < 0)
        {
            if (_hoverPlate != null)
            {
                PlateManager.Instance.HideAllHints();
                _hoverPlate = null;
                _hoverPlateSlot = -1;
            }
            return;
        }

        if (plate == _hoverPlate && slot == _hoverPlateSlot) return;

        _hoverPlate = plate;
        _hoverPlateSlot = slot;
        PlateManager.Instance.ShowHint(plate, slot, _image != null ? _image.sprite : null);
    }

    private void ClearPlateHint()
    {
        PlateManager.Instance?.HideAllHints();
        _hoverPlate = null;
        _hoverPlateSlot = -1;
    }

    // ══ MODE C — plated grill → another plate slot (move / swap) ═════════════

    /// <summary>
    /// Called during Mode C (grill already on a plate) when the player drops
    /// the grill near a plate slot instead of a customer mouth.
    ///
    /// Same slot on same plate  → no-op (return false, fall through to mouth).
    /// Different slot, free     → move: free old slot, snap into new slot.
    /// Different slot, occupied → swap: evict that occupant, snap this grill in.
    ///
    /// Returns true if the plate handled the drop (caller should return early).
    /// Returns false so the caller falls through to mouth/dustbin logic.
    /// </summary>
    private bool TryDropOnPlateFromPlate()
    {
        // Last-chance detection in case hover wasn't tracked (fast drag / tight radius).
        if ((_hoverPlate == null || _hoverPlateSlot < 0) && PlateManager.Instance != null)
        {
            float saved = PlateManager.Instance.slotDetectionRadius;
            PlateManager.Instance.slotDetectionRadius = saved * 2f;
            var (fbPlate, fbSlot) = PlateManager.Instance.GetNearestPlateAndSlot(
                _lastPointerScreenPos, _lastPointerCamera);
            PlateManager.Instance.slotDetectionRadius = saved;
            if (fbPlate != null && fbSlot >= 0)
            {
                _hoverPlate = fbPlate;
                _hoverPlateSlot = fbSlot;
            }
        }

        // Capture before ClearPlateHint nulls the fields.
        PlateController targetPlate = _hoverPlate;
        int targetSlot = _hoverPlateSlot;
        ClearPlateHint();

        if (targetPlate == null || targetSlot < 0) return false;

        // Same slot — player just picked it up and put it back.
        bool sameSlot = (targetPlate == owningPlate) &&
                        owningPlate != null &&
                        owningPlate.GetOccupant(targetSlot) == this;
        if (sameSlot) return false;

        // Remember source slot BEFORE freeing it.
        PlateController sourcePlate = owningPlate;
        int sourceSlot = sourcePlate != null ? sourcePlate.GetSlotOf(this) : -1;

        // Free this grill's current slot.
        owningPlate?.FreeSlotOf(this);
        isOnPlate = false;
        owningPlate = null;

        bool targetOccupied = targetPlate.IsSlotOccupied(targetSlot);
        if (targetOccupied)
        {
            // Target slot is full — put this grill back where it came from.
            ReturnToOrigin();
            Debug.Log($"[DraggableGrill] '{grillType}' target slot {targetSlot} is full — returned to origin.");
            return true;
        }
        else
        {
            bool placed = PlateManager.Instance != null
                ? PlateManager.Instance.PlaceGrill(this, targetPlate, targetSlot)
                : targetPlate.TryReceiveGrill(this, targetSlot);

            if (!placed)
            {
                ReturnToOrigin();
                return true;
            }
            Debug.Log($"[DraggableGrill] Moved '{grillType}' to slot {targetSlot}.");
        }

        return true;
    }

    // ══ MODE C — plated grill → customer mouth ════════════════════════════════

    /// <summary>
    /// SUCCESS  → type matches an open order slot:
    ///   eat animation plays, pig happy, grill Destroyed.
    /// FAILURE  → wrong type, or pointer missed all mouths:
    ///   ReturnToOrigin() snaps it back to its plate position.
    /// </summary>
    private void DropOnMouth(CustomerController target = null)
    {
        if (target != null)
        {
            bool accepted = target.TryFulfillOrder(grillType, owningPlate);
            if (accepted)
            {
                owningPlate?.FreeSlotOf(this);
                CookCharacter.Instance?.PlayHappy();
                PigGrillTutorialManager.Instance?.NotifyFoodDeliveredToMouth();
                Destroy(gameObject);
                return;
            }
            Debug.Log($"[DraggableGrill] '{grillType}' not accepted by customer — returning to plate.");
        }
        else
        {
            Debug.Log("[DraggableGrill] Missed all customer mouths — returning to plate.");
        }

        ReturnToOrigin();
    }

    private void TrackMouthHint(PointerEventData eventData)
    {
        if (CustomerManager.Instance == null) return;

        var (customer, _) = CustomerManager.Instance.GetNearestMouth(
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
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private void ApplyDragSize()
    {
        if (_rect != null && draggingSize != Vector2.zero)
            _rect.sizeDelta = draggingSize;
    }

    private void RefreshCanvas()
    {
        _canvas = GetComponentInParent<Canvas>();
        if (_canvas == null) _canvas = FindObjectOfType<Canvas>();
    }

    private void MoveToPointer(PointerEventData eventData)
    {
        if (_canvas == null) RefreshCanvas();
        if (_canvas == null) return;

        // Always record last known pointer so DropOnPlate can do a final check.
        _lastPointerScreenPos = eventData.position;
        _lastPointerCamera = eventData.pressEventCamera;

        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            _canvas.GetComponent<RectTransform>(),
            eventData.position,
            eventData.pressEventCamera,
            out Vector2 local);

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