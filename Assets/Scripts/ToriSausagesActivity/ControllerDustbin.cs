////using UnityEngine;
////using UnityEngine.UI;

////namespace ToriSausages
////{
////    /// <summary>
////    /// Attach to the dustbin UI GameObject.
////    ///
////    /// SETUP
////    ///   1. Add this component to the dustbin Image.
////    ///   2. Create an empty child GameObject, position it over the dustbin opening,
////    ///      and assign it to targetPoint.
////    ///   3. Adjust targetRadius until the drop zone feels right.
////    ///
////    /// FLOW
////    ///   DraggableBurned calls IsInsideTarget() on drop.
////    ///   If inside → ReceiveBurnedSausage() destroys the burned sausage.
////    /// </summary>
////    public class ControllerDustbin : MonoBehaviour
////    {
////        [Header("Drop Target")]
////        [Tooltip("Empty RectTransform positioned over the dustbin opening.\n" +
////                 "Burned sausage must be dropped within targetRadius pixels of this point.")]
////        public RectTransform targetPoint;

////        [Tooltip("Screen-pixel radius of the drop zone around the target point.")]
////        public float targetRadius = 120f;

////        [Header("Score UI (optional)")]
////        public Text scoreText;

////        private int _burnCount = 0;

////        // ── Public API ────────────────────────────────────────────────────────────

////        /// <summary>
////        /// Returns true if screenPos is within targetRadius pixels of the target point.
////        /// Called by DraggableBurned on drop.
////        /// </summary>
////        public bool IsInsideTarget(Vector2 screenPos, Camera cam)
////        {
////            if (targetPoint == null)
////            {
////                Debug.LogWarning("[ControllerDustbin] targetPoint not assigned!");
////                return false;
////            }

////            Vector2 targetScreen = RectTransformUtility.WorldToScreenPoint(cam, targetPoint.position);
////            float dist = Vector2.Distance(screenPos, targetScreen);
////            return dist <= targetRadius;
////        }

////        /// <summary>
////        /// Called by DraggableBurned when successfully dropped on the target.
////        /// Increments burn count, updates UI, and destroys the burned sausage.
////        /// </summary>
////        public void ReceiveBurnedSausage(DraggableBurned burned)
////        {
////            if (burned == null) return;

////            _burnCount++;
////            Debug.Log($"[ControllerDustbin] Burned sausage discarded. Total: {_burnCount}");

////            if (scoreText != null)
////                scoreText.text = $"Burned: {_burnCount}";

////            Destroy(burned.gameObject);
////        }
////    }
////}

//using UnityEngine;
//using UnityEngine.UI;

//namespace ToriSausages
//{
//    /// <summary>
//    /// Attach to the dustbin UI GameObject.
//    ///
//    /// CHANGES
//    ///   • ReceiveCookedSausage() — new method for discarding a normal (non-burned)
//    ///     cooked sausage that is wrong or unwanted.  Called by DraggableSausage
//    ///     when the player drops a plated sausage onto the dustbin.
//    ///   • Separate counters for burned vs cooked discards (combined in UI).
//    ///
//    /// SETUP
//    ///   1. Add this component to the dustbin Image.
//    ///   2. Create an empty child GameObject, position it over the dustbin opening,
//    ///      and assign it to targetPoint.
//    ///   3. Adjust targetRadius until the drop zone feels right.
//    /// </summary>
//    public class ControllerDustbin : MonoBehaviour
//    {
//        [Header("Drop Target")]
//        [Tooltip("Empty RectTransform positioned over the dustbin opening.")]
//        public RectTransform targetPoint;

//        [Tooltip("Screen-pixel radius of the drop zone around the target point.")]
//        public float targetRadius = 120f;

//        [Header("Score UI (optional)")]
//        public Text scoreText;

//        private int _burnCount = 0; // burned sausages thrown away
//        private int _wasteCount = 0; // cooked-but-wrong sausages thrown away

//        // ── Public API ────────────────────────────────────────────────────────────

