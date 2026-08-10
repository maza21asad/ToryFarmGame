using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace ToriSausages
{
    /// <summary>
    /// Attach to the raw sausage icon in the tray.
    /// Drag from this icon → spawns a DraggableSausage copy at the pointer.
    /// Set grillType = "Sausage" in the Inspector.
    ///
    /// While ControllerStove.InputLocked is true (intro chathead popup is up
    /// and waiting for the OK button), dragging is blocked entirely.
    /// </summary>
    public class SpawnerGrill : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        [Header("Prefab")]
        [Tooltip("The raw sausage prefab (has DraggableSausage + CookerGrill).")]
        public GameObject SausagePrefab;

        [Header("Grill Type")]
        [Tooltip("Set this to \"Sausage\".")]
        public string grillType = "Sausage";

        [Header("Alpha Hit-Test")]
        [Range(0f, 1f)]
        public float alphaThreshold = 0.1f;

        private DraggableSausage _active;

        private void Awake()
        {
            Image img = GetComponent<Image>();
            if (img != null) img.alphaHitTestMinimumThreshold = alphaThreshold;
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            if (ControllerStove.InputLocked) return;
            if (SausagePrefab == null) { Debug.LogWarning("[SpawnerGrill] prefab not assigned!"); return; }
            Canvas canvas = GetComponentInParent<Canvas>();
            if (canvas == null) return;

            GameObject spawned = Instantiate(SausagePrefab, canvas.transform);
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                canvas.GetComponent<RectTransform>(), eventData.position,
                eventData.pressEventCamera, out Vector2 local);
            spawned.GetComponent<RectTransform>().localPosition = local;

            _active = spawned.GetComponent<DraggableSausage>();
            if (_active != null) { _active.grillType = grillType; _active.BeginDragFromSpawner(eventData); }
        }

        public void OnDrag(PointerEventData eventData) => _active?.OnDrag(eventData);
        public void OnEndDrag(PointerEventData eventData) { _active?.OnEndDrag(eventData); _active = null; }
    }
}