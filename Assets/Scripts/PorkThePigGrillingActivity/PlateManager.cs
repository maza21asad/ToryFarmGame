using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Singleton registry for all PlateControllers in the scene.
///
/// Only FREE slots are ever returned to DraggableGrill — occupied slots are
/// invisible to the drag system so food always snaps back when all slots are full.
/// </summary>
[DefaultExecutionOrder(-100)]
public class PlateManager : MonoBehaviour
{
    public static PlateManager Instance { get; private set; }

    [Header("Slot Detection")]
    [Tooltip("Maximum screen-pixel distance from a slot anchor to count as hovering.\n" +
             "Increase if slots feel unresponsive; decrease to require more precision.")]
    public float slotDetectionRadius = 200f;

    private readonly List<PlateController> _plates = new List<PlateController>();

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    private void Start()
    {
        foreach (var p in FindObjectsOfType<PlateController>())
            Register(p);
    }

    // ── Registration ──────────────────────────────────────────────────────────

    public void Register(PlateController plate)
    {
        if (plate != null && !_plates.Contains(plate))
        {
            _plates.Add(plate);
            Debug.Log($"[PlateManager] Registered '{plate.plateLabel}'. Total: {_plates.Count}");
        }
    }

    // ── Nearest FREE slot detection ───────────────────────────────────────────

    /// <summary>
    /// Searches every registered plate for the nearest FREE slot to screenPos.
    /// Occupied slots are completely ignored — they cannot receive food.
    /// Returns (null, -1) when no free slot is within slotDetectionRadius.
    /// </summary>
    public (PlateController plate, int slot) GetNearestPlateAndSlot(Vector2 screenPos, Camera cam)
    {
        PlateController bestPlate = null;
        int bestSlot = -1;
        float bestDist = slotDetectionRadius;

        foreach (var plate in _plates)
        {
            if (plate == null) continue;
            int slot = plate.GetNearestFreeSlot(screenPos, cam, out float dist);
            if (slot >= 0 && dist < bestDist)
            {
                bestDist = dist;
                bestPlate = plate;
                bestSlot = slot;
            }
        }

        return (bestPlate, bestSlot);
    }

    // ── Hint API ──────────────────────────────────────────────────────────────

    public void ShowHint(PlateController plate, int slotIndex, Sprite sprite)
    {
        HideAllHints();
        plate?.ShowSlotHint(slotIndex, sprite);
    }

    public void HideAllHints()
    {
        foreach (var p in _plates)
            p?.HideHint();
    }

    // ── Drop API ──────────────────────────────────────────────────────────────

    public bool PlaceGrill(DraggableGrill grill, PlateController plate, int slotIndex)
    {
        HideAllHints();
        if (plate == null || slotIndex < 0) return false;
        bool placed = plate.TryReceiveGrill(grill, slotIndex);
        if (placed) PigGrillTutorialManager.Instance?.NotifyCookedGrillPlacedOnPlate();
        return placed;
    }
}