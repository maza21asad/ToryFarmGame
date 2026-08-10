////using UnityEngine;
////using UnityEngine.UI;
////using System.Collections;

////namespace ToriSausages
////{
////    /// <summary>
////    /// Attach to the raw sausage prefab root (alongside DraggableSausage).
////    /// Plays a cooking animation then activates the cooked child object.
////    ///
////    /// PREFAB HIERARCHY
////    ///   SausageRaw  (root)
////    ///     ├─ Image              raw sprite / cooking frames
////    ///     ├─ DraggableSausage   (isCooked = false)
////    ///     ├─ CanvasGroup
////    ///     └─ SausageCooked      ← set INACTIVE in Editor
////    ///           ├─ Image              cooked sausage sprite
////    ///           ├─ DraggableSausage   (isCooked set true at runtime)
////    ///           └─ CanvasGroup
////    /// </summary>
////    public class CookerGrill : MonoBehaviour
////    {
////        [Header("Cooking Animation")]
////        public FrameAnimation cookingAnimation;
////        public float cookDuration = 5f;

////        [Header("Cooked Child Object  (set INACTIVE in Editor)")]
////        public GameObject cookedObject;

////        [Header("Size Overrides  (0,0 = keep default)")]
////        public Vector2 cookingSize = Vector2.zero;
////        public Vector2 cookedSize = Vector2.zero;

////        [Header("Pulse on cooked")]
////        public float pulseSpeed = 3f;
////        [Range(0f, 0.25f)]
////        public float pulseAmount = 0.07f;

////        [HideInInspector] public ControllerStove stove;
////        [HideInInspector] public int slotIndex = -1;
////        [HideInInspector] public Canvas rootCanvas;

////        private Image _image;
////        private RectTransform _rect;
////        private Coroutine _animRoutine;

////        private void Awake()
////        {
////            _image = GetComponent<Image>();
////            _rect = GetComponent<RectTransform>();
////            if (cookedObject != null) cookedObject.SetActive(false);
////        }

////        public void StartCooking()
////        {
////            if (cookingSize != Vector2.zero && _rect != null) _rect.sizeDelta = cookingSize;
////            StartAnim();
////            StartCoroutine(CookTimer());
////        }

////        private IEnumerator CookTimer()
////        {
////            yield return new WaitForSeconds(cookDuration);
////            StopAnim();

////            if (cookedObject == null) { Debug.LogWarning("[CookerGrill] cookedObject not assigned!"); yield break; }
////            if (rootCanvas == null) { Debug.LogWarning("[CookerGrill] rootCanvas not set!"); yield break; }

////            Vector3 slotWorldPos = transform.position;
////            if (_image != null) _image.enabled = false;

////            DraggableSausage myDrag = GetComponent<DraggableSausage>();
////            DraggableSausage cookedDrag = cookedObject.GetComponent<DraggableSausage>();
////            if (cookedDrag != null)
////            {
////                cookedDrag.grillType = myDrag != null ? myDrag.grillType : "Sausage";
////                cookedDrag.isCooked = true;
////                cookedDrag.pendingPulse = true;
////                cookedDrag.pendingPulseSpeed = pulseSpeed;
////                cookedDrag.pendingPulseAmount = pulseAmount;
////            }

////            cookedObject.SetActive(true);

////            RectTransform cookedRect = cookedObject.GetComponent<RectTransform>();
////            cookedObject.transform.SetParent(rootCanvas.transform, false);

////            RectTransform canvasRect = rootCanvas.GetComponent<RectTransform>();
////            Camera uiCamera = rootCanvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : rootCanvas.worldCamera;
////            RectTransformUtility.ScreenPointToLocalPointInRectangle(
////                canvasRect,
////                RectTransformUtility.WorldToScreenPoint(uiCamera, slotWorldPos),
////                uiCamera, out Vector2 localPoint);
////            cookedRect.localPosition = localPoint;

////            if (cookedSize != Vector2.zero) cookedRect.sizeDelta = cookedSize;

////            CanvasGroup cg = cookedObject.GetComponent<CanvasGroup>();
////            if (cg != null) { cg.blocksRaycasts = true; cg.interactable = true; cg.alpha = 1f; }

////            Debug.Log($"[CookerGrill] Sausage cooked! Drag to a plate.");
////            stove?.FreeSlot(slotIndex);
////            Destroy(gameObject);
////        }

