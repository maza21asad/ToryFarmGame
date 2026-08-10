////////using UnityEngine;
////////using UnityEngine.UI;
////////using System.Collections;
////////using DG.Tweening;

////////[System.Serializable]
////////public class FrameAnimation
////////{
////////    [Tooltip("Drag all sprite frames here in order.")]
////////    public Sprite[] frames;

////////    [Min(1f)]
////////    [Tooltip("Playback speed in frames per second.")]
////////    public float fps = 12f;
////////}

/////////// <summary>
/////////// Attach to the raw grill prefab root (alongside DraggableGrill).
///////////
/////////// PREFAB HIERARCHY
///////////   GrillRaw  (root — this script lives here)
///////////     ├─ Image          raw grill sprite / cooking animation frames
///////////     ├─ DraggableGrill (isCooked = false)
///////////     ├─ CanvasGroup
///////////     └─ GrillCooked   ← child, set INACTIVE in the Editor
///////////           ├─ Image          cooked grill sprite
///////////           ├─ DraggableGrill (isCooked set true at runtime)
///////////           ├─ CanvasGroup
///////////           └─ BurnerGrill    (disabled — GrillCooker enables it after cooking)
///////////
/////////// FLOW
///////////   StoveController calls StartCooking()
///////////     → cooking animation plays once for cookDuration seconds
///////////     → grilling SFX loops for the full cookDuration (via coroutine)
///////////     → root Image disabled
///////////     → pulse params + isCooked written onto GrillCooked's DraggableGrill BEFORE SetActive
///////////     → GrillCooked.SetActive(true)  — its OnEnable starts the pulse safely
///////////     → GrillCooked reparented to Canvas root at the exact same screen position
///////////     → CanvasGroup.blocksRaycasts = true
///////////     → BurnerGrill enabled (burn timer starts from this moment,
///////////       not from when the prefab first activated)
///////////     → stove slot freed, raw grill destroyed
///////////
/////////// BurnerGrill lifecycle:
///////////   • Starts DISABLED on the cooked child prefab (set in Inspector / Awake).
///////////   • GrillCooker enables it here, after the grill is live at the canvas root.
///////////   • PlateController.SnapGrill() disables it when the grill lands on a plate.
///////////   • DraggableGrill.TryDropOnDustbin() disables it when discarded.
///////////   This ensures the burn timer ONLY runs while the grill is floating unplated.
/////////// </summary>
////////public class GrillCooker : MonoBehaviour
////////{
////////    [Header("Cooking Animation")]
////////    [Tooltip("Sprite frames played once over cookDuration seconds. Last frame held until done.")]
////////    public FrameAnimation cookingAnimation;

////////    [Tooltip("Seconds the cooking animation plays before the cooked object appears.")]
////////    public float cookDuration = 15f;

////////    [Header("Cooked Child Object")]
////////    [Tooltip("The child GameObject with the cooked sprite.\n" +
////////             "Set it INACTIVE in the Editor.\n" +
////////             "Required components: Image, DraggableGrill, CanvasGroup, BurnerGrill (disabled).")]
////////    public GameObject cookedObject;

////////    [Header("Size Overrides  (0,0 = keep default)")]
////////    public Vector2 cookingSize = Vector2.zero;
////////    public Vector2 cookedSize = Vector2.zero;

////////    [Header("Pulse  (shown when cooking finishes)")]
////////    public float pulseSpeed = 3f;
////////    [Range(0f, 0.25f)]
////////    public float pulseAmount = 0.07f;

////////    [Header("Grilling Sound")]
////////    [Tooltip("Must match the SFX name in AudioData exactly.")]
////////    public string grillingSFXName = "Grilling";
////////    [Tooltip("Set this to the exact duration of your Grilling audio clip in seconds.\n" +
////////             "Check it by selecting the clip in the Project window.")]
////////    public float grillingClipLength = 5f;

////////    // ── Set by StoveController ─────────────────────────────────────────────
////////    [HideInInspector] public StoveController stove;
////////    [HideInInspector] public int slotIndex = -1;
////////    [HideInInspector] public Canvas rootCanvas;

////////    // ── Private ────────────────────────────────────────────────────────────
////////    private Image _image;
////////    private RectTransform _rect;
////////    private Coroutine _animRoutine;
////////    private Sequence _grillSoundSequence;

////////    private void Awake()
////////    {
////////        _image = GetComponent<Image>();
////////        _rect = GetComponent<RectTransform>();

////////        if (cookedObject != null)
////////        {
////////            cookedObject.SetActive(false);

////////            // Ensure BurnerGrill starts disabled — GrillCooker will enable it
////////            // at the right moment after cooking completes.
////////            BurnerGrill burner = cookedObject.GetComponent<BurnerGrill>();
////////            if (burner != null) burner.enabled = false;
////////        }
////////    }

////////    // ── Entry point called by StoveController ──────────────────────────────

////////    public void StartCooking()
////////    {
////////        if (cookingSize != Vector2.zero && _rect != null)
////////            _rect.sizeDelta = cookingSize;

////////        // Play immediately, then repeat every clipLength — no gap
////////        SoundManager.Instance.PlaySFX(grillingSFXName);
////////        _grillSoundSequence = DOTween.Sequence();
////////        _grillSoundSequence
////////            .AppendInterval(grillingClipLength)
////////            .AppendCallback(() => SoundManager.Instance.PlaySFX(grillingSFXName))
////////            .SetLoops(-1);

////////        StartAnim();
////////        StartCoroutine(CookTimer());
////////    }

////////    private void StopGrillSound()
////////    {
////////        _grillSoundSequence?.Kill();
////////        _grillSoundSequence = null;
////////    }

////////    // ── Cook timer ─────────────────────────────────────────────────────────

////////    private IEnumerator CookTimer()
////////    {
////////        yield return new WaitForSeconds(cookDuration);

////////        StopGrillSound();  // stop looping grilling SFX
////////        StopAnim();        // freeze on last cooking frame

////////        if (cookedObject == null)
////////        {
////////            Debug.LogWarning("[GrillCooker] cookedObject is not assigned!");
////////            yield break;
////////        }
////////        if (rootCanvas == null)
////////        {
////////            Debug.LogWarning("[GrillCooker] rootCanvas is not set!");
////////            yield break;
////////        }

////////        // ── 1. Capture stove-slot world position BEFORE anything moves ───────
////////        Vector3 slotWorldPos = transform.position;

////////        // ── 2. Hide cooking visual ────────────────────────────────────────────
////////        if (_image != null) _image.enabled = false;

////////        // ── 3. Write data onto cooked child's DraggableGrill BEFORE SetActive ─
////////        DraggableGrill myDrag = GetComponent<DraggableGrill>();
////////        DraggableGrill cookedDrag = cookedObject.GetComponent<DraggableGrill>();
////////        if (cookedDrag != null)
////////        {
////////            cookedDrag.grillType = myDrag != null ? myDrag.grillType : "Unknown";
////////            cookedDrag.isCooked = true;
////////            cookedDrag.pendingPulse = true;
////////            cookedDrag.pendingPulseSpeed = pulseSpeed;
////////            cookedDrag.pendingPulseAmount = pulseAmount;
////////        }

////////        // ── 4. Make sure BurnerGrill is DISABLED before SetActive ─────────────
////////        //    OnEnable would start the timer immediately — we want it to start
////////        //    only after the grill is positioned at the canvas root (step 9).
////////        BurnerGrill burner = cookedObject.GetComponent<BurnerGrill>();
////////        if (burner != null) burner.enabled = false;

////////        // ── 5. Activate the cooked child ──────────────────────────────────────
////////        //    DraggableGrill.Awake() runs (blocksRaycasts = false, alpha hit test).
////////        //    DraggableGrill.OnEnable() runs (pendingPulse → SaveOrigin + pulse).
////////        //    BurnerGrill does NOT run (still disabled).
////////        cookedObject.SetActive(true);

////////        // ── 6. Reparent to canvas root ────────────────────────────────────────
////////        RectTransform cookedRect = cookedObject.GetComponent<RectTransform>();
////////        cookedObject.transform.SetParent(rootCanvas.transform, false);

////////        // ── 7. Restore exact screen position ──────────────────────────────────
////////        RectTransform canvasRect = rootCanvas.GetComponent<RectTransform>();
////////        Camera uiCamera = rootCanvas.renderMode == RenderMode.ScreenSpaceOverlay
////////                          ? null : rootCanvas.worldCamera;

////////        RectTransformUtility.ScreenPointToLocalPointInRectangle(
////////            canvasRect,
////////            RectTransformUtility.WorldToScreenPoint(uiCamera, slotWorldPos),
////////            uiCamera,
////////            out Vector2 localPoint);

////////        cookedRect.localPosition = localPoint;

////////        // ── 8. Apply optional size override ───────────────────────────────────
////////        if (cookedSize != Vector2.zero)
////////            cookedRect.sizeDelta = cookedSize;

