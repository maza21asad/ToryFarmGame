//using UnityEngine;
//using UnityEngine.UI;

//namespace ToriSausages
//{
//    /// <summary>
//    /// Attach to each plate UI Image.
//    /// After snapping a cooked sausage into a slot it is moved to the canvas root
//    /// and marked isOnPlate=true so the player can drag it to a customer's mouth
//    /// OR apply a condiment first.
//    /// </summary>
//    public class ControllerPlate : MonoBehaviour
//    {
//        public enum PlateID { Plate1, Plate2 }

//        [System.Serializable]
//        public class PlateSlot
//        {
//            public RectTransform anchor;
//            public Vector2 slotSize = new Vector2(100f, 130f);
//            [HideInInspector] public Image hintImage;
//            [HideInInspector] public DraggableSausage occupant;
//        }

//        [Header("Plate Identification")]
//        public PlateID plateID = PlateID.Plate1;
//        public string plateLabel => plateID.ToString();

//        [Header("Slots")]
//        public PlateSlot[] plateSlots = new PlateSlot[3];

//        [Header("Score UI (optional)")]
//        public Text scoreText;

//        private bool[] _occupied;
//        private int _count = 0;

//        public int SlotCount => plateSlots != null ? plateSlots.Length : 0;
//        public bool IsSlotOccupied(int i) =>
//            _occupied == null || i < 0 || i >= _occupied.Length || _occupied[i];

//        private void Awake()
//        {
//            _occupied = new bool[plateSlots != null ? plateSlots.Length : 0];
//            BuildHintImages();
//        }

//        private void Start()
//        {
//            if (ManagerPlate.Instance != null) ManagerPlate.Instance.Register(this);
//            else Debug.LogWarning($"[ControllerPlate] '{plateLabel}': ManagerPlate not found.");
//        }

//        private void BuildHintImages()
//        {
//            if (plateSlots == null) return;
//            foreach (var slot in plateSlots)
//            {
//                if (slot?.anchor == null || slot.hintImage != null) continue;
//                var go = new GameObject("SlotHint", typeof(RectTransform), typeof(Image));
//                go.transform.SetParent(slot.anchor, false);
//                var rt = go.GetComponent<RectTransform>();
//                rt.anchoredPosition = Vector2.zero; rt.sizeDelta = slot.slotSize;
//                var img = go.GetComponent<Image>();
//                img.raycastTarget = false; img.color = Color.clear;
//                slot.hintImage = img;
//            }
//        }

//        public int GetNearestFreeSlot(Vector2 screenPos, Camera cam, out float distance)
//        {
//            int best = -1; float bestDist = float.MaxValue;
//            for (int i = 0; i < plateSlots.Length; i++)
//            {
//                if (_occupied[i] || plateSlots[i]?.anchor == null) continue;
//                Vector2 s = RectTransformUtility.WorldToScreenPoint(cam, plateSlots[i].anchor.position);
//                float d = Vector2.Distance(screenPos, s);
//                if (d < bestDist) { bestDist = d; best = i; }
//            }
//            distance = bestDist; return best;
//        }

//        public void ShowSlotHint(int i, Sprite sprite)
//        {
//            HideHint();
//            if (plateSlots == null || i < 0 || i >= plateSlots.Length) return;
//            Image h = plateSlots[i].hintImage;
//            if (h == null || sprite == null) return;
//            h.sprite = sprite; h.color = new Color(1f, 1f, 1f, 0.5f);
//        }

//        public void HideHint()
//        {
//            if (plateSlots == null) return;
//            foreach (var slot in plateSlots)
//                if (slot?.hintImage != null) slot.hintImage.color = Color.clear;
//        }

//        public bool TryReceiveGrill(DraggableSausage sausage, int slotIndex)
//        {
//            if (plateSlots == null || slotIndex < 0 || slotIndex >= plateSlots.Length)
//            { Debug.Log($"[ControllerPlate] {plateLabel}: invalid slot {slotIndex}."); return false; }
//            if (_occupied[slotIndex])
//            { Debug.Log($"[ControllerPlate] {plateLabel}: slot {slotIndex} occupied."); return false; }
//            SnapGrill(sausage, slotIndex);
//            return true;
//        }

