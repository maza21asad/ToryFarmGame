using UnityEngine;

/// <summary>
/// Phase 1 — BG scrolls left to keep Tori locked at followStartX on screen.
/// Phase 2 — Once BGEndMarker hits the canvas right edge, BG stops.
///            Tori is then unlocked and walks forward on screen toward the bell.
/// </summary>
public class Backgroundscroller : MonoBehaviour
{
    [Header("References")]
    public Cowanimationcontroller cowController;
    public RectTransform cowRect;
    public RectTransform backgroundRect;

    [Tooltip("Empty child of BGImage placed at the right edge of the background art.")]
    public RectTransform bgEndMarker;

    [Header("Scroll Settings")]
    [Tooltip("The screen X at which Tori locks and BG starts scrolling (Phase 1).")]
    public float followStartX = 643f;

    [Tooltip("Hard floor fallback if canvas/marker can't be found.")]
    public float maxScrollX = -11000f;

    // ── Private ────────────────────────────────────────────────
    private float bgStartX;
    private float bgClampedX;          // the X where BG is frozen in Phase 2
    private RectTransform canvasRect;

    private bool bgClamped = false;    // true once Phase 2 begins
    private float clampedWorldX;       // worldX value at the moment BG was clamped

    // ──────────────────────────────────────────────────────────
    void Start()
    {
        bgStartX = backgroundRect.anchoredPosition.x;

        if (cowController != null)
            cowController.worldX = cowRect.anchoredPosition.x;

        Canvas canvas = backgroundRect.GetComponentInParent<Canvas>();
        if (canvas != null)
            canvasRect = canvas.GetComponent<RectTransform>();
        else
            Debug.LogError("[BGScroller] Could not find Canvas via backgroundRect!");
    }

    // ──────────────────────────────────────────────────────────
    void LateUpdate()
    {
        if (cowController == null || cowRect == null || backgroundRect == null) return;

        float worldX = cowController.worldX;

        // ── Before follow threshold: Tori walks freely, BG stays put ──
        if (worldX <= followStartX)
        {
            cowRect.anchoredPosition = new Vector2(worldX, cowRect.anchoredPosition.y);
            backgroundRect.anchoredPosition = new Vector2(bgStartX, backgroundRect.anchoredPosition.y);
            return;
        }

        // ── Phase 2 already locked — Tori walks forward on screen ──
        if (bgClamped)
        {
            // BG stays frozen
            backgroundRect.anchoredPosition = new Vector2(bgClampedX, backgroundRect.anchoredPosition.y);

            // Tori moves rightward on screen from followStartX
            float screenX = followStartX + (worldX - clampedWorldX);
            cowRect.anchoredPosition = new Vector2(screenX, cowRect.anchoredPosition.y);
            return;
        }

        // ── Phase 1 — lock Tori at followStartX, scroll BG ────
        cowRect.anchoredPosition = new Vector2(followStartX, cowRect.anchoredPosition.y);

        float overflow = worldX - followStartX;
        float markerClamp = GetMarkerClampX();
        float targetBgX = bgStartX - overflow;

        if (targetBgX <= markerClamp)
        {
            // BG has hit its limit — freeze it and switch to Phase 2
            bgClampedX = markerClamp;
            clampedWorldX = worldX;
            bgClamped = true;

            backgroundRect.anchoredPosition = new Vector2(bgClampedX, backgroundRect.anchoredPosition.y);
            Debug.Log($"[BGScroller] BG clamped at bgX={bgClampedX:F1}, worldX={worldX:F1} — Tori now walks forward.");
        }
        else
        {
            backgroundRect.anchoredPosition = new Vector2(targetBgX, backgroundRect.anchoredPosition.y);
        }
    }

    // ──────────────────────────────────────────────────────────
    // Returns the most-negative BG X allowed before BGEndMarker
    // left edge would pass the canvas right edge.
    // ──────────────────────────────────────────────────────────
    float GetMarkerClampX()
    {
        if (canvasRect == null || bgEndMarker == null) return maxScrollX;

        Vector3[] markerCorners = new Vector3[4];
        bgEndMarker.GetWorldCorners(markerCorners);  // 0=BL, 1=TL, 2=TR, 3=BR

        Vector3[] canvasCorners = new Vector3[4];
        canvasRect.GetWorldCorners(canvasCorners);

        float markerLeft = markerCorners[0].x;
        float canvasRight = canvasCorners[2].x;

        float shiftNeeded = markerLeft - canvasRight;
        float clampX = backgroundRect.anchoredPosition.x - shiftNeeded;

        return Mathf.Max(clampX, maxScrollX);
    }

    // ──────────────────────────────────────────────────────────
    // Call this on game restart to reset Phase 1/2 state
    // ──────────────────────────────────────────────────────────
    public void ResetScroller()
    {
        bgClamped = false;
        clampedWorldX = 0f;
        bgClampedX = 0f;

        if (backgroundRect != null)
            backgroundRect.anchoredPosition = new Vector2(bgStartX, backgroundRect.anchoredPosition.y);

        if (cowController != null)
            cowController.worldX = cowRect.anchoredPosition.x;
    }
}