////////        // ── 9. Enable raycasts ────────────────────────────────────────────────
////////        CanvasGroup cg = cookedObject.GetComponent<CanvasGroup>();
////////        if (cg != null)
////////        {
////////            cg.blocksRaycasts = true;
////////            cg.interactable = true;
////////            cg.alpha = 1f;
////////        }

////////        // ── 10. NOW enable BurnerGrill — timer starts from this moment ────────
////////        //    The grill is at the canvas root, visible, and draggable. The burn
////////        //    grace period begins here. PlateController.SnapGrill() will disable
////////        //    this component the instant the player drops the grill on a plate.
////////        if (burner != null) burner.enabled = true;

////////        Debug.Log($"[GrillCooker] '{cookedDrag?.grillType}' is cooked! " +
////////                  $"Burn grace timer started. Drag to a plate.");

////////        // ── 11. Play cooked SFX, free the stove slot, destroy raw grill ───────
////////        SoundManager.Instance.PlaySFX("Cooked");
////////        stove?.FreeSlot(slotIndex);
////////        Destroy(gameObject);
////////    }

////////    // ── Animation helpers ──────────────────────────────────────────────────

////////    private void StartAnim()
////////    {
////////        StopAnim();
////////        if (cookingAnimation?.frames == null || cookingAnimation.frames.Length == 0) return;
////////        _animRoutine = StartCoroutine(AnimRoutine());
////////    }

////////    private void StopAnim()
////////    {
////////        if (_animRoutine == null) return;
////////        StopCoroutine(_animRoutine);
////////        _animRoutine = null;
////////    }

////////    private IEnumerator AnimRoutine()
////////    {
////////        int frameCount = cookingAnimation.frames.Length;

////////        // Spread frames evenly across the full cookDuration so the last frame
////////        // lands exactly when the cook timer fires — regardless of the FPS value
////////        // set in the Inspector. e.g. 9 frames over 20 s = 2.22 s per frame.
////////        float delay = cookDuration / frameCount;

////////        int i = 0;

////////        while (true)
////////        {
////////            if (_image != null)
////////                _image.sprite = cookingAnimation.frames[i];

////////            yield return new WaitForSeconds(delay);

////////            i++;
////////            if (i >= frameCount)
////////                yield break;   // one-shot: stop on the last frame
////////        }
////////    }
////////}

//////using UnityEngine;
//////using UnityEngine.UI;
//////using System.Collections;
//////using DG.Tweening;

//////[System.Serializable]
//////public class FrameAnimation
//////{
//////    [Tooltip("Drag all sprite frames here in order.")]
//////    public Sprite[] frames;

//////    [Min(1f)]
//////    [Tooltip("Playback speed in frames per second.")]
//////    public float fps = 12f;
//////}

///////// <summary>
///////// Attach to the raw grill prefab root (alongside DraggableGrill).
/////////
///////// PREFAB HIERARCHY
/////////   GrillRaw  (root — this script lives here)
/////////     ├─ Image          raw grill sprite / cooking animation frames
/////////     ├─ DraggableGrill (isCooked = false)
/////////     ├─ CanvasGroup
/////////     └─ GrillCooked   ← child, set INACTIVE in the Editor
/////////           ├─ Image          cooked grill sprite
/////////           ├─ DraggableGrill (isCooked set true at runtime)
/////////           ├─ CanvasGroup
/////////           └─ BurnerGrill    (disabled — GrillCooker enables it after cooking)
/////////
///////// FLOW
/////////   StoveController calls StartCooking()
/////////     → cooking animation plays once for cookDuration seconds
/////////     → grilling SFX loops for the full cookDuration (via coroutine)
/////////     → root Image disabled
/////////     → pulse params + isCooked written onto GrillCooked's DraggableGrill BEFORE SetActive
/////////     → GrillCooked.SetActive(true)  — its OnEnable starts the pulse safely
/////////     → GrillCooked reparented to Canvas root at the exact same screen position
/////////     → CanvasGroup.blocksRaycasts = true
/////////     → BurnerGrill enabled (burn timer starts from this moment,
/////////       not from when the prefab first activated)
/////////     → stove slot freed, raw grill destroyed
/////////
///////// BurnerGrill lifecycle:
/////////   • Starts DISABLED on the cooked child prefab (set in Inspector / Awake).
/////////   • GrillCooker enables it here, after the grill is live at the canvas root.
/////////   • PlateController.SnapGrill() disables it when the grill lands on a plate.
/////////   • DraggableGrill.TryDropOnDustbin() disables it when discarded.
/////////   This ensures the burn timer ONLY runs while the grill is floating unplated.
///////// </summary>
//////public class GrillCooker : MonoBehaviour
//////{
//////    [Header("Cooking Animation")]
//////    [Tooltip("Sprite frames played once over cookDuration seconds. Last frame held until done.")]
//////    public FrameAnimation cookingAnimation;

//////    [Tooltip("Seconds the cooking animation plays before the cooked object appears.")]
//////    public float cookDuration = 15f;

//////    [Header("Cooked Child Object")]
//////    [Tooltip("The child GameObject with the cooked sprite.\n" +
//////             "Set it INACTIVE in the Editor.\n" +
//////             "Required components: Image, DraggableGrill, CanvasGroup, BurnerGrill (disabled).")]
//////    public GameObject cookedObject;

//////    [Header("Size Overrides  (0,0 = keep default)")]
//////    public Vector2 cookingSize = Vector2.zero;
//////    public Vector2 cookedSize = Vector2.zero;

//////    [Header("Pulse  (shown when cooking finishes)")]
//////    public float pulseSpeed = 3f;
//////    [Range(0f, 0.25f)]
//////    public float pulseAmount = 0.07f;

//////    [Header("Grilling Sound")]
//////    [Tooltip("Must match the SFX name in AudioData exactly.")]
//////    public string grillingSFXName = "Grilling";
//////    [Tooltip("Set this to the exact duration of your Grilling audio clip in seconds.\n" +
//////             "Check it by selecting the clip in the Project window.")]
//////    public float grillingClipLength = 5f;

//////    // ── Set by StoveController ─────────────────────────────────────────────
//////    [HideInInspector] public StoveController stove;
//////    [HideInInspector] public int slotIndex = -1;
//////    [HideInInspector] public Canvas rootCanvas;

//////    // ── Private ────────────────────────────────────────────────────────────
//////    private Image _image;
//////    private RectTransform _rect;
//////    private Coroutine _animRoutine;
//////    private Coroutine _grillSoundRoutine;

//////    private void Awake()
//////    {
//////        _image = GetComponent<Image>();
//////        _rect = GetComponent<RectTransform>();

//////        if (cookedObject != null)
//////        {
//////            cookedObject.SetActive(false);

//////            // Ensure BurnerGrill starts disabled — GrillCooker will enable it
//////            // at the right moment after cooking completes.
//////            BurnerGrill burner = cookedObject.GetComponent<BurnerGrill>();
//////            if (burner != null) burner.enabled = false;
//////        }
//////    }

//////    // ── Entry point called by StoveController ──────────────────────────────

//////    public void StartCooking()
//////    {
//////        if (cookingSize != Vector2.zero && _rect != null)
//////            _rect.sizeDelta = cookingSize;

//////        _grillSoundRoutine = StartCoroutine(GrillSoundLoop());

//////        StartAnim();
//////        StartCoroutine(CookTimer());
//////    }

//////    private IEnumerator GrillSoundLoop()
//////    {
//////        float clipLength = SoundManager.Instance.GetSFXClipLength(grillingSFXName);
//////        if (clipLength <= 0f) yield break;

//////        double nextPlayTime = AudioSettings.dspTime;

//////        while (true)
//////        {
//////            SoundManager.Instance.PlaySFX(grillingSFXName);
//////            nextPlayTime += clipLength;
//////            float waitTime = (float)(nextPlayTime - AudioSettings.dspTime) - 0.02f; // fire slightly early
//////            yield return new WaitForSeconds(waitTime);
//////        }
//////    }

//////    private void StopGrillSound()
//////    {
//////        if (_grillSoundRoutine != null)
//////        {
//////            StopCoroutine(_grillSoundRoutine);
//////            _grillSoundRoutine = null;
//////        }
//////    }

//////    // ── Cook timer ─────────────────────────────────────────────────────────

//////    private IEnumerator CookTimer()
//////    {
//////        yield return new WaitForSeconds(cookDuration);

//////        StopGrillSound();  // stop looping grilling SFX
//////        StopAnim();        // freeze on last cooking frame

//////        if (cookedObject == null)
//////        {
//////            Debug.LogWarning("[GrillCooker] cookedObject is not assigned!");
//////            yield break;
//////        }
//////        if (rootCanvas == null)
//////        {
//////            Debug.LogWarning("[GrillCooker] rootCanvas is not set!");
//////            yield break;
//////        }

//////        // ── 1. Capture stove-slot world position BEFORE anything moves ───────
//////        Vector3 slotWorldPos = transform.position;