//        public void FreeSlotOf(DraggableSausage sausage)
//        {
//            if (plateSlots == null) return;
//            for (int i = 0; i < plateSlots.Length; i++)
//            {
//                if (plateSlots[i].occupant != sausage) continue;
//                _occupied[i] = false; plateSlots[i].occupant = null;
//                if (_count > 0) _count--;
//                Debug.Log($"[ControllerPlate] {plateLabel}: slot {i} freed.");
//                UpdateScoreUI(); return;
//            }
//        }

//        private void SnapGrill(DraggableSausage sausage, int slotIndex)
//        {
//            PlateSlot slot = plateSlots[slotIndex];
//            RectTransform sausageRect = sausage.GetComponent<RectTransform>();

//            sausageRect.SetParent(slot.anchor, false);
//            sausageRect.anchoredPosition = Vector2.zero;
//            sausageRect.localScale = Vector3.one;
//            sausageRect.sizeDelta = slot.slotSize;

//            Vector3 worldPos = sausageRect.position;
//            Canvas rootCanvas = FindObjectOfType<Canvas>();
//            if (rootCanvas != null)
//            {
//                sausageRect.SetParent(rootCanvas.transform, worldPositionStays: true);
//                sausageRect.position = worldPos;
//            }

//            _occupied[slotIndex] = true;
//            slot.occupant = sausage;
//            _count++;

//            sausage.isOnPlate = true;
//            sausage.owningPlate = this;

//            Image img = sausage.GetComponent<Image>();
//            if (img != null) img.raycastTarget = true;

//            CanvasGroup cg = sausage.GetComponent<CanvasGroup>();
//            if (cg != null) { cg.blocksRaycasts = true; cg.interactable = true; cg.alpha = 1f; }

//            Debug.Log($"[ControllerPlate] {plateLabel} slot {slotIndex} ← '{sausage.grillType}'. " +
//                      $"Apply condiment or drag to a customer mouth.");
//            UpdateScoreUI();
//        }

//        private void UpdateScoreUI()
//        {
//            if (scoreText != null) scoreText.text = $"{plateLabel}: {_count}";
//        }
//    }
//}


using UnityEngine;
using UnityEngine.UI;

namespace ToriSausages
{
    /// <summary>
    /// Attach to each plate UI Image.
    ///
    /// CHANGES
    ///   • ownerCustomer — the customer who currently owns this plate.
    ///     Set by ManagerCustomer when a customer is assigned to a slot.
    ///     Cleared by ManagerCustomer.NotifyLeft() when the customer leaves.
    ///     DraggableSausage reads this to pass the plate reference to
    ///     ControllerCustomer.TryFulfillOrder() for the ownership check.
    /// </summary>
    public class ControllerPlate : MonoBehaviour
    {
        public enum PlateID { Plate1, Plate2 }

        [System.Serializable]
        public class PlateSlot
        {
            public RectTransform anchor;
            public Vector2 slotSize = new Vector2(100f, 130f);
            [HideInInspector] public Image hintImage;
            [HideInInspector] public DraggableSausage occupant;
        }

        [Header("Plate Identification")]
        public PlateID plateID = PlateID.Plate1;
        public string plateLabel => plateID.ToString();

        [Header("Slots")]
        public PlateSlot[] plateSlots = new PlateSlot[3];

        [Header("Score UI (optional)")]
        public Text scoreText;

        // ── Runtime ───────────────────────────────────────────────────────────────

        /// <summary>
        /// The customer who currently owns this plate.
        /// Assigned by ManagerCustomer; cleared when the customer leaves.
        /// </summary>
        [HideInInspector] public ControllerCustomer ownerCustomer = null;

        private bool[] _occupied;
        private int _count = 0;

        public int SlotCount => plateSlots != null ? plateSlots.Length : 0;
        public bool IsSlotOccupied(int i) =>
            _occupied == null || i < 0 || i >= _occupied.Length || _occupied[i];

        private void Awake()
        {
            _occupied = new bool[plateSlots != null ? plateSlots.Length : 0];
            BuildHintImages();
        }

