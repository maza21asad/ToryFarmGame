using UnityEngine;

public class AICustomer : MonoBehaviour
{
    [Header("Sprite Renderer")]
    public SpriteRenderer aiSprite;

    [Header("Thought Bubble 1 — order bubble (child of this AI)")]
    public GameObject thoughtBubble;

    [Header("Thought Bubble 2 — serving bubble (child of this AI)")]
    public GameObject servingThoughtBubble;

    [Header("Enter Points (in order)")]
    public Transform[] enterPoints;

    [Header("Right Side Locations")]
    public Transform[] rightLocations;

    [Header("Left Side Locations")]
    public Transform[] leftLocations;

    [Header("Last Stop (AI destroyed here when leaving)")]
    public Transform lastStop;

    [Header("Move Speed — spawn to enter points")]
    public float enterMoveSpeed = 4f;

    [Header("Move Speed — enter points to seat location")]
    public float locationMoveSpeed = 2f;

    [Header("Move Speed — leaving")]
    public float leaveMoveSpeed = 4f;

    [Header("Post-Serve Hold Duration (seconds) — AI waits before leaving")]
    public float postServeHoldDuration = 2f;

    [Header("Down Left Walk Animation")]
    public TorySpriteAnimation downLeftAnim;

    [Header("Up Left Walk Animation")]
    public TorySpriteAnimation upLeftAnim;

    [Header("Up Right Walk Animation")]
    public TorySpriteAnimation upRightAnim;

    [Header("Down Right Walk Animation")]
    public TorySpriteAnimation downRightAnim;

    [Header("Idle Animation")]
    public TorySpriteAnimation idleAnim;

    [Header("Eating Animation — plays while seated after food is delivered")]
    public TorySpriteAnimation eatAnim;

    // ── Public accessors for ToryController ───────────────────
    public bool IsRightSide => isRightSide;
    public int OccupiedLocationIndex => occupiedLocationIndex;

    // ── Public accessors for RestaurantTutorialManager ─────────
    // True while this AI has a clickable, unhandled thought bubble open
    public bool HasOpenBubble => state == AIState.Idle || state == AIState.ServingIdle;

    // Returns the transform of whichever bubble is currently open (TB1 or TB2), or null
    public Transform GetOpenBubbleTransform()
    {
        if (state == AIState.Idle && thoughtBubble != null) return thoughtBubble.transform;
        if (state == AIState.ServingIdle && servingThoughtBubble != null) return servingThoughtBubble.transform;
        return null;
    }

    // ── private state ──────────────────────────────────────────
    private enum AIState
    {
        MoveToEnter,
        MoveToLocation,
        Idle,
        WaitingForCook,
        ServingIdle,
        PostServeHold,
        LeavingToEnterLast,
        LeavingThroughEnters,
        LeavingToLastStop,
        Done
    }

    private AIState state = AIState.MoveToEnter;
    private Vector3 targetPosition;
    private int currentEnterIndex = 0;

    private TorySpriteAnimation currentAnim;
    private int currentFrame;
    private float frameTimer;

    private bool isRightSide = false;
    private int occupiedLocationIndex = -1;
    private Vector3 spawnPosition;

    private int exitEnterIndex = 0;
    private float postServeHoldTimer = 0f;

    private ToryController tory;
    private AISpawner spawner;
    private Collider2D thoughtBubbleCollider;
    private Collider2D servingBubbleCollider;
    private Collider2D aiBodyCollider;

    private bool isDestroying = false;

    // ── Safe tory accessor ─────────────────────────────────────
    bool ToryAlive => tory != null && tory.gameObject != null;

