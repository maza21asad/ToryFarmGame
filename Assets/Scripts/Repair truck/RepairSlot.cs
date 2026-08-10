//using UnityEngine;
//using UnityEngine.UI;
//using DG.Tweening;

//public class RepairSlot : MonoBehaviour
//{
//    [Header("Slot Identity")]
//    public string requiredPartID;

//    [Header("Slot Visuals")]
//    public Image repairedPartImage;

//    [Header("Dog Animation")]
//    public DogAnimationController dogController;

//    public bool isRepaired = false;
//    private Color originalColor;

//    void Awake()
//    {
//        if (repairedPartImage != null)
//        {
//            originalColor = repairedPartImage.color;
//            originalColor.a = 1f;
//            repairedPartImage.color = new Color(
//                originalColor.r,
//                originalColor.g,
//                originalColor.b,
//                0f
//            );
//        }
//    }

//    void Start()
//    {
//        isRepaired = false;
//    }

//    public void ShowPreview()
//    {
//        if (isRepaired) return;
//        if (repairedPartImage == null) return;

//        repairedPartImage.color = new Color(
//            originalColor.r,
//            originalColor.g,
//            originalColor.b,
//            0.5f
//        );
//    }

//    public void HidePreview()
//    {
//        if (isRepaired) return;
//        if (repairedPartImage == null) return;

//        repairedPartImage.color = new Color(
//            originalColor.r,
//            originalColor.g,
//            originalColor.b,
//            0f
//        );
//    }

//    public void Repair(RepairDrag drag)
//    {
//        isRepaired = true;

//        if (repairedPartImage != null)
//        {
//            repairedPartImage.DOFade(1f, 0.4f).SetEase(Ease.OutQuad);
//            repairedPartImage.transform.localScale = Vector3.one * 0.5f;
//            repairedPartImage.transform
//                .DOScale(1f, 0.4f)
//                .SetEase(Ease.OutBack);
//        }

//        drag.gameObject.SetActive(false);
//        Debug.Log($"[RepairSlot] SUCCESS - {requiredPartID}!");

//        if (RepairGameManager.Instance != null)
//            RepairGameManager.Instance.OnSlotRepaired();
//    }

//    public void WrongDrop()
//    {
//        transform.DOKill();
//        transform.DOShakePosition(0.5f, 20f, 15)
//            .SetEase(Ease.OutElastic);

//        // ✅ Play wrong dog animation
//        if (dogController != null)
//            dogController.PlayWrong();

//        Debug.Log($"[RepairSlot] Wrong part on {gameObject.name}!");
//    }

//    public void ResetSlot()
//    {
//        isRepaired = false;

//        if (repairedPartImage != null)
//        {
//            repairedPartImage.color = new Color(
//                originalColor.r,
//                originalColor.g,
//                originalColor.b,
//                0f
//            );
//            repairedPartImage.transform.localScale = Vector3.one;
//        }
//    }
//}

using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

public class RepairSlot : MonoBehaviour
{
    [Header("Slot Identity")]
    public string requiredPartID;

    [Header("Slot Visuals")]
    public Image repairedPartImage;

    [Header("Dog Animation")]
    public DogAnimationController dogController;

    public bool isRepaired = false;
    private Color originalColor;

    void Awake()
    {
        if (repairedPartImage != null)
        {
            originalColor = repairedPartImage.color;
            originalColor.a = 1f;
            repairedPartImage.color = new Color(
                originalColor.r,
                originalColor.g,
                originalColor.b,
                0f
            );
        }
    }

    void Start()
    {
        isRepaired = false;
    }

    public void ShowPreview()
    {
        if (isRepaired) return;
        if (repairedPartImage == null) return;

        repairedPartImage.color = new Color(
            originalColor.r,
            originalColor.g,
            originalColor.b,
            0.5f
        );
    }

    public void HidePreview()
    {
        if (isRepaired) return;
        if (repairedPartImage == null) return;

        repairedPartImage.color = new Color(
            originalColor.r,
            originalColor.g,
            originalColor.b,
            0f
        );
    }

    public void Repair(RepairDrag drag)
    {
        isRepaired = true;

        if (repairedPartImage != null)
        {
            repairedPartImage.DOFade(1f, 0.4f).SetEase(Ease.OutQuad);
            repairedPartImage.transform.localScale = Vector3.one * 0.5f;
            repairedPartImage.transform
                .DOScale(1f, 0.4f)
                .SetEase(Ease.OutBack);
        }

        drag.gameObject.SetActive(false);
        SoundManager.Instance.PlaySFX("Repair");
        SoundManager.Instance.PlaySFX("Satisfy1");
        Debug.Log($"[RepairSlot] SUCCESS - {requiredPartID}!");

        if (RepairGameManager.Instance != null)
            RepairGameManager.Instance.OnSlotRepaired();
    }

    public void WrongDrop()
    {
        transform.DOKill();
        transform.DOShakePosition(0.5f, 20f, 15)
            .SetEase(Ease.OutElastic);

        // Use singleton so no per-slot Inspector wiring is needed.
        if (DogAnimationController.Instance != null)
            DogAnimationController.Instance.PlayWrong();
        else if (dogController != null)
            dogController.PlayWrong();
        SoundManager.Instance.PlaySFX("NoNo");
        Debug.Log($"[RepairSlot] Wrong part on {gameObject.name}!");
    }

    public void ResetSlot()
    {
        isRepaired = false;

        if (repairedPartImage != null)
        {
            repairedPartImage.color = new Color(
                originalColor.r,
                originalColor.g,
                originalColor.b,
                0f
            );
            repairedPartImage.transform.localScale = Vector3.one;
        }
    }
}