////        private void StartAnim()
////        {
////            StopAnim();
////            if (cookingAnimation?.frames == null || cookingAnimation.frames.Length == 0) return;
////            _animRoutine = StartCoroutine(AnimRoutine());
////        }

////        private void StopAnim()
////        {
////            if (_animRoutine == null) return;
////            StopCoroutine(_animRoutine);
////            _animRoutine = null;
////        }

////        private IEnumerator AnimRoutine()
////        {
////            float delay = 1f / cookingAnimation.fps;
////            int i = 0;
////            while (true)
////            {
////                if (_image != null) _image.sprite = cookingAnimation.frames[i];
////                yield return new WaitForSeconds(delay);
////                i++;
////                if (i >= cookingAnimation.frames.Length) yield break;
////            }
////        }
////    }
////}


//using UnityEngine;
//using UnityEngine.UI;
//using System.Collections;

//namespace ToriSausages
//{
//    /// <summary>
//    /// Attach to the raw sausage prefab root (alongside DraggableSausage).
//    /// Plays a cooking animation then activates the cooked child object.
//    ///
//    /// PREFAB HIERARCHY
//    ///   SausageRaw  (root)
//    ///     ├─ Image              raw sprite / cooking frames
//    ///     ├─ DraggableSausage   (isCooked = false)
//    ///     ├─ CanvasGroup
//    ///     └─ SausageCooked      ← set INACTIVE in Editor
//    ///           ├─ Image              cooked sausage sprite
//    ///           ├─ DraggableSausage   (isCooked set true at runtime)
//    ///           └─ CanvasGroup
//    /// </summary>
//    public class CookerGrill : MonoBehaviour
//    {
//        [Header("Cooking Animation")]
//        public FrameAnimation cookingAnimation;
//        public float cookDuration = 5f;

//        [Header("Cooked Child Object  (set INACTIVE in Editor)")]
//        public GameObject cookedObject;

//        [Header("Size Overrides  (0,0 = keep default)")]
//        public Vector2 cookingSize = Vector2.zero;
//        public Vector2 cookedSize = Vector2.zero;

//        [Header("Pulse on cooked")]
//        public float pulseSpeed = 3f;
//        [Range(0f, 0.25f)]
//        public float pulseAmount = 0.07f;

//        [HideInInspector] public ControllerStove stove;
//        [HideInInspector] public int slotIndex = -1;
//        [HideInInspector] public Canvas rootCanvas;

//        private Image _image;
//        private RectTransform _rect;
//        private Coroutine _animRoutine;

//        private void Awake()
//        {
//            _image = GetComponent<Image>();
//            _rect = GetComponent<RectTransform>();
//            if (cookedObject != null) cookedObject.SetActive(false);
//        }

//        public void StartCooking()
//        {
//            if (cookingSize != Vector2.zero && _rect != null) _rect.sizeDelta = cookingSize;
//            StartAnim();
//            SoundManager.Instance?.PlaySFXLoop("Grilling");
//            StartCoroutine(CookTimer());
//        }

//        private IEnumerator CookTimer()
//        {
//            yield return new WaitForSeconds(cookDuration);
//            StopAnim();
//            SoundManager.Instance?.StopSFXLoop();
//            SoundManager.Instance?.PlaySFX("Cooked");

//            if (cookedObject == null) { Debug.LogWarning("[CookerGrill] cookedObject not assigned!"); yield break; }

//            if (_image != null) _image.enabled = false;

//            DraggableSausage myDrag = GetComponent<DraggableSausage>();
//            DraggableSausage cookedDrag = cookedObject.GetComponent<DraggableSausage>();
//            if (cookedDrag != null)
//            {
//                cookedDrag.grillType = myDrag != null ? myDrag.grillType : "Sausage";
//                cookedDrag.isCooked = true;
//                cookedDrag.pendingPulse = true;
//                cookedDrag.pendingPulseSpeed = pulseSpeed;
//                cookedDrag.pendingPulseAmount = pulseAmount;
//            }

//            cookedObject.SetActive(true);

//            RectTransform cookedRect = cookedObject.GetComponent<RectTransform>();

//            // Keep the cooked sausage parented to the same stove slot anchor that
//            // the raw sausage occupied, so it stays correctly placed across any
//            // device/resolution (instead of being reparented to the canvas root
//            // with a frozen position).
//            RectTransform slotAnchor = (stove != null && slotIndex >= 0 && slotIndex < stove.stoveSlots.Length)
//                ? stove.stoveSlots[slotIndex].anchor
//                : null;