    // ── Init (called by Spawner) ───────────────────────────────
    public void Init(Transform[] enters, Transform[] rightLocs, Transform[] leftLocs, Vector3 spawnPos, AISpawner aiSpawner)
    {
        enterPoints = enters;
        rightLocations = rightLocs;
        leftLocations = leftLocs;
        spawnPosition = spawnPos;
        spawner = aiSpawner;

        currentEnterIndex = 0;
        targetPosition = enterPoints[0].position;
        state = AIState.MoveToEnter;

        tory = FindObjectOfType<ToryController>();

        // Auto-find thought bubble if not assigned
        if (thoughtBubble == null)
        {
            foreach (Transform child in GetComponentsInChildren<Transform>(true))
            {
                if (child.name.ToLower().Contains("thought") ||
                    child.name.ToLower().Contains("bubble"))
                {
                    thoughtBubble = child.gameObject;
                    break;
                }
            }
        }

        if (thoughtBubble != null)
        {
            thoughtBubbleCollider = thoughtBubble.GetComponent<Collider2D>();
            if (thoughtBubbleCollider == null)
            {
                thoughtBubbleCollider = thoughtBubble.AddComponent<CircleCollider2D>();
                thoughtBubbleCollider.isTrigger = true;
            }
            thoughtBubble.SetActive(false);
        }
        else
        {
            Debug.LogWarning("TB1 not found on: " + gameObject.name);
        }

        if (servingThoughtBubble != null)
        {
            servingBubbleCollider = servingThoughtBubble.GetComponent<Collider2D>();
            if (servingBubbleCollider == null)
            {
                servingBubbleCollider = servingThoughtBubble.AddComponent<CircleCollider2D>();
                servingBubbleCollider.isTrigger = true;
            }
            servingThoughtBubble.SetActive(false);
        }
        else
        {
            Debug.LogWarning("TB2 (serving bubble) not found on: " + gameObject.name);
        }

        aiBodyCollider = GetComponent<Collider2D>();
        if (aiBodyCollider == null)
        {
            aiBodyCollider = gameObject.AddComponent<CircleCollider2D>();
            aiBodyCollider.isTrigger = true;
        }

        PlayAnim(downLeftAnim);
        SoundManager.Instance.PlaySFX("walking");

        RestaurantTutorialManager.Instance?.RegisterAI(this);
    }

    // ── Unity lifecycle ────────────────────────────────────────
    void Update()
    {
        if (isDestroying) return;

        TickAnimation();
        HandleMovement();
        HandleThoughtBubbleClick();
        HandleServingBubbleClick();
        HandlePostServeHold();
    }

    void SafeDestroy()
    {
        if (isDestroying) return;
        isDestroying = true;

        RestaurantTutorialManager.Instance?.OnThoughtBubbleHandled(this);
        RestaurantTutorialManager.Instance?.UnregisterAI(this);

        if (aiSprite != null) aiSprite.enabled = false;
        if (thoughtBubble != null) thoughtBubble.SetActive(false);
        if (servingThoughtBubble != null) servingThoughtBubble.SetActive(false);

        if (aiBodyCollider != null) aiBodyCollider.enabled = false;
        if (thoughtBubbleCollider != null) thoughtBubbleCollider.enabled = false;
        if (servingBubbleCollider != null) servingBubbleCollider.enabled = false;

        Destroy(gameObject);
    }

    // ── TB1 Click/Touch ────────────────────────────────────────
    void HandleThoughtBubbleClick()
    {
        if (thoughtBubble == null || !thoughtBubble.activeSelf) return;
        if (state != AIState.Idle) return;
        if (thoughtBubbleCollider == null) return;

        // Block TB1 if Tory is already handling any order (cooking, carrying food, or serving walk)
        if (ToryAlive && IsToryBusy()) return;

        Vector3 inputPos = Vector3.zero;
        bool inputDetected = false;

        if (Input.touchCount > 0)
        {
            Touch touch = Input.GetTouch(0);
            if (touch.phase == TouchPhase.Began)
            {
                inputPos = touch.position;
                inputDetected = true;
            }
        }
        else if (Input.GetMouseButtonDown(0))
        {
            inputPos = Input.mousePosition;
            inputDetected = true;
        }

        if (!inputDetected) return;

        Vector3 worldPos = Camera.main.ScreenToWorldPoint(inputPos);
        worldPos.z = thoughtBubble.transform.position.z;

        if (thoughtBubbleCollider.OverlapPoint(worldPos))
            OnTB1Tapped();
    }

    // Returns true when Tory is busy with any part of an order cycle
    bool IsToryBusy()
    {
        if (!ToryAlive) return false;
        return tory.isCookSequenceActive ||
               tory.currentMode == ToryController.ToryMode.Cooking ||
               tory.currentMode == ToryController.ToryMode.FoodOnPlate ||
               tory.currentMode == ToryController.ToryMode.CServing ||
               tory.currentMode == ToryController.ToryMode.ServingWalk;
    }

    void OnTB1Tapped()
    {
        if (thoughtBubble != null)
            thoughtBubble.SetActive(false);

        RestaurantTutorialManager.Instance?.OnThoughtBubbleHandled(this);

        if (ToryAlive)
            tory.StartCookingSequence(this);

        state = AIState.WaitingForCook;
    }

