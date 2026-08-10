using UnityEngine;

[System.Serializable]
public class TorySpriteAnimation
{
    public string name;
    public Sprite[] frames;
    public float fps = 8f;
}

public class ToryController : MonoBehaviour
{
    [Header("Tory Sprite Renderer")]
    public SpriteRenderer torySprite;

    [Header("Main Camera")]
    public Camera mainCamera;

    [Header("Move Speed (player-controlled walking)")]
    public float moveSpeed = 3f;

    [Header("Cook Move Speed (walking to cook location after TB1 tap)")]
    public float cookMoveSpeed = 5f;

    [Header("Serving Move Speed (walking to serve position after TB2 tap)")]
    public float servingMoveSpeed = 5f;

    [Header("Camera Follow")]
    public float followSmoothSpeed = 8f;

    [Header("Idle Animations")]
    public TorySpriteAnimation idleRightAnim;
    public TorySpriteAnimation idleLeftAnim;

    [Header("Diagonal Walk Animations")]
    public TorySpriteAnimation upRightAnim;
    public TorySpriteAnimation upLeftAnim;
    public TorySpriteAnimation downRightAnim;
    public TorySpriteAnimation downLeftAnim;

    [Header("Cook Animations")]
    public TorySpriteAnimation cookingAnim;
    public TorySpriteAnimation foodOnPlateAnim;

    [Header("Cook Timing")]
    public float cookDuration = 2f;
    public float foodOnPlateDuration = 1.5f;

    [Header("CServing Mode Animations")]
    public TorySpriteAnimation cIdleAnim;
    public TorySpriteAnimation cWalkRightAnim;
    public TorySpriteAnimation cWalkLeftAnim;

    [Header("CServing Mode — Diagonal Walk Animations")]
    public TorySpriteAnimation cUpRightAnim;
    public TorySpriteAnimation cUpLeftAnim;
    public TorySpriteAnimation cDownRightAnim;
    public TorySpriteAnimation cDownLeftAnim;

    [Header("Cook Location")]
    public Transform cookLocation;

    [Header("Serve Positions — Right Side (index matches right location index)")]
    public Transform[] rightServePositions;

    [Header("Serve Positions — Left Side (index matches left location index)")]
    public Transform[] leftServePositions;

    [Header("Food Objects — Right Side (index matches right location index)")]
    public GameObject[] rightSideFoodObjects;

    [Header("Food Objects — Left Side (index matches left location index)")]
    public GameObject[] leftSideFoodObjects;

    [Header("Food Visible Duration (seconds food stays visible after AI leaves)")]
    public float foodVisibleDuration = 5f;

    // ── private state ──────────────────────────────────────────
    private Vector3 targetPosition;
    private bool isMoving = false;

    private TorySpriteAnimation currentAnim;
    private int currentFrame;
    private float frameTimer;
    private bool lastFacingRight = true;

    public enum ToryMode { Normal, Cooking, FoodOnPlate, CServing, ServingWalk }
    public ToryMode currentMode = ToryMode.Normal;

    public bool isCookSequenceActive = false;

    private float cookTimer = 0f;
    private bool isAlive = false;

    private AICustomer currentServingCustomer = null;
    private bool servingToRightSide = false;

    // ── Food timer tracking ────────────────────────────────────
    private float[] rightFoodTimers;
    private float[] leftFoodTimers;
    private bool[] rightFoodTimerActive;
    private bool[] leftFoodTimerActive;

    // ── Unity lifecycle ────────────────────────────────────────
    void Awake()
    {
        isAlive = true;
    }

    void OnDestroy()
    {
        isAlive = false;
    }

    void Start()
    {
        if (mainCamera == null)
            mainCamera = Camera.main;

        targetPosition = transform.position;
        PlayAnim(idleRightAnim);

        if (rightSideFoodObjects != null)
        {
            rightFoodTimers = new float[rightSideFoodObjects.Length];
            rightFoodTimerActive = new bool[rightSideFoodObjects.Length];
        }
        if (leftSideFoodObjects != null)
        {
            leftFoodTimers = new float[leftSideFoodObjects.Length];
            leftFoodTimerActive = new bool[leftSideFoodObjects.Length];
        }
    }

    void Update()
    {
        if (!isAlive) return;
        if (torySprite == null || mainCamera == null) return;

        TickAnimation();
        HandleInput();
        if (isMoving) MoveToTarget();
        HandleCookingLogic();
        HandleFoodTimers();
        CameraFollow();
    }

