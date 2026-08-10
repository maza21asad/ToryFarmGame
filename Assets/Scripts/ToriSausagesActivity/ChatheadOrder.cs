//using UnityEngine;
//using UnityEngine.UI;

//namespace ToriSausages
//{
//    /// <summary>
//    /// One food variant that can appear in an order slot.
//    /// grillType must exactly match DraggableSausage.grillType:
//    ///   "Sausage"        — plain cooked sausage
//    ///   "SausageKetchup" — sausage with ketchup applied
//    ///   "SausageMayo"    — sausage with mayo applied
//    /// </summary>
//    [System.Serializable]
//    public class GrillOrderType
//    {
//        [Tooltip("Must match DraggableSausage.grillType exactly.\n" +
//                 "Use: \"Sausage\", \"SausageKetchup\", or \"SausageMayo\"")]
//        public string grillType;

//        [Tooltip("Icon shown in the chat-bubble order slot.")]
//        public Sprite icon;
//    }

//    /// <summary>
//    /// Attach to the chat-bubble root (set INACTIVE in the Editor).
//    /// All 3 slots are independently randomised each visit.
//    ///
//    /// VARIANTS for this game
//    ///   Add 3 entries to grillTypes[]:
//    ///     0 → grillType="Sausage"        icon=plain sausage sprite
//    ///     1 → grillType="SausageKetchup" icon=sausage+ketchup sprite
//    ///     2 → grillType="SausageMayo"    icon=sausage+mayo sprite
//    ///
//    /// FLOW
//    ///   RandomizeOrder()  — called by ControllerCustomer on arrival
//    ///   FulfillSlot(type) — called by DraggableSausage on mouth-drop; hides matched slot
//    ///   IsComplete()      — true when all 3 slots fulfilled
//    /// </summary>
//    public class ChatheadOrder : MonoBehaviour
//    {
//        [Header("Food Variants  (Sausage / SausageKetchup / SausageMayo)")]
//        public GrillOrderType[] grillTypes;

//        [Header("3 Slot Images inside the Chat Bubble")]
//        public Image[] slotImages = new Image[3];

//        [Header("Fulfilled Overlays (optional tick — same length as slotImages)")]
//        public GameObject[] fulfilledOverlays = new GameObject[3];

//        // ── Runtime ────────────────────────────────────────────────────────────────

//        private string[] _slotType = new string[3];
//        private bool[] _fulfilled = new bool[3];

//        // ── Public API ─────────────────────────────────────────────────────────────

//        public void RandomizeOrder()
//        {
//            if (grillTypes == null || grillTypes.Length == 0)
//            {
//                Debug.LogWarning("[ChatheadOrder] grillTypes[] is empty — add Sausage variants.");
//                return;
//            }

//            for (int i = 0; i < 3; i++)
//            {
//                GrillOrderType pick = grillTypes[Random.Range(0, grillTypes.Length)];
//                _slotType[i] = pick.grillType;
//                _fulfilled[i] = false;

//                if (slotImages != null && i < slotImages.Length && slotImages[i] != null)
//                {
//                    slotImages[i].sprite = pick.icon;
//                    slotImages[i].color = Color.white;
//                    slotImages[i].gameObject.SetActive(true);
//                }

//                if (fulfilledOverlays != null && i < fulfilledOverlays.Length && fulfilledOverlays[i] != null)
//                    fulfilledOverlays[i].SetActive(false);
//            }

//            Debug.Log($"[ChatheadOrder] '{name}' order: [{_slotType[0]}] [{_slotType[1]}] [{_slotType[2]}]");
//        }

//        /// <summary>
//        /// Ticks the first unfulfilled slot matching grillType and hides its icon.
//        /// Returns true if matched; false if this type isn't needed right now.
//        /// </summary>
//        public bool FulfillSlot(string grillType)
//        {
//            for (int i = 0; i < 3; i++)
//            {
//                if (_fulfilled[i] || _slotType[i] != grillType) continue;

//                _fulfilled[i] = true;

//                if (slotImages != null && i < slotImages.Length && slotImages[i] != null)
//                    slotImages[i].gameObject.SetActive(false);

//                if (fulfilledOverlays != null && i < fulfilledOverlays.Length && fulfilledOverlays[i] != null)
//                    fulfilledOverlays[i].SetActive(true);

//                Debug.Log($"[ChatheadOrder] '{name}' slot {i} fulfilled: '{grillType}'.");
//                return true;
//            }

//            Debug.Log($"[ChatheadOrder] '{name}' no open slot for '{grillType}'. " +
//                      $"Still need: [{string.Join(", ", GetPending())}]");
//            return false;
//        }

//        public bool IsComplete() => _fulfilled[0] && _fulfilled[1] && _fulfilled[2];
//        public string[] GetPending()
//        {
//            var l = new System.Collections.Generic.List<string>();
//            for (int i = 0; i < 3; i++) if (!_fulfilled[i]) l.Add(_slotType[i]);
//            return l.ToArray();
//        }
//    }
//}