//////        // ── 2. Hide cooking visual ────────────────────────────────────────────
//////        if (_image != null) _image.enabled = false;

//////        // ── 3. Write data onto cooked child's DraggableGrill BEFORE SetActive ─
//////        DraggableGrill myDrag = GetComponent<DraggableGrill>();
//////        DraggableGrill cookedDrag = cookedObject.GetComponent<DraggableGrill>();
//////        if (cookedDrag != null)
//////        {
//////            cookedDrag.grillType = myDrag != null ? myDrag.grillType : "Unknown";
//////            cookedDrag.isCooked = true;
//////            cookedDrag.pendingPulse = true;
//////            cookedDrag.pendingPulseSpeed = pulseSpeed;
//////            cookedDrag.pendingPulseAmount = pulseAmount;
//////        }

//////        // ── 4. Make sure BurnerGrill is DISABLED before SetActive ─────────────
//////        //    OnEnable would start the timer immediately — we want it to start
//////        //    only after the grill is positioned at the canvas root (step 9).
//////        BurnerGrill burner = cookedObject.GetComponent<BurnerGrill>();
//////        if (burner != null) burner.enabled = false;

//////        // ── 5. Activate the cooked child ──────────────────────────────────────
//////        //    DraggableGrill.Awake() runs (blocksRaycasts = false, alpha hit test).
//////        //    DraggableGrill.OnEnable() runs (pendingPulse → SaveOrigin + pulse).
//////        //    BurnerGrill does NOT run (still disabled).
//////        cookedObject.SetActive(true);

//////        // ── 6. Reparent to canvas root ────────────────────────────────────────
//////        RectTransform cookedRect = cookedObject.GetComponent<RectTransform>();
//////        cookedObject.transform.SetParent(rootCanvas.transform, false);

//////        // ── 7. Restore exact screen position ──────────────────────────────────
//////        RectTransform canvasRect = rootCanvas.GetComponent<RectTransform>();
//////        Camera uiCamera = rootCanvas.renderMode == RenderMode.ScreenSpaceOverlay
//////                          ? null : rootCanvas.worldCamera;

//////        RectTransformUtility.ScreenPointToLocalPointInRectangle(
//////            canvasRect,
//////            RectTransformUtility.WorldToScreenPoint(uiCamera, slotWorldPos),
//////            uiCamera,
//////            out Vector2 localPoint);

//////        cookedRect.localPosition = localPoint;

//////        // ── 8. Apply optional size override ───────────────────────────────────
//////        if (cookedSize != Vector2.zero)
//////            cookedRect.sizeDelta = cookedSize;

//////        // ── 9. Enable raycasts ────────────────────────────────────────────────
//////        CanvasGroup cg = cookedObject.GetComponent<CanvasGroup>();
//////        if (cg != null)
//////        {
//////            cg.blocksRaycasts = true;
//////            cg.interactable = true;
//////            cg.alpha = 1f;
//////        }

//////        // ── 10. NOW enable BurnerGrill — timer starts from this moment ────────
//////        //    The grill is at the canvas root, visible, and draggable. The burn
//////        //    grace period begins here. PlateController.SnapGrill() will disable
//////        //    this component the instant the player drops the grill on a plate.
//////        if (burner != null) burner.enabled = true;

//////        Debug.Log($"[GrillCooker] '{cookedDrag?.grillType}' is cooked! " +
//////                  $"Burn grace timer started. Drag to a plate.");

//////        // ── 11. Play cooked SFX, free the stove slot, destroy raw grill ───────
//////        SoundManager.Instance.PlaySFX("Cooked");
//////        stove?.FreeSlot(slotIndex);
//////        Destroy(gameObject);
//////    }

//////    // ── Animation helpers ──────────────────────────────────────────────────

//////    private void StartAnim()
//////    {
//////        StopAnim();
//////        if (cookingAnimation?.frames == null || cookingAnimation.frames.Length == 0) return;
//////        _animRoutine = StartCoroutine(AnimRoutine());
//////    }

//////    private void StopAnim()
//////    {
//////        if (_animRoutine == null) return;
//////        StopCoroutine(_animRoutine);
//////        _animRoutine = null;
//////    }

//////    private IEnumerator AnimRoutine()
//////    {
//////        int frameCount = cookingAnimation.frames.Length;

//////        // Spread frames evenly across the full cookDuration so the last frame
//////        // lands exactly when the cook timer fires — regardless of the FPS value
//////        // set in the Inspector. e.g. 9 frames over 20 s = 2.22 s per frame.
//////        float delay = cookDuration / frameCount;

//////        int i = 0;

//////        while (true)
//////        {
//////            if (_image != null)
//////                _image.sprite = cookingAnimation.frames[i];

//////            yield return new WaitForSeconds(delay);

//////            i++;
//////            if (i >= frameCount)
//////                yield break;   // one-shot: stop on the last frame
//////        }
//////    }
//////}


////using UnityEngine;
////using UnityEngine.UI;
////using System.Collections;

////[System.Serializable]
////public class FrameAnimation
////{
////    [Tooltip("Drag all sprite frames here in order.")]
////    public Sprite[] frames;

////    [Min(1f)]
////    [Tooltip("Playback speed in frames per second.")]
////    public float fps = 12f;
////}

////public class GrillCooker : MonoBehaviour
////{
////    [Header("Cooking Animation")]
////    [Tooltip("Sprite frames played once over cookDuration seconds. Last frame held until done.")]
////    public FrameAnimation cookingAnimation;

////    [Tooltip("Seconds the cooking animation plays before the cooked object appears.")]
////    public float cookDuration = 15f;

////    [Header("Cooked Child Object")]
////    [Tooltip("The child GameObject with the cooked sprite.\n" +
////             "Set it INACTIVE in the Editor.\n" +
////             "Required components: Image, DraggableGrill, CanvasGroup, BurnerGrill (disabled).")]
////    public GameObject cookedObject;

////    [Header("Size Overrides  (0,0 = keep default)")]
////    public Vector2 cookingSize = Vector2.zero;
////    public Vector2 cookedSize = Vector2.zero;

////    [Header("Pulse  (shown when cooking finishes)")]
////    public float pulseSpeed = 3f;
////    [Range(0f, 0.25f)]
////    public float pulseAmount = 0.07f;

////    [HideInInspector] public StoveController stove;
////    [HideInInspector] public int slotIndex = -1;
////    [HideInInspector] public Canvas rootCanvas;

////    private Image _image;
////    private RectTransform _rect;
////    private Coroutine _animRoutine;

////    private void Awake()
////    {
////        _image = GetComponent<Image>();
////        _rect = GetComponent<RectTransform>();

////        if (cookedObject != null)
////        {
////            cookedObject.SetActive(false);

////            BurnerGrill burner = cookedObject.GetComponent<BurnerGrill>();
////            if (burner != null) burner.enabled = false;
////        }
////    }

////    public void StartCooking()
////    {
////        if (cookingSize != Vector2.zero && _rect != null)
////            _rect.sizeDelta = cookingSize;

////        SoundManager.Instance.PlaySFXLoop("Grilling");  // ← looping

////        StartAnim();
////        StartCoroutine(CookTimer());
////    }

////    private IEnumerator CookTimer()
////    {
////        yield return new WaitForSeconds(cookDuration);

////        SoundManager.Instance.StopSFXLoop();  // ← stop loop when done
////        StopAnim();

////        if (cookedObject == null)
////        {
////            Debug.LogWarning("[GrillCooker] cookedObject is not assigned!");
////            yield break;
////        }
////        if (rootCanvas == null)
////        {
////            Debug.LogWarning("[GrillCooker] rootCanvas is not set!");
////            yield break;
////        }

////        Vector3 slotWorldPos = transform.position;

////        if (_image != null) _image.enabled = false;

////        DraggableGrill myDrag = GetComponent<DraggableGrill>();
////        DraggableGrill cookedDrag = cookedObject.GetComponent<DraggableGrill>();
////        if (cookedDrag != null)
////        {
////            cookedDrag.grillType = myDrag != null ? myDrag.grillType : "Unknown";
////            cookedDrag.isCooked = true;
////            cookedDrag.pendingPulse = true;
////            cookedDrag.pendingPulseSpeed = pulseSpeed;
////            cookedDrag.pendingPulseAmount = pulseAmount;
////        }

////        BurnerGrill burner = cookedObject.GetComponent<BurnerGrill>();
////        if (burner != null) burner.enabled = false;

////        cookedObject.SetActive(true);

////        RectTransform cookedRect = cookedObject.GetComponent<RectTransform>();
////        cookedObject.transform.SetParent(rootCanvas.transform, false);

////        RectTransform canvasRect = rootCanvas.GetComponent<RectTransform>();
////        Camera uiCamera = rootCanvas.renderMode == RenderMode.ScreenSpaceOverlay
////                          ? null : rootCanvas.worldCamera;

