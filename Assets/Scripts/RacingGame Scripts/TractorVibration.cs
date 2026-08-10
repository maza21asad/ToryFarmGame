using UnityEngine;

public class TractorVibration : MonoBehaviour
{
    [Header("Up & Down Vibration")]
    public float bounceAmount = 0.05f;    // How high it moves up and down
    public float bounceSpeed = 25f;       // How fast it bounces

    [Header("Engine")]
    public bool isRunning = true;

    private Vector3 originalPosition;

    void Start()
    {
        originalPosition = transform.localPosition;
        isRunning = false;
    }

    void Update()
    {
        if (!isRunning)
        {
            transform.localPosition = Vector3.Lerp(
                transform.localPosition,
                originalPosition,
                Time.deltaTime * 10f
            );
            return;
        }

        // Only Y axis moves — pure up and down
        float newY = originalPosition.y + Mathf.Sin(Time.time * bounceSpeed) * bounceAmount;

        transform.localPosition = new Vector3(
            originalPosition.x,
            newY,
            originalPosition.z
        );
    }

    public void StartEngine()
    {
        isRunning = true;
        SoundManager.Instance.PlaySFXLoop("Tractor");
    }

    public void StopEngine()
    {
        isRunning = false;
        SoundManager.Instance.StopSFXLoop();
    }
}