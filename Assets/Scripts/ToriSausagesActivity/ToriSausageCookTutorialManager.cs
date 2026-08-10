using System.Collections;
using UnityEngine;

namespace ToriSausages
{
    /// <summary>
    /// Sequential cooking tutorial. Triggered by pressing the OK button.
    ///
    /// STOPPING THE TUTORIAL
    ///   The tutorial does NOT auto-stop on any touch/drag.
    ///   Call HideTutorial() manually from your game scripts if you want to
    ///   cancel it (e.g. player does something completely wrong).
    ///   It ends automatically after all phases complete.
    ///
    /// PHASE ORDER
    /// ─────────────────────────────────────────────────────────────────────
    /// Phase 1 — Food → Grill
    ///   Hand: foodLocation → grillLocation
    ///   Advance: NotifyFoodPlacedOnGrill()
    ///
    /// WaitingForCook — hand hidden while food cooks
    ///   Cooked → NotifyCookingDone()   → Phase 2
    ///   Burned → NotifyFoodBurned()    → Phase 2B
    ///
    /// Phase 2 — Grill → Plate 2
    ///   Hand: grillLocation → plate2DropLocation
    ///   Advance: NotifyCookedSausagePlacedOnPlate()
    ///
    /// Phase 2B — Grill → Bin  (burned path — tutorial ends after)
    ///   Hand: grillLocation → binLocation
    ///   Advance: NotifyBurnedSausageDiscarded()
    ///
    /// Phase 3 — Condiment → Plate 2
    ///   Hand: ketchupLocation or mayoLocation (random) → plate2DropLocation
    ///   Advance: NotifyCondimentApplied()
    ///
    /// Phase 4 — Plate 2 → Mouth
    ///   Hand: plate2DropLocation → mouthLocations[N]
    ///   Advance: NotifyFoodDeliveredToMouth()
    /// ─────────────────────────────────────────────────────────────────────
    ///
    /// WIRING
    ///   OK button OnClick()                                  → StartTutorial()
    ///   ControllerStove.TryPlaceGrill()   success path       → NotifyFoodPlacedOnGrill()
    ///   CookerGrill.CookTimer()           before Destroy     → NotifyCookingDone()
    ///   BurnerSausage.BurnRoutine()       after db.enabled   → NotifyFoodBurned()
    ///   ControllerDustbin.ReceiveBurnedSausage() before Destroy → NotifyBurnedSausageDiscarded()
    ///   ManagerPlate.PlaceGrill()         success path       → NotifyCookedSausagePlacedOnPlate()
    ///   CondimentBottle end of ApplyCondimentWithAnimation() → NotifyCondimentApplied()
    ///   DraggableSausage DropOnMouth()    accepted path      → NotifyFoodDeliveredToMouth()
    /// </summary>
    public class ToriSausageCookTutorialManager : MonoBehaviour
    {
        public static ToriSausageCookTutorialManager Instance;

        // ── Hand ─────────────────────────────────────────────────────────────────
        [Header("Hand")]
        public RectTransform handRect;
        public RectTransform handTipObject;

        // ── Locations ─────────────────────────────────────────────────────────────
        [Header("Locations")]
        public Transform foodLocation;
        public Transform grillLocation;
        public Transform plate2DropLocation;
        public Transform ketchupLocation;
        public Transform mayoLocation;
        public Transform binLocation;

        [Header("Mouth Locations (3 customer slots)")]
        public Transform[] mouthLocations = new Transform[3];

        [Header("Plate 2 Reference")]
        public ControllerPlate plate2;

        [Header("Customer References")]
        public ControllerCustomer[] customers = new ControllerCustomer[3];

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
        private bool tutorialActive = false;
        private bool tutorialStarted = false;

        private enum TutorialPhase
        {
            FoodToGrill,
            WaitingForCook,
            GrillToPlate,
            GrillToBin,
            CondimentToPlate,
            PlateToMouth,
            Done
        }
        private TutorialPhase _currentPhase = TutorialPhase.FoodToGrill;
        private bool _phaseComplete = false;
        private bool _foodWasBurned = false;

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
            if (tutorialStarted) return;
            tutorialStarted = true;
            tutorialActive = true;
            _currentPhase = TutorialPhase.FoodToGrill;
            StartCoroutine(TutorialSequence());
        }

