using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using TMPro;

public class PigPenZone : MonoBehaviour
{
    [Header("Identity")]
    public int acceptToolID = 0;
    public int taskID = 0;

    // ── INTRO SEQUENCE ────────────────────────────────────────────────────────
    [Header("Intro Sequence (runs once before game starts)")]
    [Tooltip("The 'Please clean my Pen' text GameObject.")]
    public GameObject introTextObject;
    [Tooltip("The TMP_Text component on the intro text object.")]
    public TMP_Text introTMPText;
    [Tooltip("Seconds between each letter appearing / disappearing.")]
    public float introLetterDelay = 0.05f;
    [Tooltip("The Okay button. No animation — appears instantly after text is fully shown.")]
    public GameObject introOkayButton;
    [Tooltip("Seconds to wait after text finishes before the Okay button appears.")]
    public float okayButtonDelay = 1f;

    [Header("Hint Highlight Color")]
    [Tooltip("Color used to highlight a zone when the correct tool hovers over it. Default is yellow.")]
    public Color hintColor = new Color(1f, 1f, 0.5f, 1f);

    [Header("Zone Animation (plays ONCE)")]
    public Sprite[] zoneFrames;
    [Range(1f, 30f)]
    public float zoneFPS = 6f;

    [Header("Tool Animation Path")]
    [Tooltip("Empty child RectTransforms placed in the Scene. The tool travels through them while cleaning this zone.")]
    public RectTransform[] toolAnimPath;

    [Header("Per-Zone Star Animation")]
    [Tooltip("The star GameObject positioned at this zone (needs an Image component).")]
    public GameObject starObject;
    public Sprite[] starFrames;
    [Range(1f, 30f)]
    public float starFPS = 12f;
    [Tooltip("How many times the star animation plays after THIS zone is cleaned.")]
    [Min(1)]
    public int starPlayCount = 2;

    [Header("Final Star Animation (plays after ALL zones are cleaned)")]
    [Tooltip("Star object for the all-clean celebration. Can be the same as the per-zone star or a different one.")]
    public GameObject finalStarObject;
    public Sprite[] finalStarFrames;
    [Range(1f, 30f)]
    public float finalStarFPS = 12f;
    [Tooltip("How many times the final star animation plays before the All Clean panel appears.")]
    [Min(1)]
    public int finalStarPlayCount = 1;

    [Header("Pig Reaction (shared across all zones — drag the same object into every zone)")]
    [Tooltip("The pig GameObject (needs an Image component).")]
    public GameObject pigObject;
    public Sprite[] pigFrames;
    [Range(1f, 30f)]
    public float pigFPS = 12f;
    [Tooltip("How many seconds the pig and chat box stay visible (the sprite sheet loops continuously for this duration).")]
    [Min(0.1f)]
    public float pigDisplayDuration = 3f;
    [Tooltip("The chat box GameObject showing 'Nice Job! Go ahead'.")]
    public GameObject pigChatBox;
    [Tooltip("TMP_Text inside the pig chat box. Will be set to 'Nice Job!' on each zone clean.")]
    public TMP_Text pigChatTMPText;
    [Tooltip("Text shown in the pig chat box after each zone is cleaned.")]
    public string niceJobText = "Nice Job!";

    [Header("Completion Panel")]
    public GameObject allCleanPanel;

    [Header("Pixel Detection")]
    [Range(0f, 1f)]
    public float alphaThreshold = 0.1f;

    [Header("SFX Keys")]
    [Tooltip("Played when the pig slides in or out of frame (intro, reaction, hide).")]
    public string sfxPigSlide = "PigSlide";
    [Tooltip("Played once when the per-zone star sparkle animation starts.")]
    public string sfxStarSparkle = "StarSparkle";
    [Tooltip("Played once when the final (all-zones-done) star sparkle animation starts.")]
    public string sfxFinalSparkle = "FinalSparkle";
    [Tooltip("Looped while typewriter text is being revealed; stopped when done.")]
    public string sfxTyping = "Typing";

