using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using TMPro;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement; // Added for Restart functionality

public class TutorialManager : MonoBehaviour
{
    public static TutorialManager Instance;

    // =====================================================================
    // SHARED — Cow, Bubble, Hand, Luna
    // =====================================================================

    [Header("Luna Animator")]
    public AnimatorForLuna lunaAnimator;

    [Header("Female Cow")]
    public RectTransform femaleCow;
    public Vector2 cowStartPos;
    public Vector2 cowEndPos;
    public float cowSlideDuration = 1.2f;

    [Header("Thought Bubble")]
    public RectTransform thoughtBubble;
    public float bubblePopSpeed = 5f;
    public TextMeshProUGUI thoughtText;
    public float typingSpeed = 0.05f;

    [Header("Hand Pointer")]
    public RectTransform handPointer;
    public RectTransform handTip;
    public float handMoveSpeed = 3f;
    public float handPauseDuration = 0.4f;

    // =====================================================================
    // PHASE 1 — Collect Crops
    // =====================================================================

    [Header("Phase 1 - Scene Object")]
    public GameObject collectCrops;

    [Header("Phase 1 - Messages")]
    public string msg1_Hello = "Hello!";
    public string msg1_Intro = "Today we are going to collect some corn from the field!";
    public string msg1_ClickCorn = "Try clicking on the corn!";
    public string msg1_CollectAll = "Good job! Now try collecting all the corns!";
    public string msg1_WellDone = "Well done collecting all the corns!";
    public string msg1_NextStep = "We can now go to the next step!";

    [Header("Phase 1 - Hand Targets")]
    public RectTransform cartTarget;
    public RectTransform cornTarget;

    [Header("Phase 1 - Corn Objects")]
    public GameObject[] cornObjects;

    private bool p1_handActive = false;
    private bool p1_firstCornClicked = false;
    private bool p1_allCornsPlaced = false;
    private int p1_placedCornCount = 0;

    // =====================================================================
    // PHASE 2 — Chicken Hut
    // =====================================================================

    [Header("Phase 2 - Scene Object")]
    public GameObject chickenHut;

    [Header("Phase 2 - Chicken Thought Bubble")]
    public RectTransform chickenThoughtBubble;
    private Vector3 chickenBubbleOriginalScale;

    [Header("Phase 2 - Messages")]
    public string msg2_FeedChicken = "Oky Now try Give some corns to the Chicken!";

    [Header("Phase 2 - Hand Targets")]
    public RectTransform cornCartButton;
    public RectTransform chickenCenter;

    [Header("Phase 2 - Chicken Animation")]
    public GameObject chickenIdle;
    public GameObject chickenHappy;
    public float happyAnimDuration = 2f;

    [Header("Phase 2 - Egg")]
    public RectTransform eggButton;
    public float popSpeed = 8f;

    [Header("Phase 2 - Messages")]
    public string msg2_GoodJob = "Good job!";
    public string msg2_Eggs = "Looks like we got some Eggs from her!";
    public string msg2_NextStep = "Lets go to the next step!";

    [Header("Phase 2 - Cart Settings")]
    public float cartMoveSpeed = 3f;

    private bool p2_handActive = false;
    private bool p2_cartClicked = false;
    private Vector2 p2_cartOrigin;

    // =====================================================================
    // PHASE 3 — Cow Shop
    // =====================================================================

    [Header("Phase 3 - Scene Object")]
    public GameObject cowShop;

    [Header("Phase 3 - Messages")]
    public string msg3_NeedsCorns = "Looks like He also need some corns too!";
    public string msg3_GiveCorns = "Give him some corn!";
    public string msg3_GoodJob = "Good job!";
    public string msg3_GotMilks = "Looks like we got some milks from the cow!";
    public string msg3_NextStep = "Lets go to our last step!";

    [Header("Phase 3 - Hand Targets")]
    public RectTransform cowShopCartButton;
    public RectTransform cowCenter;

    [Header("Phase 3 - Milk Cart")]
    public RectTransform milkCart;
    [Tooltip("Speed (units/sec) for the corn cart moving toward the milk cart")]
    public float cornCartToMilkSpeed = 300f;
    [Tooltip("Speed (units/sec) for the milk cart moving back to the corn cart's original position")]
    public float milkCartMoveSpeed = 300f;

    [Header("Phase 3 - Cow Animation")]
    public GameObject cowIdle;
    public GameObject cowHappy;
    public float cowHappyAnimDuration = 2f;

    [Header("Phase 3 - Cow Thought Bubble")]
    public RectTransform cowThoughtBubble;
    private Vector3 cowBubbleOriginalScale;

    private bool p3_handActive = false;
    private bool p3_cartClicked = false;
    private Vector2 p3_cartOrigin;
    private Vector3 milkCartOriginalScale;

    // =====================================================================
    // PHASE 4 — Grind the Corn
    // =====================================================================

    [Header("Phase 4 - Scene Object")]
    public GameObject grindTheCorns;

    [Header("Phase 4 - Messages")]
    public string msg4_PutCorn = "Put corn on the grinder!";
    public string msg4_GotFlour = "Great! We got some Flour!";
    public string msg4_MakeCake = "We can make a cake!";
    public string msg4_LetsGo = "Let's go make it!";

    [Header("Phase 4 - Hand Targets")]
    public RectTransform grindCartButton;
    public RectTransform grinderTip;

    [Header("Phase 4 - Grinder Objects")]
    public GameObject grinder1;
    public GameObject grinder2;
    public GrinderAnim grinderAnim;

    [Header("Phase 4 - Flour Bag")]
    public GameObject flourBag1;
    public RectTransform flourBagRect;
    public FlourBagAnim flourBagAnim;
    public float flourBagMoveSpeed = 3f;

    [Header("Phase 4 - Grinder Thought Bubble")]
    public RectTransform grindThoughtBubble;
    private Vector3 grindBubbleOriginalScale;

    [Header("Phase 4 - Cart Settings")]
    public float grindCartMoveSpeed = 3f;

    private bool p4_handActive = false;
    private bool p4_cartClicked = false;
    private Vector3 p4_cartOriginWorld;

    // =====================================================================
    // PHASE 5 — Make Cake
    // =====================================================================

    [Header("Phase 5 - Scene Object")]
    public GameObject makeCake;

    [Header("Phase 5 - Thought Bubble (TB)")]
    public RectTransform tb;
    public TextMeshProUGUI tbText;
    private Vector3 tbOriginalScale;

