using UnityEngine;
using UnityEngine.UI;

public class Bowl : MonoBehaviour
{
    [Header("Frames")]
    public Sprite[] frames;             // Drag all sprites in order here

    [Header("Animation Settings")]
    public float fps = 12f;             // Frames per second
    public bool loop = true;            // Loop animation

    private Image image;
    private int currentFrame = 0;
    private float timer = 0f;
    private bool isPlaying = false;   // Starts stopped, TutorialManager controls it
    private int targetEndFrame = -1;  // -1 = normal loop/stop behaviour

    void Start()
    {
        image = GetComponent<Image>();

        if (frames.Length > 0)
            image.sprite = frames[0];
    }

    void Update()
    {
        if (!isPlaying || frames.Length == 0) return;

        timer += Time.unscaledDeltaTime;   // Immune to Time.timeScale = 0

        if (timer >= 1f / fps)
        {
            timer = 0f;
            currentFrame++;

            // If we have a specific end target, stop there
            if (targetEndFrame >= 0 && currentFrame >= targetEndFrame)
            {
                currentFrame = targetEndFrame - 1;
                isPlaying = false;
                image.sprite = frames[currentFrame];
                return;
            }

            if (currentFrame >= frames.Length)
            {
                if (loop)
                    currentFrame = 0;   // Loop back to first frame
                else
                {
                    currentFrame = frames.Length - 1; // Stay on last frame
                    isPlaying = false;
                    return;
                }
            }

            image.sprite = frames[currentFrame];
        }
    }

    // Call these from other scripts if needed
    public void Play() { targetEndFrame = -1; isPlaying = true; }
    public void Stop() => isPlaying = false;
    public void Reset()
    {
        currentFrame = 0;
        timer = 0f;
        targetEndFrame = -1;
        if (image != null && frames.Length > 0)
            image.sprite = frames[0];
    }

    // Play exactly frameCount frames starting at startFrame, then stop on last
    public void PlayFrames(int startFrame, int frameCount)
    {
        currentFrame = startFrame;
        targetEndFrame = startFrame + frameCount;
        timer = 0f;
        isPlaying = true;
        if (image != null && frames.Length > 0)
            image.sprite = frames[Mathf.Clamp(currentFrame, 0, frames.Length - 1)];
    }

    // Play from startFrame to the end of the animation, then stop
    public void PlayFromFrame(int startFrame)
    {
        currentFrame = startFrame;
        targetEndFrame = frames.Length;
        timer = 0f;
        isPlaying = true;
        if (image != null && frames.Length > 0)
            image.sprite = frames[Mathf.Clamp(currentFrame, 0, frames.Length - 1)];
    }

    // Returns how long (seconds) it takes to play frameCount frames
    public float GetDuration(int frameCount) => frameCount / fps;
}