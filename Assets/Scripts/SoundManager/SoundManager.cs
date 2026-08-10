//////////using System.Collections;
//////////using System.Collections.Generic;
//////////using UnityEngine;

//////////public class SoundManager : MonoBehaviour
//////////{
//////////    public static SoundManager Instance { get; private set; }

//////////    [Header("Audio Data")]
//////////    [SerializeField] private AudioData audioData;

//////////    [Header("Audio Sources")]
//////////    [SerializeField] private AudioSource musicSource;
//////////    [SerializeField] private AudioSource sfxSource;

//////////    // ── Persistent Settings Keys ──────────────────────────────────────────
//////////    private const string KEY_MASTER_VOL = "MasterVolume";
//////////    private const string KEY_MUSIC_VOL = "MusicVolume";
//////////    private const string KEY_SFX_VOL = "SFXVolume";
//////////    private const string KEY_MUSIC_ON = "MusicOn";
//////////    private const string KEY_SFX_ON = "SFXOn";

//////////    // ── Runtime State ─────────────────────────────────────────────────────
//////////    public float MasterVolume { get; private set; }
//////////    public float MusicVolume { get; private set; }
//////////    public float SFXVolume { get; private set; }
//////////    public bool IsMusicOn { get; private set; }
//////////    public bool IsSFXOn { get; private set; }

//////////    private Dictionary<string, AudioData.MusicTrack> _musicDict = new();
//////////    private Dictionary<string, AudioData.SFXClip> _sfxDict = new();

//////////    // ── Events (UI can subscribe) ─────────────────────────────────────────
//////////    public event System.Action OnSettingsChanged;

//////////    // ─────────────────────────────────────────────────────────────────────
//////////    // Lifecycle
//////////    // ─────────────────────────────────────────────────────────────────────

//////////    private void Awake()
//////////    {
//////////        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
//////////        Instance = this;
//////////        DontDestroyOnLoad(gameObject);

//////////        BuildDictionaries();
//////////        LoadSettings();
//////////        ApplyVolumes();
//////////    }

//////////    //private void BuildDictionaries()
//////////    //{
//////////    //    foreach (var track in AudioData.musicTracks)
//////////    //        if (!string.IsNullOrEmpty(track.name)) _musicDict[track.name] = track;

//////////    //    foreach (var sfx in AudioData.sfxClips)
//////////    //        if (!string.IsNullOrEmpty(sfx.name)) _sfxDict[sfx.name] = sfx;
//////////    //}

//////////    private void BuildDictionaries()
//////////    {
//////////        foreach (var scene in audioData.music)
//////////            foreach (var track in scene.tracks)
//////////                if (!string.IsNullOrEmpty(track.name)) _musicDict[track.name] = track;

//////////        foreach (var scene in audioData.sfx)
//////////            foreach (var clip in scene.clips)
//////////                if (!string.IsNullOrEmpty(clip.name)) _sfxDict[clip.name] = clip;
//////////    }

//////////    // ─────────────────────────────────────────────────────────────────────
//////////    // Music
//////////    // ─────────────────────────────────────────────────────────────────────

//////////    public void PlayMusic(string trackName)
//////////    {
//////////        if (!_musicDict.TryGetValue(trackName, out var track)) { Debug.LogWarning($"[SoundManager] Music not found: {trackName}"); return; }
//////////        if (musicSource.clip == track.clip && musicSource.isPlaying) return;

//////////        musicSource.clip = track.clip;
//////////        musicSource.loop = track.loop;
//////////        musicSource.volume = IsMusicOn ? track.volume * MasterVolume * MusicVolume : 0f;
//////////        musicSource.Play();
//////////    }

//////////    public void PlayMusicWithFade(string trackName, float fadeDuration = 1f)
//////////    {
//////////        StartCoroutine(FadeAndSwitch(trackName, fadeDuration));
//////////    }

//////////    private IEnumerator FadeAndSwitch(string trackName, float duration)
//////////    {
//////////        // Fade out
//////////        float startVol = musicSource.volume;
//////////        for (float t = 0; t < duration; t += Time.deltaTime)
//////////        {
//////////            musicSource.volume = Mathf.Lerp(startVol, 0f, t / duration);
//////////            yield return null;
//////////        }

//////////        PlayMusic(trackName);
//////////    }

//////////    public void StopMusic() => musicSource.Stop();
//////////    public void PauseMusic() => musicSource.Pause();
//////////    public void ResumeMusic() => musicSource.UnPause();

//////////    // ─────────────────────────────────────────────────────────────────────
//////////    // SFX
//////////    // ─────────────────────────────────────────────────────────────────────

//////////    public void PlaySFX(string sfxName)
//////////    {
//////////        if (!IsSFXOn) return;
//////////        if (!_sfxDict.TryGetValue(sfxName, out var sfx)) { Debug.LogWarning($"[SoundManager] SFX not found: {sfxName}"); return; }

//////////        sfxSource.pitch = sfx.pitch;
//////////        sfxSource.PlayOneShot(sfx.clip, sfx.volume * MasterVolume * SFXVolume);
//////////    }

//////////    // ─────────────────────────────────────────────────────────────────────
//////////    // Volume Controls  ← hook these directly to UI Sliders / Buttons
//////////    // ─────────────────────────────────────────────────────────────────────

//////////    public void SetMasterVolume(float value)
//////////    {
//////////        MasterVolume = Mathf.Clamp01(value);
//////////        PlayerPrefs.SetFloat(KEY_MASTER_VOL, MasterVolume);
//////////        ApplyVolumes();
//////////        OnSettingsChanged?.Invoke();
//////////    }

//////////    public void SetMusicVolume(float value)
//////////    {
//////////        MusicVolume = Mathf.Clamp01(value);
//////////        PlayerPrefs.SetFloat(KEY_MUSIC_VOL, MusicVolume);
//////////        ApplyVolumes();
//////////        OnSettingsChanged?.Invoke();
//////////    }

//////////    public void SetSFXVolume(float value)
//////////    {
//////////        SFXVolume = Mathf.Clamp01(value);
//////////        PlayerPrefs.SetFloat(KEY_SFX_VOL, SFXVolume);
//////////        OnSettingsChanged?.Invoke();
//////////    }

//////////    public void ToggleMusic()
//////////    {
//////////        IsMusicOn = !IsMusicOn;
//////////        PlayerPrefs.SetInt(KEY_MUSIC_ON, IsMusicOn ? 1 : 0);
//////////        musicSource.volume = IsMusicOn ? MusicVolume * MasterVolume : 0f;
//////////        OnSettingsChanged?.Invoke();
//////////    }

//////////    public void ToggleSFX()
//////////    {
//////////        IsSFXOn = !IsSFXOn;
//////////        PlayerPrefs.SetInt(KEY_SFX_ON, IsSFXOn ? 1 : 0);
//////////        OnSettingsChanged?.Invoke();
//////////    }

//////////    // ─────────────────────────────────────────────────────────────────────
//////////    // Internals
//////////    // ─────────────────────────────────────────────────────────────────────

//////////    private void LoadSettings()
//////////    {
//////////        MasterVolume = PlayerPrefs.GetFloat(KEY_MASTER_VOL, 1f);
//////////        MusicVolume = PlayerPrefs.GetFloat(KEY_MUSIC_VOL, 1f);
//////////        SFXVolume = PlayerPrefs.GetFloat(KEY_SFX_VOL, 1f);
//////////        IsMusicOn = PlayerPrefs.GetInt(KEY_MUSIC_ON, 1) == 1;
//////////        IsSFXOn = PlayerPrefs.GetInt(KEY_SFX_ON, 1) == 1;
//////////    }

//////////    private void ApplyVolumes()
//////////    {
//////////        if (musicSource.isPlaying)
//////////            musicSource.volume = IsMusicOn ? MusicVolume * MasterVolume : 0f;
//////////    }
//////////}

////////using System.Collections;
////////using System.Collections.Generic;
////////using UnityEngine;

////////public class SoundManager : MonoBehaviour
////////{
////////    public static SoundManager Instance { get; private set; }

////////    [Header("Audio Data")]
////////    [SerializeField] private AudioData audioData;

////////    [Header("Audio Sources")]
////////    [SerializeField] private AudioSource musicSource;
////////    [SerializeField] private AudioSource sfxSource;
////////    [SerializeField] private AudioSource sfxLoopSource;

////////    private const string KEY_MASTER_VOL = "MasterVolume";
////////    private const string KEY_MUSIC_VOL = "MusicVolume";
////////    private const string KEY_SFX_VOL = "SFXVolume";
////////    private const string KEY_MUSIC_ON = "MusicOn";
////////    private const string KEY_SFX_ON = "SFXOn";

////////    public float MasterVolume { get; private set; }
////////    public float MusicVolume { get; private set; }
////////    public float SFXVolume { get; private set; }
////////    public bool IsMusicOn { get; private set; }
////////    public bool IsSFXOn { get; private set; }

////////    private Dictionary<string, AudioData.MusicTrack> _musicDict = new();
////////    private Dictionary<string, AudioData.SFXClip> _sfxDict = new();

////////    public event System.Action OnSettingsChanged;

////////    private void Awake()
////////    {
////////        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
////////        Instance = this;
////////        DontDestroyOnLoad(gameObject);

////////        BuildDictionaries();
////////        LoadSettings();
////////        ApplyVolumes();
////////    }

////////    private void BuildDictionaries()
////////    {
////////        foreach (var scene in audioData.music)
////////            foreach (var track in scene.tracks)
////////                if (!string.IsNullOrEmpty(track.name)) _musicDict[track.name] = track;

////////        foreach (var scene in audioData.sfx)
////////            foreach (var clip in scene.clips)
////////                if (!string.IsNullOrEmpty(clip.name)) _sfxDict[clip.name] = clip;
////////    }

////////    // ─────────────────────────────────────────────────────────────────────
////////    // Music
////////    // ─────────────────────────────────────────────────────────────────────

