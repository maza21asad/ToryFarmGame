using System.Collections;
using UnityEngine;

/// <summary>
/// Sequential cooking tutorial for PorkThePig Grilling Activity.
/// Triggered by pressing the OK button (wire to StartTutorial()).
///
/// STOPPING THE TUTORIAL
///   The tutorial does NOT auto-stop on any touch/drag.
///   Call HideTutorial() manually from your game scripts to cancel it.
///   It ends automatically after all phases complete.
///
/// PHASE ORDER
/// ─────────────────────────────────────────────────────────────────────
/// Phase 1 — Food → Grill
///   Hand: foodLocation1 OR foodLocation2 (random each loop) → grillLocation
///   Advance: NotifyFoodPlacedOnGrill()
///
/// WaitingForCook — hand hidden while food cooks
///   Cooked → NotifyCookingDone()   → Phase 2
///   Burned → NotifyFoodBurned()    → Phase 2B
///
/// Phase 2 — Grill → Plate 2
///   Hand: grillLocation → plate2DropLocation
///   Advance: NotifyCookedGrillPlacedOnPlate()
///
/// Phase 2B — Grill → Bin  (burned path — restarts from Phase 1)
///   Hand: grillLocation → binLocation
///   Advance: NotifyBurnedGrillDiscarded()
///
/// Phase 3 — Plate 2 → Mouth
///   Hand: plate2DropLocation → mouthLocations[N]
///   Advance: NotifyFoodDeliveredToMouth()
/// ─────────────────────────────────────────────────────────────────────
///
/// WIRING
///   OK button OnClick()                                    → StartTutorial()
///   StoveController.TryPlaceGrill()     success path       → NotifyFoodPlacedOnGrill()
///   GrillCooker.CookTimer()             before Destroy     → NotifyCookingDone()
///   BurnerGrill.BurnRoutine()           after db.enabled   → NotifyFoodBurned()
///   DustbinController.ReceiveBurnedGrill() before Destroy  → NotifyBurnedGrillDiscarded()
///   PlateManager.PlaceGrill()           success path       → NotifyCookedGrillPlacedOnPlate()
///   DraggableGrill.DropOnMouth()        accepted path      → NotifyFoodDeliveredToMouth()
/// </summary>
public class PigGrillTutorialManager : MonoBehaviour
{
    public static PigGrillTutorialManager Instance;

    // ── Hand ──────────────────────────────────────────────────────────────────
    [Header("Hand")]
    public RectTransform handRect;
    public RectTransform handTipObject;

    // ── Locations ─────────────────────────────────────────────────────────────
    [Header("Food Locations (2 — hand picks randomly each cook loop)")]
    [Tooltip("First food spawn/tray location the hand can point to.")]
    public Transform foodLocation1;
    [Tooltip("Second food spawn/tray location the hand can point to.")]
    public Transform foodLocation2;

    [Header("Grill & Plate Locations")]
    [Tooltip("The stove grill slot the hand drags food toward.")]
    public Transform grillLocation;
    [Tooltip("The plate drop position the hand points to after cooking.")]
    public Transform plate2DropLocation;
    [Tooltip("The dustbin location shown when food burns.")]
    public Transform binLocation;

    [Header("Mouth Locations (3 customer slots)")]
    public Transform[] mouthLocations = new Transform[3];

    [Header("Plate 2 Reference")]
    public PlateController plate2;

    [Header("Customer References")]
    public CustomerController[] customers = new CustomerController[3];

    // ── Timing ────────────────────────────────────────────────────────────────
    [Header("Timing")]
    public float startDelay = 0.5f;
    public float bounceHoldTime = 0.8f;
    public float lerpDuration = 1.2f;
    public float holdAtDestination = 0.6f;
    public float phaseRepeatDelay = 1.5f;

    // ── Bounce ────────────────────────────────────────────────────────────────
    [Header("Bounce At Source")]
    public float bounceMagnitude = 12f;
    public float bounceSpeed = 4.5f;

    // ── Internal ──────────────────────────────────────────────────────────────
    private bool _tutorialActive = false;
    private bool _tutorialStarted = false;

    private enum TutorialPhase
    {
        FoodToGrill,
        WaitingForCook,
        GrillToPlate,
        GrillToBin,
        PlateToMouth,
        Done
    }
    private TutorialPhase _currentPhase = TutorialPhase.FoodToGrill;
    private bool _phaseComplete = false;
    private bool _foodWasBurned = false;

