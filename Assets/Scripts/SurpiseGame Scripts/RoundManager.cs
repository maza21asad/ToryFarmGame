using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using TMPro;

public class RoundManager : MonoBehaviour
{
    public static RoundManager Instance;

    // The foodType string of the food currently shown in the thought bubble
    public string CurrentBubbleFoodType { get; private set; }

    // Caches original world positions of all food objects
    private Dictionary<Transform, Vector3> foodOriginalPositions
        = new Dictionary<Transform, Vector3>();

    [Header("Rounds Bowl Configuration")]
    public List<GameObject> round1Bowls;
    public List<GameObject> round2Bowls;
    public List<GameObject> round3Bowls;

    [Header("All Bowl Lid Animations (all 6)")]
    public List<LidButtonAnimation> allLids;

    [Header("All Random Button Visibility Scripts")]
    public List<RandomButtonVisibility> allButtonVisibilities;

    [Header("Panels")]
    public GameObject youWinPanel;
    public GameObject gameOverPanel;

    [Header("Round Text")]
    [Tooltip("Assign the TextMeshPro component that shows the current round")]
    public TMP_Text roundText;

    [Header("All 6 Bowl Root GameObjects")]
    public List<GameObject> allBowls;

    [Header("Thought Bubble")]
    [Tooltip("The empty GameObject inside the thought bubble where food will be spawned")]
    public Transform thoughtBubbleContainer;

    private int currentRound = 1;
    private GameObject currentBubbleFood; // the instantiated non-clickable copy

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    void Start()
    {
        if (youWinPanel != null) youWinPanel.SetActive(false);
        if (gameOverPanel != null) gameOverPanel.SetActive(false);

        StartRound(1);


    }

    public void StartRound(int round)
    {
        currentRound = round;

        // Update round display text with looping bounce animation
        if (roundText != null)
        {
            roundText.text = currentRound.ToString();
            StopCoroutine("LoopBounce");
            StartCoroutine(LoopBounce(roundText.transform));
        }

        // Reset static flags
        RandomFoodButton.isAnyPlaying = false;
        RandomFoodButton.isFoodPending = false;
        LidButtonAnimation.isAnyLidPlaying = false;

        // Hide all bowls
        foreach (var bowl in allBowls)
            if (bowl != null) bowl.SetActive(false);

        // Show only active bowls for this round
        List<GameObject> activeBowls = GetBowlsForRound(round);
        foreach (var bowl in activeBowls)
            if (bowl != null) bowl.SetActive(true);

        // Reset lids
        foreach (var lid in allLids)
            if (lid != null && lid.gameObject.activeInHierarchy)
                lid.ResetLid();

        // Cache original food positions (first time only)
        foreach (var bowl in allBowls)
        {
            if (bowl == null) continue;
            foreach (Transform child in bowl.transform)
            {
                if (child.GetComponent<RandomButtonVisibility>() == null) continue;
                foreach (Transform foodTransform in child)
                {
                    if (!foodOriginalPositions.ContainsKey(foodTransform))
                        foodOriginalPositions[foodTransform] = foodTransform.position;
                }
            }
        }

        // Restore, re-enable and reset all food objects in active bowls
        foreach (var bowl in activeBowls)
        {
            if (bowl == null) continue;
            foreach (Transform child in bowl.transform)
            {
                if (child.GetComponent<RandomButtonVisibility>() == null) continue;
                foreach (Transform foodTransform in child)
                {
                    if (foodOriginalPositions.ContainsKey(foodTransform))
                        foodTransform.position = foodOriginalPositions[foodTransform];

                    foodTransform.gameObject.SetActive(true);

                    RandomFoodButton rfb = foodTransform.GetComponent<RandomFoodButton>();
                    if (rfb != null) rfb.ResetButton();
                }
            }
        }

        // Randomize — hides 2, shows 1 per bowl
        foreach (var vis in allButtonVisibilities)
            if (vis != null && vis.gameObject.activeInHierarchy)
                vis.ShowRandomButton();

        // Update thought bubble with one of the spawned foods
        UpdateThoughtBubble();
        StartCoroutine(FlashThoughtBubble());

        // Notify tutorial manager with the active lid RectTransforms for this round
        if (SurpriseTutorialManager.Instance != null)
        {
            List<RectTransform> lidRects = new List<RectTransform>();
            foreach (var lid in allLids)
                if (lid != null && lid.gameObject.activeInHierarchy)
                    lidRects.Add(lid.GetComponent<RectTransform>());
            SurpriseTutorialManager.Instance.OnRoundStart(round, lidRects);
        }
    }

