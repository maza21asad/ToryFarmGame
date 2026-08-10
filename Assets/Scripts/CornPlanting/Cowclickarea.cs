using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// Attach this to your BG Image (the full-screen background on the Canvas).
/// Tapping walks the cow; dragging pans the camera (handled in CameraFollow).
///
/// CameraFollow calls NotifyDragStarted() the moment a drag threshold is exceeded,
/// which prevents the tap-walk from firing when the pointer is released.
/// </summary>
public class CowClickArea : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
{
    [Tooltip("Drag the Cow GameObject here")]
    public CowController cowController;

    [Tooltip("Pixels the pointer must travel before it counts as a drag (not a tap). " +
             "Should match the value set in CameraFollow.dragThresholdPixels.")]
    public float tapThresholdPixels = 10f;

    private bool _pointerDown;
    private Vector2 _pointerDownPos;
    private bool _suppressNextTap;   // set by CameraFollow when drag is confirmed

    public void OnPointerDown(PointerEventData eventData)
    {
        _pointerDown = true;
        _pointerDownPos = eventData.position;
        _suppressNextTap = false;
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        if (!_pointerDown) return;
        _pointerDown = false;

        if (_suppressNextTap) return;

        float dist = Vector2.Distance(eventData.position, _pointerDownPos);
        if (dist >= tapThresholdPixels) return;   // moved too far — treat as drag, not tap

        // Clean tap — walk the cow
        if (cowController == null) return;
        cowController.WalkTo(eventData.position);
    }

    /// <summary>
    /// Called by CameraFollow as soon as the drag threshold is crossed.
    /// Prevents the upcoming pointer-up from triggering a walk command.
    /// </summary>
    public void NotifyDragStarted()
    {
        _suppressNextTap = true;
    }
}