//using UnityEngine;

///// <summary>
///// Attach to the pig/cook character's Image GameObject (alongside FrameAnimator).
/////
///// ANIMATIONS (all frame-based, no Animator needed)
/////   idleAnimation  — looped continuously from game start.
/////   happyAnimation — played once when a customer is served, then returns to idle.
/////   stoveAnimation — plays on a separate stoveAnimator object (the stove Image).
/////
///// SETUP
/////   1. Add FrameAnimator to the cook's Image GameObject alongside this script.
/////   2. Assign idleAnimation and happyAnimation frames + FPS in the Inspector.
/////   3. For the stove: add FrameAnimator to the Stove Image object and drag it
/////      into the stoveAnimator field, then assign stoveAnimation frames + FPS.
///// </summary>
//public class CookCharacter : MonoBehaviour
//{
//    // ── Singleton ────────────────────────────────────────────────────────────
//    public static CookCharacter Instance { get; private set; }

//    // ── Inspector ─────────────────────────────────────────────────────────────

//    [Header("Cook (Pig) Animations")]
//    [Tooltip("Idle animation — played in a loop continuously.")]
//    public FrameAnimation idleAnimation;

//    [Tooltip("Happy animation — played once when a customer is served, then returns to idle.")]
//    public FrameAnimation happyAnimation;

//    [Header("Stove")]
//    [Tooltip("FrameAnimator on the Stove Image object.\n" +
//             "The stove plays its animation independently from the cook.")]
//    public FrameAnimator stoveAnimator;

//    [Tooltip("Stove animation — looped on the stove object from game start.")]
//    public FrameAnimation stoveAnimation;

//    // ── Private ───────────────────────────────────────────────────────────────

//    private FrameAnimator _animator;
//    private bool _playingHappy;

//    // ── Unity ─────────────────────────────────────────────────────────────────

//    private void Awake()
//    {
//        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
//        Instance = this;

//        _animator = GetComponent<FrameAnimator>();
//        if (_animator == null)
//            _animator = gameObject.AddComponent<FrameAnimator>();
//    }

//    private void Start()
//    {
//        PlayIdle();

//        // Start the stove animation independently.
//        if (stoveAnimator != null && stoveAnimation != null)
//            stoveAnimator.Play(stoveAnimation, loop: true);
//    }

//    // ── Public API ────────────────────────────────────────────────────────────

//    /// <summary>Resumes the idle loop. Called internally after happy finishes.</summary>
//    public void PlayIdle()
//    {
//        _playingHappy = false;
//        if (idleAnimation != null)
//            _animator.Play(idleAnimation, loop: true);
//    }

//    /// <summary>
//    /// Plays the happy animation once, then returns to idle.
//    /// Safe to call while happy is already playing (ignored).
//    /// </summary>
//    public void PlayHappy()
//    {
//        if (_playingHappy) return;
//        _playingHappy = true;

//        if (happyAnimation != null && happyAnimation.frames != null && happyAnimation.frames.Length > 0)
//            _animator.Play(happyAnimation, loop: false, onComplete: PlayIdle);
//        else
//            PlayIdle();
//    }
//}

using UnityEngine;

/// <summary>
/// Attach to the pig/cook character's Image GameObject (alongside FrameAnimator).
///
/// ANIMATIONS (all frame-based, no Animator needed)
///   idleAnimation  — looped continuously from game start.
///   happyAnimation — played once when a customer is served, then returns to idle.
///   stoveAnimation — plays on a separate stoveAnimator object (the stove Image).
///
/// SETUP
///   1. Add FrameAnimator to the cook's Image GameObject alongside this script.
///   2. Assign idleAnimation and happyAnimation frames + FPS in the Inspector.
///   3. For the stove: add FrameAnimator to the Stove Image object and drag it
///      into the stoveAnimator field, then assign stoveAnimation frames + FPS.
/// </summary>
public class CookCharacter : MonoBehaviour
{
    // ── Singleton ────────────────────────────────────────────────────────────
    public static CookCharacter Instance { get; private set; }

    // ── Inspector ─────────────────────────────────────────────────────────────

    [Header("Cook (Pig) Animations")]
    [Tooltip("Idle animation — played in a loop continuously.")]
    public FrameAnimation idleAnimation;

    [Tooltip("Happy animation — played once when a customer is served, then returns to idle.")]
    public FrameAnimation happyAnimation;

    [Header("Stove")]
    [Tooltip("FrameAnimator on the Stove Image object.\n" +
             "The stove plays its animation independently from the cook.")]
    public FrameAnimator stoveAnimator;

    [Tooltip("Stove animation — looped on the stove object from game start.")]
    public FrameAnimation stoveAnimation;

    // ── Private ───────────────────────────────────────────────────────────────

    private FrameAnimator _animator;
    private bool _playingHappy;

    // ── Unity ─────────────────────────────────────────────────────────────────

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;

        _animator = GetComponent<FrameAnimator>();
        if (_animator == null)
            _animator = gameObject.AddComponent<FrameAnimator>();
    }

    private void Start()
    {
        PlayIdle();

        // Start the stove animation independently.
        if (stoveAnimator != null && stoveAnimation != null)
            stoveAnimator.Play(stoveAnimation, loop: true);
    }

    // ── Public API ────────────────────────────────────────────────────────────

    /// <summary>Resumes the idle loop. Called internally after happy finishes.</summary>
    public void PlayIdle()
    {
        _playingHappy = false;
        if (idleAnimation != null)
            _animator.Play(idleAnimation, loop: true);
    }

    /// <summary>
    /// Plays the happy animation once, then returns to idle.
    /// Safe to call while happy is already playing (ignored).
    /// </summary>
    public void PlayHappy()
    {
        if (_playingHappy) return;
        _playingHappy = true;
        //SoundManager.Instance.PlaySFX("PigLaugh");

        if (happyAnimation != null && happyAnimation.frames != null && happyAnimation.frames.Length > 0)
            _animator.Play(happyAnimation, loop: false, onComplete: PlayIdle);
        else
            PlayIdle();
    }
}