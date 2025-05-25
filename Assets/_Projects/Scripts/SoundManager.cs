using UnityEngine;
using System.Collections.Generic;
using System.Collections;

public enum SoundEffect
{
    DecoSpawn,
    Hearts,
    InvalidPlace,
    OpenBox,
    PickupDeco,
    PlaceDeco,
    PressButton,
    ResultSpawn,
    Stars,
    Ticking,
    DoorClose
    
}

public enum Music
{
    MenuTheme,
    GameplayTheme,
    ResultsTheme
}

public class SoundManager : MonoBehaviour
{
    [Header("Audio Sources")]
    [SerializeField] private AudioSource sfxAudioSource;
    [SerializeField] private AudioSource musicAudioSource;
    
    [Header("Volume Settings")]
    [Range(0f, 1f)] [SerializeField] private float sfxVolume = 1f;
    [Range(0f, 1f)] [SerializeField] private float musicVolume = 0.7f;
    [SerializeField] private bool muteSFX = false;
    [SerializeField] private bool muteMusic = false;
    
    [Header("Settings")]
    [SerializeField] private bool preloadAllSounds = true;
    [SerializeField] private bool debugMode = true;
    
    // Singleton
    public static SoundManager Instance { get; private set; }
    
    // Sound caches
    private Dictionary<SoundEffect, AudioClip> sfxClips = new Dictionary<SoundEffect, AudioClip>();
    private Dictionary<Music, AudioClip> musicClips = new Dictionary<Music, AudioClip>();
    
    // Current music tracking
    private Music? currentMusic = null;
    private Coroutine musicFadeCoroutine = null;
    
    // Events
    public static System.Action<SoundEffect> OnSFXPlayed;
    public static System.Action<Music> OnMusicStarted;
    public static System.Action OnMusicStopped;
    
