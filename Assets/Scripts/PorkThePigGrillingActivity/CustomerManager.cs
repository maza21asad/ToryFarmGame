////using System.Collections;
////using System.Collections.Generic;
////using UnityEngine;
////using UnityEngine.UI;
////using TMPro;
////using DG.Tweening;

/////// <summary>
/////// Singleton — manages 3 customers cycling through 2 fixed scene slots.
///////
/////// SETUP
///////   1. Attach to ONE empty GameObject in the scene.
///////   2. Drag all three CustomerController GameObjects into customer1/2/3.
///////   3. Drag the two scene target RectTransforms into targetPoint1/targetPoint2.
///////   4. Drag the two PlateControllers into plate1/plate2 (matched to target points).
///////   5. Drag the two off-screen spawn RectTransforms into spawnPoint1/spawnPoint2.
///////   6. Create an intro panel with a pig animation and an OK button.
///////      Drag the panel into introPanel and wire the OK button OnClick → StartGame().
///////
/////// FLOW
///////   On Start: the intro panel is shown (pig says "Will you help me cook?").
///////   Player presses OK → StartGame() → intro hides → customers begin spawning.
///////   Only two customers are ever on-screen at once; the third waits in the pool.
/////// </summary>
////[DefaultExecutionOrder(-50)]
////public class CustomerManager : MonoBehaviour
////{
////    // ── Singleton ─────────────────────────────────────────────────────────────

////    public static CustomerManager Instance { get; private set; }

////    // ── Inspector ─────────────────────────────────────────────────────────────

////    [Header("Intro Panel")]
////    [Tooltip("Panel with the pig animation and OK button. Set ACTIVE in the Editor.")]
////    public GameObject introPanel;

////    [Tooltip("The Chathead RectTransform inside IntroPanel — plays a looping zoom pulse.")]
////    public RectTransform introChathead;

////    [Header("Customers  (3 total — any can occupy either slot)")]
////    public CustomerController customer1;
////    public CustomerController customer2;
////    public CustomerController customer3;

////    [Header("Scene Slots  (2 slots — target points + matching plates)")]
////    [Tooltip("Counter position for slot 0.")]
////    public RectTransform targetPoint1;
////    [Tooltip("Counter position for slot 1.")]
////    public RectTransform targetPoint2;

////    [Tooltip("Plate that belongs to slot 0 (targetPoint1 side).")]
////    public PlateController plate1;
////    [Tooltip("Plate that belongs to slot 1 (targetPoint2 side).")]
////    public PlateController plate2;

////    [Tooltip("Off-screen spawn point for slot 0.")]
////    public RectTransform spawnPoint1;
////    [Tooltip("Off-screen spawn point for slot 1.")]
////    public RectTransform spawnPoint2;

////    [Header("Spawn Timing  (seconds)")]
////    [Tooltip("Delay before the very first customer walks in after OK is pressed.")]
////    public float firstSpawnDelay = 0.5f;
////    [Tooltip("Delay between the first and second customer at game start.")]
////    public float secondSpawnDelay = 2f;
////    [Tooltip("Delay after a customer leaves before the next one walks in.")]
////    public float nextCustomerDelay = 1.5f;

////    [Header("Mouth Detection")]
////    [Tooltip("Screen-pixel radius for mouth-drop detection.")]
////    public float mouthDetectionRadius = 90f;

////    [Header("Task System")]
////    [Tooltip("Text showing progress e.g. 2/7. Assign a TMP_Text in the Inspector.")]
////    public TMP_Text taskCounterText;
////    [Tooltip("Panel shown when all customers are fed. Set INACTIVE in Editor.")]
////    public GameObject congratsPanel;
////    [Tooltip("Restart button inside the congrats panel.")]
////    public Button restartButton;
////    [Tooltip("Min customers to feed per round.")]
////    public int taskMin = 5;
////    [Tooltip("Max customers to feed per round.")]
////    public int taskMax = 10;
////    [Tooltip("Delay after the last customer is served before the congrats panel appears.")]
////    public float congratsDelay = 0.5f;

////    // ── Private ───────────────────────────────────────────────────────────────

////    private int _taskGoal;
////    private int _fedCount;
////    private bool _taskComplete;

////    private List<CustomerController> _pool = new List<CustomerController>();
////    private List<CustomerController> _waiting = new List<CustomerController>();
////    private CustomerController[] _slotOccupant = new CustomerController[2];
////    private readonly List<CustomerController> _active = new List<CustomerController>();
////    private bool _gameStarted = false;

////    // ── Unity ─────────────────────────────────────────────────────────────────

////    private void Awake()
////    {
////        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
////        Instance = this;

////        // Start panel at zero scale — DOTween zoom-in plays from Start().
////        if (introPanel != null)
////            introPanel.transform.localScale = Vector3.zero;

////        if (restartButton != null) restartButton.onClick.AddListener(OnRestartClicked);
////    }

////    private void Start()
////    {
////        // Build pool
////        if (customer1 != null) _pool.Add(customer1);
////        if (customer2 != null) _pool.Add(customer2);
////        if (customer3 != null) _pool.Add(customer3);

////        // Hide all customers — manager controls visibility
////        foreach (var c in _pool)
////            c.gameObject.SetActive(false);

////        Shuffle(_pool);
////        _waiting.AddRange(_pool);

////        InitTask();
////        if (congratsPanel != null) congratsPanel.SetActive(false);

////        // Zoom the intro panel in from zero on start.
////        if (introPanel != null)
////        {
////            // Disable child CanvasGroups before activation so nothing blocks the OK button.
////            foreach (CanvasGroup child in introPanel.GetComponentsInChildren<CanvasGroup>(true))
////            {
////                if (child.gameObject == introPanel) continue;
////                child.blocksRaycasts = false;
////                child.interactable = false;
////            }

////            introPanel.SetActive(true);
////            introPanel.transform.localScale = Vector3.zero;

////            introPanel.transform.DOScale(Vector3.one, 0.3f)
////                .SetEase(Ease.OutBack)
////                .SetDelay(1f)
////                .SetUpdate(true)
////                .OnComplete(() =>
////                {
////                    CanvasGroup cg = introPanel.GetComponent<CanvasGroup>();
////                    if (cg != null) { cg.alpha = 1f; cg.interactable = true; cg.blocksRaycasts = true; }
////                    PlayChatheadZoom();
////                });
////        }
////    }

////    // ── Registration (for mouth detection) ────────────────────────────────────

////    public void Register(CustomerController c)
////    {
////        if (c != null && !_active.Contains(c)) _active.Add(c);
////    }

////    public void Unregister(CustomerController c) => _active.Remove(c);

////    // ── OK button target ──────────────────────────────────────────────────────

////    /// <summary>
////    /// Wire the intro panel's OK button OnClick → this method.
////    /// Hides the intro and starts spawning customers.
////    /// </summary>
////    public void StartGame()
////    {
////        if (_gameStarted) return;
////        _gameStarted = true;

////        // Stop the chathead zoom loop before closing the panel
////        if (introChathead != null)
////        {
////            introChathead.DOKill();
////            introChathead.localScale = Vector3.one;
////        }

////        // Close intro panel
////        if (introPanel != null)
////            StartCoroutine(IntroPanelExitRoutine());

////        StartCoroutine(InitialSpawnRoutine());
////        Debug.Log("BUTTON CLICKED");
////    }

////    // ── Initial spawn ─────────────────────────────────────────────────────────

////    private IEnumerator InitialSpawnRoutine()
////    {
////        yield return new WaitForSeconds(firstSpawnDelay);
////        SpawnIntoSlot(0);

////        yield return new WaitForSeconds(secondSpawnDelay);
////        SpawnIntoSlot(1);
////    }

////    // ── Task: called immediately when a customer's last order is fulfilled ─────

////    private void OnCustomerServed(CustomerController c)
////    {
////        c.OnServed -= OnCustomerServed;

////        _fedCount++;
////        UpdateTaskUI();

////        if (!_taskComplete && _fedCount >= _taskGoal)
////        {
////            _taskComplete = true;
////            Invoke(nameof(ShowCongratsPanel), congratsDelay);
////        }
////    }

////    // ── Called by CustomerController when it finishes walking off-screen ───────

////    public void OnCustomerLeft(CustomerController c)
////    {
////        c.OnLeft -= OnCustomerLeft;

////        int freedSlot = -1;
////        for (int i = 0; i < _slotOccupant.Length; i++)
////        {
////            if (_slotOccupant[i] != c) continue;
////            _slotOccupant[i] = null;
////            freedSlot = i;
////            break;
////        }

////        if (!_waiting.Contains(c)) _waiting.Add(c);

////        if (_taskComplete) return; // task done — no more spawning

////        if (freedSlot >= 0)
////            StartCoroutine(DelayedSpawnIntoSlot(freedSlot, nextCustomerDelay));
////    }

