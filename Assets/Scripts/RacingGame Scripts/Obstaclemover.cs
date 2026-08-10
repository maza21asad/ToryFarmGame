using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;

public class ObstacleMover : MonoBehaviour
{
    [Header("Points")]
    public RectTransform startSpawner;
    public RectTransform endSpawner;

    [Header("Size (Scale)")]
    public float startScale = 0.3f;
    public float endScale = 1.0f;

    [Header("Speed")]
    public float speed = 0.3f;

    [Header("Camera Shake")]
    public RectTransform shakeTarget;
    public float shakeDuration = 0.5f;
    public float shakeMagnitude = 30f;

    [HideInInspector] public RectTransform playerObject;
    [HideInInspector] public BoxCollider2D[] playerColliders;
    [HideInInspector] public GameObject gameOverPanel;
    [HideInInspector] public GameObject crashEffect;
    [HideInInspector] public Action onCarFinished;
    [HideInInspector] public Action onCarPassedPlayer;
    [HideInInspector] public Action onGameOver;

    private RectTransform rectTransform;
    private BoxCollider2D[] carColliders;
    private float progress = 0f;
    private bool isGameOver = false;
    private bool hasPassedPlayer = false;
    private bool isCaught = false;

    void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
    }

    public void ResetCar()
    {
        progress = 0f;
        isGameOver = false;
        hasPassedPlayer = false;
        isCaught = false;

        rectTransform.position = startSpawner.position;
        transform.localScale = Vector3.one * startScale;

        // Collect all BoxCollider2D on this car and its children
        carColliders = GetComponentsInChildren<BoxCollider2D>(true);

        if (carColliders.Length == 0)
            Debug.LogWarning("ObstacleMover → No BoxCollider2D found on " + gameObject.name + "! Add one to the car or its children.");
    }

    void Update()
    {
        if (isGameOver) return;
        if (startSpawner == null || endSpawner == null) return;

        progress += Time.deltaTime * speed;
        progress = Mathf.Clamp01(progress);

        rectTransform.position = Vector3.Lerp(
            startSpawner.position,
            endSpawner.position,
            progress
        );

        float currentScale = Mathf.Lerp(startScale, endScale, progress);
        transform.localScale = Vector3.one * currentScale;

        if (progress > 0.3f)
        {
            if (!isCaught) CheckCollision();
        }

        if (progress >= 1f)
        {
            if (!hasPassedPlayer)
            {
                hasPassedPlayer = true;
                onCarPassedPlayer?.Invoke();
            }
            onCarFinished?.Invoke();
        }
    }

    public void SetCaught()
    {
        isCaught = true;
    }

    void CheckCollision()
    {
        if (playerColliders == null || playerColliders.Length == 0) return;
        if (carColliders == null || carColliders.Length == 0) return;

        foreach (BoxCollider2D carCol in carColliders)
        {
            if (carCol == null) return;

            foreach (BoxCollider2D playerCol in playerColliders)
            {
                if (playerCol == null) continue;

                // Check if the two box collider bounds overlap
                if (carCol.bounds.Intersects(playerCol.bounds))
                {
                    Vector3 crashPos = (carCol.bounds.center + playerCol.bounds.center) / 2f;
                    crashPos.z = 0f;
                    TriggerGameOver(crashPos);
                    return;
                }
            }
        }
    }

    void TriggerGameOver(Vector3 crashPos)
    {
        if (isGameOver) return;
        isGameOver = true;

        onGameOver?.Invoke();

        SoundManager.Instance.StopSFXLoop();
        SoundManager.Instance.PlaySFX("Crush");

        if (playerObject != null)
        {
            ImageAnimator[] animators = playerObject.GetComponentsInChildren<ImageAnimator>(true);
            foreach (ImageAnimator anim in animators)
                anim.Stop();

            TractorVibration[] vibrations = playerObject.GetComponentsInChildren<TractorVibration>(true);
            foreach (TractorVibration vib in vibrations)
                vib.StopEngine();
        }

        if (shakeTarget != null)
            StartCoroutine(CameraShake());

        StartCoroutine(ShowCrashThenGameOver(crashPos));
    }

    IEnumerator CameraShake()
    {
        Vector3 originalPos = shakeTarget.localPosition;
        float elapsed = 0f;

        while (elapsed < shakeDuration)
        {
            float x = UnityEngine.Random.Range(-1f, 1f) * shakeMagnitude;
            float y = UnityEngine.Random.Range(-1f, 1f) * shakeMagnitude;

            shakeTarget.localPosition = new Vector3(
                originalPos.x + x,
                originalPos.y + y,
                originalPos.z
            );

            elapsed += Time.deltaTime;
            yield return null;
        }

        shakeTarget.localPosition = originalPos;
    }

    IEnumerator ShowCrashThenGameOver(Vector3 crashPos)
    {
        if (crashEffect != null)
        {
            RectTransform crashRT = crashEffect.GetComponent<RectTransform>();
            if (crashRT != null)
                crashRT.position = crashPos;
            else
                crashEffect.transform.position = crashPos;

            crashEffect.SetActive(true);
        }

        foreach (ImageAnimator anim in FindObjectsOfType<ImageAnimator>(true))
            anim.enabled = false;

        foreach (TractorVibration vib in FindObjectsOfType<TractorVibration>(true))
            vib.enabled = false;

        foreach (TireLineMover tire in FindObjectsOfType<TireLineMover>(true))
            tire.enabled = false;

        yield return new WaitForSecondsRealtime(2f);

        if (crashEffect != null)
            crashEffect.SetActive(false);

        if (gameOverPanel != null)
        {
            gameOverPanel.SetActive(true);

            RectTransform rt = gameOverPanel.GetComponent<RectTransform>();
            if (rt != null)
            {
                rt.anchorMin = Vector2.zero;
                rt.anchorMax = Vector2.one;
                rt.offsetMin = Vector2.zero;
                rt.offsetMax = Vector2.zero;
                rt.localScale = Vector3.one;
            }
        }

        Time.timeScale = 0f;
    }
}