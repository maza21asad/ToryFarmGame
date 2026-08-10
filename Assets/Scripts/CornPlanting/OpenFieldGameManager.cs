//using DG.Tweening;
//using UnityEngine;

//public class OpenFieldGameManager : MonoBehaviour
//{
//    public static OpenFieldGameManager Instance;

//    [Header("Popup")]
//    public GameObject growPopupPanel;
//    public RectTransform yesButtonRect;
//    public RectTransform noButtonRect;

//    [Header("References")]
//    public CowController cowController;
//    public FieldManager fieldManager;
//    public CowClickArea cowClickArea;

//    [Header("Congratulation Panel")]
//    public GameObject congratsPanel;
//    public GameObject congratsPulsingObject;

//    [Header("Game Over Panel")]
//    public GameObject gameOverPanel;
//    public GameObject gameOverPulsingObject;

//    [Header("UI Panels")]
//    public GameObject settingsPanel;

//    public bool IsGameStarted { get; private set; } = false;
//    private bool _popupOpen = false;
//    private bool _gameOverShown = false;

//    void Awake() => Instance = this;

//    void Start()
//    {
//        if (growPopupPanel != null) growPopupPanel.SetActive(false);
//        if (congratsPanel != null) congratsPanel.SetActive(false);
//        if (gameOverPanel != null) gameOverPanel.SetActive(false);
//        _gameOverShown = false;
//    }

//    void Update()
//    {
//        if (_popupOpen && !IsGameStarted)
//        {
//            if (Input.GetMouseButtonDown(0) || (Input.touchCount > 0 && Input.GetTouch(0).phase == TouchPhase.Began))
//            {
//                Vector2 clickPos = Input.touchCount > 0
//                    ? (Vector2)Input.GetTouch(0).position
//                    : (Vector2)Input.mousePosition;

//                if (yesButtonRect != null &&
//                    RectTransformUtility.RectangleContainsScreenPoint(yesButtonRect, clickPos))
//                {
//                    Debug.Log("[OpenFieldGameManager] YES clicked.");
//                    OnYesClicked();
//                }
//                else if (noButtonRect != null &&
//                         RectTransformUtility.RectangleContainsScreenPoint(noButtonRect, clickPos))
//                {
//                    Debug.Log("[OpenFieldGameManager] NO clicked.");
//                    OnNoClicked();
//                }
//            }
//        }
//    }

//    public void ShowGrowPopup()
//    {
//        if (IsGameStarted) return;
//        if (cowController != null) { cowController.ForceIdle(); cowController.enabled = false; }
//        if (growPopupPanel != null)
//        {
//            growPopupPanel.transform.DOKill(true);
//            growPopupPanel.transform.localScale = Vector3.one * 0.4f;
//            CanvasGroup cg = growPopupPanel.GetComponent<CanvasGroup>();
//            if (cg == null) cg = growPopupPanel.AddComponent<CanvasGroup>();
//            cg.alpha = 0f;
//            growPopupPanel.SetActive(true);
//            growPopupPanel.transform.DOScale(1f, 0.4f).SetEase(Ease.OutBack);
//            cg.DOFade(1f, 0.3f);
//            SoundManager.Instance.PlaySFX("Panelpop");
//        }
//        _popupOpen = true;
//    }

//    public void OnYesClicked()
//    {
//        if (IsGameStarted) return;
//        IsGameStarted = true;
//        _popupOpen = false;
//        if (growPopupPanel != null)
//        {
//            growPopupPanel.transform.DOKill(true);
//            growPopupPanel.transform.DOScale(Vector3.zero, 0.25f).SetEase(Ease.InBack)
//                .OnComplete(() => growPopupPanel.SetActive(false));
//        }
//        if (cowClickArea != null) cowClickArea.enabled = false;
//        if (fieldManager != null) fieldManager.StartGame();
//        else Debug.LogError("[OpenFieldGameManager] fieldManager is not assigned!");
//    }

//    public void OnNoClicked()
//    {
//        _popupOpen = false;
//        if (growPopupPanel != null)
//        {
//            growPopupPanel.transform.DOKill(true);
//            growPopupPanel.transform.DOScale(Vector3.zero, 0.25f).SetEase(Ease.InBack)
//                .OnComplete(() => growPopupPanel.SetActive(false));
//        }
//        if (cowController != null) cowController.enabled = true;
//    }