////    private IEnumerator DelayedSpawnIntoSlot(int slotIndex, float delay)
////    {
////        yield return new WaitForSeconds(delay);
////        SpawnIntoSlot(slotIndex);
////    }

////    // ── Spawn logic ───────────────────────────────────────────────────────────

////    private void SpawnIntoSlot(int slotIndex)
////    {
////        if (_waiting.Count == 0) return;
////        if (slotIndex < 0 || slotIndex >= _slotOccupant.Length) return;
////        if (_slotOccupant[slotIndex] != null) return;

////        RectTransform targetPt = slotIndex == 0 ? targetPoint1 : targetPoint2;
////        RectTransform spawnPt = slotIndex == 0 ? spawnPoint1 : spawnPoint2;
////        PlateController plate = slotIndex == 0 ? plate1 : plate2;

////        if (targetPt == null || spawnPt == null)
////        {
////            Debug.LogWarning($"[CustomerManager] Slot {slotIndex}: target or spawn point not assigned!");
////            return;
////        }

////        int idx = Random.Range(0, _waiting.Count);
////        CustomerController next = _waiting[idx];
////        _waiting.RemoveAt(idx);

////        _slotOccupant[slotIndex] = next;
////        next.owningPlate = plate;
////        next.OnServed += OnCustomerServed;
////        next.OnLeft += OnCustomerLeft;

////        next.gameObject.SetActive(true);
////        ApplySlotSortOrder(slotIndex, next);
////        next.StartWalking(spawnPt, targetPt);

////        Debug.Log($"[CustomerManager] '{next.name}' → slot {slotIndex} (plate: {plate?.plateLabel ?? "none"}).");
////    }

////    /// <summary>
////    /// Ensures the back slot (slot 1) always renders behind the front slot
////    /// (slot 0), regardless of which character (Tori/WhiteCow/Horse) occupies
////    /// which slot. Only re-orders the two customer GameObjects relative to
////    /// each other — their position among other siblings is preserved.
////    /// </summary>
////    private void ApplySlotSortOrder(int slotIndex, CustomerController customer)
////    {
////        Transform t = customer.transform;

////        if (slotIndex == 0)
////        {
////            // Front slot — render above the back-slot occupant if present.
////            CustomerController other = _slotOccupant[1];
////            if (other != null && other != customer && other.transform.parent == t.parent)
////                t.SetSiblingIndex(other.transform.GetSiblingIndex() + 1);
////            else
////                t.SetAsLastSibling();
////        }
////        else
////        {
////            // Back slot — render below the front-slot occupant if present.
////            CustomerController other = _slotOccupant[0];
////            if (other != null && other != customer && other.transform.parent == t.parent)
////            {
////                int otherIndex = other.transform.GetSiblingIndex();
////                int myIndex = t.GetSiblingIndex();
////                t.SetSiblingIndex(myIndex < otherIndex ? myIndex : otherIndex);
////            }
////            else
////                t.SetAsFirstSibling();
////        }
////    }

////    // ── Mouth zone proximity lookup ────────────────────────────────────────────

////    public (CustomerController customer, float dist) GetNearestMouth(Vector2 screenPos, Camera cam)
////    {
////        CustomerController best = null;
////        float bestDist = mouthDetectionRadius;

////        foreach (CustomerController c in _active)
////        {
////            if (c == null || !c.HasArrived || c.mouthPoint == null) continue;

////            Vector2 mouthScreen = RectTransformUtility.WorldToScreenPoint(cam, c.mouthPoint.position);
////            float dist = Vector2.Distance(screenPos, mouthScreen);

////            if (dist < bestDist) { bestDist = dist; best = c; }
////        }

////        return (best, bestDist);
////    }

////    // ── Intro Chathead Zoom ───────────────────────────────────────────────────

////    /// <summary>
////    /// Loops a subtle zoom-in → zoom-out pulse on the intro panel's chathead
////    /// using DOTween. Killed automatically when the player presses OK.
////    /// </summary>
////    private void PlayChatheadZoom()
////    {
////        if (introChathead == null) return;

////        introChathead.localScale = Vector3.one;

////        // Zoom in to 1.12, then zoom back to 1.0, repeat forever.
////        introChathead.DOScale(1.12f, 0.45f)
////            .SetEase(Ease.OutQuad)
////            .SetLoops(-1, LoopType.Yoyo)
////            .SetUpdate(true); // runs even if Time.timeScale = 0
////    }

////    // ── Intro Panel Animations ───────────────────────────────────────────────

////    /// <summary>DOTween zoom-out — scales panel to zero then deactivates it.</summary>
////    private IEnumerator IntroPanelExitRoutine()
////    {
////        CanvasGroup cg = introPanel.GetComponent<CanvasGroup>();
////        if (cg != null) { cg.interactable = false; cg.blocksRaycasts = false; }

////        bool done = false;
////        introPanel.transform.DOScale(Vector3.zero, 0.3f)
////            .SetEase(Ease.InBack)
////            .SetUpdate(true)
////            .OnComplete(() => { introPanel.SetActive(false); done = true; });
////        SoundManager.Instance.PlaySFX("Button");

////        yield return new WaitUntil(() => done);
////    }

////    // ── Utility ───────────────────────────────────────────────────────────────

////    private static void Shuffle<T>(List<T> list)
////    {
////        for (int i = list.Count - 1; i > 0; i--)
////        {
////            int j = Random.Range(0, i + 1);
////            (list[i], list[j]) = (list[j], list[i]);
////        }
////    }

////    public int WalkingCount { get; private set; }
////    public void OnCustomerStartWalk() => WalkingCount++;
////    public void OnCustomerStopWalk() => WalkingCount = Mathf.Max(0, WalkingCount - 1);

////    // ── Task System ───────────────────────────────────────────────────────────

////    private void InitTask()
////    {
////        _taskGoal = Random.Range(taskMin, taskMax + 1);
////        _fedCount = 0;
////        _taskComplete = false;
////        UpdateTaskUI();
////        Debug.Log($"[CustomerManager] Task goal: {_taskGoal} customers.");
////    }

////    private void UpdateTaskUI()
////    {
////        if (taskCounterText != null)
////            taskCounterText.text = $"{_fedCount}/{_taskGoal}";
////    }

////    private void ShowCongratsPanel()
////    {
////        if (congratsPanel == null) return;

////        congratsPanel.transform.localScale = Vector3.zero;
////        congratsPanel.SetActive(true);
////        congratsPanel.transform.SetAsLastSibling();
////        congratsPanel.transform.DOScale(Vector3.one, 0.35f)
////            .SetEase(Ease.OutBack)
////            .SetUpdate(true);

////        Debug.Log($"[CustomerManager] Task complete! Fed {_fedCount}/{_taskGoal} customers.");
////    }

////    private void OnRestartClicked()
////    {
////        if (congratsPanel != null)
////        {
////            congratsPanel.transform.DOScale(Vector3.zero, 0.25f)
////                .SetEase(Ease.InBack)
////                .SetUpdate(true)
////                .OnComplete(() =>
////                {
////                    congratsPanel.SetActive(false);
////                    RestartGame();
////                });
////        }
////        else
////        {
////            RestartGame();
////        }
////    }

////    private void RestartGame()
////    {
////        InitTask();

////        // Send any currently-active customers home immediately, unsubscribing their events.
////        for (int i = 0; i < _slotOccupant.Length; i++)
////        {
////            CustomerController c = _slotOccupant[i];
////            if (c == null) continue;

////            c.OnServed -= OnCustomerServed;
////            c.OnLeft -= OnCustomerLeft;
////            c.gameObject.SetActive(false);
////            _slotOccupant[i] = null;
////        }

////        // Rebuild the waiting pool from scratch.
////        _waiting.Clear();
////        _active.Clear();
////        _waiting.AddRange(_pool);
////        Shuffle(_waiting);

////        // Start spawning again.
////        StartCoroutine(InitialSpawnRoutine());
////    }
////}

//using System.Collections;
//using System.Collections.Generic;
//using UnityEngine;
//using UnityEngine.UI;
//using TMPro;
//using DG.Tweening;

///// <summary>
///// Singleton — manages 3 customers cycling through 2 fixed scene slots.
/////
///// SETUP
/////   1. Attach to ONE empty GameObject in the scene.
/////   2. Drag all three CustomerController GameObjects into customer1/2/3.
/////   3. Drag the two scene target RectTransforms into targetPoint1/targetPoint2.
/////   4. Drag the two PlateControllers into plate1/plate2 (matched to target points).
/////   5. Drag the two off-screen spawn RectTransforms into spawnPoint1/spawnPoint2.
/////   6. Create an intro panel with a pig animation and an OK button.
/////      Drag the panel into introPanel and wire the OK button OnClick → StartGame().
/////
///// FLOW
/////   On Start: the intro panel is shown (pig says "Will you help me cook?").
/////   Player presses OK → StartGame() → intro hides → customers begin spawning.
/////   Only two customers are ever on-screen at once; the third waits in the pool.
///// </summary>
//[DefaultExecutionOrder(-50)]
//public class CustomerManager : MonoBehaviour
//{
//    // ── Singleton ─────────────────────────────────────────────────────────────

