using UnityEngine;

namespace ToriSausages
{
    /// <summary>
    /// Attach to the pig/cook character's Image GameObject (alongside AnimatorFrame).
    /// Plays idle loop from game start, happy animation when a customer is served.
    /// Stove plays its own loop independently.
    /// </summary>
    public class CharacterCook : MonoBehaviour
    {
        public static CharacterCook Instance { get; private set; }

        [Header("Cook Animations")]
        public FrameAnimation idleAnimation;
        public FrameAnimation happyAnimation;

        [Header("Stove")]
        public AnimatorFrame stoveAnimator;
        public FrameAnimation stoveAnimation;

        private AnimatorFrame _animator;
        private bool _playingHappy;

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            _animator = GetComponent<AnimatorFrame>();
            if (_animator == null) _animator = gameObject.AddComponent<AnimatorFrame>();
        }

        private void Start()
        {
            PlayIdle();
            if (stoveAnimator != null && stoveAnimation != null)
                stoveAnimator.Play(stoveAnimation, loop: true);
        }

        public void PlayIdle()
        {
            _playingHappy = false;
            if (idleAnimation != null)
                _animator.Play(idleAnimation, loop: true);
        }

        public void PlayHappy()
        {
            if (_playingHappy) return;
            _playingHappy = true;
            if (happyAnimation?.frames?.Length > 0)
                _animator.Play(happyAnimation, loop: false, onComplete: PlayIdle);
            else
                PlayIdle();
        }
    }
}
