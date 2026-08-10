using UnityEngine;
using UnityEngine.UI;
using System.Collections;

namespace ToriSausages
{
    /// <summary>
    /// Attach to the COOKED sausage prefab (same GameObject as DraggableSausage).
    ///
    /// FLOW
    ///   OnEnable → burn countdown starts (burnDuration seconds)
    ///   If the sausage is picked up (DraggableSausage.OnBeginDrag) → countdown paused
    ///   If put back on plate → countdown resumes
    ///   Countdown reaches 0 → burn animation plays as overlay → sausage becomes burned
    ///   DraggableSausage disabled → DraggableBurned enabled → player drags to dustbin
    ///
    /// PREFAB SETUP
    ///   On the cooked sausage GameObject add:
    ///     BurnerSausage   (this script)
    ///     DraggableBurned (disabled by default — enabled automatically when burned)
    /// </summary>
    public class BurnerSausage : MonoBehaviour
    {
        [Header("Burn Timer")]
        [Tooltip("Seconds after the sausage is cooked before it starts burning.")]
        public float burnDuration = 5f;

        [Header("Burn Animation  (plays as overlay when burning starts)")]
        public FrameAnimation burnAnimation;

        [Header("Burn Animation Overlay Size & Offset")]
        public float overlayWidth = 100f;
        public float overlayHeight = 100f;
        public Vector2 overlayOffset = Vector2.zero;

        [Header("Burned Sprite  (shown after burn animation finishes)")]
        [Tooltip("Sprite that replaces the cooked sausage once burned.")]
        public Sprite burnedSprite;

        // ── Private ───────────────────────────────────────────────────────────────

        private float _timer;
        private bool _burning = false;
        private bool _isDragged = false;
        private Image _image;

        // ── Unity ─────────────────────────────────────────────────────────────────

        private void Awake()
        {
            _image = GetComponent<Image>();

            // DraggableBurned starts disabled — enabled only after burning.
            DraggableBurned db = GetComponent<DraggableBurned>();
            if (db != null) db.enabled = false;
        }

        private void OnEnable()
        {
            _timer = burnDuration;
            _burning = false;
        }

        private void Update()
        {
            if (_burning || _isDragged) return;

            _timer -= Time.deltaTime;
            if (_timer <= 0f) StartCoroutine(BurnRoutine());
        }

        // ── Called by DraggableSausage to pause / resume the timer ────────────────

        public void SetDragging(bool dragging) => _isDragged = dragging;

        // ── Burn sequence ─────────────────────────────────────────────────────────

        private IEnumerator BurnRoutine()
        {
            _burning = true;

            // Disable normal sausage dragging immediately.
            DraggableSausage ds = GetComponent<DraggableSausage>();
            if (ds != null) ds.enabled = false;

            // ── Overlay ───────────────────────────────────────────────────────────
            if (burnAnimation != null && burnAnimation.frames != null && burnAnimation.frames.Length > 0)
            {
                GameObject overlayGO = new GameObject("BurnOverlay", typeof(RectTransform), typeof(Image));
                overlayGO.transform.SetParent(transform, false);

                RectTransform overlayRect = overlayGO.GetComponent<RectTransform>();
                overlayRect.anchorMin = new Vector2(0.5f, 0.5f);
                overlayRect.anchorMax = new Vector2(0.5f, 0.5f);
                overlayRect.pivot = new Vector2(0.5f, 0.5f);
                overlayRect.sizeDelta = new Vector2(overlayWidth, overlayHeight);
                overlayRect.anchoredPosition = overlayOffset;
                overlayRect.localScale = Vector3.one;

                Image overlayImage = overlayGO.GetComponent<Image>();
                overlayImage.raycastTarget = false;
                overlayImage.color = Color.white;
                overlayImage.sprite = burnAnimation.frames[0];

                AnimatorFrame animator = overlayGO.AddComponent<AnimatorFrame>();
                bool done = false;
                animator.Play(burnAnimation, loop: false, onComplete: () => done = true);
                yield return new WaitUntil(() => done);

                Destroy(overlayGO);
            }

            // ── Swap to burned sprite ─────────────────────────────────────────────
            if (_image != null && burnedSprite != null)
                _image.sprite = burnedSprite;

            // ── Enable burned dragging ────────────────────────────────────────────
            DraggableBurned db = GetComponent<DraggableBurned>();
            if (db != null) db.enabled = true;
            ToriSausageCookTutorialManager.Instance?.NotifyFoodBurned();

            // Free the plate slot so it doesn't stay "occupied".
            if (ds != null && ds.owningPlate != null)
                ds.owningPlate.FreeSlotOf(ds);

            Debug.Log($"[BurnerSausage] '{name}' burned!");
        }
    }
}