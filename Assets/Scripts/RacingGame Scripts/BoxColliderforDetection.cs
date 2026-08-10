using UnityEngine;

public class BoxColliderforDetection : MonoBehaviour
{
    private BoxCollider2D detectionCollider;
    private Transform playerObject;
    private CarSpawnManager spawnManager;

    void Start()
    {
        detectionCollider = GetComponent<BoxCollider2D>();

        // Hierarchy: BoxColliderforDetection -> Car -> PlayerObject
        playerObject = transform.parent.parent;

        spawnManager = FindObjectOfType<CarSpawnManager>();

        if (detectionCollider == null)
            Debug.LogWarning("BoxColliderforDetection: No BoxCollider2D found on this object!");
        if (playerObject == null)
            Debug.LogWarning("BoxColliderforDetection: Could not find PlayerObject (expected 2 levels up)!");
        if (spawnManager == null)
            Debug.LogWarning("BoxColliderforDetection: CarSpawnManager not found in scene!");
    }

    void Update()
    {
        if (detectionCollider == null || spawnManager == null || playerObject == null) return;

        foreach (GameObject car in spawnManager.cars)
        {
            if (car == null || !car.activeSelf) continue;

            // Already a child of PlayerObject — skip
            if (car.transform.parent == playerObject) continue;

            BoxCollider2D[] carCols = car.GetComponentsInChildren<BoxCollider2D>(true);
            foreach (BoxCollider2D carCol in carCols)
            {
                if (carCol == null) continue;

                if (detectionCollider.bounds.Intersects(carCol.bounds))
                {
                    // Tell the car it's caught so it skips game over checks
                    ObstacleMover mover = car.GetComponent<ObstacleMover>();
                    if (mover != null) mover.SetCaught();

                    // Snap as child of PlayerObject
                    car.transform.SetParent(playerObject);
                    break;
                }
            }
        }
    }
}