////        RectTransformUtility.ScreenPointToLocalPointInRectangle(
////            canvasRect,
////            RectTransformUtility.WorldToScreenPoint(uiCamera, slotWorldPos),
////            uiCamera,
////            out Vector2 localPoint);

////        cookedRect.localPosition = localPoint;

////        if (cookedSize != Vector2.zero)
////            cookedRect.sizeDelta = cookedSize;

////        CanvasGroup cg = cookedObject.GetComponent<CanvasGroup>();
////        if (cg != null)
////        {
////            cg.blocksRaycasts = true;
////            cg.interactable = true;
////            cg.alpha = 1f;
////        }

////        if (burner != null) burner.enabled = true;

////        Debug.Log($"[GrillCooker] '{cookedDrag?.grillType}' is cooked! " +
////                  $"Burn grace timer started. Drag to a plate.");

////        SoundManager.Instance.PlaySFX("Cooked");
////        stove?.FreeSlot(slotIndex);
////        Destroy(gameObject);
////    }

////    private void StartAnim()
////    {
////        StopAnim();
////        if (cookingAnimation?.frames == null || cookingAnimation.frames.Length == 0) return;
////        _animRoutine = StartCoroutine(AnimRoutine());
////    }

////    private void StopAnim()
////    {
////        if (_animRoutine == null) return;
////        StopCoroutine(_animRoutine);
////        _animRoutine = null;
////    }

////    private IEnumerator AnimRoutine()
////    {
////        int frameCount = cookingAnimation.frames.Length;
////        float delay = cookDuration / frameCount;
////        int i = 0;

////        while (true)
////        {
////            if (_image != null)
////                _image.sprite = cookingAnimation.frames[i];

////            yield return new WaitForSeconds(delay);

////            i++;
////            if (i >= frameCount)
////                yield break;
////        }
////    }

////    private void OnDestroy()
////    {
////        SoundManager.Instance?.StopSFXLoop();
////    }
////}

//using UnityEngine;
//using UnityEngine.UI;
//using System.Collections;

//[System.Serializable]
//public class FrameAnimation
//{
//    [Tooltip("Drag all sprite frames here in order.")]
//    public Sprite[] frames;

//    [Min(1f)]
//    [Tooltip("Playback speed in frames per second.")]
//    public float fps = 12f;
//}

//public class GrillCooker : MonoBehaviour
//{
//    [Header("Cooking Animation")]
//    [Tooltip("Sprite frames played once over cookDuration seconds. Last frame held until done.")]
//    public FrameAnimation cookingAnimation;

//    [Tooltip("Seconds the cooking animation plays before the cooked object appears.")]
//    public float cookDuration = 15f;

//    [Header("Cooked Child Object")]
//    [Tooltip("The child GameObject with the cooked sprite.\n" +
//             "Set it INACTIVE in the Editor.\n" +
//             "Required components: Image, DraggableGrill, CanvasGroup, BurnerGrill (disabled).")]
//    public GameObject cookedObject;

//    [Header("Size Overrides  (0,0 = keep default)")]
//    public Vector2 cookingSize = Vector2.zero;
//    public Vector2 cookedSize = Vector2.zero;

//    [Header("Pulse  (shown when cooking finishes)")]
//    public float pulseSpeed = 3f;
//    [Range(0f, 0.25f)]
//    public float pulseAmount = 0.07f;

//    [HideInInspector] public StoveController stove;
//    [HideInInspector] public int slotIndex = -1;
//    [HideInInspector] public Canvas rootCanvas;

//    private Image _image;
//    private RectTransform _rect;
//    private Coroutine _animRoutine;
//    private static int _cookingCount = 0;

//    private void Awake()
//    {
//        _image = GetComponent<Image>();
//        _rect = GetComponent<RectTransform>();

//        if (cookedObject != null)
//        {
//            cookedObject.SetActive(false);

//            BurnerGrill burner = cookedObject.GetComponent<BurnerGrill>();
//            if (burner != null) burner.enabled = false;
//        }
//    }

//    //public void StartCooking()
//    //{
//    //    if (cookingSize != Vector2.zero && _rect != null)
//    //        _rect.sizeDelta = cookingSize;

//    //    SoundManager.Instance.PlaySFXLoop("Grilling");

//    //    StartAnim();
//    //    StartCoroutine(CookTimer());
//    //}

//    public void StartCooking()
//    {
//        if (cookingSize != Vector2.zero && _rect != null)
//            _rect.sizeDelta = cookingSize;

//        _cookingCount++;
//        SoundManager.Instance.PlaySFXLoop("Grilling");

//        StartAnim();
//        StartCoroutine(CookTimer());
//    }

//    private void OnDestroy()
//    {
//        _cookingCount = Mathf.Max(0, _cookingCount - 1);
//        if (_cookingCount == 0)
//            SoundManager.Instance?.StopSFXLoop();
//    }

//    //private void OnDestroy()
//    //{
//    //    SoundManager.Instance?.StopSFXLoop();
//    //}

//    private IEnumerator CookTimer()
//    {
//        yield return new WaitForSeconds(cookDuration);

//        //SoundManager.Instance.StopSFXLoop();
//        StopAnim();

//        if (cookedObject == null)
//        {
//            Debug.LogWarning("[GrillCooker] cookedObject is not assigned!");
//            yield break;
//        }

//        if (_image != null) _image.enabled = false;

//        DraggableGrill myDrag = GetComponent<DraggableGrill>();
//        DraggableGrill cookedDrag = cookedObject.GetComponent<DraggableGrill>();
//        if (cookedDrag != null)
//        {
//            cookedDrag.grillType = myDrag != null ? myDrag.grillType : "Unknown";
//            cookedDrag.isCooked = true;
//            cookedDrag.pendingPulse = true;
//            cookedDrag.pendingPulseSpeed = pulseSpeed;
//            cookedDrag.pendingPulseAmount = pulseAmount;
//        }

//        BurnerGrill burner = cookedObject.GetComponent<BurnerGrill>();
//        if (burner != null) burner.enabled = false;

//        RectTransform cookedRect = cookedObject.GetComponent<RectTransform>();

//        // Reparent to the same stove slot anchor that the raw grill occupied
//        // BEFORE activating, so OnEnable's SaveOrigin() captures slotAnchor as
//        // the correct pre-drag parent (the raw grill's GameObject is destroyed
//        // at the end of this routine, so it must not be the saved parent).
//        RectTransform slotAnchor = (stove != null && slotIndex >= 0 && slotIndex < stove.stoveSlots.Length)
//            ? stove.stoveSlots[slotIndex].anchor
//            : null;

//        if (slotAnchor != null)
//        {
//            cookedObject.transform.SetParent(slotAnchor, false);
//            cookedRect.anchoredPosition = Vector2.zero;
//            cookedRect.localScale = Vector3.one;
//        }
//        else if (rootCanvas != null)
//        {
//            Debug.LogWarning("[GrillCooker] No valid stove slot anchor — falling back to canvas root.");
//            Vector3 slotWorldPos = transform.position;
//            cookedObject.transform.SetParent(rootCanvas.transform, false);

//            RectTransform canvasRect = rootCanvas.GetComponent<RectTransform>();
//            Camera uiCamera = rootCanvas.renderMode == RenderMode.ScreenSpaceOverlay
//                              ? null : rootCanvas.worldCamera;

//            RectTransformUtility.ScreenPointToLocalPointInRectangle(
//                canvasRect,
//                RectTransformUtility.WorldToScreenPoint(uiCamera, slotWorldPos),
//                uiCamera,
//                out Vector2 localPoint);

//            cookedRect.localPosition = localPoint;
//        }

//        if (cookedSize != Vector2.zero)
//            cookedRect.sizeDelta = cookedSize;

//        // Activate AFTER reparenting and resizing — OnEnable (pendingPulse →
//        // SaveOrigin) now snapshots the correct slot-anchor parent and final size.
//        cookedObject.SetActive(true);

//        CanvasGroup cg = cookedObject.GetComponent<CanvasGroup>();
//        if (cg != null)
//        {
//            cg.blocksRaycasts = true;
//            cg.interactable = true;
//            cg.alpha = 1f;
//        }

//        if (burner != null) burner.enabled = true;

//        Debug.Log($"[GrillCooker] '{cookedDrag?.grillType}' is cooked! " +
//                  $"Burn grace timer started. Drag to a plate.");

//        SoundManager.Instance.PlaySFX("Cooked");
//        stove?.FreeSlot(slotIndex);
//        Destroy(gameObject);
//    }

//    private void StartAnim()
//    {
//        StopAnim();
//        if (cookingAnimation?.frames == null || cookingAnimation.frames.Length == 0) return;
//        _animRoutine = StartCoroutine(AnimRoutine());
//    }

//    private void StopAnim()
//    {
//        if (_animRoutine == null) return;
//        StopCoroutine(_animRoutine);
//        _animRoutine = null;
//    }

