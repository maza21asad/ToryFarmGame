using UnityEngine;

public class PulseAnimation : MonoBehaviour
{
    [Header("Pulse Settings")]
    public float minScale = 0.9f;
    public float maxScale = 1.1f;
    public float speed = 2f;

    private Vector3 originalScale;

    void Start()
    {
        originalScale = transform.localScale;
    }

    void Update()
    {
        float scale = Mathf.Lerp(minScale, maxScale, (Mathf.Sin(Time.time * speed) + 1f) / 2f);
        transform.localScale = originalScale * scale;
    }
}