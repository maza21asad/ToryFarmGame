using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class Cowanimationcontroller : MonoBehaviour
{
    public enum CowState { Idle, Run, Jump, Hit, Win }

    [Header("Auto Run")]
    public float runSpeed = 200f;

    [Header("Jump Physics")]
    public float jumpHeight = 250f;  // pixels — how high
    public float jumpDuration = 0.7f;  // seconds — time in air
    public float jumpForwardSpeed = 400f; // pixels/sec forward during jump
    public int maxJumps = 2;     // 1 = single jump, 2 = double jump

    [Header("Jump Size")]
    [Tooltip("1 = same size as run/idle.  1.3 = 30% bigger.  Drag to match your jump sprites.")]
    [Range(0.5f, 3f)]
    public float jumpScale = 1f;

    [Header("Win Size")]
    [Tooltip("1 = same size as run/idle.  1.3 = 30% bigger.  Drag to match your win sprites.")]
    [Range(0.5f, 3f)]
    public float winScale = 1f;

    [Header("Idle Animation")]
    public Sprite[] idleFrames;
    public float idleFPS = 12f;

    [Header("Run Animation")]
    public Sprite[] runFrames;
    public float runFPS = 12f;

    [Header("Jump Animation")]
    public Sprite[] jumpFrames;
    public float jumpFPS = 12f;

    [Header("Hit Animation")]
    public Sprite[] hitFrames;
    public float hitFPS = 12f;

    [Header("Hit Bounce")]
    [Tooltip("How high the cow bounces up when hit plays. 0 = no bounce, 150 = noticeable bump.")]
    public float hitBounceHeight = 80f;
    [Tooltip("How long the bounce takes in seconds.")]
    public float hitBounceDuration = 0.35f;

    [Header("Win / Celebration Animation")]
    public Sprite[] winFrames;
    public float winFPS = 12f;

    // ── Private ────────────────────────────────────────────────
    private Image cowImage;
    private RectTransform cowRect;
    private Vector2 originalSize;
    private bool isAlive = false;
    private bool hitThenWin = false;
    private float groundY;
    private bool isJumping = false;
    private int jumpsLeft = 0;

    private Coroutine animLoop = null;
    private Coroutine jumpArcCo = null;

    public CowState CurrentState { get; private set; } = CowState.Idle;

    [HideInInspector] public float worldX = 0f;
    [HideInInspector] public bool isRunning = false;

    // ──────────────────────────────────────────────────────────
    void Start()
    {
        isAlive = true;
        cowImage = GetComponent<Image>();
        cowRect = GetComponent<RectTransform>();
        originalSize = cowRect.sizeDelta;
        worldX = cowRect.anchoredPosition.x;
        groundY = cowRect.anchoredPosition.y;
        SetState(CowState.Idle);
    }

    void OnDestroy() { isAlive = false; StopAllCoroutines(); }
    void OnDisable() { isAlive = false; StopAllCoroutines(); }
    void OnEnable() { if (cowImage != null && cowRect != null) isAlive = true; }

    // ──────────────────────────────────────────────────────────
    void Update()
    {
        if (!isAlive) return;
        if (CurrentState == CowState.Win) return;
        if (CurrentState == CowState.Hit) return;

        if (isRunning)
        {
            if (!isJumping)
                worldX += runSpeed * Time.deltaTime;

            if (Input.GetMouseButtonDown(0) || (Input.touchCount > 0 && Input.GetTouch(0).phase == TouchPhase.Began))
                PlayJump();
        }
    }

    // ──────────────────────────────────────────────────────────
    public void StartRunning()
    {
        isRunning = true;
        jumpsLeft = maxJumps;
        SetState(CowState.Run);
        Debug.Log("[Cow] StartRunning!");
    }

    public void StopRunning()
    {
        isRunning = false;
    }

    // ──────────────────────────────────────────────────────────
    public void PlayJump()
    {
        if (!isAlive) return;
        if (CurrentState == CowState.Win) return;
        if (CurrentState == CowState.Hit) return;
        if (jumpsLeft <= 0) return;

        jumpsLeft--;
        Debug.Log($"[Cow] Jump! Jumps left: {jumpsLeft}");

        if (RunnerGameManager.Instance != null)
            RunnerGameManager.Instance.OnJump();

        if (jumpArcCo != null) StopCoroutine(jumpArcCo);
        jumpArcCo = StartCoroutine(JumpArc());

        SetState(CowState.Jump);
    }

    public void OnJumpPressed() => PlayJump();
    public void OnJumpDown() => PlayJump();

    // ──────────────────────────────────────────────────────────
    // Called by ObstacleController when cow hits an obstacle
    // ──────────────────────────────────────────────────────────
    public void PlayHit()
    {
        if (!isAlive) return;
        if (CurrentState == CowState.Hit) return;
        if (CurrentState == CowState.Win) return;

        // Stop any jump in progress
        if (jumpArcCo != null)
        {
            StopCoroutine(jumpArcCo);
            jumpArcCo = null;
        }
        isJumping = false;
        isRunning = false;
        hitThenWin = false;

        // ✅ Snap to ground first, then play controllable bounce arc
        if (cowRect != null)
            cowRect.anchoredPosition = new Vector2(cowRect.anchoredPosition.x, groundY);

        SetState(CowState.Hit);
        StartCoroutine(HitBounceArc());
    }

    // ──────────────────────────────────────────────────────────
    // Hit bounce arc — parabola up then back to groundY
    // Controlled by hitBounceHeight and hitBounceDuration in Inspector
    // ──────────────────────────────────────────────────────────
    IEnumerator HitBounceArc()
    {
        if (hitBounceHeight <= 0f) yield break;   // 0 = no bounce, skip

        float elapsed = 0f;
        float startY = groundY;

        while (elapsed < hitBounceDuration)
        {
            if (!isAlive) yield break;

            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / hitBounceDuration);

            // Parabola: groundY → peak → groundY
            float y = startY + hitBounceHeight * 4f * t * (1f - t);
            cowRect.anchoredPosition = new Vector2(cowRect.anchoredPosition.x, y);

            yield return null;
        }

        // Snap back to ground
        cowRect.anchoredPosition = new Vector2(cowRect.anchoredPosition.x, groundY);
    }

    // Called by BellTrigger — Hit once then Win celebration
    public void PlayHitThenWin()
    {
        if (!isAlive) return;
        if (CurrentState == CowState.Win) return;

        if (jumpArcCo != null) { StopCoroutine(jumpArcCo); jumpArcCo = null; }
        isJumping = false;
        isRunning = false;
        if (cowRect != null)
            cowRect.anchoredPosition = new Vector2(cowRect.anchoredPosition.x, groundY);
        hitThenWin = true;
        SetState(CowState.Hit);
        Debug.Log("[Cow] PlayHitThenWin!");
    }

    // ──────────────────────────────────────────────────────────
    // Called by BellTrigger after waiting for Hit animation to finish.
    // Transitions directly to Win (celebration) animation.
    // ──────────────────────────────────────────────────────────
    public void PlayWinDirect()
    {
        if (!isAlive) return;
        if (CurrentState == CowState.Win) return;

        if (jumpArcCo != null) { StopCoroutine(jumpArcCo); jumpArcCo = null; }
        isJumping = false;
        isRunning = false;
        hitThenWin = false;

        // ✅ Apply scale immediately so it's visible on the very first frame
        if (cowRect != null)
            cowRect.sizeDelta = originalSize * winScale;

        SetState(CowState.Win);
        Debug.Log("[Cow] PlayWinDirect — celebration!");
    }

    // ──────────────────────────────────────────────────────────
    // Jump arc — parabola from current Y back to groundY
    // ──────────────────────────────────────────────────────────
    IEnumerator JumpArc()
    {
        isJumping = true;

        float elapsed = 0f;
        float startY = cowRect.anchoredPosition.y;

        while (elapsed < jumpDuration)
        {
            if (!isAlive) yield break;
            if (CurrentState == CowState.Hit) yield break; // stop arc on hit

            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / jumpDuration);

            worldX += jumpForwardSpeed * Time.deltaTime;

            // Parabola: startY → peak → groundY
            float height = Mathf.Lerp(startY, groundY, t) + jumpHeight * 4f * t * (1f - t);
            cowRect.anchoredPosition = new Vector2(cowRect.anchoredPosition.x, height);

            yield return null;
        }

        // Snap to ground and reset
        cowRect.anchoredPosition = new Vector2(cowRect.anchoredPosition.x, groundY);
        isJumping = false;
        jumpsLeft = maxJumps;
        jumpArcCo = null;

        if (CurrentState == CowState.Jump)
            SetState(isRunning ? CowState.Run : CowState.Idle);

        Debug.Log("[Cow] Landed!");
    }

    // ──────────────────────────────────────────────────────────
    void SetState(CowState newState)
    {
        if (!isAlive) return;
        CurrentState = newState;
        Debug.Log($"[Cow] → {newState}");

        if (animLoop != null) StopCoroutine(animLoop);
        animLoop = StartCoroutine(MasterLoop());
    }

    IEnumerator MasterLoop()
    {
        int frame = 0;
        while (true)
        {
            if (!isAlive) yield break;

            Sprite[] frames = null;
            float fps = 12f;

            switch (CurrentState)
            {
                case CowState.Idle: frames = idleFrames; fps = idleFPS; break;
                case CowState.Run: frames = runFrames; fps = runFPS; break;
                case CowState.Jump: frames = jumpFrames; fps = jumpFPS; break;
                case CowState.Hit: frames = jumpFrames; fps = jumpFPS; break;
                case CowState.Win: frames = winFrames; fps = winFPS; break;
            }

            if (frames == null || frames.Length == 0)
            {
                Debug.LogError($"[Cow] {CurrentState} frames EMPTY!");
                yield return new WaitForSeconds(0.1f);
                continue;
            }

            if (frame >= frames.Length) frame = 0;

            cowImage.sprite = frames[frame];

            // Apply scale based on state
            if (CurrentState == CowState.Jump)
                cowRect.sizeDelta = originalSize * jumpScale;
            else if (CurrentState == CowState.Win)
                cowRect.sizeDelta = originalSize * winScale;
            else
                cowRect.sizeDelta = originalSize;





            frame++;

            // Jump → plays once → handled by JumpArc landing
            if (CurrentState == CowState.Jump && frame >= frames.Length)
            {
                frame = 0;
                // Keep looping jump frames until JumpArc finishes
            }

            // Hit → plays once → Win or Idle
            if (CurrentState == CowState.Hit && frame >= frames.Length)
            {
                frame = 0;
                if (hitThenWin)
                {
                    // PlayHitThenWin path — auto transition to Win
                    hitThenWin = false;
                    CurrentState = CowState.Win;
                    Debug.Log("[Cow] → Celebration!");
                }
                // ✅ FIX: Do NOT override CurrentState here if PlayWinDirect()
                // already changed it externally (e.g. BellTrigger celebration sequence).
                // Only fall to Idle if state is still Hit (nobody changed it).
                else if (CurrentState == CowState.Hit)
                {
                    // Game over path — no external win triggered, go to Idle
                    CurrentState = CowState.Idle;
                }
            }

            if (frame >= frames.Length) frame = 0;

            yield return new WaitForSeconds(1f / fps);
        }
    }
}