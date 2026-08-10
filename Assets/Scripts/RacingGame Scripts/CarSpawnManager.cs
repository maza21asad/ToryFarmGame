using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using TMPro;

public class CarSpawnManager : MonoBehaviour
{
    [Header("Cars (5 Car GameObjects)")]
    public GameObject[] cars;

    [Header("Spawners")]
    public RectTransform[] startSpawners;
    public RectTransform[] endSpawners;

    [Header("Spawn Settings")]
    public float firstCarDelay = 5f;
    public float spawnInterval = 10f;
    public int totalCarsToSpawn = 10;

    [Header("Player")]
    public RectTransform playerObject;

    [Header("Game Over")]
    public GameObject gameOverPanel;
    public GameObject crashEffect;

    [Header("Road Lines")]
    public GameObject[] roadLines;

    [Header("Win Settings")]
    public GameObject finishLineObject;
    public GameObject finishLineMover;
    public RectTransform finishLineTarget;
    public GameObject youWinPanel;
    public float moveToFinishSpeed = 0.5f;
    public Vector3 playerEndScale = new Vector3(0.1f, 0.1f, 0.1f);
    public float youWinDelay = 5f;

    [Header("BGHouses Grow")]
    public RectTransform bgHouses;
    public Vector3 bgHousesStartScale = new Vector3(0.1f, 0.1f, 0.1f);
    public Vector3 bgHousesEndScale = new Vector3(1f, 1f, 1f);
    public float bgHousesDuration = 5f;   // seconds to complete the grow
    public float bgHousesSpeed = 1f;   // multiplier: 1 = normal, 2 = twice as fast

    [Header("Position UI")]
    public TextMeshProUGUI positionText;

    [Header("Countdown")]
    public TextMeshProUGUI countdownText;

    private float spawnTimer = 0f;
    private float firstSpawnTimer = 0f;
    private bool firstCarSpawned = false;
    private bool isGameOver = false;
    private int spawnedCount = 0;
    private int finishedCount = 0;
    private bool allSpawned = false;
    private bool winTriggered = false;
    private int[] carSpawnerIndex;
    private int carsPassed = 0;
    private int nextCarIndex = 0;

    private BoxCollider2D[] playerColliders;
    private Coroutine bgHousesCoroutine;
    private bool gameStarted = false;

    void Start()
    {
        carSpawnerIndex = new int[cars.Length];
        for (int i = 0; i < carSpawnerIndex.Length; i++)
            carSpawnerIndex[i] = -1;

        foreach (GameObject car in cars)
            car.SetActive(false);

        if (finishLineObject != null) finishLineObject.SetActive(false);
        if (finishLineMover != null) finishLineMover.SetActive(false);
        if (youWinPanel != null) youWinPanel.SetActive(false);
        if (crashEffect != null) crashEffect.SetActive(false);

        if (bgHouses != null)
            bgHouses.localScale = bgHousesStartScale;

        BoxCollider2D[] allColliders = playerObject.GetComponentsInChildren<BoxCollider2D>(true);
        List<BoxCollider2D> filteredColliders = new List<BoxCollider2D>();
        foreach (BoxCollider2D col in allColliders)
        {
            if (col.GetComponent<BoxColliderforDetection>() == null)
                filteredColliders.Add(col);
        }
        playerColliders = filteredColliders.ToArray();

        if (playerColliders.Length == 0)
            Debug.LogWarning("CarSpawnManager → No BoxCollider2D found on Player!");
        else
            Debug.Log("CarSpawnManager → Found " + playerColliders.Length + " player BoxCollider2D(s).");

        // Disable lane input until countdown finishes
        LaneController laneCtrl = playerObject.GetComponent<LaneController>();
        if (laneCtrl != null) laneCtrl.enabled = false;

        UpdatePositionText();

        Time.timeScale = 0f;
        StartCoroutine(CountdownCoroutine());
    }

