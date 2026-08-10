using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class RestaurantTutorialManager : MonoBehaviour
{
    public static RestaurantTutorialManager Instance;

    [Header("Hand Object (world-space)")]
    [Tooltip("The hand sprite/object that points at thought bubbles")]
    public Transform handTransform;

    [Header("Hand Tip Offset")]
    [Tooltip("Child object of the Hand that marks its tip. The hand is positioned so this object lands on the target.")]
    public Transform handTipOffset;

    [Header("Hand Bounce")]
    public float bounceMagnitude = 0.25f;
    public float bounceSpeed = 4.5f;

    [Header("Idle Hint Timing (Order 2+)")]
    [Tooltip("Seconds of idle (no bubble tapped) before the hint hand appears, from the 2nd order onward")]
    public float idleTimeBeforeHint = 3f;

    // ── internal state ──────────────────────────────────────────
    private enum TutorialStep
    {
        WaitingForFirstOrder,   // forced sequence hasn't started yet
        WaitingForOrderTap,     // waiting for the player to tap SOME order bubble (TB1)
        WaitingForServeTap,     // order taken — waiting for player to tap the order-done bubble (TB2)
        Done                    // forced sequence complete — idle-hint mode from now on
    }

    private TutorialStep step = TutorialStep.WaitingForFirstOrder;

    // The AI whose ORDER is being tracked through the forced first-time sequence.
    // This follows whichever bubble the player actually taps — NOT necessarily
    // the AI the hand happened to be pointing at.
    private AICustomer pendingOrderAI;

    // The AI the hand is currently visually pointing at (purely cosmetic — may
    // differ from pendingOrderAI if the player ignored the hint and tapped
    // a different AI's bubble).
    private AICustomer handTargetAI;

    private Coroutine bounceCoroutine;
    private Coroutine idleCoroutine;
    private Vector3 handBasePosition;

    private readonly List<AICustomer> activeAIs = new List<AICustomer>();

    // =========================================================================
    void Awake()
    {
        if (Instance == null) Instance = this;
        else { Destroy(gameObject); return; }
    }

    void Start()
    {
        if (handTransform != null)
            handTransform.gameObject.SetActive(false);
    }

    // ── Roster ───────────────────────────────────────────────────

    public void RegisterAI(AICustomer ai)
    {
        if (ai != null && !activeAIs.Contains(ai))
            activeAIs.Add(ai);
    }

    public void UnregisterAI(AICustomer ai)
    {
        activeAIs.Remove(ai);

        if (pendingOrderAI == ai)
        {
            pendingOrderAI = null;
            // Don't leave the forced sequence stuck forever waiting for a
            // bubble that will never appear — let normal play continue
            if (step == TutorialStep.WaitingForServeTap)
                step = TutorialStep.Done;
        }

        if (handTargetAI == ai)
        {
            handTargetAI = null;
            HideHand();
            if (step == TutorialStep.Done)
                StartIdleTimer();
        }
    }

    // =========================================================================
    // Called by AICustomer right after it reaches its chair and its order
    // thought bubble (TB1) becomes active.
    // =========================================================================
    public void OnThoughtBubbleShown(AICustomer ai, Transform bubble)
    {
        // ── First order ever: force the hand onto this AI's order bubble ──
        if (step == TutorialStep.WaitingForFirstOrder)
        {
            step = TutorialStep.WaitingForOrderTap;
            handTargetAI = ai;
            PlaceHandAt(bubble);
            return;
        }

        // ── Every order after the first: no forced hint, just restart the
        // idle timer so a hand hint appears if the player does nothing ──
        if (step == TutorialStep.Done)
            StartIdleTimer();
    }

    // =========================================================================
    // Called by AICustomer when Tory finishes cooking and this AI's serving
    // thought bubble (TB2) becomes active.
    // =========================================================================
    public void OnServingBubbleShown(AICustomer ai, Transform bubble)
    {
        // ── This is the AI whose order is being tracked through the forced
        // sequence, now that Tory finished cooking for them: point the hand
        // at THEIR order-done bubble — regardless of which AI the hand was
        // last pointing at ──
        if (step == TutorialStep.WaitingForServeTap && pendingOrderAI == ai)
        {
            handTargetAI = ai;
            PlaceHandAt(bubble);
            return;
        }

        // ── Otherwise (2nd+ order): just restart the idle timer ──
        if (step == TutorialStep.Done)
            StartIdleTimer();
    }

    // =========================================================================
    // Called by AICustomer when its bubble gets tapped, or when it leaves /
    // is destroyed.
    // =========================================================================
    public void OnThoughtBubbleHandled(AICustomer ai)
    {
        StopIdleTimer();

        // Hide the hand if it happened to be pointing at whichever AI's
        // bubble was just tapped (could be a different AI than the order
        // we're tracking, if the player ignored the hint)
        if (handTargetAI == ai)
        {
            handTargetAI = null;
            HideHand();
        }

        // ── Order-bubble (TB1) tap during the forced sequence ──
        // Whichever AI's order bubble the player taps becomes the order we
        // track — even if it wasn't the one the hand was pointing at. The
        // hint is now moot regardless of who it was pointing at, so hide it.
        if (step == TutorialStep.WaitingForOrderTap)
        {
            pendingOrderAI = ai;
            step = TutorialStep.WaitingForServeTap;
            handTargetAI = null;
            HideHand();
        }
        // ── Serve-bubble (TB2) tap during the forced sequence ──
        // Only completes the tutorial if it's the order we're actually tracking.
        else if (step == TutorialStep.WaitingForServeTap && pendingOrderAI == ai)
        {
            pendingOrderAI = null;
            step = TutorialStep.Done;
        }

        // From the 2nd order onward, every handled bubble restarts the idle timer
        if (step == TutorialStep.Done)
            StartIdleTimer();
    }

    // ── Idle timer (order 2+) ───────────────────────────────────

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
        ShowHandOnRandomAI();
    }

    private void ShowHandOnRandomAI()
    {
        // Build the list of AIs that currently have an open, unhandled bubble
        List<AICustomer> candidates = new List<AICustomer>();
        foreach (var ai in activeAIs)
            if (ai != null && ai.HasOpenBubble)
                candidates.Add(ai);

        if (candidates.Count == 0) return;

        AICustomer chosen = candidates[Random.Range(0, candidates.Count)];
        Transform bubble = chosen.GetOpenBubbleTransform();
        if (bubble == null) return;

        handTargetAI = chosen;
        PlaceHandAt(bubble);
    }

    // ── Hand placement ──────────────────────────────────────────

    private void PlaceHandAt(Transform target)
    {
        if (handTransform == null || target == null) return;

        StopBounce();
        StopIdleTimer();

        handTransform.position = target.position;

        // Shift so the hand TIP (a child object of the hand) lands on the target,
        // not the hand sprite's pivot/centre
        if (handTipOffset != null)
        {
            Vector3 tipOffset = handTransform.position - handTipOffset.position;
            handTransform.position += tipOffset;
        }

        handBasePosition = handTransform.position;

        handTransform.gameObject.SetActive(true);
        StartBounce(handBasePosition);
    }

    private void HideHand()
    {
        StopBounce();
        if (handTransform != null)
            handTransform.gameObject.SetActive(false);
    }

    // ── Bounce ────────────────────────────────────────────────────

    private void StartBounce(Vector3 basePos)
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

    private IEnumerator BounceLoop(Vector3 basePos)
    {
        while (true)
        {
            float y = Mathf.Sin(Time.unscaledTime * bounceSpeed) * bounceMagnitude;
            if (handTransform != null)
                handTransform.position = basePos + new Vector3(0f, y, 0f);
            yield return null;
        }
    }
}