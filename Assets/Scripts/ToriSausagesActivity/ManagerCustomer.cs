////using System.Collections;
////using System.Collections.Generic;
////using DG.Tweening;
////using TMPro;
////using UnityEngine;
////using UnityEngine.UI;

////namespace ToriSausages
////{
////    [DefaultExecutionOrder(-50)]
////    public class ManagerCustomer : MonoBehaviour
////    {
////        public static ManagerCustomer Instance { get; private set; }

////        [Header("3 Customers")]
////        public ControllerCustomer customer1;
////        public ControllerCustomer customer2;
////        public ControllerCustomer customer3;

////        [Header("2 Counter Slots")]
////        public RectTransform[] slotPoints = new RectTransform[2];
////        public ControllerPlate[] slotPlates = new ControllerPlate[2];

////        [Header("Spawn Timing (seconds)")]
////        public float firstSpawnDelay = 0.5f;
////        public float secondSpawnDelay = 2f;
////        public float reSpawnDelay = 2f;

////        [Header("Mouth Detection")]
////        public float mouthDetectionRadius = 90f;

////        [Header("Intro Chathead Popup")]
////        [Tooltip("Set INACTIVE in the Editor (only this GameObject — its parent must remain active). " +
////                 "Shown on Start, blocks dragging and customer spawn until OK is pressed.")]
////        public GameObject introChathead;
////        public TMP_Text introMessageText;
////        public Button introOkButton;
////        [Tooltip("Optional separate GameObject for the OK button if it needs its own toggle.")]
////        public GameObject introOkButtonRoot;

////        [TextArea]
////        public string introMessage = "Welcome! Cook some sausages for the customers!";

////        [Header("Intro Typing")]
////        public float introCharsPerSecond = 25f;
////        public string introTypingSfxKey = "Typing";

////        [Header("Task System")]
////        [Tooltip("Text showing progress e.g. 2/7. Assign a TMP_Text in the Inspector.")]
////        public TMP_Text taskCounterText;
////        [Tooltip("Panel shown when all customers are fed. Set INACTIVE in Editor.")]
////        public GameObject congratsPanel;
////        [Tooltip("Restart button inside the congrats panel.")]
////        public Button restartButton;
////        [Tooltip("Min customers to feed per round.")]
////        public int taskMin = 5;
////        [Tooltip("Max customers to feed per round.")]
////        public int taskMax = 10;

////        // ── Internal state ────────────────────────────────────────────────────────

////        private ControllerCustomer[] _slotOccupant = new ControllerCustomer[2];
////        private readonly List<ControllerCustomer> _waiting = new List<ControllerCustomer>();
////        private readonly List<ControllerCustomer> _active = new List<ControllerCustomer>();

////        private int _taskGoal;
////        private int _fedCount;
////        private bool _taskComplete;

////        // ── Unity ─────────────────────────────────────────────────────────────────

////        private void Awake()
////        {
////            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
////            Instance = this;

////            if (introOkButton != null) introOkButton.onClick.AddListener(OnIntroOkClicked);
////            if (restartButton != null) restartButton.onClick.AddListener(OnRestartClicked);
////        }

////        private void Start()
////        {
////            var pool = new List<ControllerCustomer>();
////            if (customer1 != null) pool.Add(customer1);
////            if (customer2 != null) pool.Add(customer2);
////            if (customer3 != null) pool.Add(customer3);

////            Shuffle(pool);
////            _waiting.AddRange(pool);

////            InitTask();

////            if (congratsPanel != null) congratsPanel.SetActive(false);

////            if (introChathead != null) ShowIntroChathead();
////            else Invoke(nameof(SpawnNextIntoSlot0), firstSpawnDelay);
////        }

////        // ── Intro Chathead Popup ─────────────────────────────────────────────────────────

////        private void ShowIntroChathead()
////        {
////            ControllerStove.InputLocked = true;

////            if (introMessageText != null) introMessageText.text = "";

////            // Start at zero scale, then activate.
////            introChathead.transform.localScale = Vector3.zero;
////            introChathead.SetActive(true);
////            introChathead.transform.SetAsLastSibling();

////            // Zoom in first — typing (and SFX) only starts after chathead is visible.
////            introChathead.transform.DOScale(Vector3.one, 0.3f)
////                .SetEase(Ease.OutBack)
////                .SetDelay(0.5f)
////                .SetUpdate(true)
////                .OnComplete(() => StartCoroutine(TypeIntroMessageRoutine()));
////        }

////        private IEnumerator TypeIntroMessageRoutine()
////        {
////            if (introMessageText != null)
////            {
////                introMessageText.text = "";
////                SoundManager.Instance?.PlaySFXLoop(introTypingSfxKey);

////                float delay = introCharsPerSecond > 0f ? 1f / introCharsPerSecond : 0f;
////                for (int i = 0; i < introMessage.Length; i++)
////                {
////                    introMessageText.text += introMessage[i];
////                    yield return new WaitForSecondsRealtime(delay);
////                }

////                SoundManager.Instance?.StopSFXLoop();
////            }

////            // Typing done — reveal OK button.
////            // Alpha is set to 0 BEFORE SetActive so there is no one-frame flicker.
////            if (introOkButton != null)
////            {
////                CanvasGroup okCg = introOkButton.gameObject.GetComponent<CanvasGroup>();
////                if (okCg == null) okCg = introOkButton.gameObject.AddComponent<CanvasGroup>();
////                okCg.alpha = 0f;
////                okCg.interactable = true;
////                okCg.blocksRaycasts = true;
////                introOkButton.gameObject.SetActive(true);
////                okCg.DOFade(1f, 0.25f).SetUpdate(true);
////            }
////        }
////        private void OnIntroOkClicked()
////        {
////            ControllerStove.InputLocked = false;

////            // Zoom the chathead (with all its children — text + OK button) out to zero, then hide.
////            if (introChathead != null)
////            {
////                introChathead.transform.DOScale(Vector3.zero, 0.3f)
////                    .SetEase(Ease.InBack)
////                    .SetUpdate(true)
////                    .OnComplete(() => introChathead.SetActive(false));
////            }

////            Invoke(nameof(SpawnNextIntoSlot0), firstSpawnDelay);
////        }

////        // ── Registration ──────────────────────────────────────────────────────────

////        public void Register(ControllerCustomer c)
////        {
////            if (c != null && !_active.Contains(c)) _active.Add(c);
////        }

////        public void Unregister(ControllerCustomer c) => _active.Remove(c);

////        // ── Slot management ───────────────────────────────────────────────────────

////        /// <summary>
////        /// Called by ControllerCustomer immediately when all 3 food slots are eaten.
////        /// Updates the task counter — happens before the walk-off animation.
////        /// </summary>
////        public void NotifyServed(ControllerCustomer customer)
////        {
////            _fedCount++;
////            UpdateTaskUI();

////            if (!_taskComplete && _fedCount >= _taskGoal)
////            {
////                _taskComplete = true;
////                Invoke(nameof(ShowCongratsPanel), 0.5f);
////            }
////        }

////        /// <summary>
////        /// Called by ControllerCustomer after it has walked off-screen and deactivated.
////        /// Frees the slot and spawns the next customer (unless task is complete).
////        /// </summary>
////        public void NotifyLeft(ControllerCustomer customer)
////        {
////            for (int i = 0; i < 2; i++)
////            {
////                if (_slotOccupant[i] != customer) continue;
////                _slotOccupant[i] = null;