    // ── Called by ToryController when cooking is done ──────────
    public void ShowServingBubble()
    {
        state = AIState.ServingIdle;

        if (servingThoughtBubble != null)
        {
            if (thoughtBubble != null)
                servingThoughtBubble.transform.localPosition = thoughtBubble.transform.localPosition;

            SoundManager.Instance.PlaySFX("Pop");
            servingThoughtBubble.SetActive(true);
            RestaurantTutorialManager.Instance?.OnServingBubbleShown(this, servingThoughtBubble.transform);
        }
        else
        {
            Debug.LogWarning("TB2 is NULL on: " + gameObject.name);
        }
    }

    // ── TB2 single tap — send Tory walking to serve position ──
    void HandleServingBubbleClick()
    {
        if (state != AIState.ServingIdle) return;
        if (aiBodyCollider == null) return;

        if (ToryAlive && tory.currentMode == ToryController.ToryMode.ServingWalk) return;

        Vector3 inputPos = Vector3.zero;
        bool inputDetected = false;

        if (Input.touchCount > 0)
        {
            Touch touch = Input.GetTouch(0);
            if (touch.phase == TouchPhase.Began)
            {
                inputPos = touch.position;
                inputDetected = true;
            }
        }
        else if (Input.GetMouseButtonDown(0))
        {
            inputPos = Input.mousePosition;
            inputDetected = true;
        }

        if (!inputDetected) return;

        Vector3 worldPos = Camera.main.ScreenToWorldPoint(inputPos);
        worldPos.z = transform.position.z;

        if (aiBodyCollider.OverlapPoint(worldPos))
            OnTB2Tapped();
    }

    void OnTB2Tapped()
    {
        if (servingThoughtBubble != null)
            servingThoughtBubble.SetActive(false);

        RestaurantTutorialManager.Instance?.OnThoughtBubbleHandled(this);

        if (ToryAlive)
            tory.StartServingWalk(this);
    }

    // ── Called by ToryController after she finishes her wait ──
    public void OnToryServingDone()
    {
        SoundManager.Instance.PlaySFX("Done");

        // Reveal food right when Tory delivers
        if (ToryAlive)
            tory.RevealFoodObject(this);

        postServeHoldTimer = 0f;
        state = AIState.PostServeHold;
        PlayAnim(eatAnim != null ? eatAnim : idleAnim);
    }

    // ── Post-serve hold timer ──────────────────────────────────
    void HandlePostServeHold()
    {
        if (state != AIState.PostServeHold) return;

        postServeHoldTimer += Time.deltaTime;
        if (postServeHoldTimer >= postServeHoldDuration)
        {
            postServeHoldTimer = 0f;
            StartLeaving();
        }
    }

    // ── Leave ──────────────────────────────────────────────────
    void StartLeaving()
    {
        // ★ Hide food BEFORE the AI leaves so it disappears while still seated
        if (ToryAlive)
            tory.HideFoodObject(this);

        // Free the chair slot
        if (spawner != null)
        {
            if (isRightSide)
                spawner.FreeRightLocation(occupiedLocationIndex);
            else
                spawner.FreeLeftLocation(occupiedLocationIndex);
        }

        exitEnterIndex = enterPoints.Length - 1;
        targetPosition = enterPoints[exitEnterIndex].position;
        state = AIState.LeavingToEnterLast;
        PlayAnim(downRightAnim);
        SoundManager.Instance.PlaySFX("walking");
    }

    // ── Movement ───────────────────────────────────────────────
    void HandleMovement()
    {
        if (state == AIState.Idle ||
            state == AIState.WaitingForCook ||
            state == AIState.ServingIdle ||
            state == AIState.PostServeHold ||
            state == AIState.Done) return;

        float speed;
        switch (state)
        {
            case AIState.MoveToEnter: speed = enterMoveSpeed; break;
            case AIState.MoveToLocation: speed = locationMoveSpeed; break;
            case AIState.LeavingToEnterLast:
            case AIState.LeavingThroughEnters:
            case AIState.LeavingToLastStop: speed = leaveMoveSpeed; break;
            default: speed = enterMoveSpeed; break;
        }

        Vector3 diff = targetPosition - transform.position;
        float distance = diff.magnitude;

        if (distance < 0.05f)
        {
            transform.position = targetPosition;
            OnReachedTarget();
            return;
        }

        Vector3 moveStep = diff.normalized * speed * Time.deltaTime;
        if (moveStep.magnitude > distance) moveStep = diff;
        transform.position += moveStep;
    }

