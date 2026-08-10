using UnityEngine;
using System.Collections;

public class PauseManu : MonoBehaviour
{
    public GameObject pauseMenuUI;
    public float animationDuration = 0.3f;
    public float buttonDelay = 0.5f;

    private RectTransform menuRect;

    void Start()
    {
        pauseMenuUI.SetActive(false);
        menuRect = pauseMenuUI.GetComponent<RectTransform>();
    }

    public void PauseGame()
    {
        StopAllCoroutines();
        if (menuRect != null)
            menuRect.localScale = Vector3.one;

        StartCoroutine(DelayedAction(() =>
        {
            pauseMenuUI.SetActive(true);
            Time.timeScale = 0f;
            StartCoroutine(PopupOpen());
        }));
    }

    public void ResumeGame()
    {
        StopAllCoroutines();
        StartCoroutine(DelayedAction(() =>
        {
            StartCoroutine(PopupClose(false));
        }));
    }

    public void QuitGame()
    {
        StopAllCoroutines();
        StartCoroutine(DelayedAction(() =>
        {
            StartCoroutine(PopupClose(true));
        }));
    }


    private IEnumerator DelayedAction(System.Action action)
    {
        yield return new WaitForSecondsRealtime(buttonDelay);
        action?.Invoke();
    }


    private IEnumerator PopupOpen()
    {
        menuRect.localScale = Vector3.zero;
        float elapsed = 0f;

        while (elapsed < animationDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = elapsed / animationDuration;
            float scale = EaseOutBack(t);
            menuRect.localScale = new Vector3(scale, scale, 1f);
            yield return null;
        }

        menuRect.localScale = Vector3.one;
    }


    private IEnumerator PopupClose(bool quit)
    {
        float elapsed = 0f;

        while (elapsed < animationDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = elapsed / animationDuration;
            float scale = 1f - EaseInBack(t);
            scale = Mathf.Clamp(scale, 0f, 1f);
            menuRect.localScale = new Vector3(scale, scale, 1f);
            yield return null;
        }

        menuRect.localScale = Vector3.zero;
        pauseMenuUI.SetActive(false);
        Time.timeScale = 1f;

        if (quit)
        {
            Application.Quit();
            #if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
            #endif
        }
    }


    private float EaseOutBack(float t)
    {
        float c1 = 1.70158f;
        float c3 = c1 + 1f;
        return 1f + c3 * Mathf.Pow(t - 1f, 3f) + c1 * Mathf.Pow(t - 1f, 2f);
    }


    private float EaseInBack(float t)
    {
        float c1 = 1.70158f;
        float c3 = c1 + 1f;
        return c3 * t * t * t - c1 * t * t;
    }
}