    // ── Food Timer Logic ───────────────────────────────────────
    void HandleFoodTimers()
    {
        if (rightFoodTimerActive != null)
        {
            for (int i = 0; i < rightFoodTimerActive.Length; i++)
            {
                if (!rightFoodTimerActive[i]) continue;

                rightFoodTimers[i] += Time.deltaTime;
                if (rightFoodTimers[i] >= foodVisibleDuration)
                {
                    rightFoodTimerActive[i] = false;
                    rightFoodTimers[i] = 0f;
                    if (rightSideFoodObjects[i] != null)
                        rightSideFoodObjects[i].SetActive(false);
                }
            }
        }

        if (leftFoodTimerActive != null)
        {
            for (int i = 0; i < leftFoodTimerActive.Length; i++)
            {
                if (!leftFoodTimerActive[i]) continue;

                leftFoodTimers[i] += Time.deltaTime;
                if (leftFoodTimers[i] >= foodVisibleDuration)
                {
                    leftFoodTimerActive[i] = false;
                    leftFoodTimers[i] = 0f;
                    if (leftSideFoodObjects[i] != null)
                        leftSideFoodObjects[i].SetActive(false);
                }
            }
        }
    }

    // ── Cooking Logic ──────────────────────────────────────────
    void HandleCookingLogic()
    {
        if (currentMode == ToryMode.Cooking)
        {
            cookTimer += Time.deltaTime;
            if (cookTimer >= cookDuration)
            {
                cookTimer = 0f;
                currentMode = ToryMode.FoodOnPlate;
                PlayAnim(foodOnPlateAnim);
            }
        }
        else if (currentMode == ToryMode.FoodOnPlate)
        {
            cookTimer += Time.deltaTime;
            if (cookTimer >= foodOnPlateDuration)
            {
                cookTimer = 0f;
                EnterCServing();
            }
        }
    }

    // ── Called by AICustomer when TB1 is tapped ────────────────
    public void StartCookingSequence(AICustomer customer)
    {
        if (!isAlive) return;

        currentServingCustomer = customer;
        currentMode = ToryMode.Normal;
        cookTimer = 0f;
        isMoving = false;
        isCookSequenceActive = true;

        if (cookLocation != null)
        {
            targetPosition = cookLocation.position;
            isMoving = true;
            SoundManager.Instance.PlaySFX("walking");
        }
        else
        {
            StartCooking();
        }
    }

    // ── Called by AICustomer when TB2 is tapped ───────────────
    public void StartServingWalk(AICustomer customer)
    {
        if (!isAlive) return;

        currentServingCustomer = customer;
        servingToRightSide = customer.IsRightSide;
        currentMode = ToryMode.ServingWalk;
        isCookSequenceActive = false;

        Vector3 servePos = GetServePosition(customer);
        targetPosition = servePos;
        isMoving = true;
        SoundManager.Instance.PlaySFX("walking");

        if (servingToRightSide)
        {
            lastFacingRight = true;
            PlayAnim(cWalkRightAnim);
        }
        else
        {
            lastFacingRight = false;
            PlayAnim(cWalkLeftAnim);
        }
    }

    // ── Get the serve position for a customer ─────────────────
    Vector3 GetServePosition(AICustomer customer)
    {
        if (customer == null) return transform.position;

        bool isRight = customer.IsRightSide;
        int index = customer.OccupiedLocationIndex;
        Transform[] servePositions = isRight ? rightServePositions : leftServePositions;

        if (servePositions != null && index >= 0 && index < servePositions.Length && servePositions[index] != null)
        {
            return servePositions[index].position;
        }

        Debug.LogWarning($"No serve position for side:{(isRight ? "Right" : "Left")} index:{index} — falling back to customer position");
        return customer.transform.position;
    }

    // ── Start Cooking (called on arrival at cookLocation) ──────
    void StartCooking()
    {
        currentMode = ToryMode.Cooking;
        isMoving = false;
        cookTimer = 0f;
        PlayAnim(cookingAnim);
        SoundManager.Instance.PlaySFX("Cook");
    }

    // ── Enter CServing ─────────────────────────────────────────
    void EnterCServing()
    {
        currentMode = ToryMode.CServing;
        isCookSequenceActive = false;
        isMoving = false;
        cookTimer = 0f;
        PlayAnim(cIdleAnim);

        if (currentServingCustomer != null)
        {
            currentServingCustomer.ShowServingBubble();
            currentServingCustomer = null;
        }
    }