    [Header("Phase 5 - Messages")]
    public string msg5_Ingredients = "Now we have all the ingredients to make a Cake!";
    public string msg5_NeedFlour = "First we need some Flour!";
    public string msg5_Nice = "Nice!";
    public string msg5_NeedMilk = "Now add some Milk!";
    public string msg5_GoodJob = "Good job!";
    public string msg5_NiceJob = "Nice job!";
    public string msg5_NeedEggs = "Now add some Eggs!";

    [Header("Phase 5 - Ingredient Buttons")]
    public RectTransform flourCartButton;
    public RectTransform milkCartButton;
    public RectTransform eggCartButton;

    [Header("Phase 5 - Bowl")]
    public RectTransform bowlRect;
    public Bowl bowlAnim;
    public GameObject bowlObjectToHide; // Assign the bowl GameObject here to hide it during cake animation

    [Header("Phase 5 - Cow Making Cake")]
    public GameObject cowMakingCake;
    public CakemakingAnimator cakemakingAnim;

    [Header("Phase 5 - Visibility & UI")]
    public GameObject cowIdleInCakeScene; // The idle cow to hide during animation
    public GameObject winPanel;           // The UI panel with restart button

    [Header("Phase 5 - Cart Move Speed")]
    public float cakeCartMoveSpeed = 3f;

    private bool p5_flourClicked = false;
    private bool p5_milkClicked = false;
    private bool p5_eggClicked = false;
    private bool p5_handActive = false;
    private int _tbSkipCount = 0;
    private int _skipCount = 0;

    // =====================================================================
    // UNITY LIFECYCLE
    // =====================================================================

    void Awake()
    {
        Instance = this;
    }

    void Start()
    {
        handPointer.gameObject.SetActive(false);
        thoughtText.text = "";
        femaleCow.anchoredPosition = cowStartPos;
        thoughtBubble.localScale = Vector3.zero;

        if (collectCrops != null) collectCrops.SetActive(true);
        if (chickenHut != null) chickenHut.SetActive(false);
        if (cowShop != null) cowShop.SetActive(false);
        if (grindTheCorns != null) grindTheCorns.SetActive(false);
        if (makeCake != null) makeCake.SetActive(false);
        if (winPanel != null) winPanel.SetActive(false);

        if (chickenThoughtBubble != null)
        {
            chickenBubbleOriginalScale = chickenThoughtBubble.localScale;
            chickenThoughtBubble.gameObject.SetActive(false);
        }
        if (cowThoughtBubble != null)
        {
            cowBubbleOriginalScale = cowThoughtBubble.localScale;
            cowThoughtBubble.gameObject.SetActive(false);
        }

        if (milkCart != null)
        {
            milkCartOriginalScale = milkCart.localScale;
            milkCart.gameObject.SetActive(false);
        }

        if (grinder2 != null) grinder2.SetActive(false);
        if (grinderAnim != null) grinderAnim.gameObject.SetActive(false);
        if (flourBagRect != null) flourBagRect.gameObject.SetActive(false);

        if (grindThoughtBubble != null)
        {
            grindBubbleOriginalScale = grindThoughtBubble.localScale;
            grindThoughtBubble.gameObject.SetActive(false);
        }

        if (tb != null)
        {
            tbOriginalScale = tb.localScale;
            tb.localScale = Vector3.zero;
        }
        if (cowMakingCake != null) cowMakingCake.SetActive(false);

        Phase1_SetCornsInteractable(false);
        StartCoroutine(Phase1_Tutorial());
    }

    // =====================================================================
    // PHASE 1 COROUTINE
    // =====================================================================

    IEnumerator Phase1_Tutorial()
    {
        femaleCow.gameObject.SetActive(true);
        femaleCow.anchoredPosition = cowStartPos;

        yield return StartCoroutine(SlideCowIn());
        yield return StartCoroutine(PopBubble());

        yield return StartCoroutine(TypeText(msg1_Hello));
        yield return StartCoroutine(WaitOrSkipMain(1.5f));

        yield return StartCoroutine(DeleteText());
        yield return StartCoroutine(TypeText(msg1_Intro));
        yield return StartCoroutine(WaitOrSkipMain(2f));

        yield return StartCoroutine(DeleteText());
        yield return StartCoroutine(TypeText(msg1_ClickCorn));

        Phase1_SetupCornListener();

        p1_handActive = true;
        handPointer.gameObject.SetActive(true);
        handPointer.position = GetHandPositionForTarget(cartTarget);
        StartCoroutine(Phase1_LoopHand());

        yield return new WaitUntil(() => p1_firstCornClicked);

        p1_handActive = false;
        handPointer.gameObject.SetActive(false);
        Phase1_SetCornsInteractable(true);

        yield return StartCoroutine(DeleteText());
        yield return StartCoroutine(TypeText(msg1_CollectAll));
        yield return StartCoroutine(WaitOrSkipMain(2f));

        yield return StartCoroutine(ShrinkBubble());
        yield return StartCoroutine(SlideCowOut());

        yield return new WaitUntil(() => p1_allCornsPlaced);

        femaleCow.gameObject.SetActive(true);
        femaleCow.anchoredPosition = cowStartPos;
        yield return StartCoroutine(SlideCowIn());
        yield return StartCoroutine(PopBubble());

        yield return StartCoroutine(TypeText(msg1_WellDone));
        yield return StartCoroutine(WaitOrSkipMain(2f));

        yield return StartCoroutine(DeleteText());
        yield return StartCoroutine(TypeText(msg1_NextStep));
        yield return StartCoroutine(WaitOrSkipMain(2f));

        yield return StartCoroutine(ShrinkBubble());
        yield return StartCoroutine(SlideCowOut());

        yield return new WaitForSeconds(1f);
        if (collectCrops != null) collectCrops.SetActive(false);
        if (chickenHut != null) chickenHut.SetActive(true);
        StartCoroutine(PopChickenBubble());
        StartCoroutine(Phase2_Tutorial());
    }

    // =====================================================================
    // PHASE 1 HELPERS
    // =====================================================================

    void Phase1_SetCornsInteractable(bool state)
    {
        foreach (GameObject corn in cornObjects)
        {
            if (corn == null) continue;
            Button btn = corn.GetComponent<Button>();
            if (btn != null) btn.interactable = state;
            Image img = corn.GetComponent<Image>();
            if (img != null) img.raycastTarget = state;
        }
    }

