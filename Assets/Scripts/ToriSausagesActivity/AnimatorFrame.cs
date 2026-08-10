using UnityEngine;
using UnityEngine.UI;
using System;
using System.Collections;

namespace ToriSausages
{
    [System.Serializable]
    public class FrameAnimation
    {
        [Tooltip("Drag all sprite frames here in order.")]
        public Sprite[] frames;

        [Min(1f)]
        [Tooltip("Playback speed in frames per second.")]
        public float fps = 12f;
    }

    /// <summary>
    /// Reusable frame-by-frame sprite animator for any UI Image.
    /// Add alongside any Image that needs frame animation.
    /// Call Play() to loop or PlayOnce() to run once.
    /// </summary>
    [RequireComponent(typeof(Image))]
    public class AnimatorFrame : MonoBehaviour
    {
        private Image _image;
        private Coroutine _routine;

        private void Awake() => _image = GetComponent<Image>();

        public void Play(FrameAnimation anim, bool loop = true, Action onComplete = null)
        {
            if (anim == null || anim.frames == null || anim.frames.Length == 0) return;
            Play(anim.frames, anim.fps, loop, onComplete);
        }

        public void Play(Sprite[] frames, float fps, bool loop = true, Action onComplete = null)
        {
            Stop();
            if (frames == null || frames.Length == 0) return;
            _routine = StartCoroutine(AnimRoutine(frames, Mathf.Max(fps, 0.1f), loop, onComplete));
        }

        public void Stop()
        {
            if (_routine == null) return;
            StopCoroutine(_routine);
            _routine = null;
        }

        public bool IsPlaying => _routine != null;

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
                    if (loop) i = 0;
                    else { _routine = null; onComplete?.Invoke(); yield break; }
                }
            }
        }
    }
}