//    public static CustomerManager Instance { get; private set; }

//    // ── Inspector ─────────────────────────────────────────────────────────────

//    [Header("Intro Panel")]
//    [Tooltip("Panel with the pig animation and OK button. Set ACTIVE in the Editor.")]
//    public GameObject introPanel;

//    [Tooltip("The Chathead RectTransform inside IntroPanel — plays a looping zoom pulse.")]
//    public RectTransform introChathead;

//    [Tooltip("TMP_Text inside the intro panel — displays the task goal.\n" +
//             "E.g. 'Will you help me to serve 7 customers?'")]
//    public TMP_Text introCustomerText;

//    [Header("Customers  (3 total — any can occupy either slot)")]
//    public CustomerController customer1;
//    public CustomerController customer2;
//    public CustomerController customer3;

//    [Header("Scene Slots  (2 slots — target points + matching plates)")]
//    [Tooltip("Counter position for slot 0.")]
//    public RectTransform targetPoint1;
//    [Tooltip("Counter position for slot 1.")]
//    public RectTransform targetPoint2;

//    [Tooltip("Plate that belongs to slot 0 (targetPoint1 side).")]
//    public PlateController plate1;
//    [Tooltip("Plate that belongs to slot 1 (targetPoint2 side).")]
//    public PlateController plate2;

//    [Tooltip("Off-screen spawn point for slot 0.")]
//    public RectTransform spawnPoint1;
//    [Tooltip("Off-screen spawn point for slot 1.")]
//    public RectTransform spawnPoint2;

//    [Header("Spawn Timing  (seconds)")]
//    [Tooltip("Delay before the very first customer walks in after OK is pressed.")]
//    public float firstSpawnDelay = 0.5f;
//    [Tooltip("Delay between the first and second customer at game start.")]
//    public float secondSpawnDelay = 2f;
//    [Tooltip("Delay after a customer leaves before the next one walks in.")]
//    public float nextCustomerDelay = 1.5f;

//    [Header("Mouth Detection")]
//    [Tooltip("Screen-pixel radius for mouth-drop detection.")]
//    public float mouthDetectionRadius = 90f;

//    [Header("Task System")]
//    [Tooltip("Text showing progress e.g. 2/7. Assign a TMP_Text in the Inspector.")]
//    public TMP_Text taskCounterText;
//    [Tooltip("Panel shown when all customers are fed. Set INACTIVE in Editor.")]
//    public GameObject congratsPanel;
//    [Tooltip("Restart button inside the congrats panel.")]
//    public Button restartButton;
//    [Tooltip("Min customers to feed per round.")]
//    public int taskMin = 5;
//    [Tooltip("Max customers to feed per round.")]
//    public int taskMax = 10;
//    [Tooltip("Delay after the last customer is served before the congrats panel appears.")]
//    public float congratsDelay = 0.5f;

//    // ── Private ───────────────────────────────────────────────────────────────

//    private int _taskGoal;
//    private int _fedCount;
//    private bool _taskComplete;

//    private List<CustomerController> _pool = new List<CustomerController>();
//    private List<CustomerController> _waiting = new List<CustomerController>();
//    private CustomerController[] _slotOccupant = new CustomerController[2];
//    private readonly List<CustomerController> _active = new List<CustomerController>();
//    private bool _gameStarted = false;

//    // ── Public read — lets GrillSpawner block dragging before OK is pressed ──
//    public bool IsGameStarted => _gameStarted;

//    // ── Unity ─────────────────────────────────────────────────────────────────

//    private void Awake()
//    {
//        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
//        Instance = this;

//        // Start panel at zero scale — DOTween zoom-in plays from Start().
//        if (introPanel != null)
//            introPanel.transform.localScale = Vector3.zero;

//        if (restartButton != null) restartButton.onClick.AddListener(OnRestartClicked);
//    }

//    private void Start()
//    {
//        // Build pool
//        if (customer1 != null) _pool.Add(customer1);
//        if (customer2 != null) _pool.Add(customer2);
//        if (customer3 != null) _pool.Add(customer3);

//        // Hide all customers — manager controls visibility
//        foreach (var c in _pool)
//            c.gameObject.SetActive(false);

//        Shuffle(_pool);
//        _waiting.AddRange(_pool);

//        InitTask();
//        if (congratsPanel != null) congratsPanel.SetActive(false);

//        // Zoom the intro panel in from zero on start.
//        if (introPanel != null)
//        {
//            // Disable child CanvasGroups before activation so nothing blocks the OK button.
//            foreach (CanvasGroup child in introPanel.GetComponentsInChildren<CanvasGroup>(true))
//            {
//                if (child.gameObject == introPanel) continue;
//                child.blocksRaycasts = false;
//                child.interactable = false;
//            }

//            introPanel.SetActive(true);
//            introPanel.transform.localScale = Vector3.zero;

//            introPanel.transform.DOScale(Vector3.one, 0.3f)
//                .SetEase(Ease.OutBack)
//                .SetDelay(1f)
//                .SetUpdate(true)
//                .OnComplete(() =>
//                {
//                    CanvasGroup cg = introPanel.GetComponent<CanvasGroup>();
//                    if (cg != null) { cg.alpha = 1f; cg.interactable = true; cg.blocksRaycasts = true; }
//                    PlayChatheadZoom();
//                });
//        }
//    }

//    // ── Registration (for mouth detection) ────────────────────────────────────

//    public void Register(CustomerController c)
//    {
//        if (c != null && !_active.Contains(c)) _active.Add(c);
//    }

//    public void Unregister(CustomerController c) => _active.Remove(c);

//    // ── OK button target ──────────────────────────────────────────────────────

//    /// <summary>
//    /// Wire the intro panel's OK button OnClick → this method.
//    /// Hides the intro and starts spawning customers.
//    /// </summary>
//    public void StartGame()
//    {
//        if (_gameStarted) return;
//        _gameStarted = true;

//        // Stop the chathead zoom loop before closing the panel
//        if (introChathead != null)
//        {
//            introChathead.DOKill();
//            introChathead.localScale = Vector3.one;
//        }

//        // Close intro panel
//        if (introPanel != null)
//            StartCoroutine(IntroPanelExitRoutine());

//        StartCoroutine(InitialSpawnRoutine());
//        Debug.Log("BUTTON CLICKED");
//    }

//    // ── Initial spawn ─────────────────────────────────────────────────────────

//    private IEnumerator InitialSpawnRoutine()
//    {
//        yield return new WaitForSeconds(firstSpawnDelay);
//        SpawnIntoSlot(0);

//        yield return new WaitForSeconds(secondSpawnDelay);
//        SpawnIntoSlot(1);
//    }

//    // ── Task: called immediately when a customer's last order is fulfilled ─────

//    private void OnCustomerServed(CustomerController c)
//    {
//        c.OnServed -= OnCustomerServed;

//        _fedCount++;
//        UpdateTaskUI();

//        if (!_taskComplete && _fedCount >= _taskGoal)
//        {
//            _taskComplete = true;
//            Invoke(nameof(ShowCongratsPanel), congratsDelay);
//        }
//    }

//    // ── Called by CustomerController when it finishes walking off-screen ───────

//    public void OnCustomerLeft(CustomerController c)
//    {
//        c.OnLeft -= OnCustomerLeft;

//        int freedSlot = -1;
//        for (int i = 0; i < _slotOccupant.Length; i++)
//        {
//            if (_slotOccupant[i] != c) continue;
//            _slotOccupant[i] = null;
//            freedSlot = i;
//            break;
//        }

//        if (!_waiting.Contains(c)) _waiting.Add(c);

//        if (_taskComplete) return; // task done — no more spawning

//        if (freedSlot >= 0)
//            StartCoroutine(DelayedSpawnIntoSlot(freedSlot, nextCustomerDelay));
//    }

//    private IEnumerator DelayedSpawnIntoSlot(int slotIndex, float delay)
//    {
//        yield return new WaitForSeconds(delay);
//        SpawnIntoSlot(slotIndex);
//    }

//    // ── Spawn logic ───────────────────────────────────────────────────────────

//    private void SpawnIntoSlot(int slotIndex)
//    {
//        if (_waiting.Count == 0) return;
//        if (slotIndex < 0 || slotIndex >= _slotOccupant.Length) return;
//        if (_slotOccupant[slotIndex] != null) return;

