using DG.Tweening;
using UnityEngine;

/// <summary>
/// Corn Field Tutorial Manager
///
/// Activates the hand on start, bounces it in place, hides it when cow arrives.
/// The hand GameObject is already positioned correctly in the scene.
/// </summary>
public class CornTutorialManager : MonoBehaviour
{
    [Header("References")]
    [Tooltip("The hand UI GameObject already placed in the scene (inactive in Inspector)")]
    public GameObject handObject;

    [Tooltip("The GrowZone RectTransform")]
    public RectTransform targetGrowZone;

    [Tooltip("The Cow's RectTransform")]
    public RectTransform cowRect;

    [Header("Arrival Detection")]
    [Tooltip("Distance in World-local units at which the hand hides. " +
             "Match this to roughly the GrowZone's half-width.")]
    public float arrivalRadius = 80f;

    [Header("Bob Animation")]
    [Tooltip("How many pixels the hand bobs up and down")]
    public float bobHeight = 20f;

    [Tooltip("Seconds for one full bob cycle (up + down)")]
    public float bobDuration = 0.6f;

    // ── Private ────────────────────────────────────────────────────────────

    private bool _tutorialDone = false;
    private RectTransform _handRect;
    private Tweener _bobTween;
    private float _startY;

    // ──────────────────────────────────────────────────────────────────────
    void Start()
    {
        if (handObject == null || targetGrowZone == null || cowRect == null)
        {
            Debug.LogWarning("[CornTutorialManager] Missing references — tutorial disabled.");
            enabled = false;
            return;
        }

        _handRect = handObject.GetComponent<RectTransform>();
        _startY = _handRect.anchoredPosition.y;

        handObject.SetActive(true);
        StartBob();
    }

    void Update()
    {
        if (_tutorialDone) return;

        float dist = Vector2.Distance(cowRect.anchoredPosition,
                                      targetGrowZone.anchoredPosition);
        if (dist <= arrivalRadius)
            CompleteTutorial();
    }

    // ── Bob ────────────────────────────────────────────────────────────────

    void StartBob()
    {
        _bobTween?.Kill();
        _handRect.anchoredPosition = new Vector2(_handRect.anchoredPosition.x, _startY);

        _bobTween = _handRect
            .DOAnchorPosY(_startY + bobHeight, bobDuration * 0.5f)
            .SetEase(Ease.InOutSine)
            .SetLoops(-1, LoopType.Yoyo);
    }

    // ── Complete ───────────────────────────────────────────────────────────

    void CompleteTutorial()
    {
        _tutorialDone = true;
        _bobTween?.Kill();
        handObject.SetActive(false);
    }

    // ── Public API ─────────────────────────────────────────────────────────

    public void ResetTutorial()
    {
        _tutorialDone = false;
        _startY = _handRect.anchoredPosition.y;
        handObject.SetActive(true);
        StartBob();
    }

    public void SkipTutorial() => CompleteTutorial();

    void OnDestroy() => _bobTween?.Kill();
}