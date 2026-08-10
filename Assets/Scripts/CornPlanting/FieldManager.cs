//using System.Collections;
//using UnityEngine;
//using UnityEngine.UI;
//using UnityEngine.EventSystems;
//using TMPro;
//
///// All the original commented-out legacy block stays untouched above —
///// only the live, uncommented class below was modified.

using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

/// <summary>
/// All fields become clickable at once after StartGame() (called by the Yes button).
///
/// Per-field sequence (fully independent — multiple fields can run simultaneously):
///   1. User clicks any highlighted field.
///   2. Hand cursor appears at that field's anchor and bobs.
///   3. Seed animation plays once at normal speed.
///   4. Cursor disappears. Grow animation plays slowly over growDurationSeconds (~60 s).
///   5. Field is fully grown — done.
///
/// Inspector Setup:
///   fields[]            — drag F1…F30 RectTransforms in order.
///   animationAnchors[]  — one empty RectTransform per field (marks where cursor/anims appear).
///   handCursorPrefab    — a UI Image GameObject of your hand sprite (used as a template;
///                         one instance is spawned per clicked field, destroyed after seeding).
///   animImagePrefab     — a UI Image GameObject used as a template for both seed and grow
///                         animations (instantiated fresh per field, per animation phase).
///   seedFrames          — sprites for the seed animation (plays at seedFPS).
///   growFrames          — sprites for the crop growing animation (total time = growDurationSeconds).
///   growDurationSeconds — how long the full grow animation takes (default 60 s).
/// </summary>
public class FieldManager : MonoBehaviour
{
    // ── Inspector ─────────────────────────────────────────────────────────────

    [Header("Fields  (F1 first → F30 last)")]
    public RectTransform[] fields;

    [Header("Animation Anchors  (one per field, index 0 = F1)")]
    [Tooltip("Place each anchor exactly where the cursor and animations should appear for that field.")]
    public RectTransform[] animationAnchors = new RectTransform[30];

    [Header("Prefabs  (set up once — instantiated per field at runtime)")]
    [Tooltip("A UI Image GameObject of your hand cursor sprite. " +
             "It is Instantiated once per clicked field and destroyed after the seed animation.")]
    public GameObject handCursorPrefab;

    [Tooltip("A UI Panel/Image GameObject containing your tutorial text. " +
             "Spawned alongside the hand cursor on F1 and destroyed when the player clicks F1. " +
             "Position it in the prefab relative to its own pivot — it will appear at F1's anchor.")]
    public GameObject tutorialTextPanelPrefab;

    [Tooltip("A UI Image GameObject used as the animation canvas. " +
             "Instantiated separately for seed and grow phases on each field.")]
    public GameObject animImagePrefab;

    [Header("Seed Animation")]
    public Sprite[] seedFrames;
    [Range(1f, 30f)] public float seedFPS = 12f;

    [Header("Grow / Crop Animation")]
    public Sprite[] growFrames;
    [Tooltip("Total real-time seconds for the full grow animation (all frames combined).")]
    [Range(10f, 300f)] public float growDurationSeconds = 60f;

    [Header("Shackle Animation  (loops while Corn Drop plays)")]
    [Tooltip("Looping animation shown when the player clicks the finished grow. " +
             "Hidden automatically when Corn Drop finishes.")]
    public Sprite[] shackleFrames;
    [Range(1f, 30f)] public float shackleFPS = 10f;

    [Header("Corn Drop Animation  (plays once on click of grown field)")]
    [Tooltip("Plays once when the player clicks the finished grow animation. " +
             "Stays on screen permanently after finishing.")]
    public Sprite[] cornDropFrames;
    [Range(1f, 30f)] public float cornDropFPS = 12f;

    [Header("Corn Collection Box")]
    [Tooltip("Prefab: a UI Image GameObject with your single corn sprite on it")]
    public GameObject cornImagePrefab;

    [Tooltip("The collection box RectTransform — must have an Image component")]
    public RectTransform cornBoxRect;

    [Tooltip("Filling animation frames for the box (empty → full)")]
    public Sprite[] cornBoxFillFrames;
    [Range(0.0001f, 30f)] public float cornBoxFillFPS = 10f;

    [Tooltip("How many corn sprites fly to the box per harvest click")]
    public int cornPerField = 5;