    void Phase1_SetupCornListener()
    {
        if (cornTarget == null) return;

        Image img = cornTarget.GetComponent<Image>();
        if (img != null) img.raycastTarget = true;

        Button btn = cornTarget.GetComponent<Button>();
        if (btn != null)
        {
            btn.interactable = true;
            btn.onClick.AddListener(Phase1_OnCornClicked);
            return;
        }

        EventTrigger trigger = cornTarget.GetComponent<EventTrigger>()
                            ?? cornTarget.gameObject.AddComponent<EventTrigger>();
        EventTrigger.Entry entry = new EventTrigger.Entry();
        entry.eventID = EventTriggerType.PointerClick;
        entry.callback.AddListener((data) => Phase1_OnCornClicked());
        trigger.triggers.Add(entry);
    }

    void Phase1_OnCornClicked()
    {
        if (p1_firstCornClicked) return;
        p1_firstCornClicked = true;
    }

    public void Phase1_OnCornPlaced()
    {
        p1_placedCornCount++;
        if (p1_placedCornCount >= cornObjects.Length)
            p1_allCornsPlaced = true;
    }

    IEnumerator Phase1_LoopHand()
    {
        while (p1_handActive)
        {
            yield return StartCoroutine(MoveHand(GetHandPositionForTarget(cartTarget)));
            yield return new WaitForSeconds(handPauseDuration);
            yield return StartCoroutine(MoveHand(GetHandPositionForTarget(cornTarget)));
            yield return new WaitForSeconds(handPauseDuration);
        }
    }

    // =====================================================================
    // PHASE 2 COROUTINE
    // =====================================================================

    IEnumerator Phase2_Tutorial()
    {
        femaleCow.gameObject.SetActive(true);
        femaleCow.anchoredPosition = cowStartPos;
        yield return StartCoroutine(SlideCowIn());
        yield return StartCoroutine(PopBubble());

        yield return StartCoroutine(TypeText(msg2_FeedChicken));
        yield return StartCoroutine(WaitOrSkipMain(1.5f));

        yield return StartCoroutine(ShrinkBubble());
        yield return StartCoroutine(SlideCowOut());

        Phase2_SetupCartListener();

        p2_handActive = true;
        handPointer.gameObject.SetActive(true);
        handPointer.position = GetHandPositionForTarget(cornCartButton);
        StartCoroutine(Phase2_LoopHand());

        yield return new WaitUntil(() => p2_cartClicked);

        p2_handActive = false;
        handPointer.gameObject.SetActive(false);

        p2_cartOrigin = cornCartButton.anchoredPosition;
        yield return StartCoroutine(Phase2_MoveCartToEgg());

        femaleCow.gameObject.SetActive(true);
        femaleCow.anchoredPosition = cowStartPos;
        yield return StartCoroutine(SlideCowIn());
        yield return StartCoroutine(PopBubble());

        yield return StartCoroutine(TypeText(msg2_GoodJob));
        yield return StartCoroutine(WaitOrSkipMain(2f));

        yield return StartCoroutine(DeleteText());
        yield return StartCoroutine(TypeText(msg2_Eggs));
        yield return StartCoroutine(WaitOrSkipMain(2f));

        yield return StartCoroutine(DeleteText());
        yield return StartCoroutine(TypeText(msg2_NextStep));
        yield return StartCoroutine(WaitOrSkipMain(2f));

        yield return StartCoroutine(ShrinkBubble());
        yield return StartCoroutine(SlideCowOut());

        yield return new WaitForSeconds(1f);
        if (chickenHut != null) chickenHut.SetActive(false);
        if (cowShop != null) cowShop.SetActive(true);
        StartCoroutine(PopCowBubble());
        StartCoroutine(Phase3_Tutorial());
    }

    // =====================================================================
    // PHASE 2 HELPERS
    // =====================================================================

    void Phase2_SetupCartListener()
    {
        if (cornCartButton == null) return;

        Image img = cornCartButton.GetComponent<Image>();
        if (img != null) img.raycastTarget = true;

        Button btn = cornCartButton.GetComponent<Button>();
        if (btn != null)
        {
            btn.interactable = true;
            btn.onClick.AddListener(Phase2_OnCartClicked);
            return;
        }

        EventTrigger trigger = cornCartButton.GetComponent<EventTrigger>()
                            ?? cornCartButton.gameObject.AddComponent<EventTrigger>();
        EventTrigger.Entry entry = new EventTrigger.Entry();
        entry.eventID = EventTriggerType.PointerClick;
        entry.callback.AddListener((data) => Phase2_OnCartClicked());
        trigger.triggers.Add(entry);
    }

    void Phase2_OnCartClicked()
    {
        if (p2_cartClicked) return;
        p2_cartClicked = true;
    }

    IEnumerator Phase2_LoopHand()
    {
        while (p2_handActive)
        {
            yield return StartCoroutine(MoveHand(GetHandPositionForTarget(cornCartButton)));
            yield return new WaitForSeconds(handPauseDuration);
            yield return StartCoroutine(MoveHand(GetHandPositionForTarget(chickenCenter)));
            yield return new WaitForSeconds(handPauseDuration);
        }
    }

    IEnumerator Phase2_MoveCartToEgg()
    {
        if (cornCartButton == null || eggButton == null) yield break;

        Vector2 startPos = cornCartButton.anchoredPosition;
        Vector2 endPos = eggButton.anchoredPosition;
        float elapsed = 0f;
        float duration = Vector2.Distance(startPos, endPos) / (cartMoveSpeed * 100f);

        SoundManager.Instance.PlaySFX("Swipe");
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            cornCartButton.anchoredPosition = Vector2.Lerp(startPos, endPos, t);
            yield return null;
        }
        cornCartButton.anchoredPosition = endPos;

        yield return StartCoroutine(ShrinkChickenBubble());

        if (chickenIdle != null) chickenIdle.SetActive(false);
        if (chickenHappy != null) chickenHappy.SetActive(true);
        yield return new WaitForSeconds(happyAnimDuration);
        if (chickenHappy != null) chickenHappy.SetActive(false);
        if (chickenIdle != null) chickenIdle.SetActive(true);

        while (cornCartButton.localScale.x > 0.01f)
        {
            cornCartButton.localScale = Vector3.Lerp(
                cornCartButton.localScale, Vector3.zero,
                Time.deltaTime * popSpeed);
            yield return null;
        }
        cornCartButton.localScale = Vector3.zero;
        cornCartButton.gameObject.SetActive(false);

        Vector3 eggOriginalScale = eggButton.localScale;
        eggButton.gameObject.SetActive(true);
        eggButton.localScale = Vector3.zero;
        while (Vector3.Distance(eggButton.localScale, eggOriginalScale) > 0.01f)
        {
            eggButton.localScale = Vector3.Lerp(
                eggButton.localScale, eggOriginalScale,
                Time.deltaTime * popSpeed);
            yield return null;
        }
        eggButton.localScale = eggOriginalScale;

