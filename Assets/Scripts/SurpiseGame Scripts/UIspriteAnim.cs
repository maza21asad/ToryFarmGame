using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class UIIdleAnimation : MonoBehaviour
{
    [Header("Target")]
    public Image targetImage;

    [Header("Animation Frames")]
    public Sprite[] idleFrames;
    public Sprite[] eatFrames;
    public Sprite[] happyFrames;
    public Sprite[] spicyFrames;

    [Header("Frame Rate")]
    public float frameRate = 10f;

    public enum AnimState { Idle, Eat, Happy, Spicy }

    private AnimState currentState = AnimState.Idle;
    private int currentFrame = 0;
    private float timer = 0f;
    private Coroutine playOnceCoroutine;

    void OnEnable()
    {
        PlayIdle();
    }

    void Update()
    {
        if (currentState != AnimState.Idle) return;

        if (idleFrames == null || idleFrames.Length == 0) return;

        timer += Time.deltaTime;
        if (timer >= 1f / frameRate)
        {
            timer = 0f;
            targetImage.sprite = idleFrames[currentFrame];
            currentFrame = (currentFrame + 1) % idleFrames.Length;
        }
    }

    public void PlayIdle()
    {
        StopPlayOnce();
        currentState = AnimState.Idle;
        currentFrame = 0;
        timer = 0f;
    }

    // Called by RandomFoodButton when food reaches mouth
    // isSpicy: true = play Eat then Spicy, false = play Eat then Happy
    public void PlayEatThenReaction(bool isSpicy, float eatDuration, float reactionDuration)
    {
        StopPlayOnce();
        playOnceCoroutine = StartCoroutine(EatSequence(isSpicy, eatDuration, reactionDuration));
    }

    IEnumerator EatSequence(bool isSpicy, float eatDuration, float reactionDuration)
    {
        // Eat
        currentState = AnimState.Eat;
        SoundManager.Instance.PlaySFX("Happy");
        SoundManager.Instance.PlaySFX("Eating");
        yield return PlayFramesFor(eatFrames, eatDuration);

        // Happy or Spicy
        currentState = isSpicy ? AnimState.Spicy : AnimState.Happy;
        Sprite[] reactionFrames = isSpicy ? spicyFrames : happyFrames;
        if (isSpicy)
            SoundManager.Instance.PlaySFX("Spicy");
        yield return PlayFramesFor(reactionFrames, reactionDuration);

        // Back to idle
        PlayIdle();
    }

    IEnumerator PlayFramesFor(Sprite[] frames, float duration)
    {
        if (frames == null || frames.Length == 0)
        {
            yield return new WaitForSeconds(duration);
            yield break;
        }

        float elapsed = 0f;
        int frame = 0;
        float interval = 1f / frameRate;
        float frameTimer = 0f;

        targetImage.sprite = frames[0];

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            frameTimer += Time.deltaTime;

            if (frameTimer >= interval)
            {
                frameTimer = 0f;
                frame = (frame + 1) % frames.Length;
                targetImage.sprite = frames[frame];
            }

            yield return null;
        }
    }

    void StopPlayOnce()
    {
        if (playOnceCoroutine != null)
        {
            StopCoroutine(playOnceCoroutine);
            playOnceCoroutine = null;
        }
    }
}