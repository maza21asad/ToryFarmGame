using System.Collections;
using UnityEngine;

public class CleaningTutorialManager : MonoBehaviour
{
    public static CleaningTutorialManager Instance;

    // ── Hand ─────────────────────────────────────────────────────────────────
    [Header("Hand")]
    [Tooltip("The Hand RectTransform on the Canvas")]
    public RectTransform handRect;
    [Tooltip("Child object that marks the exact tip of the hand graphic")]
    public RectTransform handTipObject;

    // ── Tool Source Objects ───────────────────────────────────────────────────
    [Header("Tool Source Objects")]
    public Transform tool1Object;   // connects to Location 1 or 2
    public Transform tool2Object;   // connects to Location 3
    public Transform tool3Object;   // connects to Location 4 or 5
    public Transform tool4Object;   // connects to Location 6

    // ── Destination Locations ─────────────────────────────────────────────────
    [Header("Destination Locations")]
    [Tooltip("Tool 1 can drag to this location (randomly chosen with Location 2)")]
    public Transform location1;
    [Tooltip("Tool 1 can drag to this location (randomly chosen with Location 1)")]
    public Transform location2;
    [Tooltip("Tool 2 drags to this location")]
    public Transform location3;
    [Tooltip("Tool 3 can drag to this location (randomly chosen with Location 5)")]
    public Transform location4;
    [Tooltip("Tool 3 can drag to this location (randomly chosen with Location 4)")]
    public Transform location5;
    [Tooltip("Tool 4 drags to this location")]
    public Transform location6;

    // ── Timing ────────────────────────────────────────────────────────────────
    [Header("Timing")]
    public float startDelay = 0.5f;
    public float bounceHoldTime = 0.8f;
    public float lerpDuration = 1.2f;
    public float holdAtDestination = 0.8f;
    public float repeatDelay = 1.5f;

    // ── Bounce ────────────────────────────────────────────────────────────────
    [Header("Bounce At Source")]
    public float bounceMagnitude = 12f;
    public float bounceSpeed = 4.5f;

    // ── Internal ──────────────────────────────────────────────────────────────
    private bool tutorialActive = false;   // stays false until OK is pressed
    private bool tutorialStarted = false;  // guard against double-start
    private Coroutine bounceCoroutine;
    private Vector2 handBaseAnchored;

    // Each entry: [toolObject, destinationA, destinationB]
    // destinationB is null when there is only one possible destination
    private Transform[][] _toolDestinationSets;

    // =========================================================================
    void Awake()
    {
        if (Instance == null) Instance = this;
        else { Destroy(gameObject); return; }
    }

    void Start()
    {
        // Build the tool→destination mapping
        // Tool 1: location1 or location2
        // Tool 2: location3 only
        // Tool 3: location4 or location5
        // Tool 4: location6 only
        _toolDestinationSets = new Transform[][]
        {
            new Transform[] { tool1Object, location1, location2 },
            new Transform[] { tool2Object, location3,       null },
            new Transform[] { tool3Object, location4, location5 },
            new Transform[] { tool4Object, location6,       null },
        };

        if (handRect != null) handRect.gameObject.SetActive(false);
        // Tutorial does NOT auto-start — call StartTutorial() from your OK button
    }

    // =========================================================================
    void Update()
    {
        if (!tutorialActive) return;

        // Stop on any touch drag or mouse drag
#if UNITY_EDITOR || UNITY_STANDALONE
        if (Input.GetMouseButtonDown(0))
        {
            HideTutorial();
            return;
        }
#endif
        if (Input.touchCount > 0)
        {
            TouchPhase phase = Input.GetTouch(0).phase;
            if (phase == TouchPhase.Began || phase == TouchPhase.Moved)
            {
                HideTutorial();
            }
        }
    }

    // =========================================================================
    // Call this from your OK button — starts the tutorial loop
    // =========================================================================
    public void StartTutorial()
    {
        if (tutorialStarted) return;   // prevent double-start
        tutorialStarted = true;
        tutorialActive = true;
        StartCoroutine(TutorialLoop());
    }

    // =========================================================================
    // Call when player interacts — hides hand and stops the loop
    // =========================================================================
    public void HideTutorial()
    {
        tutorialActive = false;
        StopAllCoroutines();
        if (handRect != null) handRect.gameObject.SetActive(false);
    }

    // =========================================================================
    IEnumerator TutorialLoop()
    {
        yield return new WaitForSeconds(startDelay);

        while (tutorialActive)
        {
            // Pick a random tool set
            int setIdx = Random.Range(0, _toolDestinationSets.Length);
            Transform[] set = _toolDestinationSets[setIdx];

            Transform toolSource = set[0];
            Transform destA = set[1];
            Transform destB = set[2]; // may be null

            // Pick destination: if destB exists, randomly choose between A and B
            Transform destination = (destB != null && Random.value < 0.5f) ? destB : destA;

            if (toolSource == null || destination == null)
            {
                yield return new WaitForSeconds(repeatDelay);
                continue;
            }

            // Snap tip to tool source
            PlaceTipAt(toolSource);
            handRect.gameObject.SetActive(true);

            // Bounce at source
            StartBounce(handBaseAnchored);
            yield return new WaitForSeconds(bounceHoldTime);
            StopBounce();

            // Lerp tip to destination
            yield return StartCoroutine(LerpTipTo(destination));

            // Hold at destination
            yield return new WaitForSeconds(holdAtDestination);

            // Hide and wait before repeating
            handRect.gameObject.SetActive(false);
            yield return new WaitForSeconds(repeatDelay);
        }
    }

    // ── Tip placement ─────────────────────────────────────────────────────────

    void PlaceTipAt(Transform target)
    {
        if (handTipObject != null)
        {
            handRect.position = target.position;
            Vector3 tipOffset = handTipObject.position - target.position;
            handRect.position -= tipOffset;
        }
        else
        {
            handRect.position = target.position;
        }
        handBaseAnchored = handRect.anchoredPosition;
    }

    IEnumerator LerpTipTo(Transform destination)
    {
        Vector3 startHandPos = handRect.position;

        Vector3 endHandPos;
        if (handTipObject != null)
        {
            handRect.position = destination.position;
            Vector3 tipOffset = handTipObject.position - destination.position;
            endHandPos = destination.position - tipOffset;
            handRect.position = startHandPos;
        }
        else
        {
            endHandPos = destination.position;
        }

        float elapsed = 0f;
        while (elapsed < lerpDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.SmoothStep(0f, 1f, elapsed / lerpDuration);
            handRect.position = Vector3.Lerp(startHandPos, endHandPos, t);
            yield return null;
        }

        handRect.position = endHandPos;
        handBaseAnchored = handRect.anchoredPosition;
    }

    // ── Bounce ────────────────────────────────────────────────────────────────

    void StartBounce(Vector2 basePos)
    {
        StopBounce();
        bounceCoroutine = StartCoroutine(BounceLoop(basePos));
    }

    void StopBounce()
    {
        if (bounceCoroutine != null)
        {
            StopCoroutine(bounceCoroutine);
            bounceCoroutine = null;
        }
    }

    IEnumerator BounceLoop(Vector2 basePos)
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