////                if (slotPlates != null && i < slotPlates.Length && slotPlates[i] != null)
////                    slotPlates[i].ownerCustomer = null;

////                Debug.Log($"[ManagerCustomer] Slot {i} freed by '{customer.name}'.");
////                break;
////            }

////            if (_taskComplete) return; // task done — no more spawning

////            EnqueueRandom(customer);
////            Invoke(nameof(SpawnNext), reSpawnDelay);
////        }

////        // ── Spawning ──────────────────────────────────────────────────────────────

////        private void SpawnNextIntoSlot0()
////        {
////            SpawnIntoSlot(0);

////            if (_slotOccupant[0] != null)
////                _slotOccupant[0].OnArrived += OnFirstArrived;
////        }

////        private void OnFirstArrived()
////        {
////            if (_slotOccupant[0] != null)
////                _slotOccupant[0].OnArrived -= OnFirstArrived;

////            Invoke(nameof(SpawnIntoSlot1), secondSpawnDelay);
////        }

////        private void SpawnIntoSlot1() => SpawnIntoSlot(1);

////        private void SpawnNext()
////        {
////            int free = GetFreeSlot();
////            if (free >= 0) SpawnIntoSlot(free);
////        }

////        private void SpawnIntoSlot(int slotIndex)
////        {
////            if (_waiting.Count == 0) return;
////            if (slotIndex < 0 || slotIndex >= 2) return;
////            if (_slotOccupant[slotIndex] != null) return;

////            ControllerCustomer next = _waiting[0];
////            _waiting.RemoveAt(0);

////            _slotOccupant[slotIndex] = next;

////            next.AssignSlot(slotPoints[slotIndex], slotIndex);

////            if (slotPlates != null && slotIndex < slotPlates.Length && slotPlates[slotIndex] != null)
////            {
////                next.AssignOwnedPlate(slotPlates[slotIndex]);
////                slotPlates[slotIndex].ownerCustomer = next;
////            }

////            ApplySlotSortOrder(slotIndex, next);

////            next.StartWalking();
////            Debug.Log($"[ManagerCustomer] '{next.name}' → slot {slotIndex}.");
////        }

////        /// <summary>
////        /// Slot 0 = front (renders above), slot 1 = back (renders below).
////        /// </summary>
////        private void ApplySlotSortOrder(int slotIndex, ControllerCustomer customer)
////        {
////            Transform t = customer.transform;

////            if (slotIndex == 0)
////            {
////                // Front slot — render above the back-slot occupant if present.
////                ControllerCustomer other = _slotOccupant[1];
////                if (other != null && other != customer && other.transform.parent == t.parent)
////                    t.SetSiblingIndex(other.transform.GetSiblingIndex() + 1);
////                else
////                    t.SetAsLastSibling();
////            }
////            else
////            {
////                // Back slot — render below the front-slot occupant if present.
////                ControllerCustomer other = _slotOccupant[0];
////                if (other != null && other != customer && other.transform.parent == t.parent)
////                {
////                    int otherIndex = other.transform.GetSiblingIndex();
////                    int myIndex = t.GetSiblingIndex();
////                    t.SetSiblingIndex(myIndex < otherIndex ? myIndex : otherIndex);
////                }
////                else
////                    t.SetAsFirstSibling();
////            }
////        }

////        private int GetFreeSlot()
////        {
////            for (int i = 0; i < 2; i++)
////                if (_slotOccupant[i] == null) return i;
////            return -1;
////        }

////        // ── Helpers ───────────────────────────────────────────────────────────────

////        private void EnqueueRandom(ControllerCustomer c)
////        {
////            if (c == null) return;
////            int index = _waiting.Count == 0 ? 0 : Random.Range(0, _waiting.Count + 1);
////            _waiting.Insert(index, c);
////        }

////        private static void Shuffle(List<ControllerCustomer> list)
////        {
////            for (int i = list.Count - 1; i > 0; i--)
////            {
////                int j = Random.Range(0, i + 1);
////                (list[i], list[j]) = (list[j], list[i]);
////            }
////        }

////        // ── Mouth zone proximity lookup ───────────────────────────────────────────

////        public (ControllerCustomer customer, float dist) GetNearestMouth(Vector2 screenPos, Camera cam)
////        {
////            ControllerCustomer best = null;
////            float bestDist = mouthDetectionRadius;

////            foreach (var c in _active)
////            {
////                if (c == null || !c.HasArrived || c.mouthPoint == null) continue;
////                Vector2 ms = RectTransformUtility.WorldToScreenPoint(cam, c.mouthPoint.position);
////                float dist = Vector2.Distance(screenPos, ms);
////                if (dist < bestDist) { bestDist = dist; best = c; }
////            }

////            return (best, bestDist);
////        }

////        // ── Task system ───────────────────────────────────────────────────────────

////        private void InitTask()
////        {
////            _taskGoal = Random.Range(taskMin, taskMax + 1);
////            _fedCount = 0;
////            _taskComplete = false;
////            UpdateTaskUI();
////            Debug.Log($"[ManagerCustomer] Task goal: {_taskGoal} customers.");
////        }

////        private void UpdateTaskUI()
////        {
////            if (taskCounterText != null)
////                taskCounterText.text = $"{_fedCount}/{_taskGoal}";
////        }

////        private void ShowCongratsPanel()
////        {
////            ControllerStove.InputLocked = true;

////            if (congratsPanel != null)
////            {
////                congratsPanel.transform.localScale = Vector3.zero;
////                congratsPanel.SetActive(true);
////                congratsPanel.transform.SetAsLastSibling();
////                congratsPanel.transform.DOScale(Vector3.one, 0.35f)
////                    .SetEase(Ease.OutBack)
////                    .SetUpdate(true);
////            }

////            Debug.Log($"[ManagerCustomer] Task complete! Fed {_fedCount}/{_taskGoal} customers.");
////        }

////        private void OnRestartClicked()
////        {
////            // Zoom congrats panel out.
////            if (congratsPanel != null)
////            {
////                congratsPanel.transform.DOScale(Vector3.zero, 0.25f)
////                    .SetEase(Ease.InBack)
////                    .SetUpdate(true)
////                    .OnComplete(() =>
////                    {
////                        congratsPanel.SetActive(false);
////                        RestartGame();
////                    });
////            }
////            else
////            {
////                RestartGame();
////            }
////        }

////        private void RestartGame()
////        {
////            // Reset task counters with a new random goal.
////            InitTask();

////            // Unlock input.
////            ControllerStove.InputLocked = false;

////            // Clear slot occupants.
////            for (int i = 0; i < 2; i++)
////            {
////                if (_slotOccupant[i] != null)
////                {
////                    _slotOccupant[i].gameObject.SetActive(false);
////                    _slotOccupant[i] = null;
////                }
////                if (slotPlates != null && i < slotPlates.Length && slotPlates[i] != null)
////                    slotPlates[i].ownerCustomer = null;
////            }

