using UnityEngine;
using System.Collections;

public class LidOpenWobble : MonoBehaviour
{
    [Header("Wobble Settings")]
    public float wobbleAngle = 8f;   // how far it rotates
    public float wobbleSpeed = 18f;  // how fast it wobbles
    public float wobbleDuration = 0.5f; // how long it wobbles

    private Quaternion originalRotation;

    void Awake()
    {
        originalRotation = transform.localRotation;
    }

    public void PlayWobble()
    {
        StopAllCoroutines();
        StartCoroutine(Wobble());
    }

    IEnumerator Wobble()
    {
        float elapsed = 0f;

        while (elapsed < wobbleDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / wobbleDuration;

            // Dampen the angle over time so it fades out
            float currentAngle = wobbleAngle * (1f - t);
            float angle = Mathf.Sin(elapsed * wobbleSpeed) * currentAngle;

            transform.localRotation = originalRotation * Quaternion.Euler(0f, 0f, angle);
            yield return null;
        }

        transform.localRotation = originalRotation;
    }
}