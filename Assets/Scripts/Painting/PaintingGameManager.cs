using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;

public class PaintingGameManager : MonoBehaviour
{
    public static PaintingGameManager Instance;

    [Header("Drop Areas")]
    public List<ColorDropArea> allDropAreas = new List<ColorDropArea>();

    [Header("Congratulation Panel")]
    public GameObject congratsPanel;
    public GameObject congratsPulsingObject;

    [Header("Game Over Panel")]
    public GameObject gameOverPanel;
    public GameObject gameOverPulsingObject;

    [Header("UI Panels")]
    public GameObject settingsPanel;

    private int _coloredCount = 0;
    private bool _gameOverShown = false;

    void Awake()
    {
        Instance = this;
        SoundManager.Instance.PlayMusic("PaintingBGMusic");
    }

    void Start()
    {
        _coloredCount = 0;
        if (congratsPanel != null) congratsPanel.SetActive(false);
        if (gameOverPanel != null) gameOverPanel.SetActive(false);
        _gameOverShown = false;
    }

    public void OnZoneColored()
    {
        _coloredCount++;
        if (allDropAreas.Count > 0 && _coloredCount >= allDropAreas.Count)
            ShowCongrats();
    }

    // ========================
    // CONGRATS
    // ========================
    //public void ShowCongrats()
    //{
    //    if (congratsPanel != null)
    //    {
    //        GameManager.Instance.AnimatePanelFade(congratsPanel);
    //        SoundManager.Instance.PlaySFX("Congratulation");
    //    }
    //    if (GameManager.Instance.dogController != null)
    //        GameManager.Instance.dogController.PlayCongrats();
    //    if (congratsPulsingObject != null)
    //    {
    //        DOVirtual.DelayedCall(0.5f, () =>
    //        {
    //            if (congratsPulsingObject == null) return;
    //            congratsPulsingObject.transform.DOKill();
    //            congratsPulsingObject.transform.localScale = Vector3.one;
    //            congratsPulsingObject.transform
    //                .DOScale(1.15f, 0.8f).SetEase(Ease.InOutSine).SetLoops(-1, LoopType.Yoyo);
    //        });
    //    }
    //}

    public void ShowCongrats()
    {
        if (congratsPanel != null)
        {
            if (GameManager.Instance != null)
                GameManager.Instance.AnimatePanelFade(congratsPanel);
            else
                congratsPanel.SetActive(true);

            SoundManager.Instance.PlaySFX("Congratulation");
        }
        if (congratsPulsingObject != null)
        {
            DOVirtual.DelayedCall(0.5f, () =>
            {
                if (congratsPulsingObject == null) return;
                congratsPulsingObject.transform.DOKill();
                congratsPulsingObject.transform.localScale = Vector3.one;
                congratsPulsingObject.transform
                    .DOScale(1.15f, 0.8f).SetEase(Ease.InOutSine).SetLoops(-1, LoopType.Yoyo);
            });
        }
    }

    // ========================
    // GAME OVER
    // ========================
    public void ShowGameOver()
    {
        if (_gameOverShown) return;
        _gameOverShown = true;
        if (gameOverPanel == null) return;
        GameManager.Instance.AnimatePanelFade(gameOverPanel);
        if (gameOverPulsingObject != null)
        {
            DOVirtual.DelayedCall(0.5f, () =>
            {
                if (gameOverPulsingObject == null) return;
                gameOverPulsingObject.transform.DOKill();
                gameOverPulsingObject.transform.localScale = Vector3.one;
                gameOverPulsingObject.transform
                    .DOScale(1.15f, 0.6f).SetEase(Ease.InOutSine).SetLoops(-1, LoopType.Yoyo);
            });
        }
    }

    // ========================
    // RESET
    // ========================
    //public void ResetGame()
    //{
    //    _coloredCount = 0;
    //    _gameOverShown = false;
    //    if (congratsPanel != null) { congratsPanel.transform.DOKill(); congratsPanel.SetActive(false); }
    //    if (gameOverPanel != null) { gameOverPanel.transform.DOKill(); gameOverPanel.SetActive(false); }
    //    if (congratsPulsingObject != null) { congratsPulsingObject.transform.DOKill(); congratsPulsingObject.transform.localScale = Vector3.one; }
    //    if (gameOverPulsingObject != null) { gameOverPulsingObject.transform.DOKill(); gameOverPulsingObject.transform.localScale = Vector3.one; }
    //    foreach (var area in allDropAreas) area.ResetZone();
    //    if (GameManager.Instance.dogController != null)
    //        GameManager.Instance.dogController.PlayIdle();
    //}

    public void ResetGame()
    {
        _coloredCount = 0;
        _gameOverShown = false;
        if (congratsPanel != null) { congratsPanel.transform.DOKill(); congratsPanel.SetActive(false); }
        if (gameOverPanel != null) { gameOverPanel.transform.DOKill(); gameOverPanel.SetActive(false); }
        if (congratsPulsingObject != null) { congratsPulsingObject.transform.DOKill(); congratsPulsingObject.transform.localScale = Vector3.one; }
        if (gameOverPulsingObject != null) { gameOverPulsingObject.transform.DOKill(); gameOverPulsingObject.transform.localScale = Vector3.one; }
        foreach (var area in allDropAreas) area.ResetZone();
        foreach (var btn in FindObjectsOfType<ColorDrag>()) btn.ResetButton();  // ← reset buttons
        if (GameManager.Instance.dogController != null)
            GameManager.Instance.dogController.PlayIdle();
    }

    // ── Button OnClick targets ──────────────────────────
    public void OpenSettings() => UIManager.Instance.ShowPanel(settingsPanel);
    public void CloseSettings() => UIManager.Instance.CloseCurrectPanel();
    public void GoToMainMenu() => UIManager.Instance.LoadScene(0);
}