////            // Put all customers back in the waiting pool.
////            _waiting.Clear();
////            _active.Clear();
////            var pool = new List<ControllerCustomer>();
////            if (customer1 != null) pool.Add(customer1);
////            if (customer2 != null) pool.Add(customer2);
////            if (customer3 != null) pool.Add(customer3);
////            Shuffle(pool);
////            _waiting.AddRange(pool);

////            // Start spawning again.
////            Invoke(nameof(SpawnNextIntoSlot0), firstSpawnDelay);
////        }
////    }
////}

//using System.Collections;
//using System.Collections.Generic;
//using DG.Tweening;
//using TMPro;
//using UnityEngine;
//using UnityEngine.UI;

//namespace ToriSausages
//{
//    [DefaultExecutionOrder(-50)]
//    public class ManagerCustomer : MonoBehaviour
//    {
//        public static ManagerCustomer Instance { get; private set; }

//        [Header("3 Customers")]
//        public ControllerCustomer customer1;
//        public ControllerCustomer customer2;
//        public ControllerCustomer customer3;

//        [Header("2 Counter Slots")]
//        public RectTransform[] slotPoints = new RectTransform[2];
//        public ControllerPlate[] slotPlates = new ControllerPlate[2];

//        [Header("Spawn Timing (seconds)")]
//        public float firstSpawnDelay = 0.5f;
//        public float secondSpawnDelay = 2f;
//        public float reSpawnDelay = 2f;

//        [Header("Mouth Detection")]
//        public float mouthDetectionRadius = 90f;

//        [Header("Intro Chathead Popup")]
//        [Tooltip("Set INACTIVE in the Editor (only this GameObject — its parent must remain active). " +
//                 "Shown on Start, blocks dragging and customer spawn until OK is pressed.")]
//        public GameObject introChathead;
//        public TMP_Text introMessageText;
//        public Button introOkButton;
//        [Tooltip("Optional separate GameObject for the OK button if it needs its own toggle.")]
//        public GameObject introOkButtonRoot;

//        [TextArea]
//        public string introMessage = "Welcome! Cook some sausages for the customers!";

//        [Header("Intro Typing")]
//        public float introCharsPerSecond = 25f;
//        public string introTypingSfxKey = "Typing";

//        [Header("Task System")]
//        [Tooltip("Text showing progress e.g. 2/7. Assign a TMP_Text in the Inspector.")]
//        public TMP_Text taskCounterText;
//        [Tooltip("Panel shown when all customers are fed. Set INACTIVE in Editor.")]
//        public GameObject congratsPanel;
//        [Tooltip("Restart button inside the congrats panel.")]
//        public Button restartButton;
//        [Tooltip("Min customers to feed per round.")]
//        public int taskMin = 5;
//        [Tooltip("Max customers to feed per round.")]
//        public int taskMax = 10;

//        // ── Internal state ────────────────────────────────────────────────────────

//        private ControllerCustomer[] _slotOccupant = new ControllerCustomer[2];
//        private readonly List<ControllerCustomer> _waiting = new List<ControllerCustomer>();
//        private readonly List<ControllerCustomer> _active = new List<ControllerCustomer>();

//        private int _taskGoal;
//        private int _fedCount;
//        private bool _taskComplete;

//        // ── Unity ─────────────────────────────────────────────────────────────────

//        private void Awake()
//        {
//            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
//            Instance = this;

//            if (introOkButton != null) introOkButton.onClick.AddListener(OnIntroOkClicked);
//            if (restartButton != null) restartButton.onClick.AddListener(OnRestartClicked);
//        }

//        private void Start()
//        {
//            var pool = new List<ControllerCustomer>();
//            if (customer1 != null) pool.Add(customer1);
//            if (customer2 != null) pool.Add(customer2);
//            if (customer3 != null) pool.Add(customer3);

//            Shuffle(pool);
//            _waiting.AddRange(pool);

//            InitTask();

//            if (congratsPanel != null) congratsPanel.SetActive(false);

//            if (introChathead != null) ShowIntroChathead();
//            else Invoke(nameof(SpawnNextIntoSlot0), firstSpawnDelay);
//        }

//        // ── Intro Chathead Popup ─────────────────────────────────────────────────────────

//        private void ShowIntroChathead()
//        {
//            ControllerStove.InputLocked = true;

//            if (introMessageText != null) introMessageText.text = "";

//            // Start at zero scale, then activate.
//            introChathead.transform.localScale = Vector3.zero;
//            introChathead.SetActive(true);
//            introChathead.transform.SetAsLastSibling();

//            // Zoom in first — typing (and SFX) only starts after chathead is visible.
//            introChathead.transform.DOScale(Vector3.one, 0.3f)
//                .SetEase(Ease.OutBack)
//                .SetDelay(0.5f)
//                .SetUpdate(true)
//                .OnComplete(() => StartCoroutine(TypeIntroMessageRoutine()));
//        }

//        private IEnumerator TypeIntroMessageRoutine()
//        {
//            if (introMessageText != null)
//            {
//                introMessageText.text = "";
//                SoundManager.Instance?.PlaySFXLoop(introTypingSfxKey);

//                float delay = introCharsPerSecond > 0f ? 1f / introCharsPerSecond : 0f;
//                for (int i = 0; i < introMessage.Length; i++)
//                {
//                    introMessageText.text += introMessage[i];
//                    yield return new WaitForSecondsRealtime(delay);
//                }

//                SoundManager.Instance?.StopSFXLoop();
//            }

//            // Typing done — reveal OK button.
//            // Alpha is set to 0 BEFORE SetActive so there is no one-frame flicker.
//            if (introOkButton != null)
//            {
//                CanvasGroup okCg = introOkButton.gameObject.GetComponent<CanvasGroup>();
//                if (okCg == null) okCg = introOkButton.gameObject.AddComponent<CanvasGroup>();
//                okCg.alpha = 0f;
//                okCg.interactable = true;
//                okCg.blocksRaycasts = true;
//                introOkButton.gameObject.SetActive(true);
//                okCg.DOFade(1f, 0.25f).SetUpdate(true);
//            }
//        }
//        private void OnIntroOkClicked()
//        {
//            ControllerStove.InputLocked = false;

//            // Zoom the chathead (with all its children — text + OK button) out to zero, then hide.
//            if (introChathead != null)
//            {
//                introChathead.transform.DOScale(Vector3.zero, 0.3f)
//                    .SetEase(Ease.InBack)
//                    .SetUpdate(true)
//                    .OnComplete(() => introChathead.SetActive(false));
//            }

//            Invoke(nameof(SpawnNextIntoSlot0), firstSpawnDelay);
//        }

//        // ── Registration ──────────────────────────────────────────────────────────

//        public void Register(ControllerCustomer c)
//        {
//            if (c != null && !_active.Contains(c)) _active.Add(c);
//        }

//        public void Unregister(ControllerCustomer c) => _active.Remove(c);

//        // ── Slot management ───────────────────────────────────────────────────────

//        /// <summary>
//        /// Called by ControllerCustomer immediately when all 3 food slots are eaten.
//        /// Updates the task counter — happens before the walk-off animation.
//        /// </summary>
//        public void NotifyServed(ControllerCustomer customer)
//        {
//            _fedCount++;
//            UpdateTaskUI();

//            if (!_taskComplete && _fedCount >= _taskGoal)
//            {
//                _taskComplete = true;
//                Invoke(nameof(ShowCongratsPanel), 0.5f);
//            }
//        }

