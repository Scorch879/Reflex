using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class BackgroundMusic : MonoBehaviour
{
    private const string VolumePrefKey = "BackgroundMusic.Volume";
    private const string MutedPrefKey = "BackgroundMusic.Muted";
    private const float FallbackDefaultVolume = 0.7f;

    private static BackgroundMusic instance;

    [SerializeField] private AudioClip musicClip;
    [SerializeField, Range(0f, 1f)] private float defaultVolume = FallbackDefaultVolume;

    private AudioSource audioSource;
    private float currentVolume;
    private bool isMuted;

    public static BackgroundMusic Instance => instance;
    public float Volume => currentVolume;
    public bool IsMuted => isMuted;

    public static BackgroundMusic EnsureInstance(AudioClip clip = null)
    {
        if (instance != null)
        {
            instance.AssignClipIfMissing(clip);
            return instance;
        }

        BackgroundMusic existingMusic = FindFirstObjectByType<BackgroundMusic>();
        if (existingMusic != null)
        {
            existingMusic.AssignClipIfMissing(clip);
            return existingMusic;
        }

        GameObject musicObject = new GameObject("Background Music");
        BackgroundMusic music = musicObject.AddComponent<BackgroundMusic>();
        music.AssignClipIfMissing(clip);
        return music;
    }

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            AudioClip duplicateClip = musicClip;
            AudioSource duplicateSource = GetComponent<AudioSource>();
            if (duplicateClip == null && duplicateSource != null)
            {
                duplicateClip = duplicateSource.clip;
            }

            instance.AssignClipIfMissing(duplicateClip);
            Destroy(gameObject);
            return;
        }

        instance = this;
        DontDestroyOnLoad(gameObject);

        ResolveAudioSource();
        LoadSettings();
        ApplySettings();
        PlayIfReady();
    }

    public void SetVolume(float volume)
    {
        currentVolume = Mathf.Clamp01(volume);
        PlayerPrefs.SetFloat(VolumePrefKey, currentVolume);
        PlayerPrefs.Save();
        ApplySettings();
    }

    public void SetMuted(bool muted)
    {
        isMuted = muted;
        PlayerPrefs.SetInt(MutedPrefKey, isMuted ? 1 : 0);
        PlayerPrefs.Save();
        ApplySettings();
    }

    private void AssignClipIfMissing(AudioClip clip)
    {
        if (clip == null)
        {
            return;
        }

        ResolveAudioSource();
        if (musicClip == null)
        {
            musicClip = clip;
        }

        if (audioSource != null && audioSource.clip == null)
        {
            audioSource.clip = clip;
        }

        PlayIfReady();
    }

    private void ResolveAudioSource()
    {
        if (audioSource == null)
        {
            audioSource = GetComponent<AudioSource>();
        }

        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }

        audioSource.loop = true;
        audioSource.playOnAwake = true;
        audioSource.spatialBlend = 0f;

        if (audioSource.clip == null && musicClip != null)
        {
            audioSource.clip = musicClip;
        }
    }

    private void LoadSettings()
    {
        currentVolume = PlayerPrefs.GetFloat(VolumePrefKey, Mathf.Clamp01(defaultVolume));
        isMuted = PlayerPrefs.GetInt(MutedPrefKey, 0) == 1;
    }

    private void ApplySettings()
    {
        ResolveAudioSource();
        if (audioSource == null)
        {
            return;
        }

        audioSource.volume = currentVolume;
        audioSource.mute = isMuted;
    }

    private void PlayIfReady()
    {
        ResolveAudioSource();
        if (audioSource != null && audioSource.clip != null && !audioSource.isPlaying)
        {
            audioSource.Play();
        }
    }
}
