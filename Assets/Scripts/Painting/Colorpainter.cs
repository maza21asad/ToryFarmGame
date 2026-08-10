using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class ColorPainter : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    // Color palette - the draggable color buttons
    private Image colorImage;
    private Color draggedColor;
    private Vector3 originalPosition;
    private CanvasGroup canvasGroup;

    // Paintable areas
    [SerializeField] private Image paintableImage; // The B&W image to paint
    private Texture2D paintTexture;
    private Color[] originalPixels;

    void Start()
    {
        colorImage = GetComponent<Image>();
        canvasGroup = GetComponent<CanvasGroup>();

        if (canvasGroup == null)
        {
            canvasGroup = gameObject.AddComponent<CanvasGroup>();
        }

        // Store the draggable color
        draggedColor = colorImage.color;
        originalPosition = transform.position;

        Debug.Log("ColorPainter initialized - Color: " + draggedColor);
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        Debug.Log("Started dragging color: " + draggedColor);

        // Make color semi-transparent while dragging
        canvasGroup.alpha = 0.7f;
    }

    public void OnDrag(PointerEventData eventData)
    {
        // Move the color button with cursor
        transform.position = Input.mousePosition;
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        Debug.Log("Dropped color");

        // Check if dropped on a paintable area
        PointerEventData pointerData = new PointerEventData(EventSystem.current)
        {
            position = Input.mousePosition
        };

        var results = new System.Collections.Generic.List<RaycastResult>();
        EventSystem.current.RaycastAll(pointerData, results);

        bool paintedSuccessfully = false;
        foreach (RaycastResult result in results)
        {
            PaintableArea paintable = result.gameObject.GetComponent<PaintableArea>();
            if (paintable != null)
            {
                paintable.PaintArea(draggedColor);
                paintedSuccessfully = true;
                Debug.Log("Painted area with color: " + draggedColor);
                break;
            }
        }

        // Return to original position
        transform.position = originalPosition;
        canvasGroup.alpha = 1f;
    }
}

public class PaintableArea : MonoBehaviour
{
    private Image areaImage;
    private Outline outline;

    void Start()
    {
        areaImage = GetComponent<Image>();
        outline = GetComponent<Outline>();

        // Add outline if not present (for visual feedback)
        if (outline == null)
        {
            outline = gameObject.AddComponent<Outline>();
            outline.effectColor = new Color(0, 0, 0, 0.5f);
            outline.effectDistance = new Vector2(2, 2);
        }

        Debug.Log("PaintableArea initialized: " + gameObject.name);
    }

    public void PaintArea(Color color)
    {
        // Change the image color
        areaImage.color = color;

        // Play animation feedback
        StartCoroutine(PaintAnimation());

        Debug.Log("Area painted with color: " + color + " for object: " + gameObject.name);
    }

    System.Collections.IEnumerator PaintAnimation()
    {
        // Slight scale animation for feedback
        Vector3 originalScale = transform.localScale;

        // Scale up
        float elapsed = 0f;
        while (elapsed < 0.15f)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / 0.15f;
            transform.localScale = Vector3.Lerp(originalScale, originalScale * 1.1f, t);
            yield return null;
        }

        // Scale back
        elapsed = 0f;
        while (elapsed < 0.15f)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / 0.15f;
            transform.localScale = Vector3.Lerp(originalScale * 1.1f, originalScale, t);
            yield return null;
        }

        transform.localScale = originalScale;
    }
}