using UnityEngine;
using UnityEngine.SceneManagement;

public class MusicManager : MonoBehaviour
{
    public static MusicManager Instance { get; private set; }

    [Header("Music Clips")]
    public AudioClip homeMusic;
    public AudioClip gameplayMusic;

    [Header("Scene Names")]
    public string homeSceneName = "HomeScene";
    public string gameplaySceneName = "GamePlayScene";

    [Header("Settings")]
    [Range(0f, 1f)] public float volume = 0.5f;

    private AudioSource _source;
    public bool IsMuted => _source != null && _source.mute;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);

            _source = gameObject.AddComponent<AudioSource>();
            _source.loop = true;
            _source.volume = volume;
            _source.playOnAwake = false;

            // Load saved mute state (0 = unmuted, 1 = muted)
            bool savedMute = PlayerPrefs.GetInt("MusicMuted", 0) == 1;
            _source.mute = savedMute;
        }
        else
        {
            Destroy(gameObject);
            return;
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
        if (scene.name == homeSceneName)
        {
            ChangeMusic(homeMusic);
        }
        else if (scene.name == gameplaySceneName)
        {
            ChangeMusic(gameplayMusic);
        }
    }

    public void ChangeMusic(AudioClip newClip)
    {
        if (_source == null || newClip == null) return;
        if (_source.clip == newClip && _source.isPlaying) return;

        _source.Stop();
        _source.clip = newClip;
        _source.Play();
    }

    public void SetMuted(bool muted)
    {
        if (_source != null)
        {
            _source.mute = muted;
        }
    }

    public void SetVolume(float newVolume)
    {
        volume = Mathf.Clamp01(newVolume);
        if (_source != null)
        {
            _source.volume = volume;
        }
    }
}
