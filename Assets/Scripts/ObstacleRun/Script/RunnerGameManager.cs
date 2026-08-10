////using System.Collections;
////using DG.Tweening;
////using TMPro;
////using UnityEngine;
////using UnityEngine.SceneManagement;

////public class RunnerGameManager : MonoBehaviour
////{
////    public static RunnerGameManager Instance;

////    [Header("Runner Game")]
////    public Cowanimationcontroller cow;
////    public GameObject startButton;
////    public GameObject instruction;
////    public GameObject startButtonPulsingObject;
////    public TMP_Text countdownText;

////    [Header("Congratulation Panel")]
////    public GameObject congratsPanel;
////    public GameObject congratsPulsingObject;

////    [Header("Game Over Panel")]
////    public GameObject gameOverPanel;
////    public GameObject gameOverPulsingObject;

////    [Header("UI Panels")]
////    public GameObject settingsPanel;

////    private bool _gameOverShown = false;

////    void Awake() => Instance = this;

////    void Start()
////    {
////        if (startButtonPulsingObject != null)
////            startButtonPulsingObject.SetActive(true);
////        if (congratsPanel != null) congratsPanel.SetActive(false);
////        if (gameOverPanel != null) gameOverPanel.SetActive(false);
////        _gameOverShown = false;
////    }

////    public void StartGame()
////    {
////        if (startButton != null) startButton.SetActive(false);
////        if (instruction != null) instruction.SetActive(false);
////        StartCoroutine(CountdownRoutine());
////    }

////    IEnumerator CountdownRoutine()
////    {
////        countdownText.gameObject.SetActive(true);
////        countdownText.text = "3"; SoundManager.Instance.PlaySFX("Start"); yield return new WaitForSeconds(1f);
////        countdownText.text = "2"; yield return new WaitForSeconds(1f);
////        countdownText.text = "1"; yield return new WaitForSeconds(1f);
////        countdownText.text = "GO!!"; SoundManager.Instance.PlaySFX("go"); yield return new WaitForSeconds(1f);
////        countdownText.gameObject.SetActive(false);
////        cow.StartRunning();
////    }

////    // ========================
////    // JUMP
////    // ========================
////    public void OnJump()
////    {
////        SoundManager.Instance.PlaySFX("Jump");
////    }

////    // ========================
////    // PANEL FADE (local, no GameManager dependency)
////    // ========================
////    private void AnimatePanel(GameObject panel)
////    {
////        if (panel == null) return;

////        panel.SetActive(true);

////        CanvasGroup cg = panel.GetComponent<CanvasGroup>();
////        if (cg == null) cg = panel.AddComponent<CanvasGroup>();

////        cg.DOKill();
////        cg.alpha = 0f;
////        cg.interactable = true;
////        cg.blocksRaycasts = true;
////        cg.DOFade(1f, 0.4f).SetEase(Ease.OutSine);
////    }

////    // ========================
////    // CONGRATS
////    // ========================
////    public void ShowCongrats()
////    {
////        if (congratsPanel != null)
////        {
////            AnimatePanel(congratsPanel);

////            SoundManager.Instance.PlaySFX("Congratulation");
////            SoundManager.Instance.PlaySFX("Vitory");
////        }
////        if (GameManager.Instance != null && GameManager.Instance.dogController != null)
////            GameManager.Instance.dogController.PlayCongrats();
////        if (congratsPulsingObject != null)
////        {
////            DOVirtual.DelayedCall(0.5f, () =>
////            {
////                if (congratsPulsingObject == null) return;
////                congratsPulsingObject.transform.DOKill();
////                congratsPulsingObject.transform.localScale = Vector3.one;
////                congratsPulsingObject.transform
////                    .DOScale(1.15f, 0.8f).SetEase(Ease.InOutSine).SetLoops(-1, LoopType.Yoyo);
////            });
////        }
////    }