//        /// <summary>
//        /// Called by ControllerCustomer after it has walked off-screen and deactivated.
//        /// Frees the slot and spawns the next customer (unless task is complete).
//        /// </summary>
//        public void NotifyLeft(ControllerCustomer customer)
//        {
//            for (int i = 0; i < 2; i++)
//            {
//                if (_slotOccupant[i] != customer) continue;
//                _slotOccupant[i] = null;

//                if (slotPlates != null && i < slotPlates.Length && slotPlates[i] != null)
//                    slotPlates[i].ownerCustomer = null;

//                Debug.Log($"[ManagerCustomer] Slot {i} freed by '{customer.name}'.");
//                break;
//            }

//            if (_taskComplete) return; // task done — no more spawning

//            EnqueueRandom(customer);
//            Invoke(nameof(SpawnNext), reSpawnDelay);
//        }

//        // ── Spawning ──────────────────────────────────────────────────────────────

//        private void SpawnNextIntoSlot0()
//        {
//            SpawnIntoSlot(0);

//            if (_slotOccupant[0] != null)
//                _slotOccupant[0].OnArrived += OnFirstArrived;
//        }

//        private void OnFirstArrived()
//        {
//            if (_slotOccupant[0] != null)
//                _slotOccupant[0].OnArrived -= OnFirstArrived;

//            Invoke(nameof(SpawnIntoSlot1), secondSpawnDelay);
//        }

//        private void SpawnIntoSlot1() => SpawnIntoSlot(1);

//        private void SpawnNext()
//        {
//            int free = GetFreeSlot();
//            if (free >= 0) SpawnIntoSlot(free);
//        }

//        private void SpawnIntoSlot(int slotIndex)
//        {
//            if (_waiting.Count == 0) return;
//            if (slotIndex < 0 || slotIndex >= 2) return;
//            if (_slotOccupant[slotIndex] != null) return;

//            ControllerCustomer next = _waiting[0];
//            _waiting.RemoveAt(0);

//            _slotOccupant[slotIndex] = next;

//            next.AssignSlot(slotPoints[slotIndex], slotIndex);

//            if (slotPlates != null && slotIndex < slotPlates.Length && slotPlates[slotIndex] != null)
//            {
//                next.AssignOwnedPlate(slotPlates[slotIndex]);
//                slotPlates[slotIndex].ownerCustomer = next;
//            }

//            ApplySlotSortOrder(slotIndex, next);

//            next.StartWalking();
//            Debug.Log($"[ManagerCustomer] '{next.name}' → slot {slotIndex}.");
//        }

//        /// <summary>
//        /// Slot 0 = front (renders above), slot 1 = back (renders below).
//        /// </summary>
//        private void ApplySlotSortOrder(int slotIndex, ControllerCustomer customer)
//        {
//            Transform t = customer.transform;

//            if (slotIndex == 0)
//            {
//                // Front slot — render above the back-slot occupant if present.
//                ControllerCustomer other = _slotOccupant[1];
//                if (other != null && other != customer && other.transform.parent == t.parent)
//                    t.SetSiblingIndex(other.transform.GetSiblingIndex() + 1);
//                else
//                    t.SetAsLastSibling();
//            }
//            else
//            {
//                // Back slot — render below the front-slot occupant if present.
//                ControllerCustomer other = _slotOccupant[0];
//                if (other != null && other != customer && other.transform.parent == t.parent)
//                {
//                    int otherIndex = other.transform.GetSiblingIndex();
//                    int myIndex = t.GetSiblingIndex();
//                    t.SetSiblingIndex(myIndex < otherIndex ? myIndex : otherIndex);
//                }
//                else
//                    t.SetAsFirstSibling();
//            }
//        }

//        private int GetFreeSlot()
//        {
//            for (int i = 0; i < 2; i++)
//                if (_slotOccupant[i] == null) return i;
//            return -1;
//        }

//        // ── Helpers ───────────────────────────────────────────────────────────────

//        private void EnqueueRandom(ControllerCustomer c)
//        {
//            if (c == null) return;
//            int index = _waiting.Count == 0 ? 0 : Random.Range(0, _waiting.Count + 1);
//            _waiting.Insert(index, c);
//        }

//        private static void Shuffle(List<ControllerCustomer> list)
//        {
//            for (int i = list.Count - 1; i > 0; i--)
//            {
//                int j = Random.Range(0, i + 1);
//                (list[i], list[j]) = (list[j], list[i]);
//            }
//        }

//        // ── Mouth zone proximity lookup ───────────────────────────────────────────

//        public (ControllerCustomer customer, float dist) GetNearestMouth(Vector2 screenPos, Camera cam)
//        {
//            ControllerCustomer best = null;
//            float bestDist = mouthDetectionRadius;

//            foreach (var c in _active)
//            {
//                if (c == null || !c.HasArrived || c.mouthPoint == null) continue;
//                Vector2 ms = RectTransformUtility.WorldToScreenPoint(cam, c.mouthPoint.position);
//                float dist = Vector2.Distance(screenPos, ms);
//                if (dist < bestDist) { bestDist = dist; best = c; }
//            }

//            return (best, bestDist);
//        }

//        // ── Task system ───────────────────────────────────────────────────────────

//        private void InitTask()
//        {
//            _taskGoal = Random.Range(taskMin, taskMax + 1);
//            _fedCount = 0;
//            _taskComplete = false;
//            UpdateTaskUI();

//            // Overwrite the intro message with the actual customer count so
//            // the typing effect shows "Will you help me to serve X customers?"
//            introMessage = $"Will you help me to serve {_taskGoal} customers?";

//            Debug.Log($"[ManagerCustomer] Task goal: {_taskGoal} customers.");
//        }

//        private void UpdateTaskUI()
//        {
//            if (taskCounterText != null)
//                taskCounterText.text = $"{_fedCount}/{_taskGoal}";
//        }

//        private void ShowCongratsPanel()
//        {
//            ControllerStove.InputLocked = true;

//            if (congratsPanel != null)
//            {
//                congratsPanel.transform.localScale = Vector3.zero;
//                congratsPanel.SetActive(true);
//                congratsPanel.transform.SetAsLastSibling();
//                congratsPanel.transform.DOScale(Vector3.one, 0.35f)
//                    .SetEase(Ease.OutBack)
//                    .SetUpdate(true);
//            }

//            Debug.Log($"[ManagerCustomer] Task complete! Fed {_fedCount}/{_taskGoal} customers.");
//        }

//        private void OnRestartClicked()
//        {
//            // Zoom congrats panel out.
//            if (congratsPanel != null)
//            {
//                congratsPanel.transform.DOScale(Vector3.zero, 0.25f)
//                    .SetEase(Ease.InBack)
//                    .SetUpdate(true)
//                    .OnComplete(() =>
//                    {
//                        congratsPanel.SetActive(false);
//                        RestartGame();
//                    });
//            }
//            else
//            {
//                RestartGame();
//            }
//        }

//        private void RestartGame()
//        {
//            // Reset task counters with a new random goal.
//            InitTask();

//            // Unlock input.
//            ControllerStove.InputLocked = false;