    // ── Static registry ───────────────────────────────────────────────────────
    private static readonly List<PigPenZone> allZones = new List<PigPenZone>();
    public static IReadOnlyList<PigPenZone> AllZones => allZones;
    private static int cleanedCount = 0;

    /// <summary>
    /// True while the intro sequence is running — PigPenTool checks this
    /// to block dragging until the player presses Okay.
    /// </summary>
    public static bool GameLocked { get; private set; } = true;

    // ── Private state ─────────────────────────────────────────────────────────
    private RectTransform rt;
    private Image img;
    private Sprite defaultSprite;

    private Vector2 pigHomePos;
    private Vector2 chatHomePos;
    private Vector2 textHomePos;

    private bool isCleaning;
    private bool isClean;

    // Only one zone triggers the intro (the first one to call Start)
    private static bool introPlayed = false;

    public bool IsCleaning => isCleaning;
    public bool IsClean => isClean;
    public int AcceptToolID => acceptToolID;
    public int TaskID => taskID;

    // ── Lifecycle ─────────────────────────────────────────────────────────────

    void Awake()
    {
        rt = GetComponent<RectTransform>();
        img = GetComponent<Image>();
        defaultSprite = img != null ? img.sprite : null;

        if (starObject != null) starObject.SetActive(false);
        if (finalStarObject != null) finalStarObject.SetActive(false);

        // Capture resting positions BEFORE deactivating.
        // If this zone owns the pig it is the intro-owner — reset the static
        // gate here (Awake) so re-entering the scene always plays the intro,
        // regardless of how the previous scene was unloaded.
        if (pigObject != null)
        {
            introPlayed = false;
            GameLocked = true;
            niceJobShown = false;
            cleanedCount = 0;
            RectTransform pigRT = pigObject.GetComponent<RectTransform>();
            if (pigRT != null) pigHomePos = pigRT.anchoredPosition;
            pigObject.SetActive(false);
        }
        if (pigChatBox != null)
        {
            RectTransform chatRT = pigChatBox.GetComponent<RectTransform>();
            if (chatRT != null) chatHomePos = chatRT.anchoredPosition;
            pigChatBox.SetActive(false);
        }

        // Hide intro Okay button and intro text until the intro starts
        // Capture text home position before any tween can move it
        if (introTextObject != null)
        {
            RectTransform textRT = introTextObject.GetComponent<RectTransform>();
            if (textRT != null) textHomePos = textRT.anchoredPosition;
        }

        if (introOkayButton != null) introOkayButton.SetActive(false);
        if (introTextObject != null) introTextObject.SetActive(false);
        if (introTMPText != null) introTMPText.maxVisibleCharacters = 0;
    }

    private static bool niceJobShown = false;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ClearStatics()
    {
        allZones.Clear();
        cleanedCount = 0;
        GameLocked = true;
        introPlayed = false;
        niceJobShown = false;
    }


    void OnEnable() { if (!allZones.Contains(this)) allZones.Add(this); }
    void OnDisable() { allZones.Remove(this); }
    void OnDestroy() { allZones.Remove(this); }

    void Start()
    {
        // Only the first zone with a pig assigned triggers the intro
        if (!introPlayed && pigObject != null)
        {
            introPlayed = true;
            GameLocked = true;
            StartCoroutine(PlayIntroSequence());
        }
        else if (!introPlayed)
        {
            // No pig assigned on any zone — unlock immediately
            introPlayed = true;
            GameLocked = false;
        }
    }

    // ── Hit testing ───────────────────────────────────────────────────────────

