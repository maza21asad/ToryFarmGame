using UnityEngine;


public class ObstacleController : MonoBehaviour
{
    [Header("Cow References")]
    [Tooltip("Drag the CowHitZone child GameObject from inside the Cow.")]
    public RectTransform cowHitZone;
    public Cowanimationcontroller cowController;

    [Header("Obstacle Hit Zone")]
    [Tooltip("Drag the HitZone child GameObject from inside this obstacle.")]
    public RectTransform obstacleHitZone;

    private bool triggered = false;

    // ──────────────────────────────────────────────────────────
    void Start()
    {
        // Auto-find cow controller if not assigned
        if (cowController == null)
            cowController = Object.FindFirstObjectByType<Cowanimationcontroller>();

        // Auto-find CowHitZone if not assigned
        if (cowHitZone == null && cowController != null)
        {
            Transform found = cowController.transform.Find("CowHitZone");
            if (found != null)
                cowHitZone = found.GetComponent<RectTransform>();
        }

        if (cowHitZone == null)
            Debug.LogError($"[Obstacle] {gameObject.name}: CowHitZone not assigned! " +
                           "Create a child on the Cow named 'CowHitZone' and drag it in.");

        if (obstacleHitZone == null)
            Debug.LogError($"[Obstacle] {gameObject.name}: Obstacle HitZone not assigned! " +
                           "Create a child on this obstacle and drag it in.");

        if (cowController == null)
            Debug.LogError($"[Obstacle] {gameObject.name}: CowController not found!");
    }

    void Update()
    {
        if (triggered) return;
        if (cowController == null || cowHitZone == null || obstacleHitZone == null) return;

        Cowanimationcontroller.CowState s = cowController.CurrentState;
        if (s == Cowanimationcontroller.CowState.Win) return;
        if (s == Cowanimationcontroller.CowState.Hit) return;
        if (s == Cowanimationcontroller.CowState.Idle) return;

        if (!Overlaps(cowHitZone, obstacleHitZone)) return;

        // ── Hit! ───────────────────────────────────────────────
        triggered = true;
        Debug.Log($"[Obstacle] CowHitZone touched HitZone of {gameObject.name}!");

        cowController.PlayHit();

        if (RunnerGameManager.Instance != null)
            RunnerGameManager.Instance.ShowGameOver();
        else
            Debug.LogError("[Obstacle] RunnerGameManager.Instance is NULL!");
    }

    // ──────────────────────────────────────────────────────────
    bool Overlaps(RectTransform a, RectTransform b)
    {
        return GetCanvasRect(a).Overlaps(GetCanvasRect(b));
    }

    Rect GetCanvasRect(RectTransform rt)
    {
        Vector3[] corners = new Vector3[4];
        rt.GetWorldCorners(corners);

        Canvas canvas = rt.GetComponentInParent<Canvas>();
        if (canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay)
        {
            for (int i = 0; i < 4; i++)
                corners[i] = canvas.transform.InverseTransformPoint(corners[i]);
        }

        float xMin = Mathf.Min(corners[0].x, corners[2].x);
        float xMax = Mathf.Max(corners[0].x, corners[2].x);
        float yMin = Mathf.Min(corners[0].y, corners[2].y);
        float yMax = Mathf.Max(corners[0].y, corners[2].y);

        return new Rect(xMin, yMin, xMax - xMin, yMax - yMin);
    }
}