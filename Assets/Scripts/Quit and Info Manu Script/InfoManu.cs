using UnityEngine;
using System.Collections;

public class InfoManu : MonoBehaviour
{
    public GameObject infoMenuUI;
    public float animationDuration = 0.3f;
    public float buttonDelay = 0.5f;

    private RectTransform menuRect;

    void Start()
    {
        infoMenuUI.SetActive(false);
        menuRect = infoMenuUI.GetComponent<RectTransform>();
    }

    public void OpenInfo()
    {
        StopAllCoroutines();
        StartCoroutine(DelayedAction(() =>
        {
            // Pause the game
            Time.timeScale = 0f;
            infoMenuUI.SetActive(true);
            StartCoroutine(PopupOpen());
        }));
    }

    public void CloseInfo()
    {
        StopAllCoroutines();
        StartCoroutine(DelayedAction(() =>
        {
            StartCoroutine(PopupClose());
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

    private IEnumerator PopupClose()
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
        infoMenuUI.SetActive(false);

        // Resume the game
        Time.timeScale = 1f;
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