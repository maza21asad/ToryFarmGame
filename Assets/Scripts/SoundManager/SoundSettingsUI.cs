using UnityEngine;
using UnityEngine.UI;

public class SoundSettingsUI : MonoBehaviour
{
    // ─────────────────────────────────────────────────────────────────────
    // Feature Toggles
    // ─────────────────────────────────────────────────────────────────────

    [Header("── Feature Toggles ──────────────────")]
    [SerializeField] private bool useMasterSlider = true;
    [SerializeField] private bool useMusicSlider = true;
    [SerializeField] private bool useSFXSlider = true;
    [SerializeField] private bool useMusicToggle = true;
    [SerializeField] private bool useSFXToggle = true;
    [SerializeField] private bool useVibration = false;

    // ─────────────────────────────────────────────────────────────────────
    // Sliders
    // ─────────────────────────────────────────────────────────────────────

    [Header("── Sliders ──────────────────────────")]
    [SerializeField] private Slider masterSlider;
    [SerializeField] private Slider musicSlider;
    [SerializeField] private Slider sfxSlider;

    // ─────────────────────────────────────────────────────────────────────
    // Toggle Buttons — each has its own ON/OFF icon pair
    // ─────────────────────────────────────────────────────────────────────

    [Header("── Music Toggle ─────────────────────")]
    [SerializeField] private Button musicToggleBtn;
    [SerializeField] private Image musicBtnIcon;
    [SerializeField] private Sprite musicIconOn;
    [SerializeField] private Sprite musicIconOff;

    [Header("── SFX Toggle ───────────────────────")]
    [SerializeField] private Button sfxToggleBtn;
    [SerializeField] private Image sfxBtnIcon;
    [SerializeField] private Sprite sfxIconOn;
    [SerializeField] private Sprite sfxIconOff;

    [Header("── Vibration Toggle ─────────────────")]
    [SerializeField] private Button vibrationToggleBtn;
    [SerializeField] private Image vibrationBtnIcon;
    [SerializeField] private Sprite vibrationIconOn;
    [SerializeField] private Sprite vibrationIconOff;

    // ─────────────────────────────────────────────────────────────────────
    // Vibration State
    // ─────────────────────────────────────────────────────────────────────

    private const string KEY_VIBRATION = "VibrationOn";
    public bool IsVibrationOn { get; private set; }

    // ─────────────────────────────────────────────────────────────────────
    // Init
    // ─────────────────────────────────────────────────────────────────────

    private void Start()
    {
        InitSliders();
        InitToggleButtons();
        InitVibration();

        SoundManager.Instance.OnSettingsChanged += RefreshUI;
    }

    private void OnDestroy()
    {
        if (SoundManager.Instance != null)
            SoundManager.Instance.OnSettingsChanged -= RefreshUI;
    }

    // ─────────────────────────────────────────────────────────────────────
    // Sliders
    // ─────────────────────────────────────────────────────────────────────

    private void InitSliders()
    {
        var sm = SoundManager.Instance;
        SetupSlider(useMasterSlider, masterSlider, sm.MasterVolume, sm.SetMasterVolume);
        SetupSlider(useMusicSlider, musicSlider, sm.MusicVolume, sm.SetMusicVolume);
        SetupSlider(useSFXSlider, sfxSlider, sm.SFXVolume, sm.SetSFXVolume);
    }

    private void SetupSlider(bool isEnabled, Slider slider, float initValue, UnityEngine.Events.UnityAction<float> callback)
    {
        if (!isEnabled || slider == null)
        {
            if (slider != null) slider.gameObject.SetActive(false);
            return;
        }
        slider.gameObject.SetActive(true);
        slider.SetValueWithoutNotify(initValue);
        slider.onValueChanged.AddListener(callback);
    }

    // ─────────────────────────────────────────────────────────────────────
    // Toggle Buttons
    // ─────────────────────────────────────────────────────────────────────