//        /// <summary>
//        /// Returns true if screenPos is within targetRadius pixels of the target point.
//        /// Called by both DraggableBurned and DraggableSausage on drop.
//        /// </summary>
//        public bool IsInsideTarget(Vector2 screenPos, Camera cam)
//        {
//            if (targetPoint == null)
//            {
//                Debug.LogWarning("[ControllerDustbin] targetPoint not assigned!");
//                return false;
//            }
//            Vector2 targetScreen = RectTransformUtility.WorldToScreenPoint(cam, targetPoint.position);
//            return Vector2.Distance(screenPos, targetScreen) <= targetRadius;
//        }

//        /// <summary>
//        /// Called by DraggableBurned when a burned sausage is dropped on the dustbin.
//        /// </summary>
//        public void ReceiveBurnedSausage(DraggableBurned burned)
//        {
//            if (burned == null) return;
//            _burnCount++;
//            Debug.Log($"[ControllerDustbin] Burned sausage discarded. Burned total: {_burnCount}");
//            UpdateUI();
//            Destroy(burned.gameObject);
//        }

//        /// <summary>
//        /// Called by DraggableSausage when a cooked (non-burned) sausage is discarded —
//        /// either because it was the wrong type or because the player no longer needs it.
//        /// Works for both pre-plate (MODE B) and plated (MODE C) sausages.
//        /// </summary>
//        public void ReceiveCookedSausage(DraggableSausage sausage)
//        {
//            if (sausage == null) return;
//            _wasteCount++;
//            Debug.Log($"[ControllerDustbin] Cooked sausage '{sausage.grillType}' discarded. " +
//                      $"Waste total: {_wasteCount}");
//            UpdateUI();
//            Destroy(sausage.gameObject);
//        }

//        // ── Helpers ───────────────────────────────────────────────────────────────

//        private void UpdateUI()
//        {
//            if (scoreText != null)
//                scoreText.text = $"Bin — Burned:{_burnCount}  Wasted:{_wasteCount}";
//        }
//    }
//}

//using UnityEngine;
//using UnityEngine.UI;

//namespace ToriSausages
//{
//    /// <summary>
//    /// Attach to the dustbin UI GameObject.
//    ///
//    /// SETUP
//    ///   1. Add this component to the dustbin Image.
//    ///   2. Create an empty child GameObject, position it over the dustbin opening,
//    ///      and assign it to targetPoint.
//    ///   3. Adjust targetRadius until the drop zone feels right.
//    ///
//    /// FLOW
//    ///   DraggableBurned calls IsInsideTarget() on drop.
//    ///   If inside → ReceiveBurnedSausage() destroys the burned sausage.
//    /// </summary>
//    public class ControllerDustbin : MonoBehaviour
//    {
//        [Header("Drop Target")]
//        [Tooltip("Empty RectTransform positioned over the dustbin opening.\n" +
//                 "Burned sausage must be dropped within targetRadius pixels of this point.")]
//        public RectTransform targetPoint;

//        [Tooltip("Screen-pixel radius of the drop zone around the target point.")]
//        public float targetRadius = 120f;

//        [Header("Score UI (optional)")]
//        public Text scoreText;

//        private int _burnCount = 0;

//        // ── Public API ────────────────────────────────────────────────────────────

//        /// <summary>
//        /// Returns true if screenPos is within targetRadius pixels of the target point.
//        /// Called by DraggableBurned on drop.
//        /// </summary>
//        public bool IsInsideTarget(Vector2 screenPos, Camera cam)
//        {
//            if (targetPoint == null)
//            {
//                Debug.LogWarning("[ControllerDustbin] targetPoint not assigned!");
//                return false;
//            }

//            Vector2 targetScreen = RectTransformUtility.WorldToScreenPoint(cam, targetPoint.position);
//            float dist = Vector2.Distance(screenPos, targetScreen);
//            return dist <= targetRadius;
//        }

