using UnityEngine;
using UnityEngine.UI;

public class SettingsPanelController : MonoBehaviour
{
    [Header("Music Buttons")]
    public Button musicOnButton;   // Green checkmark button
    public Button musicOffButton;  // Red X button

    [Header("Sound (SFX) Buttons")]
    public Button soundOnButton;   // Green checkmark button
    public Button soundOffButton;  // Red X button

    [Header("Save Button")]
    public Button saveButton;

    [Header("Visual Feedback")]
    public Color activeColor = Color.white;
    public Color inactiveColor = new Color(0.5f, 0.5f, 0.5f, 0.7f);

    private bool _tempMusicMuted;
    private bool _tempSfxMuted;

    private void Awake()
    {
        // Hook up button clicks via code
        musicOnButton.onClick.AddListener(() => SetMusicState(false));
        musicOffButton.onClick.AddListener(() => SetMusicState(true));

        soundOnButton.onClick.AddListener(() => SetSoundState(false));
        soundOffButton.onClick.AddListener(() => SetSoundState(true));

        saveButton.onClick.AddListener(SaveAndClose);
    }

    private void OnEnable()
    {
        // Load currently saved settings every time the panel opens
        _tempMusicMuted = PlayerPrefs.GetInt("MusicMuted", 0) == 1;
        _tempSfxMuted = PlayerPrefs.GetInt("SFXMuted", 0) == 1;

        UpdateButtonVisuals();
    }

    private void SetMusicState(bool isMuted)
    {
        _tempMusicMuted = isMuted;

        if (MusicManager.Instance != null)
            MusicManager.Instance.SetMuted(_tempMusicMuted);

        UpdateButtonVisuals();
    }

    private void SetSoundState(bool isMuted)
    {
        _tempSfxMuted = isMuted;

        if (SFXManager.Instance != null)
            SFXManager.Instance.SetMuted(_tempSfxMuted);

        UpdateButtonVisuals();
    }

    private void UpdateButtonVisuals()
    {
        // Music visual highlight (On = not muted, Off = muted)
        musicOnButton.image.color = !_tempMusicMuted ? activeColor : inactiveColor;
        musicOffButton.image.color = _tempMusicMuted ? activeColor : inactiveColor;

        // Sound visual highlight (On = not muted, Off = muted)
        soundOnButton.image.color = !_tempSfxMuted ? activeColor : inactiveColor;
        soundOffButton.image.color = _tempSfxMuted ? activeColor : inactiveColor;
    }

    public void SaveAndClose()
    {
        PlayerPrefs.SetInt("MusicMuted", _tempMusicMuted ? 1 : 0);
        PlayerPrefs.SetInt("SFXMuted", _tempSfxMuted ? 1 : 0);
        PlayerPrefs.Save();

        gameObject.SetActive(false);
    }
}