//    private IEnumerator AnimRoutine()
//    {
//        int frameCount = cookingAnimation.frames.Length;
//        float delay = cookDuration / frameCount;
//        int i = 0;

//        while (true)
//        {
//            if (_image != null)
//                _image.sprite = cookingAnimation.frames[i];

//            yield return new WaitForSeconds(delay);

//            i++;
//            if (i >= frameCount)
//                yield break;
//        }
//    }
//}

//////using UnityEngine;
//////using UnityEngine.UI;
//////using System.Collections;
//////using DG.Tweening;

//////[System.Serializable]
//////public class FrameAnimation
//////{
//////    [Tooltip("Drag all sprite frames here in order.")]
//////    public Sprite[] frames;

//////    [Min(1f)]
//////    [Tooltip("Playback speed in frames per second.")]
//////    public float fps = 12f;
//////}

///////// <summary>
///////// Attach to the raw grill prefab root (alongside DraggableGrill).
/////////
///////// PREFAB HIERARCHY
/////////   GrillRaw  (root — this script lives here)
/////////     ├─ Image          raw grill sprite / cooking animation frames
/////////     ├─ DraggableGrill (isCooked = false)
/////////     ├─ CanvasGroup
/////////     └─ GrillCooked   ← child, set INACTIVE in the Editor
/////////           ├─ Image          cooked grill sprite
/////////           ├─ DraggableGrill (isCooked set true at runtime)
/////////           ├─ CanvasGroup
/////////           └─ BurnerGrill    (disabled — GrillCooker enables it after cooking)
/////////
///////// FLOW
/////////   StoveController calls StartCooking()
/////////     → cooking animation plays once for cookDuration seconds
/////////     → grilling SFX loops for the full cookDuration (via coroutine)
/////////     → root Image disabled
/////////     → pulse params + isCooked written onto GrillCooked's DraggableGrill BEFORE SetActive
/////////     → GrillCooked.SetActive(true)  — its OnEnable starts the pulse safely
/////////     → GrillCooked reparented to Canvas root at the exact same screen position
/////////     → CanvasGroup.blocksRaycasts = true
/////////     → BurnerGrill enabled (burn timer starts from this moment,
/////////       not from when the prefab first activated)
/////////     → stove slot freed, raw grill destroyed
/////////
///////// BurnerGrill lifecycle:
/////////   • Starts DISABLED on the cooked child prefab (set in Inspector / Awake).
/////////   • GrillCooker enables it here, after the grill is live at the canvas root.
/////////   • PlateController.SnapGrill() disables it when the grill lands on a plate.
/////////   • DraggableGrill.TryDropOnDustbin() disables it when discarded.
/////////   This ensures the burn timer ONLY runs while the grill is floating unplated.
///////// </summary>
//////public class GrillCooker : MonoBehaviour
//////{
//////    [Header("Cooking Animation")]
//////    [Tooltip("Sprite frames played once over cookDuration seconds. Last frame held until done.")]
//////    public FrameAnimation cookingAnimation;

//////    [Tooltip("Seconds the cooking animation plays before the cooked object appears.")]
//////    public float cookDuration = 15f;

//////    [Header("Cooked Child Object")]
//////    [Tooltip("The child GameObject with the cooked sprite.\n" +
//////             "Set it INACTIVE in the Editor.\n" +
//////             "Required components: Image, DraggableGrill, CanvasGroup, BurnerGrill (disabled).")]
//////    public GameObject cookedObject;

//////    [Header("Size Overrides  (0,0 = keep default)")]
//////    public Vector2 cookingSize = Vector2.zero;
//////    public Vector2 cookedSize = Vector2.zero;

//////    [Header("Pulse  (shown when cooking finishes)")]
//////    public float pulseSpeed = 3f;
//////    [Range(0f, 0.25f)]
//////    public float pulseAmount = 0.07f;

//////    [Header("Grilling Sound")]
//////    [Tooltip("Must match the SFX name in AudioData exactly.")]
//////    public string grillingSFXName = "Grilling";
//////    [Tooltip("Set this to the exact duration of your Grilling audio clip in seconds.\n" +
//////             "Check it by selecting the clip in the Project window.")]
//////    public float grillingClipLength = 5f;

//////    // ── Set by StoveController ─────────────────────────────────────────────
//////    [HideInInspector] public StoveController stove;
//////    [HideInInspector] public int slotIndex = -1;
//////    [HideInInspector] public Canvas rootCanvas;

//////    // ── Private ────────────────────────────────────────────────────────────
//////    private Image _image;
//////    private RectTransform _rect;
//////    private Coroutine _animRoutine;
//////    private Sequence _grillSoundSequence;

//////    private void Awake()
//////    {
//////        _image = GetComponent<Image>();
//////        _rect = GetComponent<RectTransform>();

//////        if (cookedObject != null)
//////        {
//////            cookedObject.SetActive(false);

//////            // Ensure BurnerGrill starts disabled — GrillCooker will enable it
//////            // at the right moment after cooking completes.
//////            BurnerGrill burner = cookedObject.GetComponent<BurnerGrill>();
//////            if (burner != null) burner.enabled = false;
//////        }
//////    }

//////    // ── Entry point called by StoveController ──────────────────────────────

//////    public void StartCooking()
//////    {
//////        if (cookingSize != Vector2.zero && _rect != null)
//////            _rect.sizeDelta = cookingSize;

//////        // Play immediately, then repeat every clipLength — no gap
//////        SoundManager.Instance.PlaySFX(grillingSFXName);
//////        _grillSoundSequence = DOTween.Sequence();
//////        _grillSoundSequence
//////            .AppendInterval(grillingClipLength)
//////            .AppendCallback(() => SoundManager.Instance.PlaySFX(grillingSFXName))
//////            .SetLoops(-1);

//////        StartAnim();
//////        StartCoroutine(CookTimer());
//////    }

//////    private void StopGrillSound()
//////    {
//////        _grillSoundSequence?.Kill();
//////        _grillSoundSequence = null;
//////    }

//////    // ── Cook timer ─────────────────────────────────────────────────────────

//////    private IEnumerator CookTimer()
//////    {
//////        yield return new WaitForSeconds(cookDuration);

//////        StopGrillSound();  // stop looping grilling SFX
//////        StopAnim();        // freeze on last cooking frame

//////        if (cookedObject == null)
//////        {
//////            Debug.LogWarning("[GrillCooker] cookedObject is not assigned!");
//////            yield break;
//////        }
//////        if (rootCanvas == null)
//////        {
//////            Debug.LogWarning("[GrillCooker] rootCanvas is not set!");
//////            yield break;
//////        }

//////        // ── 1. Capture stove-slot world position BEFORE anything moves ───────
//////        Vector3 slotWorldPos = transform.position;

//////        // ── 2. Hide cooking visual ────────────────────────────────────────────
//////        if (_image != null) _image.enabled = false;

//////        // ── 3. Write data onto cooked child's DraggableGrill BEFORE SetActive ─
//////        DraggableGrill myDrag = GetComponent<DraggableGrill>();
//////        DraggableGrill cookedDrag = cookedObject.GetComponent<DraggableGrill>();
//////        if (cookedDrag != null)
//////        {
//////            cookedDrag.grillType = myDrag != null ? myDrag.grillType : "Unknown";
//////            cookedDrag.isCooked = true;
//////            cookedDrag.pendingPulse = true;
//////            cookedDrag.pendingPulseSpeed = pulseSpeed;
//////            cookedDrag.pendingPulseAmount = pulseAmount;
//////        }

//////        // ── 4. Make sure BurnerGrill is DISABLED before SetActive ─────────────
//////        //    OnEnable would start the timer immediately — we want it to start
//////        //    only after the grill is positioned at the canvas root (step 9).
//////        BurnerGrill burner = cookedObject.GetComponent<BurnerGrill>();
//////        if (burner != null) burner.enabled = false;

//////        // ── 5. Activate the cooked child ──────────────────────────────────────
//////        //    DraggableGrill.Awake() runs (blocksRaycasts = false, alpha hit test).
//////        //    DraggableGrill.OnEnable() runs (pendingPulse → SaveOrigin + pulse).
//////        //    BurnerGrill does NOT run (still disabled).
//////        cookedObject.SetActive(true);

//////        // ── 6. Reparent to canvas root ────────────────────────────────────────
//////        RectTransform cookedRect = cookedObject.GetComponent<RectTransform>();
//////        cookedObject.transform.SetParent(rootCanvas.transform, false);

//////        // ── 7. Restore exact screen position ──────────────────────────────────
//////        RectTransform canvasRect = rootCanvas.GetComponent<RectTransform>();
//////        Camera uiCamera = rootCanvas.renderMode == RenderMode.ScreenSpaceOverlay
//////                          ? null : rootCanvas.worldCamera;

//////        RectTransformUtility.ScreenPointToLocalPointInRectangle(
//////            canvasRect,
//////            RectTransformUtility.WorldToScreenPoint(uiCamera, slotWorldPos),
//////            uiCamera,
//////            out Vector2 localPoint);