    private void Awake()
    {
        // Singleton setup
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            SetupAudioSources();
            
            if (preloadAllSounds)
            {
                PreloadAllSounds();
            }
        }
        else
        {
            Destroy(gameObject);
        }
    }
    
    private void SetupAudioSources()
    {
        // Create SFX audio source if not assigned
        if (sfxAudioSource == null)
        {
            GameObject sfxObj = new GameObject("SFX AudioSource");
            sfxObj.transform.SetParent(transform);
            sfxAudioSource = sfxObj.AddComponent<AudioSource>();
            sfxAudioSource.playOnAwake = false;
            sfxAudioSource.volume = sfxVolume;
        }
        
        // Create Music audio source if not assigned
        if (musicAudioSource == null)
        {
            GameObject musicObj = new GameObject("Music AudioSource");
            musicObj.transform.SetParent(transform);
            musicAudioSource = musicObj.AddComponent<AudioSource>();
            musicAudioSource.playOnAwake = false;
            musicAudioSource.volume = musicVolume;
            musicAudioSource.loop = true;
        }
    }
    
    private void PreloadAllSounds()
    {
        if (debugMode)
            Debug.Log("SoundManager: Preloading all sounds...");
        
        // Preload SFX
        foreach (SoundEffect sfx in System.Enum.GetValues(typeof(SoundEffect)))
        {
            LoadSFXClip(sfx);
        }
        
        // Preload Music
        foreach (Music music in System.Enum.GetValues(typeof(Music)))
        {
            LoadMusicClip(music);
        }
        
        if (debugMode)
            Debug.Log($"SoundManager: Preloaded {sfxClips.Count} SFX clips and {musicClips.Count} music clips");
    }
    
    private AudioClip LoadSFXClip(SoundEffect sfx)
    {
        if (sfxClips.ContainsKey(sfx))
            return sfxClips[sfx];
        
        string resourcePath = $"Audio/SFX/{sfx}";
        AudioClip clip = Resources.Load<AudioClip>(resourcePath);
        
        if (clip != null)
        {
            sfxClips[sfx] = clip;
            if (debugMode)
                Debug.Log($"SoundManager: Loaded SFX '{sfx}' from {resourcePath}");
        }
        else
        {
            Debug.LogWarning($"SoundManager: Could not load SFX '{sfx}' from {resourcePath}");
        }
        
        return clip;
    }
    
    private AudioClip LoadMusicClip(Music music)
    {
        if (musicClips.ContainsKey(music))
            return musicClips[music];
        
        string resourcePath = $"Audio/Music/{music}";
        AudioClip clip = Resources.Load<AudioClip>(resourcePath);
        
        if (clip != null)
        {
            musicClips[music] = clip;
            if (debugMode)
                Debug.Log($"SoundManager: Loaded Music '{music}' from {resourcePath}");
        }
        else
        {
            Debug.LogWarning($"SoundManager: Could not load Music '{music}' from {resourcePath}");
        }
        
        return clip;
    }
    
    // SFX Methods
    public void PlaySFX(SoundEffect sfx, float volumeMultiplier = 1f)
    {
        if (muteSFX || sfxAudioSource == null) return;
        
        AudioClip clip = LoadSFXClip(sfx);
        if (clip != null)
        {
            sfxAudioSource.PlayOneShot(clip, sfxVolume * volumeMultiplier);
            OnSFXPlayed?.Invoke(sfx);
            
            if (debugMode)
                Debug.Log($"SoundManager: Played SFX '{sfx}' at volume {sfxVolume * volumeMultiplier:F2}");
        }
    }
    
    public void PlaySFXWithPitch(SoundEffect sfx, float pitch, float volumeMultiplier = 1f)
    {
        if (muteSFX || sfxAudioSource == null) return;
        
        AudioClip clip = LoadSFXClip(sfx);
        if (clip != null)
        {
            // Store original pitch
            float originalPitch = sfxAudioSource.pitch;
            
            // Set new pitch and play
            sfxAudioSource.pitch = pitch;
            sfxAudioSource.PlayOneShot(clip, sfxVolume * volumeMultiplier);
            
            // Reset pitch after a short delay
            StartCoroutine(ResetPitchAfterDelay(originalPitch, 0.1f));
            
            OnSFXPlayed?.Invoke(sfx);
            
            if (debugMode)
                Debug.Log($"SoundManager: Played SFX '{sfx}' with pitch {pitch} at volume {sfxVolume * volumeMultiplier:F2}");
        }
    }
    
    private IEnumerator ResetPitchAfterDelay(float originalPitch, float delay)
    {
        yield return new WaitForSeconds(delay);
        if (sfxAudioSource != null)
            sfxAudioSource.pitch = originalPitch;
    }
    
    // Music Methods
    public void PlayMusic(Music music, bool fadeIn = true, float fadeTime = 1f)
    {
        if (muteMusic || musicAudioSource == null) return;
        
        AudioClip clip = LoadMusicClip(music);
        if (clip == null) return;
        
        // Stop any current fade coroutine
        if (musicFadeCoroutine != null)
        {
            StopCoroutine(musicFadeCoroutine);
        }
        
        // If same music is already playing, don't restart
        if (currentMusic == music && musicAudioSource.isPlaying)
        {
            if (debugMode)
                Debug.Log($"SoundManager: Music '{music}' is already playing");
            return;
        }
        
        currentMusic = music;
        
        if (fadeIn && musicAudioSource.isPlaying)
        {
            // Crossfade to new music
            musicFadeCoroutine = StartCoroutine(CrossfadeMusic(clip, fadeTime));
        }
        else
        {
            // Start music immediately
            musicAudioSource.clip = clip;
            musicAudioSource.volume = fadeIn ? 0f : musicVolume;
            musicAudioSource.Play();
            
            if (fadeIn)
            {
                musicFadeCoroutine = StartCoroutine(FadeIn(fadeTime));
            }
        }
        
        OnMusicStarted?.Invoke(music);
        
        if (debugMode)
            Debug.Log($"SoundManager: Started playing music '{music}'{(fadeIn ? " with fade-in" : "")}");
    }
    
    public void StopMusic(bool fadeOut = true, float fadeTime = 1f)
    {
        if (musicAudioSource == null) return;
        
        // Stop any current fade coroutine
        if (musicFadeCoroutine != null)
        {
            StopCoroutine(musicFadeCoroutine);
        }
        
        if (fadeOut && musicAudioSource.isPlaying)
        {
            musicFadeCoroutine = StartCoroutine(FadeOut(fadeTime));
        }
        else
        {
            musicAudioSource.Stop();
            currentMusic = null;
        }
        
        OnMusicStopped?.Invoke();
        
        if (debugMode)
            Debug.Log($"SoundManager: Stopped music{(fadeOut ? " with fade-out" : "")}");
    }
    
    public void PauseMusic()
    {
        if (musicAudioSource != null && musicAudioSource.isPlaying)
        {
            musicAudioSource.Pause();
            
            if (debugMode)
                Debug.Log("SoundManager: Paused music");
        }
    }
    
    public void ResumeMusic()
    {
        if (musicAudioSource != null && !musicAudioSource.isPlaying && musicAudioSource.clip != null)
        {
            musicAudioSource.UnPause();
            
            if (debugMode)
                Debug.Log("SoundManager: Resumed music");
        }
    }
    
    // Volume Control
    public void SetSFXVolume(float volume)
    {
        sfxVolume = Mathf.Clamp01(volume);
        if (sfxAudioSource != null)
            sfxAudioSource.volume = sfxVolume;
    }
    
    public void SetMusicVolume(float volume)
    {
        musicVolume = Mathf.Clamp01(volume);
        if (musicAudioSource != null)
            musicAudioSource.volume = musicVolume;
    }
    
    public void SetMuteSFX(bool mute)
    {
        muteSFX = mute;
    }
    
    public void SetMuteMusic(bool mute)
    {
        muteMusic = mute;
        if (mute && musicAudioSource != null && musicAudioSource.isPlaying)
        {
            musicAudioSource.Pause();
        }
        else if (!mute && musicAudioSource != null && !musicAudioSource.isPlaying && musicAudioSource.clip != null)
        {
            musicAudioSource.UnPause();
        }
    }
    
    // Fade Coroutines
    private IEnumerator FadeIn(float fadeTime)
    {
        float startVolume = 0f;
        musicAudioSource.volume = startVolume;
        
        float timer = 0f;
        while (timer < fadeTime)
        {
            timer += Time.deltaTime;
            float progress = timer / fadeTime;
            musicAudioSource.volume = Mathf.Lerp(startVolume, musicVolume, progress);
            yield return null;
        }
        
        musicAudioSource.volume = musicVolume;
        musicFadeCoroutine = null;
    }
    
    private IEnumerator FadeOut(float fadeTime)
    {
        float startVolume = musicAudioSource.volume;
        
        float timer = 0f;
        while (timer < fadeTime)
        {
            timer += Time.deltaTime;
            float progress = timer / fadeTime;
            musicAudioSource.volume = Mathf.Lerp(startVolume, 0f, progress);
            yield return null;
        }
        
        musicAudioSource.volume = 0f;
        musicAudioSource.Stop();
        currentMusic = null;
        musicFadeCoroutine = null;
    }
    
    private IEnumerator CrossfadeMusic(AudioClip newClip, float fadeTime)
    {
        float halfFadeTime = fadeTime * 0.5f;
        
        // Fade out current music
        yield return StartCoroutine(FadeOut(halfFadeTime));
        
        // Start new music
        musicAudioSource.clip = newClip;
        musicAudioSource.Play();
        
        // Fade in new music
        yield return StartCoroutine(FadeIn(halfFadeTime));
        
        musicFadeCoroutine = null;
    }
    
    // Utility Methods
    public bool IsMusicPlaying() => musicAudioSource != null && musicAudioSource.isPlaying;
    public bool IsMusicPlaying(Music music) => currentMusic == music && IsMusicPlaying();
    public Music? GetCurrentMusic() => currentMusic;
    public float GetSFXVolume() => sfxVolume;
    public float GetMusicVolume() => musicVolume;
    public bool IsSFXMuted() => muteSFX;
    public bool IsMusicMuted() => muteMusic;
    
    
    // Debug GUI (similar to MusicManager)
    private void OnGUI()
    {
        if (!debugMode) return;
        
        GUILayout.BeginArea(new Rect(Screen.width - 250, Screen.height - 300, 240, 290));
        GUILayout.BeginVertical("box");
        
        GUILayout.Label("SOUND MANAGER DEBUG");
        GUILayout.Label($"SFX Volume: {sfxVolume:F2} (Muted: {muteSFX})");
        GUILayout.Label($"Music Volume: {musicVolume:F2} (Muted: {muteMusic})");
        GUILayout.Label($"Current Music: {currentMusic?.ToString() ?? "None"}");
        GUILayout.Label($"Music Playing: {IsMusicPlaying()}");
        
        // GUILayout.Space(10);
        
        // GUILayout.Label("Quick SFX Test:");
        // GUILayout.BeginHorizontal();
        // if (GUILayout.Button("Click")) PlayClickSound();
        // if (GUILayout.Button("Success")) PlaySuccessSound();
        // GUILayout.EndHorizontal();
        
        // GUILayout.BeginHorizontal();
        // if (GUILayout.Button("Pickup")) PlayPickupSound();
        // if (GUILayout.Button("Drop")) PlayDropSound();
        GUILayout.EndHorizontal();
        
        GUILayout.Space(10);
        
        GUILayout.Label("Music Controls:");
        if (GUILayout.Button(IsMusicPlaying() ? "Pause Music" : "Resume Music"))
        {
            if (IsMusicPlaying()) PauseMusic(); else ResumeMusic();
        }
        
        if (GUILayout.Button("Stop Music"))
        {
            StopMusic();
        }
        
        GUILayout.Space(10);
        GUILayout.Label("Volume Controls:");
        float newSFXVol = GUILayout.HorizontalSlider(sfxVolume, 0f, 1f);
        if (newSFXVol != sfxVolume) SetSFXVolume(newSFXVol);
        
        float newMusicVol = GUILayout.HorizontalSlider(musicVolume, 0f, 1f);
        if (newMusicVol != musicVolume) SetMusicVolume(newMusicVol);
        
        GUILayout.EndVertical();
        GUILayout.EndArea();
    }
}