//        RectTransform targetPt = slotIndex == 0 ? targetPoint1 : targetPoint2;
//        RectTransform spawnPt = slotIndex == 0 ? spawnPoint1 : spawnPoint2;
//        PlateController plate = slotIndex == 0 ? plate1 : plate2;

//        if (targetPt == null || spawnPt == null)
//        {
//            Debug.LogWarning($"[CustomerManager] Slot {slotIndex}: target or spawn point not assigned!");
//            return;
//        }

//        int idx = Random.Range(0, _waiting.Count);
//        CustomerController next = _waiting[idx];
//        _waiting.RemoveAt(idx);

//        _slotOccupant[slotIndex] = next;
//        next.owningPlate = plate;
//        next.OnServed += OnCustomerServed;
//        next.OnLeft += OnCustomerLeft;

//        next.gameObject.SetActive(true);
//        ApplySlotSortOrder(slotIndex, next);
//        next.StartWalking(spawnPt, targetPt);

//        Debug.Log($"[CustomerManager] '{next.name}' → slot {slotIndex} (plate: {plate?.plateLabel ?? "none"}).");
//    }

//    /// <summary>
//    /// Ensures the back slot (slot 1) always renders behind the front slot
//    /// (slot 0), regardless of which character (Tori/WhiteCow/Horse) occupies
//    /// which slot. Only re-orders the two customer GameObjects relative to
//    /// each other — their position among other siblings is preserved.
//    /// </summary>
//    private void ApplySlotSortOrder(int slotIndex, CustomerController customer)
//    {
//        Transform t = customer.transform;

//        if (slotIndex == 0)
//        {
//            // Front slot — render above the back-slot occupant if present.
//            CustomerController other = _slotOccupant[1];
//            if (other != null && other != customer && other.transform.parent == t.parent)
//                t.SetSiblingIndex(other.transform.GetSiblingIndex() + 1);
//            else
//                t.SetAsLastSibling();
//        }
//        else
//        {
//            // Back slot — render below the front-slot occupant if present.
//            CustomerController other = _slotOccupant[0];
//            if (other != null && other != customer && other.transform.parent == t.parent)
//            {
//                int otherIndex = other.transform.GetSiblingIndex();
//                int myIndex = t.GetSiblingIndex();
//                t.SetSiblingIndex(myIndex < otherIndex ? myIndex : otherIndex);
//            }
//            else
//                t.SetAsFirstSibling();
//        }
//    }

//    // ── Mouth zone proximity lookup ────────────────────────────────────────────

//    public (CustomerController customer, float dist) GetNearestMouth(Vector2 screenPos, Camera cam)
//    {
//        CustomerController best = null;
//        float bestDist = mouthDetectionRadius;

//        foreach (CustomerController c in _active)
//        {
//            if (c == null || !c.HasArrived || c.mouthPoint == null) continue;

//            Vector2 mouthScreen = RectTransformUtility.WorldToScreenPoint(cam, c.mouthPoint.position);
//            float dist = Vector2.Distance(screenPos, mouthScreen);

//            if (dist < bestDist) { bestDist = dist; best = c; }
//        }

//        return (best, bestDist);
//    }

//    // ── Intro Chathead Zoom ───────────────────────────────────────────────────

//    /// <summary>
//    /// Loops a subtle zoom-in → zoom-out pulse on the intro panel's chathead
//    /// using DOTween. Killed automatically when the player presses OK.
//    /// </summary>
//    private void PlayChatheadZoom()
//    {
//        if (introChathead == null) return;

//        introChathead.localScale = Vector3.one;

//        // Zoom in to 1.12, then zoom back to 1.0, repeat forever.
//        introChathead.DOScale(1.12f, 0.45f)
//            .SetEase(Ease.OutQuad)
//            .SetLoops(-1, LoopType.Yoyo)
//            .SetUpdate(true); // runs even if Time.timeScale = 0
//    }

//    // ── Intro Panel Animations ───────────────────────────────────────────────

//    /// <summary>DOTween zoom-out — scales panel to zero then deactivates it.</summary>
//    private IEnumerator IntroPanelExitRoutine()
//    {
//        CanvasGroup cg = introPanel.GetComponent<CanvasGroup>();
//        if (cg != null) { cg.interactable = false; cg.blocksRaycasts = false; }

//        bool done = false;
//        introPanel.transform.DOScale(Vector3.zero, 0.3f)
//            .SetEase(Ease.InBack)
//            .SetUpdate(true)
//            .OnComplete(() => { introPanel.SetActive(false); done = true; });
//        SoundManager.Instance.PlaySFX("Button");

//        yield return new WaitUntil(() => done);
//    }

//    // ── Utility ───────────────────────────────────────────────────────────────

//    private static void Shuffle<T>(List<T> list)
//    {
//        for (int i = list.Count - 1; i > 0; i--)
//        {
//            int j = Random.Range(0, i + 1);
//            (list[i], list[j]) = (list[j], list[i]);
//        }
//    }

//    public int WalkingCount { get; private set; }
//    public void OnCustomerStartWalk() => WalkingCount++;
//    public void OnCustomerStopWalk() => WalkingCount = Mathf.Max(0, WalkingCount - 1);

//    // ── Task System ───────────────────────────────────────────────────────────

//    private void InitTask()
//    {
//        _taskGoal = Random.Range(taskMin, taskMax + 1);
//        _fedCount = 0;
//        _taskComplete = false;
//        UpdateTaskUI();

//        // Update intro panel text with the freshly-chosen goal.
//        if (introCustomerText != null)
//            introCustomerText.text = $"Will you help me to serve {_taskGoal} customers?";

//        Debug.Log($"[CustomerManager] Task goal: {_taskGoal} customers.");
//    }

//    private void UpdateTaskUI()
//    {
//        if (taskCounterText != null)
//            taskCounterText.text = $"{_fedCount}/{_taskGoal}";
//    }

//    private void ShowCongratsPanel()
//    {
//        if (congratsPanel == null) return;

//        congratsPanel.transform.localScale = Vector3.zero;
//        congratsPanel.SetActive(true);
//        congratsPanel.transform.SetAsLastSibling();
//        congratsPanel.transform.DOScale(Vector3.one, 0.35f)
//            .SetEase(Ease.OutBack)
//            .SetUpdate(true);

//        Debug.Log($"[CustomerManager] Task complete! Fed {_fedCount}/{_taskGoal} customers.");
//    }

//    private void OnRestartClicked()
//    {
//        if (congratsPanel != null)
//        {
//            congratsPanel.transform.DOScale(Vector3.zero, 0.25f)
//                .SetEase(Ease.InBack)
//                .SetUpdate(true)
//                .OnComplete(() =>
//                {
//                    congratsPanel.SetActive(false);
//                    RestartGame();
//                });
//        }
//        else
//        {
//            RestartGame();
//        }
//    }

//    private void RestartGame()
//    {
//        InitTask();

//        // Send any currently-active customers home immediately, unsubscribing their events.
//        for (int i = 0; i < _slotOccupant.Length; i++)
//        {
//            CustomerController c = _slotOccupant[i];
//            if (c == null) continue;

//            c.OnServed -= OnCustomerServed;
//            c.OnLeft -= OnCustomerLeft;
//            c.gameObject.SetActive(false);
//            _slotOccupant[i] = null;
//        }

//        // Rebuild the waiting pool from scratch.
//        _waiting.Clear();
//        _active.Clear();
//        _waiting.AddRange(_pool);
//        Shuffle(_waiting);

//        // Start spawning again.
//        StartCoroutine(InitialSpawnRoutine());
//    }
//}

//using System.Collections;
//using System.Collections.Generic;
//using UnityEngine;
//using UnityEngine.UI;
//using TMPro;
//using DG.Tweening;

///// <summary>
///// Singleton — manages 3 customers cycling through 2 fixed scene slots.
/////
///// SETUP
/////   1. Attach to ONE empty GameObject in the scene.
/////   2. Drag all three CustomerController GameObjects into customer1/2/3.
/////   3. Drag the two scene target RectTransforms into targetPoint1/targetPoint2.
/////   4. Drag the two PlateControllers into plate1/plate2 (matched to target points).
/////   5. Drag the two off-screen spawn RectTransforms into spawnPoint1/spawnPoint2.
/////   6. Create an intro panel with a pig animation and an OK button.
/////      Drag the panel into introPanel and wire the OK button OnClick → StartGame().
/////
///// FLOW
/////   On Start: the intro panel is shown (pig says "Will you help me cook?").
/////   Player presses OK → StartGame() → intro hides → customers begin spawning.
/////   Only two customers are ever on-screen at once; the third waits in the pool.
///// </summary>
//[DefaultExecutionOrder(-50)]
//public class CustomerManager : MonoBehaviour
//{
//    // ── Singleton ─────────────────────────────────────────────────────────────