////    // ========================
////    // GAME OVER
////    // ========================
////    public void ShowGameOver()
////    {
////        if (_gameOverShown) return;
////        _gameOverShown = true;
////        if (gameOverPanel == null) return;

////        AnimatePanel(gameOverPanel);

////        SoundManager.Instance.PlaySFX("Lose");
////        if (gameOverPulsingObject != null)
////        {
////            DOVirtual.DelayedCall(0.5f, () =>
////            {
////                if (gameOverPulsingObject == null) return;
////                gameOverPulsingObject.transform.DOKill();
////                gameOverPulsingObject.transform.localScale = Vector3.one;
////                gameOverPulsingObject.transform
////                    .DOScale(1.15f, 0.6f).SetEase(Ease.InOutSine).SetLoops(-1, LoopType.Yoyo);
////            });
////        }
////    }

////    // ========================
////    // RESET
////    // ========================
////    public void ResetGame()
////    {
////        _gameOverShown = false;
////        if (congratsPanel != null)
////        {
////            congratsPanel.transform.DOKill();
////            CanvasGroup cg = congratsPanel.GetComponent<CanvasGroup>();
////            if (cg != null) cg.DOKill();
////            congratsPanel.SetActive(false);
////        }
////        if (gameOverPanel != null)
////        {
////            gameOverPanel.transform.DOKill();
////            CanvasGroup cg = gameOverPanel.GetComponent<CanvasGroup>();
////            if (cg != null) cg.DOKill();
////            gameOverPanel.SetActive(false);
////        }
////        if (congratsPulsingObject != null) { congratsPulsingObject.transform.DOKill(); congratsPulsingObject.transform.localScale = Vector3.one; }
////        if (gameOverPulsingObject != null) { gameOverPulsingObject.transform.DOKill(); gameOverPulsingObject.transform.localScale = Vector3.one; }
////        if (GameManager.Instance != null && GameManager.Instance.dogController != null)
////            GameManager.Instance.dogController.PlayIdle();
////    }

////    // ── Button OnClick targets ──────────────────────────
////    public void OpenSettings() => UIManager.Instance.ShowPanel(settingsPanel);
////    public void CloseSettings() => UIManager.Instance.CloseCurrectPanel();
////    public void GoToMainMenu() => UIManager.Instance.LoadScene(0);

////    public void RestartGame()
////    {
////        Time.timeScale = 1f;
////        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
////    }
////}

//using System.Collections;
//using DG.Tweening;
//using TMPro;
//using UnityEngine;
//using UnityEngine.SceneManagement;

//public class RunnerGameManager : MonoBehaviour
//{
//    public static RunnerGameManager Instance;

//    [Header("Runner Game")]
//    public Cowanimationcontroller cow;
//    public GameObject startButton;
//    public GameObject instruction;
//    public GameObject startButtonPulsingObject;
//    public TMP_Text countdownText;

//    [Header("Congratulation Panel")]
//    public GameObject congratsPanel;
//    public GameObject congratsPulsingObject;

//    [Header("Game Over Panel")]
//    public GameObject gameOverPanel;
//    public GameObject gameOverPulsingObject;

//    [Header("UI Panels")]
//    public GameObject settingsPanel;

//    private bool _gameOverShown = false;
//    private Coroutine _countdownCo;

//    void Awake() => Instance = this;

//    void Start()
//    {
//        if (startButtonPulsingObject != null)
//            startButtonPulsingObject.SetActive(true);
//        if (congratsPanel != null) congratsPanel.SetActive(false);
//        if (gameOverPanel != null) gameOverPanel.SetActive(false);
//        _gameOverShown = false;
//    }

//    public void StartGame()
//    {
//        if (startButton != null) startButton.SetActive(false);
//        if (instruction != null) instruction.SetActive(false);
//        _countdownCo = StartCoroutine(CountdownRoutine());
//    }

