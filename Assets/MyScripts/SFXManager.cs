using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class SFXManager : MonoBehaviour
{
    public static SFXManager Instance { get; private set; }

    [Header("Setup")]
    public AudioClip clickSound;
    [Range(0f, 1f)] public float volume = 1f;

    private AudioSource _source;
    private bool _muted;
    public bool IsMuted => _muted;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);

            _source = gameObject.AddComponent<AudioSource>();
            _source.playOnAwake = false;

            // Load saved mute state (0 = unmuted, 1 = muted)
            bool savedMute = PlayerPrefs.GetInt("SFXMuted", 0) == 1;
            SetMuted(savedMute);
        }
        else
        {
            Destroy(gameObject);
            return;
        }
    }

    public void SetMuted(bool muted)
    {
        _muted = muted;
        if (_source != null)
        {
            _source.mute = muted;
        }
    }

    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        HookAllButtons();
    }

    public void HookAllButtons()
    {
        Button[] buttons = FindObjectsOfType<Button>(true);
        foreach (var button in buttons)
        {
            button.onClick.RemoveListener(PlayClick);
            button.onClick.AddListener(PlayClick);
        }
    }

    public void PlayClick()
    {
        if (_source != null && clickSound != null && !_muted)
        {
            _source.PlayOneShot(clickSound, volume);
        }
    }
}
