using System.Collections;
using UnityEngine;

public class TruckRepairTutorialManager : MonoBehaviour
{
    public static TruckRepairTutorialManager Instance;

    // ── Hand ─────────────────────────────────────────────────────────────────
    [Header("Hand")]
    [Tooltip("The Hand RectTransform on the Canvas")]
    public RectTransform handRect;
    [Tooltip("Child object that marks the exact tip of the hand graphic")]
    public RectTransform handTipObject;

    // ── Part Source Objects (the draggable pieces) ────────────────────────────
    [Header("Part Source Objects")]
    public Transform part1;
    public Transform part2;
    public Transform part3;
    public Transform part4;
    public Transform part5;
    public Transform part6;

    // ── Destination Locations (the repair slots on the truck) ─────────────────
    [Header("Destination Locations")]
    [Tooltip("part1 drags to location1")]
    public Transform location1;
    [Tooltip("part2 drags to location2")]
    public Transform location2;
    [Tooltip("part3 drags to location3")]
    public Transform location3;
    [Tooltip("part4 drags to location4")]
    public Transform location4;
    [Tooltip("part5 drags to location5")]
    public Transform location5;
    [Tooltip("part6 drags to location6")]
    public Transform location6;

    // ── Timing ────────────────────────────────────────────────────────────────
    [Header("Timing")]
    public float startDelay        = 0.5f;
    public float bounceHoldTime    = 0.8f;
    public float lerpDuration      = 1.2f;
    public float holdAtDestination = 0.8f;
    public float repeatDelay       = 1.5f;

    // ── Bounce ────────────────────────────────────────────────────────────────
    [Header("Bounce At Source")]
    public float bounceMagnitude = 12f;
    public float bounceSpeed     = 4.5f;

    // ── Internal ──────────────────────────────────────────────────────────────
    private bool      tutorialActive = true;
    private Coroutine bounceCoroutine;
    private Vector2   handBaseAnchored;

    private Transform[] _parts;
    private Transform[] _locations;

    // =========================================================================
    void Awake()
    {
        if (Instance == null) Instance = this;
        else { Destroy(gameObject); return; }
    }

    void Start()
    {
        _parts     = new Transform[] { part1, part2, part3, part4, part5, part6 };
        _locations = new Transform[] { location1, location2, location3, location4, location5, location6 };

        if (handRect != null) handRect.gameObject.SetActive(false);

        StartCoroutine(TutorialLoop());
    }

    // =========================================================================
    // Call when player starts dragging any part — hides hand and stops loop
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
            // Pick a random valid part that hasn't been repaired yet
            int idx = GetRandomAvailableIndex();
            if (idx < 0)
            {
                // All parts repaired — stop tutorial
                HideTutorial();
                yield break;
            }

            Transform partSource = _parts[idx];
            Transform destination = _locations[idx];

            if (partSource == null || destination == null)
            {
                yield return new WaitForSeconds(repeatDelay);
                continue;
            }

            // Snap tip to the part source
            PlaceTipAt(partSource);
            handRect.gameObject.SetActive(true);

            // Bounce at source to draw attention
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

    // Picks a random index whose part object is still active in the scene
    private int GetRandomAvailableIndex()
    {
        // Collect indices of parts that are still active (not yet placed)
        System.Collections.Generic.List<int> available = new System.Collections.Generic.List<int>();
        for (int i = 0; i < _parts.Length; i++)
        {
            if (_parts[i] != null && _parts[i].gameObject.activeInHierarchy)
                available.Add(i);
        }
        if (available.Count == 0) return -1;
        return available[Random.Range(0, available.Count)];
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
        handBaseAnchored  = handRect.anchoredPosition;
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
