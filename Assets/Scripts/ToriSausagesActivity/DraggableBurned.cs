using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace ToriSausages
{
    /// <summary>
    /// Attach to the cooked sausage prefab alongside BurnerSausage.
    /// Starts DISABLED — BurnerSausage enables it once the sausage is burned.
    ///
    /// FLOW
    ///   Player drags burned sausage → drops on dustbin target point → destroyed.
    ///   If dropped anywhere else → returns to its last position.
    /// </summary>
    public class DraggableBurned : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
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
            // Save position so we can return if dropped in the wrong place.
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

            // Find dustbin and check if we're close enough to its target point.
            ControllerDustbin dustbin = FindObjectOfType<ControllerDustbin>();

            if (dustbin != null && dustbin.IsInsideTarget(eventData.position, eventData.pressEventCamera))
            {
                dustbin.ReceiveBurnedSausage(this);
                // ReceiveBurnedSausage calls Destroy — nothing more to do.
            }
            else
            {
                Debug.Log("[DraggableBurned] Missed dustbin — returning.");
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
}