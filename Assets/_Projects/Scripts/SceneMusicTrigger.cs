using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Place this component in any scene to automatically trigger music when that scene loads.
/// This provides an alternative to setting up music in the MusicManager's scene mappings.
/// </summary>
public class SceneMusicTrigger : MonoBehaviour
{
    [Header("Music Settings")]
    [SerializeField] private List<string> trackNames = new List<string>();
    
    [Header("Playback Options")]
    [SerializeField] private bool shuffleTrackOrder = false;
    [SerializeField] private bool crossfadeToNext = true;
    [SerializeField] private float delayBeforeStart = 0f;
    [SerializeField] private bool stopCurrentMusicFirst = false;
    
    [Header("Conditions")]
    [SerializeField] private bool onlyTriggerOnce = true;
    [SerializeField] private bool debugMode = true;
    
    private bool hasTriggered = false;
    
    private void Start()
    {
        // Small delay to ensure MusicManager is ready
        if (delayBeforeStart > 0)
        {
            Invoke(nameof(TriggerMusic), delayBeforeStart);
        }
        else
        {
            Invoke(nameof(TriggerMusic), 0.1f); // Small delay to ensure proper initialization
        }
    }
    
    private void TriggerMusic()
    {
        // Check if we should only trigger once
        if (onlyTriggerOnce && hasTriggered)
        {
            if (debugMode)
                Debug.Log($"SceneMusicTrigger: Already triggered for this scene, skipping");
            return;
        }
        
        // Check if MusicManager exists
        if (MusicManager.Instance == null)
        {
            if (debugMode)
                Debug.LogWarning("SceneMusicTrigger: MusicManager not found!");
            return;
        }
        
        // Check if we have tracks to play
        if (trackNames.Count == 0)
        {
            if (debugMode)
                Debug.LogWarning("SceneMusicTrigger: No track names specified!");
            return;
        }
        
        hasTriggered = true;
        
        // Stop current music if requested
        if (stopCurrentMusicFirst && MusicManager.Instance.IsPlaying)
        {
            MusicManager.Instance.StopMusic(1f);
            // Wait a moment before starting new music
            Invoke(nameof(StartNewMusic), 1.1f);
        }
        else
        {
            StartNewMusic();
        }
    }
    
    private void StartNewMusic()
    {
        if (trackNames.Count == 1)
        {
            // Single track
            MusicManager.Instance.PlayTrack(trackNames[0]);
            
            if (debugMode)
                Debug.Log($"SceneMusicTrigger: Playing single track '{trackNames[0]}'");
        }
        else
        {
            // Multiple tracks - create playlist
            MusicManager.Instance.PlayPlaylist(trackNames, shuffleTrackOrder, crossfadeToNext);
            
            if (debugMode)
                Debug.Log($"SceneMusicTrigger: Playing playlist with {trackNames.Count} tracks (shuffle: {shuffleTrackOrder})");
        }
    }
    
    // Public method to manually trigger (useful for testing or special cases)
    public void ManualTrigger()
    {
        hasTriggered = false; // Reset the trigger state
        TriggerMusic();
    }
    
    // Public method to add a track to this trigger
    public void AddTrack(string trackName)
    {
        if (!trackNames.Contains(trackName))
        {
            trackNames.Add(trackName);
        }
    }
    
    // Public method to remove a track from this trigger
    public void RemoveTrack(string trackName)
    {
        trackNames.Remove(trackName);
    }
    
    // Public method to clear all tracks
    public void ClearTracks()
    {
        trackNames.Clear();
    }
    
    // For debugging in inspector
    [ContextMenu("Test Trigger Music")]
    private void TestTriggerMusic()
    {
        ManualTrigger();
    }
}