//            // Clear slot occupants.
//            for (int i = 0; i < 2; i++)
//            {
//                if (_slotOccupant[i] != null)
//                {
//                    _slotOccupant[i].gameObject.SetActive(false);
//                    _slotOccupant[i] = null;
//                }
//                if (slotPlates != null && i < slotPlates.Length && slotPlates[i] != null)
//                    slotPlates[i].ownerCustomer = null;
//            }

//            // Put all customers back in the waiting pool.
//            _waiting.Clear();
//            _active.Clear();
//            var pool = new List<ControllerCustomer>();
//            if (customer1 != null) pool.Add(customer1);
//            if (customer2 != null) pool.Add(customer2);
//            if (customer3 != null) pool.Add(customer3);
//            Shuffle(pool);
//            _waiting.AddRange(pool);

//            // Start spawning again.
//            Invoke(nameof(SpawnNextIntoSlot0), firstSpawnDelay);
//        }
//    }
//}

//using System.Collections;
//using System.Collections.Generic;
//using DG.Tweening;
//using TMPro;
//using UnityEngine;
//using UnityEngine.UI;

//namespace ToriSausages
//{
//    [DefaultExecutionOrder(-50)]
//    public class ManagerCustomer : MonoBehaviour
//    {
//        public static ManagerCustomer Instance { get; private set; }

//        [Header("3 Customers")]
//        public ControllerCustomer customer1;
//        public ControllerCustomer customer2;
//        public ControllerCustomer customer3;

//        [Header("2 Counter Slots")]
//        public RectTransform[] slotPoints = new RectTransform[2];
//        public ControllerPlate[] slotPlates = new ControllerPlate[2];

//        [Header("Spawn Timing (seconds)")]
//        public float firstSpawnDelay = 0.5f;
//        public float secondSpawnDelay = 2f;
//        public float reSpawnDelay = 2f;

//        [Header("Mouth Detection")]
//        public float mouthDetectionRadius = 90f;

//        [Header("Intro Chathead Popup")]
//        [Tooltip("Set INACTIVE in the Editor (only this GameObject — its parent must remain active). " +
//                 "Shown on Start, blocks dragging and customer spawn until OK is pressed.")]
//        public GameObject introChathead;
//        public TMP_Text introMessageText;
//        public Button introOkButton;
//        [Tooltip("Optional separate GameObject for the OK button if it needs its own toggle.")]
//        public GameObject introOkButtonRoot;

//        [TextArea]
//        public string introMessage = "Welcome! Cook some sausages for the customers!";

//        [Header("Intro Typing")]
//        public float introCharsPerSecond = 25f;
//        public string introTypingSfxKey = "Typing";

//        [Header("Task System")]
//        [Tooltip("Text showing progress e.g. 2/7. Assign a TMP_Text in the Inspector.")]
//        public TMP_Text taskCounterText;
//        [Tooltip("Panel shown when all customers are fed. Set INACTIVE in Editor.")]
//        public GameObject congratsPanel;
//        [Tooltip("Restart button inside the congrats panel.")]
//        public Button restartButton;
//        [Tooltip("Min customers to feed per round.")]
//        public int taskMin = 5;
//        [Tooltip("Max customers to feed per round.")]
//        public int taskMax = 10;

//        // ── Internal state ────────────────────────────────────────────────────────

//        private ControllerCustomer[] _slotOccupant = new ControllerCustomer[2];
//        private readonly List<ControllerCustomer> _waiting = new List<ControllerCustomer>();
//        private readonly List<ControllerCustomer> _active = new List<ControllerCustomer>();

//        private int _taskGoal;
//        private int _fedCount;
//        private bool _taskComplete;

//        // ── Unity ─────────────────────────────────────────────────────────────────

//        private void Awake()
//        {
//            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
//            Instance = this;

//            if (introOkButton != null) introOkButton.onClick.AddListener(OnIntroOkClicked);
//            if (restartButton != null) restartButton.onClick.AddListener(OnRestartClicked);
//        }

//        private void Start()
//        {
//            var pool = new List<ControllerCustomer>();
//            if (customer1 != null) pool.Add(customer1);
//            if (customer2 != null) pool.Add(customer2);
//            if (customer3 != null) pool.Add(customer3);

//            Shuffle(pool);
//            _waiting.AddRange(pool);

//            InitTask();

//            if (congratsPanel != null) congratsPanel.SetActive(false);

//            if (introChathead != null) ShowIntroChathead();
//            else Invoke(nameof(SpawnNextIntoSlot0), firstSpawnDelay);
//        }

//        // ── Intro Chathead Popup ─────────────────────────────────────────────────────────

//        private void ShowIntroChathead()
//        {
//            ControllerStove.InputLocked = true;

//            if (introMessageText != null) introMessageText.text = "";

//            // Start at zero scale, then activate.
//            introChathead.transform.localScale = Vector3.zero;
//            introChathead.SetActive(true);
//            introChathead.transform.SetAsLastSibling();

//            // Zoom in first — typing (and SFX) only starts after chathead is visible.
//            introChathead.transform.DOScale(Vector3.one, 0.3f)
//                .SetEase(Ease.OutBack)
//                .SetDelay(0.5f)
//                .SetUpdate(true)
//                .OnComplete(() => StartCoroutine(TypeIntroMessageRoutine()));
//        }

//        private IEnumerator TypeIntroMessageRoutine()
//        {
//            if (introMessageText != null)
//            {
//                introMessageText.text = "";
//                SoundManager.Instance?.PlaySFXLoop(introTypingSfxKey);

//                float delay = introCharsPerSecond > 0f ? 1f / introCharsPerSecond : 0f;
//                for (int i = 0; i < introMessage.Length; i++)
//                {
//                    introMessageText.text += introMessage[i];
//                    yield return new WaitForSecondsRealtime(delay);
//                }

//                SoundManager.Instance?.StopSFXLoop();
//            }

//            // Typing done — reveal OK button.
//            // Alpha is set to 0 BEFORE SetActive so there is no one-frame flicker.
//            if (introOkButton != null)
//            {
//                CanvasGroup okCg = introOkButton.gameObject.GetComponent<CanvasGroup>();
//                if (okCg == null) okCg = introOkButton.gameObject.AddComponent<CanvasGroup>();
//                okCg.alpha = 0f;
//                okCg.interactable = true;
//                okCg.blocksRaycasts = true;
//                introOkButton.gameObject.SetActive(true);
//                okCg.DOFade(1f, 0.25f).SetUpdate(true);
//            }
//        }
//        private void OnIntroOkClicked()
//        {
//            ControllerStove.InputLocked = false;

//            // Zoom the chathead (with all its children — text + OK button) out to zero, then hide.
//            if (introChathead != null)
//            {
//                introChathead.transform.DOScale(Vector3.zero, 0.3f)
//                    .SetEase(Ease.InBack)
//                    .SetUpdate(true)
//                    .OnComplete(() => introChathead.SetActive(false));
//            }

//            Invoke(nameof(SpawnNextIntoSlot0), firstSpawnDelay);
//        }

//        // ── Registration ──────────────────────────────────────────────────────────

//        public void Register(ControllerCustomer c)
//        {
//            if (c != null && !_active.Contains(c)) _active.Add(c);
//        }

//        public void Unregister(ControllerCustomer c) => _active.Remove(c);

//        // ── Slot management ───────────────────────────────────────────────────────

