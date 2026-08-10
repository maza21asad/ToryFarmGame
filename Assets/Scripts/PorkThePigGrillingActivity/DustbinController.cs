using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Attach to the dustbin UI GameObject.
///
/// Accepts two kinds of food:
///   1. BURNED grills — dragged via DraggableBurnedGrill (original path).
///   2. COOKED grills (extra food) — dragged via DraggableGrill and discarded
///      by the player before serving. DraggableGrill.TryDropOnDustbin() calls
///      ReceiveCookedGrill() then destroys the object itself.
///
/// SETUP
///   1. Add this component to the dustbin Image.
///   2. Create an empty child GameObject, position it over the dustbin opening,
///      and assign it to targetPoint.
///   3. Adjust targetRadius until the drop zone feels right.
/// </summary>
public class DustbinController : MonoBehaviour
{
    [Header("Drop Target")]
    [Tooltip("Empty RectTransform positioned over the dustbin opening.\n" +
             "Food must be dropped within targetRadius pixels of this point.")]
    public RectTransform targetPoint;

    [Tooltip("Screen-pixel radius of the drop zone around the target point.")]
    public float targetRadius = 120f;

    [Header("Score UI  (optional)")]
    public int penaltyPerBurn = 1;
    public Text scoreText;

    private int _discardCount = 0;

    // ── Public API ────────────────────────────────────────────────────────────

    /// <summary>
    /// Returns true if screenPos is within targetRadius pixels of the target point.
    /// Called by DraggableBurnedGrill and DraggableGrill on drop.
    /// </summary>
    public bool IsInsideTarget(Vector2 screenPos, Camera cam)
    {
        if (targetPoint == null)
        {
            Debug.LogWarning("[DustbinController] targetPoint not assigned!");
            return false;
        }

        Vector2 targetScreen = RectTransformUtility.WorldToScreenPoint(cam, targetPoint.position);
        float dist = Vector2.Distance(screenPos, targetScreen);
        return dist <= targetRadius;
    }

    /// <summary>
    /// Called by DraggableBurnedGrill when a burned grill is successfully dropped.
    /// Increments discard count, updates UI, and destroys the burned grill.
    /// </summary>
    public void ReceiveBurnedGrill(DraggableBurnedGrill burned)
    {
        if (burned == null) return;

        _discardCount++;

        DraggableGrill dg = burned.GetComponent<DraggableGrill>();
        string grillType = dg != null ? dg.grillType : "Unknown";

        Debug.Log($"[DustbinController] Discarded BURNED '{grillType}'. Total discarded: {_discardCount}");
        UpdateUI();

        PigGrillTutorialManager.Instance?.NotifyBurnedGrillDiscarded();
        Destroy(burned.gameObject);
        SoundManager.Instance.PlaySFX("TrashBin");
    }

    /// <summary>
    /// Called by DraggableGrill.TryDropOnDustbin() when a cooked (extra) grill
    /// is discarded. The grill object is destroyed by the caller.
    /// This method just handles logging and UI.
    /// </summary>
    public void ReceiveCookedGrill(string grillType)
    {
        _discardCount++;
        Debug.Log($"[DustbinController] Discarded COOKED '{grillType}'. Total discarded: {_discardCount}");
        UpdateUI();
        SoundManager.Instance.PlaySFX("TrashBin");
    }

    // ── Private ───────────────────────────────────────────────────────────────

    private void UpdateUI()
    {
        if (scoreText != null)
            scoreText.text = $"Discarded: {_discardCount}";
    }
}