    /// <summary>
    /// Returns true only when the screen point lands on a non-transparent pixel
    /// of this zone's sprite. pixelAlpha is set to the sampled alpha (0 on miss)
    /// so callers can compare overlapping candidates and pick the best hit.
    /// Never silently falls back to true — unreadable textures log an error and
    /// return false so a bad import setting cannot accidentally accept a drop.
    /// </summary>
    public bool PixelHit(Vector2 screenPos, Camera uiCamera, out float pixelAlpha)
    {
        pixelAlpha = 0f;

        if (!RectTransformUtility.RectangleContainsScreenPoint(rt, screenPos, uiCamera))
            return false;

        if (img == null || img.sprite == null)
        {
            pixelAlpha = 1f;
            return true;
        }

        Sprite sprite = img.sprite;
        Texture2D tex = sprite.texture;

        if (!tex.isReadable)
        {
            Debug.LogError($"[PigPenZone] '{gameObject.name}': texture '{tex.name}' is NOT " +
                           "readable. Enable Read/Write in its Texture Import Settings.", this);
            return false;
        }

        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            rt, screenPos, uiCamera, out Vector2 local);

        Rect r = rt.rect;
        float nx = (local.x - r.x) / r.width;
        float ny = (local.y - r.y) / r.height;

        int px = Mathf.Clamp((int)(sprite.rect.x + nx * sprite.rect.width),
                             (int)sprite.rect.x, (int)sprite.rect.xMax - 1);
        int py = Mathf.Clamp((int)(sprite.rect.y + ny * sprite.rect.height),
                             (int)sprite.rect.y, (int)sprite.rect.yMax - 1);