        private void Start()
        {
            if (ManagerPlate.Instance != null) ManagerPlate.Instance.Register(this);
            else Debug.LogWarning($"[ControllerPlate] '{plateLabel}': ManagerPlate not found.");
        }

        private void BuildHintImages()
        {
            if (plateSlots == null) return;
            foreach (var slot in plateSlots)
            {
                if (slot?.anchor == null || slot.hintImage != null) continue;
                var go = new GameObject("SlotHint", typeof(RectTransform), typeof(Image));
                go.transform.SetParent(slot.anchor, false);
                var rt = go.GetComponent<RectTransform>();
                rt.anchoredPosition = Vector2.zero;
                rt.sizeDelta = slot.slotSize;
                var img = go.GetComponent<Image>();
                img.raycastTarget = false;
                img.color = Color.clear;
                slot.hintImage = img;
            }
        }

        public int GetNearestFreeSlot(Vector2 screenPos, Camera cam, out float distance)
        {
            int best = -1;
            float bestDist = float.MaxValue;
            for (int i = 0; i < plateSlots.Length; i++)
            {
                if (_occupied[i] || plateSlots[i]?.anchor == null) continue;
                Vector2 s = RectTransformUtility.WorldToScreenPoint(cam, plateSlots[i].anchor.position);
                float d = Vector2.Distance(screenPos, s);
                if (d < bestDist) { bestDist = d; best = i; }
            }
            distance = bestDist;
            return best;
        }

        public void ShowSlotHint(int i, Sprite sprite)
        {
            HideHint();
            if (plateSlots == null || i < 0 || i >= plateSlots.Length) return;
            Image h = plateSlots[i].hintImage;
            if (h == null || sprite == null) return;
            h.sprite = sprite;
            h.color = new Color(1f, 1f, 1f, 0.5f);
        }

        public void HideHint()
        {
            if (plateSlots == null) return;
            foreach (var slot in plateSlots)
                if (slot?.hintImage != null) slot.hintImage.color = Color.clear;
        }

        public bool TryReceiveGrill(DraggableSausage sausage, int slotIndex)
        {
            if (plateSlots == null || slotIndex < 0 || slotIndex >= plateSlots.Length)
            { Debug.Log($"[ControllerPlate] {plateLabel}: invalid slot {slotIndex}."); return false; }
            if (_occupied[slotIndex])
            { Debug.Log($"[ControllerPlate] {plateLabel}: slot {slotIndex} occupied."); return false; }
            SnapGrill(sausage, slotIndex);
            ToriSausageCookTutorialManager.Instance?.NotifyCookedSausagePlacedOnPlate();
            return true;
        }

        public void FreeSlotOf(DraggableSausage sausage)
        {
            if (plateSlots == null) return;
            for (int i = 0; i < plateSlots.Length; i++)
            {
                if (plateSlots[i].occupant != sausage) continue;
                _occupied[i] = false;
                plateSlots[i].occupant = null;
                if (_count > 0) _count--;
                Debug.Log($"[ControllerPlate] {plateLabel}: slot {i} freed.");
                UpdateScoreUI();
                return;
            }
        }

        /// <summary>
        /// Returns the index of the slot occupied by this sausage, or -1 if not found.
        /// </summary>
        public int GetSlotIndexOf(DraggableSausage sausage)
        {
            if (plateSlots == null) return -1;
            for (int i = 0; i < plateSlots.Length; i++)
                if (plateSlots[i].occupant == sausage) return i;
            return -1;
        }

        /// <summary>
        /// Returns the occupant of the nearest occupied slot to screenPos, within slotDetectionRadius.
        /// Used by DraggableSausage to find a swap target on the same plate.
        /// </summary>
        public (DraggableSausage occupant, int slotIndex) GetNearestOccupiedSlot(
            Vector2 screenPos, Camera cam, float maxDist, DraggableSausage exclude)
        {
            DraggableSausage best = null;
            int bestSlot = -1;
            float bestDist = maxDist;

            for (int i = 0; i < plateSlots.Length; i++)
            {
                if (!_occupied[i] || plateSlots[i].occupant == null) continue;
                if (plateSlots[i].occupant == exclude) continue;

                Vector2 s = RectTransformUtility.WorldToScreenPoint(cam, plateSlots[i].anchor.position);
                float d = Vector2.Distance(screenPos, s);
                if (d < bestDist) { bestDist = d; best = plateSlots[i].occupant; bestSlot = i; }
            }

            return (best, bestSlot);
        }

