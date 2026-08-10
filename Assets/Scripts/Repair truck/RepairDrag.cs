using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class RepairDrag : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    [Header("Part Identity")]
    public string partID;

    private RectTransform rectTransform;
    private Canvas rootCanvas;
    private CanvasGroup canvasGroup;
    private Transform originalParent;
    private int originalSiblingIndex;
    private Vector2 originalAnchoredPos;
    private RepairSlot currentPreviewSlot = null;

    void Start()
    {
        rectTransform = GetComponent<RectTransform>();
        canvasGroup = GetComponent<CanvasGroup>();
        rootCanvas = GetComponentInParent<Canvas>().rootCanvas;
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        // Hide tutorial hand as soon as the player grabs any part
        TruckRepairTutorialManager.Instance?.HideTutorial();

        originalParent = transform.parent;
        originalSiblingIndex = transform.GetSiblingIndex();
        originalAnchoredPos = rectTransform.anchoredPosition;

        transform.SetParent(rootCanvas.transform, true);
        transform.SetAsLastSibling();
        canvasGroup.blocksRaycasts = false;

        HideAllPreviews();
        currentPreviewSlot = null;
    }

    public void OnDrag(PointerEventData eventData)
    {
        rectTransform.anchoredPosition += eventData.delta / rootCanvas.scaleFactor;

        RepairSlot hoveredSlot = GetMatchingSlotUnderPointer(eventData.position);

        if (hoveredSlot != currentPreviewSlot)
        {
            HideAllPreviews();

            if (hoveredSlot != null)
                hoveredSlot.ShowPreview();

            currentPreviewSlot = hoveredSlot;
        }
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        canvasGroup.blocksRaycasts = true;
        HideAllPreviews();
        currentPreviewSlot = null;

        RepairSlot targetSlot = GetMatchingSlotUnderPointer(eventData.position);

        if (targetSlot != null)
        {
            targetSlot.Repair(this);
        }
        else
        {
            RepairSlot wrongSlot = GetAnyVisibleSlotUnderPointer(eventData.position);
            if (wrongSlot != null)
                wrongSlot.WrongDrop();
        }

        transform.SetParent(originalParent, true);
        transform.SetSiblingIndex(originalSiblingIndex);
        rectTransform.anchoredPosition = originalAnchoredPos;
    }

    void HideAllPreviews()
    {
        RepairSlot[] allSlots = FindObjectsOfType<RepairSlot>();
        foreach (RepairSlot slot in allSlots)
            slot.HidePreview();
    }

    private RepairSlot GetMatchingSlotUnderPointer(Vector2 screenPos)
    {
        RepairSlot[] allSlots = FindObjectsOfType<RepairSlot>();

        foreach (RepairSlot slot in allSlots)
        {
            if (slot.isRepaired) continue;
            if (slot.requiredPartID != partID) continue;
            if (IsVisiblePixelAtPosition(slot, screenPos))
                return slot;
        }

        return null;
    }

    private RepairSlot GetAnyVisibleSlotUnderPointer(Vector2 screenPos)
    {
        RepairSlot[] allSlots = FindObjectsOfType<RepairSlot>();

        foreach (RepairSlot slot in allSlots)
        {
            if (slot.isRepaired) continue;
            if (IsVisiblePixelAtPosition(slot, screenPos))
                return slot;
        }

        return null;
    }

    private bool IsVisiblePixelAtPosition(RepairSlot slot, Vector2 screenPos)
    {
        Image img = slot.repairedPartImage;
        if (img == null || img.sprite == null) return false;

        RectTransform rt = img.GetComponent<RectTransform>();
        if (rt == null) return false;

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
        return pixel.a > 0.1f;
    }
}