//    IEnumerator CountdownRoutine()
//    {
//        countdownText.gameObject.SetActive(true);
//        countdownText.text = "3"; SoundManager.Instance.PlaySFX("Start"); yield return new WaitForSeconds(1f);
//        countdownText.text = "2"; yield return new WaitForSeconds(1f);
//        countdownText.text = "1"; yield return new WaitForSeconds(1f);
//        countdownText.text = "GO!!"; SoundManager.Instance.PlaySFX("go"); yield return new WaitForSeconds(1f);
//        countdownText.gameObject.SetActive(false);
//        cow.StartRunning();
//        _countdownCo = null;
//    }

//    // ========================
//    // JUMP
//    // ========================
//    public void OnJump()
//    {
//        SoundManager.Instance.PlaySFX("Jump");
//    }

//    // ========================
//    // PANEL FADE (local, no GameManager dependency)
//    // ========================
//    private void AnimatePanel(GameObject panel)
//    {
//        if (panel == null) return;

//        panel.SetActive(true);

//        CanvasGroup cg = panel.GetComponent<CanvasGroup>();
//        if (cg == null) cg = panel.AddComponent<CanvasGroup>();

//        cg.DOKill();
//        cg.alpha = 0f;
//        cg.interactable = true;
//        cg.blocksRaycasts = true;
//        cg.DOFade(1f, 0.4f).SetEase(Ease.OutSine);
//    }

//    // ========================
//    // CONGRATS
//    // ========================
//    public void ShowCongrats()
//    {
//        if (congratsPanel != null)
//        {
//            AnimatePanel(congratsPanel);

//            SoundManager.Instance.PlaySFX("Congratulation");
//            SoundManager.Instance.PlaySFX("Vitory");
//        }
//        if (GameManager.Instance != null && GameManager.Instance.dogController != null)
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

//        AnimatePanel(gameOverPanel);

//        SoundManager.Instance.PlaySFX("Lose");
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
//        _gameOverShown = false;
//        if (congratsPanel != null)
//        {
//            congratsPanel.transform.DOKill();
//            CanvasGroup cg = congratsPanel.GetComponent<CanvasGroup>();
//            if (cg != null) cg.DOKill();
//            congratsPanel.SetActive(false);
//        }
//        if (gameOverPanel != null)
//        {
//            gameOverPanel.transform.DOKill();
//            CanvasGroup cg = gameOverPanel.GetComponent<CanvasGroup>();
//            if (cg != null) cg.DOKill();
//            gameOverPanel.SetActive(false);
//        }
//        if (congratsPulsingObject != null) { congratsPulsingObject.transform.DOKill(); congratsPulsingObject.transform.localScale = Vector3.one; }
//        if (gameOverPulsingObject != null) { gameOverPulsingObject.transform.DOKill(); gameOverPulsingObject.transform.localScale = Vector3.one; }
//        if (GameManager.Instance != null && GameManager.Instance.dogController != null)
//            GameManager.Instance.dogController.PlayIdle();
//    }

//    // ── Button OnClick targets ──────────────────────────
//    public void OpenSettings() => UIManager.Instance.ShowPanel(settingsPanel);
//    public void CloseSettings() => UIManager.Instance.CloseCurrectPanel();
//    public void GoToMainMenu()
//    {
//        if (_countdownCo != null)
//        {
//            StopCoroutine(_countdownCo);
//            _countdownCo = null;
//        }
//        if (countdownText != null)
//            countdownText.gameObject.SetActive(false);

//        UIManager.Instance.LoadScene(0);
//    }

//    public void RestartGame()
//    {
//        Time.timeScale = 1f;
//        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
//    }
//}

using System.Collections;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

public class RunnerGameManager : MonoBehaviour
{
    public static RunnerGameManager Instance;

    [Header("Runner Game")]
    public Cowanimationcontroller cow;
    public GameObject startButton;
    public GameObject instruction;
    public GameObject startButtonPulsingObject;
    public TMP_Text countdownText;

    [Header("Congratulation Panel")]
    public GameObject congratsPanel;
    public GameObject congratsPulsingObject;

    [Header("Game Over Panel")]
    public GameObject gameOverPanel;
    public GameObject gameOverPulsingObject;