    // The food location chosen randomly at the start of each cooking loop.
    private Transform _chosenFoodLocation;

    private Coroutine _bounceCoroutine;
    private Vector2 _handBaseAnchored;

    // =========================================================================
    void Awake()
    {
        if (Instance == null) Instance = this;
        else { Destroy(gameObject); return; }
    }

    void Start()
    {
        if (handRect != null) handRect.gameObject.SetActive(false);
    }

    // =========================================================================
    // PUBLIC API
    // =========================================================================

    public void StartTutorial()
    {
        if (_tutorialStarted) return;
        _tutorialStarted = true;
        _tutorialActive = true;
        _currentPhase = TutorialPhase.FoodToGrill;
        StartCoroutine(TutorialSequence());
    }

    /// <summary>
    /// Called by StoveController.TryPlaceGrill() on the success path.
    /// </summary>
    public void NotifyFoodPlacedOnGrill()
    {
        Debug.Log($"[PigGrillTutorial] NotifyFoodPlacedOnGrill — phase={_currentPhase}");
        if (!_tutorialActive || _currentPhase != TutorialPhase.FoodToGrill) return;
        _phaseComplete = true;
    }

    /// <summary>
    /// Called by GrillCooker.CookTimer() just before it destroys the raw grill.
    /// </summary>
    public void NotifyCookingDone()
    {
        Debug.Log($"[PigGrillTutorial] NotifyCookingDone — phase={_currentPhase}");
        if (!_tutorialActive || _currentPhase != TutorialPhase.WaitingForCook) return;
        _foodWasBurned = false;
        _phaseComplete = true;
    }

    /// <summary>
    /// Called by BurnerGrill.BurnRoutine() after DraggableBurnedGrill is enabled.
    /// Also valid during GrillToPlate if the player is too slow.
    /// </summary>
    public void NotifyFoodBurned()
    {
        Debug.Log($"[PigGrillTutorial] NotifyFoodBurned — phase={_currentPhase}");
        if (!_tutorialActive) return;
        if (_currentPhase != TutorialPhase.WaitingForCook &&
            _currentPhase != TutorialPhase.GrillToPlate) return;
        _foodWasBurned = true;
        _phaseComplete = true;
    }

    /// <summary>
    /// Called by DustbinController.ReceiveBurnedGrill() before the object is destroyed.
    /// </summary>
    public void NotifyBurnedGrillDiscarded()
    {
        Debug.Log($"[PigGrillTutorial] NotifyBurnedGrillDiscarded — phase={_currentPhase}");
        if (!_tutorialActive || _currentPhase != TutorialPhase.GrillToBin) return;
        _phaseComplete = true;
    }

    /// <summary>
    /// Called by PlateManager.PlaceGrill() (or PlateController.TryReceiveGrill()) on success.
    /// </summary>
    public void NotifyCookedGrillPlacedOnPlate()
    {
        Debug.Log($"[PigGrillTutorial] NotifyCookedGrillPlacedOnPlate — phase={_currentPhase}");
        if (!_tutorialActive || _currentPhase != TutorialPhase.GrillToPlate) return;
        _phaseComplete = true;
    }

    /// <summary>
    /// Called by DraggableGrill.DropOnMouth() on the accepted path.
    /// </summary>
    public void NotifyFoodDeliveredToMouth()
    {
        Debug.Log($"[PigGrillTutorial] NotifyFoodDeliveredToMouth — phase={_currentPhase}");
        if (!_tutorialActive || _currentPhase != TutorialPhase.PlateToMouth) return;
        _phaseComplete = true;
    }

    /// <summary>
    /// Stops the tutorial immediately (e.g. player does something wrong).
    /// Can also be called from external scripts at any time.
    /// </summary>
    public void HideTutorial()
    {
        Debug.Log("[PigGrillTutorial] HideTutorial called.");
        _tutorialActive = false;
        StopAllCoroutines();
        if (handRect != null) handRect.gameObject.SetActive(false);
    }

    // =========================================================================
    // MAIN SEQUENCE
    // =========================================================================