        public void NotifyFoodPlacedOnGrill()
        {
            Debug.Log($"[Tutorial] NotifyFoodPlacedOnGrill — phase={_currentPhase}");
            if (!tutorialActive || _currentPhase != TutorialPhase.FoodToGrill) return;
            _phaseComplete = true;
        }

        public void NotifyCookingDone()
        {
            Debug.Log($"[Tutorial] NotifyCookingDone — phase={_currentPhase}");
            if (!tutorialActive || _currentPhase != TutorialPhase.WaitingForCook) return;
            _foodWasBurned = false;
            _phaseComplete = true;
        }

        public void NotifyFoodBurned()
        {
            Debug.Log($"[Tutorial] NotifyFoodBurned — phase={_currentPhase}");
            if (!tutorialActive) return;
            // Food can burn during WaitingForCook (cook ends then burns fast)
            // OR during GrillToPlate (player ignores cooked food until it burns).
            // Both cases must redirect the tutorial to the bin path.
            if (_currentPhase != TutorialPhase.WaitingForCook &&
                _currentPhase != TutorialPhase.GrillToPlate) return;
            _foodWasBurned = true;
            _phaseComplete = true;
        }

        public void NotifyBurnedSausageDiscarded()
        {
            Debug.Log($"[Tutorial] NotifyBurnedSausageDiscarded — phase={_currentPhase}");
            if (!tutorialActive || _currentPhase != TutorialPhase.GrillToBin) return;
            _phaseComplete = true;
        }

        public void NotifyCookedSausagePlacedOnPlate()
        {
            Debug.Log($"[Tutorial] NotifyCookedSausagePlacedOnPlate — phase={_currentPhase}");
            if (!tutorialActive || _currentPhase != TutorialPhase.GrillToPlate) return;
            _phaseComplete = true;
        }

        public void NotifyCondimentApplied()
        {
            Debug.Log($"[Tutorial] NotifyCondimentApplied — phase={_currentPhase}");
            if (!tutorialActive || _currentPhase != TutorialPhase.CondimentToPlate) return;
            _phaseComplete = true;
        }

        public void NotifyFoodDeliveredToMouth()
        {
            Debug.Log($"[Tutorial] NotifyFoodDeliveredToMouth — phase={_currentPhase}");
            if (!tutorialActive || _currentPhase != TutorialPhase.PlateToMouth) return;
            _phaseComplete = true;
        }

