using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Attach to each plate UI Image.
///
/// Slot occupancy is strictly enforced:
///   - GetNearestFreeSlot only considers UNOCCUPIED slots.
///   - TryReceiveGrill rejects any drop onto an already-occupied slot.
///   - No swapping — a full slot simply refuses the incoming food so it snaps back.
/// </summary>
public class PlateController : MonoBehaviour
{
    public enum PlateID { Plate1, Plate2 }

    [System.Serializable]
    public class PlateSlot
    {
        [Tooltip("Empty child RectTransform — position it over your plate art in the Scene view.")]
        public RectTransform anchor;

        [Tooltip("Grill image is resized to this when placed in this slot.")]
        public Vector2 slotSize = new Vector2(100f, 130f);

        [HideInInspector] public Image hintImage;
        [HideInInspector] public DraggableGrill occupant;
    }

    [Header("Plate Identification")]
    public PlateID plateID = PlateID.Plate1;
    public string plateLabel => plateID.ToString();

    [Header("Slots — position anchors in the Scene view")]
    public PlateSlot[] plateSlots = new PlateSlot[3];

    [Header("Score UI  (optional)")]
    public Text scoreText;

    private bool[] _occupied;
    private int _count = 0;

    public int SlotCount => plateSlots != null ? plateSlots.Length : 0;

    public bool IsSlotOccupied(int i)
    {
        if (_occupied == null || i < 0 || i >= _occupied.Length) return true;
        return _occupied[i];
    }

    private void Awake()
    {
        _occupied = new bool[plateSlots != null ? plateSlots.Length : 0];
        BuildHintImages();
    }

    private void Start()
    {
        if (PlateManager.Instance != null)
            PlateManager.Instance.Register(this);
        else
            Debug.LogWarning($"[PlateController] '{plateLabel}': PlateManager not found.");
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

    // ── Slot detection — FREE slots ONLY ──────────────────────────────────────

    /// <summary>
    /// Returns the nearest FREE (unoccupied) slot index within distance.
    /// Occupied slots are completely skipped — food cannot land there.
    /// </summary>
    public int GetNearestFreeSlot(Vector2 screenPos, Camera cam, out float distance)
    {
        int best = -1;
        float bestDist = float.MaxValue;

        for (int i = 0; i < plateSlots.Length; i++)
        {
            // ── Core enforcement: skip occupied slots entirely ────────────────
            if (_occupied[i] || plateSlots[i]?.anchor == null) continue;

            Vector2 screen = RectTransformUtility.WorldToScreenPoint(cam, plateSlots[i].anchor.position);
            float dist = Vector2.Distance(screenPos, screen);

            if (dist < bestDist) { bestDist = dist; best = i; }
        }

        distance = bestDist;
        return best;
    }

    // ── Hint API ──────────────────────────────────────────────────────────────

    public void ShowSlotHint(int slotIndex, Sprite sprite)
    {
        HideHint();
        if (plateSlots == null || slotIndex < 0 || slotIndex >= plateSlots.Length) return;
        if (_occupied[slotIndex]) return; // never hint an occupied slot
        Image hint = plateSlots[slotIndex].hintImage;
        if (hint == null || sprite == null) return;
        hint.sprite = sprite;
        hint.color = new Color(1f, 1f, 1f, 0.5f);
    }

    public void HideHint()
    {
        if (plateSlots == null) return;
        foreach (var slot in plateSlots)
            if (slot?.hintImage != null)
                slot.hintImage.color = Color.clear;
    }

    // ── Drop API ──────────────────────────────────────────────────────────────

    /// <summary>
    /// Snaps the grill into slotIndex.
    /// Returns false (and does nothing) if the slot is already occupied —
    /// DraggableGrill will snap the food back to its origin.
    /// </summary>
    public bool TryReceiveGrill(DraggableGrill grill, int slotIndex)
    {
        if (plateSlots == null || slotIndex < 0 || slotIndex >= plateSlots.Length)
        {
            Debug.Log($"[PlateController] {plateLabel}: invalid slot {slotIndex}.");
            return false;
        }

        // Hard block — occupied slot refuses the food
        if (_occupied[slotIndex])
        {
            Debug.Log($"[PlateController] {plateLabel}: slot {slotIndex} is full — food snaps back.");
            return false;
        }

        SnapGrill(grill, slotIndex);
        return true;
    }

    // ── Slot helpers ──────────────────────────────────────────────────────────

    public int GetSlotOf(DraggableGrill grill)
    {
        if (plateSlots == null) return -1;
        for (int i = 0; i < plateSlots.Length; i++)
            if (plateSlots[i].occupant == grill) return i;
        return -1;
    }

    public DraggableGrill GetOccupant(int slotIndex)
    {
        if (plateSlots == null || slotIndex < 0 || slotIndex >= plateSlots.Length) return null;
        return plateSlots[slotIndex].occupant;
    }

    public void FreeSlotOf(DraggableGrill grill)
    {
        if (plateSlots == null) return;
        for (int i = 0; i < plateSlots.Length; i++)
        {
            if (plateSlots[i].occupant != grill) continue;
            _occupied[i] = false;
            plateSlots[i].occupant = null;
            if (_count > 0) _count--;
            Debug.Log($"[PlateController] {plateLabel}: slot {i} freed.");
            UpdateScoreUI();
            return;
        }
    }

    // ── Internal snap ─────────────────────────────────────────────────────────

    private void SnapGrill(DraggableGrill grill, int slotIndex)
    {
        PlateSlot slot = plateSlots[slotIndex];
        RectTransform grillRect = grill.GetComponent<RectTransform>();

        // Keep the grill parented to the slot anchor so it stays correctly
        // positioned across any device/resolution (anchor stays the single
        // source of truth for placement).
        grillRect.SetParent(slot.anchor, false);
        grillRect.anchoredPosition = Vector2.zero;
        grillRect.localScale = Vector3.one;
        grillRect.sizeDelta = slot.slotSize;

        _occupied[slotIndex] = true;
        slot.occupant = grill;
        _count++;

        // Switch to Mode C (plate → mouth drag)
        grill.isOnPlate = true;
        grill.owningPlate = this;

        Image img = grill.GetComponent<Image>();
        if (img != null) img.raycastTarget = true;

        CanvasGroup cg = grill.GetComponent<CanvasGroup>();
        if (cg != null) { cg.blocksRaycasts = true; cg.interactable = true; cg.alpha = 1f; }

        // Stop burn timer — grill is safe on the plate
        BurnerGrill burner = grill.GetComponent<BurnerGrill>();
        if (burner != null) burner.enabled = false;

        Debug.Log($"[PlateController] {plateLabel} slot {slotIndex} ← '{grill.grillType}'. Burn stopped.");
        UpdateScoreUI();
    }

    private void UpdateScoreUI()
    {
        if (scoreText != null)
            scoreText.text = $"{plateLabel}: {_count}";
    }
}