////////    public void PlayMusic(string trackName)
////////    {
////////        if (!_musicDict.TryGetValue(trackName, out var track)) { Debug.LogWarning($"[SoundManager] Music not found: {trackName}"); return; }
////////        if (musicSource.clip == track.clip && musicSource.isPlaying) return;

////////        musicSource.clip = track.clip;
////////        musicSource.loop = track.loop;
////////        musicSource.volume = IsMusicOn ? track.volume * MasterVolume * MusicVolume : 0f;
////////        musicSource.Play();
////////    }

////////    public void PlayMusicWithFade(string trackName, float fadeDuration = 1f)
////////    {
////////        StartCoroutine(FadeAndSwitch(trackName, fadeDuration));
////////    }

////////    private IEnumerator FadeAndSwitch(string trackName, float duration)
////////    {
////////        float startVol = musicSource.volume;
////////        for (float t = 0; t < duration; t += Time.deltaTime)
////////        {
////////            musicSource.volume = Mathf.Lerp(startVol, 0f, t / duration);
////////            yield return null;
////////        }
////////        PlayMusic(trackName);
////////    }

////////    public void StopMusic() => musicSource.Stop();
////////    public void PauseMusic() => musicSource.Pause();
////////    public void ResumeMusic() => musicSource.UnPause();

////////    // ─────────────────────────────────────────────────────────────────────
////////    // SFX
////////    // ─────────────────────────────────────────────────────────────────────

////////    public void PlaySFX(string sfxName)
////////    {
////////        if (!IsSFXOn) return;
////////        if (!_sfxDict.TryGetValue(sfxName, out var sfx)) { Debug.LogWarning($"[SoundManager] SFX not found: {sfxName}"); return; }

////////        sfxSource.pitch = sfx.pitch;
////////        sfxSource.PlayOneShot(sfx.clip, sfx.volume * MasterVolume * SFXVolume);
////////    }

////////    public void PlaySFXLoop(string sfxName)
////////    {
////////        if (!IsSFXOn) return;
////////        if (!_sfxDict.TryGetValue(sfxName, out var sfx)) { Debug.LogWarning($"[SoundManager] SFX not found: {sfxName}"); return; }

////////        sfxLoopSource.clip = sfx.clip;
////////        sfxLoopSource.volume = sfx.volume * MasterVolume * SFXVolume;
////////        sfxLoopSource.pitch = sfx.pitch;
////////        sfxLoopSource.loop = true;
////////        sfxLoopSource.Play();
////////    }

////////    public void StopSFXLoop()
////////    {
////////        sfxLoopSource.Stop();
////////        sfxLoopSource.clip = null;
////////    }

////////    // ─────────────────────────────────────────────────────────────────────
////////    // Volume Controls
////////    // ─────────────────────────────────────────────────────────────────────

////////    public void SetMasterVolume(float value)
////////    {
////////        MasterVolume = Mathf.Clamp01(value);
////////        PlayerPrefs.SetFloat(KEY_MASTER_VOL, MasterVolume);
////////        ApplyVolumes();
////////        OnSettingsChanged?.Invoke();
////////    }

////////    public void SetMusicVolume(float value)
////////    {
////////        MusicVolume = Mathf.Clamp01(value);
////////        PlayerPrefs.SetFloat(KEY_MUSIC_VOL, MusicVolume);
////////        ApplyVolumes();
////////        OnSettingsChanged?.Invoke();
////////    }

////////    public void SetSFXVolume(float value)
////////    {
////////        SFXVolume = Mathf.Clamp01(value);
////////        PlayerPrefs.SetFloat(KEY_SFX_VOL, SFXVolume);
////////        OnSettingsChanged?.Invoke();
////////    }

////////    public void ToggleMusic()
////////    {
////////        IsMusicOn = !IsMusicOn;
////////        PlayerPrefs.SetInt(KEY_MUSIC_ON, IsMusicOn ? 1 : 0);
////////        musicSource.volume = IsMusicOn ? MusicVolume * MasterVolume : 0f;
////////        OnSettingsChanged?.Invoke();
////////    }

////////    //public void ToggleSFX()
////////    //{
////////    //    IsSFXOn = !IsSFXOn;
////////    //    PlayerPrefs.SetInt(KEY_SFX_ON, IsSFXOn ? 1 : 0);
////////    //    OnSettingsChanged?.Invoke();
////////    //}

////////    public void ToggleSFX()
////////    {
////////        IsSFXOn = !IsSFXOn;
////////        PlayerPrefs.SetInt(KEY_SFX_ON, IsSFXOn ? 1 : 0);
////////        if (!IsSFXOn) StopSFXLoop();
////////        OnSettingsChanged?.Invoke();
////////    }

////////    // ─────────────────────────────────────────────────────────────────────
////////    // Internals
////////    // ─────────────────────────────────────────────────────────────────────

////////    private void LoadSettings()
////////    {
////////        MasterVolume = PlayerPrefs.GetFloat(KEY_MASTER_VOL, 1f);
////////        MusicVolume = PlayerPrefs.GetFloat(KEY_MUSIC_VOL, 1f);
////////        SFXVolume = PlayerPrefs.GetFloat(KEY_SFX_VOL, 1f);
////////        IsMusicOn = PlayerPrefs.GetInt(KEY_MUSIC_ON, 1) == 1;
////////        IsSFXOn = PlayerPrefs.GetInt(KEY_SFX_ON, 1) == 1;
////////    }

////////    private void ApplyVolumes()
////////    {
////////        if (musicSource.isPlaying)
////////            musicSource.volume = IsMusicOn ? MusicVolume * MasterVolume : 0f;
////////    }
////////}

//////using System.Collections;
//////using System.Collections.Generic;
//////using UnityEngine;

//////public class SoundManager : MonoBehaviour
//////{
//////    public static SoundManager Instance { get; private set; }

//////    [Header("Audio Data")]
//////    [SerializeField] private AudioData audioData;

//////    [Header("Audio Sources")]
//////    [SerializeField] private AudioSource musicSource;
//////    [SerializeField] private AudioSource sfxSource;
//////    [SerializeField] private AudioSource sfxLoopSource;

//////    private const string KEY_MASTER_VOL = "MasterVolume";
//////    private const string KEY_MUSIC_VOL = "MusicVolume";
//////    private const string KEY_SFX_VOL = "SFXVolume";
//////    private const string KEY_MUSIC_ON = "MusicOn";
//////    private const string KEY_SFX_ON = "SFXOn";

//////    public float MasterVolume { get; private set; }
//////    public float MusicVolume { get; private set; }
//////    public float SFXVolume { get; private set; }
//////    public bool IsMusicOn { get; private set; }
//////    public bool IsSFXOn { get; private set; }

//////    private Dictionary<string, AudioData.MusicTrack> _musicDict = new();
//////    private Dictionary<string, AudioData.SFXClip> _sfxDict = new();

//////    public event System.Action OnSettingsChanged;

//////    private void Awake()
//////    {
//////        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
//////        Instance = this;
//////        DontDestroyOnLoad(gameObject);

//////        BuildDictionaries();
//////        LoadSettings();
//////        ApplyVolumes();
//////    }

//////    private void BuildDictionaries()
//////    {
//////        foreach (var scene in audioData.music)
//////            foreach (var track in scene.tracks)
//////                if (!string.IsNullOrEmpty(track.name)) _musicDict[track.name] = track;

//////        foreach (var scene in audioData.sfx)
//////            foreach (var clip in scene.clips)
//////                if (!string.IsNullOrEmpty(clip.name)) _sfxDict[clip.name] = clip;
//////    }

//////    // ─────────────────────────────────────────────────────────────────────
//////    // Music
//////    // ─────────────────────────────────────────────────────────────────────

//////    public void PlayMusic(string trackName)
//////    {
//////        if (!_musicDict.TryGetValue(trackName, out var track)) { Debug.LogWarning($"[SoundManager] Music not found: {trackName}"); return; }
//////        if (musicSource.clip == track.clip && musicSource.isPlaying) return;

//////        musicSource.clip = track.clip;
//////        musicSource.loop = track.loop;
//////        musicSource.volume = IsMusicOn ? track.volume * MasterVolume * MusicVolume : 0f;
//////        musicSource.Play();
//////    }

//////    public void PlayMusicWithFade(string trackName, float fadeDuration = 1f)
//////    {
//////        StartCoroutine(FadeAndSwitch(trackName, fadeDuration));
//////    }

//////    private IEnumerator FadeAndSwitch(string trackName, float duration)
//////    {
//////        float startVol = musicSource.volume;
//////        for (float t = 0; t < duration; t += Time.deltaTime)
//////        {
//////            musicSource.volume = Mathf.Lerp(startVol, 0f, t / duration);
//////            yield return null;
//////        }
//////        PlayMusic(trackName);
//////    }

//////    public void StopMusic() => musicSource.Stop();
//////    public void PauseMusic() => musicSource.Pause();
//////    public void ResumeMusic() => musicSource.UnPause();

//////    // ─────────────────────────────────────────────────────────────────────
//////    // SFX
//////    // ─────────────────────────────────────────────────────────────────────

//////    //public void PlaySFX(string sfxName)
//////    //{
//////    //    if (!IsSFXOn) return;
//////    //    if (!_sfxDict.TryGetValue(sfxName, out var sfx)) { Debug.LogWarning($"[SoundManager] SFX not found: {sfxName}"); return; }

//////    //    sfxSource.pitch = sfx.pitch;
//////    //    sfxSource.PlayOneShot(sfx.clip, sfx.volume * MasterVolume * SFXVolume);
//////    //}

//////    public void PlaySFX(string sfxName)
//////    {
//////        if (!IsSFXOn) return;
//////        if (!_sfxDict.TryGetValue(sfxName, out var sfx)) { Debug.LogWarning($"[SoundManager] SFX not found: {sfxName}"); return; }

//////        AudioSource temp = gameObject.AddComponent<AudioSource>();
//////        temp.clip = sfx.clip;
//////        temp.volume = sfx.volume * MasterVolume * SFXVolume;
//////        temp.pitch = sfx.pitch;
//////        temp.PlayOneShot(sfx.clip, sfx.volume * MasterVolume * SFXVolume);
//////        Destroy(temp, sfx.clip.length / Mathf.Abs(sfx.pitch) + 0.1f);
//////    }