    IEnumerator CountdownCoroutine()
    {
        if (countdownText != null) countdownText.gameObject.SetActive(true);

        string[] counts = { "3", "2", "1" };
        foreach (string count in counts)
        {
            if (countdownText != null) countdownText.text = count;
            SoundManager.Instance.PlaySFX("count");
            yield return new WaitForSecondsRealtime(1f);
        }

        // Show Go! and resume the game
        if (countdownText != null) countdownText.text = "Go!";
        SoundManager.Instance.PlaySFX("go");

        Time.timeScale = 1f;

        foreach (TractorVibration tv in FindObjectsOfType<TractorVibration>(true))
            tv.StartEngine();

        LaneController laneCtrl = playerObject.GetComponent<LaneController>();
        if (laneCtrl != null) laneCtrl.enabled = true;

        if (bgHouses != null)
            StartBGHousesGrow();

        gameStarted = true;

        // Hide countdown text after a short moment
        yield return new WaitForSecondsRealtime(0.8f);
        if (countdownText != null) countdownText.gameObject.SetActive(false);
    }

    void StartBGHousesGrow()
    {
        if (bgHouses == null) return;
        if (bgHousesCoroutine != null)
            StopCoroutine(bgHousesCoroutine);
        bgHousesCoroutine = StartCoroutine(BGHousesGrowCoroutine());
    }

    IEnumerator BGHousesGrowCoroutine()
    {
        bgHouses.localScale = bgHousesStartScale;

        float elapsed = 0f;

        float effectiveDuration = bgHousesDuration / Mathf.Max(bgHousesSpeed, 0.01f);

        while (elapsed < effectiveDuration)
        {
            elapsed += Time.unscaledDeltaTime;  // unscaled so timeScale=0 won't freeze it
            float t = Mathf.Clamp01(elapsed / effectiveDuration);
            bgHouses.localScale = Vector3.Lerp(bgHousesStartScale, bgHousesEndScale, Mathf.SmoothStep(0f, 1f, t));
            yield return null;
        }

        bgHouses.localScale = bgHousesEndScale;
        bgHousesCoroutine = null;

        Debug.Log("BGHouses grow complete.");
    }

    void Update()
    {
        if (isGameOver) return;
        if (!gameStarted) return;

        UpdatePositionText();

        if (allSpawned) return;

        if (!firstCarSpawned)
        {
            firstSpawnTimer += Time.deltaTime;
            if (firstSpawnTimer >= firstCarDelay)
            {
                firstCarSpawned = true;
                SpawnNextCar();
            }
        }
        else
        {
            spawnTimer += Time.deltaTime;
            if (spawnTimer >= spawnInterval)
            {
                spawnTimer = 0f;
                SpawnNextCar();
            }
        }
    }

    public void OnCarPassedPlayer()
    {
        carsPassed++;
    }

    void UpdatePositionText()
    {
        if (positionText == null) return;
        int playerPosition = Mathf.Max(1, totalCarsToSpawn - carsPassed);
        positionText.text = playerPosition + "/" + totalCarsToSpawn;
    }

    void SpawnNextCar()
    {
        if (spawnedCount >= totalCarsToSpawn)
        {
            allSpawned = true;
            return;
        }

        int freeSpawner = GetFreeSpawner();
        if (freeSpawner == -1) return;

        int freeCar = GetNextFreeCar();
        if (freeCar == -1) return;

        carSpawnerIndex[freeCar] = freeSpawner;

        RectTransform start = startSpawners[freeSpawner];
        RectTransform end = endSpawners[freeSpawner];
        GameObject car = cars[freeCar];

        car.SetActive(true);
        spawnedCount++;

        ObstacleMover mover = car.GetComponent<ObstacleMover>();
        mover.startSpawner = start;
        mover.endSpawner = end;
        mover.playerObject = playerObject;
        mover.playerColliders = playerColliders;
        mover.gameOverPanel = gameOverPanel;
        mover.crashEffect = crashEffect;
        mover.onCarFinished = () => OnCarFinished(freeCar);
        mover.onCarPassedPlayer = () => OnCarPassedPlayer();
        mover.onGameOver = () => OnGameOver();
        mover.ResetCar();
    }

