using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class OpenYouTubeButton : MonoBehaviour
{
    [System.Serializable]
    public class VideoEntry
    {
        public Button button;
        public string youtubeUrl; // e.g. "https://www.youtube.com/watch?v=VIDEO_ID"
    }

    [SerializeField] private List<VideoEntry> videoEntries;

    private void Start()
    {
        foreach (var entry in videoEntries)
        {
            string url = entry.youtubeUrl; // local copy to avoid closure bug
            entry.button.onClick.AddListener(() => Application.OpenURL(url));
        }
    }
}