    [Tooltip("Seconds each corn takes to fly from the field to the box")]
    [Range(0.1f, 2f)] public float cornFlyDuration = 0.5f;

    [Tooltip("TMP_Text to show total corn collected beside the box (optional)")]
    public TMP_Text cornCountText;

    [Header("Corn Collection Task")]
    [Tooltip("Each round, a random target is picked from this list (e.g. 100, 150, 200, 250, 300). " +
             "Reaching it triggers the congratulations panel.")]
    public int[] cornTargetOptions = { 10, 15, 20, 25, 30 };

    [Header("Hand Cursor Bobbing")]
    public bool animateCursor = true;
    [Range(1f, 40f)] public float bobAmplitude = 8f;
    [Range(0.1f, 5f)] public float bobSpeed = 2f;

    [Header("Field Highlight Color")]
    [Tooltip("Tint applied to a field while it is waiting to be clicked.")]
    public Color highlightColor = new Color(1f, 1f, 0f, 0.55f);

    // ── Private ───────────────────────────────────────────────────────────────

    private bool[] _fieldBusy;   // true once a field has been clicked
    private Canvas _rootCanvas;

    // ── Corn collection state ──────────────────────────────────────────────────
    private int _totalCornCollected = 0;
    private int _totalCornExpected = 0;
    private int _boxFrameIndex = 0;
    private Coroutine _boxAnimCoroutine;
    private Image _cornBoxImage;
    private bool _congratsShown = false;

    // ── Lifecycle ─────────────────────────────────────────────────────────────

    void Awake()
    {
        int count = fields != null ? fields.Length : 0;
        _fieldBusy = new bool[count];

        // Disable all fields until the Yes button calls StartGame()
        for (int i = 0; i < count; i++)
            SetFieldClickable(i, false, Color.white);

        // Cache the root Canvas so we can parent spawned objects to it
        _rootCanvas = GetComponentInParent<Canvas>();
        if (_rootCanvas == null) _rootCanvas = FindObjectOfType<Canvas>();

        // Cache the box Image (the actual corn target is rolled per-round in StartGame)
        if (cornBoxRect != null)
        {
            _cornBoxImage = cornBoxRect.GetComponent<Image>();
            if (_cornBoxImage == null)
                _cornBoxImage = cornBoxRect.gameObject.AddComponent<Image>();
        }

        UpdateCornCountText();
    }

    // ── Public API ────────────────────────────────────────────────────────────

    // Holds the auto-spawned tutorial cursor and text panel so FieldSequence can destroy them when F1 is clicked.
    private GameObject _tutorialCursorGO;
    private GameObject _tutorialTextPanelGO;
    private Coroutine _tutorialBobRoutine;

    /// <summary>Call this from the Yes button (or GameManager) to open all fields.</summary>
    public void StartGame()
    {
        // ── Roll this round's corn target and reset collection progress ──
        _totalCornCollected = 0;
        _congratsShown = false;
        _totalCornExpected = PickRandomCornTarget();
        UpdateCornCountText();

        Debug.Log("[FieldManager] StartGame — corn target this round: " + _totalCornExpected);
        for (int i = 0; i < fields.Length; i++)
            SetFieldClickable(i, true, highlightColor);

        // Auto-spawn the tutorial cursor on F1 — player has not clicked yet.
        SpawnTutorialCursor();
    }

    /// <summary>Picks a random corn target for the round from cornTargetOptions.</summary>
    int PickRandomCornTarget()
    {
        if (cornTargetOptions == null || cornTargetOptions.Length == 0)
            return (fields != null ? fields.Length : 0) * cornPerField; // fallback to old fixed behaviour

        return cornTargetOptions[Random.Range(0, cornTargetOptions.Length)];
    }

