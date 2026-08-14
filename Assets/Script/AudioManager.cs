using UnityEngine;

public enum MusicType
{
    MainMenu,
    LevelSelect,
    GamePlay
}

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }
    [Header("Audio Sources")]
    [SerializeField] private AudioSource musicSource;
    [SerializeField] private AudioSource sfxSource;
    [SerializeField] private AudioSource uiSource;

    [Header("Background Music Clips")]
    [SerializeField] private AudioClip mainMenuMusic;
    [SerializeField] private AudioClip levelSelectMusic;
    [SerializeField] private AudioClip gamePlayMusic;

    [Header("Ui Sound Effects Clips")]
    [SerializeField] private AudioClip buttonClickSound;

    [Header("Results Effect Clips")]
    [SerializeField] private AudioClip winSound;
    [SerializeField] private AudioClip loseSound;

    [Header("GamePlay Effect Clips")]
    [SerializeField] private SfxEntry[] sfxLibrary;

    [Header("Volume Settings")]
    [Range(0f, 1f)]
    [SerializeField] public float musicVolume = 1f;
    [Range(0f, 1f)]
    [SerializeField] public float sfxVolume = 1f;
    [Range(0f, 1f)]
    [SerializeField] public float uiVolume = 1f;

    private const string MusicVolumeKey = "MusicVolume";
    private const string SfxVolumeKey = "SfxVolume";
    private const string UiVolumeKey = "UiVolume";

    [System.Serializable]
    public class SfxEntry
    {
        public string name;
        public AudioClip clip;
    }
    private MusicType? currentMusicType = null;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
            return;
        }
        LoadVolumeSetting();
        ApplyVolumes();
    }
    private void ApplyVolumes()
    {
        musicSource.volume = musicVolume;
        sfxSource.volume = sfxVolume;
        uiSource.volume = uiVolume;
    }
    private void LoadVolumeSetting()
    {
        musicVolume = PlayerPrefs.GetFloat(MusicVolumeKey, 1f);
        sfxVolume = PlayerPrefs.GetFloat(SfxVolumeKey, 1f);
        uiVolume = PlayerPrefs.GetFloat(UiVolumeKey, 1f);
    }

    public void PlayMusicFor(MusicType musicType, bool forceRestart = false)
    {
        if (!forceRestart && currentMusicType == musicType && musicSource.isPlaying)
        {
            return;
        }
        AudioClip clip = GetMusicClip(musicType);
        if (clip == null)
        {
            Debug.LogWarning($"No music clip found for {musicType}");
            return;
        }

        currentMusicType = musicType;
        musicSource.clip = clip;
        musicSource.loop = true;
        musicSource.Play();
    }
    private AudioClip GetMusicClip(MusicType musicType)
    {
        switch (musicType)
        {
            case MusicType.MainMenu:
                return mainMenuMusic;
            case MusicType.LevelSelect:
                return levelSelectMusic;
            case MusicType.GamePlay:
                return gamePlayMusic;
            default:
                return null;
        }
    }
    public void StopMusic()
    {
        musicSource.Stop();
        currentMusicType = null;
    }
    public void ResumeMusic()
    {
        if (currentMusicType.HasValue && !musicSource.isPlaying)
        {
            musicSource.clip = GetMusicClip(currentMusicType.Value);
            musicSource.loop = true;
            musicSource.Play();
        }
    }
    public void PlaySfx(string sfxName)
    {
        AudioClip clip = FindSfxClip(sfxName);
        if (clip == null)
        {
            Debug.LogWarning($"No SFX clip found for {sfxName}");
            return;
        }
        sfxSource.PlayOneShot(clip, sfxVolume);
    }
    public void PlaySfx(AudioClip clip)
    {
        if (clip == null)
        {
            Debug.LogWarning("Attempted to play a null SFX clip.");
            return;
        }
        sfxSource.PlayOneShot(clip, sfxVolume);
    }
    private AudioClip FindSfxClip(string sfxName)
    {
        foreach (var entry in sfxLibrary)
        {
            if (entry.name == sfxName)
            {
                return entry.clip;
            }
        }
        return null;
    }

    public void PlayWin()
    {
        StopMusic();
        if (winSound == null)
        {
            Debug.LogWarning("Win sound clip is not assigned.");
            return;
        }
        sfxSource.PlayOneShot(winSound, sfxVolume);
    }
    public void PlayLose()
    {
        StopMusic();
        if (loseSound == null)
        {
            Debug.LogWarning("Lose sound clip is not assigned.");
            return;
        }
        sfxSource.PlayOneShot(loseSound, sfxVolume);
    }

    public void PlayButtonClick()
    {
        if (buttonClickSound == null)
        {
            Debug.LogWarning("Button click sound clip is not assigned.");
            return;
        }
        uiSource.PlayOneShot(buttonClickSound, uiVolume);
    }

    public void SetMusicVolume(float value)
    {
        musicVolume = value;
        musicSource.volume = value;
        PlayerPrefs.SetFloat(MusicVolumeKey, value);
    }
    public void SetSfxVolume(float value)
    {
        sfxVolume = value;
        sfxSource.volume = value;
        PlayerPrefs.SetFloat(SfxVolumeKey, value);
    }
    public void SetUiVolume(float value)
    {
        uiVolume = value;
        uiSource.volume = value;
        PlayerPrefs.SetFloat(UiVolumeKey, value);
    }
    public void SaveVolumeSettings()
    {
        PlayerPrefs.Save();
    }
}