        /// <summary>
        /// Swaps the two occupants between slotA and slotB on this plate.
        /// Re-parents each sausage to the other's anchor so both stay correctly
        /// anchored (and resolution-independent) afterwards.
        /// Do NOT call ReturnToOrigin() on either sausage after this.
        /// </summary>
        public void SwapSlots(int slotA, int slotB)
        {
            if (plateSlots == null) return;
            if (slotA < 0 || slotA >= plateSlots.Length) return;
            if (slotB < 0 || slotB >= plateSlots.Length) return;
            if (slotA == slotB) return;

            DraggableSausage sausageA = plateSlots[slotA].occupant;
            DraggableSausage sausageB = plateSlots[slotB].occupant;

            RectTransform anchorA = plateSlots[slotA].anchor;
            RectTransform anchorB = plateSlots[slotB].anchor;
            Vector2 sizeA = plateSlots[slotA].slotSize;
            Vector2 sizeB = plateSlots[slotB].slotSize;

            // sausageA goes into slotB's anchor; sausageB goes into slotA's anchor.
            SnapToAnchor(sausageA, anchorB, sizeB);
            SnapToAnchor(sausageB, anchorA, sizeA);

            // Swap occupant bookkeeping — _occupied stays true for both.
            plateSlots[slotA].occupant = sausageB;
            plateSlots[slotB].occupant = sausageA;

            Debug.Log($"[ControllerPlate] {plateLabel}: swapped slot {slotA} ↔ slot {slotB}.");
        }

        /// <summary>
        /// Parents a sausage to the given slot anchor and zeros its anchored
        /// position so it stays correctly placed on any device/resolution.
        /// </summary>
        private static void SnapToAnchor(DraggableSausage sausage, RectTransform anchor, Vector2 slotSize)
        {
            if (sausage == null || anchor == null) return;
            RectTransform rt = sausage.GetComponent<RectTransform>();
            if (rt == null) return;

            rt.SetParent(anchor, false);
            rt.anchoredPosition = Vector2.zero;
            rt.localScale = Vector3.one;
            rt.sizeDelta = slotSize;

            Image img = sausage.GetComponent<Image>();
            if (img != null) img.raycastTarget = true;

            CanvasGroup cg = sausage.GetComponent<CanvasGroup>();
            if (cg != null) { cg.blocksRaycasts = true; cg.interactable = true; cg.alpha = 1f; }
        }

        private void SnapGrill(DraggableSausage sausage, int slotIndex)
        {
            PlateSlot slot = plateSlots[slotIndex];
            RectTransform sausageRect = sausage.GetComponent<RectTransform>();

            // Keep the sausage parented to the slot anchor so it stays correctly
            // positioned across any device/resolution (anchor stays the single
            // source of truth for placement).
            sausageRect.SetParent(slot.anchor, false);
            sausageRect.anchoredPosition = Vector2.zero;
            sausageRect.localScale = Vector3.one;
            sausageRect.sizeDelta = slot.slotSize;

            _occupied[slotIndex] = true;
            slot.occupant = sausage;
            _count++;

            sausage.isOnPlate = true;
            sausage.owningPlate = this;

            Image img = sausage.GetComponent<Image>();
            if (img != null) img.raycastTarget = true;

            CanvasGroup cg = sausage.GetComponent<CanvasGroup>();
            if (cg != null) { cg.blocksRaycasts = true; cg.interactable = true; cg.alpha = 1f; }

            Debug.Log($"[ControllerPlate] {plateLabel} slot {slotIndex} ← '{sausage.grillType}'. " +
                      $"Owner: '{ownerCustomer?.name ?? "none"}'.");
            UpdateScoreUI();
        }

        private void UpdateScoreUI()
        {
            if (scoreText != null) scoreText.text = $"{plateLabel}: {_count}";
        }
    }
}