using UnityEngine;
using UnityEngine.UI;

namespace ToriSausages
{
    /// <summary>
    /// One food variant that can appear in an order slot.
    /// grillType must exactly match DraggableSausage.grillType:
    ///   "Sausage"             — plain cooked sausage
    ///   "SausageKetchup"      — sausage with ketchup
    ///   "SausageMayo"         — sausage with mayo
    ///   "SausageKetchupMayo"  — sausage with both ketchup and mayo
    /// </summary>
    [System.Serializable]
    public class GrillOrderType
    {
        [Tooltip("Must match DraggableSausage.grillType exactly.\n" +
                 "Use: \"Sausage\", \"SausageKetchup\", \"SausageMayo\", or \"SausageKetchupMayo\"")]
        public string grillType;

        [Tooltip("Icon shown in the chat-bubble order slot.")]
        public Sprite icon;
    }

    /// <summary>
    /// Attach to the chat-bubble root (set INACTIVE in the Editor).
    /// All 3 slots are independently randomised each visit.
    ///
    /// VARIANTS — add up to 4 entries to grillTypes[]:
    ///   grillType="Sausage"            icon = plain sausage sprite
    ///   grillType="SausageKetchup"     icon = sausage + ketchup sprite
    ///   grillType="SausageMayo"        icon = sausage + mayo sprite
    ///   grillType="SausageKetchupMayo" icon = sausage + ketchup + mayo sprite
    ///
    /// FLOW
    ///   RandomizeOrder()  — called by ControllerCustomer on arrival
    ///   FulfillSlot(type) — called by DraggableSausage on mouth-drop
    ///   IsComplete()      — true when all 3 slots fulfilled
    /// </summary>
    public class ChatheadOrder : MonoBehaviour
    {
        [Header("Food Variants (add all 4 sausage types here)")]
        public GrillOrderType[] grillTypes;

        [Header("3 Slot Images inside the Chat Bubble")]
        public Image[] slotImages = new Image[3];

        [Header("Fulfilled Overlays (optional tick — same length as slotImages)")]
        public GameObject[] fulfilledOverlays = new GameObject[3];

        // ── Runtime ────────────────────────────────────────────────────────────────

        private string[] _slotType = new string[3];
        private bool[] _fulfilled = new bool[3];

        // ── Public API ─────────────────────────────────────────────────────────────

        public void RandomizeOrder()
        {
            if (grillTypes == null || grillTypes.Length == 0)
            {
                Debug.LogWarning("[ChatheadOrder] grillTypes[] is empty — add sausage variants.");
                return;
            }

            for (int i = 0; i < 3; i++)
            {
                GrillOrderType pick = grillTypes[Random.Range(0, grillTypes.Length)];
                _slotType[i] = pick.grillType;
                _fulfilled[i] = false;

                if (slotImages != null && i < slotImages.Length && slotImages[i] != null)
                {
                    slotImages[i].sprite = pick.icon;
                    slotImages[i].color = Color.white;
                    slotImages[i].gameObject.SetActive(true);
                }

                if (fulfilledOverlays != null && i < fulfilledOverlays.Length && fulfilledOverlays[i] != null)
                    fulfilledOverlays[i].SetActive(false);
            }

            Debug.Log($"[ChatheadOrder] '{name}' order: [{_slotType[0]}] [{_slotType[1]}] [{_slotType[2]}]");
        }

        /// <summary>
        /// Ticks the first unfulfilled slot that exactly matches grillType.
        /// Returns true if matched; false if this type isn't needed right now.
        /// </summary>
        public bool FulfillSlot(string grillType)
        {
            for (int i = 0; i < 3; i++)
            {
                if (_fulfilled[i] || _slotType[i] != grillType) continue;

                _fulfilled[i] = true;

                if (slotImages != null && i < slotImages.Length && slotImages[i] != null)
                    slotImages[i].gameObject.SetActive(false);

                if (fulfilledOverlays != null && i < fulfilledOverlays.Length && fulfilledOverlays[i] != null)
                    fulfilledOverlays[i].SetActive(true);

                Debug.Log($"[ChatheadOrder] '{name}' slot {i} fulfilled: '{grillType}'.");
                return true;
            }

            Debug.Log($"[ChatheadOrder] '{name}' no open slot for '{grillType}'. " +
                      $"Still need: [{string.Join(", ", GetPending())}]");
            return false;
        }

        public bool IsComplete() => _fulfilled[0] && _fulfilled[1] && _fulfilled[2];

        public string[] GetPending()
        {
            var l = new System.Collections.Generic.List<string>();
            for (int i = 0; i < 3; i++) if (!_fulfilled[i]) l.Add(_slotType[i]);
            return l.ToArray();
        }
    }
}