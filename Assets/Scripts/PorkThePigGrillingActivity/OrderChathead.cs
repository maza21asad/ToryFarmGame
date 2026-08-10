//using UnityEngine;
//using UnityEngine.UI;
//using System.Collections.Generic;

///// <summary>Describes a type of grill that can appear in a customer order.</summary>
//[System.Serializable]
//public class GrillOrderItem
//{
//    [Tooltip("Must match DraggableGrill.grillType exactly — case-sensitive.\n" +
//             "E.g. \"Shrimp\" or \"RedMeat\"")]
//    public string grillType;

//    [Tooltip("Icon sprite shown in the order slot image.")]
//    public Sprite icon;
//}

///// <summary>
///// Attach to the chat-bubble root GameObject (set INACTIVE in the Editor).
///// CustomerController shows it and calls RandomizeOrder() when the customer arrives.
/////
///// SETUP
/////   1. Create up to 3 child Image GameObjects for order slots.
/////      Assign each Image to orderSlotImages[].
/////   2. Optionally create fulfilled-overlay GameObjects (tick, checkmark, etc.)
/////      and assign them to fulfilledOverlays[]. Must match orderSlotImages length.
/////   3. Add at least one GrillOrderItem to possibleGrills.
/////   4. Adjust minOrderSize / maxOrderSize to control how many items appear.
///// </summary>
//public class OrderChathead : MonoBehaviour
//{
//    [Header("Order Slot Images  (up to 3)")]
//    [Tooltip("Image components that show each ordered grill icon.")]
//    public Image[] orderSlotImages = new Image[3];

//    [Header("Fulfilled Overlays  (optional)")]
//    [Tooltip("A checkmark / green-tick GameObject shown on each slot once it is delivered.\n" +
//             "Length must match orderSlotImages.")]
//    public GameObject[] fulfilledOverlays = new GameObject[3];

//    [Header("Grill Types Available in Orders")]
//    public GrillOrderItem[] possibleGrills;

//    [Header("Order Size")]
//    [Min(1)]
//    [Tooltip("Minimum number of grills in one order.")]
//    public int minOrderSize = 1;

//    [Min(1)]
//    [Tooltip("Maximum number of grills in one order (capped to slot count).")]
//    public int maxOrderSize = 3;

//    // ── State ─────────────────────────────────────────────────────────────────

//    private string[] _order;      // null = slot not active this order
//    private bool[] _fulfilled;

//    // ── Public API ─────────────────────────────────────────────────────────────

//    /// <summary>
//    /// Randomly fills order slots and resets all fulfilled states.
//    /// Called by CustomerController.Arrived().
//    /// </summary>
//    public void RandomizeOrder()
//    {
//        int slotCount = orderSlotImages != null ? orderSlotImages.Length : 0;
//        _order = new string[slotCount];
//        _fulfilled = new bool[slotCount];

//        int orderSize = Mathf.Clamp(Random.Range(minOrderSize, maxOrderSize + 1), 1, slotCount);

//        for (int i = 0; i < slotCount; i++)
//        {
//            bool active = (i < orderSize) && (possibleGrills != null && possibleGrills.Length > 0);

//            if (active)
//            {
//                GrillOrderItem item = possibleGrills[Random.Range(0, possibleGrills.Length)];
//                _order[i] = item.grillType;

//                if (orderSlotImages[i] != null)
//                {
//                    orderSlotImages[i].sprite = item.icon;
//                    orderSlotImages[i].color = Color.white;
//                    orderSlotImages[i].gameObject.SetActive(true);
//                }
//            }
//            else
//            {
//                _order[i] = null;
//                _fulfilled[i] = true;   // inactive slots count as already done

//                if (orderSlotImages[i] != null)
//                    orderSlotImages[i].gameObject.SetActive(false);
//            }

//            // Hide fulfilled overlay
//            if (fulfilledOverlays != null && i < fulfilledOverlays.Length && fulfilledOverlays[i] != null)
//                fulfilledOverlays[i].SetActive(false);
//        }

//        Debug.Log($"[OrderChathead] New order ({gameObject.name}): {string.Join(", ", GetPendingOrders())}");
//    }

//    /// <summary>
//    /// Marks the first unfulfilled slot matching grillType as done.
//    /// Returns true if a match was found.
//    /// </summary>
//    public bool FulfillSlot(string grillType)
//    {
//        if (_order == null) return false;

//        for (int i = 0; i < _order.Length; i++)
//        {
//            if (_fulfilled[i] || _order[i] != grillType) continue;

//            _fulfilled[i] = true;

//            // Dim the slot image to indicate it is filled.
//            if (orderSlotImages != null && i < orderSlotImages.Length && orderSlotImages[i] != null)
//                orderSlotImages[i].color = new Color(0.4f, 0.8f, 0.4f, 0.6f);  // greenish dim

//            // Show fulfilled overlay (tick / checkmark).
//            if (fulfilledOverlays != null && i < fulfilledOverlays.Length && fulfilledOverlays[i] != null)
//                fulfilledOverlays[i].SetActive(true);

//            Debug.Log($"[OrderChathead] Slot {i} fulfilled: {grillType}");
//            return true;
//        }

//        return false;   // no matching unfulfilled slot
//    }

//    /// <summary>True when every active slot has been fulfilled.</summary>
//    public bool IsComplete()
//    {
//        if (_fulfilled == null) return false;
//        foreach (bool f in _fulfilled)
//            if (!f) return false;
//        return true;
//    }

//    /// <summary>Returns the list of grill types still pending.</summary>
//    public List<string> GetPendingOrders()
//    {
//        var list = new List<string>();
//        if (_order == null) return list;
//        for (int i = 0; i < _order.Length; i++)
//            if (!_fulfilled[i] && _order[i] != null)
//                list.Add(_order[i]);
//        return list;
//    }
//}

