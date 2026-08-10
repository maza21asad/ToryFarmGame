using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;

public class RepairGameManager : MonoBehaviour
{
    public static RepairGameManager Instance;

    [Header("Repair Slots")]
    public List<RepairSlot> allRepairSlots = new List<RepairSlot>();

    [Header("Congratulation Panel")]
    public GameObject congratsPanel;
    public GameObject congratsPulsingObject;

    [Header("Game Over Panel")]
    public GameObject gameOverPanel;
    public GameObject gameOverPulsingObject;

    [Header("UI Panels")]
    public GameObject settingsPanel;

    private int _repairedCount = 0;
    private bool _gameOverShown = false;

    void Awake() => Instance = this;

    void Start()
    {
        _repairedCount = 0;

        // Auto-populate in case Inspector list is incomplete.
        if (allRepairSlots == null || allRepairSlots.Count == 0)
        {
            allRepairSlots = new List<RepairSlot>(FindObjectsOfType<RepairSlot>());
            Debug.Log($"[RepairGameManager] Auto-found {allRepairSlots.Count} RepairSlots.");
        }

        if (congratsPanel != null) congratsPanel.SetActive(false);
        if (gameOverPanel != null) gameOverPanel.SetActive(false);
        _gameOverShown = false;
    }

    public void OnSlotRepaired()
    {
        _repairedCount++;
        if (allRepairSlots.Count > 0 && _repairedCount >= allRepairSlots.Count)

            ShowCongrats();
        
    }

    // ========================
    // CONGRATS
    // ========================
    //public void ShowCongrats()
    //{
    //    Debug.Log("[RepairGameManager] ShowCongrats triggered!");
    //    SoundManager.Instance.PlaySFX("DogLaugh");
    //    if (congratsPanel != null)
    //    {
    //        if (GameManager.Instance != null)
    //            GameManager.Instance.AnimatePanelFade(congratsPanel);
    //        else
    //            congratsPanel.SetActive(true);
    //        if (SoundManager.Instance != null)
    //            SoundManager.Instance.PlaySFX("Congratulation");
    //    }
    //    if (DogAnimationController.Instance != null)
    //        DogAnimationController.Instance.PlayCongrats();
    //    else if (GameManager.Instance != null && GameManager.Instance.dogController != null)
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
        Debug.Log("[RepairGameManager] ShowCongrats triggered!");

        DOVirtual.DelayedCall(0.5f, () =>
        {
            SoundManager.Instance.PlaySFX("DogLaugh");
            if (congratsPanel != null)
            {
                if (GameManager.Instance != null)
                    GameManager.Instance.AnimatePanelFade(congratsPanel);
                else
                    congratsPanel.SetActive(true);
                if (SoundManager.Instance != null)
                    SoundManager.Instance.PlaySFX("Congratulation");
            }
            if (DogAnimationController.Instance != null)
                DogAnimationController.Instance.PlayCongrats();
            else if (GameManager.Instance != null && GameManager.Instance.dogController != null)
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
        });
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
    public void ResetGame()
    {
        _repairedCount = 0;
        _gameOverShown = false;
        if (congratsPanel != null) { congratsPanel.transform.DOKill(); congratsPanel.SetActive(false); }
        if (gameOverPanel != null) { gameOverPanel.transform.DOKill(); gameOverPanel.SetActive(false); }
        if (congratsPulsingObject != null) { congratsPulsingObject.transform.DOKill(); congratsPulsingObject.transform.localScale = Vector3.one; }
        if (gameOverPulsingObject != null) { gameOverPulsingObject.transform.DOKill(); gameOverPulsingObject.transform.localScale = Vector3.one; }
        foreach (var slot in allRepairSlots) slot.ResetSlot();


        // Re-activate all drag pieces (hidden via SetActive(false) when placed).
        RepairDrag[] allParts = FindObjectsOfType<RepairDrag>(includeInactive: true);
        foreach (var part in allParts) part.gameObject.SetActive(true);

        if (DogAnimationController.Instance != null)
            DogAnimationController.Instance.PlayIdle();
        else if (GameManager.Instance != null && GameManager.Instance.dogController != null)
            GameManager.Instance.dogController.PlayIdle();
    }

    // ── Button OnClick targets ──────────────────────────
    public void OpenSettings() => UIManager.Instance.ShowPanel(settingsPanel);
    public void CloseSettings() => UIManager.Instance.CloseCurrectPanel();
    public void GoToMainMenu() => UIManager.Instance.LoadScene(0);
}