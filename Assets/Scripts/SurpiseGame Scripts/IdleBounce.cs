using UnityEngine;

public class IdleBounce : MonoBehaviour
{
    [Header("Bounce Settings")]
    public float bounceHeight = 8f;   // how many pixels up/down
    public float bounceSpeed = 2f;    // how fast it bounces
    public float squishAmount = 0.03f; // subtle squish on scale

    private Vector3 originalPosition;
    private Vector3 originalScale;

    void Start()
    {
        originalPosition = transform.localPosition;
        originalScale = transform.localScale;
    }

    void Update()
    {
        float sine = Mathf.Sin(Time.time * bounceSpeed);

        // Move up and down
        transform.localPosition = originalPosition + new Vector3(0f, sine * bounceHeight, 0f);

        // Subtle squish — slightly wider when down, taller when up
        float scaleX = 1f - sine * squishAmount;
        float scaleY = 1f + sine * squishAmount;
        transform.localScale = new Vector3(
            originalScale.x * scaleX,
            originalScale.y * scaleY,
            originalScale.z
        );
    }
}