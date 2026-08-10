//using UnityEngine;
//using UnityEngine.UI;
//using System.Collections;

//public class DogAnimationController : MonoBehaviour
//{
//    public enum DogState { Idle, Wrong, Congrats }

//    [Header("Idle Animation")]
//    public Sprite[] idleFrames;
//    public float idleFPS = 12f;

//    [Header("Wrong Animation")]
//    public Sprite[] wrongFrames;
//    public float wrongFPS = 12f;

//    [Header("Congratulation Animation")]
//    public Sprite[] congratsFrames;
//    public float congratsFPS = 12f;

//    private Image dogImage;
//    private RectTransform dogRect;
//    private Vector2 originalSize;

//    // ✅ Current state controls which loop runs
//    private DogState currentState = DogState.Idle;

//    void Start()
//    {
//        dogImage = GetComponent<Image>();
//        dogRect = GetComponent<RectTransform>();
//        originalSize = dogRect.sizeDelta;

//        SetState(DogState.Idle);
//    }

//    public void PlayIdle()
//    {
//        SetState(DogState.Idle);
//    }

//    public void PlayWrong()
//    {
//        if (currentState == DogState.Wrong) return;
//        if (currentState == DogState.Congrats) return;
//        SetState(DogState.Wrong);
//    }

//    public void PlayCongrats()
//    {
//        Debug.Log("[Dog] PlayCongrats called!");
//        SetState(DogState.Congrats);
//    }

//    void SetState(DogState newState)
//    {
//        currentState = newState;
//        Debug.Log($"[Dog] State changed to: {newState}");

//        // ✅ Stop ALL then start master loop
//        StopAllCoroutines();
//        StartCoroutine(MasterLoop());
//    }

//    // ✅ Single master loop - checks state every frame
//    IEnumerator MasterLoop()
//    {
//        int frame = 0;

//        while (true)
//        {
//            Sprite[] frames = null;
//            float fps = 12f;

//            // ✅ Pick correct frames based on state
//            switch (currentState)
//            {
//                case DogState.Idle:
//                    frames = idleFrames;
//                    fps = idleFPS;
//                    break;

//                case DogState.Wrong:
//                    frames = wrongFrames;
//                    fps = wrongFPS;
//                    break;

//                case DogState.Congrats:
//                    frames = congratsFrames;
//                    fps = congratsFPS;
//                    break;
//            }

//            if (frames == null || frames.Length == 0)
//            {
//                Debug.LogError($"[Dog] {currentState} frames are EMPTY!");
//                yield return new WaitForSeconds(0.1f);
//                continue;
//            }

//            // ✅ Clamp frame index
//            if (frame >= frames.Length)
//                frame = 0;

//            dogImage.sprite = frames[frame];
//            dogRect.sizeDelta = originalSize;
//            frame++;

//            // ✅ Wrong animation plays ONCE then returns to idle
//            if (currentState == DogState.Wrong && frame >= frames.Length)
//            {
//                frame = 0;
//                currentState = DogState.Idle;
//            }

//            // ✅ Idle and Congrats loop forever
//            if (frame >= frames.Length)
//                frame = 0;

//            yield return new WaitForSeconds(1f / fps);
//        }
//    }
//}
////```

////## How It Works
////```
////SetState(Idle)    → StopAllCoroutines → MasterLoop starts
////                  → plays idle frames forever ✅

////SetState(Wrong)   → StopAllCoroutines → MasterLoop starts
////                  → plays wrong frames once
////                  → auto switches to Idle ✅

////SetState(Congrats)→ StopAllCoroutines → MasterLoop starts
////                  → plays congrats frames forever ✅
////                  → idle completely stopped ✅
////```

////## Check Console After Playing
////```
////"[Dog] State changed to: Idle"     → on start ✅
////"[Dog] PlayCongrats called!"       → when all repaired
////"[Dog] State changed to: Congrats" → should appear ✅
////"[Dog] congratsFrames are EMPTY!"  → frames missing ❌
///
using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class DogAnimationController : MonoBehaviour
{
    public enum DogState { Idle, Wrong, Congrats }

    [Header("Idle Animation")]
    public Sprite[] idleFrames;
    public float idleFPS = 12f;

    [Header("Wrong Animation")]
    public Sprite[] wrongFrames;
    public float wrongFPS = 12f;

    [Header("Congratulation Animation")]
    public Sprite[] congratsFrames;
    public float congratsFPS = 12f;

    private Image dogImage;
    private RectTransform dogRect;
    private Vector2 originalSize;

    // ✅ Current state controls which loop runs
    public static DogAnimationController Instance { get; private set; }

    private DogState currentState = DogState.Idle;

    void Awake()
    {
        Instance = this;
    }

    void Start()
    {
        dogImage = GetComponent<Image>();
        dogRect = GetComponent<RectTransform>();
        originalSize = dogRect.sizeDelta;

        SetState(DogState.Idle);
    }

    public void PlayIdle()
    {
        SetState(DogState.Idle);
    }

    public void PlayWrong()
    {
        if (currentState == DogState.Wrong) return;
        if (currentState == DogState.Congrats) return;
        SetState(DogState.Wrong);
    }

    public void PlayCongrats()
    {
        Debug.Log("[Dog] PlayCongrats called!");
        SetState(DogState.Congrats);
    }

    void SetState(DogState newState)
    {
        currentState = newState;
        Debug.Log($"[Dog] State changed to: {newState}");

        // ✅ Stop ALL then start master loop
        StopAllCoroutines();
        StartCoroutine(MasterLoop());
    }

    // ✅ Single master loop - checks state every frame
    IEnumerator MasterLoop()
    {
        int frame = 0;

        while (true)
        {
            Sprite[] frames = null;
            float fps = 12f;

            // ✅ Pick correct frames based on state
            switch (currentState)
            {
                case DogState.Idle:
                    frames = idleFrames;
                    fps = idleFPS;
                    break;

                case DogState.Wrong:
                    frames = wrongFrames;
                    fps = wrongFPS;
                    break;

                case DogState.Congrats:
                    frames = congratsFrames;
                    fps = congratsFPS;
                    break;
            }

            if (frames == null || frames.Length == 0)
            {
                Debug.LogError($"[Dog] {currentState} frames are EMPTY!");
                yield return new WaitForSeconds(0.1f);
                continue;
            }

            // ✅ Clamp frame index
            if (frame >= frames.Length)
                frame = 0;

            dogImage.sprite = frames[frame];
            dogRect.sizeDelta = originalSize;
            frame++;

            // ✅ Wrong animation plays ONCE then returns to idle
            if (currentState == DogState.Wrong && frame >= frames.Length)
            {
                frame = 0;
                currentState = DogState.Idle;
            }

            // ✅ Idle and Congrats loop forever
            if (frame >= frames.Length)
                frame = 0;

            yield return new WaitForSeconds(1f / fps);
        }
    }
}
//```

//## How It Works
//```
//SetState(Idle)    → StopAllCoroutines → MasterLoop starts
//                  → plays idle frames forever ✅

//SetState(Wrong)   → StopAllCoroutines → MasterLoop starts
//                  → plays wrong frames once
//                  → auto switches to Idle ✅

//SetState(Congrats)→ StopAllCoroutines → MasterLoop starts
//                  → plays congrats frames forever ✅
//                  → idle completely stopped ✅
//```

//## Check Console After Playing
//```
//"[Dog] State changed to: Idle"     → on start ✅
//"[Dog] PlayCongrats called!"       → when all repaired
//"[Dog] State changed to: Congrats" → should appear ✅
//"[Dog] congratsFrames are EMPTY!"  → frames missing ❌