    // ── Called when Tory arrives at serve position — instantly back to normal ──
    void FinishServing()
    {
        AICustomer served = currentServingCustomer;

        currentMode = ToryMode.Normal;
        isCookSequenceActive = false;
        currentServingCustomer = null;
        cookTimer = 0f;
        isMoving = false;

        PlayAnim(lastFacingRight ? idleRightAnim : idleLeftAnim);

        if (served != null)
            served.OnToryServingDone();
    }

    // ── Reveal food object and start its auto-hide timer ──────
    public void RevealFoodObject(AICustomer customer)
    {
        if (customer == null) return;

        bool isRight = customer.IsRightSide;
        int index = customer.OccupiedLocationIndex;
        GameObject[] foods = isRight ? rightSideFoodObjects : leftSideFoodObjects;

        if (foods == null || index < 0 || index >= foods.Length) return;

        if (foods[index] != null)
        {
            foods[index].SetActive(true);
            Debug.Log($"Food revealed — side: {(isRight ? "Right" : "Left")}, position index: {index}");

            if (isRight && rightFoodTimerActive != null && index < rightFoodTimerActive.Length)
            {
                rightFoodTimers[index] = 0f;
                rightFoodTimerActive[index] = true;
            }
            else if (!isRight && leftFoodTimerActive != null && index < leftFoodTimerActive.Length)
            {
                leftFoodTimers[index] = 0f;
                leftFoodTimerActive[index] = true;
            }
        }
    }

    // ── Hide food object (now only called if needed externally) ──
    public void HideFoodObject(AICustomer customer)
    {
        if (customer == null) return;

        bool isRight = customer.IsRightSide;
        int index = customer.OccupiedLocationIndex;
        GameObject[] foods = isRight ? rightSideFoodObjects : leftSideFoodObjects;

        if (foods == null || index < 0 || index >= foods.Length) return;

        if (foods[index] != null)
            foods[index].SetActive(false);

        if (isRight && rightFoodTimerActive != null && index < rightFoodTimerActive.Length)
        {
            rightFoodTimerActive[index] = false;
            rightFoodTimers[index] = 0f;
        }
        else if (!isRight && leftFoodTimerActive != null && index < leftFoodTimerActive.Length)
        {
            leftFoodTimerActive[index] = false;
            leftFoodTimers[index] = 0f;
        }
    }

    // ── Legacy / unused ───────────────────────────────────────
    public void OnServed()
    {
        if (!isAlive) return;

        currentMode = ToryMode.Normal;
        isCookSequenceActive = false;
        currentServingCustomer = null;
        cookTimer = 0f;
        isMoving = false;
        PlayAnim(lastFacingRight ? idleRightAnim : idleLeftAnim);
    }

    // ── Input Detection ────────────────────────────────────────
    void HandleInput()
    {
        if (currentMode == ToryMode.Cooking ||
            currentMode == ToryMode.FoodOnPlate ||
            currentMode == ToryMode.ServingWalk) return;

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

        Vector3 worldPoint = mainCamera.ScreenToWorldPoint(inputPos);
        worldPoint.z = transform.position.z;

        Collider2D[] allHits = Physics2D.OverlapPointAll(worldPoint);

        foreach (Collider2D h in allHits)
        {
            if (h.CompareTag("ThoughtBubble"))
                return;
        }

        bool hitFloor = false;

        foreach (Collider2D hit in allHits)
        {
            if (hit.CompareTag("Floor"))
                hitFloor = true;
        }

        if (hitFloor)
        {
            targetPosition = worldPoint;
            isMoving = true;
            SoundManager.Instance.PlaySFX("walking");
        }
    }

    // ── Camera Follow ──────────────────────────────────────────
    void CameraFollow()
    {
        if (!isAlive || mainCamera == null) return;

        Vector3 targetCamPos = new Vector3(
            transform.position.x,
            transform.position.y,
            mainCamera.transform.position.z
        );

        mainCamera.transform.position = Vector3.Lerp(
            mainCamera.transform.position,
            targetCamPos,
            followSmoothSpeed * Time.deltaTime
        );
    }

