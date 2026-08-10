using System.Collections.Generic;
using UnityEngine;

namespace ToriSausages
{
    /// <summary>
    /// Singleton registry for all ControllerPlate instances in the scene.
    /// Uses pure distance math — no raycasts — so overlapping plates never block each other.
    /// </summary>
    [DefaultExecutionOrder(-100)]
    public class ManagerPlate : MonoBehaviour
    {
        public static ManagerPlate Instance { get; private set; }

        [Header("Slot Detection")]
        [Tooltip("Max screen-pixel distance from a slot anchor to count as hovering.")]
        public float slotDetectionRadius = 200f;

        private readonly List<ControllerPlate> _plates = new List<ControllerPlate>();

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
        }

        private void Start()
        {
            foreach (var p in FindObjectsOfType<ControllerPlate>()) Register(p);
        }

        public void Register(ControllerPlate plate)
        {
            if (plate != null && !_plates.Contains(plate))
            { _plates.Add(plate); Debug.Log($"[ManagerPlate] Registered '{plate.plateLabel}'."); }
        }

        public (ControllerPlate plate, int slot) GetNearestPlateAndSlot(Vector2 screenPos, Camera cam)
        {
            ControllerPlate best = null; int bestSlot = -1; float bestDist = slotDetectionRadius;
            foreach (var plate in _plates)
            {
                if (plate == null) continue;
                int slot = plate.GetNearestFreeSlot(screenPos, cam, out float dist);
                if (slot >= 0 && dist < bestDist) { bestDist = dist; best = plate; bestSlot = slot; }
            }
            return (best, bestSlot);
        }

        public void ShowHint(ControllerPlate plate, int slotIndex, Sprite sprite)
        { HideAllHints(); plate?.ShowSlotHint(slotIndex, sprite); }

        public void HideAllHints() { foreach (var p in _plates) p?.HideHint(); }

        public bool PlaceGrill(DraggableSausage sausage, ControllerPlate plate, int slotIndex)
        { HideAllHints(); if (plate == null || slotIndex < 0) return false; return plate.TryReceiveGrill(sausage, slotIndex); }
    }
}