//////    public void PlaySFXLoop(string sfxName)
//////    {
//////        if (!IsSFXOn) return;
//////        if (!_sfxDict.TryGetValue(sfxName, out var sfx)) { Debug.LogWarning($"[SoundManager] SFX not found: {sfxName}"); return; }

//////        sfxLoopSource.clip = sfx.clip;
//////        sfxLoopSource.volume = sfx.volume * MasterVolume * SFXVolume;
//////        sfxLoopSource.pitch = sfx.pitch;
//////        sfxLoopSource.loop = true;
//////        sfxLoopSource.Play();
//////    }

//////    public void StopSFXLoop()
//////    {
//////        sfxLoopSource.Stop();
//////        sfxLoopSource.clip = null;
//////    }

//////    // ─────────────────────────────────────────────────────────────────────
//////    // Volume Controls
//////    // ─────────────────────────────────────────────────────────────────────

//////    public void SetMasterVolume(float value)
//////    {
//////        MasterVolume = Mathf.Clamp01(value);
//////        PlayerPrefs.SetFloat(KEY_MASTER_VOL, MasterVolume);
//////        ApplyVolumes();
//////        OnSettingsChanged?.Invoke();
//////    }

//////    public void SetMusicVolume(float value)
//////    {
//////        MusicVolume = Mathf.Clamp01(value);
//////        PlayerPrefs.SetFloat(KEY_MUSIC_VOL, MusicVolume);
//////        ApplyVolumes();
//////        OnSettingsChanged?.Invoke();
//////    }

//////    public void SetSFXVolume(float value)
//////    {
//////        SFXVolume = Mathf.Clamp01(value);
//////        PlayerPrefs.SetFloat(KEY_SFX_VOL, SFXVolume);
//////        OnSettingsChanged?.Invoke();
//////    }

//////    public void ToggleMusic()
//////    {
//////        IsMusicOn = !IsMusicOn;
//////        PlayerPrefs.SetInt(KEY_MUSIC_ON, IsMusicOn ? 1 : 0);
//////        musicSource.volume = IsMusicOn ? MusicVolume * MasterVolume : 0f;
//////        OnSettingsChanged?.Invoke();
//////    }

//////    public void ToggleSFX()
//////    {
//////        IsSFXOn = !IsSFXOn;
//////        PlayerPrefs.SetInt(KEY_SFX_ON, IsSFXOn ? 1 : 0);
//////        if (!IsSFXOn)
//////        {
//////            sfxLoopSource.Pause();
//////        }
//////        else if (sfxLoopSource.clip != null)
//////        {
//////            sfxLoopSource.UnPause();
//////        }
//////        OnSettingsChanged?.Invoke();
//////    }

//////    // ─────────────────────────────────────────────────────────────────────
//////    // Internals
//////    // ─────────────────────────────────────────────────────────────────────

//////    private void LoadSettings()
//////    {
//////        MasterVolume = PlayerPrefs.GetFloat(KEY_MASTER_VOL, 1f);
//////        MusicVolume = PlayerPrefs.GetFloat(KEY_MUSIC_VOL, 1f);
//////        SFXVolume = PlayerPrefs.GetFloat(KEY_SFX_VOL, 1f);
//////        IsMusicOn = PlayerPrefs.GetInt(KEY_MUSIC_ON, 1) == 1;
//////        IsSFXOn = PlayerPrefs.GetInt(KEY_SFX_ON, 1) == 1;
//////    }

//////    private void ApplyVolumes()
//////    {
//////        if (musicSource.isPlaying)
//////            musicSource.volume = IsMusicOn ? MusicVolume * MasterVolume : 0f;
//////    }
//////}

////////using System.Collections;
////////using System.Collections.Generic;
////////using UnityEngine;

////////public class SoundManager : MonoBehaviour
////////{
////////    public static SoundManager Instance { get; private set; }

////////    [Header("Audio Data")]
////////    [SerializeField] private AudioData audioData;

////////    [Header("Audio Sources")]
////////    [SerializeField] private AudioSource musicSource;
////////    [SerializeField] private AudioSource sfxSource;

////////    // ── Persistent Settings Keys ──────────────────────────────────────────
////////    private const string KEY_MASTER_VOL = "MasterVolume";
////////    private const string KEY_MUSIC_VOL = "MusicVolume";
////////    private const string KEY_SFX_VOL = "SFXVolume";
////////    private const string KEY_MUSIC_ON = "MusicOn";
////////    private const string KEY_SFX_ON = "SFXOn";

////////    // ── Runtime State ─────────────────────────────────────────────────────
////////    public float MasterVolume { get; private set; }
////////    public float MusicVolume { get; private set; }
////////    public float SFXVolume { get; private set; }
////////    public bool IsMusicOn { get; private set; }
////////    public bool IsSFXOn { get; private set; }

////////    private Dictionary<string, AudioData.MusicTrack> _musicDict = new();
////////    private Dictionary<string, AudioData.SFXClip> _sfxDict = new();

////////    // ── Events (UI can subscribe) ─────────────────────────────────────────
////////    public event System.Action OnSettingsChanged;

////////    // ─────────────────────────────────────────────────────────────────────
////////    // Lifecycle
////////    // ─────────────────────────────────────────────────────────────────────

////////    private void Awake()
////////    {
////////        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
////////        Instance = this;
////////        DontDestroyOnLoad(gameObject);

////////        BuildDictionaries();
////////        LoadSettings();
////////        ApplyVolumes();
////////    }

////////    //private void BuildDictionaries()
////////    //{
////////    //    foreach (var track in AudioData.musicTracks)
////////    //        if (!string.IsNullOrEmpty(track.name)) _musicDict[track.name] = track;

////////    //    foreach (var sfx in AudioData.sfxClips)
////////    //        if (!string.IsNullOrEmpty(sfx.name)) _sfxDict[sfx.name] = sfx;
////////    //}

////////    private void BuildDictionaries()
////////    {
////////        foreach (var scene in audioData.music)
////////            foreach (var track in scene.tracks)
////////                if (!string.IsNullOrEmpty(track.name)) _musicDict[track.name] = track;

////////        foreach (var scene in audioData.sfx)
////////            foreach (var clip in scene.clips)
////////                if (!string.IsNullOrEmpty(clip.name)) _sfxDict[clip.name] = clip;
////////    }

////////    // ─────────────────────────────────────────────────────────────────────
////////    // Music
////////    // ─────────────────────────────────────────────────────────────────────

////////    public void PlayMusic(string trackName)
////////    {
////////        if (!_musicDict.TryGetValue(trackName, out var track)) { Debug.LogWarning($"[SoundManager] Music not found: {trackName}"); return; }
////////        if (musicSource.clip == track.clip && musicSource.isPlaying) return;

////////        musicSource.clip = track.clip;
////////        musicSource.loop = track.loop;
////////        musicSource.volume = IsMusicOn ? track.volume * MasterVolume * MusicVolume : 0f;
////////        musicSource.Play();
////////    }

////////    public void PlayMusicWithFade(string trackName, float fadeDuration = 1f)
////////    {
////////        StartCoroutine(FadeAndSwitch(trackName, fadeDuration));
////////    }

////////    private IEnumerator FadeAndSwitch(string trackName, float duration)
////////    {
////////        // Fade out
////////        float startVol = musicSource.volume;
////////        for (float t = 0; t < duration; t += Time.deltaTime)
////////        {
////////            musicSource.volume = Mathf.Lerp(startVol, 0f, t / duration);
////////            yield return null;
////////        }

////////        PlayMusic(trackName);
////////    }

////////    public void StopMusic() => musicSource.Stop();
////////    public void PauseMusic() => musicSource.Pause();
////////    public void ResumeMusic() => musicSource.UnPause();

////////    // ─────────────────────────────────────────────────────────────────────
////////    // SFX
////////    // ─────────────────────────────────────────────────────────────────────

////////    public void PlaySFX(string sfxName)
////////    {
////////        if (!IsSFXOn) return;
////////        if (!_sfxDict.TryGetValue(sfxName, out var sfx)) { Debug.LogWarning($"[SoundManager] SFX not found: {sfxName}"); return; }

////////        sfxSource.pitch = sfx.pitch;
////////        sfxSource.PlayOneShot(sfx.clip, sfx.volume * MasterVolume * SFXVolume);
////////    }

////////    // ─────────────────────────────────────────────────────────────────────
////////    // Volume Controls  ← hook these directly to UI Sliders / Buttons
////////    // ─────────────────────────────────────────────────────────────────────

////////    public void SetMasterVolume(float value)
////////    {
////////        MasterVolume = Mathf.Clamp01(value);
////////        PlayerPrefs.SetFloat(KEY_MASTER_VOL, MasterVolume);
////////        ApplyVolumes();
////////        OnSettingsChanged?.Invoke();
////////    }

////////    public void SetMusicVolume(float value)
////////    {
////////        MusicVolume = Mathf.Clamp01(value);
////////        PlayerPrefs.SetFloat(KEY_MUSIC_VOL, MusicVolume);
////////        ApplyVolumes();
////////        OnSettingsChanged?.Invoke();
////////    }

////////    public void SetSFXVolume(float value)
////////    {
////////        SFXVolume = Mathf.Clamp01(value);
////////        PlayerPrefs.SetFloat(KEY_SFX_VOL, SFXVolume);
////////        OnSettingsChanged?.Invoke();
////////    }

////////    public void ToggleMusic()
////////    {
////////        IsMusicOn = !IsMusicOn;
////////        PlayerPrefs.SetInt(KEY_MUSIC_ON, IsMusicOn ? 1 : 0);
////////        musicSource.volume = IsMusicOn ? MusicVolume * MasterVolume : 0f;
////////        OnSettingsChanged?.Invoke();
////////    }