    [Header("UI Panels")]
    public GameObject settingsPanel;

    private bool _gameOverShown = false;
    private Coroutine _countdownCo;

    void Awake() => Instance = this;

    void Start()
    {
        if (startButtonPulsingObject != null)
            startButtonPulsingObject.SetActive(true);
        if (congratsPanel != null) congratsPanel.SetActive(false);
        if (gameOverPanel != null) gameOverPanel.SetActive(false);
        _gameOverShown = false;
    }

    public void StartGame()
    {
        if (startButton != null) startButton.SetActive(false);
        if (instruction != null) instruction.SetActive(false);
        _countdownCo = StartCoroutine(CountdownRoutine());
    }

    IEnumerator CountdownRoutine()
    {
        countdownText.gameObject.SetActive(true);
        countdownText.text = "3"; SoundManager.Instance.PlaySFX("Start"); yield return new WaitForSeconds(1f);
        countdownText.text = "2"; yield return new WaitForSeconds(1f);
        countdownText.text = "1"; yield return new WaitForSeconds(1f);
        countdownText.text = "GO!!"; SoundManager.Instance.PlaySFX("go"); yield return new WaitForSeconds(1f);
        countdownText.gameObject.SetActive(false);
        cow.StartRunning();
        _countdownCo = null;
    }

    // ========================
    // JUMP
    // ========================
    public void OnJump()
    {
        SoundManager.Instance.PlaySFX("Jump");
    }

    // ========================
    // PANEL FADE (local, no GameManager dependency)
    // ========================
    private void AnimatePanel(GameObject panel)
    {
        if (panel == null) return;

        panel.SetActive(true);

        CanvasGroup cg = panel.GetComponent<CanvasGroup>();
        if (cg == null) cg = panel.AddComponent<CanvasGroup>();

        cg.DOKill();
        cg.alpha = 0f;
        cg.interactable = true;
        cg.blocksRaycasts = true;
        cg.DOFade(1f, 0.4f).SetEase(Ease.OutSine);
    }

    // ========================
    // CONGRATS
    // ========================
    public void ShowCongrats()
    {
        if (congratsPanel != null)
        {
            AnimatePanel(congratsPanel);

            SoundManager.Instance.PlaySFX("Congratulation");
            SoundManager.Instance.PlaySFX("Vitory");
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

        AnimatePanel(gameOverPanel);

        SoundManager.Instance.PlaySFX("Lose");
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
            CanvasGroup cg = congratsPanel.GetComponent<CanvasGroup>();
            if (cg != null) cg.DOKill();
            congratsPanel.SetActive(false);
        }
        if (gameOverPanel != null)
        {
            gameOverPanel.transform.DOKill();
            CanvasGroup cg = gameOverPanel.GetComponent<CanvasGroup>();
            if (cg != null) cg.DOKill();
            gameOverPanel.SetActive(false);
        }
        if (congratsPulsingObject != null) { congratsPulsingObject.transform.DOKill(); congratsPulsingObject.transform.localScale = Vector3.one; }
        if (gameOverPulsingObject != null) { gameOverPulsingObject.transform.DOKill(); gameOverPulsingObject.transform.localScale = Vector3.one; }
        if (GameManager.Instance != null && GameManager.Instance.dogController != null)
            GameManager.Instance.dogController.PlayIdle();
    }

    // ── Button OnClick targets ──────────────────────────
    public void OpenSettings() => UIManager.Instance.ShowPanel(settingsPanel);
    public void CloseSettings() => UIManager.Instance.CloseCurrectPanel();
    public void GoToMainMenu()
    {
        if (_countdownCo != null)
        {
            StopCoroutine(_countdownCo);
            _countdownCo = null;
        }
        if (countdownText != null)
            countdownText.gameObject.SetActive(false);

        SoundManager.Instance.StopAllOneShotSFX();

        UIManager.Instance.LoadScene(0);
    }

    public void RestartGame()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }
}