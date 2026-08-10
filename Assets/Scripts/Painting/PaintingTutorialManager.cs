using System.Collections;
using UnityEngine;

public class PaintingTutorialManager : MonoBehaviour
{
    public static PaintingTutorialManager Instance;

    // ── Hand ─────────────────────────────────────────────────────────────────
    [Header("Hand")]
    [Tooltip("The Hand RectTransform on the Canvas")]
    public RectTransform handRect;
    [Tooltip("Child object that marks the exact tip of the hand graphic")]
    public RectTransform handTipObject;

    // ── Color Source Objects ──────────────────────────────────────────────────
    [Header("Color Source Objects")]
    public Transform blackColorObject;
    public Transform ashColorObject;
    public Transform greenColorObject;
    public Transform lightGreenColorObject;

    // ── Destination Locations ─────────────────────────────────────────────────
    [Header("Destination Locations")]
    [Tooltip("Black color drags to Luna")]
    public Transform lunaLocation;
    [Tooltip("Ash color drags to House")]
    public Transform houseLocation;
    [Tooltip("Green color drags to Tree")]
    public Transform treeLocation;
    [Tooltip("Light Green color drags to Grass")]
    public Transform grassLocation;

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

    private Transform[] _colorObjects;
    private Transform[] _destinations;

    // =========================================================================
    void Awake()
    {
        if (Instance == null) Instance = this;
        else { Destroy(gameObject); return; }
    }

    void Start()
    {
        _colorObjects = new Transform[] { blackColorObject, ashColorObject, greenColorObject, lightGreenColorObject };
        _destinations = new Transform[] { lunaLocation,     houseLocation,  treeLocation,    grassLocation         };

        if (handRect != null) handRect.gameObject.SetActive(false);

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
            int idx = Random.Range(0, _colorObjects.Length);
            Transform colorSource = _colorObjects[idx];
            Transform destination = _destinations[idx];

            if (colorSource == null || destination == null)
            {
                yield return new WaitForSeconds(repeatDelay);
                continue;
            }

            // Snap tip to color source
            PlaceTipAt(colorSource);
            handRect.gameObject.SetActive(true);

            // Bounce at source
            StartBounce(handBaseAnchored);
            yield return new WaitForSeconds(bounceHoldTime);
            StopBounce();

            // Lerp tip to destination
            yield return StartCoroutine(LerpTipTo(destination));

            // Hold
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