        Vector2 eggStart = eggButton.anchoredPosition;
        elapsed = 0f;
        duration = Vector2.Distance(eggStart, p2_cartOrigin) / (cartMoveSpeed * 100f);

        SoundManager.Instance.PlaySFX("Swipe");
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            eggButton.anchoredPosition = Vector2.Lerp(eggStart, p2_cartOrigin, t);
            yield return null;
        }

        eggButton.anchoredPosition = p2_cartOrigin;
        Debug.Log("Egg reached cart position!");
    }

    // =====================================================================
    // PHASE 3 COROUTINE
    // =====================================================================

    IEnumerator Phase3_Tutorial()
    {
        femaleCow.gameObject.SetActive(true);
        femaleCow.anchoredPosition = cowStartPos;
        yield return StartCoroutine(SlideCowIn());
        yield return StartCoroutine(PopBubble());

        yield return StartCoroutine(TypeText(msg3_NeedsCorns));
        yield return StartCoroutine(WaitOrSkipMain(2f));

        yield return StartCoroutine(DeleteText());
        yield return StartCoroutine(TypeText(msg3_GiveCorns));
        yield return StartCoroutine(WaitOrSkipMain(1.5f));

        yield return StartCoroutine(ShrinkBubble());
        yield return StartCoroutine(SlideCowOut());

        Phase3_SetupCartListener();

        p3_handActive = true;
        handPointer.gameObject.SetActive(true);
        handPointer.position = GetHandPositionForTarget(cowShopCartButton);
        StartCoroutine(Phase3_LoopHand());

        yield return new WaitUntil(() => p3_cartClicked);

        p3_handActive = false;
        handPointer.gameObject.SetActive(false);

        p3_cartOrigin = cowShopCartButton.anchoredPosition;
        yield return StartCoroutine(Phase3_MoveCartToMilk());

        femaleCow.gameObject.SetActive(true);
        femaleCow.anchoredPosition = cowStartPos;
        yield return StartCoroutine(SlideCowIn());
        yield return StartCoroutine(PopBubble());

        yield return StartCoroutine(TypeText(msg3_GoodJob));
        yield return StartCoroutine(WaitOrSkipMain(2f));

        yield return StartCoroutine(DeleteText());
        yield return StartCoroutine(TypeText(msg3_GotMilks));
        yield return StartCoroutine(WaitOrSkipMain(2f));

        yield return StartCoroutine(DeleteText());
        yield return StartCoroutine(TypeText(msg3_NextStep));
        yield return StartCoroutine(WaitOrSkipMain(2f));

        yield return StartCoroutine(ShrinkBubble());
        yield return StartCoroutine(SlideCowOut());

        yield return new WaitForSeconds(1f);
        if (cowShop != null) cowShop.SetActive(false);
        if (grindTheCorns != null) grindTheCorns.SetActive(true);
        StartCoroutine(PopGrindBubble());
        StartCoroutine(Phase4_Tutorial());
    }

    // =====================================================================
    // PHASE 3 HELPERS
    // =====================================================================

    void Phase3_SetupCartListener()
    {
        if (cowShopCartButton == null) return;

        Image img = cowShopCartButton.GetComponent<Image>();
        if (img != null) img.raycastTarget = true;

        Button btn = cowShopCartButton.GetComponent<Button>();
        if (btn != null)
        {
            btn.interactable = true;
            btn.onClick.AddListener(Phase3_OnCartClicked);
            return;
        }

        EventTrigger trigger = cowShopCartButton.GetComponent<EventTrigger>()
                            ?? cowShopCartButton.gameObject.AddComponent<EventTrigger>();
        EventTrigger.Entry entry = new EventTrigger.Entry();
        entry.eventID = EventTriggerType.PointerClick;
        entry.callback.AddListener((data) => Phase3_OnCartClicked());
        trigger.triggers.Add(entry);
    }

    void Phase3_OnCartClicked()
    {
        if (p3_cartClicked) return;
        p3_cartClicked = true;
    }

    IEnumerator Phase3_LoopHand()
    {
        while (p3_handActive)
        {
            yield return StartCoroutine(MoveHand(GetHandPositionForTarget(cowShopCartButton)));
            yield return new WaitForSeconds(handPauseDuration);
            yield return StartCoroutine(MoveHand(GetHandPositionForTarget(cowCenter)));
            yield return new WaitForSeconds(handPauseDuration);
        }
    }

    IEnumerator Phase3_MoveCartToMilk()
    {
        if (cowShopCartButton == null || milkCart == null) yield break;

        Vector3 cartStartWorld = cowShopCartButton.position;
        Vector3 milkTargetWorld = cowCenter.position;   // corn cart delivers TO the cow
        Vector3 cartOriginWorld = cowShopCartButton.position;

        float elapsed = 0f;
        float duration = cornCartToMilkSpeed > 0f
            ? Vector3.Distance(cartStartWorld, milkTargetWorld) / (cornCartToMilkSpeed * 100f)
            : 0f;

        if (duration > 0f)
        {
            SoundManager.Instance.PlaySFX("Swipe");
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                cowShopCartButton.position = Vector3.Lerp(cartStartWorld, milkTargetWorld, t);
                yield return null;
            }
        }
        cowShopCartButton.position = milkTargetWorld;

        yield return StartCoroutine(ShrinkCowBubble());

        if (cowIdle != null) cowIdle.SetActive(false);
        if (cowHappy != null) cowHappy.SetActive(true);
        yield return new WaitForSeconds(cowHappyAnimDuration);
        if (cowHappy != null) cowHappy.SetActive(false);
        if (cowIdle != null) cowIdle.SetActive(true);

        while (cowShopCartButton.localScale.x > 0.01f)
        {
            cowShopCartButton.localScale = Vector3.Lerp(
                cowShopCartButton.localScale, Vector3.zero,
                Time.deltaTime * popSpeed);
            yield return null;
        }
        cowShopCartButton.localScale = Vector3.zero;
        cowShopCartButton.gameObject.SetActive(false);

        milkCart.position = milkTargetWorld;
        milkCart.localScale = Vector3.zero;
        milkCart.gameObject.SetActive(true);

        while (Vector3.Distance(milkCart.localScale, milkCartOriginalScale) > 0.01f)
        {
            milkCart.localScale = Vector3.Lerp(
                milkCart.localScale, milkCartOriginalScale,
                Time.deltaTime * popSpeed);
            yield return null;
        }
        milkCart.localScale = milkCartOriginalScale;

        Vector3 milkStartWorld = milkCart.position;
        elapsed = 0f;
        duration = milkCartMoveSpeed > 0f
            ? Vector3.Distance(milkStartWorld, cartOriginWorld) / milkCartMoveSpeed
            : 0f;

        if (duration > 0f)
        {
            SoundManager.Instance.PlaySFX("Swipe");
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                milkCart.position = Vector3.Lerp(milkStartWorld, cartOriginWorld, t);
                yield return null;
            }
        }
        milkCart.position = cartOriginWorld;
        Debug.Log("MilkCart reached cart position!");
    }

    // =====================================================================
    // PHASE 4 COROUTINE — Grind the Corn
    // =====================================================================

    IEnumerator Phase4_Tutorial()
    {
        femaleCow.gameObject.SetActive(true);
        femaleCow.anchoredPosition = cowStartPos;
        yield return StartCoroutine(SlideCowIn());
        yield return StartCoroutine(PopBubble());

        yield return StartCoroutine(TypeText(msg4_PutCorn));
        yield return StartCoroutine(WaitOrSkipMain(1.5f));

        yield return StartCoroutine(ShrinkBubble());
        yield return StartCoroutine(SlideCowOut());

        Phase4_SetupCartListener();

        p4_handActive = true;
        handPointer.gameObject.SetActive(true);
        handPointer.position = GetHandPositionForTarget(grindCartButton);
        StartCoroutine(Phase4_LoopHand());

        yield return new WaitUntil(() => p4_cartClicked);

        p4_handActive = false;
        handPointer.gameObject.SetActive(false);

        p4_cartOriginWorld = grindCartButton.position;

        yield return StartCoroutine(Phase4_GrindSequence());

        femaleCow.gameObject.SetActive(true);
        femaleCow.anchoredPosition = cowStartPos;
        yield return StartCoroutine(SlideCowIn());
        yield return StartCoroutine(PopBubble());

        yield return StartCoroutine(TypeText(msg4_GotFlour));
        yield return StartCoroutine(WaitOrSkipMain(2f));

        yield return StartCoroutine(DeleteText());
        yield return StartCoroutine(TypeText(msg4_MakeCake));
        yield return StartCoroutine(WaitOrSkipMain(2f));

        yield return StartCoroutine(DeleteText());
        yield return StartCoroutine(TypeText(msg4_LetsGo));
        yield return StartCoroutine(WaitOrSkipMain(2f));

        yield return StartCoroutine(ShrinkBubble());
        yield return StartCoroutine(SlideCowOut());

        Debug.Log("Phase 4 complete!");

        yield return new WaitForSeconds(1f);
        if (grindTheCorns != null) grindTheCorns.SetActive(false);
        if (makeCake != null) makeCake.SetActive(true);
        StartCoroutine(Phase5_Tutorial());
    }

    IEnumerator Phase4_GrindSequence()
    {
        Vector3 cartStartWorld = grindCartButton.position;
        Vector3 grinderTipWorld = grinderTip.position;

        float elapsed = 0f;
        float duration = grindCartMoveSpeed > 0f
            ? Vector3.Distance(cartStartWorld, grinderTipWorld) / grindCartMoveSpeed
            : 0f;

        if (duration > 0f)
        {
            SoundManager.Instance.PlaySFX("Swipe");
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                grindCartButton.position = Vector3.Lerp(cartStartWorld, grinderTipWorld, t);
                yield return null;
            }
        }
        grindCartButton.position = grinderTipWorld;

        yield return StartCoroutine(ShrinkGrindBubble());

        while (grindCartButton.localScale.x > 0.01f)
        {
            grindCartButton.localScale = Vector3.Lerp(
                grindCartButton.localScale, Vector3.zero,
                Time.deltaTime * popSpeed);
            yield return null;
        }
        grindCartButton.localScale = Vector3.zero;
        grindCartButton.gameObject.SetActive(false);

        if (grinder2 != null) grinder2.SetActive(true);
        yield return new WaitForSecondsRealtime(1f);
        if (grinder2 != null) grinder2.SetActive(false);

        if (grinder1 != null) grinder1.SetActive(false);
        if (grinderAnim != null) grinderAnim.gameObject.SetActive(true);
        yield return null;

        if (flourBagRect != null) flourBagRect.gameObject.SetActive(true);
        if (flourBag1 != null) flourBag1.SetActive(false);
        yield return null;

        if (grinderAnim != null) { SoundManager.Instance.PlaySFX("Corngrind"); grinderAnim.Reset(); grinderAnim.Play(); }
        if (flourBagAnim != null) { flourBagAnim.Reset(); flourBagAnim.Play(); }

        float grinderDuration = (grinderAnim != null) ? (float)grinderAnim.frames.Length / grinderAnim.fps : 0f;
        float flourDuration = (flourBagAnim != null) ? (float)flourBagAnim.frames.Length / flourBagAnim.fps : 0f;
        yield return new WaitForSecondsRealtime(Mathf.Max(grinderDuration, flourDuration) + 0.1f);

        if (grinderAnim != null) grinderAnim.gameObject.SetActive(false);
        if (grinder2 != null) grinder2.SetActive(false);
        if (grinder1 != null) grinder1.SetActive(true);

        Vector3 flourStartWorld = flourBagRect.position;

        elapsed = 0f;
        duration = flourBagMoveSpeed > 0f
            ? Vector3.Distance(flourStartWorld, p4_cartOriginWorld) / flourBagMoveSpeed
            : 0f;

        if (duration > 0f)
        {
            SoundManager.Instance.PlaySFX("Swipe");
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                flourBagRect.position = Vector3.Lerp(flourStartWorld, p4_cartOriginWorld, t);
                yield return null;
            }
        }
        flourBagRect.position = p4_cartOriginWorld;
        Debug.Log("FlourBag reached cart position!");
    }

    // =====================================================================
    // PHASE 4 GRINDER BUBBLE HELPERS
    // =====================================================================

    IEnumerator PopGrindBubble()
    {
        if (grindThoughtBubble == null) yield break;

        var pulse = grindThoughtBubble.GetComponent("PulseAnimation") as MonoBehaviour;
        if (pulse != null) pulse.enabled = false;

        grindThoughtBubble.localScale = Vector3.zero;
        grindThoughtBubble.gameObject.SetActive(true);

        while (Vector3.Distance(grindThoughtBubble.localScale, grindBubbleOriginalScale) > 0.01f)
        {
            grindThoughtBubble.localScale = Vector3.Lerp(
                grindThoughtBubble.localScale, grindBubbleOriginalScale,
                Time.deltaTime * bubblePopSpeed);
            yield return null;
        }
        grindThoughtBubble.localScale = grindBubbleOriginalScale;

        if (pulse != null) pulse.enabled = true;
    }

    IEnumerator ShrinkGrindBubble()
    {
        if (grindThoughtBubble == null) yield break;

        var pulse = grindThoughtBubble.GetComponent("PulseAnimation") as MonoBehaviour;
        if (pulse != null) pulse.enabled = false;

        while (Vector3.Distance(grindThoughtBubble.localScale, Vector3.zero) > 0.01f)
        {
            grindThoughtBubble.localScale = Vector3.Lerp(
                grindThoughtBubble.localScale, Vector3.zero,
                Time.deltaTime * bubblePopSpeed);
            yield return null;
        }
        grindThoughtBubble.localScale = Vector3.zero;
        grindThoughtBubble.gameObject.SetActive(false);
    }

    // =====================================================================
    // PHASE 4 HELPERS
    // =====================================================================

    void Phase4_SetupCartListener()
    {
        if (grindCartButton == null) return;

        Image img = grindCartButton.GetComponent<Image>();
        if (img != null) img.raycastTarget = true;

        Button btn = grindCartButton.GetComponent<Button>();
        if (btn != null)
        {
            btn.interactable = true;
            btn.onClick.AddListener(Phase4_OnCartClicked);
            return;
        }

        EventTrigger trigger = grindCartButton.GetComponent<EventTrigger>()
                            ?? grindCartButton.gameObject.AddComponent<EventTrigger>();
        EventTrigger.Entry entry = new EventTrigger.Entry();
        entry.eventID = EventTriggerType.PointerClick;
        entry.callback.AddListener((data) => Phase4_OnCartClicked());
        trigger.triggers.Add(entry);
    }

    void Phase4_OnCartClicked()
    {
        if (p4_cartClicked) return;
        p4_cartClicked = true;
    }

    IEnumerator Phase4_LoopHand()
    {
        while (p4_handActive)
        {
            yield return StartCoroutine(MoveHand(GetHandPositionForTarget(grindCartButton)));
            yield return new WaitForSeconds(handPauseDuration);
            yield return StartCoroutine(MoveHand(GetHandPositionForTarget(grinderTip)));
            yield return new WaitForSeconds(handPauseDuration);
        }
    }

    // =====================================================================
    // PHASE 2 CHICKEN BUBBLE HELPERS
    // =====================================================================

    IEnumerator PopChickenBubble()
    {
        if (chickenThoughtBubble == null) yield break;

        var pulse = chickenThoughtBubble.GetComponent("PulseAnimation") as MonoBehaviour;
        if (pulse != null) pulse.enabled = false;

        chickenThoughtBubble.localScale = Vector3.zero;
        chickenThoughtBubble.gameObject.SetActive(true);

        while (Vector3.Distance(chickenThoughtBubble.localScale, chickenBubbleOriginalScale) > 0.01f)
        {
            chickenThoughtBubble.localScale = Vector3.Lerp(
                chickenThoughtBubble.localScale, chickenBubbleOriginalScale,
                Time.deltaTime * bubblePopSpeed);
            yield return null;
        }
        chickenThoughtBubble.localScale = chickenBubbleOriginalScale;

        if (pulse != null) pulse.enabled = true;
    }

    IEnumerator ShrinkChickenBubble()
    {
        if (chickenThoughtBubble == null) yield break;

        var pulse = chickenThoughtBubble.GetComponent("PulseAnimation") as MonoBehaviour;
        if (pulse != null) pulse.enabled = false;

        while (Vector3.Distance(chickenThoughtBubble.localScale, Vector3.zero) > 0.01f)
        {
            chickenThoughtBubble.localScale = Vector3.Lerp(
                chickenThoughtBubble.localScale, Vector3.zero,
                Time.deltaTime * bubblePopSpeed);
            yield return null;
        }
        chickenThoughtBubble.localScale = Vector3.zero;
        chickenThoughtBubble.gameObject.SetActive(false);
    }

    IEnumerator PopCowBubble()
    {
        if (cowThoughtBubble == null) yield break;

        var pulse = cowThoughtBubble.GetComponent("PulseAnimation") as MonoBehaviour;
        if (pulse != null) pulse.enabled = false;

        cowThoughtBubble.localScale = Vector3.zero;
        cowThoughtBubble.gameObject.SetActive(true);

        while (Vector3.Distance(cowThoughtBubble.localScale, cowBubbleOriginalScale) > 0.01f)
        {
            cowThoughtBubble.localScale = Vector3.Lerp(
                cowThoughtBubble.localScale, cowBubbleOriginalScale,
                Time.deltaTime * bubblePopSpeed);
            yield return null;
        }
        cowThoughtBubble.localScale = cowBubbleOriginalScale;

        if (pulse != null) pulse.enabled = true;
    }

    IEnumerator ShrinkCowBubble()
    {
        if (cowThoughtBubble == null) yield break;

        var pulse = cowThoughtBubble.GetComponent("PulseAnimation") as MonoBehaviour;
        if (pulse != null) pulse.enabled = false;

        while (Vector3.Distance(cowThoughtBubble.localScale, Vector3.zero) > 0.01f)
        {
            cowThoughtBubble.localScale = Vector3.Lerp(
                cowThoughtBubble.localScale, Vector3.zero,
                Time.deltaTime * bubblePopSpeed);
            yield return null;
        }
        cowThoughtBubble.localScale = Vector3.zero;
        cowThoughtBubble.gameObject.SetActive(false);
    }

    IEnumerator SlideCowIn()
    {
        float elapsed = 0f;
        femaleCow.anchoredPosition = cowStartPos;

        while (elapsed < cowSlideDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / cowSlideDuration);
            float smoothT = 1f - Mathf.Pow(1f - t, 3f);
            femaleCow.anchoredPosition = Vector2.LerpUnclamped(cowStartPos, cowEndPos, smoothT);
            yield return null;
        }

        femaleCow.anchoredPosition = cowEndPos;
    }

    IEnumerator SlideCowOut()
    {
        float elapsed = 0f;
        Vector2 startPos = femaleCow.anchoredPosition;

        while (elapsed < cowSlideDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / cowSlideDuration);
            float smoothT = Mathf.Pow(t, 3f);
            femaleCow.anchoredPosition = Vector2.LerpUnclamped(startPos, cowStartPos, smoothT);
            yield return null;
        }

        femaleCow.anchoredPosition = cowStartPos;
        femaleCow.gameObject.SetActive(false);
    }

    IEnumerator PopBubble()
    {
        SoundManager.Instance.PlaySFX("Panelpop2");
        thoughtBubble.localScale = Vector3.zero;
        while (Vector3.Distance(thoughtBubble.localScale, Vector3.one) > 0.01f)
        {
            thoughtBubble.localScale = Vector3.Lerp(
                thoughtBubble.localScale, Vector3.one,
                Time.deltaTime * bubblePopSpeed);
            yield return null;
        }
        thoughtBubble.localScale = Vector3.one;
    }

    IEnumerator ShrinkBubble()
    {
        SoundManager.Instance.PlaySFX("Panelpop2");
        while (Vector3.Distance(thoughtBubble.localScale, Vector3.zero) > 0.01f)
        {
            thoughtBubble.localScale = Vector3.Lerp(
                thoughtBubble.localScale, Vector3.zero,
                Time.deltaTime * bubblePopSpeed);
            yield return null;
        }
        thoughtBubble.localScale = Vector3.zero;
        thoughtText.text = "";
    }

    IEnumerator TypeText(string message)
    {
        if (lunaAnimator != null) lunaAnimator.Play();
        SoundManager.Instance.PlaySFXLoop("Typing");
        int skipAtStart = _skipCount;
        thoughtText.text = "";
        foreach (char c in message)
        {
            if (_skipCount != skipAtStart)
            {
                thoughtText.text = message;
                break;
            }
            thoughtText.text += c;
            yield return new WaitForSeconds(typingSpeed);
        }
        SoundManager.Instance.StopSFXLoop();
        if (lunaAnimator != null) lunaAnimator.Stop();
    }

    IEnumerator DeleteText()
    {
        while (thoughtText.text.Length > 0)
        {
            thoughtText.text = thoughtText.text.Substring(0, thoughtText.text.Length - 1);
            yield return new WaitForSeconds(typingSpeed * 0.5f);
        }
    }

    IEnumerator MoveHand(Vector3 target)
    {
        while (Vector3.Distance(handPointer.position, target) > 2f)
        {
            handPointer.position = Vector3.Lerp(
                handPointer.position, target,
                Time.deltaTime * handMoveSpeed);
            yield return null;
        }
        handPointer.position = target;
    }

    Vector3 GetRectCenter(RectTransform rt)
    {
        Vector3[] corners = new Vector3[4];
        rt.GetWorldCorners(corners);
        return (corners[0] + corners[1] + corners[2] + corners[3]) / 4f;
    }

    Vector3 GetHandPositionForTarget(RectTransform rt)
    {
        Vector3 targetCenter = GetRectCenter(rt);
        if (handTip == null) return targetCenter;
        Vector3 tipOffset = handTip.position - handPointer.position;
        return targetCenter - tipOffset;
    }

    // =====================================================================
    // PHASE 5 COROUTINE — Make Cake
    // =====================================================================

    IEnumerator Phase5_Tutorial()
    {
        if (femaleCow != null) femaleCow.gameObject.SetActive(false);

        // Show the bowl immediately at the start of Phase 5
        if (bowlRect != null) bowlRect.gameObject.SetActive(true);

        yield return StartCoroutine(PopTB());

        yield return StartCoroutine(TypeTB(msg5_Ingredients));
        yield return StartCoroutine(WaitOrSkip(2.5f));

        yield return StartCoroutine(DeleteTB());
        yield return StartCoroutine(TypeTB(msg5_NeedFlour));
        yield return StartCoroutine(WaitOrSkip(1.5f));

        Phase5_SetupListener(flourCartButton, () => p5_flourClicked = true, ref p5_flourClicked);
        p5_handActive = true;
        handPointer.gameObject.SetActive(true);
        handPointer.position = GetHandPositionForTarget(flourCartButton);
        StartCoroutine(Phase5_LoopHand(flourCartButton));

        yield return new WaitUntil(() => p5_flourClicked);

        p5_handActive = false;
        handPointer.gameObject.SetActive(false);

        yield return StartCoroutine(Phase5_MoveIngredientToBowl(flourCartButton));

        // Bowl: show and play first 2 frames for flour
        if (bowlRect != null) bowlRect.gameObject.SetActive(true);
        if (bowlAnim != null)
        {
            bowlAnim.PlayFrames(0, 2);
            yield return new WaitForSecondsRealtime(bowlAnim.GetDuration(2) + 0.1f);
        }

        yield return StartCoroutine(DeleteTB());
        yield return StartCoroutine(TypeTB(msg5_Nice));
        yield return StartCoroutine(WaitOrSkip(1.5f));

        yield return StartCoroutine(DeleteTB());
        yield return StartCoroutine(TypeTB(msg5_NeedMilk));
        yield return StartCoroutine(WaitOrSkip(1.5f));

        Phase5_SetupListener(milkCartButton, () => p5_milkClicked = true, ref p5_milkClicked);
        p5_handActive = true;
        handPointer.gameObject.SetActive(true);
        handPointer.position = GetHandPositionForTarget(milkCartButton);
        StartCoroutine(Phase5_LoopHand(milkCartButton));

        yield return new WaitUntil(() => p5_milkClicked);

        p5_handActive = false;
        handPointer.gameObject.SetActive(false);

        yield return StartCoroutine(Phase5_MoveIngredientToBowl(milkCartButton));

        // Bowl: play next 3 frames for milk (frames 2-4)
        if (bowlAnim != null)
        {
            bowlAnim.PlayFrames(2, 3);
            yield return new WaitForSecondsRealtime(bowlAnim.GetDuration(3) + 0.1f);
        }

        yield return StartCoroutine(DeleteTB());
        yield return StartCoroutine(TypeTB(msg5_GoodJob));
        yield return StartCoroutine(WaitOrSkip(1.5f));

        yield return StartCoroutine(DeleteTB());
        yield return StartCoroutine(TypeTB(msg5_NeedEggs));
        yield return StartCoroutine(WaitOrSkip(1.5f));

        Phase5_SetupListener(eggCartButton, () => p5_eggClicked = true, ref p5_eggClicked);
        p5_handActive = true;
        handPointer.gameObject.SetActive(true);
        handPointer.position = GetHandPositionForTarget(eggCartButton);
        StartCoroutine(Phase5_LoopHand(eggCartButton));

        yield return new WaitUntil(() => p5_eggClicked);

        p5_handActive = false;
        handPointer.gameObject.SetActive(false);

        yield return StartCoroutine(Phase5_MoveIngredientToBowl(eggCartButton));

        // Bowl: play all remaining frames for egg (from frame 5 to end)
        if (bowlAnim != null)
        {
            int remainingFrames = bowlAnim.frames.Length - 5;
            bowlAnim.PlayFromFrame(5);
            float bowlLength = remainingFrames > 0 ? bowlAnim.GetDuration(remainingFrames) : 0f;
            yield return new WaitForSecondsRealtime(bowlLength + 0.1f);
        }

        yield return new WaitForSecondsRealtime(1f);

        yield return StartCoroutine(DeleteTB());
        yield return StartCoroutine(TypeTB(msg5_NiceJob));
        yield return StartCoroutine(WaitOrSkip(1.5f));

        // --- CAKE ANIMATION: hide idle, show cake anim, keep it visible after ---
        if (cowIdleInCakeScene != null) cowIdleInCakeScene.SetActive(false);

        // Hide the bowl while the cow's cake-making animation plays
        if (bowlObjectToHide != null) bowlObjectToHide.SetActive(false);

        if (cowMakingCake != null)
        {
            cowMakingCake.SetActive(true);
            yield return null;
            if (cakemakingAnim != null)
            {
                cakemakingAnim.Reset();
                cakemakingAnim.Play();
                float cakeLength = (float)cakemakingAnim.frames.Length / cakemakingAnim.fps;
                yield return new WaitForSecondsRealtime(cakeLength + 0.1f);
                cakemakingAnim.Stop(); // Stop looping before win panel
            }
        }

        yield return StartCoroutine(ShrinkTB());

        // --- SHOW WIN PANEL & PAUSE GAME ---
        if (winPanel != null) winPanel.SetActive(true);
        SoundManager.Instance.PlaySFX("Tada");
        Time.timeScale = 0f;

        Debug.Log("Phase 5 complete! Tutorial finished.");
    }

    IEnumerator Phase5_MoveIngredientToBowl(RectTransform cart)
    {
        if (cart == null || bowlRect == null) yield break;

        Vector3 startWorld = cart.position;
        Vector3 endWorld = bowlRect.position;

        float elapsed = 0f;
        float duration = cakeCartMoveSpeed > 0f
            ? Vector3.Distance(startWorld, endWorld) / cakeCartMoveSpeed
            : 0f;

        if (duration > 0f)
        {
            SoundManager.Instance.PlaySFX("Swipe");
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                cart.position = Vector3.Lerp(startWorld, endWorld, t);
                yield return null;
            }
        }
        cart.position = endWorld;

        while (cart.localScale.x > 0.01f)
        {
            cart.localScale = Vector3.Lerp(cart.localScale, Vector3.zero, Time.deltaTime * popSpeed);
            yield return null;
        }
        cart.localScale = Vector3.zero;
        cart.gameObject.SetActive(false);
    }

    // =====================================================================
    // PHASE 5 HELPERS
    // =====================================================================

    void Phase5_SetupListener(RectTransform target, System.Action onClicked, ref bool flag)
    {
        if (target == null) return;
        flag = false;

        Image img = target.GetComponent<Image>();
        if (img != null) img.raycastTarget = true;

        Button btn = target.GetComponent<Button>();
        if (btn != null)
        {
            btn.interactable = true;
            btn.onClick.RemoveAllListeners();
            btn.onClick.AddListener(() => onClicked());
            return;
        }

        EventTrigger trigger = target.GetComponent<EventTrigger>()
                            ?? target.gameObject.AddComponent<EventTrigger>();
        trigger.triggers.Clear();
        EventTrigger.Entry entry = new EventTrigger.Entry();
        entry.eventID = EventTriggerType.PointerClick;
        entry.callback.AddListener((data) => onClicked());
        trigger.triggers.Add(entry);
    }

    IEnumerator Phase5_LoopHand(RectTransform ingredientButton)
    {
        while (p5_handActive)
        {
            yield return StartCoroutine(MoveHand(GetHandPositionForTarget(ingredientButton)));
            yield return new WaitForSeconds(handPauseDuration);
            yield return StartCoroutine(MoveHand(GetHandPositionForTarget(bowlRect)));
            yield return new WaitForSeconds(handPauseDuration);
        }
    }

    // =====================================================================
    // RESTART FUNCTIONALITY
    // =====================================================================

    public void RestartGame()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    // =====================================================================
    // PHASE 5 TB BUBBLE HELPERS
    // =====================================================================

    IEnumerator PopTB()
    {
        if (tb == null) yield break;
        SoundManager.Instance.PlaySFX("Panelpop2");
        tb.localScale = Vector3.zero;
        while (Vector3.Distance(tb.localScale, tbOriginalScale) > 0.01f)
        {
            tb.localScale = Vector3.Lerp(tb.localScale, tbOriginalScale, Time.deltaTime * bubblePopSpeed);
            yield return null;
        }
        tb.localScale = tbOriginalScale;
    }

    IEnumerator ShrinkTB()
    {
        if (tb == null) yield break;
        SoundManager.Instance.PlaySFX("Panelpop2");
        while (Vector3.Distance(tb.localScale, Vector3.zero) > 0.01f)
        {
            tb.localScale = Vector3.Lerp(tb.localScale, Vector3.zero, Time.deltaTime * bubblePopSpeed);
            yield return null;
        }
        tb.localScale = Vector3.zero;
        if (tbText != null) tbText.text = "";
    }

    public void OnSkipPressed()
    {
        _skipCount++;
    }

    IEnumerator WaitOrSkipMain(float seconds)
    {
        int countAtStart = _skipCount;
        float elapsed = 0f;
        while (elapsed < seconds && _skipCount == countAtStart)
        {
            elapsed += Time.deltaTime;
            yield return null;
        }
    }

    public void OnTBSkipPressed()
    {
        _tbSkipCount++;
    }

    IEnumerator WaitOrSkip(float seconds)
    {
        int countAtStart = _tbSkipCount;
        float elapsed = 0f;
        while (elapsed < seconds && _tbSkipCount == countAtStart)
        {
            elapsed += Time.deltaTime;
            yield return null;
        }
    }

    IEnumerator TypeTB(string message)
    {
        if (tbText == null) yield break;
        SoundManager.Instance.PlaySFXLoop("Typing");
        int skipAtStart = _tbSkipCount;
        tbText.text = "";
        foreach (char c in message)
        {
            if (_tbSkipCount != skipAtStart)
            {
                tbText.text = message;
                break;
            }
            tbText.text += c;
            yield return new WaitForSeconds(typingSpeed);
        }
        SoundManager.Instance.StopSFXLoop();
    }

    IEnumerator DeleteTB()
    {
        if (tbText == null) yield break;
        while (tbText.text.Length > 0)
        {
            tbText.text = tbText.text.Substring(0, tbText.text.Length - 1);
            yield return new WaitForSeconds(typingSpeed * 0.5f);
        }
    }
}