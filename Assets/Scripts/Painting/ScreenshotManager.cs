using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.IO;
using DG.Tweening;

public class ScreenshotManager : MonoBehaviour
{
    [Header("Assign the PaintableImage RectTransform")]
    public RectTransform paintableArea;

    [Header("Download Button")]
    public Button downloadButton;

    [Header("Feedback UI (optional)")]
    public GameObject savedFeedbackText; // "Image Saved!" text

    void Start()
    {
        if (downloadButton != null)
            downloadButton.onClick.AddListener(CaptureAndSave);

        if (savedFeedbackText != null)
            savedFeedbackText.SetActive(false);
    }

    public void CaptureAndSave()
    {
        StartCoroutine(CaptureRoutine());
    }

    IEnumerator CaptureRoutine()
    {
        yield return new WaitForEndOfFrame();

        Vector3[] corners = new Vector3[4];
        paintableArea.GetWorldCorners(corners);

        Canvas canvas = paintableArea.GetComponentInParent<Canvas>().rootCanvas;
        Camera cam = (canvas.renderMode == RenderMode.ScreenSpaceOverlay)
                     ? null
                     : (canvas.worldCamera != null ? canvas.worldCamera : Camera.main);

        Vector2 bottomLeft = RectTransformUtility.WorldToScreenPoint(cam, corners[0]);
        Vector2 topRight = RectTransformUtility.WorldToScreenPoint(cam, corners[2]);

        int x = Mathf.Clamp(Mathf.RoundToInt(bottomLeft.x), 0, Screen.width);
        int y = Mathf.Clamp(Mathf.RoundToInt(bottomLeft.y), 0, Screen.height);
        int width = Mathf.Clamp(Mathf.RoundToInt(topRight.x - bottomLeft.x), 1, Screen.width - x);
        int height = Mathf.Clamp(Mathf.RoundToInt(topRight.y - bottomLeft.y), 1, Screen.height - y);

        Debug.Log($"[Screenshot] Capturing rect: x={x} y={y} w={width} h={height}");

        Texture2D screenshot = new Texture2D(width, height, TextureFormat.RGB24, false);
        screenshot.ReadPixels(new Rect(x, y, width, height), 0, 0);
        screenshot.Apply();

        byte[] pngBytes = screenshot.EncodeToPNG();
        Destroy(screenshot);

        string filePath = GetSavePath();
        File.WriteAllBytes(filePath, pngBytes);

        Debug.Log($"[Screenshot] Saved to: {filePath}");

        ShowSavedFeedback();

#if UNITY_ANDROID && !UNITY_EDITOR
        RefreshAndroidGallery(filePath);
#endif
    }

    string GetSavePath()
    {
        string fileName = "ColoringImage_" +
                          System.DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss") +
                          ".png";

#if UNITY_EDITOR
        // Always save to Desktop when running inside the Unity Editor
        string desktop = System.Environment.GetFolderPath(
                         System.Environment.SpecialFolder.Desktop);
        return Path.Combine(desktop, fileName);

#elif UNITY_ANDROID
        string folder = "/storage/emulated/0/Pictures/ColoringApp/";
        if (!Directory.Exists(folder))
            Directory.CreateDirectory(folder);
        return Path.Combine(folder, fileName);

#elif UNITY_IOS
        return Path.Combine(Application.persistentDataPath, fileName);

#else
        string desktop = System.Environment.GetFolderPath(
                         System.Environment.SpecialFolder.Desktop);
        return Path.Combine(desktop, fileName);
#endif
    }

    void ShowSavedFeedback()
    {
        if (savedFeedbackText == null) return;

        savedFeedbackText.SetActive(true);

        savedFeedbackText.transform.localScale = Vector3.zero;
        savedFeedbackText.transform
            .DOScale(1f, 0.3f)
            .SetEase(Ease.OutBack)
            .OnComplete(() =>
            {
                DOVirtual.DelayedCall(2f, () =>
                {
                    savedFeedbackText.transform
                        .DOScale(0f, 0.2f)
                        .SetEase(Ease.InBack)
                        .OnComplete(() => savedFeedbackText.SetActive(false));
                });
            });
        SoundManager.Instance.PlaySFX("PhotoTaken");
    }

#if UNITY_ANDROID && !UNITY_EDITOR
    void RefreshAndroidGallery(string filePath)
    {
        using (AndroidJavaClass mediaScannerConnection =
               new AndroidJavaClass("android.media.MediaScannerConnection"))
        {
            using (AndroidJavaClass unityPlayer =
                   new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
            {
                AndroidJavaObject currentActivity =
                    unityPlayer.GetStatic<AndroidJavaObject>("currentActivity");

                mediaScannerConnection.CallStatic("scanFile",
                    currentActivity,
                    new string[] { filePath },
                    null,
                    null);
            }
        }
        Debug.Log("[Screenshot] Android gallery refreshed!");
    }
#endif
}