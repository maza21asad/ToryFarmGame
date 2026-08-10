using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// Attach to the cooked grill prefab alongside BurnerGrill.
/// Starts DISABLED — BurnerGrill enables it once the grill is burned.
///
/// Works for both RedMeat and Shrimp.
///
/// FLOW
///   Player drags burned grill → drops on dustbin target point → destroyed.
///   Dropped anywhere else → returns to last position.
/// </summary>
public class DraggableBurnedGrill : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    // ── Private ───────────────────────────────────────────────────────────────

    private Canvas _canvas;
    private RectTransform _rect;
    private CanvasGroup _cg;

    private Transform _preDragParent;
    private Vector3 _preDragWorldPos;
    private Vector2 _preDragSizeDelta;

    // ── Unity ─────────────────────────────────────────────────────────────────

    private void Awake()
    {
        _rect = GetComponent<RectTransform>();
        _cg = GetComponent<CanvasGroup>();
        _canvas = GetComponentInParent<Canvas>();
    }

    // ── Drag ──────────────────────────────────────────────────────────────────

    public void OnBeginDrag(PointerEventData eventData)
    {
        _preDragParent = _rect.parent;
        _preDragWorldPos = _rect.position;
        _preDragSizeDelta = _rect.sizeDelta;

        RefreshCanvas();

        // Lift to canvas root so it renders above everything.
        Vector3 wp = _rect.position;
        _rect.SetParent(_canvas.transform, worldPositionStays: true);
        _rect.position = wp;

        if (_cg != null) _cg.blocksRaycasts = false;

        MoveToPointer(eventData);
    }

    public void OnDrag(PointerEventData eventData) => MoveToPointer(eventData);

    public void OnEndDrag(PointerEventData eventData)
    {
        if (_cg != null) _cg.blocksRaycasts = true;

        DustbinController dustbin = FindObjectOfType<DustbinController>();

        if (dustbin != null && dustbin.IsInsideTarget(eventData.position, eventData.pressEventCamera))
        {
            dustbin.ReceiveBurnedGrill(this);
        }
        else
        {
            Debug.Log("[DraggableBurnedGrill] Missed dustbin — returning.");
            ReturnToOrigin();
        }
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private void ReturnToOrigin()
    {
        if (_preDragParent != null)
        {
            _rect.SetParent(_preDragParent, false);
            _rect.position = _preDragWorldPos;
            _rect.sizeDelta = _preDragSizeDelta;
        }
        _rect.localScale = Vector3.one;
        if (_cg != null) { _cg.blocksRaycasts = true; _cg.interactable = true; _cg.alpha = 1f; }
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

        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            _canvas.GetComponent<RectTransform>(),
            eventData.position,
            eventData.pressEventCamera,
            out Vector2 local);

        _rect.localPosition = local;
    }
}