        public void HideTutorial()
        {
            Debug.Log("[Tutorial] HideTutorial called.");
            tutorialActive = false;
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
            // Phases 1 → WaitCook → 2A repeat until food lands on plate.
            // If food burns at any point → Phase 2B (grill→bin) then restart Phase 1.
            while (tutorialActive)
            {
                // ── Phase 1: Food → Grill ──────────────────────────────────────────
                Debug.Log("[Tutorial] Phase 1: Food → Grill");
                _currentPhase = TutorialPhase.FoodToGrill;
                _phaseComplete = false;
                yield return StartCoroutine(RunPhaseLoop(foodLocation, grillLocation));
                if (!tutorialActive) yield break;

                // ── WaitingForCook — hand hidden ───────────────────────────────────
                Debug.Log("[Tutorial] WaitingForCook — hand hidden.");
                _currentPhase = TutorialPhase.WaitingForCook;
                _phaseComplete = false;
                _foodWasBurned = false;
                yield return new WaitUntil(() => _phaseComplete || !tutorialActive);
                if (!tutorialActive) yield break;

                yield return new WaitForSeconds(0.3f);
                if (!tutorialActive) yield break;

                if (_foodWasBurned)
                {
                    // ── Phase 2B: Grill → Bin → restart from Phase 1 ──────────────
                    Debug.Log("[Tutorial] Phase 2B: Grill → Bin → restarting.");
                    _currentPhase = TutorialPhase.GrillToBin;
                    _phaseComplete = false;
                    yield return StartCoroutine(RunPhaseLoop(grillLocation, binLocation));
                    if (!tutorialActive) yield break;
                    yield return new WaitForSeconds(0.5f);
                    continue;   // ← back to Phase 1
                }

                // ── Phase 2A: Grill → Plate 2 ─────────────────────────────────────
                Debug.Log("[Tutorial] Phase 2A: Grill → Plate 2");
                _currentPhase = TutorialPhase.GrillToPlate;
                _phaseComplete = false;
                _foodWasBurned = false;   // food can still burn if player is slow here
                yield return StartCoroutine(RunPhaseLoop(grillLocation, plate2DropLocation));
                if (!tutorialActive) yield break;

                if (_foodWasBurned)
                {
                    // Burned while player was slow — Grill → Bin → restart Phase 1
                    Debug.Log("[Tutorial] Burned during Phase 2A: Grill → Bin → restarting.");
                    _currentPhase = TutorialPhase.GrillToBin;
                    _phaseComplete = false;
                    yield return StartCoroutine(RunPhaseLoop(grillLocation, binLocation));
                    if (!tutorialActive) yield break;
                    yield return new WaitForSeconds(0.5f);
                    continue;   // ← back to Phase 1
                }

                // Food successfully placed on plate — exit cooking loop
                break;
            }

            if (!tutorialActive) yield break;

            yield return new WaitForSeconds(0.3f);
            if (!tutorialActive) yield break;

            // ── Phase 3: Condiment → Plate 2 ──────────────────────────────────────
            Debug.Log("[Tutorial] Phase 3: Condiment → Plate 2");
            _currentPhase = TutorialPhase.CondimentToPlate;
            _phaseComplete = false;
            Transform condimentSrc = (Random.value < 0.5f) ? ketchupLocation : mayoLocation;
            yield return StartCoroutine(RunPhaseLoop(condimentSrc, plate2DropLocation));
            if (!tutorialActive) yield break;

            yield return new WaitForSeconds(0.3f);
            if (!tutorialActive) yield break;

            // ── Phase 4: Plate 2 → Mouth ──────────────────────────────────────────
            Debug.Log("[Tutorial] Phase 4: Plate 2 → Mouth");
            _currentPhase = TutorialPhase.PlateToMouth;
            _phaseComplete = false;
            Transform mouthTarget = ResolveMouthLocation();
            Debug.Log($"[Tutorial] Mouth target: {(mouthTarget != null ? mouthTarget.name : "NULL")}");
            yield return StartCoroutine(RunPhaseLoop(plate2DropLocation, mouthTarget));
            if (!tutorialActive) yield break;

            Debug.Log("[Tutorial] All phases complete.");
            _currentPhase = TutorialPhase.Done;
            HideTutorial();
        }

        // =========================================================================
        // PER-PHASE LOOP
        // Repeats bounce→lerp until _phaseComplete is set by a Notify* call.
        // =========================================================================

        IEnumerator RunPhaseLoop(Transform source, Transform destination)
        {
            while (!_phaseComplete && tutorialActive)
            {
                if (source == null || destination == null)
                {
                    Debug.LogWarning("[Tutorial] RunPhaseLoop: NULL source or destination!");
                    yield return new WaitForSeconds(phaseRepeatDelay);
                    continue;
                }

                PlaceTipAt(source);
                handRect.gameObject.SetActive(true);

                StartBounce(_handBaseAnchored);
                yield return new WaitForSeconds(bounceHoldTime);
                StopBounce();

                if (_phaseComplete || !tutorialActive) break;

                yield return StartCoroutine(LerpTipTo(destination));

                if (_phaseComplete || !tutorialActive) break;

                yield return new WaitForSeconds(holdAtDestination);

                handRect.gameObject.SetActive(false);

                if (_phaseComplete || !tutorialActive) break;

                yield return new WaitForSeconds(phaseRepeatDelay);
            }

            StopBounce();
            if (handRect != null) handRect.gameObject.SetActive(false);
        }

        // =========================================================================
        // MOUTH RESOLVER
        // =========================================================================

        Transform ResolveMouthLocation()
        {
            if (mouthLocations == null || mouthLocations.Length == 0) return null;
            Transform fallback = mouthLocations[0];

            if (plate2 == null || plate2.ownerCustomer == null)
            {
                Debug.LogWarning("[Tutorial] plate2 or ownerCustomer null — fallback mouth[0].");
                return fallback;
            }

            ControllerCustomer owner = plate2.ownerCustomer;

            if (customers != null)
            {
                for (int i = 0; i < customers.Length; i++)
                {
                    if (customers[i] == owner && i < mouthLocations.Length && mouthLocations[i] != null)
                        return mouthLocations[i];
                }
            }

            if (owner.mouthPoint != null) return owner.mouthPoint;

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
                if (!tutorialActive) yield break;
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
}