////////    public void ToggleSFX()
////////    {
////////        IsSFXOn = !IsSFXOn;
////////        PlayerPrefs.SetInt(KEY_SFX_ON, IsSFXOn ? 1 : 0);
////////        OnSettingsChanged?.Invoke();
////////    }

////////    // ─────────────────────────────────────────────────────────────────────
////////    // Internals
////////    // ─────────────────────────────────────────────────────────────────────

////////    private void LoadSettings()
////////    {
////////        MasterVolume = PlayerPrefs.GetFloat(KEY_MASTER_VOL, 1f);
////////        MusicVolume = PlayerPrefs.GetFloat(KEY_MUSIC_VOL, 1f);
////////        SFXVolume = PlayerPrefs.GetFloat(KEY_SFX_VOL, 1f);
////////        IsMusicOn = PlayerPrefs.GetInt(KEY_MUSIC_ON, 1) == 1;
////////        IsSFXOn = PlayerPrefs.GetInt(KEY_SFX_ON, 1) == 1;
////////    }

////////    private void ApplyVolumes()
////////    {
////////        if (musicSource.isPlaying)
////////            musicSource.volume = IsMusicOn ? MusicVolume * MasterVolume : 0f;
////////    }
////////}

//////using System.Collections;
//////using System.Collections.Generic;
//////using UnityEngine;

//////public class SoundManager : MonoBehaviour
//////{
//////    public static SoundManager Instance { get; private set; }

//////    [Header("Audio Data")]
//////    [SerializeField] private AudioData audioData;

//////    [Header("Audio Sources")]
//////    [SerializeField] private AudioSource musicSource;
//////    [SerializeField] private AudioSource sfxSource;
//////    [SerializeField] private AudioSource sfxLoopSource;

//////    private const string KEY_MASTER_VOL = "MasterVolume";
//////    private const string KEY_MUSIC_VOL = "MusicVolume";
//////    private const string KEY_SFX_VOL = "SFXVolume";
//////    private const string KEY_MUSIC_ON = "MusicOn";
//////    private const string KEY_SFX_ON = "SFXOn";

//////    public float MasterVolume { get; private set; }
//////    public float MusicVolume { get; private set; }
//////    public float SFXVolume { get; private set; }
//////    public bool IsMusicOn { get; private set; }
//////    public bool IsSFXOn { get; private set; }

//////    private Dictionary<string, AudioData.MusicTrack> _musicDict = new();
//////    private Dictionary<string, AudioData.SFXClip> _sfxDict = new();

//////    public event System.Action OnSettingsChanged;

//////    private void Awake()
//////    {
//////        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
//////        Instance = this;
//////        DontDestroyOnLoad(gameObject);

//////        BuildDictionaries();
//////        LoadSettings();
//////        ApplyVolumes();
//////    }

//////    private void BuildDictionaries()
//////    {
//////        foreach (var scene in audioData.music)
//////            foreach (var track in scene.tracks)
//////                if (!string.IsNullOrEmpty(track.name)) _musicDict[track.name] = track;

//////        foreach (var scene in audioData.sfx)
//////            foreach (var clip in scene.clips)
//////                if (!string.IsNullOrEmpty(clip.name)) _sfxDict[clip.name] = clip;
//////    }

//////    // ─────────────────────────────────────────────────────────────────────
//////    // Music
//////    // ─────────────────────────────────────────────────────────────────────

//////    public void PlayMusic(string trackName)
//////    {
//////        if (!_musicDict.TryGetValue(trackName, out var track)) { Debug.LogWarning($"[SoundManager] Music not found: {trackName}"); return; }
//////        if (musicSource.clip == track.clip && musicSource.isPlaying) return;

//////        musicSource.clip = track.clip;
//////        musicSource.loop = track.loop;
//////        musicSource.volume = IsMusicOn ? track.volume * MasterVolume * MusicVolume : 0f;
//////        musicSource.Play();
//////    }

//////    public void PlayMusicWithFade(string trackName, float fadeDuration = 1f)
//////    {
//////        StartCoroutine(FadeAndSwitch(trackName, fadeDuration));
//////    }

//////    private IEnumerator FadeAndSwitch(string trackName, float duration)
//////    {
//////        float startVol = musicSource.volume;
//////        for (float t = 0; t < duration; t += Time.deltaTime)
//////        {
//////            musicSource.volume = Mathf.Lerp(startVol, 0f, t / duration);
//////            yield return null;
//////        }
//////        PlayMusic(trackName);
//////    }

//////    public void StopMusic() => musicSource.Stop();
//////    public void PauseMusic() => musicSource.Pause();
//////    public void ResumeMusic() => musicSource.UnPause();

//////    // ─────────────────────────────────────────────────────────────────────
//////    // SFX
//////    // ─────────────────────────────────────────────────────────────────────

//////    public void PlaySFX(string sfxName)
//////    {
//////        if (!IsSFXOn) return;
//////        if (!_sfxDict.TryGetValue(sfxName, out var sfx)) { Debug.LogWarning($"[SoundManager] SFX not found: {sfxName}"); return; }

//////        sfxSource.pitch = sfx.pitch;
//////        sfxSource.PlayOneShot(sfx.clip, sfx.volume * MasterVolume * SFXVolume);
//////    }

//////    public void PlaySFXLoop(string sfxName)
//////    {
//////        if (!IsSFXOn) return;
//////        if (!_sfxDict.TryGetValue(sfxName, out var sfx)) { Debug.LogWarning($"[SoundManager] SFX not found: {sfxName}"); return; }

//////        sfxLoopSource.clip = sfx.clip;
//////        sfxLoopSource.volume = sfx.volume * MasterVolume * SFXVolume;
//////        sfxLoopSource.pitch = sfx.pitch;
//////        sfxLoopSource.loop = true;
//////        sfxLoopSource.Play();
//////    }

//////    public void StopSFXLoop()
//////    {
//////        sfxLoopSource.Stop();
//////        sfxLoopSource.clip = null;
//////    }

//////    // ─────────────────────────────────────────────────────────────────────
//////    // Volume Controls
//////    // ─────────────────────────────────────────────────────────────────────

//////    public void SetMasterVolume(float value)
//////    {
//////        MasterVolume = Mathf.Clamp01(value);
//////        PlayerPrefs.SetFloat(KEY_MASTER_VOL, MasterVolume);
//////        ApplyVolumes();
//////        OnSettingsChanged?.Invoke();
//////    }

//////    public void SetMusicVolume(float value)
//////    {
//////        MusicVolume = Mathf.Clamp01(value);
//////        PlayerPrefs.SetFloat(KEY_MUSIC_VOL, MusicVolume);
//////        ApplyVolumes();
//////        OnSettingsChanged?.Invoke();
//////    }

//////    public void SetSFXVolume(float value)
//////    {
//////        SFXVolume = Mathf.Clamp01(value);
//////        PlayerPrefs.SetFloat(KEY_SFX_VOL, SFXVolume);
//////        OnSettingsChanged?.Invoke();
//////    }

//////    public void ToggleMusic()
//////    {
//////        IsMusicOn = !IsMusicOn;
//////        PlayerPrefs.SetInt(KEY_MUSIC_ON, IsMusicOn ? 1 : 0);
//////        musicSource.volume = IsMusicOn ? MusicVolume * MasterVolume : 0f;
//////        OnSettingsChanged?.Invoke();
//////    }

//////    //public void ToggleSFX()
//////    //{
//////    //    IsSFXOn = !IsSFXOn;
//////    //    PlayerPrefs.SetInt(KEY_SFX_ON, IsSFXOn ? 1 : 0);
//////    //    OnSettingsChanged?.Invoke();
//////    //}

//////    public void ToggleSFX()
//////    {
//////        IsSFXOn = !IsSFXOn;
//////        PlayerPrefs.SetInt(KEY_SFX_ON, IsSFXOn ? 1 : 0);
//////        if (!IsSFXOn) StopSFXLoop();
//////        OnSettingsChanged?.Invoke();
//////    }

//////    // ─────────────────────────────────────────────────────────────────────
//////    // Internals
//////    // ─────────────────────────────────────────────────────────────────────

//////    private void LoadSettings()
//////    {
//////        MasterVolume = PlayerPrefs.GetFloat(KEY_MASTER_VOL, 1f);
//////        MusicVolume = PlayerPrefs.GetFloat(KEY_MUSIC_VOL, 1f);
//////        SFXVolume = PlayerPrefs.GetFloat(KEY_SFX_VOL, 1f);
//////        IsMusicOn = PlayerPrefs.GetInt(KEY_MUSIC_ON, 1) == 1;
//////        IsSFXOn = PlayerPrefs.GetInt(KEY_SFX_ON, 1) == 1;
//////    }

//////    private void ApplyVolumes()
//////    {
//////        if (musicSource.isPlaying)
//////            musicSource.volume = IsMusicOn ? MusicVolume * MasterVolume : 0f;
//////    }
//////}

////using System.Collections;
////using System.Collections.Generic;
////using UnityEngine;
////using UnityEngine.SceneManagement;

////public class SoundManager : MonoBehaviour
////{
////    public static SoundManager Instance { get; private set; }

////    [Header("Audio Data")]
////    [SerializeField] private AudioData audioData;

////    [Header("Audio Sources")]
////    [SerializeField] private AudioSource musicSource;
////    [SerializeField] private AudioSource sfxSource;
////    [SerializeField] private AudioSource sfxLoopSource;

////    private const string KEY_MASTER_VOL = "MasterVolume";
////    private const string KEY_MUSIC_VOL = "MusicVolume";
////    private const string KEY_SFX_VOL = "SFXVolume";
////    private const string KEY_MUSIC_ON = "MusicOn";
////    private const string KEY_SFX_ON = "SFXOn";

////    public float MasterVolume { get; private set; }
////    public float MusicVolume { get; private set; }
////    public float SFXVolume { get; private set; }
////    public bool IsMusicOn { get; private set; }
////    public bool IsSFXOn { get; private set; }

////    private Dictionary<string, AudioData.MusicTrack> _musicDict = new();
////    private Dictionary<string, AudioData.SFXClip> _sfxDict = new();

