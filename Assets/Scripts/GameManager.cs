using DG.Tweening;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;

    public List<GameObject> allPanel;
    private Stack<GameObject> panelHistory = new Stack<GameObject>();

    [Header("Dog Animation")]
    public DogAnimationController dogController;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    // ========================
    // PANEL MANAGEMENT
    // ========================
    public void ShowPanel(GameObject panelToShow)
    {
        foreach (GameObject panel in allPanel) panel.SetActive(false);
        if (panelToShow != null)
        {
            AnimatePanelFade(panelToShow);
            panelHistory.Push(panelToShow);
        }
    }

    public void AnimatePanelFade(GameObject panel)
    {
        CanvasGroup cg = panel.GetComponent<CanvasGroup>();
        if (cg == null) cg = panel.AddComponent<CanvasGroup>();
        panel.transform.localScale = Vector3.one * 0.4f;
        cg.alpha = 0f;
        panel.SetActive(true);
        panel.transform.DOScale(1f, 0.4f).SetEase(Ease.OutBack);
        cg.DOFade(1f, 0.3f);
    }

    public void AnimatePanelClose(GameObject panel, System.Action onComplete = null)
    {
        panel.transform.DOScale(Vector3.zero, 0.25f).SetEase(Ease.InBack)
            .OnComplete(() =>
            {
                panel.SetActive(false);
                onComplete?.Invoke();
            });
    }

    public void CloseCurrectPanel()
    {
        if (panelHistory.Count == 0) return;
        GameObject closePanel = panelHistory.Pop();
        closePanel.transform.DOScale(Vector3.zero, 0.25f).SetEase(Ease.InBack)
            .OnComplete(() => closePanel.SetActive(false));
        if (panelHistory.Count > 0) AnimatePanelFade(panelHistory.Peek());
    }

    public void RestartGame()
    {
        DOTween.KillAll();
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    public void LoadScene(int sceneId)
    {
        DOTween.KillAll();
        SceneManager.LoadScene(sceneId);
    }
}