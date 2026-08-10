using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class ColorDropArea : MonoBehaviour, IDropHandler, IPointerEnterHandler, IPointerExitHandler
{
    [Header("Settings")]
    public bool requireCorrectColor = false;
    public Color correctColor;

    [Header("Target Image to Color")]
    public Image targetPartImage;

    public bool IsColored { get; private set; }

    private Color originalColor;

    void Start()
    {
        if (targetPartImage == null)
            targetPartImage = GetComponent<Image>();

        originalColor = targetPartImage.color;

        targetPartImage.color = new Color(
            originalColor.r,
            originalColor.g,
            originalColor.b,
            0f
        );

        targetPartImage.raycastTarget = true;
        IsColored = false;
    }

    //public void ShowPreview(Color color)
    //{
    //    if (IsColored) return;
    //    targetPartImage.color = new Color(
    //        originalColor.r,
    //        originalColor.g,
    //        originalColor.b,
    //        0.5f
    //    );
    //}

    public void ShowPreview(Color color)
    {
        if (IsColored) return;
        if (!ColorsMatch(color, correctColor)) return;   // ← only hint on correct zone
        targetPartImage.color = new Color(
            originalColor.r,
            originalColor.g,
            originalColor.b,
            0.5f
        );
    }

    public void HidePreview()
    {
        if (IsColored) return;
        targetPartImage.color = new Color(
            originalColor.r,
            originalColor.g,
            originalColor.b,
            0f
        );
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (IsColored) return;
        if (eventData.pointerDrag == null) return;

        ColorDrag drag = eventData.pointerDrag.GetComponent<ColorDrag>();
        if (drag == null) return;

        ShowPreview(drag.dragColor);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (IsColored) return;
        HidePreview();
    }

    public void OnDrop(PointerEventData eventData)
    {
        if (IsColored) return;

        ColorDrag drag = eventData.pointerDrag?.GetComponent<ColorDrag>();
        if (drag == null) return;

        if (requireCorrectColor && !ColorsMatch(drag.dragColor, correctColor))
        {
            Debug.Log($"[DropArea] Wrong color for {gameObject.name}!");
            HidePreview();
            return;
        }

        targetPartImage.color = new Color(
            originalColor.r,
            originalColor.g,
            originalColor.b,
            1f
        );
        IsColored = true;
        Debug.Log($"[DropArea] SUCCESS - {gameObject.name} colored!");
        SoundManager.Instance.PlaySFX("ColorDrop");

        // Tell the dragged color button to check if all its zones are done
        foreach (ColorDrag btn in FindObjectsOfType<ColorDrag>())
        {
            if (Mathf.Abs(btn.dragColor.r - drag.dragColor.r) < 0.1f &&
                Mathf.Abs(btn.dragColor.g - drag.dragColor.g) < 0.1f &&
                Mathf.Abs(btn.dragColor.b - drag.dragColor.b) < 0.1f)
            {
                btn.CheckCompletion();
                break;
            }
        }

        // ✅ Notify GameManager
        if (PaintingGameManager.Instance != null)
            PaintingGameManager.Instance.OnZoneColored();

    }

    private bool ColorsMatch(Color a, Color b, float tolerance = 0.1f)
    {
        return Mathf.Abs(a.r - b.r) < tolerance &&
               Mathf.Abs(a.g - b.g) < tolerance &&
               Mathf.Abs(a.b - b.b) < tolerance;
    }

    public void ResetZone()
    {
        IsColored = false;
        targetPartImage.color = new Color(
            originalColor.r,
            originalColor.g,
            originalColor.b,
            0f
        );
    }
}
//```

//---

//## Inspector Setup

//**GameManager:**
//```
//Congrats Panel    → your CongratsPanel GameObject
//Pulsing Object    → any GameObject (image/star/character)

//All Drop Areas (15):
//  Element 0  → Fench
//  Element 1  → Grass1
//  Element 2  → Grass2
//  Element 3  → Grass3
//  Element 4  → Grass4
//  Element 5  → Sky
//  Element 6  → Trees
//  Element 7  → House
//  Element 8  → Cows
//  Element 9  → BrownCow
//  Element 10 → WhiteCow
//  Element 11 → (add remaining)
//  Element 12 → ...
//  Element 13 → ...
//  Element 14 → ...
//```

//To add them quickly:
//```
//Click lock icon 🔒 on Inspector
//Select all 15 GameObjects in Hierarchy
//Drag them all into "All Drop Areas" list at once
//```

//## How It Works
//```
//Game starts     → coloredCount = 0, panel hidden ✅
//Color 1 done    → coloredCount = 1  (1/15) ✅
//Color 2 done    → coloredCount = 2  (2/15) ✅
//...
//Color 15 done   → coloredCount = 15 (15/15)
//                → CongratsPanel appears! 🎉
//                → PulsingObject bounces forever! 💫
//Close button    → panel hides, pulse stops ✅