////    public event System.Action OnSettingsChanged;

////    private void Awake()
////    {
////        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
////        Instance = this;
////        DontDestroyOnLoad(gameObject);

////        BuildDictionaries();
////        LoadSettings();
////        ApplyVolumes();

////        SceneManager.sceneLoaded += OnSceneLoaded;
////    }

////    private void OnDestroy()
////    {
////        SceneManager.sceneLoaded -= OnSceneLoaded;
////    }

////    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
////    {
////        // Stop any looping SFX (e.g. Walking) whenever the scene changes.
////        StopSFXLoop();
////    }

////    private void BuildDictionaries()
////    {
////        foreach (var scene in audioData.music)
////            foreach (var track in scene.tracks)
////                if (!string.IsNullOrEmpty(track.name)) _musicDict[track.name] = track;

////        foreach (var scene in audioData.sfx)
////            foreach (var clip in scene.clips)
////                if (!string.IsNullOrEmpty(clip.name)) _sfxDict[clip.name] = clip;
////    }

////    // ─────────────────────────────────────────────────────────────────────
////    // Music
////    // ─────────────────────────────────────────────────────────────────────

////    public void PlayMusic(string trackName)
////    {
////        if (!_musicDict.TryGetValue(trackName, out var track)) { Debug.LogWarning($"[SoundManager] Music not found: {trackName}"); return; }
////        if (musicSource.clip == track.clip && musicSource.isPlaying) return;

////        musicSource.clip = track.clip;
////        musicSource.loop = track.loop;
////        musicSource.volume = IsMusicOn ? track.volume * MasterVolume * MusicVolume : 0f;
////        musicSource.Play();
////    }

////    public void PlayMusicWithFade(string trackName, float fadeDuration = 1f)
////    {
////        StartCoroutine(FadeAndSwitch(trackName, fadeDuration));
////    }

////    private IEnumerator FadeAndSwitch(string trackName, float duration)
////    {
////        float startVol = musicSource.volume;
////        for (float t = 0; t < duration; t += Time.deltaTime)
////        {
////            musicSource.volume = Mathf.Lerp(startVol, 0f, t / duration);
////            yield return null;
////        }
////        PlayMusic(trackName);
////    }

////    public void StopMusic() => musicSource.Stop();
////    public void PauseMusic() => musicSource.Pause();
////    public void ResumeMusic() => musicSource.UnPause();

////    // ─────────────────────────────────────────────────────────────────────
////    // SFX
////    // ─────────────────────────────────────────────────────────────────────

////    //public void PlaySFX(string sfxName)
////    //{
////    //    if (!IsSFXOn) return;
////    //    if (!_sfxDict.TryGetValue(sfxName, out var sfx)) { Debug.LogWarning($"[SoundManager] SFX not found: {sfxName}"); return; }

////    //    sfxSource.pitch = sfx.pitch;
////    //    sfxSource.PlayOneShot(sfx.clip, sfx.volume * MasterVolume * SFXVolume);
////    //}

////    public void PlaySFX(string sfxName)
////    {
////        if (!IsSFXOn) return;
////        if (!_sfxDict.TryGetValue(sfxName, out var sfx)) { Debug.LogWarning($"[SoundManager] SFX not found: {sfxName}"); return; }

////        AudioSource temp = gameObject.AddComponent<AudioSource>();
////        temp.clip = sfx.clip;
////        temp.volume = sfx.volume * MasterVolume * SFXVolume;
////        temp.pitch = sfx.pitch;
////        temp.PlayOneShot(sfx.clip, sfx.volume * MasterVolume * SFXVolume);
////        Destroy(temp, sfx.clip.length / Mathf.Abs(sfx.pitch) + 0.1f);
////    }

////    public void PlaySFXLoop(string sfxName)
////    {
////        if (!IsSFXOn) return;
////        if (!_sfxDict.TryGetValue(sfxName, out var sfx)) { Debug.LogWarning($"[SoundManager] SFX not found: {sfxName}"); return; }

////        sfxLoopSource.clip = sfx.clip;
////        sfxLoopSource.volume = sfx.volume * MasterVolume * SFXVolume;
////        sfxLoopSource.pitch = sfx.pitch;
////        sfxLoopSource.loop = true;
////        sfxLoopSource.Play();
////    }

////    public void StopSFXLoop()
////    {
////        sfxLoopSource.Stop();
////        sfxLoopSource.clip = null;
////    }

////    // ─────────────────────────────────────────────────────────────────────
////    // Volume Controls
////    // ─────────────────────────────────────────────────────────────────────

////    public void SetMasterVolume(float value)
////    {
////        MasterVolume = Mathf.Clamp01(value);
////        PlayerPrefs.SetFloat(KEY_MASTER_VOL, MasterVolume);
////        ApplyVolumes();
////        OnSettingsChanged?.Invoke();
////    }

////    public void SetMusicVolume(float value)
////    {
////        MusicVolume = Mathf.Clamp01(value);
////        PlayerPrefs.SetFloat(KEY_MUSIC_VOL, MusicVolume);
////        ApplyVolumes();
////        OnSettingsChanged?.Invoke();
////    }

////    public void SetSFXVolume(float value)
////    {
////        SFXVolume = Mathf.Clamp01(value);
////        PlayerPrefs.SetFloat(KEY_SFX_VOL, SFXVolume);
////        OnSettingsChanged?.Invoke();
////    }

////    public void ToggleMusic()
////    {
////        IsMusicOn = !IsMusicOn;
////        PlayerPrefs.SetInt(KEY_MUSIC_ON, IsMusicOn ? 1 : 0);
////        musicSource.volume = IsMusicOn ? MusicVolume * MasterVolume : 0f;
////        OnSettingsChanged?.Invoke();
////    }

////    public void ToggleSFX()
////    {
////        IsSFXOn = !IsSFXOn;
////        PlayerPrefs.SetInt(KEY_SFX_ON, IsSFXOn ? 1 : 0);
////        if (!IsSFXOn)
////        {
////            sfxLoopSource.Pause();
////        }
////        else if (sfxLoopSource.clip != null)
////        {
////            sfxLoopSource.UnPause();
////        }
////        OnSettingsChanged?.Invoke();
////    }

////    // ─────────────────────────────────────────────────────────────────────
////    // Internals
////    // ─────────────────────────────────────────────────────────────────────

////    private void LoadSettings()
////    {
////        MasterVolume = PlayerPrefs.GetFloat(KEY_MASTER_VOL, 1f);
////        MusicVolume = PlayerPrefs.GetFloat(KEY_MUSIC_VOL, 1f);
////        SFXVolume = PlayerPrefs.GetFloat(KEY_SFX_VOL, 1f);
////        IsMusicOn = PlayerPrefs.GetInt(KEY_MUSIC_ON, 1) == 1;
////        IsSFXOn = PlayerPrefs.GetInt(KEY_SFX_ON, 1) == 1;
////    }

////    private void ApplyVolumes()
////    {
////        if (musicSource.isPlaying)
////            musicSource.volume = IsMusicOn ? MusicVolume * MasterVolume : 0f;
////    }
////}

//////using System.Collections;
//////using System.Collections.Generic;
//////using UnityEngine;

//////public class SoundManager : MonoBehaviour
//////{
//////    public static SoundManager Instance { get; private set; }

//////    [Header("Audio Data")]
//////    [SerializeField] private AudioData audioData;

//////    [Header("Audio Sources")]
//////    [SerializeField] private AudioSource musicSource;
//////    [SerializeField] private AudioSource sfxSource;

//////    // ── Persistent Settings Keys ──────────────────────────────────────────
//////    private const string KEY_MASTER_VOL = "MasterVolume";
//////    private const string KEY_MUSIC_VOL = "MusicVolume";
//////    private const string KEY_SFX_VOL = "SFXVolume";
//////    private const string KEY_MUSIC_ON = "MusicOn";
//////    private const string KEY_SFX_ON = "SFXOn";

//////    // ── Runtime State ─────────────────────────────────────────────────────
//////    public float MasterVolume { get; private set; }
//////    public float MusicVolume { get; private set; }
//////    public float SFXVolume { get; private set; }
//////    public bool IsMusicOn { get; private set; }
//////    public bool IsSFXOn { get; private set; }

//////    private Dictionary<string, AudioData.MusicTrack> _musicDict = new();
//////    private Dictionary<string, AudioData.SFXClip> _sfxDict = new();

//////    // ── Events (UI can subscribe) ─────────────────────────────────────────
//////    public event System.Action OnSettingsChanged;

//////    // ─────────────────────────────────────────────────────────────────────
//////    // Lifecycle
//////    // ─────────────────────────────────────────────────────────────────────

//////    private void Awake()
//////    {
//////        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
//////        Instance = this;
//////        DontDestroyOnLoad(gameObject);

//////        BuildDictionaries();
//////        LoadSettings();
//////        ApplyVolumes();
//////    }

//////    //private void BuildDictionaries()
//////    //{
//////    //    foreach (var track in AudioData.musicTracks)
//////    //        if (!string.IsNullOrEmpty(track.name)) _musicDict[track.name] = track;

//////    //    foreach (var sfx in AudioData.sfxClips)
//////    //        if (!string.IsNullOrEmpty(sfx.name)) _sfxDict[sfx.name] = sfx;
//////    //}

//////    private void BuildDictionaries()
//////    {
//////        foreach (var scene in audioData.music)
//////            foreach (var track in scene.tracks)
//////                if (!string.IsNullOrEmpty(track.name)) _musicDict[track.name] = track;

//////        foreach (var scene in audioData.sfx)
//////            foreach (var clip in scene.clips)
//////                if (!string.IsNullOrEmpty(clip.name)) _sfxDict[clip.name] = clip;
//////    }

//////    // ─────────────────────────────────────────────────────────────────────
//////    // Music
//////    // ─────────────────────────────────────────────────────────────────────