    void UpdateThoughtBubble()
    {
        // Collect all selected (spawned) food buttons from active bowls
        List<RandomFoodButton> spawnedFoods = new List<RandomFoodButton>();
        List<RandomFoodButton> allSpawned = new List<RandomFoodButton>();

        foreach (var vis in allButtonVisibilities)
        {
            if (vis == null || !vis.gameObject.activeInHierarchy) continue;
            if (vis.SelectedButton == null) continue;

            RandomFoodButton rfb = vis.SelectedButton.GetComponent<RandomFoodButton>();
            if (rfb == null) continue;

            allSpawned.Add(rfb);
            if (!rfb.isSpicy) // prefer non-spicy in bubble
                spawnedFoods.Add(rfb);
        }

        // Fallback: if every spawned food this round happens to be spicy,
        // still pick from all spawned foods so the bubble never spawns nothing
        if (spawnedFoods.Count == 0)
            spawnedFoods = allSpawned;

        if (spawnedFoods.Count == 0) return;

        // Pick one randomly, but skip any candidate missing a foodObject visual
        // (retry instead of giving up, so the bubble doesn't end up empty)
        List<RandomFoodButton> candidates = new List<RandomFoodButton>(spawnedFoods);
        RandomFoodButton chosen = null;
        while (candidates.Count > 0)
        {
            int idx = Random.Range(0, candidates.Count);
            if (candidates[idx].foodObject != null)
            {
                chosen = candidates[idx];
                break;
            }
            candidates.RemoveAt(idx);
        }

        if (chosen == null) return; // no spawned food has a foodObject visual assigned

        CurrentBubbleFoodType = chosen.foodType;

        // Clear old bubble food and spawn just the food visual (no scripts)
        if (thoughtBubbleContainer != null)
        {
            for (int i = thoughtBubbleContainer.childCount - 1; i >= 0; i--)
                Destroy(thoughtBubbleContainer.GetChild(i).gameObject);

            GameObject foodCopy = Instantiate(chosen.foodObject.gameObject, thoughtBubbleContainer);
            foodCopy.transform.localPosition = Vector3.zero;
            foodCopy.transform.localScale = new Vector3(0.75f, 1.2f, 1f);
            foodCopy.SetActive(true);
            currentBubbleFood = foodCopy;
        }
    }

    private List<GameObject> GetBowlsForRound(int round)
    {
        switch (round)
        {
            case 1: return round1Bowls;
            case 2: return round2Bowls;
            case 3: return round3Bowls;
            default: return new List<GameObject>();
        }
    }

    public void TriggerGameOver()
    {
        Time.timeScale = 0f;
        if (gameOverPanel != null)
            StartCoroutine(PopupShow(gameOverPanel));
    }

    // Called immediately after a correct food click
    public void RoundComplete()
    {
        Time.timeScale = 0f;
        if (youWinPanel != null)
        {
            SoundManager.Instance.PlaySFX("lvldone");
            StartCoroutine(PopupShow(youWinPanel));
        }
    }

    IEnumerator PopupShow(GameObject panel)
    {
        panel.SetActive(true);
        RectTransform rect = panel.GetComponent<RectTransform>();
        rect.localScale = Vector3.zero;

        float duration = 0.8f;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            float scale = EaseOutElastic(t);
            rect.localScale = new Vector3(scale, scale, 1f);
            yield return null;
        }

        rect.localScale = Vector3.one;
    }

    IEnumerator FlashThoughtBubble()
    {
        if (thoughtBubbleContainer == null) yield break;

        // Get the parent of the container (the full thought bubble GameObject)
        Transform bubble = thoughtBubbleContainer.parent != null
            ? thoughtBubbleContainer.parent
            : thoughtBubbleContainer;

        CanvasGroup cg = bubble.GetComponent<CanvasGroup>();
        if (cg == null) cg = bubble.gameObject.AddComponent<CanvasGroup>();

        int flashes = 3;
        float flashSpeed = 0.1f;

        for (int i = 0; i < flashes; i++)
        {
            cg.alpha = 0f;
            yield return new WaitForSeconds(flashSpeed);
            cg.alpha = 1f;
            yield return new WaitForSeconds(flashSpeed);
        }

        cg.alpha = 1f;
    }

    float EaseOutElastic(float t)
    {
        if (t == 0f) return 0f;
        if (t == 1f) return 1f;
        float c4 = (2f * Mathf.PI) / 3f;
        return Mathf.Pow(2f, -10f * t) * Mathf.Sin((t * 10f - 0.75f) * c4) + 1f;
    }

    IEnumerator LoopBounce(Transform target)
    {
        Vector3 originalScale = target.localScale;
        float speed = 3f;      // how fast it bounces
        float amount = 0.15f;  // how much it grows

        while (true)
        {
            float scale = 1f + Mathf.Abs(Mathf.Sin(Time.unscaledTime * speed)) * amount;
            target.localScale = originalScale * scale;
            yield return null;
        }
    }

    public void OnNextButtonPressed()
    {
        Time.timeScale = 1f;
        if (youWinPanel != null) youWinPanel.SetActive(false);

        if (currentRound < 3)
        {
            StartRound(currentRound + 1);
        }
        else
        {
            Application.Quit();
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#endif
        }
    }

    public void OnRetryPressed()
    {
        Time.timeScale = 1f;
        if (gameOverPanel != null) gameOverPanel.SetActive(false);
        StartRound(1);
    }
}