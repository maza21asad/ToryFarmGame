using UnityEngine;
using System;
using System.Collections.Generic;

public class FinishLineMover : MonoBehaviour
{
    [Header("Points")]
    public RectTransform startSpawner;
    public RectTransform endSpawner;

    [Header("Size (Scale)")]
    public float startScale = 0.3f;
    public float endScale   = 1.0f;

    [Header("Speed")]
    public float speed = 0.3f;

    [HideInInspector] public Action onFinishLineReachedEnd;
    [HideInInspector] public Action onFinishLinePassedPlayer;

    private RectTransform rectTransform;
    private float progress = 0f;
    private bool hasPassedPlayer = false;

    void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
    }

    public void ResetFinishLine()
    {
        progress        = 0f;
        hasPassedPlayer = false;

        rectTransform.position = startSpawner.position;
        transform.localScale   = Vector3.one * startScale;
    }

    void Update()
    {
        if (startSpawner == null || endSpawner == null) return;

        progress += Time.deltaTime * speed;
        progress  = Mathf.Clamp01(progress);

        rectTransform.position = Vector3.Lerp(
            startSpawner.position,
            endSpawner.position,
            progress
        );

        float currentScale   = Mathf.Lerp(startScale, endScale, progress);
        transform.localScale = Vector3.one * currentScale;

        if (progress >= 1f)
        {
            if (!hasPassedPlayer)
            {
                hasPassedPlayer = true;
                onFinishLinePassedPlayer?.Invoke();
            }
            onFinishLineReachedEnd?.Invoke();
        }
    }
}