using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// One grill type that can appear in an order slot.
/// grillType MUST exactly match GrillSpawner.grillType (e.g. "RedMeat", "Shrimp").
/// </summary>
[System.Serializable]
public class GrillOrderType
{
    [Tooltip("Must match GrillSpawner.grillType exactly — e.g. \"RedMeat\" or \"Shrimp\"")]
    public string grillType;

    [Tooltip("Icon sprite shown inside the chat-bubble slot.")]
    public Sprite icon;
}

/// <summary>
/// Attach to the chat-bubble root (set INACTIVE in the Editor).
/// CustomerController shows it and calls RandomizeOrder() when the customer arrives.
///
/// ORDER LOGIC
///   All 3 slots are filled every time. Each slot is independently and randomly
///   assigned one grill type from your grillTypes[] list.
///
///   Example outcomes with RedMeat + Shrimp:
///     [RedMeat, RedMeat, Shrimp]
///     [Shrimp,  Shrimp,  Shrimp]
///     [RedMeat, Shrimp,  RedMeat]  …and so on.
///
///   The player must cook and deliver one matching grill per slot.
///   Delivering a "RedMeat" to a customer with [RedMeat, Shrimp, RedMeat]
///   ticks the FIRST unfulfilled RedMeat slot — order matters left-to-right internally.
///
/// SETUP
///   1. Create 3 child Image GameObjects inside the bubble → assign to slotImages[0-2].
///   2. (Optional) Create 3 checkmark child GameObjects → assign to fulfilledOverlays[0-2].
///   3. Add grillTypes entries: one per food type (RedMeat, Shrimp, etc.).
///      grillType string must match GrillSpawner.grillType exactly.
/// </summary>
public class OrderChathead : MonoBehaviour
{
    // ── Inspector ──────────────────────────────────────────────────────────────

    [Header("Available Grill Types  (add RedMeat + Shrimp here)")]
    [Tooltip("Each entry = one food type.\n" +
             "grillType must match GrillSpawner.grillType exactly (case-sensitive).")]
    public GrillOrderType[] grillTypes;

    [Header("3 Slot Images inside the Chat Bubble")]
    public Image[] slotImages = new Image[3];

    [Header("Fulfilled Overlays (optional tick/checkmark — same length as slotImages)")]
    public GameObject[] fulfilledOverlays = new GameObject[3];

    // ── Runtime ────────────────────────────────────────────────────────────────

    private string[] _slotType = new string[3];   // grill type per slot
    private bool[] _fulfilled = new bool[3];       // whether each slot is done

    // ── Public API ─────────────────────────────────────────────────────────────

    /// <summary>
    /// Randomly assigns a grill type to each of the 3 slots and resets fulfillment.
    /// Called by CustomerController the moment the customer reaches the counter.
    /// </summary>
    public void RandomizeOrder()
    {
        if (grillTypes == null || grillTypes.Length == 0)
        {
            Debug.LogWarning("[OrderChathead] grillTypes[] is empty — add at least RedMeat and Shrimp.");
            return;
        }

        for (int i = 0; i < 3; i++)
        {
            // Pick one grill type at random for this slot.
            GrillOrderType pick = grillTypes[Random.Range(0, grillTypes.Length)];

            _slotType[i] = pick.grillType;
            _fulfilled[i] = false;

            // Show the icon at full opacity (= unfulfilled).
            if (slotImages != null && i < slotImages.Length && slotImages[i] != null)
            {
                slotImages[i].sprite = pick.icon;
                slotImages[i].color = Color.white;
                slotImages[i].gameObject.SetActive(true);
            }

            // Hide any checkmark overlay.
            if (fulfilledOverlays != null && i < fulfilledOverlays.Length && fulfilledOverlays[i] != null)
                fulfilledOverlays[i].SetActive(false);
        }

        Debug.Log($"[OrderChathead] '{name}' new order: [{_slotType[0]}] [{_slotType[1]}] [{_slotType[2]}]");
    }

    /// <summary>
    /// Called when the player delivers a cooked grill to this customer's mouth.
    /// Ticks off the first unfulfilled slot that matches grillType.
    /// Returns true if a match was found; false if this grill wasn't needed.
    /// </summary>
    public bool FulfillSlot(string grillType)
    {
        for (int i = 0; i < 3; i++)
        {
            if (_fulfilled[i] || _slotType[i] != grillType) continue;

            _fulfilled[i] = true;

            // Hide the slot icon — the grill has been eaten, so the order item disappears.
            if (slotImages != null && i < slotImages.Length && slotImages[i] != null)
                slotImages[i].gameObject.SetActive(false);

            // Show optional tick/checkmark overlay if assigned.
            if (fulfilledOverlays != null && i < fulfilledOverlays.Length && fulfilledOverlays[i] != null)
                fulfilledOverlays[i].SetActive(true);

            Debug.Log($"[OrderChathead] '{name}' slot {i} fulfilled: '{grillType}'.");
            return true;
        }

        Debug.Log($"[OrderChathead] '{name}' no open slot for '{grillType}'. " +
                  $"Still need: [{string.Join(", ", GetPending())}]");
        return false;
    }

    /// <summary>True when every slot has been fulfilled.</summary>
    public bool IsComplete() => _fulfilled[0] && _fulfilled[1] && _fulfilled[2];

    /// <summary>Debug helper — returns types of unfulfilled slots.</summary>
    public string[] GetPending()
    {
        var list = new System.Collections.Generic.List<string>();
        for (int i = 0; i < 3; i++)
            if (!_fulfilled[i]) list.Add(_slotType[i]);
        return list.ToArray();
    }
}