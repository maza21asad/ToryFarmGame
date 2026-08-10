using DG.Tweening;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class UIManager : MonoBehaviour
{
    public static UIManager Instance;

    private Stack<GameObject> panelHistory = new Stack<GameObject>();

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    public void ShowPanel(GameObject panelToShow, List<GameObject> allPanels = null)
    {
        if (allPanels != null)
            foreach (GameObject panel in allPanels) panel.SetActive(false);

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
        panel.transform.DOScale(1f, 0.4f).SetEase(Ease.OutBack).SetUpdate(true);
        cg.DOFade(1f, 0.3f).SetUpdate(true);
    }

    public void CloseCurrectPanel()
    {
        if (panelHistory.Count == 0) return;
        GameObject closePanel = panelHistory.Pop();
        closePanel.transform.DOScale(Vector3.zero, 0.25f).SetEase(Ease.InBack).SetUpdate(true)
            .OnComplete(() => closePanel.SetActive(false));
        if (panelHistory.Count > 0)
            AnimatePanelFade(panelHistory.Peek());
    }

    public void GoBack()
    {
        if (panelHistory.Count <= 1) return;
        GameObject current = panelHistory.Pop();
        GameObject previous = panelHistory.Peek();
        CanvasGroup cg = current.GetComponent<CanvasGroup>();
        if (cg == null) cg = current.AddComponent<CanvasGroup>();
        current.transform.DOScale(0.8f, 0.25f).SetEase(Ease.InBack).SetUpdate(true);
        cg.DOFade(0f, 0.25f).SetUpdate(true).OnComplete(() =>
        {
            current.SetActive(false);
            AnimatePanelFade(previous);
        });
    }

    public void ClearHistory() => panelHistory.Clear();

    public void LoadScene(int sceneId)
    {
        ClearHistory();
        SceneManager.LoadScene(sceneId);
    }
}