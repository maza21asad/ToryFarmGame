// AISpawner.cs
using UnityEngine;
using System.Collections.Generic;

public class AISpawner : MonoBehaviour
{
    [Header("AI Type 1")]
    public GameObject[] aiType1Objects;
    [Tooltip("Scale applied to every AI of Type 1 when spawned")]
    public Vector3 aiType1Scale = Vector3.one;

    [Header("AI Type 2")]
    public GameObject[] aiType2Objects;
    [Tooltip("Scale applied to every AI of Type 2 when spawned")]
    public Vector3 aiType2Scale = Vector3.one;

    [Header("AI Type 3")]
    public GameObject[] aiType3Objects;
    [Tooltip("Scale applied to every AI of Type 3 when spawned")]
    public Vector3 aiType3Scale = Vector3.one;

    [Header("AI Type 4")]
    public GameObject[] aiType4Objects;
    [Tooltip("Scale applied to every AI of Type 4 when spawned")]
    public Vector3 aiType4Scale = Vector3.one;

    [Header("Spawn Point")]
    public Transform spawnPoint;

    [Header("Enter Points (in order)")]
    public Transform[] enterPoints;

    [Header("Right Side Locations")]
    public Transform[] rightLocations;

    [Header("Left Side Locations")]
    public Transform[] leftLocations;

    [Header("Spawn Timing")]
    [Tooltip("Minimum random delay between each individual AI spawn")]
    public float minSpawnInterval = 2f;
    [Tooltip("Maximum random delay between each individual AI spawn")]
    public float maxSpawnInterval = 6f;

    [Header("Spawn Settings")]
    [Tooltip("How many AI customers spawn per phase (no repeats within a phase)")]
    public int numberOfAIsToSpawn = 4;
    [Tooltip("How many total phases before the game finishes")]
    public int totalPhases = 3;
    [Tooltip("How many AIs must leave before the next phase starts spawning")]
    [Range(1, 8)]
    public int respawnTriggerCount = 2;

    [Header("UI — Game Finish")]
    [Tooltip("Panel shown permanently when all phases are complete and all AIs have left")]
    public GameObject congratulationPanel;

    // ── Location tracking ──────────────────────────────────────
    private HashSet<int> occupiedRight = new HashSet<int>();
    private HashSet<int> occupiedLeft = new HashSet<int>();

    // ── Phase tracking ─────────────────────────────────────────
    private int phasesStarted = 0;  // how many phases have been kicked off
    private int activeAICount = 0;  // currently alive on screen
    private int leftSincePhaseStart = 0;  // AIs that left since last phase began
    private bool gameFinished = false;

    // ── Spawn queue — types waiting to spawn one by one ───────
    private List<(GameObject[] objects, Vector3 scale)> spawnQueue
        = new List<(GameObject[] objects, Vector3 scale)>();

    // ── Individual spawn timer ────────────────────────────────
    private float spawnTimer = 0f;
    private bool isSpawnTimerActive = false;

    // ── Helpers ───────────────────────────────────────────────
    private int TotalLocations =>
        (rightLocations != null ? rightLocations.Length : 0) +
        (leftLocations != null ? leftLocations.Length : 0);

    private int OccupiedCount => occupiedRight.Count + occupiedLeft.Count;
    private bool AnyLocationFree => OccupiedCount < TotalLocations;

    // ── Unity lifecycle ────────────────────────────────────────
    void Start()
    {
        if (congratulationPanel != null)
            congratulationPanel.SetActive(false);

        StartNextPhase(); // kicks off phase 1
    }

    void Update()
    {
        if (gameFinished || !isSpawnTimerActive) return;

        spawnTimer -= Time.deltaTime;
        if (spawnTimer <= 0f)
        {
            isSpawnTimerActive = false;
            TrySpawnNext();
        }
    }

    // ── Start a new phase ─────────────────────────────────────
    void StartNextPhase()
    {
        if (gameFinished) return;

        phasesStarted++;
        Debug.Log($"[AISpawner] Starting phase {phasesStarted} / {totalPhases}");

        BuildPhaseQueue();

        if (AnyLocationFree)
            ScheduleNextSpawn();
    }

    // ── Build a fresh shuffled queue for the phase ────────────
    void BuildPhaseQueue()
    {
        var types = new List<(GameObject[] objects, Vector3 scale)>();

        if (aiType1Objects != null && aiType1Objects.Length > 0) types.Add((aiType1Objects, aiType1Scale));
        if (aiType2Objects != null && aiType2Objects.Length > 0) types.Add((aiType2Objects, aiType2Scale));
        if (aiType3Objects != null && aiType3Objects.Length > 0) types.Add((aiType3Objects, aiType3Scale));
        if (aiType4Objects != null && aiType4Objects.Length > 0) types.Add((aiType4Objects, aiType4Scale));

        if (types.Count == 0)
        {
            Debug.LogWarning("AISpawner: No AI types assigned!");
            return;
        }

        // Fisher-Yates shuffle — different random order every phase
        for (int i = types.Count - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            var temp = types[i];
            types[i] = types[j];
            types[j] = temp;
        }

        spawnQueue.Clear();
        int count = Mathf.Min(numberOfAIsToSpawn, types.Count);
        for (int i = 0; i < count; i++)
            spawnQueue.Add(types[i]);

        leftSincePhaseStart = 0;
    }

