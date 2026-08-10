using UnityEngine;

public class TreeMover : MonoBehaviour
{
    [Header("Points")]
    public RectTransform startPoint;   // Right side (or left side) spawn point
    public RectTransform endPoint;     // Opposite side exit point

    [Header("Speed")]
    public float speed = 0.05f;        // Slow sideways drift

    [Header("Spawn Rate")]
    [Tooltip("Delay before this tree loops back. Higher = longer gap.")]
    public float loopInterval = 0.5f;

    private RectTransform rectTransform;
    private float progress = 0f;
    private bool hasInitialized = false;

    void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
    }

    void OnEnable()
    {
        if (hasInitialized) return;
        hasInitialized = true;

        progress = 0f;

        if (startPoint != null)
            rectTransform.position = startPoint.position;
    }

    void Update()
    {
        if (startPoint == null || endPoint == null) return;

        progress += Time.deltaTime * speed;
        progress = Mathf.Clamp01(progress);

        rectTransform.position = Vector3.Lerp(startPoint.position, endPoint.position, progress);

        if (progress >= 1f)
        {
            // Loop back with a gap delay using negative progress
            progress = -(loopInterval * speed);

            rectTransform.position = startPoint.position;
        }
    }

    public void PauseTree()
    {
        enabled = false;
    }
}