//            if (slotAnchor != null)
//            {
//                cookedRect.SetParent(slotAnchor, false);
//                cookedRect.anchoredPosition = Vector2.zero;
//                cookedRect.localScale = Vector3.one;
//            }
//            else
//            {
//                Debug.LogWarning("[CookerGrill] No valid stove slot anchor — falling back to canvas root.");
//                if (rootCanvas != null)
//                {
//                    Vector3 slotWorldPos = transform.position;
//                    cookedObject.transform.SetParent(rootCanvas.transform, false);

//                    RectTransform canvasRect = rootCanvas.GetComponent<RectTransform>();
//                    Camera uiCamera = rootCanvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : rootCanvas.worldCamera;
//                    RectTransformUtility.ScreenPointToLocalPointInRectangle(
//                        canvasRect,
//                        RectTransformUtility.WorldToScreenPoint(uiCamera, slotWorldPos),
//                        uiCamera, out Vector2 localPoint);
//                    cookedRect.localPosition = localPoint;
//                }
//            }

//            if (cookedSize != Vector2.zero) cookedRect.sizeDelta = cookedSize;

//            CanvasGroup cg = cookedObject.GetComponent<CanvasGroup>();
//            if (cg != null) { cg.blocksRaycasts = true; cg.interactable = true; cg.alpha = 1f; }

//            Debug.Log($"[CookerGrill] Sausage cooked! Drag to a plate.");
//            stove?.FreeSlot(slotIndex);
//            Destroy(gameObject);
//        }

//        private void StartAnim()
//        {
//            StopAnim();
//            if (cookingAnimation?.frames == null || cookingAnimation.frames.Length == 0) return;
//            _animRoutine = StartCoroutine(AnimRoutine());
//        }

//        private void StopAnim()
//        {
//            if (_animRoutine == null) return;
//            StopCoroutine(_animRoutine);
//            _animRoutine = null;
//        }

//        private IEnumerator AnimRoutine()
//        {
//            float delay = 1f / cookingAnimation.fps;
//            int i = 0;
//            while (true)
//            {
//                if (_image != null) _image.sprite = cookingAnimation.frames[i];
//                yield return new WaitForSeconds(delay);
//                i++;
//                if (i >= cookingAnimation.frames.Length) yield break;
//            }
//        }
//    }
//}

//using UnityEngine;
//using UnityEngine.UI;
//using System.Collections;

//namespace ToriSausages
//{
//    /// <summary>
//    /// Attach to the raw sausage prefab root (alongside DraggableSausage).
//    /// Plays a cooking animation then activates the cooked child object.
//    ///
//    /// PREFAB HIERARCHY
//    ///   SausageRaw  (root)
//    ///     ├─ Image              raw sprite / cooking frames
//    ///     ├─ DraggableSausage   (isCooked = false)
//    ///     ├─ CanvasGroup
//    ///     └─ SausageCooked      ← set INACTIVE in Editor
//    ///           ├─ Image              cooked sausage sprite
//    ///           ├─ DraggableSausage   (isCooked set true at runtime)
//    ///           └─ CanvasGroup
//    /// </summary>
//    public class CookerGrill : MonoBehaviour
//    {
//        [Header("Cooking Animation")]
//        public FrameAnimation cookingAnimation;
//        public float cookDuration = 5f;

//        [Header("Cooked Child Object  (set INACTIVE in Editor)")]
//        public GameObject cookedObject;

//        [Header("Size Overrides  (0,0 = keep default)")]
//        public Vector2 cookingSize = Vector2.zero;
//        public Vector2 cookedSize = Vector2.zero;

//        [Header("Pulse on cooked")]
//        public float pulseSpeed = 3f;
//        [Range(0f, 0.25f)]
//        public float pulseAmount = 0.07f;

//        [HideInInspector] public ControllerStove stove;
//        [HideInInspector] public int slotIndex = -1;
//        [HideInInspector] public Canvas rootCanvas;

//        private Image _image;
//        private RectTransform _rect;
//        private Coroutine _animRoutine;

//        private void Awake()
//        {
//            _image = GetComponent<Image>();
//            _rect = GetComponent<RectTransform>();
//            if (cookedObject != null) cookedObject.SetActive(false);
//        }

//        public void StartCooking()
//        {
//            if (cookingSize != Vector2.zero && _rect != null) _rect.sizeDelta = cookingSize;
//            StartAnim();
//            StartCoroutine(CookTimer());
//        }

//        private IEnumerator CookTimer()
//        {
//            yield return new WaitForSeconds(cookDuration);
//            StopAnim();

