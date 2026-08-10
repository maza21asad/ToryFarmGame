using UnityEngine;
using UnityEngine.UI;

public class DogIdleAnimation : MonoBehaviour
{
    [Header("Drag all idle frames here in order")]
    public Sprite[] idleFrames;

    [Header("Speed")]
    public float fps = 12f;

    private Image dogImage;
    private int currentFrame = 0;
    private float timer = 0f;

    void Start()
    {
        dogImage = GetComponent<Image>();

        // ✅ Set first frame
        if (idleFrames.Length > 0)
            dogImage.sprite = idleFrames[0];
    }

    void Update()
    {
        if (idleFrames.Length == 0) return;

        timer += Time.deltaTime;

        if (timer >= 1f / fps)
        {
            timer = 0f;
            currentFrame = (currentFrame + 1) % idleFrames.Length;
            dogImage.sprite = idleFrames[currentFrame];
        }
    }
}