using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using System.Collections.Generic;

public class ColorDrag : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    public Color dragColor;

    private RectTransform rectTransform;
    private Canvas rootCanvas;
    private CanvasGroup canvasGroup;
    private Transform originalParent;
    private int originalSiblingIndex;
    private Vector2 originalAnchoredPos;
    private ColorDropArea currentPreviewArea = null;
    private int _totalZones = 0;
    private Image _buttonImage;

    void Start()
    {
        rectTransform = GetComponent<RectTransform>();
        canvasGroup = GetComponent<CanvasGroup>();
        rootCanvas = GetComponentInParent<Canvas>().rootCanvas;
        _buttonImage = GetComponent<Image>();

        // Count how many drop zones belong to this color
        foreach (ColorDropArea area in FindObjectsOfType<ColorDropArea>())
        {
            if (ColorsMatch(area.correctColor, dragColor))
                _totalZones++;
        }
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        // Hide tutorial hand as soon as the player grabs any color
        PaintingTutorialManager.Instance?.HideTutorial();

        originalParent = transform.parent;
        originalSiblingIndex = transform.GetSiblingIndex();
        originalAnchoredPos = rectTransform.anchoredPosition;

        transform.SetParent(rootCanvas.transform, true);
        transform.SetAsLastSibling();
        canvasGroup.blocksRaycasts = false;
    }

    public void OnDrag(PointerEventData eventData)
    {
        rectTransform.anchoredPosition += eventData.delta / rootCanvas.scaleFactor;

        // ✅ Get best area under pointer using pixel check
        ColorDropArea hoveredArea = GetDropAreaUnderPointer(eventData.position);

        if (hoveredArea != currentPreviewArea)
        {
            // ✅ Hide ALL previews first
            ColorDropArea[] allAreas = FindObjectsOfType<ColorDropArea>();
            foreach (ColorDropArea area in allAreas)
                area.HidePreview();

            // ✅ Show preview on ONLY the correct area
            if (hoveredArea != null)
                hoveredArea.ShowPreview(dragColor);

            currentPreviewArea = hoveredArea;
        }
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        canvasGroup.blocksRaycasts = true;

        // ✅ Hide all previews on drop
        ColorDropArea[] allAreas = FindObjectsOfType<ColorDropArea>();
        foreach (ColorDropArea area in allAreas)
            area.HidePreview();

        currentPreviewArea = null;

        ColorDropArea bestMatch = GetDropAreaUnderPointer(eventData.position);

        if (bestMatch != null)
        {
            Debug.Log($"[ColorDrag] Dropping on: {bestMatch.gameObject.name}");
            bestMatch.OnDrop(eventData);
        }
        else
        {
            Debug.Log("[ColorDrag] Nothing found - empty area");
        }

        transform.SetParent(originalParent, true);
        transform.SetSiblingIndex(originalSiblingIndex);
        rectTransform.anchoredPosition = originalAnchoredPos;
    }

    private ColorDropArea GetDropAreaUnderPointer(Vector2 screenPos)
    {
        ColorDropArea[] allDropAreas = FindObjectsOfType<ColorDropArea>();

        ColorDropArea bestMatch = null;
        int highestSiblingIndex = -1;

        foreach (ColorDropArea dropArea in allDropAreas)
        {
            if (dropArea.IsColored) continue;
            if (!IsVisiblePixelAtPosition(dropArea, screenPos)) continue;

            int siblingIndex = dropArea.transform.GetSiblingIndex();
            if (siblingIndex > highestSiblingIndex)
            {
                highestSiblingIndex = siblingIndex;
                bestMatch = dropArea;
            }
        }

        return bestMatch;
    }

    private bool IsVisiblePixelAtPosition(ColorDropArea dropArea, Vector2 screenPos)
    {
        Image img = dropArea.GetComponent<Image>();
        if (img == null || img.sprite == null) return false;

        RectTransform rt = img.GetComponent<RectTransform>();

        Camera cam = rootCanvas.renderMode == RenderMode.ScreenSpaceOverlay
            ? null : Camera.main;

        if (!RectTransformUtility.RectangleContainsScreenPoint(rt, screenPos, cam))
            return false;

        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            rt, screenPos, cam, out Vector2 localPoint
        );

        Rect rect = rt.rect;
        float normalizedX = Mathf.Clamp01((localPoint.x - rect.x) / rect.width);
        float normalizedY = Mathf.Clamp01((localPoint.y - rect.y) / rect.height);

        Sprite sprite = img.sprite;
        Texture2D tex = sprite.texture;
        Rect spriteRect = sprite.textureRect;

        int px = Mathf.RoundToInt(spriteRect.x + normalizedX * spriteRect.width);
        int py = Mathf.RoundToInt(spriteRect.y + normalizedY * spriteRect.height);

        px = Mathf.Clamp(px, 0, tex.width - 1);
        py = Mathf.Clamp(py, 0, tex.height - 1);

        Color pixel = tex.GetPixel(px, py);
        return pixel.a > 0.01f;
    }

    /// <summary>
    /// Called after a drop — darkens this button if all its zones are now colored.
    /// </summary>
    //public void CheckCompletion()
    //{
    //    if (_totalZones == 0) return;

    //    int doneCount = 0;
    //    foreach (ColorDropArea area in FindObjectsOfType<ColorDropArea>())
    //    {
    //        if (ColorsMatch(area.correctColor, dragColor) && area.IsColored)
    //            doneCount++;
    //    }

    //    if (doneCount >= _totalZones && _buttonImage != null)
    //        _buttonImage.color = new Color(0.45f, 0.45f, 0.45f, 1f);
    //}

    public void CheckCompletion()
    {
        if (_totalZones == 0) return;

        int doneCount = 0;
        foreach (ColorDropArea area in FindObjectsOfType<ColorDropArea>())
        {
            if (ColorsMatch(area.correctColor, dragColor) && area.IsColored)
                doneCount++;
        }

        if (doneCount >= _totalZones)
        {
            if (_buttonImage != null)
            {
                Color c = _buttonImage.color;
                _buttonImage.color = new Color(c.r, c.g, c.b, 150f / 255f);
            }
            canvasGroup.blocksRaycasts = false;
            canvasGroup.interactable = false;
        }
    }

    private bool ColorsMatch(Color a, Color b, float tolerance = 0.1f)
    {
        return Mathf.Abs(a.r - b.r) < tolerance &&
               Mathf.Abs(a.g - b.g) < tolerance &&
               Mathf.Abs(a.b - b.b) < tolerance;
    }

    public void ResetButton()
    {
        if (_buttonImage != null)
        {
            Color c = _buttonImage.color;
            _buttonImage.color = new Color(c.r, c.g, c.b, 1f);
        }
        canvasGroup.blocksRaycasts = true;
        canvasGroup.interactable = true;
    }
}