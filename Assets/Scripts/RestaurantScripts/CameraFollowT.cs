using UnityEngine;

public class CameraFollowT : MonoBehaviour
{
    [Header("Target")]
    public RectTransform toryRect;

    [Header("Settings")]
    public float smoothSpeed = 8f;

    private Canvas canvas;

    void Start()
    {
        canvas = FindObjectOfType<Canvas>();
    }

    void LateUpdate()
    {
        if (toryRect == null || canvas == null) return;

        float scaleFactor = canvas.scaleFactor;

        Vector3 targetPos = new Vector3(
            -(toryRect.anchoredPosition.x * scaleFactor / 100f),  // ← negated
            -(toryRect.anchoredPosition.y * scaleFactor / 100f),  // ← negated
            transform.position.z
        );

        transform.position = Vector3.Lerp(
            transform.position,
            targetPos,
            smoothSpeed * Time.deltaTime
        );
    }
}