    void SpawnTutorialCursor()
    {
        if (handCursorPrefab == null) return;
        if (fields == null || fields.Length == 0 || fields[0] == null) return;

        // Parent under the field itself (not Canvas) so this scales correctly with zoom.
        RectTransform fieldParent = fields[0];
        RectTransform anchor = GetAnchor(0);

        _tutorialCursorGO = Instantiate(handCursorPrefab, fieldParent);
        RectTransform cursorRT = _tutorialCursorGO.GetComponent<RectTransform>();
        cursorRT.position = anchor.position;
        _tutorialCursorGO.transform.SetAsLastSibling();
        _tutorialCursorGO.SetActive(true);

        // Spawn the tutorial text panel at the same anchor position.
        RectTransform panelRT = null;
        if (tutorialTextPanelPrefab != null)
        {
            _tutorialTextPanelGO = Instantiate(tutorialTextPanelPrefab, fieldParent);
            panelRT = _tutorialTextPanelGO.GetComponent<RectTransform>();
            panelRT.position = anchor.position;
            _tutorialTextPanelGO.transform.SetAsLastSibling();
            _tutorialTextPanelGO.SetActive(true);
        }

        // Bob both cursor and text panel in sync.
        if (animateCursor)
            _tutorialBobRoutine = StartCoroutine(BobCursor(cursorRT, panelRT));

        Debug.Log("[FieldManager] Tutorial cursor + text panel active on F1.");
    }

    /// <summary>Called by FieldClickReceiver when the player taps a field.</summary>
    public void OnFieldClicked(int index)
    {
        if (index < 0 || index >= fields.Length) return;
        if (_fieldBusy[index]) return;          // already clicked — ignore
        _fieldBusy[index] = true;

        // Hide tutorial cursor on ANY field click
        if (_tutorialBobRoutine != null) StopCoroutine(_tutorialBobRoutine);
        if (_tutorialCursorGO != null) Destroy(_tutorialCursorGO);
        if (_tutorialTextPanelGO != null) Destroy(_tutorialTextPanelGO);
        _tutorialCursorGO = null;
        _tutorialTextPanelGO = null;
        _tutorialBobRoutine = null;

        // Lock this field immediately so it can't be clicked again
        SetFieldClickable(index, false, Color.white);

        StartCoroutine(FieldSequence(index));
    }

    // ── Per-field animation sequence ──────────────────────────────────────────

    IEnumerator FieldSequence(int index)
    {
        RectTransform anchor = GetAnchor(index);

        // Parent spawned animations under this specific field — NOT the Canvas.
        // This keeps seed/grow/corn sprites locked to the field's own scale,
        // so they zoom in/out together with BG instead of drifting.
        RectTransform canvasRect = fields[index];

        // ── Phase 2: Seed animation (normal speed, plays once) ────────────
        if (seedFrames != null && seedFrames.Length > 0 && animImagePrefab != null && canvasRect != null)
        {
            GameObject seedGO = SpawnAnimImage(canvasRect, anchor.position);
            Image seedImg = seedGO.GetComponent<Image>();
            float frameDelay = 1f / Mathf.Max(seedFPS, 0.01f);

            foreach (Sprite frame in seedFrames)
            {
                if (seedImg != null) seedImg.sprite = frame;
                yield return new WaitForSeconds(frameDelay);
            }
            Destroy(seedGO);
        }

        // ── Phase 3: Grow animation (slow — spans growDurationSeconds) ────
        GameObject growGO = null;
        Image growImg = null;

        if (growFrames != null && growFrames.Length > 0 && animImagePrefab != null && canvasRect != null)
        {
            growGO = SpawnAnimImage(canvasRect, anchor.position);
            growImg = growGO.GetComponent<Image>();

            SoundManager.Instance.PlaySFX("Corngrow");
            float frameDelay = growDurationSeconds / Mathf.Max(growFrames.Length, 1);
            foreach (Sprite frame in growFrames)
            {
                if (growImg != null) growImg.sprite = frame;
                yield return new WaitForSeconds(frameDelay);
            }

            // growGO stays visible — it becomes the clickable harvest target below.
        }

        Debug.Log("[FieldManager] Field " + (index + 1) + " fully grown — waiting for harvest click.");

        // ── Phase 4: Wait for player to click the grown field image ───────
        bool harvestClicked = false;

        if (growGO != null && canvasRect != null)
        {
            GrowHarvestReceiver hr = growGO.AddComponent<GrowHarvestReceiver>();
            hr.Init(() => harvestClicked = true);

            if (growImg != null)
            {
                growImg.raycastTarget = true;
                growImg.alphaHitTestMinimumThreshold = 0.1f;
            }

            yield return new WaitUntil(() => harvestClicked);
            Destroy(hr);
            Destroy(growGO);
        }

        // ── Phase 5: Shackle (loop) + Corn Drop (once) + Click to collect ────
        if (animImagePrefab != null && canvasRect != null)
        {
            bool hasShackle = shackleFrames != null && shackleFrames.Length > 0;
            bool hasCornDrop = cornDropFrames != null && cornDropFrames.Length > 0;

            GameObject shackleGO = null;
            Coroutine shackleLoop = null;

            if (hasShackle)
            {
                shackleGO = SpawnAnimImage(canvasRect, anchor.position);
                shackleLoop = StartCoroutine(LoopAnim(shackleGO.GetComponent<Image>(), shackleFrames, shackleFPS));
            }

            if (hasCornDrop)
            {
                GameObject cornDropGO = SpawnAnimImage(canvasRect, anchor.position);
                Image cornDropImg = cornDropGO.GetComponent<Image>();
                float frameDelay = 1f / Mathf.Max(cornDropFPS, 0.01f);

                SoundManager.Instance.PlaySFX("Corndrop");
                foreach (Sprite frame in cornDropFrames)
                {
                    if (cornDropImg != null) cornDropImg.sprite = frame;
                    yield return new WaitForSeconds(frameDelay);
                }

                if (shackleLoop != null) StopCoroutine(shackleLoop);
                if (shackleGO != null) Destroy(shackleGO);

                bool collectClicked = false;
                CornHarvestReceiver chr = cornDropGO.AddComponent<CornHarvestReceiver>();
                chr.Init(() => collectClicked = true);

                if (cornDropImg != null)
                {
                    cornDropImg.raycastTarget = true;
                    cornDropImg.alphaHitTestMinimumThreshold = 0.1f;
                }

                yield return new WaitUntil(() => collectClicked);
                Destroy(cornDropGO);

                SoundManager.Instance.PlaySFX("Corncollect");

                LaunchCornBatch(anchor.position, index);

                Debug.Log("[FieldManager] Field " + (index + 1) + " — corn collected.");
            }
        }
    }