//////        cookedRect.localPosition = localPoint;

//////        // ── 8. Apply optional size override ───────────────────────────────────
//////        if (cookedSize != Vector2.zero)
//////            cookedRect.sizeDelta = cookedSize;

//////        // ── 9. Enable raycasts ────────────────────────────────────────────────
//////        CanvasGroup cg = cookedObject.GetComponent<CanvasGroup>();
//////        if (cg != null)
//////        {
//////            cg.blocksRaycasts = true;
//////            cg.interactable = true;
//////            cg.alpha = 1f;
//////        }

//////        // ── 10. NOW enable BurnerGrill — timer starts from this moment ────────
//////        //    The grill is at the canvas root, visible, and draggable. The burn
//////        //    grace period begins here. PlateController.SnapGrill() will disable
//////        //    this component the instant the player drops the grill on a plate.
//////        if (burner != null) burner.enabled = true;

//////        Debug.Log($"[GrillCooker] '{cookedDrag?.grillType}' is cooked! " +
//////                  $"Burn grace timer started. Drag to a plate.");

//////        // ── 11. Play cooked SFX, free the stove slot, destroy raw grill ───────
//////        SoundManager.Instance.PlaySFX("Cooked");
//////        stove?.FreeSlot(slotIndex);
//////        Destroy(gameObject);
//////    }

//////    // ── Animation helpers ──────────────────────────────────────────────────

//////    private void StartAnim()
//////    {
//////        StopAnim();
//////        if (cookingAnimation?.frames == null || cookingAnimation.frames.Length == 0) return;
//////        _animRoutine = StartCoroutine(AnimRoutine());
//////    }

//////    private void StopAnim()
//////    {
//////        if (_animRoutine == null) return;
//////        StopCoroutine(_animRoutine);
//////        _animRoutine = null;
//////    }

//////    private IEnumerator AnimRoutine()
//////    {
//////        int frameCount = cookingAnimation.frames.Length;

//////        // Spread frames evenly across the full cookDuration so the last frame
//////        // lands exactly when the cook timer fires — regardless of the FPS value
//////        // set in the Inspector. e.g. 9 frames over 20 s = 2.22 s per frame.
//////        float delay = cookDuration / frameCount;

//////        int i = 0;

//////        while (true)
//////        {
//////            if (_image != null)
//////                _image.sprite = cookingAnimation.frames[i];

//////            yield return new WaitForSeconds(delay);

//////            i++;
//////            if (i >= frameCount)
//////                yield break;   // one-shot: stop on the last frame
//////        }
//////    }
//////}

////using UnityEngine;
////using UnityEngine.UI;
////using System.Collections;
////using DG.Tweening;

////[System.Serializable]
////public class FrameAnimation
////{
////    [Tooltip("Drag all sprite frames here in order.")]
////    public Sprite[] frames;

////    [Min(1f)]
////    [Tooltip("Playback speed in frames per second.")]
////    public float fps = 12f;
////}

/////// <summary>
/////// Attach to the raw grill prefab root (alongside DraggableGrill).
///////
/////// PREFAB HIERARCHY
///////   GrillRaw  (root — this script lives here)
///////     ├─ Image          raw grill sprite / cooking animation frames
///////     ├─ DraggableGrill (isCooked = false)
///////     ├─ CanvasGroup
///////     └─ GrillCooked   ← child, set INACTIVE in the Editor
///////           ├─ Image          cooked grill sprite
///////           ├─ DraggableGrill (isCooked set true at runtime)
///////           ├─ CanvasGroup
///////           └─ BurnerGrill    (disabled — GrillCooker enables it after cooking)
///////
/////// FLOW
///////   StoveController calls StartCooking()
///////     → cooking animation plays once for cookDuration seconds
///////     → grilling SFX loops for the full cookDuration (via coroutine)
///////     → root Image disabled
///////     → pulse params + isCooked written onto GrillCooked's DraggableGrill BEFORE SetActive
///////     → GrillCooked.SetActive(true)  — its OnEnable starts the pulse safely
///////     → GrillCooked reparented to Canvas root at the exact same screen position
///////     → CanvasGroup.blocksRaycasts = true
///////     → BurnerGrill enabled (burn timer starts from this moment,
///////       not from when the prefab first activated)
///////     → stove slot freed, raw grill destroyed
///////
/////// BurnerGrill lifecycle:
///////   • Starts DISABLED on the cooked child prefab (set in Inspector / Awake).
///////   • GrillCooker enables it here, after the grill is live at the canvas root.
///////   • PlateController.SnapGrill() disables it when the grill lands on a plate.
///////   • DraggableGrill.TryDropOnDustbin() disables it when discarded.
///////   This ensures the burn timer ONLY runs while the grill is floating unplated.
/////// </summary>
////public class GrillCooker : MonoBehaviour
////{
////    [Header("Cooking Animation")]
////    [Tooltip("Sprite frames played once over cookDuration seconds. Last frame held until done.")]
////    public FrameAnimation cookingAnimation;

////    [Tooltip("Seconds the cooking animation plays before the cooked object appears.")]
////    public float cookDuration = 15f;

////    [Header("Cooked Child Object")]
////    [Tooltip("The child GameObject with the cooked sprite.\n" +
////             "Set it INACTIVE in the Editor.\n" +
////             "Required components: Image, DraggableGrill, CanvasGroup, BurnerGrill (disabled).")]
////    public GameObject cookedObject;

////    [Header("Size Overrides  (0,0 = keep default)")]
////    public Vector2 cookingSize = Vector2.zero;
////    public Vector2 cookedSize = Vector2.zero;

////    [Header("Pulse  (shown when cooking finishes)")]
////    public float pulseSpeed = 3f;
////    [Range(0f, 0.25f)]
////    public float pulseAmount = 0.07f;

////    [Header("Grilling Sound")]
////    [Tooltip("Must match the SFX name in AudioData exactly.")]
////    public string grillingSFXName = "Grilling";
////    [Tooltip("Set this to the exact duration of your Grilling audio clip in seconds.\n" +
////             "Check it by selecting the clip in the Project window.")]
////    public float grillingClipLength = 5f;

////    // ── Set by StoveController ─────────────────────────────────────────────
////    [HideInInspector] public StoveController stove;
////    [HideInInspector] public int slotIndex = -1;
////    [HideInInspector] public Canvas rootCanvas;

////    // ── Private ────────────────────────────────────────────────────────────
////    private Image _image;
////    private RectTransform _rect;
////    private Coroutine _animRoutine;
////    private Coroutine _grillSoundRoutine;

////    private void Awake()
////    {
////        _image = GetComponent<Image>();
////        _rect = GetComponent<RectTransform>();

////        if (cookedObject != null)
////        {
////            cookedObject.SetActive(false);

////            // Ensure BurnerGrill starts disabled — GrillCooker will enable it
////            // at the right moment after cooking completes.
////            BurnerGrill burner = cookedObject.GetComponent<BurnerGrill>();
////            if (burner != null) burner.enabled = false;
////        }
////    }

////    // ── Entry point called by StoveController ──────────────────────────────

////    public void StartCooking()
////    {
////        if (cookingSize != Vector2.zero && _rect != null)
////            _rect.sizeDelta = cookingSize;

////        _grillSoundRoutine = StartCoroutine(GrillSoundLoop());

////        StartAnim();
////        StartCoroutine(CookTimer());
////    }

////    private IEnumerator GrillSoundLoop()
////    {
////        float clipLength = SoundManager.Instance.GetSFXClipLength(grillingSFXName);
////        if (clipLength <= 0f) yield break;

////        double nextPlayTime = AudioSettings.dspTime;

////        while (true)
////        {
////            SoundManager.Instance.PlaySFX(grillingSFXName);
////            nextPlayTime += clipLength;
////            float waitTime = (float)(nextPlayTime - AudioSettings.dspTime) - 0.02f; // fire slightly early
////            yield return new WaitForSeconds(waitTime);
////        }
////    }

////    private void StopGrillSound()
////    {
////        if (_grillSoundRoutine != null)
////        {
////            StopCoroutine(_grillSoundRoutine);
////            _grillSoundRoutine = null;
////        }
////    }

////    // ── Cook timer ─────────────────────────────────────────────────────────

////    private IEnumerator CookTimer()
////    {
////        yield return new WaitForSeconds(cookDuration);

////        StopGrillSound();  // stop looping grilling SFX
////        StopAnim();        // freeze on last cooking frame

////        if (cookedObject == null)
////        {
////            Debug.LogWarning("[GrillCooker] cookedObject is not assigned!");
////            yield break;
////        }
////        if (rootCanvas == null)
////        {
////            Debug.LogWarning("[GrillCooker] rootCanvas is not set!");
////            yield break;
////        }

////        // ── 1. Capture stove-slot world position BEFORE anything moves ───────
////        Vector3 slotWorldPos = transform.position;

