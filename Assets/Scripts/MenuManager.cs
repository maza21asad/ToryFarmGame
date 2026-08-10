using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class MenuManager : MonoBehaviour
{
    public static MenuManager instance;

    [Header("Menu Panels")]
    public GameObject mainMenuPanel;
    public List<GameObject> allPanel;

    [Header("Timed Panels")]
    public int targetSceneIndex;

    [Header("Pause Settings")]
    public GameObject settingsPanel;

    private Stack<GameObject> panelHistory = new Stack<GameObject>();

    private void Awake()
    {
        if (instance == null) instance = this;
        SoundManager.Instance.PlayMusic("BackgroundMusic");
        ShowPanel(mainMenuPanel);
    }

    public void ShowPanelFor5SecThenLoad(GameObject panel)
    {
        StartCoroutine(PanelTimerRoutine(panel));
    }

    private IEnumerator PanelTimerRoutine(GameObject panel)
    {
        ShowPanel(panel);
        yield return new WaitForSeconds(5f);
        SceneManager.LoadScene(targetSceneIndex);
    }

    public void ShowPanel(GameObject panelToShow)
    {
        foreach (GameObject panel in allPanel) panel.SetActive(false);
        if (panelToShow != null)
        {
            if (settingsPanel != null && panelToShow == settingsPanel)
                Time.timeScale = 0f;
            AnimatePanelFade(panelToShow);
            panelHistory.Push(panelToShow);
        }
    }

    public void AnimatePanelFade(GameObject panel)
    {
        CanvasGroup cg = panel.GetComponent<CanvasGroup>();
        if (cg == null) cg = panel.AddComponent<CanvasGroup>();
        panel.transform.localScale = Vector3.one * 0.4f;
        cg.alpha = 1f;
        panel.SetActive(true);
        panel.transform.DOScale(1f, 0.4f).SetEase(Ease.OutBack).SetUpdate(true);
        cg.DOFade(1f, 0.3f).SetUpdate(true);
    }

    private void ResumeIfLeavingSettings(GameObject closingPanel)
    {
        if (settingsPanel != null && closingPanel == settingsPanel)
            Time.timeScale = 1f;
    }

    public void CloseCurrectPanel()
    {
        if (panelHistory.Count == 0) return;
        GameObject closePanel = panelHistory.Pop();
        ResumeIfLeavingSettings(closePanel);
        closePanel.transform.DOScale(Vector3.zero, 0.25f).SetEase(Ease.InBack).SetUpdate(true)
            .OnComplete(() => closePanel.SetActive(false));
        if (panelHistory.Count > 0)
            AnimatePanelFade(panelHistory.Peek());
        else
        {
            AnimatePanelFade(mainMenuPanel);
            panelHistory.Push(mainMenuPanel);
        }
    }

    public void GoBack()
    {
        if (panelHistory.Count <= 1) return;
        GameObject current = panelHistory.Pop();
        GameObject previous = panelHistory.Peek();
        ResumeIfLeavingSettings(current);
        CanvasGroup cg = current.GetComponent<CanvasGroup>();
        if (cg == null) cg = current.AddComponent<CanvasGroup>();
        current.transform.DOScale(0.8f, 0.25f).SetEase(Ease.InBack).SetUpdate(true);
        cg.DOFade(0f, 0.25f).SetUpdate(true).OnComplete(() =>
        {
            current.SetActive(false);
            AnimatePanelFade(previous);
        });
    }

    public void LoadScene(int sceneId)
    {
        SoundManager.Instance.StopSFXLoop();
        SceneManager.LoadScene(sceneId);
    }
}