//////    public void PlayMusic(string trackName)
//////    {
//////        if (!_musicDict.TryGetValue(trackName, out var track)) { Debug.LogWarning($"[SoundManager] Music not found: {trackName}"); return; }
//////        if (musicSource.clip == track.clip && musicSource.isPlaying) return;

//////        musicSource.clip = track.clip;
//////        musicSource.loop = track.loop;
//////        musicSource.volume = IsMusicOn ? track.volume * MasterVolume * MusicVolume : 0f;
//////        musicSource.Play();
//////    }

//////    public void PlayMusicWithFade(string trackName, float fadeDuration = 1f)
//////    {
//////        StartCoroutine(FadeAndSwitch(trackName, fadeDuration));
//////    }

//////    private IEnumerator FadeAndSwitch(string trackName, float duration)
//////    {
//////        // Fade out
//////        float startVol = musicSource.volume;
//////        for (float t = 0; t < duration; t += Time.deltaTime)
//////        {
//////            musicSource.volume = Mathf.Lerp(startVol, 0f, t / duration);
//////            yield return null;
//////        }

//////        PlayMusic(trackName);
//////    }

//////    public void StopMusic() => musicSource.Stop();
//////    public void PauseMusic() => musicSource.Pause();
//////    public void ResumeMusic() => musicSource.UnPause();

//////    // ─────────────────────────────────────────────────────────────────────
//////    // SFX
//////    // ─────────────────────────────────────────────────────────────────────

//////    public void PlaySFX(string sfxName)
//////    {
//////        if (!IsSFXOn) return;
//////        if (!_sfxDict.TryGetValue(sfxName, out var sfx)) { Debug.LogWarning($"[SoundManager] SFX not found: {sfxName}"); return; }

//////        sfxSource.pitch = sfx.pitch;
//////        sfxSource.PlayOneShot(sfx.clip, sfx.volume * MasterVolume * SFXVolume);
//////    }

//////    // ─────────────────────────────────────────────────────────────────────
//////    // Volume Controls  ← hook these directly to UI Sliders / Buttons
//////    // ─────────────────────────────────────────────────────────────────────

//////    public void SetMasterVolume(float value)
//////    {
//////        MasterVolume = Mathf.Clamp01(value);
//////        PlayerPrefs.SetFloat(KEY_MASTER_VOL, MasterVolume);
//////        ApplyVolumes();
//////        OnSettingsChanged?.Invoke();
//////    }

//////    public void SetMusicVolume(float value)
//////    {
//////        MusicVolume = Mathf.Clamp01(value);
//////        PlayerPrefs.SetFloat(KEY_MUSIC_VOL, MusicVolume);
//////        ApplyVolumes();
//////        OnSettingsChanged?.Invoke();
//////    }

//////    public void SetSFXVolume(float value)
//////    {
//////        SFXVolume = Mathf.Clamp01(value);
//////        PlayerPrefs.SetFloat(KEY_SFX_VOL, SFXVolume);
//////        OnSettingsChanged?.Invoke();
//////    }

//////    public void ToggleMusic()
//////    {
//////        IsMusicOn = !IsMusicOn;
//////        PlayerPrefs.SetInt(KEY_MUSIC_ON, IsMusicOn ? 1 : 0);
//////        musicSource.volume = IsMusicOn ? MusicVolume * MasterVolume : 0f;
//////        OnSettingsChanged?.Invoke();
//////    }

//////    public void ToggleSFX()
//////    {
//////        IsSFXOn = !IsSFXOn;
//////        PlayerPrefs.SetInt(KEY_SFX_ON, IsSFXOn ? 1 : 0);
//////        OnSettingsChanged?.Invoke();
//////    }

//////    // ─────────────────────────────────────────────────────────────────────
//////    // Internals
//////    // ─────────────────────────────────────────────────────────────────────

//////    private void LoadSettings()
//////    {
//////        MasterVolume = PlayerPrefs.GetFloat(KEY_MASTER_VOL, 1f);
//////        MusicVolume = PlayerPrefs.GetFloat(KEY_MUSIC_VOL, 1f);
//////        SFXVolume = PlayerPrefs.GetFloat(KEY_SFX_VOL, 1f);
//////        IsMusicOn = PlayerPrefs.GetInt(KEY_MUSIC_ON, 1) == 1;
//////        IsSFXOn = PlayerPrefs.GetInt(KEY_SFX_ON, 1) == 1;
//////    }

//////    private void ApplyVolumes()
//////    {
//////        if (musicSource.isPlaying)
//////            musicSource.volume = IsMusicOn ? MusicVolume * MasterVolume : 0f;
//////    }
//////}

////using System.Collections;
////using System.Collections.Generic;
////using UnityEngine;

////public class SoundManager : MonoBehaviour
////{
////    public static SoundManager Instance { get; private set; }

////    [Header("Audio Data")]
////    [SerializeField] private AudioData audioData;

////    [Header("Audio Sources")]
////    [SerializeField] private AudioSource musicSource;
////    [SerializeField] private AudioSource sfxSource;
////    [SerializeField] private AudioSource sfxLoopSource;

////    private const string KEY_MASTER_VOL = "MasterVolume";
////    private const string KEY_MUSIC_VOL = "MusicVolume";
////    private const string KEY_SFX_VOL = "SFXVolume";
////    private const string KEY_MUSIC_ON = "MusicOn";
////    private const string KEY_SFX_ON = "SFXOn";

////    public float MasterVolume { get; private set; }
////    public float MusicVolume { get; private set; }
////    public float SFXVolume { get; private set; }
////    public bool IsMusicOn { get; private set; }
////    public bool IsSFXOn { get; private set; }

////    private Dictionary<string, AudioData.MusicTrack> _musicDict = new();
////    private Dictionary<string, AudioData.SFXClip> _sfxDict = new();

////    public event System.Action OnSettingsChanged;

////    private void Awake()
////    {
////        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
////        Instance = this;
////        DontDestroyOnLoad(gameObject);

////        BuildDictionaries();
////        LoadSettings();
////        ApplyVolumes();
////    }

////    private void BuildDictionaries()
////    {
////        foreach (var scene in audioData.music)
////            foreach (var track in scene.tracks)
////                if (!string.IsNullOrEmpty(track.name)) _musicDict[track.name] = track;

////        foreach (var scene in audioData.sfx)
////            foreach (var clip in scene.clips)
////                if (!string.IsNullOrEmpty(clip.name)) _sfxDict[clip.name] = clip;
////    }

////    // ─────────────────────────────────────────────────────────────────────
////    // Music
////    // ─────────────────────────────────────────────────────────────────────

////    public void PlayMusic(string trackName)
////    {
////        if (!_musicDict.TryGetValue(trackName, out var track)) { Debug.LogWarning($"[SoundManager] Music not found: {trackName}"); return; }
////        if (musicSource.clip == track.clip && musicSource.isPlaying) return;

////        musicSource.clip = track.clip;
////        musicSource.loop = track.loop;
////        musicSource.volume = IsMusicOn ? track.volume * MasterVolume * MusicVolume : 0f;
////        musicSource.Play();
////    }

////    public void PlayMusicWithFade(string trackName, float fadeDuration = 1f)
////    {
////        StartCoroutine(FadeAndSwitch(trackName, fadeDuration));
////    }

////    private IEnumerator FadeAndSwitch(string trackName, float duration)
////    {
////        float startVol = musicSource.volume;
////        for (float t = 0; t < duration; t += Time.deltaTime)
////        {
////            musicSource.volume = Mathf.Lerp(startVol, 0f, t / duration);
////            yield return null;
////        }
////        PlayMusic(trackName);
////    }

////    public void StopMusic() => musicSource.Stop();
////    public void PauseMusic() => musicSource.Pause();
////    public void ResumeMusic() => musicSource.UnPause();

////    // ─────────────────────────────────────────────────────────────────────
////    // SFX
////    // ─────────────────────────────────────────────────────────────────────

////    public void PlaySFX(string sfxName)
////    {
////        if (!IsSFXOn) return;
////        if (!_sfxDict.TryGetValue(sfxName, out var sfx)) { Debug.LogWarning($"[SoundManager] SFX not found: {sfxName}"); return; }

////        sfxSource.pitch = sfx.pitch;
////        sfxSource.PlayOneShot(sfx.clip, sfx.volume * MasterVolume * SFXVolume);
////    }

////    public void PlaySFXLoop(string sfxName)
////    {
////        if (!IsSFXOn) return;
////        if (!_sfxDict.TryGetValue(sfxName, out var sfx)) { Debug.LogWarning($"[SoundManager] SFX not found: {sfxName}"); return; }

////        sfxLoopSource.clip = sfx.clip;
////        sfxLoopSource.volume = sfx.volume * MasterVolume * SFXVolume;
////        sfxLoopSource.pitch = sfx.pitch;
////        sfxLoopSource.loop = true;
////        sfxLoopSource.Play();
////    }

////    public void StopSFXLoop()
////    {
////        sfxLoopSource.Stop();
////        sfxLoopSource.clip = null;
////    }

////    // ─────────────────────────────────────────────────────────────────────
////    // Volume Controls
////    // ─────────────────────────────────────────────────────────────────────

////    public void SetMasterVolume(float value)
////    {
////        MasterVolume = Mathf.Clamp01(value);
////        PlayerPrefs.SetFloat(KEY_MASTER_VOL, MasterVolume);
////        ApplyVolumes();
////        OnSettingsChanged?.Invoke();
////    }

////    public void SetMusicVolume(float value)
////    {
////        MusicVolume = Mathf.Clamp01(value);
////        PlayerPrefs.SetFloat(KEY_MUSIC_VOL, MusicVolume);
////        ApplyVolumes();
////        OnSettingsChanged?.Invoke();
////    }

////    public void SetSFXVolume(float value)
////    {
////        SFXVolume = Mathf.Clamp01(value);
////        PlayerPrefs.SetFloat(KEY_SFX_VOL, SFXVolume);
////        OnSettingsChanged?.Invoke();
////    }