//            if (cookedObject == null) { Debug.LogWarning("[CookerGrill] cookedObject not assigned!"); yield break; }
//            if (rootCanvas == null) { Debug.LogWarning("[CookerGrill] rootCanvas not set!"); yield break; }

//            Vector3 slotWorldPos = transform.position;
//            if (_image != null) _image.enabled = false;

//            DraggableSausage myDrag = GetComponent<DraggableSausage>();
//            DraggableSausage cookedDrag = cookedObject.GetComponent<DraggableSausage>();
//            if (cookedDrag != null)
//            {
//                cookedDrag.grillType = myDrag != null ? myDrag.grillType : "Sausage";
//                cookedDrag.isCooked = true;
//                cookedDrag.pendingPulse = true;
//                cookedDrag.pendingPulseSpeed = pulseSpeed;
//                cookedDrag.pendingPulseAmount = pulseAmount;
//            }

//            cookedObject.SetActive(true);

//            RectTransform cookedRect = cookedObject.GetComponent<RectTransform>();
//            cookedObject.transform.SetParent(rootCanvas.transform, false);

//            RectTransform canvasRect = rootCanvas.GetComponent<RectTransform>();
//            Camera uiCamera = rootCanvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : rootCanvas.worldCamera;
//            RectTransformUtility.ScreenPointToLocalPointInRectangle(
//                canvasRect,
//                RectTransformUtility.WorldToScreenPoint(uiCamera, slotWorldPos),
//                uiCamera, out Vector2 localPoint);
//            cookedRect.localPosition = localPoint;

//            if (cookedSize != Vector2.zero) cookedRect.sizeDelta = cookedSize;

//            CanvasGroup cg = cookedObject.GetComponent<CanvasGroup>();
//            if (cg != null) { cg.blocksRaycasts = true; cg.interactable = true; cg.alpha = 1f; }

//            Debug.Log($"[CookerGrill] Sausage cooked! Drag to a plate.");
//            stove?.FreeSlot(slotIndex);
//            Destroy(gameObject);
//        }

//        private void StartAnim()
//        {
//            StopAnim();
//            if (cookingAnimation?.frames == null || cookingAnimation.frames.Length == 0) return;
//            _animRoutine = StartCoroutine(AnimRoutine());
//        }

//        private void StopAnim()
//        {
//            if (_animRoutine == null) return;
//            StopCoroutine(_animRoutine);
//            _animRoutine = null;
//        }

//        private IEnumerator AnimRoutine()
//        {
//            float delay = 1f / cookingAnimation.fps;
//            int i = 0;
//            while (true)
//            {
//                if (_image != null) _image.sprite = cookingAnimation.frames[i];
//                yield return new WaitForSeconds(delay);
//                i++;
//                if (i >= cookingAnimation.frames.Length) yield break;
//            }
//        }
//    }
//}


using UnityEngine;
using UnityEngine.UI;
using System.Collections;

namespace ToriSausages
{
    /// <summary>
    /// Attach to the raw sausage prefab root (alongside DraggableSausage).
    /// Plays a cooking animation then activates the cooked child object.
    ///
    /// PREFAB HIERARCHY
    ///   SausageRaw  (root)
    ///     ├─ Image              raw sprite / cooking frames
    ///     ├─ DraggableSausage   (isCooked = false)
    ///     ├─ CanvasGroup
    ///     └─ SausageCooked      ← set INACTIVE in Editor
    ///           ├─ Image              cooked sausage sprite
    ///           ├─ DraggableSausage   (isCooked set true at runtime)
    ///           └─ CanvasGroup
    /// </summary>
    public class CookerGrill : MonoBehaviour
    {
        [Header("Cooking Animation")]
        public FrameAnimation cookingAnimation;
        public float cookDuration = 5f;

        [Header("Cooked Child Object  (set INACTIVE in Editor)")]
        public GameObject cookedObject;

        [Header("Size Overrides  (0,0 = keep default)")]
        public Vector2 cookingSize = Vector2.zero;
        public Vector2 cookedSize = Vector2.zero;

        [Header("Pulse on cooked")]
        public float pulseSpeed = 3f;
        [Range(0f, 0.25f)]
        public float pulseAmount = 0.07f;

        [HideInInspector] public ControllerStove stove;
        [HideInInspector] public int slotIndex = -1;
        [HideInInspector] public Canvas rootCanvas;

        private Image _image;
        private RectTransform _rect;
        private Coroutine _animRoutine;