//    public static CustomerManager Instance { get; private set; }

//    // ── Inspector ─────────────────────────────────────────────────────────────

//    [Header("Intro Panel")]
//    [Tooltip("Panel with the pig animation and OK button. Set ACTIVE in the Editor.")]
//    public GameObject introPanel;

//    [Tooltip("The Chathead RectTransform inside IntroPanel — plays a looping zoom pulse.")]
//    public RectTransform introChathead;

//    [Header("Customers  (3 total — any can occupy either slot)")]
//    public CustomerController customer1;
//    public CustomerController customer2;
//    public CustomerController customer3;

//    [Header("Scene Slots  (2 slots — target points + matching plates)")]
//    [Tooltip("Counter position for slot 0.")]
//    public RectTransform targetPoint1;
//    [Tooltip("Counter position for slot 1.")]
//    public RectTransform targetPoint2;

//    [Tooltip("Plate that belongs to slot 0 (targetPoint1 side).")]
//    public PlateController plate1;
//    [Tooltip("Plate that belongs to slot 1 (targetPoint2 side).")]
//    public PlateController plate2;

//    [Tooltip("Off-screen spawn point for slot 0.")]
//    public RectTransform spawnPoint1;
//    [Tooltip("Off-screen spawn point for slot 1.")]
//    public RectTransform spawnPoint2;

//    [Header("Spawn Timing  (seconds)")]
//    [Tooltip("Delay before the very first customer walks in after OK is pressed.")]
//    public float firstSpawnDelay = 0.5f;
//    [Tooltip("Delay between the first and second customer at game start.")]
//    public float secondSpawnDelay = 2f;
//    [Tooltip("Delay after a customer leaves before the next one walks in.")]
//    public float nextCustomerDelay = 1.5f;

//    [Header("Mouth Detection")]
//    [Tooltip("Screen-pixel radius for mouth-drop detection.")]
//    public float mouthDetectionRadius = 90f;

//    [Header("Task System")]
//    [Tooltip("Text showing progress e.g. 2/7. Assign a TMP_Text in the Inspector.")]
//    public TMP_Text taskCounterText;
//    [Tooltip("Panel shown when all customers are fed. Set INACTIVE in Editor.")]
//    public GameObject congratsPanel;
//    [Tooltip("Restart button inside the congrats panel.")]
//    public Button restartButton;
//    [Tooltip("Min customers to feed per round.")]
//    public int taskMin = 5;
//    [Tooltip("Max customers to feed per round.")]
//    public int taskMax = 10;
//    [Tooltip("Delay after the last customer is served before the congrats panel appears.")]
//    public float congratsDelay = 0.5f;

//    // ── Private ───────────────────────────────────────────────────────────────

//    private int _taskGoal;
//    private int _fedCount;
//    private bool _taskComplete;

//    private List<CustomerController> _pool = new List<CustomerController>();
//    private List<CustomerController> _waiting = new List<CustomerController>();
//    private CustomerController[] _slotOccupant = new CustomerController[2];
//    private readonly List<CustomerController> _active = new List<CustomerController>();
//    private bool _gameStarted = false;

//    // ── Unity ─────────────────────────────────────────────────────────────────

//    private void Awake()
//    {
//        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
//        Instance = this;

//        // Start panel at zero scale — DOTween zoom-in plays from Start().
//        if (introPanel != null)
//            introPanel.transform.localScale = Vector3.zero;

//        if (restartButton != null) restartButton.onClick.AddListener(OnRestartClicked);
//    }

//    private void Start()
//    {
//        // Build pool
//        if (customer1 != null) _pool.Add(customer1);
//        if (customer2 != null) _pool.Add(customer2);
//        if (customer3 != null) _pool.Add(customer3);

//        // Hide all customers — manager controls visibility
//        foreach (var c in _pool)
//            c.gameObject.SetActive(false);

//        Shuffle(_pool);
//        _waiting.AddRange(_pool);

//        InitTask();
//        if (congratsPanel != null) congratsPanel.SetActive(false);

//        // Zoom the intro panel in from zero on start.
//        if (introPanel != null)
//        {
//            // Disable child CanvasGroups before activation so nothing blocks the OK button.
//            foreach (CanvasGroup child in introPanel.GetComponentsInChildren<CanvasGroup>(true))
//            {
//                if (child.gameObject == introPanel) continue;
//                child.blocksRaycasts = false;
//                child.interactable = false;
//            }

//            introPanel.SetActive(true);
//            introPanel.transform.localScale = Vector3.zero;

//            introPanel.transform.DOScale(Vector3.one, 0.3f)
//                .SetEase(Ease.OutBack)
//                .SetDelay(1f)
//                .SetUpdate(true)
//                .OnComplete(() =>
//                {
//                    CanvasGroup cg = introPanel.GetComponent<CanvasGroup>();
//                    if (cg != null) { cg.alpha = 1f; cg.interactable = true; cg.blocksRaycasts = true; }
//                    PlayChatheadZoom();
//                });
//        }
//    }

//    // ── Registration (for mouth detection) ────────────────────────────────────

//    public void Register(CustomerController c)
//    {
//        if (c != null && !_active.Contains(c)) _active.Add(c);
//    }

//    public void Unregister(CustomerController c) => _active.Remove(c);

//    // ── OK button target ──────────────────────────────────────────────────────

//    /// <summary>
//    /// Wire the intro panel's OK button OnClick → this method.
//    /// Hides the intro and starts spawning customers.
//    /// </summary>
//    public void StartGame()
//    {
//        if (_gameStarted) return;
//        _gameStarted = true;

//        // Stop the chathead zoom loop before closing the panel
//        if (introChathead != null)
//        {
//            introChathead.DOKill();
//            introChathead.localScale = Vector3.one;
//        }

//        // Close intro panel
//        if (introPanel != null)
//            StartCoroutine(IntroPanelExitRoutine());

//        StartCoroutine(InitialSpawnRoutine());
//        Debug.Log("BUTTON CLICKED");
//    }

//    // ── Initial spawn ─────────────────────────────────────────────────────────

//    private IEnumerator InitialSpawnRoutine()
//    {
//        yield return new WaitForSeconds(firstSpawnDelay);
//        SpawnIntoSlot(0);

//        yield return new WaitForSeconds(secondSpawnDelay);
//        SpawnIntoSlot(1);
//    }

//    // ── Task: called immediately when a customer's last order is fulfilled ─────

//    private void OnCustomerServed(CustomerController c)
//    {
//        c.OnServed -= OnCustomerServed;

//        _fedCount++;
//        UpdateTaskUI();

//        if (!_taskComplete && _fedCount >= _taskGoal)
//        {
//            _taskComplete = true;
//            Invoke(nameof(ShowCongratsPanel), congratsDelay);
//        }
//    }

//    // ── Called by CustomerController when it finishes walking off-screen ───────

//    public void OnCustomerLeft(CustomerController c)
//    {
//        c.OnLeft -= OnCustomerLeft;

//        int freedSlot = -1;
//        for (int i = 0; i < _slotOccupant.Length; i++)
//        {
//            if (_slotOccupant[i] != c) continue;
//            _slotOccupant[i] = null;
//            freedSlot = i;
//            break;
//        }

//        if (!_waiting.Contains(c)) _waiting.Add(c);

//        if (_taskComplete) return; // task done — no more spawning

//        if (freedSlot >= 0)
//            StartCoroutine(DelayedSpawnIntoSlot(freedSlot, nextCustomerDelay));
//    }

//    private IEnumerator DelayedSpawnIntoSlot(int slotIndex, float delay)
//    {
//        yield return new WaitForSeconds(delay);
//        SpawnIntoSlot(slotIndex);
//    }

//    // ── Spawn logic ───────────────────────────────────────────────────────────

//    private void SpawnIntoSlot(int slotIndex)
//    {
//        if (_waiting.Count == 0) return;
//        if (slotIndex < 0 || slotIndex >= _slotOccupant.Length) return;
//        if (_slotOccupant[slotIndex] != null) return;

//        RectTransform targetPt = slotIndex == 0 ? targetPoint1 : targetPoint2;
//        RectTransform spawnPt = slotIndex == 0 ? spawnPoint1 : spawnPoint2;
//        PlateController plate = slotIndex == 0 ? plate1 : plate2;

//        if (targetPt == null || spawnPt == null)
//        {
//            Debug.LogWarning($"[CustomerManager] Slot {slotIndex}: target or spawn point not assigned!");
//            return;
//        }

//        int idx = Random.Range(0, _waiting.Count);
//        CustomerController next = _waiting[idx];
//        _waiting.RemoveAt(idx);

//        _slotOccupant[slotIndex] = next;
//        next.owningPlate = plate;
//        next.OnServed += OnCustomerServed;
//        next.OnLeft += OnCustomerLeft;