    int GetNextFreeCar()
    {
        int total = cars.Length;
        for (int i = 0; i < total; i++)
        {
            int index = (nextCarIndex + i) % total;
            if (!cars[index].activeSelf)
            {
                nextCarIndex = (index + 1) % total;
                return index;
            }
        }
        return -1;
    }

    void OnCarFinished(int carIndex)
    {
        carSpawnerIndex[carIndex] = -1;
        cars[carIndex].SetActive(false);

        finishedCount++;

        Debug.Log("Car finished. finishedCount: " + finishedCount + " / " + totalCarsToSpawn + " allSpawned: " + allSpawned);

        if (allSpawned && finishedCount >= totalCarsToSpawn && !winTriggered)
        {
            winTriggered = true;
            Debug.Log("Triggering win sequence!");
            StartWinSequence();
        }
    }

    void OnGameOver()
    {
        // Stop BGHouses grow
        if (bgHousesCoroutine != null)
        {
            StopCoroutine(bgHousesCoroutine);
            bgHousesCoroutine = null;
        }
    }

    public void SetGameOver()
    {
        isGameOver = true;
    }

    void StartWinSequence()
    {
        Debug.Log("StartWinSequence called!");

        foreach (GameObject line in roadLines)
        {
            if (line != null)
                line.SetActive(false);
        }

        foreach (TireLineMover tire in FindObjectsOfType<TireLineMover>(true))
            tire.enabled = false;

        LaneController lane = playerObject.GetComponent<LaneController>();
        if (lane != null)
            lane.enabled = false;

        if (finishLineMover != null)
        {
            finishLineMover.SetActive(true);

            FinishLineMover flMover = finishLineMover.GetComponent<FinishLineMover>();
            if (flMover != null)
            {
                flMover.ResetFinishLine();

                flMover.onFinishLineReachedEnd = () =>
                {
                    flMover.onFinishLineReachedEnd = null;

                    if (finishLineObject != null)
                        finishLineObject.SetActive(true);

                    StartCoroutine(MovePlayerToFinish());
                };
            }
            else
            {
                if (finishLineObject != null)
                    finishLineObject.SetActive(true);
                StartCoroutine(MovePlayerToFinish());
            }
        }
        else
        {
            if (finishLineObject != null)
                finishLineObject.SetActive(true);
            StartCoroutine(MovePlayerToFinish());
        }
    }

    IEnumerator MovePlayerToFinish()
    {
        yield return null;

        SoundManager.Instance.StopSFXLoop();

        foreach (TreeMover tree in FindObjectsOfType<TreeMover>(true))
            tree.PauseTree();

        Vector3 startPos = playerObject.position;
        Vector3 targetPos = finishLineTarget.position;
        Vector3 startScale = playerObject.localScale;

        float progress = 0f;

        while (progress < 1f)
        {
            progress += Time.deltaTime * moveToFinishSpeed;
            progress = Mathf.Clamp01(progress);

            playerObject.position = Vector3.Lerp(startPos, targetPos, progress);
            playerObject.localScale = Vector3.Lerp(startScale, playerEndScale, progress);

            yield return null;
        }

        Debug.Log("Player reached finish line!");
        SoundManager.Instance.PlaySFX("tada");

        yield return new WaitForSeconds(youWinDelay);

        if (youWinPanel != null)
        {
            youWinPanel.SetActive(true);
            SoundManager.Instance.PlaySFX("Victory");
        }

        Debug.Log("Win sequence done!");
    }

    int GetFreeSpawner()
    {
        bool[] busySpawners = new bool[startSpawners.Length];
        for (int i = 0; i < carSpawnerIndex.Length; i++)
        {
            if (carSpawnerIndex[i] != -1)
                busySpawners[carSpawnerIndex[i]] = true;
        }

        int attempts = 0;
        while (attempts < 10)
        {
            int random = Random.Range(0, startSpawners.Length);
            if (!busySpawners[random])
                return random;
            attempts++;
        }
        return -1;
    }
}