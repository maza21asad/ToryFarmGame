using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

public class YouWinPanel : MonoBehaviour
{
    [Header("Pop Up Settings")]
    public float popSpeed = 5f;
    public float overshootScale = 1.2f;

    private Vector3 originalScale;

    void OnEnable()
    {
        originalScale = transform.localScale;
        transform.localScale = Vector3.zero;
        StartCoroutine(PopUp());
    }

    IEnumerator PopUp()
    {
        float progress = 0f;
        while (progress < 1f)
        {
            progress += Time.deltaTime * popSpeed;
            progress  = Mathf.Clamp01(progress);

            transform.localScale = Vector3.Lerp(
                Vector3.zero,
                originalScale * overshootScale,
                progress
            );

            yield return null;
        }

        progress = 0f;
        while (progress < 1f)
        {
            progress += Time.deltaTime * popSpeed;
            progress  = Mathf.Clamp01(progress);

            transform.localScale = Vector3.Lerp(
                originalScale * overshootScale,
                originalScale,
                progress
            );

            yield return null;
        }

        transform.localScale = originalScale;
    }

    // Assign this to your Restart Button OnClick
    public void OnRestartButton()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }
}