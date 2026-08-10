using UnityEngine;
using System;

public class TireLineMover : MonoBehaviour
{
    [Header("Points")]
    public RectTransform startSpawner;
    public RectTransform endSpawner;

    [Header("Scale XYZ")]
    public Vector3 startScaleXYZ = new Vector3(0.3f, 0.3f, 0.3f);
    public Vector3 endScaleXYZ   = new Vector3(1.0f, 1.0f, 1.0f);

    [Header("Speed")]
    public float speed = 0.3f;

    [Header("Spawn Rate")]
    [Tooltip("Delay before this tire loops back. Lower = faster rapid fire.")]
    public float spawnInterval = 0f;

    [Header("Loop Offset")]
    [Tooltip("Stagger multiple tires. E.g. 0, 0.33, 0.66 for 3 tires evenly spaced.")]
    [Range(0f, 1f)]
    public float startOffset = 0f;

    [HideInInspector] public Action onTirePassedPlayer;

    private RectTransform rectTransform;
    private float progress = 0f;
    private bool hasPassedPlayer = false;

    void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
    }

    void OnEnable()
    {
        // Apply offset so multiple tires are evenly spread apart
        progress        = startOffset;
        hasPassedPlayer = false;

        rectTransform.position   = startSpawner.position;
        rectTransform.localScale = startScaleXYZ;
    }

    public void StopSpawning()
    {
        enabled = false;

        rectTransform.position   = startSpawner.position;
        rectTransform.localScale = startScaleXYZ;
    }

    void Update()
    {
        if (startSpawner == null || endSpawner == null) return;

        progress += Time.deltaTime * speed;
        progress  = Mathf.Clamp01(progress);

        rectTransform.position   = Vector3.Lerp(startSpawner.position, endSpawner.position, progress);
        rectTransform.localScale = Vector3.Lerp(startScaleXYZ, endScaleXYZ, progress);

        if (!hasPassedPlayer && progress >= 0.5f)
        {
            hasPassedPlayer = true;
            onTirePassedPlayer?.Invoke();
        }

        if (progress >= 1f)
        {
            // Use spawnInterval to create a gap before re-entering
            // Negative progress = waiting off-screen before starting again
            progress        = -(spawnInterval * speed);
            hasPassedPlayer = false;

            rectTransform.position   = startSpawner.position;
            rectTransform.localScale = startScaleXYZ;
        }
    }
}