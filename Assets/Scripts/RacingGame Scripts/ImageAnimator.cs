using UnityEngine;
using UnityEngine.UI;

public class ImageAnimator : MonoBehaviour
{
    [Header("Frames")]
    public Sprite[] frames;             // Drag all sprites in order here

    [Header("Animation Settings")]
    public float fps = 12f;             // Frames per second
    public bool loop = true;            // Loop animation

    private Image image;
    private int currentFrame = 0;
    private float timer = 0f;

    void Start()
    {
        image = GetComponent<Image>();

        if (frames.Length > 0)
            image.sprite = frames[0];
    }

    void Update()
    {
        if (frames.Length == 0) return;

        timer += Time.deltaTime;

        if (timer >= 1f / fps)
        {
            timer = 0f;
            currentFrame++;

            if (currentFrame >= frames.Length)
            {
                if (loop)
                    currentFrame = 0;   // Loop back to first frame
                else
                {
                    currentFrame = frames.Length - 1; // Stay on last frame
                    return;
                }
            }

            image.sprite = frames[currentFrame];
        }
    }

    // Call these from other scripts if needed
    public void Play()  => enabled = true;
    public void Stop()  => enabled = false;
    public void Reset()
    {
        currentFrame  = 0;
        timer         = 0f;
        image.sprite  = frames[0];
    }
}