    IEnumerator TutorialSequence()
    {
        yield return new WaitForSeconds(startDelay);

        // ── Cooking loop ───────────────────────────────────────────────────────
        // Phase 1 → WaitCook → Phase 2A repeat until food lands on plate.
        // If food burns at any point → Phase 2B (grill→bin) → restart Phase 1.
        while (_tutorialActive)
        {
            // Pick a random food location for this cooking loop iteration.
            _chosenFoodLocation = PickRandomFoodLocation();

            // ── Phase 1: Food → Grill ──────────────────────────────────────────
            Debug.Log("[PigGrillTutorial] Phase 1: Food → Grill");
            _currentPhase = TutorialPhase.FoodToGrill;
            _phaseComplete = false;
            yield return StartCoroutine(RunPhaseLoop(_chosenFoodLocation, grillLocation));
            if (!_tutorialActive) yield break;

            // ── WaitingForCook — hand hidden ───────────────────────────────────
            Debug.Log("[PigGrillTutorial] WaitingForCook — hand hidden.");
            _currentPhase = TutorialPhase.WaitingForCook;
            _phaseComplete = false;
            _foodWasBurned = false;

            // Hide hand while food is cooking.
            if (handRect != null) handRect.gameObject.SetActive(false);

            yield return new WaitUntil(() => _phaseComplete || !_tutorialActive);
            if (!_tutorialActive) yield break;

            yield return new WaitForSeconds(0.3f);
            if (!_tutorialActive) yield break;

            if (_foodWasBurned)
            {
                // ── Phase 2B: Grill → Bin → restart from Phase 1 ──────────────
                Debug.Log("[PigGrillTutorial] Phase 2B: Grill → Bin → restarting.");
                _currentPhase = TutorialPhase.GrillToBin;
                _phaseComplete = false;
                yield return StartCoroutine(RunPhaseLoop(grillLocation, binLocation));
                if (!_tutorialActive) yield break;
                yield return new WaitForSeconds(0.5f);
                continue; // ← back to Phase 1 (new random food location)
            }

            // ── Phase 2A: Grill → Plate 2 ─────────────────────────────────────
            Debug.Log("[PigGrillTutorial] Phase 2A: Grill → Plate 2");
            _currentPhase = TutorialPhase.GrillToPlate;
            _phaseComplete = false;
            _foodWasBurned = false; // food can still burn if player is slow
            yield return StartCoroutine(RunPhaseLoop(grillLocation, plate2DropLocation));
            if (!_tutorialActive) yield break;

            if (_foodWasBurned)
            {
                // Burned while player was slow during Phase 2A → Bin → restart
                Debug.Log("[PigGrillTutorial] Burned during Phase 2A: Grill → Bin → restarting.");
                _currentPhase = TutorialPhase.GrillToBin;
                _phaseComplete = false;
                yield return StartCoroutine(RunPhaseLoop(grillLocation, binLocation));
                if (!_tutorialActive) yield break;
                yield return new WaitForSeconds(0.5f);
                continue; // ← back to Phase 1
            }

            // Food successfully placed on plate — exit cooking loop.
            break;
        }

        if (!_tutorialActive) yield break;

        yield return new WaitForSeconds(0.3f);
        if (!_tutorialActive) yield break;

        // ── Phase 3: Plate 2 → Mouth ──────────────────────────────────────────
        Debug.Log("[PigGrillTutorial] Phase 3: Plate 2 → Mouth");
        _currentPhase = TutorialPhase.PlateToMouth;
        _phaseComplete = false;
        Transform mouthTarget = ResolveMouthLocation();
        Debug.Log($"[PigGrillTutorial] Mouth target: {(mouthTarget != null ? mouthTarget.name : "NULL")}");
        yield return StartCoroutine(RunPhaseLoop(plate2DropLocation, mouthTarget));
        if (!_tutorialActive) yield break;

        Debug.Log("[PigGrillTutorial] All phases complete.");
        _currentPhase = TutorialPhase.Done;
        HideTutorial();
    }

    // =========================================================================
    // PER-PHASE LOOP
    // Repeats bounce → lerp until _phaseComplete is set by a Notify* call.
    // =========================================================================

    IEnumerator RunPhaseLoop(Transform source, Transform destination)
    {
        while (!_phaseComplete && _tutorialActive)
        {
            if (source == null || destination == null)
            {
                Debug.LogWarning("[PigGrillTutorial] RunPhaseLoop: NULL source or destination!");
                yield return new WaitForSeconds(phaseRepeatDelay);
                continue;
            }

            PlaceTipAt(source);
            handRect.gameObject.SetActive(true);

            StartBounce(_handBaseAnchored);
            yield return new WaitForSeconds(bounceHoldTime);
            StopBounce();

            if (_phaseComplete || !_tutorialActive) break;

            yield return StartCoroutine(LerpTipTo(destination));

            if (_phaseComplete || !_tutorialActive) break;

            yield return new WaitForSeconds(holdAtDestination);

            handRect.gameObject.SetActive(false);

            if (_phaseComplete || !_tutorialActive) break;

            yield return new WaitForSeconds(phaseRepeatDelay);
        }

        StopBounce();
        if (handRect != null) handRect.gameObject.SetActive(false);
    }

