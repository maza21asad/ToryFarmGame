using UnityEngine;
using System.Collections;

public class ButtonPressAnimation : MonoBehaviour
{
    [Header("Animation Settings")]
    public float shrinkScale = 0.85f;      
    public float shrinkDuration = 0.08f;   
    public float returnDuration = 0.12f;   

    private Vector3 originalScale;
    private bool isAnimating = false;

    void Awake()
    {
        originalScale = transform.localScale;
    }

    public void OnButtonPressed()
    {
        if (!isAnimating)
            StartCoroutine(PressAnimation());
    }

    IEnumerator PressAnimation()
    {
        isAnimating = true;


        float elapsed = 0f;
        Vector3 targetScale = originalScale * shrinkScale;

        while (elapsed < shrinkDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / shrinkDuration;
            transform.localScale = Vector3.Lerp(originalScale, targetScale, t);
            yield return null;
        }
        transform.localScale = targetScale;


        elapsed = 0f;
        while (elapsed < returnDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / returnDuration;
            float eased = 1f - Mathf.Pow(1f - t, 3f);
            transform.localScale = Vector3.Lerp(targetScale, originalScale, eased);
            yield return null;
        }
        transform.localScale = originalScale;

        isAnimating = false;
    }
}