    // ── Movement ───────────────────────────────────────────────
    void MoveToTarget()
    {
        if (!isAlive) return;

        Vector3 diff = targetPosition - transform.position;
        float distance = diff.magnitude;

        float speed;
        if (currentMode == ToryMode.ServingWalk)
            speed = servingMoveSpeed;
        else if (isCookSequenceActive)
            speed = cookMoveSpeed;
        else
            speed = moveSpeed;

        float arrivalThreshold = 0.05f;

        if (distance < arrivalThreshold)
        {
            transform.position = targetPosition;
            isMoving = false;

            if (currentMode == ToryMode.Normal && isCookSequenceActive && cookLocation != null)
            {
                float dist = Vector3.Distance(transform.position, cookLocation.position);
                if (dist < 0.2f)
                {
                    StartCooking();
                    return;
                }
            }

            // ★ CHANGED: Tory arrives at serve position → instantly finish, no waiting
            if (currentMode == ToryMode.ServingWalk)
            {
                FinishServing();
                return;
            }

            if (currentMode == ToryMode.CServing)
                PlayAnim(cIdleAnim);
            else
                PlayAnim(lastFacingRight ? idleRightAnim : idleLeftAnim);

            return;
        }

        Vector3 direction = diff.normalized;
        Vector3 moveStep = direction * speed * Time.deltaTime;

        if (moveStep.magnitude > distance)
            moveStep = diff;

        Vector3 newPos = transform.position + moveStep;

        if (currentMode != ToryMode.ServingWalk)
        {
            Collider2D[] hits = Physics2D.OverlapPointAll(newPos);
            bool insideFloor = false;

            foreach (Collider2D hit in hits)
            {
                if (hit.CompareTag("Floor"))
                {
                    insideFloor = true;
                    break;
                }
            }

            if (!insideFloor)
            {
                isMoving = false;
                PlayAnim(currentMode == ToryMode.CServing
                    ? cIdleAnim
                    : (lastFacingRight ? idleRightAnim : idleLeftAnim));
                return;
            }
        }

        transform.position = newPos;
        HandleAnimation(new Vector2(direction.x, direction.y));
    }

    // ── Direction Animation ────────────────────────────────────
    void HandleAnimation(Vector2 dir)
    {
        if (currentMode == ToryMode.ServingWalk)
        {
            if (servingToRightSide)
            {
                lastFacingRight = true;
                PlayAnim(cWalkRightAnim);
            }
            else
            {
                lastFacingRight = false;
                PlayAnim(cWalkLeftAnim);
            }
            return;
        }

        if (currentMode == ToryMode.CServing)
        {
            PlayCServingDirectionalAnim(dir);
            return;
        }

        if (dir.x > 0 && dir.y > 0) { lastFacingRight = true; PlayAnim(upRightAnim); }
        else if (dir.x < 0 && dir.y > 0) { lastFacingRight = false; PlayAnim(upLeftAnim); }
        else if (dir.x > 0 && dir.y < 0) { lastFacingRight = true; PlayAnim(downRightAnim); }
        else if (dir.x < 0 && dir.y < 0) { lastFacingRight = false; PlayAnim(downLeftAnim); }
    }

    // ── CServing Direction Animation (mirrors normal-mode diagonal logic) ──
    void PlayCServingDirectionalAnim(Vector2 dir)
    {
        if (dir.x > 0 && dir.y > 0) { lastFacingRight = true; PlayAnim(cUpRightAnim); }
        else if (dir.x < 0 && dir.y > 0) { lastFacingRight = false; PlayAnim(cUpLeftAnim); }
        else if (dir.x > 0 && dir.y < 0) { lastFacingRight = true; PlayAnim(cDownRightAnim); }
        else if (dir.x < 0 && dir.y < 0) { lastFacingRight = false; PlayAnim(cDownLeftAnim); }
        else if (dir.x >= 0) { lastFacingRight = true; PlayAnim(cWalkRightAnim); }
        else { lastFacingRight = false; PlayAnim(cWalkLeftAnim); }
    }

    // ── Animation System ───────────────────────────────────────
    void PlayAnim(TorySpriteAnimation anim)
    {
        if (!isAlive) return;
        if (anim == null || anim.frames == null || anim.frames.Length == 0) return;
        if (anim == currentAnim) return;

        currentAnim = anim;
        currentFrame = 0;
        frameTimer = 0f;
        torySprite.sprite = currentAnim.frames[0];
    }

    void TickAnimation()
    {
        if (!isAlive) return;
        if (currentAnim == null || currentAnim.frames.Length == 0) return;

        frameTimer += Time.deltaTime;
        float frameDuration = 1f / Mathf.Max(currentAnim.fps, 0.01f);

        if (frameTimer >= frameDuration)
        {
            frameTimer -= frameDuration;
            currentFrame = (currentFrame + 1) % currentAnim.frames.Length;
            torySprite.sprite = currentAnim.frames[currentFrame];
        }
    }
}