//        next.gameObject.SetActive(true);
//        ApplySlotSortOrder(slotIndex, next);
//        next.StartWalking(spawnPt, targetPt);

//        Debug.Log($"[CustomerManager] '{next.name}' → slot {slotIndex} (plate: {plate?.plateLabel ?? "none"}).");
//    }

//    /// <summary>
//    /// Ensures the back slot (slot 1) always renders behind the front slot
//    /// (slot 0), regardless of which character (Tori/WhiteCow/Horse) occupies
//    /// which slot. Only re-orders the two customer GameObjects relative to
//    /// each other — their position among other siblings is preserved.
//    /// </summary>
//    private void ApplySlotSortOrder(int slotIndex, CustomerController customer)
//    {
//        Transform t = customer.transform;

//        if (slotIndex == 0)
//        {
//            // Front slot — render above the back-slot occupant if present.
//            CustomerController other = _slotOccupant[1];
//            if (other != null && other != customer && other.transform.parent == t.parent)
//                t.SetSiblingIndex(other.transform.GetSiblingIndex() + 1);
//            else
//                t.SetAsLastSibling();
//        }
//        else
//        {
//            // Back slot — render below the front-slot occupant if present.
//            CustomerController other = _slotOccupant[0];
//            if (other != null && other != customer && other.transform.parent == t.parent)
//            {
//                int otherIndex = other.transform.GetSiblingIndex();
//                int myIndex = t.GetSiblingIndex();
//                t.SetSiblingIndex(myIndex < otherIndex ? myIndex : otherIndex);
//            }
//            else
//                t.SetAsFirstSibling();
//        }
//    }

//    // ── Mouth zone proximity lookup ────────────────────────────────────────────

//    public (CustomerController customer, float dist) GetNearestMouth(Vector2 screenPos, Camera cam)
//    {
//        CustomerController best = null;
//        float bestDist = mouthDetectionRadius;

//        foreach (CustomerController c in _active)
//        {
//            if (c == null || !c.HasArrived || c.mouthPoint == null) continue;

//            Vector2 mouthScreen = RectTransformUtility.WorldToScreenPoint(cam, c.mouthPoint.position);
//            float dist = Vector2.Distance(screenPos, mouthScreen);

//            if (dist < bestDist) { bestDist = dist; best = c; }
//        }

//        return (best, bestDist);
//    }

//    // ── Intro Chathead Zoom ───────────────────────────────────────────────────

//    /// <summary>
//    /// Loops a subtle zoom-in → zoom-out pulse on the intro panel's chathead
//    /// using DOTween. Killed automatically when the player presses OK.
//    /// </summary>
//    private void PlayChatheadZoom()
//    {
//        if (introChathead == null) return;

//        introChathead.localScale = Vector3.one;

//        // Zoom in to 1.12, then zoom back to 1.0, repeat forever.
//        introChathead.DOScale(1.12f, 0.45f)
//            .SetEase(Ease.OutQuad)
//            .SetLoops(-1, LoopType.Yoyo)
//            .SetUpdate(true); // runs even if Time.timeScale = 0
//    }

//    // ── Intro Panel Animations ───────────────────────────────────────────────

//    /// <summary>DOTween zoom-out — scales panel to zero then deactivates it.</summary>
//    private IEnumerator IntroPanelExitRoutine()
//    {
//        CanvasGroup cg = introPanel.GetComponent<CanvasGroup>();
//        if (cg != null) { cg.interactable = false; cg.blocksRaycasts = false; }

//        bool done = false;
//        introPanel.transform.DOScale(Vector3.zero, 0.3f)
//            .SetEase(Ease.InBack)
//            .SetUpdate(true)
//            .OnComplete(() => { introPanel.SetActive(false); done = true; });
//        SoundManager.Instance.PlaySFX("Button");

//        yield return new WaitUntil(() => done);
//    }

//    // ── Utility ───────────────────────────────────────────────────────────────

//    private static void Shuffle<T>(List<T> list)
//    {
//        for (int i = list.Count - 1; i > 0; i--)
//        {
//            int j = Random.Range(0, i + 1);
//            (list[i], list[j]) = (list[j], list[i]);
//        }
//    }

//    public int WalkingCount { get; private set; }
//    public void OnCustomerStartWalk() => WalkingCount++;
//    public void OnCustomerStopWalk() => WalkingCount = Mathf.Max(0, WalkingCount - 1);

//    // ── Task System ───────────────────────────────────────────────────────────

//    private void InitTask()
//    {
//        _taskGoal = Random.Range(taskMin, taskMax + 1);
//        _fedCount = 0;
//        _taskComplete = false;
//        UpdateTaskUI();
//        Debug.Log($"[CustomerManager] Task goal: {_taskGoal} customers.");
//    }

//    private void UpdateTaskUI()
//    {
//        if (taskCounterText != null)
//            taskCounterText.text = $"{_fedCount}/{_taskGoal}";
//    }

//    private void ShowCongratsPanel()
//    {
//        if (congratsPanel == null) return;

//        congratsPanel.transform.localScale = Vector3.zero;
//        congratsPanel.SetActive(true);
//        congratsPanel.transform.SetAsLastSibling();
//        congratsPanel.transform.DOScale(Vector3.one, 0.35f)
//            .SetEase(Ease.OutBack)
//            .SetUpdate(true);

//        Debug.Log($"[CustomerManager] Task complete! Fed {_fedCount}/{_taskGoal} customers.");
//    }

//    private void OnRestartClicked()
//    {
//        if (congratsPanel != null)
//        {
//            congratsPanel.transform.DOScale(Vector3.zero, 0.25f)
//                .SetEase(Ease.InBack)
//                .SetUpdate(true)
//                .OnComplete(() =>
//                {
//                    congratsPanel.SetActive(false);
//                    RestartGame();
//                });
//        }
//        else
//        {
//            RestartGame();
//        }
//    }

//    private void RestartGame()
//    {
//        InitTask();

//        // Send any currently-active customers home immediately, unsubscribing their events.
//        for (int i = 0; i < _slotOccupant.Length; i++)
//        {
//            CustomerController c = _slotOccupant[i];
//            if (c == null) continue;

//            c.OnServed -= OnCustomerServed;
//            c.OnLeft -= OnCustomerLeft;
//            c.gameObject.SetActive(false);
//            _slotOccupant[i] = null;
//        }

//        // Rebuild the waiting pool from scratch.
//        _waiting.Clear();
//        _active.Clear();
//        _waiting.AddRange(_pool);
//        Shuffle(_waiting);

//        // Start spawning again.
//        StartCoroutine(InitialSpawnRoutine());
//    }
//}

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;

/// <summary>
/// Singleton — manages 3 customers cycling through 2 fixed scene slots.
///
/// SETUP
///   1. Attach to ONE empty GameObject in the scene.
///   2. Drag all three CustomerController GameObjects into customer1/2/3.
///   3. Drag the two scene target RectTransforms into targetPoint1/targetPoint2.
///   4. Drag the two PlateControllers into plate1/plate2 (matched to target points).
///   5. Drag the two off-screen spawn RectTransforms into spawnPoint1/spawnPoint2.
///   6. Create an intro panel with a pig animation and an OK button.
///      Drag the panel into introPanel and wire the OK button OnClick → StartGame().
///
/// FLOW
///   On Start: the intro panel is shown (pig says "Will you help me cook?").
///   Player presses OK → StartGame() → intro hides → customers begin spawning.
///   Only two customers are ever on-screen at once; the third waits in the pool.
/// </summary>
[DefaultExecutionOrder(-50)]
public class CustomerManager : MonoBehaviour
{
    // ── Singleton ─────────────────────────────────────────────────────────────

    public static CustomerManager Instance { get; private set; }

    // ── Inspector ─────────────────────────────────────────────────────────────

    [Header("Intro Panel")]
    [Tooltip("Panel with the pig animation and OK button. Set ACTIVE in the Editor.")]
    public GameObject introPanel;

    [Tooltip("The Chathead RectTransform inside IntroPanel — plays a looping zoom pulse.")]
    public RectTransform introChathead;

    [Tooltip("TMP_Text inside the intro panel — displays the task goal.\n" +
             "E.g. 'Will you help me to serve 7 customers?'")]
    public TMP_Text introCustomerText;

    [Tooltip("Characters per second for the intro text's left-to-right typing effect.")]
    public float introTextCharsPerSecond = 20f;

    [Tooltip("Seconds to wait after the intro panel is shown before the chathead appears.")]
    public float introChatheadAppearDelay = 0.5f;

    [Tooltip("Must match the SFX name in AudioData exactly.\n" +
             "Looped via SoundManager.PlaySFXLoop while typing, stopped when typing ends.")]
    public string typingSFXName = "Typing";

