using UnityEngine;

// Attach this to each Food RectTransform to save and restore its original position
public class FoodOriginalPosition : MonoBehaviour
{
    private Vector3 savedPosition;
    private RectTransform rectTransform;
    private bool initialized = false;

    void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
    }

    void Start()
    {
        // Save position in Start so Unity layout is fully ready
        SavePosition();
    }

    // Call this once after the scene is fully set up if Start hasn't run yet
    public void SavePosition()
    {
        if (rectTransform == null)
            rectTransform = GetComponent<RectTransform>();

        savedPosition = rectTransform.position;
        initialized = true;
    }

    public void ResetPosition()
    {
        if (!initialized) SavePosition();
        if (rectTransform == null)
            rectTransform = GetComponent<RectTransform>();

        rectTransform.position = savedPosition;
    }
}