    // ── Corn Collection ───────────────────────────────────────────────────────

    /// <summary>
    /// Spawns cornPerField corn sprites at worldPos and flies them to the corn box.
    /// Box animation plays while they travel and freezes when the last one lands.
    /// </summary>
    void LaunchCornBatch(Vector3 worldPos, int fieldIndex)
    {
        if (cornBoxRect == null || cornImagePrefab == null) return;

        StartBoxAnimation();

        int arrived = 0;
        for (int i = 0; i < cornPerField; i++)
        {
            float stagger = i * 0.1f;
            StartCoroutine(FlyCorn(worldPos, cornBoxRect.position, stagger, () =>
            {
                arrived++;
                _totalCornCollected++;
                UpdateCornCountText();

                // ── Task complete: target reached ──────────────────────────
                if (!_congratsShown && _totalCornCollected >= _totalCornExpected)
                {
                    _congratsShown = true;
                    Debug.Log("[FieldManager] Corn target of " + _totalCornExpected + " reached — showing congrats.");
                    if (OpenFieldGameManager.Instance != null)
                        OpenFieldGameManager.Instance.ShowCongrats();
                    LockAllFields(); // stop new fields from being planted once the goal is hit
                }

                if (arrived >= cornPerField)
                {
                    bool allCollected = _totalCornCollected >= _totalCornExpected;
                    StopBoxAnimation(allCollected);
                    ResetField(fieldIndex);  // ← reset field for replanting
                }
            }));
        }
    }

    void ResetField(int index)
    {
        // Don't reopen a field for replanting once the round's corn target has been reached.
        if (_congratsShown)
        {
            SetFieldClickable(index, false, Color.white);
            return;
        }

        _fieldBusy[index] = false;
        SetFieldClickable(index, true, highlightColor);
        Debug.Log("[FieldManager] Field " + (index + 1) + " reset — ready to plant again.");
    }

    /// <summary>Locks every currently-idle (non-busy) field so no new planting can start.</summary>
    void LockAllFields()
    {
        if (fields == null) return;
        for (int i = 0; i < fields.Length; i++)
        {
            if (!_fieldBusy[i])
                SetFieldClickable(i, false, Color.white);
        }
    }

