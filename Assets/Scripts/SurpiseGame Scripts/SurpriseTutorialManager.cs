using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class SurpriseTutorialManager : MonoBehaviour
{
    public static SurpriseTutorialManager Instance;

    [Header("Hand Object")]
    [Tooltip("The Hand RectTransform sitting on the Canvas")]
    public RectTransform handRect;

    [Header("Timing")]
    [Tooltip("Seconds of idle before the hint hand appears (Level 2+)")]
    public float idleTimeBeforeHint = 3f;

    [Header("Hand Tip Offset")]
    [Tooltip("Child object of the Hand that marks its tip (e.g. fingertip). The hand is positioned so this object lands on the target.")]
    public RectTransform handTipOffset;

    [Header("Hand Bounce")]
    public float bounceMagnitude = 14f;
    public float bounceSpeed = 4.5f;

    // ── internal state ────────────────────────────────────────────────────────
    private int currentRound = 1;
    private bool lidPhase = true;   // true = pointing at lid, false = pointing at food
    private RectTransform[] activeLidRects;
    private RectTransform revealedFoodRect;
    private Coroutine idleCoroutine;
    private Coroutine bounceCoroutine;
    private Vector2 handBaseAnchored;

    // =========================================================================
    void Awake()
    {
        if (Instance == null) Instance = this;
        else { Destroy(gameObject); return; }
    }

    void Start()
    {
        if (handRect != null)
            handRect.gameObject.SetActive(false);
    }

    // =========================================================================
    // Called by RoundManager at the start of every round
    // =========================================================================
    public void OnRoundStart(int round, List<RectTransform> lidRects)
    {
        currentRound = round;
        activeLidRects = lidRects.ToArray();
        revealedFoodRect = null;
        lidPhase = true;

        StopIdleTimer();
        HideHand();

        if (currentRound == 1)
            ShowHandOnRandomLid();   // Level 1 → show immediately
        else
            StartIdleTimer();        // Level 2+ → only show after player is idle
    }

    // =========================================================================
    // Called by LidButtonAnimation the moment a lid finishes opening.
    // Pass the RectTransform of the food button that is now visible.
    // =========================================================================
    public void OnLidOpened(RectTransform foodRect)
    {
        revealedFoodRect = foodRect;
        lidPhase = false;

        StopIdleTimer();
        HideHand();

        if (currentRound == 1)
            ShowHandOnFood();        // Level 1 → point at food immediately
        else
            StartIdleTimer();        // Level 2+ → restart idle timer for food phase
    }

    // =========================================================================
    // Called by RandomFoodButton when the food button is clicked
    // =========================================================================
    public void OnFoodClicked()
    {
        StopIdleTimer();
        HideHand();
    }

    // ── Hand placement ────────────────────────────────────────────────────────

    private void ShowHandOnRandomLid()
    {
        if (activeLidRects == null || activeLidRects.Length == 0) return;
        PlaceHandAt(activeLidRects[Random.Range(0, activeLidRects.Length)]);
    }

    private void ShowHandOnFood()
    {
        if (revealedFoodRect == null) return;
        PlaceHandAt(revealedFoodRect);
    }

    private void PlaceHandAt(RectTransform target)
    {
        if (handRect == null || target == null) return;

        StopBounce();

        // Use world position so it works regardless of Canvas nesting
        handRect.position = target.position;

        // Shift so the hand TIP (a child object of the hand) lands on the target,
        // not the hand image's pivot/centre
        if (handTipOffset != null)
        {
            Vector3 tipWorldOffset = handRect.position - handTipOffset.position;
            handRect.position += tipWorldOffset;
        }

        handBaseAnchored = handRect.anchoredPosition;

        handRect.gameObject.SetActive(true);
        StartBounce(handBaseAnchored);
    }

    private void HideHand()
    {
        StopBounce();
        if (handRect != null)
            handRect.gameObject.SetActive(false);
    }

    // ── Idle timer ────────────────────────────────────────────────────────────

    private void StartIdleTimer()
    {
        StopIdleTimer();
        idleCoroutine = StartCoroutine(IdleTimerRoutine());
    }

    private void StopIdleTimer()
    {
        if (idleCoroutine != null)
        {
            StopCoroutine(idleCoroutine);
            idleCoroutine = null;
        }
    }

    private IEnumerator IdleTimerRoutine()
    {
        yield return new WaitForSeconds(idleTimeBeforeHint);

        if (lidPhase) ShowHandOnRandomLid();
        else ShowHandOnFood();
    }

    // ── Bounce ────────────────────────────────────────────────────────────────

    private void StartBounce(Vector2 basePos)
    {
        StopBounce();
        bounceCoroutine = StartCoroutine(BounceLoop(basePos));
    }

    private void StopBounce()
    {
        if (bounceCoroutine != null)
        {
            StopCoroutine(bounceCoroutine);
            bounceCoroutine = null;
        }
    }

    private IEnumerator BounceLoop(Vector2 basePos)
    {
        while (true)
        {
            float y = Mathf.Sin(Time.unscaledTime * bounceSpeed) * bounceMagnitude;
            if (handRect != null)
                handRect.anchoredPosition = basePos + new Vector2(0f, y);
            yield return null;
        }
    }
}