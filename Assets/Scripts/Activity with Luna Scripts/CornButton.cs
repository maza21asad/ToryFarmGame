using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class CornButton : MonoBehaviour, IPointerClickHandler
{
    [Header("Assign Target C Location")]
    public RectTransform targetCLocation;

    [Header("Lerp Settings")]
    public float lerpSpeed = 8f;

    private RectTransform rectTransform;
    private Vector3 targetPos;
    private bool isMoving = false;
    private bool hasBeenPlaced = false;

    void Start()
    {
        rectTransform = GetComponent<RectTransform>();
        targetPos = rectTransform.position;

        Button btn = GetComponent<Button>();
        if (btn != null)
            btn.onClick.AddListener(OnPressed);
    }

    void Update()
    {
        if (!isMoving) return;

        rectTransform.position = Vector3.Lerp(
            rectTransform.position,
            targetPos,
            Time.deltaTime * lerpSpeed
        );

        if (Vector3.Distance(rectTransform.position, targetPos) < 0.5f)
        {
            rectTransform.position = targetPos;
            isMoving = false;

            if (!hasBeenPlaced)
            {
                hasBeenPlaced = true;
                Debug.Log(gameObject.name + " placed! Notifying TutorialManager.");

                if (TutorialManager.Instance != null)
                    TutorialManager.Instance.Phase1_OnCornPlaced();
                else
                    Debug.LogError("TutorialManager.Instance is NULL!");
            }
        }
    }

    void OnPressed()
    {
        if (hasBeenPlaced) return;
        if (targetCLocation == null)
        {
            Debug.LogWarning(gameObject.name + ": No C location assigned!");
            return;
        }

        targetPos = targetCLocation.position;
        isMoving = true;
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        OnPressed();
    }
}