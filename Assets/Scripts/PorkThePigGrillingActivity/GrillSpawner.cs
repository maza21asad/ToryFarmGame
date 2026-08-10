//using UnityEngine;
//using UnityEngine.EventSystems;
//using UnityEngine.UI;

///// <summary>
///// Attach this to each raw shrimp icon in your tray.
/////
///// SETUP
/////   1. Assign shrimpPrefab  — the raw shrimp prefab (has DraggableGrill + GrillCooker).
/////   2. Set grillType        — e.g. "Shrimp".
/////   3. Make sure this GameObject's Image has Raycast Target = ON.
/////   4. Sprite texture must have Read/Write Enabled (for alpha hit-test).
/////
///// WHAT IT DOES
/////   When the player drags this icon, it spawns a copy of shrimpPrefab at the
/////   pointer position and hands drag control to that copy.
///// </summary>
//public class GrillSpawner : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
//{
//    [Header("Prefab")]
//    [Tooltip("Raw shrimp prefab to spawn when the player drags from this icon.")]
//    public GameObject shrimpPrefab;

//    [Header("Grill Type")]
//    [Tooltip("Passed to DraggableGrill so the stove and plate know what kind of food this is.")]
//    public string grillType = "Shrimp";

//    [Header("Alpha Hit-Test")]
//    [Range(0f, 1f)]
//    [Tooltip("Pixels below this alpha value are not draggable. Requires Read/Write Enabled on the texture.")]
//    public float alphaThreshold = 0.1f;

//    // The live instance the player is currently dragging
//    private DraggableGrill _active;

//    private void Awake()
//    {
//        Image img = GetComponent<Image>();
//        if (img != null)
//            img.alphaHitTestMinimumThreshold = alphaThreshold;
//    }

//    public void OnBeginDrag(PointerEventData eventData)
//    {
//        if (shrimpPrefab == null)
//        {
//            Debug.LogWarning("[GrillSpawner] shrimpPrefab is not assigned!");
//            return;
//        }

//        Canvas canvas = GetComponentInParent<Canvas>();
//        if (canvas == null) return;

//        // Spawn at the pointer's position on the canvas
//        GameObject spawned = Instantiate(shrimpPrefab, canvas.transform);

//        RectTransformUtility.ScreenPointToLocalPointInRectangle(
//            canvas.GetComponent<RectTransform>(),
//            eventData.position,
//            eventData.pressEventCamera,
//            out Vector2 localPoint);

//        spawned.GetComponent<RectTransform>().localPosition = localPoint;

//        _active = spawned.GetComponent<DraggableGrill>();
//        if (_active != null)
//        {
//            _active.grillType = grillType;
//            _active.BeginDragFromSpawner(eventData);
//        }
//    }

//    public void OnDrag(PointerEventData eventData)
//    {
//        _active?.OnDrag(eventData);
//    }

//    public void OnEndDrag(PointerEventData eventData)
//    {
//        _active?.OnEndDrag(eventData);
//        _active = null;
//    }
//}

using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// Attach this to each raw shrimp icon in your tray.
///
/// SETUP
///   1. Assign shrimpPrefab  — the raw shrimp prefab (has DraggableGrill + GrillCooker).
///   2. Set grillType        — e.g. "Shrimp".
///   3. Make sure this GameObject's Image has Raycast Target = ON.
///   4. Sprite texture must have Read/Write Enabled (for alpha hit-test).
///
/// WHAT IT DOES
///   When the player drags this icon, it spawns a copy of shrimpPrefab at the
///   pointer position and hands drag control to that copy.
/// </summary>
public class GrillSpawner : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    [Header("Prefab")]
    [Tooltip("Raw shrimp prefab to spawn when the player drags from this icon.")]
    public GameObject shrimpPrefab;

    [Header("Grill Type")]
    [Tooltip("Passed to DraggableGrill so the stove and plate know what kind of food this is.")]
    public string grillType = "Shrimp";

    [Header("Alpha Hit-Test")]
    [Range(0f, 1f)]
    [Tooltip("Pixels below this alpha value are not draggable. Requires Read/Write Enabled on the texture.")]
    public float alphaThreshold = 0.1f;

    // The live instance the player is currently dragging
    private DraggableGrill _active;

    private void Awake()
    {
        Image img = GetComponent<Image>();
        if (img != null)
            img.alphaHitTestMinimumThreshold = alphaThreshold;
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        // Block spawning until the player presses OK on the intro panel.
        if (CustomerManager.Instance != null && !CustomerManager.Instance.IsGameStarted)
            return;

        if (shrimpPrefab == null)
        {
            Debug.LogWarning("[GrillSpawner] shrimpPrefab is not assigned!");
            return;
        }

        Canvas canvas = GetComponentInParent<Canvas>();
        if (canvas == null) return;

        // Spawn at the pointer's position on the canvas
        GameObject spawned = Instantiate(shrimpPrefab, canvas.transform);

        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            canvas.GetComponent<RectTransform>(),
            eventData.position,
            eventData.pressEventCamera,
            out Vector2 localPoint);

        spawned.GetComponent<RectTransform>().localPosition = localPoint;

        _active = spawned.GetComponent<DraggableGrill>();
        if (_active != null)
        {
            _active.grillType = grillType;
            _active.BeginDragFromSpawner(eventData);
        }
    }

    public void OnDrag(PointerEventData eventData)
    {
        _active?.OnDrag(eventData);
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        _active?.OnEndDrag(eventData);
        _active = null;
    }
}