    [Tooltip("The OK / continue button inside the intro panel.\n" +
             "Hidden until the intro text finishes typing, then appears after okButtonAppearDelay.")]
    public GameObject okButton;

    [Tooltip("Seconds to wait after typing finishes before the OK button appears.")]
    public float okButtonAppearDelay = 0.5f;

    private Coroutine _introTypeRoutine;
    private string _introLine;

    [Header("Customers  (3 total — any can occupy either slot)")]
    public CustomerController customer1;
    public CustomerController customer2;
    public CustomerController customer3;

    [Header("Scene Slots  (2 slots — target points + matching plates)")]
    [Tooltip("Counter position for slot 0.")]
    public RectTransform targetPoint1;
    [Tooltip("Counter position for slot 1.")]
    public RectTransform targetPoint2;

    [Tooltip("Plate that belongs to slot 0 (targetPoint1 side).")]
    public PlateController plate1;
    [Tooltip("Plate that belongs to slot 1 (targetPoint2 side).")]
    public PlateController plate2;

    [Tooltip("Off-screen spawn point for slot 0.")]
    public RectTransform spawnPoint1;
    [Tooltip("Off-screen spawn point for slot 1.")]
    public RectTransform spawnPoint2;

    [Header("Spawn Timing  (seconds)")]
    [Tooltip("Delay before the very first customer walks in after OK is pressed.")]
    public float firstSpawnDelay = 0.5f;
    [Tooltip("Delay between the first and second customer at game start.")]
    public float secondSpawnDelay = 2f;
    [Tooltip("Delay after a customer leaves before the next one walks in.")]
    public float nextCustomerDelay = 1.5f;

    [Header("Mouth Detection")]
    [Tooltip("Screen-pixel radius for mouth-drop detection.")]
    public float mouthDetectionRadius = 90f;

    [Header("Task System")]
    [Tooltip("Text showing progress e.g. 2/7. Assign a TMP_Text in the Inspector.")]
    public TMP_Text taskCounterText;
    [Tooltip("Panel shown when all customers are fed. Set INACTIVE in Editor.")]
    public GameObject congratsPanel;
    [Tooltip("Restart button inside the congrats panel.")]
    public Button restartButton;
    [Tooltip("Min customers to feed per round.")]
    public int taskMin = 5;
    [Tooltip("Max customers to feed per round.")]
    public int taskMax = 10;
    [Tooltip("Delay after the last customer is served before the congrats panel appears.")]
    public float congratsDelay = 0.5f;

    // ── Private ───────────────────────────────────────────────────────────────

    private int _taskGoal;
    private int _fedCount;
    private bool _taskComplete;

    private List<CustomerController> _pool = new List<CustomerController>();
    private List<CustomerController> _waiting = new List<CustomerController>();
    private CustomerController[] _slotOccupant = new CustomerController[2];
    private readonly List<CustomerController> _active = new List<CustomerController>();
    private bool _gameStarted = false;

    // ── Public read — lets GrillSpawner block dragging before OK is pressed ──
    public bool IsGameStarted => _gameStarted;

    // ── Unity ─────────────────────────────────────────────────────────────────

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;

        // Start panel at zero scale — DOTween zoom-in plays from Start().
        if (introPanel != null)
            introPanel.transform.localScale = Vector3.zero;

        // Hide the chathead until its delayed appear (see Start()/OnComplete below).
        if (introChathead != null)
            introChathead.gameObject.SetActive(false);

        // Hide the OK button until the intro text finishes typing.
        if (okButton != null)
            okButton.SetActive(false);