    private void InitToggleButtons()
    {
        var sm = SoundManager.Instance;

        SetupToggleButton(useMusicToggle, musicToggleBtn, () => { sm.ToggleMusic(); RefreshIcons(); });
        SetupToggleButton(useSFXToggle, sfxToggleBtn, () => { sm.ToggleSFX(); RefreshIcons(); });

        RefreshIcons();
    }

    private void SetupToggleButton(bool isEnabled, Button btn, System.Action callback)
    {
        if (!isEnabled || btn == null)
        {
            if (btn != null) btn.gameObject.SetActive(false);
            return;
        }
        btn.gameObject.SetActive(true);
        btn.onClick.AddListener(() => callback());
    }

    // ─────────────────────────────────────────────────────────────────────
    // Vibration
    // ─────────────────────────────────────────────────────────────────────

    private void InitVibration()
    {
        if (!useVibration || vibrationToggleBtn == null)
        {
            if (vibrationToggleBtn != null) vibrationToggleBtn.gameObject.SetActive(false);
            return;
        }

        IsVibrationOn = PlayerPrefs.GetInt(KEY_VIBRATION, 1) == 1;
        vibrationToggleBtn.gameObject.SetActive(true);
        vibrationToggleBtn.onClick.AddListener(ToggleVibration);

        RefreshIcons();
    }

    private void ToggleVibration()
    {
        IsVibrationOn = !IsVibrationOn;
        PlayerPrefs.SetInt(KEY_VIBRATION, IsVibrationOn ? 1 : 0);
        if (IsVibrationOn) TriggerVibration();
        RefreshIcons();
    }

    public void TriggerVibration()
    {
        if (!useVibration || !IsVibrationOn) return;
#if UNITY_ANDROID || UNITY_IOS
        Handheld.Vibrate();
#endif
    }

    // ─────────────────────────────────────────────────────────────────────
    // UI Refresh
    // ─────────────────────────────────────────────────────────────────────

    private void RefreshUI()
    {
        var sm = SoundManager.Instance;
        if (useMasterSlider && masterSlider != null) masterSlider.SetValueWithoutNotify(sm.MasterVolume);
        if (useMusicSlider && musicSlider != null) musicSlider.SetValueWithoutNotify(sm.MusicVolume);
        if (useSFXSlider && sfxSlider != null) sfxSlider.SetValueWithoutNotify(sm.SFXVolume);
        RefreshIcons();
    }

    private void RefreshIcons()
    {
        var sm = SoundManager.Instance;
        SetIcon(useMusicToggle, musicBtnIcon, sm.IsMusicOn, musicIconOn, musicIconOff);
        SetIcon(useSFXToggle, sfxBtnIcon, sm.IsSFXOn, sfxIconOn, sfxIconOff);
        SetIcon(useVibration, vibrationBtnIcon, IsVibrationOn, vibrationIconOn, vibrationIconOff);
    }

    // ─────────────────────────────────────────────────────────────────────
    // Helper
    // ─────────────────────────────────────────────────────────────────────

    private void SetIcon(bool isEnabled, Image icon, bool state, Sprite onSprite, Sprite offSprite)
    {
        if (!isEnabled || icon == null) return;
        icon.sprite = state ? onSprite : offSprite;
    }

    // ─────────────────────────────────────────────────────────────────────
    // Sound Callers  ← use these directly in Button OnClick() Inspector
    // ─────────────────────────────────────────────────────────────────────

    public void PlaySFX(string sfxName)
    {
        SoundManager.Instance.PlaySFX(sfxName);
    }

    public void PlayMusic(string trackName)
    {
        SoundManager.Instance.PlayMusic(trackName);
    }

    public void PlayMusicWithFade(string trackName)
    {
        SoundManager.Instance.PlayMusicWithFade(trackName);
    }

    public void StopMusic()
    {
        SoundManager.Instance.StopMusic();
    }

}