////    public void ToggleMusic()
////    {
////        IsMusicOn = !IsMusicOn;
////        PlayerPrefs.SetInt(KEY_MUSIC_ON, IsMusicOn ? 1 : 0);
////        musicSource.volume = IsMusicOn ? MusicVolume * MasterVolume : 0f;
////        OnSettingsChanged?.Invoke();
////    }

////    //public void ToggleSFX()
////    //{
////    //    IsSFXOn = !IsSFXOn;
////    //    PlayerPrefs.SetInt(KEY_SFX_ON, IsSFXOn ? 1 : 0);
////    //    OnSettingsChanged?.Invoke();
////    //}

////    public void ToggleSFX()
////    {
////        IsSFXOn = !IsSFXOn;
////        PlayerPrefs.SetInt(KEY_SFX_ON, IsSFXOn ? 1 : 0);
////        if (!IsSFXOn) StopSFXLoop();
////        OnSettingsChanged?.Invoke();
////    }

////    // ─────────────────────────────────────────────────────────────────────
////    // Internals
////    // ─────────────────────────────────────────────────────────────────────

////    private void LoadSettings()
////    {
////        MasterVolume = PlayerPrefs.GetFloat(KEY_MASTER_VOL, 1f);
////        MusicVolume = PlayerPrefs.GetFloat(KEY_MUSIC_VOL, 1f);
////        SFXVolume = PlayerPrefs.GetFloat(KEY_SFX_VOL, 1f);
////        IsMusicOn = PlayerPrefs.GetInt(KEY_MUSIC_ON, 1) == 1;
////        IsSFXOn = PlayerPrefs.GetInt(KEY_SFX_ON, 1) == 1;
////    }

////    private void ApplyVolumes()
////    {
////        if (musicSource.isPlaying)
////            musicSource.volume = IsMusicOn ? MusicVolume * MasterVolume : 0f;
////    }
////}

//using System.Collections;
//using System.Collections.Generic;
//using UnityEngine;
//using UnityEngine.SceneManagement;

//public class SoundManager : MonoBehaviour
//{
//    public static SoundManager Instance { get; private set; }

//    [Header("Audio Data")]
//    [SerializeField] private AudioData audioData;

//    [Header("Audio Sources")]
//    [SerializeField] private AudioSource musicSource;
//    [SerializeField] private AudioSource sfxSource;
//    [SerializeField] private AudioSource sfxLoopSource;

//    private const string KEY_MASTER_VOL = "MasterVolume";
//    private const string KEY_MUSIC_VOL = "MusicVolume";
//    private const string KEY_SFX_VOL = "SFXVolume";
//    private const string KEY_MUSIC_ON = "MusicOn";
//    private const string KEY_SFX_ON = "SFXOn";

//    public float MasterVolume { get; private set; }
//    public float MusicVolume { get; private set; }
//    public float SFXVolume { get; private set; }
//    public bool IsMusicOn { get; private set; }
//    public bool IsSFXOn { get; private set; }

//    private Dictionary<string, AudioData.MusicTrack> _musicDict = new();
//    private Dictionary<string, AudioData.SFXClip> _sfxDict = new();

//    public event System.Action OnSettingsChanged;

//    private void Awake()
//    {
//        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
//        Instance = this;
//        DontDestroyOnLoad(gameObject);

//        BuildDictionaries();
//        LoadSettings();
//        ApplyVolumes();

//        SceneManager.sceneLoaded += OnSceneLoaded;
//    }

//    private void OnDestroy()
//    {
//        SceneManager.sceneLoaded -= OnSceneLoaded;
//    }

//    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
//    {
//        // Stop any looping SFX (e.g. Walking) whenever the scene changes.
//        StopSFXLoop();
//    }

//    private void BuildDictionaries()
//    {
//        foreach (var scene in audioData.music)
//            foreach (var track in scene.tracks)
//                if (!string.IsNullOrEmpty(track.name)) _musicDict[track.name] = track;

//        foreach (var scene in audioData.sfx)
//            foreach (var clip in scene.clips)
//                if (!string.IsNullOrEmpty(clip.name)) _sfxDict[clip.name] = clip;
//    }

//    // ─────────────────────────────────────────────────────────────────────
//    // Music
//    // ─────────────────────────────────────────────────────────────────────

//    public void PlayMusic(string trackName)
//    {
//        if (!_musicDict.TryGetValue(trackName, out var track)) { Debug.LogWarning($"[SoundManager] Music not found: {trackName}"); return; }
//        if (musicSource.clip == track.clip && musicSource.isPlaying) return;

//        musicSource.clip = track.clip;
//        musicSource.loop = track.loop;
//        musicSource.volume = IsMusicOn ? track.volume * MasterVolume * MusicVolume : 0f;
//        musicSource.Play();
//    }

//    public void PlayMusicWithFade(string trackName, float fadeDuration = 1f)
//    {
//        StartCoroutine(FadeAndSwitch(trackName, fadeDuration));
//    }

//    private IEnumerator FadeAndSwitch(string trackName, float duration)
//    {
//        float startVol = musicSource.volume;
//        for (float t = 0; t < duration; t += Time.deltaTime)
//        {
//            musicSource.volume = Mathf.Lerp(startVol, 0f, t / duration);
//            yield return null;
//        }
//        PlayMusic(trackName);
//    }

//    public void StopMusic() => musicSource.Stop();
//    public void PauseMusic() => musicSource.Pause();
//    public void ResumeMusic() => musicSource.UnPause();

//    // ─────────────────────────────────────────────────────────────────────
//    // SFX
//    // ─────────────────────────────────────────────────────────────────────

//    //public void PlaySFX(string sfxName)
//    //{
//    //    if (!IsSFXOn) return;
//    //    if (!_sfxDict.TryGetValue(sfxName, out var sfx)) { Debug.LogWarning($"[SoundManager] SFX not found: {sfxName}"); return; }

//    //    sfxSource.pitch = sfx.pitch;
//    //    sfxSource.PlayOneShot(sfx.clip, sfx.volume * MasterVolume * SFXVolume);
//    //}

//    public void PlaySFX(string sfxName)
//    {
//        if (!IsSFXOn) return;
//        if (!_sfxDict.TryGetValue(sfxName, out var sfx)) { Debug.LogWarning($"[SoundManager] SFX not found: {sfxName}"); return; }

//        AudioSource temp = gameObject.AddComponent<AudioSource>();
//        temp.clip = sfx.clip;
//        temp.volume = sfx.volume * MasterVolume * SFXVolume;
//        temp.pitch = sfx.pitch;
//        temp.PlayOneShot(sfx.clip, sfx.volume * MasterVolume * SFXVolume);
//        Destroy(temp, sfx.clip.length / Mathf.Abs(sfx.pitch) + 0.1f);
//    }

//    public void PlaySFXLoop(string sfxName)
//    {
//        if (!IsSFXOn) return;
//        if (!_sfxDict.TryGetValue(sfxName, out var sfx)) { Debug.LogWarning($"[SoundManager] SFX not found: {sfxName}"); return; }

//        sfxLoopSource.clip = sfx.clip;
//        sfxLoopSource.volume = sfx.volume * MasterVolume * SFXVolume;
//        sfxLoopSource.pitch = sfx.pitch;
//        sfxLoopSource.loop = true;
//        sfxLoopSource.Play();
//    }

//    public void StopSFXLoop()
//    {
//        sfxLoopSource.Stop();
//        sfxLoopSource.clip = null;
//    }

//    // ─────────────────────────────────────────────────────────────────────
//    // SFX Data accessors  (used by per-object AudioSources, e.g. customers)
//    // ─────────────────────────────────────────────────────────────────────

//    /// <summary>Returns the AudioClip for an SFX key, or null if not found.</summary>
//    public AudioClip GetSFXClip(string sfxName)
//    {
//        return _sfxDict.TryGetValue(sfxName, out var sfx) ? sfx.clip : null;
//    }

//    /// <summary>Returns the final volume (sfx.volume * master * sfxVol) for an SFX key.</summary>
//    public float GetSFXVolume(string sfxName)
//    {
//        return _sfxDict.TryGetValue(sfxName, out var sfx) ? sfx.volume * MasterVolume * SFXVolume : 1f;
//    }

//    /// <summary>Returns the pitch for an SFX key.</summary>
//    public float GetSFXPitch(string sfxName)
//    {
//        return _sfxDict.TryGetValue(sfxName, out var sfx) ? sfx.pitch : 1f;
//    }

//    // ─────────────────────────────────────────────────────────────────────
//    // Volume Controls
//    // ─────────────────────────────────────────────────────────────────────

//    public void SetMasterVolume(float value)
//    {
//        MasterVolume = Mathf.Clamp01(value);
//        PlayerPrefs.SetFloat(KEY_MASTER_VOL, MasterVolume);
//        ApplyVolumes();
//        OnSettingsChanged?.Invoke();
//    }

//    public void SetMusicVolume(float value)
//    {
//        MusicVolume = Mathf.Clamp01(value);
//        PlayerPrefs.SetFloat(KEY_MUSIC_VOL, MusicVolume);
//        ApplyVolumes();
//        OnSettingsChanged?.Invoke();
//    }

//    public void SetSFXVolume(float value)
//    {
//        SFXVolume = Mathf.Clamp01(value);
//        PlayerPrefs.SetFloat(KEY_SFX_VOL, SFXVolume);
//        OnSettingsChanged?.Invoke();
//    }

//    public void ToggleMusic()
//    {
//        IsMusicOn = !IsMusicOn;
//        PlayerPrefs.SetInt(KEY_MUSIC_ON, IsMusicOn ? 1 : 0);
//        musicSource.volume = IsMusicOn ? MusicVolume * MasterVolume : 0f;
//        OnSettingsChanged?.Invoke();
//    }

//    public void ToggleSFX()
//    {
//        IsSFXOn = !IsSFXOn;
//        PlayerPrefs.SetInt(KEY_SFX_ON, IsSFXOn ? 1 : 0);
//        if (!IsSFXOn)
//        {
//            sfxLoopSource.Pause();
//        }
//        else if (sfxLoopSource.clip != null)
//        {
//            sfxLoopSource.UnPause();
//        }
//        OnSettingsChanged?.Invoke();
//    }