        if (restartButton != null) restartButton.onClick.AddListener(OnRestartClicked);
    }

    private void Start()
    {
        // Build pool
        if (customer1 != null) _pool.Add(customer1);
        if (customer2 != null) _pool.Add(customer2);
        if (customer3 != null) _pool.Add(customer3);

        // Hide all customers — manager controls visibility
        foreach (var c in _pool)
            c.gameObject.SetActive(false);

        Shuffle(_pool);
        _waiting.AddRange(_pool);

        InitTask();
        if (congratsPanel != null) congratsPanel.SetActive(false);

        // Zoom the intro panel in from zero on start.
        if (introPanel != null)
        {
            // Disable child CanvasGroups before activation so nothing blocks the OK button.
            foreach (CanvasGroup child in introPanel.GetComponentsInChildren<CanvasGroup>(true))
            {
                if (child.gameObject == introPanel) continue;
                child.blocksRaycasts = false;
                child.interactable = false;
            }

            introPanel.SetActive(true);
            introPanel.transform.localScale = Vector3.zero;

            introPanel.transform.DOScale(Vector3.one, 0.3f)
                .SetEase(Ease.OutBack)
                .SetDelay(1f)
                .SetUpdate(true)
                .OnComplete(() =>
                {
                    CanvasGroup cg = introPanel.GetComponent<CanvasGroup>();
                    if (cg != null) { cg.alpha = 1f; cg.interactable = true; cg.blocksRaycasts = true; }
                    StartCoroutine(ShowChatheadThenType());
                });
        }
    }

    // ── Registration (for mouth detection) ────────────────────────────────────

    public void Register(CustomerController c)
    {
        if (c != null && !_active.Contains(c)) _active.Add(c);
    }

    public void Unregister(CustomerController c) => _active.Remove(c);

    // ── OK button target ──────────────────────────────────────────────────────

    /// <summary>
    /// Wire the intro panel's OK button OnClick → this method.
    /// Hides the intro and starts spawning customers.
    /// </summary>
    public void StartGame()
    {
        if (_gameStarted) return;
        _gameStarted = true;

        // Stop the chathead zoom loop before closing the panel
        if (introChathead != null)
        {
            introChathead.DOKill();
            introChathead.localScale = Vector3.one;
        }

        // If the player skips before typing finishes, stop the routine and its SFX.
        if (_introTypeRoutine != null)
        {
            StopCoroutine(_introTypeRoutine);
            _introTypeRoutine = null;
            SoundManager.Instance.StopSFXLoop();
        }

        // Close intro panel
        if (introPanel != null)
            StartCoroutine(IntroPanelExitRoutine());

        StartCoroutine(InitialSpawnRoutine());
        Debug.Log("BUTTON CLICKED");
    }

    // ── Initial spawn ─────────────────────────────────────────────────────────

    private IEnumerator InitialSpawnRoutine()
    {
        yield return new WaitForSeconds(firstSpawnDelay);
        SpawnIntoSlot(0);

        yield return new WaitForSeconds(secondSpawnDelay);
        SpawnIntoSlot(1);
    }

    // ── Task: called immediately when a customer's last order is fulfilled ─────

    private void OnCustomerServed(CustomerController c)
    {
        c.OnServed -= OnCustomerServed;

        _fedCount++;
        UpdateTaskUI();

        if (!_taskComplete && _fedCount >= _taskGoal)
        {
            _taskComplete = true;
            Invoke(nameof(ShowCongratsPanel), congratsDelay);
        }
    }

    // ── Called by CustomerController when it finishes walking off-screen ───────

    public void OnCustomerLeft(CustomerController c)
    {
        c.OnLeft -= OnCustomerLeft;

        int freedSlot = -1;
        for (int i = 0; i < _slotOccupant.Length; i++)
        {
            if (_slotOccupant[i] != c) continue;
            _slotOccupant[i] = null;
            freedSlot = i;
            break;
        }

        if (!_waiting.Contains(c)) _waiting.Add(c);

        if (_taskComplete) return; // task done — no more spawning

        if (freedSlot >= 0)
            StartCoroutine(DelayedSpawnIntoSlot(freedSlot, nextCustomerDelay, c));
    }

    private IEnumerator DelayedSpawnIntoSlot(int slotIndex, float delay, CustomerController avoid = null)
    {
        yield return new WaitForSeconds(delay);
        SpawnIntoSlot(slotIndex, avoid);
    }

    // ── Spawn logic ───────────────────────────────────────────────────────────

    /// <summary>
    /// Fills slotIndex with a random waiting customer. If avoid is non-null and
    /// another waiting customer is available, avoid is skipped so the same
    /// customer doesn't immediately return to the same target point they just
    /// left. If avoid is the only one waiting, it is used anyway.
    /// </summary>
    private void SpawnIntoSlot(int slotIndex, CustomerController avoid = null)
    {
        if (_waiting.Count == 0) return;
        if (slotIndex < 0 || slotIndex >= _slotOccupant.Length) return;
        if (_slotOccupant[slotIndex] != null) return;

        RectTransform targetPt = slotIndex == 0 ? targetPoint1 : targetPoint2;
        RectTransform spawnPt = slotIndex == 0 ? spawnPoint1 : spawnPoint2;
        PlateController plate = slotIndex == 0 ? plate1 : plate2;

        if (targetPt == null || spawnPt == null)
        {
            Debug.LogWarning($"[CustomerManager] Slot {slotIndex}: target or spawn point not assigned!");
            return;
        }

        // Prefer any waiting customer other than 'avoid'; fall back to avoid
        // only if nobody else is currently waiting.
        List<int> candidates = new List<int>();
        for (int i = 0; i < _waiting.Count; i++)
            if (_waiting[i] != avoid) candidates.Add(i);

        int idx = candidates.Count > 0
            ? candidates[Random.Range(0, candidates.Count)]
            : Random.Range(0, _waiting.Count);

        CustomerController next = _waiting[idx];
        _waiting.RemoveAt(idx);

        _slotOccupant[slotIndex] = next;
        next.owningPlate = plate;
        next.OnServed += OnCustomerServed;
        next.OnLeft += OnCustomerLeft;

        next.gameObject.SetActive(true);
        ApplySlotSortOrder(slotIndex, next);
        next.StartWalking(spawnPt, targetPt);

        Debug.Log($"[CustomerManager] '{next.name}' → slot {slotIndex} (plate: {plate?.plateLabel ?? "none"}).");
    }

    /// <summary>
    /// Ensures the back slot (slot 1) always renders behind the front slot
    /// (slot 0), regardless of which character (Tori/WhiteCow/Horse) occupies
    /// which slot. Only re-orders the two customer GameObjects relative to
    /// each other — their position among other siblings is preserved.
    /// </summary>
    private void ApplySlotSortOrder(int slotIndex, CustomerController customer)
    {
        Transform t = customer.transform;

        if (slotIndex == 0)
        {
            // Front slot — render above the back-slot occupant if present.
            CustomerController other = _slotOccupant[1];
            if (other != null && other != customer && other.transform.parent == t.parent)
                t.SetSiblingIndex(other.transform.GetSiblingIndex() + 1);
            else
                t.SetAsLastSibling();
        }
        else
        {
            // Back slot — render below the front-slot occupant if present.
            CustomerController other = _slotOccupant[0];
            if (other != null && other != customer && other.transform.parent == t.parent)
            {
                int otherIndex = other.transform.GetSiblingIndex();
                int myIndex = t.GetSiblingIndex();
                t.SetSiblingIndex(myIndex < otherIndex ? myIndex : otherIndex);
            }
            else
                t.SetAsFirstSibling();
        }
    }

    // ── Mouth zone proximity lookup ────────────────────────────────────────────

    public (CustomerController customer, float dist) GetNearestMouth(Vector2 screenPos, Camera cam)
    {
        CustomerController best = null;
        float bestDist = mouthDetectionRadius;

        foreach (CustomerController c in _active)
        {
            if (c == null || !c.HasArrived || c.mouthPoint == null) continue;

            Vector2 mouthScreen = RectTransformUtility.WorldToScreenPoint(cam, c.mouthPoint.position);
            float dist = Vector2.Distance(screenPos, mouthScreen);

            if (dist < bestDist) { bestDist = dist; best = c; }
        }

        return (best, bestDist);
    }

    // ── Intro Chathead Zoom ───────────────────────────────────────────────────

    /// <summary>
    /// Loops a subtle zoom-in → zoom-out pulse on the intro panel's chathead
    /// using DOTween. Killed automatically when the player presses OK.
    /// </summary>
    private void PlayChatheadZoom()
    {
        if (introChathead == null) return;

        introChathead.localScale = Vector3.one;

        // Zoom in to 1.12, then zoom back to 1.0, repeat forever.
        introChathead.DOScale(1.12f, 0.45f)
            .SetEase(Ease.OutQuad)
            .SetLoops(-1, LoopType.Yoyo)
            .SetUpdate(true); // runs even if Time.timeScale = 0
    }

    // ── Intro Panel Animations ───────────────────────────────────────────────

    /// <summary>DOTween zoom-out — scales panel to zero then deactivates it.</summary>
    private IEnumerator IntroPanelExitRoutine()
    {
        CanvasGroup cg = introPanel.GetComponent<CanvasGroup>();
        if (cg != null) { cg.interactable = false; cg.blocksRaycasts = false; }

        bool done = false;
        introPanel.transform.DOScale(Vector3.zero, 0.3f)
            .SetEase(Ease.InBack)
            .SetUpdate(true)
            .OnComplete(() => { introPanel.SetActive(false); done = true; });
        SoundManager.Instance.PlaySFX("Button");

        yield return new WaitUntil(() => done);
    }

    // ── Utility ───────────────────────────────────────────────────────────────

    /// <summary>
    /// Waits introChatheadAppearDelay seconds, reveals the chathead, starts its
    /// zoom pulse, then types out the intro line with the typing SFX looping.
    /// </summary>
    private IEnumerator ShowChatheadThenType()
    {
        yield return new WaitForSeconds(introChatheadAppearDelay);

        if (introChathead != null)
            introChathead.gameObject.SetActive(true);

        PlayChatheadZoom();

        if (introCustomerText != null)
        {
            if (_introTypeRoutine != null) StopCoroutine(_introTypeRoutine);
            _introTypeRoutine = StartCoroutine(TypeText(introCustomerText, _introLine));
        }
    }

    /// <summary>Types fullText into target left-to-right at introTextCharsPerSecond.
    /// Loops the typing SFX for the duration, stops it once typing ends, then
    /// reveals the OK button after okButtonAppearDelay.</summary>
    private IEnumerator TypeText(TMP_Text target, string fullText)
    {
        target.text = string.Empty;
        if (string.IsNullOrEmpty(fullText)) yield break;

        SoundManager.Instance.PlaySFXLoop(typingSFXName);

        float delay = 1f / Mathf.Max(introTextCharsPerSecond, 0.01f);
        for (int i = 1; i <= fullText.Length; i++)
        {
            target.text = fullText.Substring(0, i);
            yield return new WaitForSeconds(delay);
        }

        SoundManager.Instance.StopSFXLoop();
        _introTypeRoutine = null;

        yield return new WaitForSeconds(okButtonAppearDelay);
        if (okButton != null)
            okButton.SetActive(true);
    }

    private static void Shuffle<T>(List<T> list)
    {
        for (int i = list.Count - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            (list[i], list[j]) = (list[j], list[i]);
        }
    }

    public int WalkingCount { get; private set; }
    public void OnCustomerStartWalk() => WalkingCount++;
    public void OnCustomerStopWalk() => WalkingCount = Mathf.Max(0, WalkingCount - 1);

    // ── Task System ───────────────────────────────────────────────────────────

    private void InitTask()
    {
        _taskGoal = Random.Range(taskMin, taskMax + 1);
        _fedCount = 0;
        _taskComplete = false;
        UpdateTaskUI();

        // Compute the intro line — actual typing starts once the chathead appears (see Start()).
        _introLine = $"Will you help me to serve {_taskGoal} customers?";
        if (introCustomerText != null)
            introCustomerText.text = string.Empty;

        Debug.Log($"[CustomerManager] Task goal: {_taskGoal} customers.");
    }

    private void UpdateTaskUI()
    {
        if (taskCounterText != null)
            taskCounterText.text = $"{_fedCount}/{_taskGoal}";
    }

    private void ShowCongratsPanel()
    {
        if (congratsPanel == null) return;

        congratsPanel.transform.localScale = Vector3.zero;
        congratsPanel.SetActive(true);
        congratsPanel.transform.SetAsLastSibling();
        congratsPanel.transform.DOScale(Vector3.one, 0.35f)
            .SetEase(Ease.OutBack)
            .SetUpdate(true);

        Debug.Log($"[CustomerManager] Task complete! Fed {_fedCount}/{_taskGoal} customers.");
    }

    private void OnRestartClicked()
    {
        if (congratsPanel != null)
        {
            congratsPanel.transform.DOScale(Vector3.zero, 0.25f)
                .SetEase(Ease.InBack)
                .SetUpdate(true)
                .OnComplete(() =>
                {
                    congratsPanel.SetActive(false);
                    RestartGame();
                });
        }
        else
        {
            RestartGame();
        }
    }

    private void RestartGame()
    {
        InitTask();

        // Send any currently-active customers home immediately, unsubscribing their events.
        for (int i = 0; i < _slotOccupant.Length; i++)
        {
            CustomerController c = _slotOccupant[i];
            if (c == null) continue;

            c.OnServed -= OnCustomerServed;
            c.OnLeft -= OnCustomerLeft;
            c.gameObject.SetActive(false);
            _slotOccupant[i] = null;
        }

        // Rebuild the waiting pool from scratch.
        _waiting.Clear();
        _active.Clear();
        _waiting.AddRange(_pool);
        Shuffle(_waiting);

        // Start spawning again.
        StartCoroutine(InitialSpawnRoutine());
    }
}