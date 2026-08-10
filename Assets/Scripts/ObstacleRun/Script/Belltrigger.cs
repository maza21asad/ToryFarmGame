using UnityEngine;
using UnityEngine.UI;
using System.Collections;


public class BellTrigger : MonoBehaviour
{
    [Header("Cow Hit Zone")]
    public RectTransform cowHitZone;

    [Header("Bell Hit Zones")]
    public RectTransform bellHitZone;       // triggers hit animation only
    public RectTransform bellHitingZone;    // triggers full celebration

    [Header("References")]
    public Cowanimationcontroller cowController;

    [Header("Friends")]
    public FriendAnimationController[] friends;

    [Header("Bell Ring Animation")]
    public Sprite[] bellRingFrames;
    public float bellFPS = 12f;

    [Header("Timing")]
    [Tooltip("Seconds to wait after hit starts before celebrations fire. Match to: hitFrames / hitFPS.")]
    public float hitAnimationDuration = 0.5f;

    [Tooltip("Seconds after celebrations start before congrats panel appears.")]
    public float congratsDelay = 1.0f;

    [Tooltip("Seconds after BellHitZone contact before bell ring animation starts.")]
    public float bellAnimationDelay = 0.3f;

    // ── Private ────────────────────────────────────────────────
    private Image bellImage;
    private bool hitTriggered = false;
    private bool celebrationTriggered = false;

    // ──────────────────────────────────────────────────────────
    void Start()
    {
        bellImage = GetComponent<Image>();

        if (cowController == null)
            cowController = Object.FindFirstObjectByType<Cowanimationcontroller>();

        if (cowHitZone == null && cowController != null)
        {
            Transform found = cowController.transform.Find("CowHitZone");
            if (found != null) cowHitZone = found.GetComponent<RectTransform>();
        }

        if (bellHitZone == null)
            Debug.LogError("[Bell] BellHitZone not assigned in Inspector!");
        if (bellHitingZone == null)
            Debug.LogError("[Bell] BellHitingZone not assigned in Inspector!");
        if (cowHitZone == null)
            Debug.LogError("[Bell] CowHitZone not assigned in Inspector!");
        if (cowController == null)
            Debug.LogError("[Bell] CowController not found!");
    }

    void Update()
    {
        if (cowController == null || cowHitZone == null) return;

        Cowanimationcontroller.CowState s = cowController.CurrentState;

        // ── Zone 1: BellHitZone → Hit animation + delayed bell ─
        if (!hitTriggered
            && bellHitZone != null
            && (s == Cowanimationcontroller.CowState.Run || s == Cowanimationcontroller.CowState.Jump)
            && Overlaps(cowHitZone, bellHitZone))
        {
            hitTriggered = true;
            Debug.Log("[Bell] ✅ Zone1 hit! Playing hit animation.");
            cowController.PlayHit();
            StartCoroutine(DelayedBellAnimation());
        }

        // ── Zone 2: BellHitingZone → Full celebration ──────────
        if (!celebrationTriggered
            && bellHitingZone != null
            && s != Cowanimationcontroller.CowState.Win
            && s != Cowanimationcontroller.CowState.Idle
            && Overlaps(cowHitZone, bellHitingZone))
        {
            celebrationTriggered = true;
            Debug.Log("[Bell] ✅ Zone2 hit! Starting celebration sequence.");
            StartCoroutine(CelebrationSequence());
        }
    }

    // ──────────────────────────────────────────────────────────
    IEnumerator DelayedBellAnimation()
    {
        yield return new WaitForSeconds(bellAnimationDelay);
        StartCoroutine(PlayBellAnimation());
    }

    // ──────────────────────────────────────────────────────────
    IEnumerator CelebrationSequence()
    {
        Debug.Log("[Bell] CelebrationSequence started.");

        // Stop cow and play hit if not already hitting
        cowController.StopRunning();
        if (cowController.CurrentState != Cowanimationcontroller.CowState.Hit)
        {
            Debug.Log("[Bell] Cow not in Hit state — calling PlayHit.");
            cowController.PlayHit();
        }
        else
        {
            Debug.Log("[Bell] Cow already in Hit state — skipping PlayHit.");
        }

        // Wait for hit animation to finish
        Debug.Log($"[Bell] Waiting {hitAnimationDuration}s for hit animation...");
        yield return new WaitForSeconds(hitAnimationDuration);

        // All celebrations simultaneously
        Debug.Log("[Bell] Hit done — firing all celebrations now!");
        cowController.PlayWinDirect();
        StartCoroutine(PlayBellAnimation());

        if (friends != null)
            foreach (var f in friends)
                if (f != null) f.PlayCelebrate();

        // Wait then show congrats panel
        Debug.Log($"[Bell] Waiting {congratsDelay}s before congrats panel...");
        yield return new WaitForSeconds(congratsDelay);

        Debug.Log($"[Bell] RunnerGameManager.Instance = {RunnerGameManager.Instance}");
        if (RunnerGameManager.Instance != null)
        {
            Debug.Log("[Bell] Calling ShowRunnerCongrats!");
            RunnerGameManager.Instance.ShowCongrats();
        }
        else
        {
            Debug.LogError("[Bell] RunnerGameManager.Instance is NULL! " +
                           "Make sure RunnerGameManager script is in the scene.");
        }
    }

    // ──────────────────────────────────────────────────────────
    IEnumerator PlayBellAnimation()
    {
        if (bellRingFrames == null || bellRingFrames.Length == 0)
        {
            Debug.LogWarning("[Bell] No bell ring frames assigned!");
            yield break;
        }

        SoundManager.Instance.PlaySFX("Bell");

        int frame = 0;
        while (true)
        {
            if (bellImage != null)
                bellImage.sprite = bellRingFrames[frame];
            frame = (frame + 1) % bellRingFrames.Length;
            yield return new WaitForSeconds(1f / bellFPS);
        }
    }

    // ──────────────────────────────────────────────────────────
    bool Overlaps(RectTransform a, RectTransform b)
    {
        return GetWorldRect(a).Overlaps(GetWorldRect(b));
    }

    Rect GetWorldRect(RectTransform rt)
    {
        Vector3[] c = new Vector3[4];
        rt.GetWorldCorners(c);
        return new Rect(c[0].x, c[0].y, c[2].x - c[0].x, c[2].y - c[0].y);
    }

    public void ResetBell()
    {
        hitTriggered = false;
        celebrationTriggered = false;
    }
}