//        /// <summary>
//        /// Called by ControllerCustomer immediately when all 3 food slots are eaten.
//        /// Updates the task counter — happens before the walk-off animation.
//        /// </summary>
//        public void NotifyServed(ControllerCustomer customer)
//        {
//            _fedCount++;
//            UpdateTaskUI();

//            if (!_taskComplete && _fedCount >= _taskGoal)
//            {
//                _taskComplete = true;
//                Invoke(nameof(ShowCongratsPanel), 0.5f);
//            }
//        }

//        /// <summary>
//        /// Called by ControllerCustomer after it has walked off-screen and deactivated.
//        /// Frees the slot and spawns the next customer (unless task is complete).
//        /// </summary>
//        public void NotifyLeft(ControllerCustomer customer)
//        {
//            for (int i = 0; i < 2; i++)
//            {
//                if (_slotOccupant[i] != customer) continue;
//                _slotOccupant[i] = null;

//                if (slotPlates != null && i < slotPlates.Length && slotPlates[i] != null)
//                    slotPlates[i].ownerCustomer = null;

//                Debug.Log($"[ManagerCustomer] Slot {i} freed by '{customer.name}'.");
//                break;
//            }

//            if (_taskComplete) return; // task done — no more spawning

//            EnqueueRandom(customer);
//            Invoke(nameof(SpawnNext), reSpawnDelay);
//        }

//        // ── Spawning ──────────────────────────────────────────────────────────────

//        private void SpawnNextIntoSlot0()
//        {
//            SpawnIntoSlot(0);

//            if (_slotOccupant[0] != null)
//                _slotOccupant[0].OnArrived += OnFirstArrived;
//        }

//        private void OnFirstArrived()
//        {
//            if (_slotOccupant[0] != null)
//                _slotOccupant[0].OnArrived -= OnFirstArrived;

//            Invoke(nameof(SpawnIntoSlot1), secondSpawnDelay);
//        }

//        private void SpawnIntoSlot1() => SpawnIntoSlot(1);

//        private void SpawnNext()
//        {
//            int free = GetFreeSlot();
//            if (free >= 0) SpawnIntoSlot(free);
//        }

//        private void SpawnIntoSlot(int slotIndex)
//        {
//            if (_waiting.Count == 0) return;
//            if (slotIndex < 0 || slotIndex >= 2) return;
//            if (_slotOccupant[slotIndex] != null) return;

//            ControllerCustomer next = _waiting[0];
//            _waiting.RemoveAt(0);

//            _slotOccupant[slotIndex] = next;

//            next.AssignSlot(slotPoints[slotIndex], slotIndex);

//            if (slotPlates != null && slotIndex < slotPlates.Length && slotPlates[slotIndex] != null)
//            {
//                next.AssignOwnedPlate(slotPlates[slotIndex]);
//                slotPlates[slotIndex].ownerCustomer = next;
//            }

//            ApplySlotSortOrder(slotIndex, next);

//            next.StartWalking();
//            Debug.Log($"[ManagerCustomer] '{next.name}' → slot {slotIndex}.");
//        }

//        /// <summary>
//        /// Slot 0 = front (renders above), slot 1 = back (renders below).
//        /// </summary>
//        private void ApplySlotSortOrder(int slotIndex, ControllerCustomer customer)
//        {
//            Transform t = customer.transform;

//            if (slotIndex == 0)
//            {
//                // Front slot — render above the back-slot occupant if present.
//                ControllerCustomer other = _slotOccupant[1];
//                if (other != null && other != customer && other.transform.parent == t.parent)
//                    t.SetSiblingIndex(other.transform.GetSiblingIndex() + 1);
//                else
//                    t.SetAsLastSibling();
//            }
//            else
//            {
//                // Back slot — render below the front-slot occupant if present.
//                ControllerCustomer other = _slotOccupant[0];
//                if (other != null && other != customer && other.transform.parent == t.parent)
//                {
//                    int otherIndex = other.transform.GetSiblingIndex();
//                    int myIndex = t.GetSiblingIndex();
//                    t.SetSiblingIndex(myIndex < otherIndex ? myIndex : otherIndex);
//                }
//                else
//                    t.SetAsFirstSibling();
//            }
//        }

//        private int GetFreeSlot()
//        {
//            for (int i = 0; i < 2; i++)
//                if (_slotOccupant[i] == null) return i;
//            return -1;
//        }

//        // ── Helpers ───────────────────────────────────────────────────────────────

//        private void EnqueueRandom(ControllerCustomer c)
//        {
//            if (c == null) return;
//            int index = _waiting.Count == 0 ? 0 : Random.Range(0, _waiting.Count + 1);
//            _waiting.Insert(index, c);
//        }

//        private static void Shuffle(List<ControllerCustomer> list)
//        {
//            for (int i = list.Count - 1; i > 0; i--)
//            {
//                int j = Random.Range(0, i + 1);
//                (list[i], list[j]) = (list[j], list[i]);
//            }
//        }

//        // ── Mouth zone proximity lookup ───────────────────────────────────────────

//        public (ControllerCustomer customer, float dist) GetNearestMouth(Vector2 screenPos, Camera cam)
//        {
//            ControllerCustomer best = null;
//            float bestDist = mouthDetectionRadius;

//            foreach (var c in _active)
//            {
//                if (c == null || !c.HasArrived || c.mouthPoint == null) continue;
//                Vector2 ms = RectTransformUtility.WorldToScreenPoint(cam, c.mouthPoint.position);
//                float dist = Vector2.Distance(screenPos, ms);
//                if (dist < bestDist) { bestDist = dist; best = c; }
//            }

//            return (best, bestDist);
//        }

//        // ── Task system ───────────────────────────────────────────────────────────

//        private void InitTask()
//        {
//            _taskGoal = Random.Range(taskMin, taskMax + 1);
//            _fedCount = 0;
//            _taskComplete = false;
//            UpdateTaskUI();
//            Debug.Log($"[ManagerCustomer] Task goal: {_taskGoal} customers.");
//        }

//        private void UpdateTaskUI()
//        {
//            if (taskCounterText != null)
//                taskCounterText.text = $"{_fedCount}/{_taskGoal}";
//        }

//        private void ShowCongratsPanel()
//        {
//            ControllerStove.InputLocked = true;

//            if (congratsPanel != null)
//            {
//                congratsPanel.transform.localScale = Vector3.zero;
//                congratsPanel.SetActive(true);
//                congratsPanel.transform.SetAsLastSibling();
//                congratsPanel.transform.DOScale(Vector3.one, 0.35f)
//                    .SetEase(Ease.OutBack)
//                    .SetUpdate(true);
//            }

//            Debug.Log($"[ManagerCustomer] Task complete! Fed {_fedCount}/{_taskGoal} customers.");
//        }

//        private void OnRestartClicked()
//        {
//            // Zoom congrats panel out.
//            if (congratsPanel != null)
//            {
//                congratsPanel.transform.DOScale(Vector3.zero, 0.25f)
//                    .SetEase(Ease.InBack)
//                    .SetUpdate(true)
//                    .OnComplete(() =>
//                    {
//                        congratsPanel.SetActive(false);
//                        RestartGame();
//                    });
//            }
//            else
//            {
//                RestartGame();
//            }
//        }

