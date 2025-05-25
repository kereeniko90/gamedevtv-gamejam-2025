using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections.Generic;
using System.Collections;
using DG.Tweening;

[System.Serializable]
public class MusicTrack
{
    [Header("Track Info")]
    public string trackName;
    public AudioClip audioClip;
    
    [Header("Playback Settings")]
    [Range(0f, 1f)] public float volume = 1f;
    public bool loop = true;
    
    [Header("Crossfade Settings")]
    public float fadeInDuration = 1f;
    public float fadeOutDuration = 1f;
}

[System.Serializable]
public class SceneMusicMapping
{
    [Header("Scene Settings")]
    public string sceneName;
    public List<string> trackNames = new List<string>();
    
    [Header("Playback Options")]
    public bool shuffleTrackOrder = false;
    public bool crossfadeToNext = true;
    public float delayBeforeStart = 0f;
}

public class MusicManager : MonoBehaviour
{
    [Header("Music Library")]
    [SerializeField] private List<MusicTrack> musicTracks = new List<MusicTrack>();
    
    [Header("Scene Music Mapping")]
    [SerializeField] private List<SceneMusicMapping> sceneMusicMappings = new List<SceneMusicMapping>();
    
    [Header("Audio Sources")]
    [SerializeField] private AudioSource primaryAudioSource;
    [SerializeField] private AudioSource secondaryAudioSource;
    
    [Header("Global Settings")]
    [Range(0f, 1f)] [SerializeField] private float masterVolume = 1f;
    [SerializeField] private bool playOnStart = true;
    [SerializeField] private bool debugMode = true;
    
    [Header("Default Track")]
    [SerializeField] private string defaultTrackName = "";
    
    // Singleton
    public static MusicManager Instance { get; private set; }
    
    // Current state
    private MusicTrack currentTrack;
    private List<string> currentPlaylist = new List<string>();
    private int currentPlaylistIndex = 0;
    private bool isPlayingPlaylist = false;
    private bool isCrossfading = false;
    
    // Track lookup dictionary for performance
    private Dictionary<string, MusicTrack> trackLookup = new Dictionary<string, MusicTrack>();
    
    // Events
    public static System.Action<string> OnTrackStarted;
    public static System.Action<string> OnTrackEnded;
    public static System.Action<float> OnVolumeChanged;
    
