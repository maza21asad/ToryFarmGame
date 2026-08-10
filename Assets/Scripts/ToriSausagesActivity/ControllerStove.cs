using UnityEngine;
using UnityEngine.UI;

namespace ToriSausages
{
    /// <summary>
    /// Attach to the stove/BBQ UI Image (Raycast Target = ON).
    /// Manages stove slots. Accepts raw sausages dropped by DraggableSausage,
    /// snaps them into a slot, and tells CookerGrill to start cooking.
    /// Failed drop → ReturnToOrigin().
    /// </summary>
    public class ControllerStove : MonoBehaviour
    {
        [System.Serializable]
        public class StoveSlot
        {
            public RectTransform anchor;
            public Vector2 slotSize = new Vector2(150f, 200f);
        }

        [Header("Stove Slots")]
        public StoveSlot[] stoveSlots = new StoveSlot[5];

        [Header("Hover Hint")]
        [Range(0f, 1f)]
        public float hintAlpha = 0.5f;

        /// <summary>
        /// While true, raw sausages (isCooked == false) cannot be dragged from
        /// SpawnerGrill or placed on the stove. Set by ManagerCustomer's intro
        /// chathead popup.
        /// </summary>
        public static bool InputLocked = false;

        private bool[] _occupied;
        private Canvas _canvas;
        private Image[] _hintImages;

        private void Awake()
        {
            _canvas = GetComponentInParent<Canvas>();
            _occupied = new bool[stoveSlots.Length];
            BuildHintImages();
        }

        private void BuildHintImages()
        {
            _hintImages = new Image[stoveSlots.Length];
            for (int i = 0; i < stoveSlots.Length; i++)
            {
                if (stoveSlots[i]?.anchor == null) continue;
                var go = new GameObject("SlotHint");
                go.transform.SetParent(stoveSlots[i].anchor, false);
                var rt = go.AddComponent<RectTransform>();
                rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
                rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;
                var img = go.AddComponent<Image>();
                img.color = Color.clear; img.raycastTarget = false;
                _hintImages[i] = img;
                go.SetActive(false);
            }
        }

        public void ShowSlotHint(int index, Sprite sprite)
        {
            HideAllHints();
            if (index < 0 || index >= _hintImages.Length || _occupied[index]) return;
            _hintImages[index].gameObject.SetActive(true);
            _hintImages[index].sprite = sprite;
            _hintImages[index].color = new Color(1f, 1f, 1f, hintAlpha);
        }

        public void HideAllHints()
        {
            if (_hintImages == null) return;
            foreach (var img in _hintImages)
            { if (img == null) continue; img.gameObject.SetActive(false); img.color = Color.clear; }
        }

        public int GetHoveredSlot(Vector2 screenPoint, Camera eventCamera)
        {
            for (int i = 0; i < stoveSlots.Length; i++)
            {
                if (_occupied[i] || stoveSlots[i]?.anchor == null) continue;
                if (RectTransformUtility.RectangleContainsScreenPoint(
                        stoveSlots[i].anchor, screenPoint, eventCamera))
                    return i;
            }
            return -1;
        }

        public void TryPlaceGrill(DraggableSausage sausage, int slotIndex)
        {
            HideAllHints();
            if (slotIndex < 0 || slotIndex >= stoveSlots.Length || _occupied[slotIndex])
            {
                Debug.Log("[ControllerStove] Slot unavailable — returning sausage.");
                sausage.ReturnToOrigin();
                return;
            }

            StoveSlot slot = stoveSlots[slotIndex];
            RectTransform sausageRect = sausage.GetComponent<RectTransform>();
            sausageRect.SetParent(slot.anchor, false);
            sausageRect.anchoredPosition = Vector2.zero;
            sausageRect.localScale = Vector3.one;
            sausageRect.sizeDelta = slot.slotSize;
            _occupied[slotIndex] = true;

            // Notify the cook tutorial that food was successfully placed on the grill.
            // Without this call the tutorial hand loops on food→grill forever because
            // _phaseComplete is never set.
            ToriSausageCookTutorialManager.Instance?.NotifyFoodPlacedOnGrill();

            Debug.Log($"[ControllerStove] Sausage placed in slot {slotIndex}.");
            CookerGrill cooker = sausage.GetComponent<CookerGrill>();
            if (cooker != null)
            {
                cooker.stove = this;
                cooker.slotIndex = slotIndex;
                cooker.rootCanvas = _canvas;
                cooker.StartCooking();
            }
            else Debug.LogWarning("[ControllerStove] No CookerGrill on prefab!");
        }

        public void FreeSlot(int index)
        {
            if (index >= 0 && index < _occupied.Length) _occupied[index] = false;
        }
    }
}