        pixelAlpha = tex.GetPixel(px, py).a;
        return pixelAlpha >= alphaThreshold;
    }

    /// <summary>Legacy wrapper so any external code still compiles.</summary>
    public bool ContainsScreenPoint(Vector2 screenPos, Camera uiCamera)
        => PixelHit(screenPos, uiCamera, out _);

    // ── Highlight ─────────────────────────────────────────────────────────────

    public void SetHighlight(bool on)
    {
        if (img != null)
            img.color = on ? hintColor : Color.white;
    }

    // ── Cleaning ──────────────────────────────────────────────────────────────

    //public bool TryClean(PigPenTool tool)
    //{
    //    if (GameLocked) return false;
    //    if (isCleaning || isClean) return false;
    //    if (tool.GetToolID() != acceptToolID) return false;
    //    if (!tool.HasTaskID(taskID)) return false;

    //    isCleaning = true;
    //    StartCoroutine(CleanSequence(tool));
    //    return true;
    //}

    public bool TryClean(PigPenTool tool)
    {
        if (GameLocked) return false;
        if (isCleaning || isClean) return false;
        if (tool.GetToolID() != acceptToolID) return false;
        if (!tool.HasTaskID(taskID)) return false;

        isCleaning = true;
        StartCoroutine(CleanSequence(tool));
        return true;
    }

    //IEnumerator CleanSequence(PigPenTool tool)
    //{
    //    // 1. Tool animation + zone sprite play together
    //    tool.StartToolAnimation(toolAnimPath);
    //    yield return StartCoroutine(PlayZoneOnce());

    //    // 2. Tool goes home
    //    tool.ReturnHome();

    //    // 3. Star and pig reaction run independently — neither waits for the other.
    //    //    We start both, then wait for each separately so they truly overlap
    //    //    and the zone only proceeds once BOTH are fully done.
    //    bool starDone = false;
    //    bool pigDone = false;

    //    StartCoroutine(PlayStarNTimes(starObject, starFrames, starFPS, starPlayCount,
    //                                  () => starDone = true, isFinal: false));
    //    StartCoroutine(PlayPigReaction(() => pigDone = true));

    //    yield return new WaitUntil(() => starDone && pigDone);

    //    // 4. Mark zone done
    //    isCleaning = false;
    //    isClean = true;
    //    cleanedCount++;

    //    allZones.RemoveAll(z => z == null);

    //    // 5. All zones clean → final star → all-clean panel → congrats panel
    //    //    Check every zone is actually marked clean, not just a count match,
    //    //    so a mid-restart state can never trigger this prematurely.
    //    bool allDone = allZones.Count > 0;
    //    foreach (PigPenZone z in allZones)
    //        if (!z.isClean) { allDone = false; break; }

    //    if (allDone)
    //    {
    //        bool finalStarDone = false;
    //        StartCoroutine(PlayStarNTimes(finalStarObject, finalStarFrames, finalStarFPS,
    //                                      finalStarPlayCount, () => finalStarDone = true, isFinal: true));
    //        yield return new WaitUntil(() => finalStarDone);

    //        if (allCleanPanel != null)
    //            ZoomInPanel(allCleanPanel);

    //        // Show the congratulations panel.
    //        if (CleaningGameManager.Instance != null)
    //            CleaningGameManager.Instance.ShowCongrats();
    //    }
    //}

    IEnumerator CleanSequence(PigPenTool tool)
    {
        // 1. Tool animation + zone sprite play together.
        //    Pass this zone's transform so the tool is reparented into THIS
        //    zone GameObject (e.g. hammer -> Frame, brush -> Mud2) instead of
        //    staying parented to the root canvas while it animates.
        tool.StartToolAnimation(toolAnimPath, transform);
        yield return StartCoroutine(PlayZoneOnce());

        // 2. Tool goes home
        tool.ReturnHome();

        // 3. Star and pig reaction run independently — neither waits for the other.
        bool starDone = false;
        bool pigDone = false;

        StartCoroutine(PlayStarNTimes(starObject, starFrames, starFPS, starPlayCount,
                                      () => starDone = true, isFinal: false));
        StartCoroutine(PlayPigReaction(() => pigDone = true));

        yield return new WaitUntil(() => starDone && pigDone);

        // 4. Mark zone done
        isCleaning = false;
        isClean = true;
        cleanedCount++;

        allZones.RemoveAll(z => z == null);

        bool allDone = allZones.Count > 0;
        foreach (PigPenZone z in allZones)
            if (!z.isClean) { allDone = false; break; }

        if (allDone)
        {
            bool finalStarDone = false;
            StartCoroutine(PlayStarNTimes(finalStarObject, finalStarFrames, finalStarFPS,
                                          finalStarPlayCount, () => finalStarDone = true, isFinal: true));
            yield return new WaitUntil(() => finalStarDone);

            if (allCleanPanel != null)
                ZoomInPanel(allCleanPanel);

            if (CleaningGameManager.Instance != null)
                CleaningGameManager.Instance.ShowCongrats();
        }
    }

    /// <summary>
    /// Shows a panel with a single punch zoom-in (scale 0 → 1 with OutBack ease).
    /// No fade — just a clean zoom.
    /// </summary>
    static void ZoomInPanel(GameObject panel)
    {
        panel.transform.localScale = Vector3.zero;
        panel.SetActive(true);
        panel.transform.DOScale(1f, 0.45f).SetEase(Ease.OutBack);
    }

    // ── Zone sprite animation ─────────────────────────────────────────────────

    IEnumerator PlayZoneOnce()
    {
        if (zoneFrames == null || zoneFrames.Length == 0)
        {
            yield return new WaitForSeconds(2f);
            yield break;
        }

        float delay = 1f / Mathf.Max(zoneFPS, 1f);
        foreach (Sprite frame in zoneFrames)
        {
            if (img == null) yield break;
            if (frame != null) img.sprite = frame;
            yield return new WaitForSeconds(delay);
        }
    }

    // ── Star animation ────────────────────────────────────────────────────────

    /// <summary>
    /// Shows the star, plays its sprite sheet <paramref name="times"/> times,
    /// hides it, then invokes <paramref name="onDone"/> so the caller knows
    /// this independent coroutine has finished.
    /// </summary>
    IEnumerator PlayStarNTimes(GameObject starGO, Sprite[] frames, float fps,
                               int times, System.Action onDone = null, bool isFinal = false)
    {
        if (starGO == null || frames == null || frames.Length == 0 || times <= 0)
        {
            onDone?.Invoke();
            yield break;
        }

        Image starImg = starGO.GetComponent<Image>();
        if (starImg == null)
        {
            Debug.LogWarning($"[PigPenZone] '{gameObject.name}': star object " +
                             $"'{starGO.name}' has no Image component.", this);
            onDone?.Invoke();
            yield break;
        }

        float delay = 1f / Mathf.Max(fps, 1f);
        starGO.SetActive(true);

        // SFX: per-zone sparkle or final sparkle
        SoundManager.Instance?.PlaySFX(isFinal ? sfxFinalSparkle : sfxStarSparkle);

        for (int i = 0; i < times; i++)
            foreach (Sprite frame in frames)
            {
                if (frame != null) starImg.sprite = frame;
                yield return new WaitForSeconds(delay);
            }

        starGO.SetActive(false);
        onDone?.Invoke();
    }

    // ── Pig reaction ──────────────────────────────────────────────────────────

    /// <summary>
    /// Shows the pig + chat box, loops the sprite sheet continuously for
    /// <see cref="pigDisplayDuration"/> seconds, hides both, then invokes <paramref name="onDone"/>.
    /// Runs independently of the star — neither waits for the other.
    /// </summary>
    IEnumerator PlayPigReaction(System.Action onDone = null)
    {
        if (pigObject == null)
        {
            onDone?.Invoke();
            yield break;
        }

        Image pigImg = pigObject.GetComponent<Image>();
        if (pigImg == null)
        {
            Debug.LogWarning($"[PigPenZone] '{gameObject.name}': pig object " +
                             $"'{pigObject.name}' has no Image component.", this);
            onDone?.Invoke();
            yield break;
        }

        // ── Slide in from the left ────────────────────────────────────────────
        RectTransform pigRT = pigObject.GetComponent<RectTransform>();
        RectTransform chatRT = pigChatBox != null ? pigChatBox.GetComponent<RectTransform>() : null;

        // Use canvas width so the offset is in the same space as anchoredPosition
        Canvas canvas = pigObject.GetComponentInParent<Canvas>();
        if (canvas != null && !canvas.isRootCanvas) canvas = canvas.rootCanvas;
        float canvasWidth = canvas != null
            ? canvas.GetComponent<RectTransform>().rect.width
            : 1080f;
        float offscreen = canvasWidth + 200f;

        // Always start from the fixed home position captured in Awake
        if (pigRT != null) pigRT.anchoredPosition = new Vector2(pigHomePos.x - offscreen, pigHomePos.y);
        if (chatRT != null) chatRT.anchoredPosition = new Vector2(chatHomePos.x - offscreen, chatHomePos.y);

        pigObject.SetActive(true);
        if (pigChatTMPText != null) pigChatTMPText.text = niceJobText;
        if (pigChatBox != null) pigChatBox.SetActive(true);

        // SFX: pig + chat box slide in
        SoundManager.Instance?.PlaySFX(sfxPigSlide);

        if (pigRT != null) pigRT.DOAnchorPosX(pigHomePos.x, 0.45f).SetEase(Ease.OutCubic);
        if (chatRT != null) chatRT.DOAnchorPosX(chatHomePos.x, 0.45f).SetEase(Ease.OutCubic);

        yield return new WaitForSeconds(0.5f);

        // ── Loop pig animation for pigDisplayDuration ─────────────────────────
        if (pigFrames != null && pigFrames.Length > 0)
        {
            float delay = 1f / Mathf.Max(pigFPS, 1f);
            float elapsed = 0f;
            int frame = 0;
            while (elapsed < pigDisplayDuration)
            {
                if (pigFrames[frame % pigFrames.Length] != null)
                    pigImg.sprite = pigFrames[frame % pigFrames.Length];
                frame++;
                yield return new WaitForSeconds(delay);
                elapsed += delay;
            }
        }
        else
        {
            yield return new WaitForSeconds(pigDisplayDuration);
        }

        // ── Slide out to the left ─────────────────────────────────────────────
        SoundManager.Instance?.PlaySFX(sfxPigSlide);
        if (pigRT != null) pigRT.DOAnchorPosX(pigHomePos.x - offscreen, 0.45f)
                                 .SetEase(Ease.InCubic)
                                 .OnComplete(() => { if (pigObject != null) pigObject.SetActive(false); });
        if (chatRT != null) chatRT.DOAnchorPosX(chatHomePos.x - offscreen, 0.45f)
                                  .SetEase(Ease.InCubic)
                                  .OnComplete(() => { if (pigChatBox != null) pigChatBox.SetActive(false); });

        yield return new WaitForSeconds(0.5f);

        onDone?.Invoke();
    }

    // ── Intro Sequence ────────────────────────────────────────────────────────

    /// <summary>
    /// Called by GameManager.ResetCleaningGame() to restart the intro after a Play Again.
    /// </summary>
    public void StartIntro()
    {
        GameLocked = true;
        StartCoroutine(PlayIntroSequence());
    }

    IEnumerator PlayIntroSequence()
    {
        if (pigObject == null) { UnlockGame(); yield break; }

        // Hide everything at start
        if (introOkayButton != null) introOkayButton.SetActive(false);
        if (introTextObject != null) introTextObject.SetActive(false);
        pigObject.SetActive(false);

        // 1. Pig slides in from the left
        RectTransform pigRT = pigObject.GetComponent<RectTransform>();
        Vector2 pigOriginalPos = pigRT != null ? pigRT.anchoredPosition : Vector2.zero;
        float offscreenLeft = -Screen.width - 200f;

        if (pigRT != null)
        {
            pigRT.anchoredPosition = new Vector2(offscreenLeft, pigOriginalPos.y);
            pigObject.SetActive(true);
            // SFX: pig slides in from left
            SoundManager.Instance?.PlaySFX(sfxPigSlide);
            pigRT.DOAnchorPosX(pigOriginalPos.x, 0.45f).SetEase(Ease.OutCubic);
        }
        else
        {
            pigObject.SetActive(true);
        }

        yield return new WaitForSeconds(0.5f);

        // 2. Start pig animation immediately (runs in parallel with text + button steps)
        Image pigImg = pigObject.GetComponent<Image>();
        if (pigImg != null && pigFrames != null && pigFrames.Length > 0)
            StartCoroutine(LoopPigAnimation(pigImg));

        // 3. Text object slides in from the left then reveals letter by letter
        RectTransform textRT = introTextObject != null
            ? introTextObject.GetComponent<RectTransform>() : null;

        if (textRT != null && introTMPText != null)
        {
            // Start fully offscreen left, no letters visible
            textRT.anchoredPosition = new Vector2(offscreenLeft, textHomePos.y);
            introTMPText.maxVisibleCharacters = 0;
            introTextObject.SetActive(true);

            // Slide the text panel in to its captured home position
            textRT.DOAnchorPosX(textHomePos.x, 0.35f).SetEase(Ease.OutCubic);
            yield return new WaitForSeconds(0.35f);

            // Reveal letters one by one left → right
            SoundManager.Instance?.PlaySFXLoop(sfxTyping);
            int totalChars = introTMPText.text.Length;
            for (int i = 0; i <= totalChars; i++)
            {
                introTMPText.maxVisibleCharacters = i;
                yield return new WaitForSeconds(introLetterDelay);
            }
            SoundManager.Instance?.StopSFXLoop();
        }
        else if (introTextObject != null)
        {
            introTextObject.SetActive(true);
        }

        // 3. Okay button appears after a delay
        if (introOkayButton != null)
        {
            yield return new WaitForSeconds(okayButtonDelay);
            introOkayButton.SetActive(true);
        }

        // 4. Wait until Okay is clicked
        yield return new WaitUntil(() => !GameLocked);
    }

    IEnumerator LoopPigAnimation(Image pigImg)
    {
        float delay = 1f / Mathf.Max(pigFPS, 1f);
        int frame = 0;
        while (GameLocked)
        {
            if (pigFrames[frame % pigFrames.Length] != null)
                pigImg.sprite = pigFrames[frame % pigFrames.Length];
            frame++;
            yield return new WaitForSeconds(delay);
        }
    }

    /// <summary>
    /// Wire this to the Okay button's OnClick in the Inspector.
    /// </summary>
    public void OnOkayButtonClicked()
    {
        Debug.Log("[PigPenZone] Okay button clicked.");
        StartCoroutine(HideIntroRoutine());
    }

    IEnumerator HideIntroRoutine()
    {
        // 1. Okay button disappears instantly
        if (introOkayButton != null) introOkayButton.SetActive(false);

        // 2. Slide pig AND text together to the LEFT (text stays fully visible)
        float offscreenLeft = -Screen.width - 200f;

        RectTransform pigRT = pigObject != null ? pigObject.GetComponent<RectTransform>() : null;
        RectTransform textRT = introTextObject != null ? introTextObject.GetComponent<RectTransform>() : null;

        if (pigRT != null)
            pigRT.DOAnchorPosX(pigRT.anchoredPosition.x + offscreenLeft, 0.45f)
                .SetEase(Ease.InCubic)
                .OnComplete(() => { if (pigObject != null) pigObject.SetActive(false); });

        if (textRT != null)
            textRT.DOAnchorPosX(textRT.anchoredPosition.x + offscreenLeft, 0.45f)
                .SetEase(Ease.InCubic)
                .OnComplete(() => { if (introTextObject != null) introTextObject.SetActive(false); });

        // SFX: pig + text slide out after Okay
        SoundManager.Instance?.PlaySFX(sfxPigSlide);

        yield return new WaitForSeconds(0.5f);

        // 3. Unlock the game
        UnlockGame();
    }

    static void UnlockGame() => GameLocked = false;

    // ── Reset ─────────────────────────────────────────────────────────────────

    public void ResetZone()
    {
        StopAllCoroutines();
        isCleaning = false;
        isClean = false;
        cleanedCount = 0;

        // Reset intro and nice-job flag so they play again on restart.
        introPlayed = false;
        GameLocked = true;
        niceJobShown = false;

        if (img != null)
        {
            img.sprite = defaultSprite;
            img.color = Color.white;
        }

        if (starObject != null) starObject.SetActive(false);
        if (finalStarObject != null) finalStarObject.SetActive(false);

        // Kill any in-progress DOTween slide on pig / chat box, then snap back
        // to their home positions so the intro slide-in starts from the right placedisaghdiasgo
        if (pigObject != null)
        {
            pigObject.transform.DOKill();
            RectTransform pigRT = pigObject.GetComponent<RectTransform>();
            if (pigRT != null) pigRT.anchoredPosition = pigHomePos;
            pigObject.SetActive(false);
        }
        if (pigChatBox != null)
        {
            pigChatBox.transform.DOKill();
            RectTransform chatRT = pigChatBox.GetComponent<RectTransform>();
            if (chatRT != null) chatRT.anchoredPosition = chatHomePos;
            pigChatBox.SetActive(false);
        }

        if (allCleanPanel != null) { allCleanPanel.transform.DOKill(); allCleanPanel.SetActive(false); }

        if (introOkayButton != null) introOkayButton.SetActive(false);
        if (introTextObject != null)
        {
            introTextObject.transform.DOKill();
            RectTransform textRT2 = introTextObject.GetComponent<RectTransform>();
            if (textRT2 != null) textRT2.anchoredPosition = textHomePos;
            introTextObject.SetActive(false);
        }
        if (introTMPText != null) introTMPText.maxVisibleCharacters = 0;
    }
}