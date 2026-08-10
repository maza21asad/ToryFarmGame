using UnityEngine;
using UnityEngine.UI;

public class ImageToggleSize : MonoBehaviour
{
    private RectTransform rectTransform;
    private Vector3 originalScale;
    private Vector3 enlargedScale;
    private bool isEnlarged = false;

    public float enlargeScale = 1.5f; // How much to enlarge (1.5 = 150% of original)
    public float animationSpeed = 0.2f; // Duration of size change animation

    void Start()
    {
        rectTransform = GetComponent<RectTransform>();

        // Store original scale
        originalScale = rectTransform.localScale;

        // Calculate enlarged scale
        enlargedScale = originalScale * enlargeScale;

        // Add button component if not already there
        Button button = GetComponent<Button>();
        if (button == null)
        {
            button = gameObject.AddComponent<Button>();
        }

        // Add listener to button click
        button.onClick.AddListener(ToggleSize);

        // Make it interactable
        button.interactable = true;

        Debug.Log("ImageToggleSize initialized - Original scale: " + originalScale + ", Enlarged scale: " + enlargedScale);
    }

    void ToggleSize()
    {
        Debug.Log("Image clicked! Current state: " + (isEnlarged ? "Enlarged" : "Normal"));

        if (isEnlarged)
        {
            // Shrink back to original size
            StopAllCoroutines();
            StartCoroutine(AnimateSize(enlargedScale, originalScale, animationSpeed));
            isEnlarged = false;
            Debug.Log("Shrinking to original size");
            SoundManager.Instance.PlaySFX("PicPopUp");
        }
        else
        {
            // Enlarge
            StopAllCoroutines();
            StartCoroutine(AnimateSize(originalScale, enlargedScale, animationSpeed));
            isEnlarged = true;
            Debug.Log("Enlarging to " + enlargeScale + "x size");
            SoundManager.Instance.PlaySFX("PicPopUp");
        }
    }

    System.Collections.IEnumerator AnimateSize(Vector3 fromScale, Vector3 toScale, float duration)
    {
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;

            // Smooth easing (ease-in-out)
            t = t < 0.5f ? 2 * t * t : -1 + (4 - 2 * t) * t;

            rectTransform.localScale = Vector3.Lerp(fromScale, toScale, t);

            yield return null;
        }

        // Ensure final scale is exact
        rectTransform.localScale = toScale;
    }
}