    /// <summary>Flies a single corn image from <paramref name="from"/> to <paramref name="to"/>.</summary>
    IEnumerator FlyCorn(Vector3 from, Vector3 to, float delay, System.Action onArrived)
    {
        if (delay > 0f) yield return new WaitForSeconds(delay);

        RectTransform canvasRT = _rootCanvas != null ? _rootCanvas.GetComponent<RectTransform>() : null;
        if (canvasRT == null) yield break;

        GameObject cornGO = Instantiate(cornImagePrefab, canvasRT);
        cornGO.transform.SetAsLastSibling();
        cornGO.transform.position = from;
        cornGO.transform.localScale = Vector3.zero;
        cornGO.SetActive(true);

        // ── Phase 1: Pop zoom in ───────────────────────────────────────────
        float popDuration = 0.2f;
        float elapsed = 0f;
        while (elapsed < popDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / popDuration);
            float scale = t < 0.6f
                ? Mathf.Lerp(0f, 1.4f, t / 0.6f)
                : Mathf.Lerp(1.4f, 1f, (t - 0.6f) / 0.4f);
            cornGO.transform.localScale = Vector3.one * scale;
            yield return null;
        }
        cornGO.transform.localScale = Vector3.one;

        // ── Phase 2: Fly straight to the box ──────────────────────────────
        elapsed = 0f;
        while (elapsed < cornFlyDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / cornFlyDuration);
            cornGO.transform.position = Vector3.Lerp(from, to, t);
            yield return null;
        }

        cornGO.transform.position = to;
        Destroy(cornGO);
        onArrived?.Invoke();
    }

    /// <summary>Starts (or restarts) the looping box fill animation from the current frame.</summary>
    void StartBoxAnimation()
    {
        if (_cornBoxImage == null || cornBoxFillFrames == null || cornBoxFillFrames.Length == 0) return;
        if (_boxAnimCoroutine != null) StopCoroutine(_boxAnimCoroutine);
        _boxAnimCoroutine = StartCoroutine(LoopBoxAnim());
    }

    /// <summary>Loops box fill frames, always continuing from where it last paused.</summary>
    IEnumerator LoopBoxAnim()
    {
        float delay = 1f / Mathf.Max(cornBoxFillFPS, 0.01f);
        while (true)
        {
            _cornBoxImage.sprite = cornBoxFillFrames[_boxFrameIndex];
            _boxFrameIndex = (_boxFrameIndex + 1) % cornBoxFillFrames.Length;
            yield return new WaitForSeconds(delay);
        }
    }

    /// <summary>
    /// Stops the box animation and freezes it.
    /// </summary>
    void StopBoxAnimation(bool showFilled)
    {
        if (_boxAnimCoroutine != null) StopCoroutine(_boxAnimCoroutine);
        _boxAnimCoroutine = null;

        if (_cornBoxImage == null || cornBoxFillFrames == null || cornBoxFillFrames.Length == 0) return;

        // Always pick the correct frame based on collection progress (collected / target),
        // not batch count — this stays correct no matter how cornTargetOptions relates to cornPerField.
        int frameCount = cornBoxFillFrames.Length;
        float progress = _totalCornExpected > 0
            ? (float)_totalCornCollected / _totalCornExpected
            : 0f;

        int targetFrame = Mathf.Min(
            Mathf.FloorToInt(progress * frameCount),
            frameCount - 1
        );

        _boxFrameIndex = targetFrame;
        _cornBoxImage.sprite = cornBoxFillFrames[targetFrame];
    }

    /// <summary>Refreshes the corn count label beside the box.</summary>
    void UpdateCornCountText()
    {
        if (cornCountText != null)
            cornCountText.text = _totalCornCollected + " / " + _totalCornExpected;
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    /// <summary>Enables or disables clicking on a field and sets its highlight colour.</summary>
    void SetFieldClickable(int index, bool clickable, Color color)
    {
        if (fields == null || index >= fields.Length || fields[index] == null) return;

        RectTransform field = fields[index];

        Image img = field.GetComponent<Image>();
        if (img == null && clickable)
            img = field.gameObject.AddComponent<Image>();

        if (img != null)
        {
            img.color = color;
            img.raycastTarget = clickable;
            img.alphaHitTestMinimumThreshold = clickable ? 0.1f : 0f;
        }

        FieldClickReceiver r = field.GetComponent<FieldClickReceiver>();
        if (r == null && clickable)
            r = field.gameObject.AddComponent<FieldClickReceiver>();

        if (r != null)
        {
            r.fieldManager = this;
            r.fieldIndex = index;
            r.enabled = clickable;
        }
    }

    /// <summary>Instantiates an animImagePrefab at the given world position.</summary>
    GameObject SpawnAnimImage(RectTransform parent, Vector3 worldPos)
    {
        GameObject go = Instantiate(animImagePrefab, parent);
        go.GetComponent<RectTransform>().position = worldPos;
        go.transform.SetAsLastSibling();
        go.SetActive(true);
        return go;
    }

    /// <summary>Returns the animation anchor for a field, falling back to the field itself.</summary>
    RectTransform GetAnchor(int index)
    {
        if (animationAnchors != null && index < animationAnchors.Length && animationAnchors[index] != null)
            return animationAnchors[index];
        return fields[index];
    }

    /// <summary>Bobs one or two RectTransforms in sync until the first is destroyed.</summary>
    IEnumerator BobCursor(RectTransform rt, RectTransform rt2 = null)
    {
        if (rt == null) yield break;
        Vector2 basePos = rt.anchoredPosition;
        Vector2 basePos2 = rt2 != null ? rt2.anchoredPosition : Vector2.zero;
        while (rt != null)
        {
            float yOff = Mathf.Sin(Time.time * bobSpeed * Mathf.PI * 2f) * bobAmplitude;
            rt.anchoredPosition = basePos + new Vector2(0f, yOff);
            if (rt2 != null) rt2.anchoredPosition = basePos2 + new Vector2(0f, yOff);
            yield return null;
        }
    }

    /// <summary>
    /// Loops a sprite animation on an Image indefinitely.
    /// Stop with StopCoroutine() — caller must Destroy the GameObject after.
    /// </summary>
    IEnumerator LoopAnim(Image img, Sprite[] frames, float fps)
    {
        if (img == null || frames == null || frames.Length == 0) yield break;
        float delay = 1f / Mathf.Max(fps, 0.01f);
        int frameIndex = 0;
        while (true)
        {
            img.sprite = frames[frameIndex];
            frameIndex = (frameIndex + 1) % frames.Length;
            yield return new WaitForSeconds(delay);
        }
    }
}

