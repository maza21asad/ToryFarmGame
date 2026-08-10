using System.Collections;
using UnityEngine;
using TMPro;
using DG.Tweening;

/// <summary>
/// Countdown: 1 zooms in → zooms out → 2 zooms in → zooms out → 3 zooms in → zooms out → GO! zooms in → stays
///
/// Setup:
///   CountdownPanel (full screen, any background)
///     └── CountdownText  (TMP_Text, centred, large font)
///
/// Attach this script to CountdownPanel.
/// Drag CountdownText into the Inspector.
/// </summary>
public class CountdownUI : MonoBehaviour
{
    [Header("References")]
    public GameObject countdownPanel;
    public TMP_Text countdownText;

    [Header("Timing")]
    public float zoomInDuration = 0.35f;   // how fast it zooms IN
    public float holdDuration = 0.15f;   // pause at full size
    public float zoomOutDuration = 0.25f;   // how fast it zooms OUT
    public float gapDuration = 0.1f;    // gap between numbers

    [Header("Scale")]
    public float startScale = 0.1f;   // tiny — starts here
    public float peakScale = 1.1f;   // slightly overshoot
    public float normalScale = 1.0f;   // resting size
    public float goScale = 1.2f;   // GO! stays at this size

    [Header("Colors")]
    public Color color1 = Color.red;
    public Color color2 = new Color(1f, 0.5f, 0f);   // orange
    public Color color3 = Color.yellow;
    public Color colorGo = new Color(0.2f, 1f, 0.2f); // green

    [Header("Audio (optional)")]
    public AudioSource audioSource;
    public AudioClip tickClip;
    public AudioClip goClip;

    // ──────────────────────────────────────────────────────────
    void Awake()
    {
        if (countdownPanel != null) countdownPanel.SetActive(false);
    }

    // ──────────────────────────────────────────────────────────
    // ✅ Called by GameManager — runs countdown then calls onFinished
    // ──────────────────────────────────────────────────────────
    public void PlayCountdown(System.Action onFinished)
    {
        StartCoroutine(RunCountdown(onFinished));
    }

    IEnumerator RunCountdown(System.Action onFinished)
    {
        countdownPanel.SetActive(true);
        countdownText.transform.localScale = Vector3.zero;

        // ── 1 ── zoom in → zoom out
        yield return ZoomInOut("1", color1, tickClip, zoomOut: true);
        yield return new WaitForSeconds(gapDuration);

        // ── 2 ── zoom in → zoom out
        yield return ZoomInOut("2", color2, tickClip, zoomOut: true);
        yield return new WaitForSeconds(gapDuration);

        // ── 3 ── zoom in → zoom out
        yield return ZoomInOut("3", color3, tickClip, zoomOut: true);
        yield return new WaitForSeconds(gapDuration);

        // ── GO! ── zoom in → STAYS
        yield return ZoomInOut("GO!", colorGo, goClip, zoomOut: false);

        yield return new WaitForSeconds(0.6f);

        // ── Hide panel and start game ──
        countdownText.transform
            .DOScale(0f, 0.2f)
            .SetEase(Ease.InBack)
            .OnComplete(() => countdownPanel.SetActive(false));

        yield return new WaitForSeconds(0.25f);

        onFinished?.Invoke();
    }

    // ──────────────────────────────────────────────────────────
    IEnumerator ZoomInOut(string label, Color col, AudioClip clip, bool zoomOut)
    {
        // Set text
        countdownText.text = label;
        countdownText.color = col;

        PlaySound(clip);

        // Start tiny
        countdownText.transform.localScale = Vector3.one * startScale;

        // ── Zoom IN ────────────────────────────────────────────
        bool done = false;
        countdownText.transform
            .DOScale(peakScale, zoomInDuration)
            .SetEase(Ease.OutBack)
            .OnComplete(() => done = true);

        yield return new WaitUntil(() => done);
        yield return new WaitForSeconds(holdDuration);

        if (!zoomOut) yield break;   // GO! stays — skip zoom out

        // ── Zoom OUT ───────────────────────────────────────────
        done = false;
        countdownText.transform
            .DOScale(0f, zoomOutDuration)
            .SetEase(Ease.InBack)
            .OnComplete(() => done = true);

        yield return new WaitUntil(() => done);
    }

    // ──────────────────────────────────────────────────────────
    void PlaySound(AudioClip clip)
    {
        if (audioSource != null && clip != null)
            audioSource.PlayOneShot(clip);
    }
}