using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "AudioData", menuName = "Audio/AudioData")]
public class AudioData : ScriptableObject
{
    [System.Serializable]
    public class MusicTrack
    {
        public string name;
        public AudioClip clip;
        [Range(0f, 1f)] public float volume = 1f;
        public bool loop = true;
    }

    [System.Serializable]
    public class SFXClip
    {
        public string name;
        public AudioClip clip;
        [Range(0f, 1f)] public float volume = 1f;
        [Range(0.5f, 2f)] public float pitch = 1f;
    }

    [System.Serializable]
    public class MusicScene
    {
        public string sceneName;
        public List<MusicTrack> tracks = new();
    }

    [System.Serializable]
    public class SFXScene
    {
        public string sceneName;
        public List<SFXClip> clips = new();
    }

    [Header("Music")]
    public List<MusicScene> music = new();

    [Header("SFX")]
    public List<SFXScene> sfx = new();
}