    // ── Schedule a random-delay timer for the next spawn ──────
    void ScheduleNextSpawn()
    {
        if (spawnQueue.Count == 0) return;
        if (isSpawnTimerActive) return;
        if (gameFinished) return;

        spawnTimer = Random.Range(minSpawnInterval, maxSpawnInterval);
        isSpawnTimerActive = true;
    }

    // ── Try to pop and spawn the next AI in the queue ─────────
    void TrySpawnNext()
    {
        if (spawnQueue.Count == 0 || gameFinished) return;

        if (!AnyLocationFree)
        {
            // Chairs full — retry after another random delay
            ScheduleNextSpawn();
            return;
        }

        var (objects, scale) = spawnQueue[0];
        spawnQueue.RemoveAt(0);
        SpawnSingle(objects, scale);

        if (spawnQueue.Count > 0)
            ScheduleNextSpawn();
    }

    // ── Instantiate one AI ─────────────────────────────────────
    void SpawnSingle(GameObject[] pool, Vector3 scale)
    {
        if (pool == null || pool.Length == 0) return;

        GameObject original = pool[Random.Range(0, pool.Length)];
        GameObject spawned = Instantiate(original, spawnPoint.position, Quaternion.identity);

        spawned.SetActive(false);
        spawned.transform.localScale = scale;

        SpriteRenderer sr = spawned.GetComponent<SpriteRenderer>();
        if (sr != null) sr.enabled = true;

        AICustomer ai = spawned.GetComponent<AICustomer>();
        if (ai != null)
            ai.Init(enterPoints, rightLocations, leftLocations, spawnPoint.position, this);

        activeAICount++;
        spawned.SetActive(true);
    }

    // ── Location checks (called by AICustomer) ─────────────────
    public bool HasFreeRightLocation()
    {
        if (rightLocations == null || rightLocations.Length == 0) return false;
        return occupiedRight.Count < rightLocations.Length;
    }

    public bool HasFreeLeftLocation()
    {
        if (leftLocations == null || leftLocations.Length == 0) return false;
        return occupiedLeft.Count < leftLocations.Length;
    }

    public int GetFreeRightLocation()
    {
        if (rightLocations == null) return -1;
        for (int i = 0; i < rightLocations.Length; i++)
        {
            if (!occupiedRight.Contains(i))
            {
                occupiedRight.Add(i);
                return i;
            }
        }
        return -1;
    }

    public int GetFreeLeftLocation()
    {
        if (leftLocations == null) return -1;
        for (int i = 0; i < leftLocations.Length; i++)
        {
            if (!occupiedLeft.Contains(i))
            {
                occupiedLeft.Add(i);
                return i;
            }
        }
        return -1;
    }

    // ── Free location when AI leaves ──────────────────────────
    public void FreeRightLocation(int index)
    {
        occupiedRight.Remove(index);
        activeAICount = Mathf.Max(0, activeAICount - 1);
        OnAILeft();
    }

    public void FreeLeftLocation(int index)
    {
        occupiedLeft.Remove(index);
        activeAICount = Mathf.Max(0, activeAICount - 1);
        OnAILeft();
    }

    // ── Called every time any AI fully leaves ─────────────────
    void OnAILeft()
    {
        if (gameFinished) return;

        leftSincePhaseStart++;

        bool queueDone = spawnQueue.Count == 0 && !isSpawnTimerActive;
        bool allGone = activeAICount <= 0;

        // ── All phases done + all AIs gone = GAME FINISH ───────
        if (allGone && queueDone && phasesStarted >= totalPhases)
        {
            GameFinish();
            return;
        }

        // ── Enough left + more phases remain = start next phase ─
        if (leftSincePhaseStart >= respawnTriggerCount
            && queueDone
            && phasesStarted < totalPhases)
        {
            StartNextPhase();
            return;
        }

        // ── Failsafe: all gone but phases remain and nothing queued
        if (allGone && queueDone && phasesStarted < totalPhases)
        {
            StartNextPhase();
        }
    }

    // ── Game finish — show panel permanently ──────────────────
    void GameFinish()
    {
        gameFinished = true;
        isSpawnTimerActive = false;
        spawnQueue.Clear();

        Debug.Log("[AISpawner] All phases complete — Game Finished!");

        if (congratulationPanel != null)
        {
            SoundManager.Instance.PlaySFX("tada");
            congratulationPanel.SetActive(true);
        }
    }
}