//        private void RestartGame()
//        {
//            // Reset task counters with a new random goal.
//            InitTask();

//            // Unlock input.
//            ControllerStove.InputLocked = false;

//            // Clear slot occupants.
//            for (int i = 0; i < 2; i++)
//            {
//                if (_slotOccupant[i] != null)
//                {
//                    _slotOccupant[i].gameObject.SetActive(false);
//                    _slotOccupant[i] = null;
//                }
//                if (slotPlates != null && i < slotPlates.Length && slotPlates[i] != null)
//                    slotPlates[i].ownerCustomer = null;
//            }

//            // Put all customers back in the waiting pool.
//            _waiting.Clear();
//            _active.Clear();
//            var pool = new List<ControllerCustomer>();
//            if (customer1 != null) pool.Add(customer1);
//            if (customer2 != null) pool.Add(customer2);
//            if (customer3 != null) pool.Add(customer3);
//            Shuffle(pool);
//            _waiting.AddRange(pool);

//            // Start spawning again.
//            Invoke(nameof(SpawnNextIntoSlot0), firstSpawnDelay);
//        }
//    }
//}

using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ToriSausages
{
    [DefaultExecutionOrder(-50)]
    public class ManagerCustomer : MonoBehaviour
    {
        public static ManagerCustomer Instance { get; private set; }

        [Header("3 Customers")]
        public ControllerCustomer customer1;
        public ControllerCustomer customer2;
        public ControllerCustomer customer3;

        [Header("2 Counter Slots")]
        public RectTransform[] slotPoints = new RectTransform[2];
        public ControllerPlate[] slotPlates = new ControllerPlate[2];

        [Header("Spawn Timing (seconds)")]
        public float firstSpawnDelay = 0.5f;
        public float secondSpawnDelay = 2f;
        public float reSpawnDelay = 2f;

        [Header("Mouth Detection")]
        public float mouthDetectionRadius = 90f;

        [Header("Intro Chathead Popup")]
        [Tooltip("Set INACTIVE in the Editor (only this GameObject — its parent must remain active). " +
                 "Shown on Start, blocks dragging and customer spawn until OK is pressed.")]
        public GameObject introChathead;
        public TMP_Text introMessageText;
        public Button introOkButton;
        [Tooltip("Optional separate GameObject for the OK button if it needs its own toggle.")]
        public GameObject introOkButtonRoot;

        [TextArea]
        public string introMessage = "Welcome! Cook some sausages for the customers!";

        [Header("Intro Typing")]
        public float introCharsPerSecond = 25f;
        public string introTypingSfxKey = "Typing";

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

        // ── Internal state ────────────────────────────────────────────────────────

        private ControllerCustomer[] _slotOccupant = new ControllerCustomer[2];
        private readonly List<ControllerCustomer> _waiting = new List<ControllerCustomer>();
        private readonly List<ControllerCustomer> _active = new List<ControllerCustomer>();

        private int _taskGoal;
        private int _fedCount;
        private bool _taskComplete;

        // ── Unity ─────────────────────────────────────────────────────────────────

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;

            if (introOkButton != null) introOkButton.onClick.AddListener(OnIntroOkClicked);
            if (restartButton != null) restartButton.onClick.AddListener(OnRestartClicked);
        }

        private void Start()
        {
            var pool = new List<ControllerCustomer>();
            if (customer1 != null) pool.Add(customer1);
            if (customer2 != null) pool.Add(customer2);
            if (customer3 != null) pool.Add(customer3);

            Shuffle(pool);
            _waiting.AddRange(pool);

            InitTask();

            if (congratsPanel != null) congratsPanel.SetActive(false);

            if (introChathead != null) ShowIntroChathead();
            else Invoke(nameof(SpawnNextIntoSlot0), firstSpawnDelay);
        }

        // ── Intro Chathead Popup ─────────────────────────────────────────────────────────

        private void ShowIntroChathead()
        {
            ControllerStove.InputLocked = true;

            if (introMessageText != null) introMessageText.text = "";

            // Start at zero scale, then activate.
            introChathead.transform.localScale = Vector3.zero;
            introChathead.SetActive(true);
            introChathead.transform.SetAsLastSibling();

            // Zoom in first — typing (and SFX) only starts after chathead is visible.
            introChathead.transform.DOScale(Vector3.one, 0.3f)
                .SetEase(Ease.OutBack)
                .SetDelay(0.5f)
                .SetUpdate(true)
                .OnComplete(() => StartCoroutine(TypeIntroMessageRoutine()));
        }

        private IEnumerator TypeIntroMessageRoutine()
        {
            if (introMessageText != null)
            {
                introMessageText.text = "";
                SoundManager.Instance?.PlaySFXLoop(introTypingSfxKey);

                float delay = introCharsPerSecond > 0f ? 1f / introCharsPerSecond : 0f;
                for (int i = 0; i < introMessage.Length; i++)
                {
                    introMessageText.text += introMessage[i];
                    yield return new WaitForSecondsRealtime(delay);
                }

                SoundManager.Instance?.StopSFXLoop();
            }

            // Typing done — reveal OK button.
            // Alpha is set to 0 BEFORE SetActive so there is no one-frame flicker.
            if (introOkButton != null)
            {
                CanvasGroup okCg = introOkButton.gameObject.GetComponent<CanvasGroup>();
                if (okCg == null) okCg = introOkButton.gameObject.AddComponent<CanvasGroup>();
                okCg.alpha = 0f;
                okCg.interactable = true;
                okCg.blocksRaycasts = true;
                introOkButton.gameObject.SetActive(true);
                okCg.DOFade(1f, 0.25f).SetUpdate(true);
            }
        }
        private void OnIntroOkClicked()
        {
            ControllerStove.InputLocked = false;

            // Zoom the chathead (with all its children — text + OK button) out to zero, then hide.
            if (introChathead != null)
            {
                introChathead.transform.DOScale(Vector3.zero, 0.3f)
                    .SetEase(Ease.InBack)
                    .SetUpdate(true)
                    .OnComplete(() => introChathead.SetActive(false));
            }

            Invoke(nameof(SpawnNextIntoSlot0), firstSpawnDelay);
        }

        // ── Registration ──────────────────────────────────────────────────────────

        public void Register(ControllerCustomer c)
        {
            if (c != null && !_active.Contains(c)) _active.Add(c);
        }

        public void Unregister(ControllerCustomer c) => _active.Remove(c);

        // ── Slot management ───────────────────────────────────────────────────────

        /// <summary>
        /// Called by ControllerCustomer immediately when all 3 food slots are eaten.
        /// Updates the task counter — happens before the walk-off animation.
        /// </summary>
        public void NotifyServed(ControllerCustomer customer)
        {
            _fedCount++;
            UpdateTaskUI();

            if (!_taskComplete && _fedCount >= _taskGoal)
            {
                _taskComplete = true;
                Invoke(nameof(ShowCongratsPanel), 0.5f);
            }
        }

        /// <summary>
        /// Called by ControllerCustomer after it has walked off-screen and deactivated.
        /// Frees the slot and spawns the next customer (unless task is complete).
        /// </summary>
        public void NotifyLeft(ControllerCustomer customer)
        {
            for (int i = 0; i < 2; i++)
            {
                if (_slotOccupant[i] != customer) continue;
                _slotOccupant[i] = null;

                if (slotPlates != null && i < slotPlates.Length && slotPlates[i] != null)
                    slotPlates[i].ownerCustomer = null;

                Debug.Log($"[ManagerCustomer] Slot {i} freed by '{customer.name}'.");
                break;
            }

            if (_taskComplete) return; // task done — no more spawning

            EnqueueRandom(customer);
            StartCoroutine(DelayedSpawnNext(reSpawnDelay, customer));
        }

        private IEnumerator DelayedSpawnNext(float delay, ControllerCustomer avoid)
        {
            yield return new WaitForSeconds(delay);
            SpawnNext(avoid);
        }

        // ── Spawning ──────────────────────────────────────────────────────────────

        private void SpawnNextIntoSlot0()
        {
            SpawnIntoSlot(0);

            if (_slotOccupant[0] != null)
                _slotOccupant[0].OnArrived += OnFirstArrived;
        }

        private void OnFirstArrived()
        {
            if (_slotOccupant[0] != null)
                _slotOccupant[0].OnArrived -= OnFirstArrived;

            Invoke(nameof(SpawnIntoSlot1), secondSpawnDelay);
        }

        private void SpawnIntoSlot1() => SpawnIntoSlot(1);

        private void SpawnNext(ControllerCustomer avoid = null)
        {
            int free = GetFreeSlot();
            if (free >= 0) SpawnIntoSlot(free, avoid);
        }

        /// <summary>
        /// Fills slotIndex with the next waiting customer. If avoid is non-null
        /// and another waiting customer is available, avoid is skipped so the
        /// same customer doesn't immediately return to the slot they just left.
        /// If avoid is the only one waiting, it is used anyway.
        /// </summary>
        private void SpawnIntoSlot(int slotIndex, ControllerCustomer avoid = null)
        {
            if (_waiting.Count == 0) return;
            if (slotIndex < 0 || slotIndex >= 2) return;
            if (_slotOccupant[slotIndex] != null) return;

            int pickIndex = 0;
            if (avoid != null)
            {
                int altIndex = _waiting.FindIndex(w => w != avoid);
                if (altIndex >= 0) pickIndex = altIndex;
            }

            ControllerCustomer next = _waiting[pickIndex];
            _waiting.RemoveAt(pickIndex);

            _slotOccupant[slotIndex] = next;

            next.AssignSlot(slotPoints[slotIndex], slotIndex);

            if (slotPlates != null && slotIndex < slotPlates.Length && slotPlates[slotIndex] != null)
            {
                next.AssignOwnedPlate(slotPlates[slotIndex]);
                slotPlates[slotIndex].ownerCustomer = next;
            }

            ApplySlotSortOrder(slotIndex, next);

            next.StartWalking();
            Debug.Log($"[ManagerCustomer] '{next.name}' → slot {slotIndex}.");
        }

        /// <summary>
        /// Slot 0 = front (renders above), slot 1 = back (renders below).
        /// </summary>
        private void ApplySlotSortOrder(int slotIndex, ControllerCustomer customer)
        {
            Transform t = customer.transform;

            if (slotIndex == 0)
            {
                // Front slot — render above the back-slot occupant if present.
                ControllerCustomer other = _slotOccupant[1];
                if (other != null && other != customer && other.transform.parent == t.parent)
                    t.SetSiblingIndex(other.transform.GetSiblingIndex() + 1);
                else
                    t.SetAsLastSibling();
            }
            else
            {
                // Back slot — render below the front-slot occupant if present.
                ControllerCustomer other = _slotOccupant[0];
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

        private int GetFreeSlot()
        {
            for (int i = 0; i < 2; i++)
                if (_slotOccupant[i] == null) return i;
            return -1;
        }

        // ── Helpers ───────────────────────────────────────────────────────────────

        private void EnqueueRandom(ControllerCustomer c)
        {
            if (c == null) return;
            int index = _waiting.Count == 0 ? 0 : Random.Range(0, _waiting.Count + 1);
            _waiting.Insert(index, c);
        }

        private static void Shuffle(List<ControllerCustomer> list)
        {
            for (int i = list.Count - 1; i > 0; i--)
            {
                int j = Random.Range(0, i + 1);
                (list[i], list[j]) = (list[j], list[i]);
            }
        }

        // ── Mouth zone proximity lookup ───────────────────────────────────────────

        public (ControllerCustomer customer, float dist) GetNearestMouth(Vector2 screenPos, Camera cam)
        {
            ControllerCustomer best = null;
            float bestDist = mouthDetectionRadius;

            foreach (var c in _active)
            {
                if (c == null || !c.HasArrived || c.mouthPoint == null) continue;
                Vector2 ms = RectTransformUtility.WorldToScreenPoint(cam, c.mouthPoint.position);
                float dist = Vector2.Distance(screenPos, ms);
                if (dist < bestDist) { bestDist = dist; best = c; }
            }

            return (best, bestDist);
        }

        // ── Task system ───────────────────────────────────────────────────────────

        private void InitTask()
        {
            _taskGoal = Random.Range(taskMin, taskMax + 1);
            _fedCount = 0;
            _taskComplete = false;
            UpdateTaskUI();

            // Overwrite the intro message with the actual customer count so
            // the typing effect shows "Will you help me to serve X customers?"
            introMessage = $"Will you help me to serve {_taskGoal} customers?";

            Debug.Log($"[ManagerCustomer] Task goal: {_taskGoal} customers.");
        }

        private void UpdateTaskUI()
        {
            if (taskCounterText != null)
                taskCounterText.text = $"{_fedCount}/{_taskGoal}";
        }

        private void ShowCongratsPanel()
        {
            ControllerStove.InputLocked = true;

            if (congratsPanel != null)
            {
                congratsPanel.transform.localScale = Vector3.zero;
                congratsPanel.SetActive(true);
                congratsPanel.transform.SetAsLastSibling();
                congratsPanel.transform.DOScale(Vector3.one, 0.35f)
                    .SetEase(Ease.OutBack)
                    .SetUpdate(true);
            }

            Debug.Log($"[ManagerCustomer] Task complete! Fed {_fedCount}/{_taskGoal} customers.");
        }

        private void OnRestartClicked()
        {
            // Zoom congrats panel out.
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
            // Reset task counters with a new random goal.
            InitTask();

            // Unlock input.
            ControllerStove.InputLocked = false;

            // Clear slot occupants.
            for (int i = 0; i < 2; i++)
            {
                if (_slotOccupant[i] != null)
                {
                    _slotOccupant[i].gameObject.SetActive(false);
                    _slotOccupant[i] = null;
                }
                if (slotPlates != null && i < slotPlates.Length && slotPlates[i] != null)
                    slotPlates[i].ownerCustomer = null;
            }

            // Put all customers back in the waiting pool.
            _waiting.Clear();
            _active.Clear();
            var pool = new List<ControllerCustomer>();
            if (customer1 != null) pool.Add(customer1);
            if (customer2 != null) pool.Add(customer2);
            if (customer3 != null) pool.Add(customer3);
            Shuffle(pool);
            _waiting.AddRange(pool);

            // Start spawning again.
            Invoke(nameof(SpawnNextIntoSlot0), firstSpawnDelay);
        }
    }
}