////        // ── 2. Hide cooking visual ────────────────────────────────────────────
////        if (_image != null) _image.enabled = false;

////        // ── 3. Write data onto cooked child's DraggableGrill BEFORE SetActive ─
////        DraggableGrill myDrag = GetComponent<DraggableGrill>();
////        DraggableGrill cookedDrag = cookedObject.GetComponent<DraggableGrill>();
////        if (cookedDrag != null)
////        {
////            cookedDrag.grillType = myDrag != null ? myDrag.grillType : "Unknown";
////            cookedDrag.isCooked = true;
////            cookedDrag.pendingPulse = true;
////            cookedDrag.pendingPulseSpeed = pulseSpeed;
////            cookedDrag.pendingPulseAmount = pulseAmount;
////        }

////        // ── 4. Make sure BurnerGrill is DISABLED before SetActive ─────────────
////        //    OnEnable would start the timer immediately — we want it to start
////        //    only after the grill is positioned at the canvas root (step 9).
////        BurnerGrill burner = cookedObject.GetComponent<BurnerGrill>();
////        if (burner != null) burner.enabled = false;

////        // ── 5. Activate the cooked child ──────────────────────────────────────
////        //    DraggableGrill.Awake() runs (blocksRaycasts = false, alpha hit test).
////        //    DraggableGrill.OnEnable() runs (pendingPulse → SaveOrigin + pulse).
////        //    BurnerGrill does NOT run (still disabled).
////        cookedObject.SetActive(true);

////        // ── 6. Reparent to canvas root ────────────────────────────────────────
////        RectTransform cookedRect = cookedObject.GetComponent<RectTransform>();
////        cookedObject.transform.SetParent(rootCanvas.transform, false);

////        // ── 7. Restore exact screen position ──────────────────────────────────
////        RectTransform canvasRect = rootCanvas.GetComponent<RectTransform>();
////        Camera uiCamera = rootCanvas.renderMode == RenderMode.ScreenSpaceOverlay
////                          ? null : rootCanvas.worldCamera;

////        RectTransformUtility.ScreenPointToLocalPointInRectangle(
////            canvasRect,
////            RectTransformUtility.WorldToScreenPoint(uiCamera, slotWorldPos),
////            uiCamera,
////            out Vector2 localPoint);

////        cookedRect.localPosition = localPoint;

////        // ── 8. Apply optional size override ───────────────────────────────────
////        if (cookedSize != Vector2.zero)
////            cookedRect.sizeDelta = cookedSize;

////        // ── 9. Enable raycasts ────────────────────────────────────────────────
////        CanvasGroup cg = cookedObject.GetComponent<CanvasGroup>();
////        if (cg != null)
////        {
////            cg.blocksRaycasts = true;
////            cg.interactable = true;
////            cg.alpha = 1f;
////        }

////        // ── 10. NOW enable BurnerGrill — timer starts from this moment ────────
////        //    The grill is at the canvas root, visible, and draggable. The burn
////        //    grace period begins here. PlateController.SnapGrill() will disable
////        //    this component the instant the player drops the grill on a plate.
////        if (burner != null) burner.enabled = true;

////        Debug.Log($"[GrillCooker] '{cookedDrag?.grillType}' is cooked! " +
////                  $"Burn grace timer started. Drag to a plate.");

////        // ── 11. Play cooked SFX, free the stove slot, destroy raw grill ───────
////        SoundManager.Instance.PlaySFX("Cooked");
////        stove?.FreeSlot(slotIndex);
////        Destroy(gameObject);
////    }

////    // ── Animation helpers ──────────────────────────────────────────────────

////    private void StartAnim()
////    {
////        StopAnim();
////        if (cookingAnimation?.frames == null || cookingAnimation.frames.Length == 0) return;
////        _animRoutine = StartCoroutine(AnimRoutine());
////    }

////    private void StopAnim()
////    {
////        if (_animRoutine == null) return;
////        StopCoroutine(_animRoutine);
////        _animRoutine = null;
////    }

////    private IEnumerator AnimRoutine()
////    {
////        int frameCount = cookingAnimation.frames.Length;

////        // Spread frames evenly across the full cookDuration so the last frame
////        // lands exactly when the cook timer fires — regardless of the FPS value
////        // set in the Inspector. e.g. 9 frames over 20 s = 2.22 s per frame.
////        float delay = cookDuration / frameCount;

////        int i = 0;

////        while (true)
////        {
////            if (_image != null)
////                _image.sprite = cookingAnimation.frames[i];

////            yield return new WaitForSeconds(delay);

////            i++;
////            if (i >= frameCount)
////                yield break;   // one-shot: stop on the last frame
////        }
////    }
////}


//using UnityEngine;
//using UnityEngine.UI;
//using System.Collections;

//[System.Serializable]
//public class FrameAnimation
//{
//    [Tooltip("Drag all sprite frames here in order.")]
//    public Sprite[] frames;

//    [Min(1f)]
//    [Tooltip("Playback speed in frames per second.")]
//    public float fps = 12f;
//}

//public class GrillCooker : MonoBehaviour
//{
//    [Header("Cooking Animation")]
//    [Tooltip("Sprite frames played once over cookDuration seconds. Last frame held until done.")]
//    public FrameAnimation cookingAnimation;

//    [Tooltip("Seconds the cooking animation plays before the cooked object appears.")]
//    public float cookDuration = 15f;

//    [Header("Cooked Child Object")]
//    [Tooltip("The child GameObject with the cooked sprite.\n" +
//             "Set it INACTIVE in the Editor.\n" +
//             "Required components: Image, DraggableGrill, CanvasGroup, BurnerGrill (disabled).")]
//    public GameObject cookedObject;

//    [Header("Size Overrides  (0,0 = keep default)")]
//    public Vector2 cookingSize = Vector2.zero;
//    public Vector2 cookedSize = Vector2.zero;

//    [Header("Pulse  (shown when cooking finishes)")]
//    public float pulseSpeed = 3f;
//    [Range(0f, 0.25f)]
//    public float pulseAmount = 0.07f;

//    [HideInInspector] public StoveController stove;
//    [HideInInspector] public int slotIndex = -1;
//    [HideInInspector] public Canvas rootCanvas;

//    private Image _image;
//    private RectTransform _rect;
//    private Coroutine _animRoutine;

//    private void Awake()
//    {
//        _image = GetComponent<Image>();
//        _rect = GetComponent<RectTransform>();

//        if (cookedObject != null)
//        {
//            cookedObject.SetActive(false);

//            BurnerGrill burner = cookedObject.GetComponent<BurnerGrill>();
//            if (burner != null) burner.enabled = false;
//        }
//    }

//    public void StartCooking()
//    {
//        if (cookingSize != Vector2.zero && _rect != null)
//            _rect.sizeDelta = cookingSize;

//        SoundManager.Instance.PlaySFXLoop("Grilling");  // ← looping

//        StartAnim();
//        StartCoroutine(CookTimer());
//    }

//    private IEnumerator CookTimer()
//    {
//        yield return new WaitForSeconds(cookDuration);

//        SoundManager.Instance.StopSFXLoop();  // ← stop loop when done
//        StopAnim();

//        if (cookedObject == null)
//        {
//            Debug.LogWarning("[GrillCooker] cookedObject is not assigned!");
//            yield break;
//        }
//        if (rootCanvas == null)
//        {
//            Debug.LogWarning("[GrillCooker] rootCanvas is not set!");
//            yield break;
//        }

//        Vector3 slotWorldPos = transform.position;

//        if (_image != null) _image.enabled = false;

//        DraggableGrill myDrag = GetComponent<DraggableGrill>();
//        DraggableGrill cookedDrag = cookedObject.GetComponent<DraggableGrill>();
//        if (cookedDrag != null)
//        {
//            cookedDrag.grillType = myDrag != null ? myDrag.grillType : "Unknown";
//            cookedDrag.isCooked = true;
//            cookedDrag.pendingPulse = true;
//            cookedDrag.pendingPulseSpeed = pulseSpeed;
//            cookedDrag.pendingPulseAmount = pulseAmount;
//        }

//        BurnerGrill burner = cookedObject.GetComponent<BurnerGrill>();
//        if (burner != null) burner.enabled = false;

//        cookedObject.SetActive(true);

//        RectTransform cookedRect = cookedObject.GetComponent<RectTransform>();
//        cookedObject.transform.SetParent(rootCanvas.transform, false);

//        RectTransform canvasRect = rootCanvas.GetComponent<RectTransform>();
//        Camera uiCamera = rootCanvas.renderMode == RenderMode.ScreenSpaceOverlay
//                          ? null : rootCanvas.worldCamera;

//        RectTransformUtility.ScreenPointToLocalPointInRectangle(
//            canvasRect,
//            RectTransformUtility.WorldToScreenPoint(uiCamera, slotWorldPos),
//            uiCamera,
//            out Vector2 localPoint);

//        cookedRect.localPosition = localPoint;