    // =========================================================================
    // RANDOM FOOD LOCATION PICKER
    // =========================================================================

    /// <summary>
    /// Picks either foodLocation1 or foodLocation2 at random.
    /// Falls back gracefully if one (or both) are not assigned.
    /// </summary>
    private Transform PickRandomFoodLocation()
    {
        bool has1 = foodLocation1 != null;
        bool has2 = foodLocation2 != null;

        if (has1 && has2)
            return (Random.value < 0.5f) ? foodLocation1 : foodLocation2;

        if (has1) return foodLocation1;
        if (has2) return foodLocation2;

        Debug.LogWarning("[PigGrillTutorial] Neither foodLocation1 nor foodLocation2 is assigned!");
        return null;
    }

    // =========================================================================
    // MOUTH RESOLVER
    // Matches plate2's ownerCustomer to the correct mouthLocations entry.
    // =========================================================================

    Transform ResolveMouthLocation()
    {
        if (mouthLocations == null || mouthLocations.Length == 0) return null;
        Transform fallback = mouthLocations[0];

        if (plate2 == null)
        {
            Debug.LogWarning("[PigGrillTutorial] plate2 is null — fallback mouth[0].");
            return fallback;
        }

        // PlateController does not hold a customer reference.
        // CustomerController.owningPlate points back to its plate, so we search
        // the customers array for whoever currently owns plate2.
        if (customers != null)
        {
            for (int i = 0; i < customers.Length; i++)
            {
                if (customers[i] == null) continue;
                if (customers[i].owningPlate != plate2) continue;

                // Prefer the matching mouthLocations index first.
                if (i < mouthLocations.Length && mouthLocations[i] != null)
                    return mouthLocations[i];

                // Fall back to the customer's own mouthPoint.
                if (customers[i].mouthPoint != null)
                    return customers[i].mouthPoint;
            }
        }

        Debug.LogWarning("[PigGrillTutorial] No customer found owning plate2 — fallback mouth[0].");
        return fallback;
    }

    // =========================================================================
    // HELPERS
    // =========================================================================

    void PlaceTipAt(Transform target)
    {
        if (handTipObject != null)
        {
            handRect.position = target.position;
            Vector3 tipOffset = handTipObject.position - target.position;
            handRect.position -= tipOffset;
        }
        else
        {
            handRect.position = target.position;
        }
        _handBaseAnchored = handRect.anchoredPosition;
    }

    IEnumerator LerpTipTo(Transform destination)
    {
        Vector3 startHandPos = handRect.position;

        Vector3 endHandPos;
        if (handTipObject != null)
        {
            handRect.position = destination.position;
            Vector3 tipOffset = handTipObject.position - destination.position;
            endHandPos = destination.position - tipOffset;
            handRect.position = startHandPos;
        }
        else
        {
            endHandPos = destination.position;
        }

        float elapsed = 0f;
        while (elapsed < lerpDuration)
        {
            if (!_tutorialActive) yield break;
            elapsed += Time.deltaTime;
            float t = Mathf.SmoothStep(0f, 1f, elapsed / lerpDuration);
            handRect.position = Vector3.Lerp(startHandPos, endHandPos, t);
            yield return null;
        }

        handRect.position = endHandPos;
        _handBaseAnchored = handRect.anchoredPosition;
    }

    void StartBounce(Vector2 basePos)
    {
        StopBounce();
        _bounceCoroutine = StartCoroutine(BounceLoop(basePos));
    }

    void StopBounce()
    {
        if (_bounceCoroutine != null)
        {
            StopCoroutine(_bounceCoroutine);
            _bounceCoroutine = null;
        }
    }

    IEnumerator BounceLoop(Vector2 basePos)
    {
        while (true)
        {
            float y = Mathf.Sin(Time.unscaledTime * bounceSpeed) * bounceMagnitude;
            if (handRect != null)
                handRect.anchoredPosition = basePos + new Vector2(0f, y);
            yield return null;
        }
    }
}