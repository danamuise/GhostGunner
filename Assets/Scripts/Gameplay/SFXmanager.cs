// 10/5/2025 AI-Tag
// This was created with the help of Assistant, a Unity Artificial Intelligence product.

using UnityEngine;
using System.Collections.Generic;

public class SFXManager : MonoBehaviour
{
    public static SFXManager Instance;
    public static bool isClickingButton = false;

    [System.Serializable]
    public class NamedAudioClip
    {
        public string name;
        public AudioClip clip;
    }

    [Header("SFX Library")]
    public List<NamedAudioClip> soundLibrary = new List<NamedAudioClip>();

    [Header("Music Library")]
    public List<NamedAudioClip> musicLibrary = new List<NamedAudioClip>();

    private Dictionary<string, AudioSource> sfxSources = new Dictionary<string, AudioSource>();
    private Dictionary<string, AudioClip> musicClips = new Dictionary<string, AudioClip>();

    private AudioSource musicSource;
    private string lastRandomSFXName = null;

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

        BuildSFXSources();
        BuildMusicLibrary();

        GameObject musicGO = new GameObject("MusicSource");
        musicGO.transform.SetParent(transform);
        musicSource = musicGO.AddComponent<AudioSource>();
        musicSource.loop = true;
        musicSource.playOnAwake = false;
        musicSource.spatialBlend = 0f;
    }

    private void BuildSFXSources()
    {
        foreach (var entry in soundLibrary)
        {
            if (entry.clip == null || string.IsNullOrEmpty(entry.name)) continue;

            if (!sfxSources.ContainsKey(entry.name))
            {
                GameObject child = new GameObject($"SFX_{entry.name}");
                child.transform.SetParent(transform);

                AudioSource source = child.AddComponent<AudioSource>();
                source.clip = entry.clip;
                source.playOnAwake = false;
                source.spatialBlend = 0f;

                sfxSources.Add(entry.name, source);
                Debug.Log($"SFXSource created for: {entry.name}");
            }
        }
    }

    private void BuildMusicLibrary()
    {
        foreach (var entry in musicLibrary)
        {
            if (entry.clip == null || string.IsNullOrEmpty(entry.name)) continue;

            if (!musicClips.ContainsKey(entry.name))
            {
                musicClips.Add(entry.name, entry.clip);
            }
        }
    }

    // 🔊 Play SFX with optional pitch variation
    public void Play(string soundName, float volume = 1f, float pitchMin = 0.95f, float pitchMax = 1.05f)
    {
        bool sfxEnabled = PlayerPrefs.GetInt("SFX_ENABLED", 1) == 1;
        if (!sfxEnabled) return;

        if (!sfxSources.TryGetValue(soundName, out AudioSource source))
        {
            Debug.LogWarning($"🔇 SFXManager: No AudioSource found for '{soundName}'");
            return;
        }

        source.pitch = Random.Range(pitchMin, pitchMax);
        source.volume = volume;
        source.PlayOneShot(source.clip);

        Debug.Log($"🎧 Playing SFX: {soundName}");
    }

    public void PlayRandom(string[] soundNames, float volume = 1f, float pitchMin = 0.95f, float pitchMax = 1.05f)
    {
        bool sfxEnabled = PlayerPrefs.GetInt("SFX_ENABLED", 1) == 1;
        if (!sfxEnabled || soundNames == null || soundNames.Length == 0) return;

        string chosen;
        int attempts = 0;

        do
        {
            chosen = soundNames[Random.Range(0, soundNames.Length)];
            attempts++;
        } while (chosen == lastRandomSFXName && attempts < 5);

        lastRandomSFXName = chosen;
        Play(chosen, volume, pitchMin, pitchMax);
    }

    // 🎵 Play music by name
    public void PlayMusic(string musicName, float volume = 1f)
    {
        bool musicEnabled = PlayerPrefs.GetInt("MUSIC_ENABLED", 1) == 1;
        if (!musicEnabled) return;

        if (!musicClips.TryGetValue(musicName, out AudioClip clip))
        {
            Debug.LogWarning($"🎵 SFXManager: No music clip found for '{musicName}'");
            return;
        }

        if (musicSource.clip == clip && musicSource.isPlaying)
            return; // Already playing

        musicSource.clip = clip;
        musicSource.volume = volume;
        musicSource.Play();
    }

    public void StopMusic()
    {
        musicSource.Stop();
    }

    public void FadeOutMusic(float duration = 1f)
    {
        StartCoroutine(FadeMusicOut(duration));
    }

    private System.Collections.IEnumerator FadeMusicOut(float duration)
    {
        float startVol = musicSource.volume;
        float time = 0f;

        while (time < duration)
        {
            musicSource.volume = Mathf.Lerp(startVol, 0f, time / duration);
            time += Time.deltaTime;
            yield return null;
        }

        musicSource.Stop();
        musicSource.volume = startVol;
    }

    // 🔁 Play looping SFX
    public void PlayLoopingSFX(string soundName, float volume = 1f, float pitch = 1f)
    {
        bool sfxEnabled = PlayerPrefs.GetInt("SFX_ENABLED", 1) == 1;
        if (!sfxEnabled) return;

        if (!sfxSources.TryGetValue(soundName, out AudioSource source))
        {
            Debug.LogWarning($"🔇 SFXManager: No AudioSource found for '{soundName}'");
            return;
        }

        source.pitch = pitch;
        source.volume = volume;
        source.loop = true; // Enable looping
        source.Play();

        Debug.Log($"🔁 Playing looping SFX: {soundName}");
    }

    // ⏹️ Stop looping SFX
    public void StopLoopingSFX(string soundName, float fadeDuration = 0.5f)
    {
        if (!sfxSources.TryGetValue(soundName, out AudioSource source))
        {
            Debug.LogWarning($"🔇 SFXManager: No AudioSource found for '{soundName}'");
            return;
        }

        if (source.isPlaying)
        {
            StartCoroutine(FadeOutAndStop(source, fadeDuration));
        }
        else
        {
            source.loop = false; // Ensure looping is disabled
            source.Stop();
            Debug.Log($"⏹ Stopped looping SFX: {soundName}");
        }
    }

    // Coroutine to fade out the volume and stop the AudioSource
    private System.Collections.IEnumerator FadeOutAndStop(AudioSource source, float duration)
    {
        float startVolume = source.volume;
        float time = 0f;

        while (time < duration)
        {
            time += Time.deltaTime;
            source.volume = Mathf.Lerp(startVolume, 0f, time / duration);
            yield return null;
        }

        source.volume = startVolume; // Reset volume for future use
        source.loop = false; // Ensure looping is disabled
        source.Stop();

        Debug.Log($"⏹ Faded out and stopped looping SFX.");
    }
}