//    // ─────────────────────────────────────────────────────────────────────
//    // Internals
//    // ─────────────────────────────────────────────────────────────────────

//    private void LoadSettings()
//    {
//        MasterVolume = PlayerPrefs.GetFloat(KEY_MASTER_VOL, 1f);
//        MusicVolume = PlayerPrefs.GetFloat(KEY_MUSIC_VOL, 1f);
//        SFXVolume = PlayerPrefs.GetFloat(KEY_SFX_VOL, 1f);
//        IsMusicOn = PlayerPrefs.GetInt(KEY_MUSIC_ON, 1) == 1;
//        IsSFXOn = PlayerPrefs.GetInt(KEY_SFX_ON, 1) == 1;
//    }

//    private void ApplyVolumes()
//    {
//        if (musicSource.isPlaying)
//            musicSource.volume = IsMusicOn ? MusicVolume * MasterVolume : 0f;
//    }
//}

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class SoundManager : MonoBehaviour
{
    public static SoundManager Instance { get; private set; }

    [Header("Audio Data")]
    [SerializeField] private AudioData audioData;

    [Header("Audio Sources")]
    [SerializeField] private AudioSource musicSource;
    [SerializeField] private AudioSource sfxSource;
    [SerializeField] private AudioSource sfxLoopSource;

    private const string KEY_MASTER_VOL = "MasterVolume";
    private const string KEY_MUSIC_VOL = "MusicVolume";
    private const string KEY_SFX_VOL = "SFXVolume";
    private const string KEY_MUSIC_ON = "MusicOn";
    private const string KEY_SFX_ON = "SFXOn";

    public float MasterVolume { get; private set; }
    public float MusicVolume { get; private set; }
    public float SFXVolume { get; private set; }
    public bool IsMusicOn { get; private set; }
    public bool IsSFXOn { get; private set; }

    private Dictionary<string, AudioData.MusicTrack> _musicDict = new();
    private Dictionary<string, AudioData.SFXClip> _sfxDict = new();
    private readonly List<AudioSource> _oneShotSources = new();

    public event System.Action OnSettingsChanged;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        BuildDictionaries();
        LoadSettings();
        ApplyVolumes();

        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // Stop any looping SFX (e.g. Walking) whenever the scene changes.
        StopSFXLoop();
        StopAllOneShotSFX();
    }

    private void BuildDictionaries()
    {
        foreach (var scene in audioData.music)
            foreach (var track in scene.tracks)
                if (!string.IsNullOrEmpty(track.name)) _musicDict[track.name] = track;

        foreach (var scene in audioData.sfx)
            foreach (var clip in scene.clips)
                if (!string.IsNullOrEmpty(clip.name)) _sfxDict[clip.name] = clip;
    }

    // ─────────────────────────────────────────────────────────────────────
    // Music
    // ─────────────────────────────────────────────────────────────────────

    public void PlayMusic(string trackName)
    {
        if (!_musicDict.TryGetValue(trackName, out var track)) { Debug.LogWarning($"[SoundManager] Music not found: {trackName}"); return; }
        if (musicSource.clip == track.clip && musicSource.isPlaying) return;

        musicSource.clip = track.clip;
        musicSource.loop = track.loop;
        musicSource.volume = IsMusicOn ? track.volume * MasterVolume * MusicVolume : 0f;
        musicSource.Play();
    }

    public void PlayMusicWithFade(string trackName, float fadeDuration = 1f)
    {
        StartCoroutine(FadeAndSwitch(trackName, fadeDuration));
    }

    private IEnumerator FadeAndSwitch(string trackName, float duration)
    {
        float startVol = musicSource.volume;
        for (float t = 0; t < duration; t += Time.deltaTime)
        {
            musicSource.volume = Mathf.Lerp(startVol, 0f, t / duration);
            yield return null;
        }
        PlayMusic(trackName);
    }

    public void StopMusic() => musicSource.Stop();
    public void PauseMusic() => musicSource.Pause();
    public void ResumeMusic() => musicSource.UnPause();

    // ─────────────────────────────────────────────────────────────────────
    // SFX
    // ─────────────────────────────────────────────────────────────────────

    //public void PlaySFX(string sfxName)
    //{
    //    if (!IsSFXOn) return;
    //    if (!_sfxDict.TryGetValue(sfxName, out var sfx)) { Debug.LogWarning($"[SoundManager] SFX not found: {sfxName}"); return; }

    //    sfxSource.pitch = sfx.pitch;
    //    sfxSource.PlayOneShot(sfx.clip, sfx.volume * MasterVolume * SFXVolume);
    //}

    public void PlaySFX(string sfxName)
    {
        if (!IsSFXOn) return;
        if (!_sfxDict.TryGetValue(sfxName, out var sfx)) { Debug.LogWarning($"[SoundManager] SFX not found: {sfxName}"); return; }

        _oneShotSources.RemoveAll(s => s == null);

        AudioSource temp = gameObject.AddComponent<AudioSource>();
        temp.clip = sfx.clip;
        temp.volume = sfx.volume * MasterVolume * SFXVolume;
        temp.pitch = sfx.pitch;
        temp.PlayOneShot(sfx.clip, sfx.volume * MasterVolume * SFXVolume);
        _oneShotSources.Add(temp);
        Destroy(temp, sfx.clip.length / Mathf.Abs(sfx.pitch) + 0.1f);
    }

    public void PlaySFXLoop(string sfxName)
    {
        if (!IsSFXOn) return;
        if (!_sfxDict.TryGetValue(sfxName, out var sfx)) { Debug.LogWarning($"[SoundManager] SFX not found: {sfxName}"); return; }

        sfxLoopSource.clip = sfx.clip;
        sfxLoopSource.volume = sfx.volume * MasterVolume * SFXVolume;
        sfxLoopSource.pitch = sfx.pitch;
        sfxLoopSource.loop = true;
        sfxLoopSource.Play();
    }

    public void StopSFXLoop()
    {
        sfxLoopSource.Stop();
        sfxLoopSource.clip = null;
    }

    /// <summary>Instantly stops and destroys every in-flight one-shot SFX (e.g. countdown ticks). Call on demand, e.g. when leaving a scene mid-sound.</summary>
    public void StopAllOneShotSFX()
    {
        foreach (var s in _oneShotSources)
        {
            if (s != null)
            {
                s.Stop();
                Destroy(s);
            }
        }
        _oneShotSources.Clear();
    }

    // ─────────────────────────────────────────────────────────────────────
    // SFX Data accessors  (used by per-object AudioSources, e.g. customers)
    // ─────────────────────────────────────────────────────────────────────

    /// <summary>Returns the AudioClip for an SFX key, or null if not found.</summary>
    public AudioClip GetSFXClip(string sfxName)
    {
        return _sfxDict.TryGetValue(sfxName, out var sfx) ? sfx.clip : null;
    }

    /// <summary>Returns the final volume (sfx.volume * master * sfxVol) for an SFX key.</summary>
    public float GetSFXVolume(string sfxName)
    {
        return _sfxDict.TryGetValue(sfxName, out var sfx) ? sfx.volume * MasterVolume * SFXVolume : 1f;
    }

    /// <summary>Returns the pitch for an SFX key.</summary>
    public float GetSFXPitch(string sfxName)
    {
        return _sfxDict.TryGetValue(sfxName, out var sfx) ? sfx.pitch : 1f;
    }

    // ─────────────────────────────────────────────────────────────────────
    // Volume Controls
    // ─────────────────────────────────────────────────────────────────────

    public void SetMasterVolume(float value)
    {
        MasterVolume = Mathf.Clamp01(value);
        PlayerPrefs.SetFloat(KEY_MASTER_VOL, MasterVolume);
        ApplyVolumes();
        OnSettingsChanged?.Invoke();
    }

    public void SetMusicVolume(float value)
    {
        MusicVolume = Mathf.Clamp01(value);
        PlayerPrefs.SetFloat(KEY_MUSIC_VOL, MusicVolume);
        ApplyVolumes();
        OnSettingsChanged?.Invoke();
    }

    public void SetSFXVolume(float value)
    {
        SFXVolume = Mathf.Clamp01(value);
        PlayerPrefs.SetFloat(KEY_SFX_VOL, SFXVolume);
        OnSettingsChanged?.Invoke();
    }

    public void ToggleMusic()
    {
        IsMusicOn = !IsMusicOn;
        PlayerPrefs.SetInt(KEY_MUSIC_ON, IsMusicOn ? 1 : 0);
        musicSource.volume = IsMusicOn ? MusicVolume * MasterVolume : 0f;
        OnSettingsChanged?.Invoke();
    }

    public void ToggleSFX()
    {
        IsSFXOn = !IsSFXOn;
        PlayerPrefs.SetInt(KEY_SFX_ON, IsSFXOn ? 1 : 0);
        if (!IsSFXOn)
        {
            sfxLoopSource.Pause();
        }
        else if (sfxLoopSource.clip != null)
        {
            sfxLoopSource.UnPause();
        }
        OnSettingsChanged?.Invoke();
    }

    // ─────────────────────────────────────────────────────────────────────
    // Internals
    // ─────────────────────────────────────────────────────────────────────

    private void LoadSettings()
    {
        MasterVolume = PlayerPrefs.GetFloat(KEY_MASTER_VOL, 1f);
        MusicVolume = PlayerPrefs.GetFloat(KEY_MUSIC_VOL, 1f);
        SFXVolume = PlayerPrefs.GetFloat(KEY_SFX_VOL, 1f);
        IsMusicOn = PlayerPrefs.GetInt(KEY_MUSIC_ON, 1) == 1;
        IsSFXOn = PlayerPrefs.GetInt(KEY_SFX_ON, 1) == 1;
    }

    private void ApplyVolumes()
    {
        if (musicSource.isPlaying)
            musicSource.volume = IsMusicOn ? MusicVolume * MasterVolume : 0f;
    }
}