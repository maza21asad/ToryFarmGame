using DG.Tweening;
using UnityEngine;

public class CleaningGameManager : MonoBehaviour
{
    public static CleaningGameManager Instance;

    [Header("Congratulation Panel")]
    public GameObject congratsPanel;
    public GameObject congratsPulsingObject;

    [Header("Game Over Panel")]
    public GameObject gameOverPanel;
    public GameObject gameOverPulsingObject;

    [Header("UI Panels")]
    public GameObject settingsPanel;

    private bool _gameOverShown = false;

    void Awake() => Instance = this;

    void Start()
    {
        if (congratsPanel != null) congratsPanel.SetActive(false);
        if (gameOverPanel != null) gameOverPanel.SetActive(false);
        _gameOverShown = false;
    }

    // ========================
    // CONGRATS
    // ========================
    public void ShowCongrats()
    {
        Debug.Log("[CleaningGameManager] ShowCongrats triggered!");
        if (congratsPanel != null)
        {
            if (GameManager.Instance != null)
                GameManager.Instance.AnimatePanelFade(congratsPanel);
            else
                congratsPanel.SetActive(true);
            if (SoundManager.Instance != null)
                SoundManager.Instance.PlaySFX("Congratulation");
        }
        if (GameManager.Instance != null && GameManager.Instance.dogController != null)
            GameManager.Instance.dogController.PlayCongrats();
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
        if (GameManager.Instance != null)
            GameManager.Instance.AnimatePanelFade(gameOverPanel);
        else
            gameOverPanel.SetActive(true);
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
    public void ResetGame()
    {
        _gameOverShown = false;
        if (congratsPanel != null)
        {
            congratsPanel.transform.DOKill();
            congratsPanel.SetActive(false);
        }
        if (gameOverPanel != null)
        {
            gameOverPanel.transform.DOKill();
            gameOverPanel.SetActive(false);
        }
        if (congratsPulsingObject != null)
        {
            congratsPulsingObject.transform.DOKill();
            congratsPulsingObject.transform.localScale = Vector3.one;
        }
        if (gameOverPulsingObject != null)
        {
            gameOverPulsingObject.transform.DOKill();
            gameOverPulsingObject.transform.localScale = Vector3.one;
        }
        StopAllCoroutines();

        // Reset all zones (sprites, stars, pig, intro flags, cleanedCount).
        foreach (PigPenZone zone in PigPenZone.AllZones)
            if (zone != null) zone.ResetZone();

        // Reset all tools back to their home positions and re-enable them.
        PigPenTool.ResetAll();

        // Dog back to idle.
        if (GameManager.Instance != null && GameManager.Instance.dogController != null)
            GameManager.Instance.dogController.PlayIdle();

        // Restart the intro on the first zone that has a pig assigned.
        foreach (PigPenZone zone in PigPenZone.AllZones)
        {
            if (zone != null && zone.pigObject != null) { zone.StartIntro(); break; }
        }

        Debug.Log("[CleaningGameManager] Cleaning game reset.");
    }

    // ── Button OnClick targets ──────────────────────────
    public void OpenSettings() => UIManager.Instance.ShowPanel(settingsPanel);
    public void CloseSettings() => UIManager.Instance.CloseCurrectPanel();
    public void GoToMainMenu() => UIManager.Instance.LoadScene(0);
}