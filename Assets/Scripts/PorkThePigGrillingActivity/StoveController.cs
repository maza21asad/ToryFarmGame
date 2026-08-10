using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Attach to your Stove UI Image (Raycast Target = ON).
/// Manages stove slots. Accepts raw shrimp dropped by DraggableGrill,
/// snaps them into a slot, and tells GrillCooker to start cooking.
///
/// FAILED DROP BEHAVIOUR
///   If the player drops onto an occupied slot, or misses all slots,
///   the grill is returned to its pre-drag position via ReturnToOrigin().
///
/// SETUP
///   1. Create empty child GameObjects under the stove — one per slot.
///      Position and size them in the Scene view to sit over your stove art.
///   2. Assign each child to a StoveSlot entry in the stoveSlots array.
///   3. Set slotSize per slot to control how big the raw shrimp looks on the stove.
/// </summary>
public class StoveController : MonoBehaviour
{
    [System.Serializable]
    public class StoveSlot
    {
        [Tooltip("Empty child RectTransform that marks this slot on the stove.\n" +
                 "Move it in the Scene view to position it over your stove art.")]
        public RectTransform anchor;

        [Tooltip("The raw shrimp is resized to this when placed in the slot.")]
        public Vector2 slotSize = new Vector2(150f, 200f);
    }

    [Header("Stove Slots")]
    public StoveSlot[] stoveSlots = new StoveSlot[5];

    [Header("Hover Hint")]
    [Range(0f, 1f)]
    [Tooltip("Opacity of the ghost preview shown when a shrimp is dragged over a free slot.")]
    public float hintAlpha = 0.5f;

    // ── Private ────────────────────────────────────────────────────────────
    private bool[] _occupied;
    private Canvas _canvas;
    private Image[] _hintImages;

    // ── Unity ──────────────────────────────────────────────────────────────

    private void Awake()
    {
        _canvas = GetComponentInParent<Canvas>();
        _occupied = new bool[stoveSlots.Length];
        BuildHintImages();
    }

    // ── Hint images ────────────────────────────────────────────────────────

    private void BuildHintImages()
    {
        _hintImages = new Image[stoveSlots.Length];

        for (int i = 0; i < stoveSlots.Length; i++)
        {
            if (stoveSlots[i]?.anchor == null) continue;

            var go = new GameObject("SlotHint");
            go.transform.SetParent(stoveSlots[i].anchor, false);

            var rt = go.AddComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;

            var img = go.AddComponent<Image>();
            img.color = Color.clear;
            img.raycastTarget = false;

            _hintImages[i] = img;
            go.SetActive(false);
        }
    }

    public void ShowSlotHint(int index, Sprite sprite)
    {
        HideAllHints();
        if (index < 0 || index >= _hintImages.Length) return;
        if (_occupied[index]) return;

        _hintImages[index].gameObject.SetActive(true);
        _hintImages[index].sprite = sprite;
        _hintImages[index].color = new Color(1f, 1f, 1f, hintAlpha);
    }

    public void HideAllHints()
    {
        if (_hintImages == null) return;
        foreach (var img in _hintImages)
        {
            if (img == null) continue;
            img.gameObject.SetActive(false);
            img.color = Color.clear;
        }
    }

    // ── Hover detection (called by DraggableGrill every drag frame) ────────

    /// <summary>
    /// Returns the index of the free slot whose rectangle contains screenPoint.
    /// Returns -1 if the pointer is over an occupied slot or outside all slots.
    /// On -1 the grill returns to its origin (ReturnToOrigin) instead of being destroyed.
    /// </summary>
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

    // ── Placement (called by DraggableGrill on drop) ───────────────────────

    /// <summary>
    /// Snaps the raw shrimp to the given slot, resizes it, then starts cooking.
    /// If the slot is invalid or occupied, calls grill.ReturnToOrigin() instead
    /// of destroying — the player can try again.
    /// </summary>
    public void TryPlaceGrill(DraggableGrill grill, int slotIndex)
    {
        HideAllHints();

        if (slotIndex < 0 || slotIndex >= stoveSlots.Length || _occupied[slotIndex])
        {
            Debug.Log("[StoveController] Slot unavailable — returning grill to origin.");
            grill.ReturnToOrigin();
            return;
        }

        StoveSlot slot = stoveSlots[slotIndex];
        RectTransform grillRect = grill.GetComponent<RectTransform>();

        // Snap into the slot center.
        grillRect.SetParent(slot.anchor, false);
        grillRect.anchoredPosition = Vector2.zero;
        grillRect.localScale = Vector3.one;
        grillRect.sizeDelta = slot.slotSize;

        _occupied[slotIndex] = true;

        PigGrillTutorialManager.Instance?.NotifyFoodPlacedOnGrill();
        Debug.Log($"[StoveController] '{grill.grillType}' placed in slot {slotIndex}.");

        // Hand off to GrillCooker.
        GrillCooker cooker = grill.GetComponent<GrillCooker>();
        if (cooker != null)
        {
            cooker.stove = this;
            cooker.slotIndex = slotIndex;
            cooker.rootCanvas = _canvas;
            cooker.StartCooking();
        }
        else
        {
            Debug.LogWarning("[StoveController] No GrillCooker found on grill prefab!");
        }
    }

    // ── Slot management (called by GrillCooker when cooking finishes) ──────

    public void FreeSlot(int index)
    {
        if (index >= 0 && index < _occupied.Length)
            _occupied[index] = false;
    }
}