//        /// <summary>
//        /// Called by DraggableBurned when successfully dropped on the target.
//        /// Increments burn count, updates UI, and destroys the burned sausage.
//        /// </summary>
//        public void ReceiveBurnedSausage(DraggableBurned burned)
//        {
//            if (burned == null) return;

//            _burnCount++;
//            Debug.Log($"[ControllerDustbin] Burned sausage discarded. Total: {_burnCount}");

//            if (scoreText != null)
//                scoreText.text = $"Burned: {_burnCount}";

//            Destroy(burned.gameObject);
//        }
//    }
//}

using UnityEngine;
using UnityEngine.UI;

namespace ToriSausages
{
    /// <summary>
    /// Attach to the dustbin UI GameObject.
    ///
    /// CHANGES
    ///   • ReceiveCookedSausage() — new method for discarding a normal (non-burned)
    ///     cooked sausage that is wrong or unwanted.  Called by DraggableSausage
    ///     when the player drops a plated sausage onto the dustbin.
    ///   • Separate counters for burned vs cooked discards (combined in UI).
    ///
    /// SETUP
    ///   1. Add this component to the dustbin Image.
    ///   2. Create an empty child GameObject, position it over the dustbin opening,
    ///      and assign it to targetPoint.
    ///   3. Adjust targetRadius until the drop zone feels right.
    /// </summary>
    public class ControllerDustbin : MonoBehaviour
    {
        [Header("Drop Target")]
        [Tooltip("Empty RectTransform positioned over the dustbin opening.")]
        public RectTransform targetPoint;

        [Tooltip("Screen-pixel radius of the drop zone around the target point.")]
        public float targetRadius = 120f;

        [Header("Score UI (optional)")]
        public Text scoreText;

        private int _burnCount = 0; // burned sausages thrown away
        private int _wasteCount = 0; // cooked-but-wrong sausages thrown away

        // ── Public API ────────────────────────────────────────────────────────────

        /// <summary>
        /// Returns true if screenPos is within targetRadius pixels of the target point.
        /// Called by both DraggableBurned and DraggableSausage on drop.
        /// </summary>
        public bool IsInsideTarget(Vector2 screenPos, Camera cam)
        {
            if (targetPoint == null)
            {
                Debug.LogWarning("[ControllerDustbin] targetPoint not assigned!");
                return false;
            }
            Vector2 targetScreen = RectTransformUtility.WorldToScreenPoint(cam, targetPoint.position);
            return Vector2.Distance(screenPos, targetScreen) <= targetRadius;
        }

        /// <summary>
        /// Called by DraggableBurned when a burned sausage is dropped on the dustbin.
        /// </summary>
        public void ReceiveBurnedSausage(DraggableBurned burned)
        {
            if (burned == null) return;
            _burnCount++;
            Debug.Log($"[ControllerDustbin] Burned sausage discarded. Burned total: {_burnCount}");
            SoundManager.Instance?.PlaySFX("TrashBin");
            ToriSausageCookTutorialManager.Instance?.NotifyBurnedSausageDiscarded();
            UpdateUI();
            Destroy(burned.gameObject);
        }

        /// <summary>
        /// Called by DraggableSausage when a cooked (non-burned) sausage is discarded —
        /// either because it was the wrong type or because the player no longer needs it.
        /// Works for both pre-plate (MODE B) and plated (MODE C) sausages.
        /// </summary>
        public void ReceiveCookedSausage(DraggableSausage sausage)
        {
            if (sausage == null) return;
            _wasteCount++;
            Debug.Log($"[ControllerDustbin] Cooked sausage '{sausage.grillType}' discarded. " +
                      $"Waste total: {_wasteCount}");
            SoundManager.Instance?.PlaySFX("TrashBin");
            UpdateUI();
            Destroy(sausage.gameObject);
        }

        // ── Helpers ───────────────────────────────────────────────────────────────

        private void UpdateUI()
        {
            if (scoreText != null)
                scoreText.text = $"Bin — Burned:{_burnCount}  Wasted:{_wasteCount}";
        }
    }
}