//    // ========================
//    // CONGRATS
//    // ========================
//    public void ShowCongrats()
//    {
//        if (congratsPanel != null)
//        {
//            GameManager.Instance.AnimatePanelFade(congratsPanel);
//            SoundManager.Instance.PlaySFX("Congratulation");
//        }
//        if (GameManager.Instance.dogController != null)
//            GameManager.Instance.dogController.PlayCongrats();
//        if (congratsPulsingObject != null)
//        {
//            DOVirtual.DelayedCall(0.5f, () =>
//            {
//                if (congratsPulsingObject == null) return;
//                congratsPulsingObject.transform.DOKill();
//                congratsPulsingObject.transform.localScale = Vector3.one;
//                congratsPulsingObject.transform
//                    .DOScale(1.15f, 0.8f).SetEase(Ease.InOutSine).SetLoops(-1, LoopType.Yoyo);
//            });
//        }
//    }

//    // ========================
//    // GAME OVER
//    // ========================
//    public void ShowGameOver()
//    {
//        if (_gameOverShown) return;
//        _gameOverShown = true;
//        if (gameOverPanel == null) return;
//        GameManager.Instance.AnimatePanelFade(gameOverPanel);
//        if (gameOverPulsingObject != null)
//        {
//            DOVirtual.DelayedCall(0.5f, () =>
//            {
//                if (gameOverPulsingObject == null) return;
//                gameOverPulsingObject.transform.DOKill();
//                gameOverPulsingObject.transform.localScale = Vector3.one;
//                gameOverPulsingObject.transform
//                    .DOScale(1.15f, 0.6f).SetEase(Ease.InOutSine).SetLoops(-1, LoopType.Yoyo);
//            });
//        }
//    }

//    // ========================
//    // RESET
//    // ========================
//    public void ResetGame()
//    {
//        IsGameStarted = false;
//        _popupOpen = false;
//        _gameOverShown = false;
//        if (congratsPanel != null) { congratsPanel.transform.DOKill(); congratsPanel.SetActive(false); }
//        if (gameOverPanel != null) { gameOverPanel.transform.DOKill(); gameOverPanel.SetActive(false); }
//        if (congratsPulsingObject != null) { congratsPulsingObject.transform.DOKill(); congratsPulsingObject.transform.localScale = Vector3.one; }
//        if (gameOverPulsingObject != null) { gameOverPulsingObject.transform.DOKill(); gameOverPulsingObject.transform.localScale = Vector3.one; }
//        if (GameManager.Instance.dogController != null)
//            GameManager.Instance.dogController.PlayIdle();
//    }

//    // ── Button OnClick targets ──────────────────────────
//    public void OpenSettings() => UIManager.Instance.ShowPanel(settingsPanel);
//    public void CloseSettings() => UIManager.Instance.CloseCurrectPanel();
//    public void GoToMainMenu() => UIManager.Instance.LoadScene(0);
//}

using DG.Tweening;
using UnityEngine;

public class OpenFieldGameManager : MonoBehaviour
{
    public static OpenFieldGameManager Instance;

    [Header("Popup")]
    public GameObject growPopupPanel;
    public RectTransform yesButtonRect;
    public RectTransform noButtonRect;

    [Header("References")]
    public CowController cowController;
    public FieldManager fieldManager;
    public CowClickArea cowClickArea;

    [Header("Congratulation Panel")]
    public GameObject congratsPanel;
    public GameObject congratsPulsingObject;

    [Header("Game Over Panel")]
    public GameObject gameOverPanel;
    public GameObject gameOverPulsingObject;

    [Header("UI Panels")]
    public GameObject settingsPanel;

    public bool IsGameStarted { get; private set; } = false;
    private bool _popupOpen = false;
    private bool _gameOverShown = false;

    void Awake() => Instance = this;

    void Start()
    {
        if (growPopupPanel != null) growPopupPanel.SetActive(false);
        if (congratsPanel != null) congratsPanel.SetActive(false);
        if (gameOverPanel != null) gameOverPanel.SetActive(false);
        _gameOverShown = false;
    }