        private void Awake()
        {
            _image = GetComponent<Image>();
            _rect = GetComponent<RectTransform>();
            if (cookedObject != null) cookedObject.SetActive(false);
        }

        public void StartCooking()
        {
            if (cookingSize != Vector2.zero && _rect != null) _rect.sizeDelta = cookingSize;

            // Lock the raw sausage — undraggable while cooking.
            // The raw GameObject is Destroyed when CookTimer finishes, so
            // re-enabling is never needed.
            DraggableSausage rawDrag = GetComponent<DraggableSausage>();
            if (rawDrag != null) rawDrag.enabled = false;

            StartAnim();
            SoundManager.Instance?.PlaySFXLoop("Grilling");
            StartCoroutine(CookTimer());
        }

        private IEnumerator CookTimer()
        {
            yield return new WaitForSeconds(cookDuration);
            StopAnim();
            SoundManager.Instance?.StopSFXLoop();
            SoundManager.Instance?.PlaySFX("Cooked");

            if (cookedObject == null) { Debug.LogWarning("[CookerGrill] cookedObject not assigned!"); yield break; }

            if (_image != null) _image.enabled = false;

            DraggableSausage myDrag = GetComponent<DraggableSausage>();
            DraggableSausage cookedDrag = cookedObject.GetComponent<DraggableSausage>();
            if (cookedDrag != null)
            {
                cookedDrag.grillType = myDrag != null ? myDrag.grillType : "Sausage";
                cookedDrag.isCooked = true;
                cookedDrag.pendingPulse = true;
                cookedDrag.pendingPulseSpeed = pulseSpeed;
                cookedDrag.pendingPulseAmount = pulseAmount;
            }

            cookedObject.SetActive(true);

            RectTransform cookedRect = cookedObject.GetComponent<RectTransform>();

            // Keep the cooked sausage parented to the same stove slot anchor that
            // the raw sausage occupied, so it stays correctly placed across any
            // device/resolution (instead of being reparented to the canvas root
            // with a frozen position).
            RectTransform slotAnchor = (stove != null && slotIndex >= 0 && slotIndex < stove.stoveSlots.Length)
                ? stove.stoveSlots[slotIndex].anchor
                : null;

            if (slotAnchor != null)
            {
                cookedRect.SetParent(slotAnchor, false);
                cookedRect.anchoredPosition = Vector2.zero;
                cookedRect.localScale = Vector3.one;
            }
            else
            {
                Debug.LogWarning("[CookerGrill] No valid stove slot anchor — falling back to canvas root.");
                if (rootCanvas != null)
                {
                    Vector3 slotWorldPos = transform.position;
                    cookedObject.transform.SetParent(rootCanvas.transform, false);

                    RectTransform canvasRect = rootCanvas.GetComponent<RectTransform>();
                    Camera uiCamera = rootCanvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : rootCanvas.worldCamera;
                    RectTransformUtility.ScreenPointToLocalPointInRectangle(
                        canvasRect,
                        RectTransformUtility.WorldToScreenPoint(uiCamera, slotWorldPos),
                        uiCamera, out Vector2 localPoint);
                    cookedRect.localPosition = localPoint;
                }
            }

            if (cookedSize != Vector2.zero) cookedRect.sizeDelta = cookedSize;

            CanvasGroup cg = cookedObject.GetComponent<CanvasGroup>();
            if (cg != null) { cg.blocksRaycasts = true; cg.interactable = true; cg.alpha = 1f; }

            Debug.Log($"[CookerGrill] Sausage cooked! Drag to a plate.");
            stove?.FreeSlot(slotIndex);
            ToriSausageCookTutorialManager.Instance?.NotifyCookingDone();
            Destroy(gameObject);
        }

        private void StartAnim()
        {
            StopAnim();
            if (cookingAnimation?.frames == null || cookingAnimation.frames.Length == 0) return;
            _animRoutine = StartCoroutine(AnimRoutine());
        }

        private void StopAnim()
        {
            if (_animRoutine == null) return;
            StopCoroutine(_animRoutine);
            _animRoutine = null;
        }

        private IEnumerator AnimRoutine()
        {
            float delay = 1f / cookingAnimation.fps;
            int i = 0;
            while (true)
            {
                if (_image != null) _image.sprite = cookingAnimation.frames[i];
                yield return new WaitForSeconds(delay);
                i++;
                if (i >= cookingAnimation.frames.Length) yield break;
            }
        }
    }
}