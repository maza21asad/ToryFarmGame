using UnityEngine;
using DG.Tweening;

public class UIPulse : MonoBehaviour
{
    [Header("Pulse Settings")]
    [Tooltip("How large the element grows at the peak. 1.15 = 15% bigger.")]
    public float scaleAmount = 1.15f;

    [Tooltip("Duration of one half-pulse (up or down). 0.6 = gentle, 0.3 = fast.")]
    public float speed = 0.6f;

    [Tooltip("Seconds before pulsing starts.")]
    public float delay = 0f;

    [Tooltip("Ease curve for the pulse.")]
    public Ease easeType = Ease.InOutSine;

    [Header("Punch Mode (one-shot on Enable)")]
    [Tooltip("If true, plays a single punch-scale on Enable instead of looping.")]
    public bool punchOnEnable = false;
    public float punchStrength = 0.3f;
    public float punchDuration = 0.5f;

    private Vector3 originalScale;

    // ──────────────────────────────────────────────────────────
    void Awake()
    {
        originalScale = transform.localScale;
    }

    void OnEnable()
    {
        transform.localScale = originalScale;
        transform.DOKill();

        if (punchOnEnable)
        {
            // One-shot punch — great for "GO!!" or panel pop-in
            transform.DOPunchScale(Vector3.one * punchStrength, punchDuration, 5, 0.5f)
                     .SetDelay(delay)
                     .SetUpdate(true);
        }
        else
        {
            // Looping pulse
            transform.DOScale(originalScale * scaleAmount, speed)
                     .SetEase(easeType)
                     .SetLoops(-1, LoopType.Yoyo)
                     .SetDelay(delay)
                     .SetUpdate(true); // SetUpdate(true) = works even when Time.timeScale = 0
        }
    }

    void OnDisable()
    {
        transform.DOKill();
        transform.localScale = originalScale;
    }
}