    private void Awake()
    {
        // Singleton pattern with persistence
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            SetupMusicManager();
        }
        else
        {
            Destroy(gameObject);
            return;
        }
    }
    
    private void SetupMusicManager()
    {
        // Create audio sources if they don't exist
        if (primaryAudioSource == null)
        {
            primaryAudioSource = gameObject.AddComponent<AudioSource>();
            primaryAudioSource.playOnAwake = false;
        }
        
        if (secondaryAudioSource == null)
        {
            secondaryAudioSource = gameObject.AddComponent<AudioSource>();
            secondaryAudioSource.playOnAwake = false;
        }
        
        // Build track lookup dictionary
        BuildTrackLookup();
        
        // Subscribe to scene events
        SceneManager.sceneLoaded += OnSceneLoaded;
        
        // Ensure we have an AudioListener (remove duplicates if needed)
        EnsureAudioListener();
        
        if (debugMode)
            Debug.Log("MusicManager initialized and ready");
    }
    
    private void OnDestroy()
    {
        // Unsubscribe from scene events
        SceneManager.sceneLoaded -= OnSceneLoaded;
        
        // Kill any ongoing tweens
        primaryAudioSource?.DOKill();
        secondaryAudioSource?.DOKill();
    }
    
    private void Start()
    {
        if (playOnStart)
        {
            // Check if current scene has music mapping
            string currentSceneName = SceneManager.GetActiveScene().name;
            if (!TryPlaySceneMusic(currentSceneName))
            {
                // Fall back to default track
                PlayDefaultTrack();
            }
        }
    }
    
    private void Update()
    {
        // Handle playlist progression
        if (isPlayingPlaylist && !primaryAudioSource.isPlaying && !isCrossfading)
        {
            PlayNextInPlaylist();
        }
        
        // Debug controls
        if (debugMode)
        {
            HandleDebugInput();
        }
    }
    
    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (debugMode)
            Debug.Log($"MusicManager: Scene '{scene.name}' loaded");
            
        // Only change music for single scene loads (not additive)
        if (mode == LoadSceneMode.Single)
        {
            // Small delay to let the scene initialize
            StartCoroutine(DelayedSceneMusic(scene.name, 0.1f));
        }
    }
    
    private IEnumerator DelayedSceneMusic(string sceneName, float delay)
    {
        yield return new WaitForSeconds(delay);
        
        if (!TryPlaySceneMusic(sceneName))
        {
            if (debugMode)
                Debug.Log($"No music mapping found for scene '{sceneName}', continuing current track");
        }
    }
    
    private bool TryPlaySceneMusic(string sceneName)
    {
        SceneMusicMapping mapping = FindSceneMusicMapping(sceneName);
        if (mapping != null && mapping.trackNames.Count > 0)
        {
            if (mapping.delayBeforeStart > 0)
            {
                StartCoroutine(DelayedSceneMusic(sceneName, mapping.delayBeforeStart));
                return true;
            }
            
            if (mapping.trackNames.Count == 1)
            {
                // Single track
                PlayTrack(mapping.trackNames[0]);
            }
            else
            {
                // Multiple tracks - create playlist
                PlayPlaylist(mapping.trackNames, mapping.shuffleTrackOrder, mapping.crossfadeToNext);
            }
            
            return true;
        }
        
        return false;
    }
    
    private SceneMusicMapping FindSceneMusicMapping(string sceneName)
    {
        foreach (SceneMusicMapping mapping in sceneMusicMappings)
        {
            if (mapping.sceneName.Equals(sceneName, System.StringComparison.OrdinalIgnoreCase))
            {
                return mapping;
            }
        }
        return null;
    }
    
    private void BuildTrackLookup()
    {
        trackLookup.Clear();
        foreach (MusicTrack track in musicTracks)
        {
            if (!string.IsNullOrEmpty(track.trackName))
            {
                trackLookup[track.trackName] = track;
            }
        }
        
        if (debugMode)
            Debug.Log($"MusicManager: Built lookup table with {trackLookup.Count} tracks");
    }
    
    public void PlayTrack(string trackName)
    {
        if (string.IsNullOrEmpty(trackName))
        {
            if (debugMode)
                Debug.LogWarning("MusicManager: Cannot play track with empty name");
            return;
        }
        
        if (!trackLookup.TryGetValue(trackName, out MusicTrack track))
        {
            if (debugMode)
                Debug.LogWarning($"MusicManager: Track '{trackName}' not found");
            return;
        }
        
        if (track.audioClip == null)
        {
            if (debugMode)
                Debug.LogWarning($"MusicManager: Track '{trackName}' has no audio clip assigned");
            return;
        }
        
        // Stop playlist mode
        isPlayingPlaylist = false;
        currentPlaylist.Clear();
        
        // Play the track
        PlayTrackInternal(track);
    }
    
    public void PlayPlaylist(List<string> trackNames, bool shuffle = false, bool crossfade = true)
    {
        if (trackNames == null || trackNames.Count == 0)
        {
            if (debugMode)
                Debug.LogWarning("MusicManager: Cannot play empty playlist");
            return;
        }
        
        // Setup playlist
        currentPlaylist = new List<string>(trackNames);
        if (shuffle)
        {
            ShufflePlaylist();
        }
        
        currentPlaylistIndex = 0;
        isPlayingPlaylist = true;
        
        // Start playing first track
        if (currentPlaylist.Count > 0)
        {
            PlayTrack(currentPlaylist[0]);
        }
        
        if (debugMode)
            Debug.Log($"MusicManager: Started playlist with {currentPlaylist.Count} tracks (shuffle: {shuffle})");
    }
    
    private void PlayNextInPlaylist()
    {
        if (!isPlayingPlaylist || currentPlaylist.Count == 0) return;
        
        currentPlaylistIndex = (currentPlaylistIndex + 1) % currentPlaylist.Count;
        PlayTrack(currentPlaylist[currentPlaylistIndex]);
        
        if (debugMode)
            Debug.Log($"MusicManager: Playing next track in playlist ({currentPlaylistIndex + 1}/{currentPlaylist.Count})");
    }
    
    private void ShufflePlaylist()
    {
        for (int i = currentPlaylist.Count - 1; i > 0; i--)
        {
            int randomIndex = Random.Range(0, i + 1);
            string temp = currentPlaylist[i];
            currentPlaylist[i] = currentPlaylist[randomIndex];
            currentPlaylist[randomIndex] = temp;
        }
    }
    
    private void PlayTrackInternal(MusicTrack track)
    {
        if (isCrossfading) return;
        
        // Check if this track is already playing
        if (currentTrack == track && primaryAudioSource.isPlaying)
        {
            if (debugMode)
                Debug.Log($"MusicManager: Track '{track.trackName}' is already playing");
            return;
        }
        
        if (primaryAudioSource.isPlaying)
        {
            // Crossfade to new track
            StartCoroutine(CrossfadeToTrack(track));
        }
        else
        {
            // Start playing immediately
            StartTrack(track, primaryAudioSource);
        }
    }
    
    private IEnumerator CrossfadeToTrack(MusicTrack newTrack)
    {
        isCrossfading = true;
        
        float fadeOutDuration = currentTrack?.fadeOutDuration ?? 1f;
        float fadeInDuration = newTrack.fadeInDuration;
        
        // Swap audio sources
        AudioSource oldSource = primaryAudioSource;
        AudioSource newSource = secondaryAudioSource;
        
        // Start new track on secondary source
        StartTrack(newTrack, newSource);
        newSource.volume = 0f;
        
        // Crossfade
        Tween fadeOut = oldSource.DOFade(0f, fadeOutDuration);
        Tween fadeIn = newSource.DOFade(newTrack.volume * masterVolume, fadeInDuration);
        
        // Wait for crossfade to complete
        yield return new WaitForSeconds(Mathf.Max(fadeOutDuration, fadeInDuration));
        
        // Stop old source and swap references
        oldSource.Stop();
        primaryAudioSource = newSource;
        secondaryAudioSource = oldSource;
        
        isCrossfading = false;
        
        if (debugMode)
            Debug.Log($"MusicManager: Crossfaded to '{newTrack.trackName}'");
    }
    
    private void StartTrack(MusicTrack track, AudioSource source)
    {
        source.clip = track.audioClip;
        source.volume = track.volume * masterVolume;
        source.loop = track.loop;
        source.Play();
        
        currentTrack = track;
        OnTrackStarted?.Invoke(track.trackName);
        
        if (debugMode)
            Debug.Log($"MusicManager: Started playing '{track.trackName}'");
    }
    
    public void StopMusic(float fadeOutDuration = 1f)
    {
        if (primaryAudioSource.isPlaying)
        {
            primaryAudioSource.DOFade(0f, fadeOutDuration).OnComplete(() => {
                primaryAudioSource.Stop();
                if (currentTrack != null)
                {
                    OnTrackEnded?.Invoke(currentTrack.trackName);
                }
                currentTrack = null;
            });
        }
        
        isPlayingPlaylist = false;
        currentPlaylist.Clear();
    }
    
    public void PauseMusic()
    {
        if (primaryAudioSource.isPlaying)
        {
            primaryAudioSource.Pause();
            if (debugMode)
                Debug.Log("MusicManager: Music paused");
        }
    }
    
    public void ResumeMusic()
    {
        if (!primaryAudioSource.isPlaying && primaryAudioSource.clip != null)
        {
            primaryAudioSource.UnPause();
            if (debugMode)
                Debug.Log("MusicManager: Music resumed");
        }
    }
    
    public void SetMasterVolume(float volume)
    {
        masterVolume = Mathf.Clamp01(volume);
        
        if (currentTrack != null)
        {
            primaryAudioSource.volume = currentTrack.volume * masterVolume;
        }
        
        OnVolumeChanged?.Invoke(masterVolume);
        
        if (debugMode)
            Debug.Log($"MusicManager: Master volume set to {masterVolume:F2}");
    }
    
    private void PlayDefaultTrack()
    {
        if (!string.IsNullOrEmpty(defaultTrackName))
        {
            PlayTrack(defaultTrackName);
        }
    }
    
    private void EnsureAudioListener()
    {
        AudioListener[] listeners = FindObjectsByType<AudioListener>(FindObjectsSortMode.None);
        
        if (listeners.Length > 1)
        {
            if (debugMode)
                Debug.Log($"MusicManager: Found {listeners.Length} AudioListeners, keeping the first one");
                
            // Keep the first one (usually main camera), remove others
            for (int i = 1; i < listeners.Length; i++)
            {
                Destroy(listeners[i]);
            }
        }
        else if (listeners.Length == 0)
        {
            // Add one to the main camera or this object
            Camera mainCam = Camera.main;
            if (mainCam != null)
            {
                mainCam.gameObject.AddComponent<AudioListener>();
            }
            else
            {
                gameObject.AddComponent<AudioListener>();
            }
            
            if (debugMode)
                Debug.Log("MusicManager: Added missing AudioListener");
        }
    }
    
    private void HandleDebugInput()
    {
        if (Input.GetKeyDown(KeyCode.M))
        {
            if (primaryAudioSource.isPlaying)
                PauseMusic();
            else
                ResumeMusic();
        }
        
        if (Input.GetKeyDown(KeyCode.N))
        {
            if (isPlayingPlaylist)
                PlayNextInPlaylist();
        }
        
        if (Input.GetKeyDown(KeyCode.Comma))
        {
            SetMasterVolume(Mathf.Max(0f, masterVolume - 0.1f));
        }
        
        if (Input.GetKeyDown(KeyCode.Period))
        {
            SetMasterVolume(Mathf.Min(1f, masterVolume + 0.1f));
        }
    }
    
    // Public getters
    public bool IsPlaying => primaryAudioSource.isPlaying;
    public string CurrentTrackName => currentTrack?.trackName ?? "";
    public float MasterVolume => masterVolume;
    public bool IsPlayingPlaylist => isPlayingPlaylist;
    
    // Editor helper methods
    [ContextMenu("Rebuild Track Lookup")]
    private void RebuildTrackLookup()
    {
        BuildTrackLookup();
    }
    
    [ContextMenu("Test Play Default Track")]
    private void TestPlayDefault()
    {
        PlayDefaultTrack();
    }
    
    // Debug GUI
    private void OnGUI()
    {
        if (!debugMode) return;
        
        GUILayout.BeginArea(new Rect(Screen.width - 250, 10, 240, 300));
        GUILayout.BeginVertical("box");
        
        GUILayout.Label("MUSIC MANAGER DEBUG");
        GUILayout.Label($"Current: {CurrentTrackName}");
        GUILayout.Label($"Volume: {masterVolume:F2}");
        GUILayout.Label($"Playing: {IsPlaying}");
        GUILayout.Label($"Playlist Mode: {IsPlayingPlaylist}");
        
        if (IsPlayingPlaylist)
        {
            GUILayout.Label($"Playlist: {currentPlaylistIndex + 1}/{currentPlaylist.Count}");
        }
        
        GUILayout.Space(10);
        
        if (GUILayout.Button(IsPlaying ? "Pause (M)" : "Resume (M)"))
        {
            if (IsPlaying) PauseMusic(); else ResumeMusic();
        }
        
        if (IsPlayingPlaylist && GUILayout.Button("Next Track (N)"))
        {
            PlayNextInPlaylist();
        }
        
        if (GUILayout.Button("Stop Music"))
        {
            StopMusic();
        }
        
        GUILayout.Label("Volume Controls:");
        GUILayout.BeginHorizontal();
        if (GUILayout.Button("- (,)")) SetMasterVolume(masterVolume - 0.1f);
        if (GUILayout.Button("+ (.)")) SetMasterVolume(masterVolume + 0.1f);
        GUILayout.EndHorizontal();
        
        GUILayout.EndVertical();
        GUILayout.EndArea();
    }
}