// ─────────────────────────────────────────────────────────────────────────────
// FieldClickReceiver  —  auto-added to every field by FieldManager.
// Do NOT add manually in the Inspector.
// ─────────────────────────────────────────────────────────────────────────────
public class FieldClickReceiver : MonoBehaviour, IPointerClickHandler
{
    [HideInInspector] public FieldManager fieldManager;
    [HideInInspector] public int fieldIndex;

    public void OnPointerClick(PointerEventData eventData)
    {
        if (fieldManager == null) return;
        Debug.Log("[FieldClickReceiver] Clicked: " + gameObject.name);
        fieldManager.OnFieldClicked(fieldIndex);
    }
}

// ─────────────────────────────────────────────────────────────────────────────
// GrowHarvestReceiver  —  temporarily added to the grown-field image by
// FieldSequence so the player can click it to start the harvest sequence.
// Removed automatically once the click is received.
// ─────────────────────────────────────────────────────────────────────────────
public class GrowHarvestReceiver : MonoBehaviour, IPointerClickHandler
{
    private System.Action _onClicked;

    public void Init(System.Action onClicked)
    {
        _onClicked = onClicked;
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        Debug.Log("[GrowHarvestReceiver] Grown field clicked — starting harvest sequence.");
        _onClicked?.Invoke();
    }
}

// ─────────────────────────────────────────────────────────────────────────────
// CornHarvestReceiver  —  temporarily added to the corn-drop image so the
// player can click it to send corn flying to the collection box.
// Removed automatically once the click is received.
// ─────────────────────────────────────────────────────────────────────────────
public class CornHarvestReceiver : MonoBehaviour, IPointerClickHandler
{
    private System.Action _onClicked;

    public void Init(System.Action onClicked) { _onClicked = onClicked; }

    public void OnPointerClick(PointerEventData eventData)
    {
        Debug.Log("[CornHarvestReceiver] Corn drop clicked — launching corn batch.");
        _onClicked?.Invoke();
    }
}