//        if (cookedSize != Vector2.zero)
//            cookedRect.sizeDelta = cookedSize;

//        CanvasGroup cg = cookedObject.GetComponent<CanvasGroup>();
//        if (cg != null)
//        {
//            cg.blocksRaycasts = true;
//            cg.interactable = true;
//            cg.alpha = 1f;
//        }

//        if (burner != null) burner.enabled = true;

//        Debug.Log($"[GrillCooker] '{cookedDrag?.grillType}' is cooked! " +
//                  $"Burn grace timer started. Drag to a plate.");

//        SoundManager.Instance.PlaySFX("Cooked");
//        stove?.FreeSlot(slotIndex);
//        Destroy(gameObject);
//    }

//    private void StartAnim()
//    {
//        StopAnim();
//        if (cookingAnimation?.frames == null || cookingAnimation.frames.Length == 0) return;
//        _animRoutine = StartCoroutine(AnimRoutine());
//    }

//    private void StopAnim()
//    {
//        if (_animRoutine == null) return;
//        StopCoroutine(_animRoutine);
//        _animRoutine = null;
//    }

//    private IEnumerator AnimRoutine()
//    {
//        int frameCount = cookingAnimation.frames.Length;
//        float delay = cookDuration / frameCount;
//        int i = 0;

//        while (true)
//        {
//            if (_image != null)
//                _image.sprite = cookingAnimation.frames[i];

//            yield return new WaitForSeconds(delay);

//            i++;
//            if (i >= frameCount)
//                yield break;
//        }
//    }

//    private void OnDestroy()
//    {
//        SoundManager.Instance?.StopSFXLoop();
//    }
//}

using UnityEngine;
using UnityEngine.UI;
using System.Collections;

[System.Serializable]
public class FrameAnimation
{
    [Tooltip("Drag all sprite frames here in order.")]
    public Sprite[] frames;

    [Min(1f)]
    [Tooltip("Playback speed in frames per second.")]
    public float fps = 12f;
}

public class GrillCooker : MonoBehaviour
{
    [Header("Cooking Animation")]
    [Tooltip("Sprite frames played once over cookDuration seconds. Last frame held until done.")]
    public FrameAnimation cookingAnimation;

    [Tooltip("Seconds the cooking animation plays before the cooked object appears.")]
    public float cookDuration = 15f;

    [Header("Cooked Child Object")]
    [Tooltip("The child GameObject with the cooked sprite.\n" +
             "Set it INACTIVE in the Editor.\n" +
             "Required components: Image, DraggableGrill, CanvasGroup, BurnerGrill (disabled).")]
    public GameObject cookedObject;

    [Header("Size Overrides  (0,0 = keep default)")]
    public Vector2 cookingSize = Vector2.zero;
    public Vector2 cookedSize = Vector2.zero;

    [Header("Pulse  (shown when cooking finishes)")]
    public float pulseSpeed = 3f;
    [Range(0f, 0.25f)]
    public float pulseAmount = 0.07f;

    [HideInInspector] public StoveController stove;
    [HideInInspector] public int slotIndex = -1;
    [HideInInspector] public Canvas rootCanvas;

    private Image _image;
    private RectTransform _rect;
    private Coroutine _animRoutine;
    private static int _cookingCount = 0;

    private void Awake()
    {
        _image = GetComponent<Image>();
        _rect = GetComponent<RectTransform>();

        if (cookedObject != null)
        {
            cookedObject.SetActive(false);

            BurnerGrill burner = cookedObject.GetComponent<BurnerGrill>();
            if (burner != null) burner.enabled = false;
        }
    }

    //public void StartCooking()
    //{
    //    if (cookingSize != Vector2.zero && _rect != null)
    //        _rect.sizeDelta = cookingSize;

    //    SoundManager.Instance.PlaySFXLoop("Grilling");

    //    StartAnim();
    //    StartCoroutine(CookTimer());
    //}

    public void StartCooking()
    {
        if (cookingSize != Vector2.zero && _rect != null)
            _rect.sizeDelta = cookingSize;

        // Lock the raw grill — player cannot drag it off the stove while cooking.
        // The raw grill GameObject is Destroyed when CookTimer finishes, so
        // there is no need to re-enable DraggableGrill afterwards.
        DraggableGrill rawDrag = GetComponent<DraggableGrill>();
        if (rawDrag != null) rawDrag.enabled = false;

        _cookingCount++;
        SoundManager.Instance.PlaySFXLoop("Grilling");

        StartAnim();
        StartCoroutine(CookTimer());
    }

    private void OnDestroy()
    {
        _cookingCount = Mathf.Max(0, _cookingCount - 1);
        if (_cookingCount == 0)
            SoundManager.Instance?.StopSFXLoop();
    }

    //private void OnDestroy()
    //{
    //    SoundManager.Instance?.StopSFXLoop();
    //}

    private IEnumerator CookTimer()
    {
        yield return new WaitForSeconds(cookDuration);

        //SoundManager.Instance.StopSFXLoop();
        StopAnim();

        if (cookedObject == null)
        {
            Debug.LogWarning("[GrillCooker] cookedObject is not assigned!");
            yield break;
        }

        if (_image != null) _image.enabled = false;

        DraggableGrill myDrag = GetComponent<DraggableGrill>();
        DraggableGrill cookedDrag = cookedObject.GetComponent<DraggableGrill>();
        if (cookedDrag != null)
        {
            cookedDrag.grillType = myDrag != null ? myDrag.grillType : "Unknown";
            cookedDrag.isCooked = true;
            cookedDrag.pendingPulse = true;
            cookedDrag.pendingPulseSpeed = pulseSpeed;
            cookedDrag.pendingPulseAmount = pulseAmount;
        }

        BurnerGrill burner = cookedObject.GetComponent<BurnerGrill>();
        if (burner != null) burner.enabled = false;

        RectTransform cookedRect = cookedObject.GetComponent<RectTransform>();

        // Reparent to the same stove slot anchor that the raw grill occupied
        // BEFORE activating, so OnEnable's SaveOrigin() captures slotAnchor as
        // the correct pre-drag parent (the raw grill's GameObject is destroyed
        // at the end of this routine, so it must not be the saved parent).
        RectTransform slotAnchor = (stove != null && slotIndex >= 0 && slotIndex < stove.stoveSlots.Length)
            ? stove.stoveSlots[slotIndex].anchor
            : null;

        if (slotAnchor != null)
        {
            cookedObject.transform.SetParent(slotAnchor, false);
            cookedRect.anchoredPosition = Vector2.zero;
            cookedRect.localScale = Vector3.one;
        }
        else if (rootCanvas != null)
        {
            Debug.LogWarning("[GrillCooker] No valid stove slot anchor — falling back to canvas root.");
            Vector3 slotWorldPos = transform.position;
            cookedObject.transform.SetParent(rootCanvas.transform, false);

            RectTransform canvasRect = rootCanvas.GetComponent<RectTransform>();
            Camera uiCamera = rootCanvas.renderMode == RenderMode.ScreenSpaceOverlay
                              ? null : rootCanvas.worldCamera;

            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                canvasRect,
                RectTransformUtility.WorldToScreenPoint(uiCamera, slotWorldPos),
                uiCamera,
                out Vector2 localPoint);

            cookedRect.localPosition = localPoint;
        }

        if (cookedSize != Vector2.zero)
            cookedRect.sizeDelta = cookedSize;

        // Activate AFTER reparenting and resizing — OnEnable (pendingPulse →
        // SaveOrigin) now snapshots the correct slot-anchor parent and final size.
        cookedObject.SetActive(true);

        CanvasGroup cg = cookedObject.GetComponent<CanvasGroup>();
        if (cg != null)
        {
            cg.blocksRaycasts = true;
            cg.interactable = true;
            cg.alpha = 1f;
        }

        if (burner != null) burner.enabled = true;

        Debug.Log($"[GrillCooker] '{cookedDrag?.grillType}' is cooked! " +
                  $"Burn grace timer started. Drag to a plate.");

        SoundManager.Instance.PlaySFX("Cooked");
        stove?.FreeSlot(slotIndex);
        PigGrillTutorialManager.Instance?.NotifyCookingDone();
        Destroy(gameObject);
    }

    private void StartAnim()
    {
        StopAnim();
        if (cookingAnimation?.frames == null || cookingAnimation.frames.Length == 0) return;
        _animRoutine = StartCoroutine(AnimRoutine());
    }

    private void StopAnim()
    {
        if (_animRoutine == null) return;
        StopCoroutine(_animRoutine);
        _animRoutine = null;
    }

    private IEnumerator AnimRoutine()
    {
        int frameCount = cookingAnimation.frames.Length;
        float delay = cookDuration / frameCount;
        int i = 0;

        while (true)
        {
            if (_image != null)
                _image.sprite = cookingAnimation.frames[i];

            yield return new WaitForSeconds(delay);

            i++;
            if (i >= frameCount)
                yield break;
        }
    }
}