    void OnReachedTarget()
    {
        switch (state)
        {
            case AIState.MoveToEnter:
                currentEnterIndex++;
                if (currentEnterIndex < enterPoints.Length)
                {
                    targetPosition = enterPoints[currentEnterIndex].position;
                    PlayAnim(upLeftAnim);
                }
                else
                {
                    DecideSide();
                }
                break;

            case AIState.MoveToLocation:
                state = AIState.Idle;
                PlayAnim(idleAnim);
                if (thoughtBubble != null)
                {
                    SoundManager.Instance.PlaySFX("Pop");
                    thoughtBubble.SetActive(true);
                    RestaurantTutorialManager.Instance?.OnThoughtBubbleShown(this, thoughtBubble.transform);
                }
                else
                    Debug.LogWarning("TB1 is NULL on: " + gameObject.name);
                break;

            case AIState.LeavingToEnterLast:
                exitEnterIndex--;
                if (exitEnterIndex >= 0)
                {
                    targetPosition = enterPoints[exitEnterIndex].position;
                    state = AIState.LeavingThroughEnters;
                    PlayAnim(downRightAnim);
                }
                else
                {
                    GoToLastStop();
                }
                break;

            case AIState.LeavingThroughEnters:
                exitEnterIndex--;
                if (exitEnterIndex >= 0)
                {
                    targetPosition = enterPoints[exitEnterIndex].position;
                    PlayAnim(downRightAnim);
                }
                else
                {
                    GoToLastStop();
                }
                break;

            case AIState.LeavingToLastStop:
                state = AIState.Done;
                SafeDestroy();
                break;
        }
    }

    void GoToLastStop()
    {
        if (lastStop != null)
            targetPosition = lastStop.position;
        else
        {
            Debug.LogWarning("LastStop not assigned on: " + gameObject.name + " — using spawn position");
            targetPosition = spawnPosition;
        }

        state = AIState.LeavingToLastStop;
        PlayAnim(downLeftAnim);
        SoundManager.Instance.PlaySFX("walking");
    }

    // ── Side Decision ──────────────────────────────────────────
    void DecideSide()
    {
        bool rightHasFree = spawner != null && spawner.HasFreeRightLocation();
        bool leftHasFree = spawner != null && spawner.HasFreeLeftLocation();

        if (rightHasFree && leftHasFree) { if (Random.value > 0.5f) GoRight(); else GoLeft(); }
        else if (rightHasFree) GoRight();
        else if (leftHasFree) GoLeft();
        else
        {
            Debug.Log("No free locations — destroying AI");
            SafeDestroy();
        }
    }

    void GoRight()
    {
        int index = spawner.GetFreeRightLocation();
        if (index == -1)
        {
            int leftIndex = spawner.GetFreeLeftLocation();
            if (leftIndex == -1) { SafeDestroy(); return; }
            isRightSide = false;
            occupiedLocationIndex = leftIndex;
            targetPosition = leftLocations[leftIndex].position;
            state = AIState.MoveToLocation;
            PlayAnim(upLeftAnim);
            SoundManager.Instance.PlaySFX("walking");
            return;
        }
        isRightSide = true;
        occupiedLocationIndex = index;
        targetPosition = rightLocations[index].position;
        state = AIState.MoveToLocation;
        PlayAnim(upRightAnim);
        SoundManager.Instance.PlaySFX("walking");
    }

    void GoLeft()
    {
        int index = spawner.GetFreeLeftLocation();
        if (index == -1)
        {
            int rightIndex = spawner.GetFreeRightLocation();
            if (rightIndex == -1) { SafeDestroy(); return; }
            isRightSide = true;
            occupiedLocationIndex = rightIndex;
            targetPosition = rightLocations[rightIndex].position;
            state = AIState.MoveToLocation;
            PlayAnim(upRightAnim);
            SoundManager.Instance.PlaySFX("walking");
            return;
        }
        isRightSide = false;
        occupiedLocationIndex = index;
        targetPosition = leftLocations[index].position;
        state = AIState.MoveToLocation;
        PlayAnim(upLeftAnim);
        SoundManager.Instance.PlaySFX("walking");
    }

    // ── Animation System ───────────────────────────────────────
    void PlayAnim(TorySpriteAnimation anim)
    {
        if (anim == null || anim.frames == null || anim.frames.Length == 0) return;
        if (anim == currentAnim) return;

        currentAnim = anim;
        currentFrame = 0;
        frameTimer = 0f;
        aiSprite.sprite = currentAnim.frames[0];
    }

    void TickAnimation()
    {
        if (currentAnim == null || currentAnim.frames.Length == 0) return;

        frameTimer += Time.deltaTime;
        float frameDuration = 1f / Mathf.Max(currentAnim.fps, 0.01f);

        if (frameTimer >= frameDuration)
        {
            frameTimer -= frameDuration;
            currentFrame = (currentFrame + 1) % currentAnim.frames.Length;
            aiSprite.sprite = currentAnim.frames[currentFrame];
        }
    }
}