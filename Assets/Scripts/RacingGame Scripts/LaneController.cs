using UnityEngine;

public class LaneController : MonoBehaviour
{
    [Header("Lane Transforms")]
    public RectTransform leftLane;
    public RectTransform middleLane;
    public RectTransform rightLane;

    [Header("Snap Settings")]
    public float snapSpeed = 10f;
    public bool useSmoothing = true;

    public int currentLane = 1; // Public so ObstacleCar can read it
    private float targetX;
    private RectTransform[] lanes;
    private RectTransform rectTransform;

    void Start()
    {
        rectTransform = GetComponent<RectTransform>();
        lanes = new RectTransform[] { leftLane, middleLane, rightLane };

        // Snap to middle lane at start
        targetX = middleLane.anchoredPosition.x;
        Vector2 pos = rectTransform.anchoredPosition;
        pos.x = targetX;
        rectTransform.anchoredPosition = pos;

        Debug.Log("Left X: " + leftLane.anchoredPosition.x);
        Debug.Log("Middle X: " + middleLane.anchoredPosition.x);
        Debug.Log("Right X: " + rightLane.anchoredPosition.x);
        Debug.Log("Player X: " + rectTransform.anchoredPosition.x);
    }

    void Update()
    {
        Vector2 currentPos = rectTransform.anchoredPosition;

        if (useSmoothing)
        {
            currentPos.x = Mathf.Lerp(currentPos.x, targetX, Time.deltaTime * snapSpeed);
        }
        else
        {
            currentPos.x = targetX;
        }

        rectTransform.anchoredPosition = currentPos;
    }

    public void MoveRight()
    {
        if (currentLane < lanes.Length - 1)
        {
            currentLane++;
            targetX = lanes[currentLane].anchoredPosition.x;
            Debug.Log("Moved Right - Lane: " + currentLane + " targetX: " + targetX);
        }
        else
        {
            Debug.Log("Already at Right lane");
        }
    }

    public void MoveLeft()
    {
        if (currentLane > 0)
        {
            currentLane--;
            targetX = lanes[currentLane].anchoredPosition.x;
            Debug.Log("Moved Left - Lane: " + currentLane + " targetX: " + targetX);
        }
        else
        {
            Debug.Log("Already at Left lane");
        }
    }
}