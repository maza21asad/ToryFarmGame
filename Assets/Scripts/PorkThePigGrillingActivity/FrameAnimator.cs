using UnityEngine;
using UnityEngine.UI;
using System;
using System.Collections;

/// <summary>
/// Reusable frame-by-frame sprite animator for any UI Image.
/// Uses the FrameAnimation type already defined in GrillCooker.cs.
///
/// SETUP
///   Add alongside any Image component that needs frame animation.
///   Call Play() or PlayOnce() at runtime — no Animator required.
/// </summary>
[RequireComponent(typeof(Image))]
public class FrameAnimator : MonoBehaviour
{
    private Image _image;
    private Coroutine _routine;

    private void Awake()
    {
        _image = GetComponent<Image>();
    }

    // ── Public API ───────────────────────────────────────────────────────────

    /// <summary>
    /// Play a FrameAnimation. Loops forever by default.
    /// onComplete is called once when a non-looping animation finishes.
    /// </summary>
    public void Play(FrameAnimation anim, bool loop = true, Action onComplete = null)
    {
        if (anim == null || anim.frames == null || anim.frames.Length == 0) return;
        Play(anim.frames, anim.fps, loop, onComplete);
    }

    /// <summary>
    /// Play raw frames at a given fps. Loops forever by default.
    /// </summary>
    public void Play(Sprite[] frames, float fps, bool loop = true, Action onComplete = null)
    {
        Stop();
        if (frames == null || frames.Length == 0) return;
        _routine = StartCoroutine(AnimRoutine(frames, Mathf.Max(fps, 0.1f), loop, onComplete));
    }

    /// <summary>Stops the current animation and holds the last frame shown.</summary>
    public void Stop()
    {
        if (_routine == null) return;
        StopCoroutine(_routine);
        _routine = null;
    }

    public bool IsPlaying => _routine != null;

    // ── Internal ─────────────────────────────────────────────────────────────

    private IEnumerator AnimRoutine(Sprite[] frames, float fps, bool loop, Action onComplete)
    {
        float delay = 1f / fps;
        int i = 0;

        while (true)
        {
            if (_image != null) _image.sprite = frames[i];
            yield return new WaitForSeconds(delay);

            i++;
            if (i >= frames.Length)
            {
                if (loop)
                {
                    i = 0;
                }
                else
                {
                    _routine = null;
                    onComplete?.Invoke();
                    yield break;
                }
            }
        }
    }
}