    void Update()
    {
        if (_popupOpen && !IsGameStarted)
        {
            if (Input.GetMouseButtonDown(0) || (Input.touchCount > 0 && Input.GetTouch(0).phase == TouchPhase.Began))
            {
                Vector2 clickPos = Input.touchCount > 0
                    ? (Vector2)Input.GetTouch(0).position
                    : (Vector2)Input.mousePosition;

                if (yesButtonRect != null &&
                    RectTransformUtility.RectangleContainsScreenPoint(yesButtonRect, clickPos))
                {
                    Debug.Log("[OpenFieldGameManager] YES clicked.");
                    OnYesClicked();
                }
                else if (noButtonRect != null &&
                         RectTransformUtility.RectangleContainsScreenPoint(noButtonRect, clickPos))
                {
                    Debug.Log("[OpenFieldGameManager] NO clicked.");
                    OnNoClicked();
                }
            }
        }
    }

    public void ShowGrowPopup()
    {
        if (IsGameStarted) return;
        if (cowController != null) { cowController.ForceIdle(); cowController.enabled = false; }
        if (growPopupPanel != null)
        {
            growPopupPanel.transform.DOKill(true);
            growPopupPanel.transform.localScale = Vector3.one * 0.4f;
            CanvasGroup cg = growPopupPanel.GetComponent<CanvasGroup>();
            if (cg == null) cg = growPopupPanel.AddComponent<CanvasGroup>();
            cg.alpha = 0f;
            growPopupPanel.SetActive(true);
            growPopupPanel.transform.DOScale(1f, 0.4f).SetEase(Ease.OutBack);
            cg.DOFade(1f, 0.3f);
            SoundManager.Instance.PlaySFX("Panelpop");
        }
        _popupOpen = true;
    }

    public void OnYesClicked()
    {
        if (IsGameStarted) return;
        IsGameStarted = true;
        _popupOpen = false;

        if (growPopupPanel != null)
        {
            growPopupPanel.transform.DOKill(true);
            growPopupPanel.transform.DOScale(Vector3.zero, 0.25f).SetEase(Ease.InBack)
                .OnComplete(() => growPopupPanel.SetActive(false));
        }

        // ── Re-enable Tori so she can walk freely while planting ──────────
        // We only paused her during the popup; now the game is running and
        // she should be free to roam between fields.
        if (cowController != null) cowController.enabled = true;
        // cowClickArea stays enabled — player taps the BG to walk Tori.

        if (fieldManager != null) fieldManager.StartGame();
        else Debug.LogError("[OpenFieldGameManager] fieldManager is not assigned!");
    }

    public void OnNoClicked()
    {
        _popupOpen = false;
        if (growPopupPanel != null)
        {
            growPopupPanel.transform.DOKill(true);
            growPopupPanel.transform.DOScale(Vector3.zero, 0.25f).SetEase(Ease.InBack)
                .OnComplete(() => growPopupPanel.SetActive(false));
        }
        if (cowController != null) cowController.enabled = true;
    }

    // ========================
    // CONGRATS
    // ========================
public void ShowCongrats()
{
    Debug.Log("[OpenFieldGameManager] ShowCongrats called.");

    if (congratsPanel == null)
    {
        Debug.LogError("[OpenFieldGameManager] congratsPanel is not assigned in the Inspector — cannot show it!");
    }
    else
    {
        // Activate the GameObject ourselves first — AnimatePanelFade only handles
        // the fade-in, it does not flip the panel active (same as growPopupPanel above).
        congratsPanel.SetActive(true);

        if (GameManager.Instance != null)
            GameManager.Instance.AnimatePanelFade(congratsPanel);
        else
            Debug.LogWarning("[OpenFieldGameManager] GameManager.Instance is null — panel shown without fade animation.");

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
        IsGameStarted = false;
        _popupOpen = false;
        _gameOverShown = false;
        if (congratsPanel != null) { congratsPanel.transform.DOKill(); congratsPanel.SetActive(false); }
        if (gameOverPanel != null) { gameOverPanel.transform.DOKill(); gameOverPanel.SetActive(false); }
        if (congratsPulsingObject != null) { congratsPulsingObject.transform.DOKill(); congratsPulsingObject.transform.localScale = Vector3.one; }
        if (gameOverPulsingObject != null) { gameOverPulsingObject.transform.DOKill(); gameOverPulsingObject.transform.localScale = Vector3.one; }
        if (GameManager.Instance.dogController != null)
            GameManager.Instance.dogController.PlayIdle();
    }

    // ── Button OnClick targets ──────────────────────────
    public void OpenSettings() => UIManager.Instance.ShowPanel(settingsPanel);
    public void CloseSettings() => UIManager.Instance.CloseCurrectPanel();
    public void GoToMainMenu() => UIManager.Instance.LoadScene(0);
}