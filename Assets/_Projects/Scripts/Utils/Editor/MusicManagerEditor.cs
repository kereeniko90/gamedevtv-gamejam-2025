#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

[CustomEditor(typeof(MusicManager))]
public class MusicManagerEditor : Editor
{
    private MusicManager musicManager;
    private Vector2 scrollPosition;
    private bool showTracks = true;
    private bool showSceneMappings = true;
    private bool showControls = true;
    private bool showTrackTester = true;
    
    // Track tester
    private string trackToTest = "";
    
    private void OnEnable()
    {
        musicManager = (MusicManager)target;
    }
    
    public override void OnInspectorGUI()
    {
        // Draw default inspector
        DrawDefaultInspector();
        
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Music Manager Controls", EditorStyles.boldLabel);
        
        // Quick controls section
        showControls = EditorGUILayout.Foldout(showControls, "Runtime Controls", true);
        if (showControls)
        {
            EditorGUILayout.BeginVertical("box");
            
            if (Application.isPlaying)
            {
                // Runtime controls
                EditorGUILayout.LabelField($"Current Track: {musicManager.CurrentTrackName}");
                EditorGUILayout.LabelField($"Is Playing: {musicManager.IsPlaying}");
                EditorGUILayout.LabelField($"Master Volume: {musicManager.MasterVolume:F2}");
                EditorGUILayout.LabelField($"Playlist Mode: {musicManager.IsPlayingPlaylist}");
                
                EditorGUILayout.Space();
                
                EditorGUILayout.BeginHorizontal();
                if (GUILayout.Button(musicManager.IsPlaying ? "Pause" : "Resume"))
                {
                    if (musicManager.IsPlaying)
                        musicManager.PauseMusic();
                    else
                        musicManager.ResumeMusic();
                }
                
                if (GUILayout.Button("Stop"))
                {
                    musicManager.StopMusic();
                }
                EditorGUILayout.EndHorizontal();
                
                // Volume control
                float newVolume = EditorGUILayout.Slider("Master Volume", musicManager.MasterVolume, 0f, 1f);
                if (newVolume != musicManager.MasterVolume)
                {
                    musicManager.SetMasterVolume(newVolume);
                }
            }
            else
            {
                EditorGUILayout.HelpBox("Runtime controls available when playing", MessageType.Info);
            }
            
            EditorGUILayout.EndVertical();
        }
        
        EditorGUILayout.Space();
        
        // Track tester section
        showTrackTester = EditorGUILayout.Foldout(showTrackTester, "Track Tester", true);
        if (showTrackTester)
        {
            EditorGUILayout.BeginVertical("box");
            
            if (Application.isPlaying)
            {
                trackToTest = EditorGUILayout.TextField("Track Name", trackToTest);
                
                EditorGUILayout.BeginHorizontal();
                if (GUILayout.Button("Test Play Track"))
                {
                    if (!string.IsNullOrEmpty(trackToTest))
                    {
                        musicManager.PlayTrack(trackToTest);
                    }
                }
                
                if (GUILayout.Button("Stop"))
                {
                    musicManager.StopMusic();
                }
                EditorGUILayout.EndHorizontal();
            }
            else
            {
                EditorGUILayout.HelpBox("Track testing available when playing", MessageType.Info);
            }
            
            EditorGUILayout.EndVertical();
        }
        
        EditorGUILayout.Space();
        
        // Track validation section
        showTracks = EditorGUILayout.Foldout(showTracks, "Track Validation", true);
        if (showTracks)
        {
            EditorGUILayout.BeginVertical("box");
            
            // Check for issues
            List<string> issues = ValidateTracks();
            
            if (issues.Count > 0)
            {
                EditorGUILayout.HelpBox("Issues found:", MessageType.Warning);
                foreach (string issue in issues)
                {
                    EditorGUILayout.LabelField("• " + issue, EditorStyles.miniLabel);
                }
            }
            else
            {
                EditorGUILayout.HelpBox("All tracks configured correctly!", MessageType.Info);
            }
            
            if (GUILayout.Button("Rebuild Track Lookup"))
            {
                // This would call the private method - for editor we'll just mark dirty
                EditorUtility.SetDirty(musicManager);
            }
            
            EditorGUILayout.EndVertical();
        }
        
        EditorGUILayout.Space();
        
        // Scene mapping validation
        showSceneMappings = EditorGUILayout.Foldout(showSceneMappings, "Scene Mapping Validation", true);
        if (showSceneMappings)
        {
            EditorGUILayout.BeginVertical("box");
            
            List<string> mappingIssues = ValidateSceneMappings();
            
            if (mappingIssues.Count > 0)
            {
                EditorGUILayout.HelpBox("Scene mapping issues:", MessageType.Warning);
                foreach (string issue in mappingIssues)
                {
                    EditorGUILayout.LabelField("• " + issue, EditorStyles.miniLabel);
                }
            }
            else
            {
                EditorGUILayout.HelpBox("Scene mappings look good!", MessageType.Info);
            }
            
            EditorGUILayout.Space();
            
            if (GUILayout.Button("Add Current Scene Mapping"))
            {
                AddCurrentSceneMapping();
            }
            
            EditorGUILayout.EndVertical();
        }
        
        // Apply changes
        if (GUI.changed)
        {
            EditorUtility.SetDirty(musicManager);
        }
    }
    
    private List<string> ValidateTracks()
    {
        List<string> issues = new List<string>();
        SerializedProperty tracksProperty = serializedObject.FindProperty("musicTracks");
        
        HashSet<string> trackNames = new HashSet<string>();
        
        for (int i = 0; i < tracksProperty.arraySize; i++)
        {
            SerializedProperty trackProperty = tracksProperty.GetArrayElementAtIndex(i);
            SerializedProperty nameProperty = trackProperty.FindPropertyRelative("trackName");
            SerializedProperty clipProperty = trackProperty.FindPropertyRelative("audioClip");
            
            string trackName = nameProperty.stringValue;
            
            // Check for empty names
            if (string.IsNullOrEmpty(trackName))
            {
                issues.Add($"Track {i} has no name");
            }
            // Check for duplicate names
            else if (trackNames.Contains(trackName))
            {
                issues.Add($"Duplicate track name: '{trackName}'");
            }
            else
            {
                trackNames.Add(trackName);
            }
            
            // Check for missing audio clips
            if (clipProperty.objectReferenceValue == null)
            {
                issues.Add($"Track '{trackName}' has no audio clip");
            }
        }
        
        return issues;
    }
    
    private List<string> ValidateSceneMappings()
    {
        List<string> issues = new List<string>();
        SerializedProperty mappingsProperty = serializedObject.FindProperty("sceneMusicMappings");
        SerializedProperty tracksProperty = serializedObject.FindProperty("musicTracks");
        
        // Build list of available track names
        HashSet<string> availableTrackNames = new HashSet<string>();
        for (int i = 0; i < tracksProperty.arraySize; i++)
        {
            SerializedProperty trackProperty = tracksProperty.GetArrayElementAtIndex(i);
            SerializedProperty nameProperty = trackProperty.FindPropertyRelative("trackName");
            string trackName = nameProperty.stringValue;
            
            if (!string.IsNullOrEmpty(trackName))
            {
                availableTrackNames.Add(trackName);
            }
        }
        
        // Check scene mappings
        HashSet<string> sceneNames = new HashSet<string>();
        
        for (int i = 0; i < mappingsProperty.arraySize; i++)
        {
            SerializedProperty mappingProperty = mappingsProperty.GetArrayElementAtIndex(i);
            SerializedProperty sceneNameProperty = mappingProperty.FindPropertyRelative("sceneName");
            SerializedProperty trackNamesProperty = mappingProperty.FindPropertyRelative("trackNames");
            
            string sceneName = sceneNameProperty.stringValue;
            
            // Check for empty scene names
            if (string.IsNullOrEmpty(sceneName))
            {
                issues.Add($"Scene mapping {i} has no scene name");
            }
            // Check for duplicate scene names
            else if (sceneNames.Contains(sceneName))
            {
                issues.Add($"Duplicate scene mapping: '{sceneName}'");
            }
            else
            {
                sceneNames.Add(sceneName);
            }
            
            // Check track references
            for (int j = 0; j < trackNamesProperty.arraySize; j++)
            {
                SerializedProperty trackNameProperty = trackNamesProperty.GetArrayElementAtIndex(j);
                string trackName = trackNameProperty.stringValue;
                
                if (string.IsNullOrEmpty(trackName))
                {
                    issues.Add($"Scene '{sceneName}' has empty track name at index {j}");
                }
                else if (!availableTrackNames.Contains(trackName))
                {
                    issues.Add($"Scene '{sceneName}' references unknown track: '{trackName}'");
                }
            }
            
            // Check for empty track lists
            if (trackNamesProperty.arraySize == 0)
            {
                issues.Add($"Scene '{sceneName}' has no tracks assigned");
            }
        }
        
        return issues;
    }
    
    private void AddCurrentSceneMapping()
    {
        string currentSceneName = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
        
        SerializedProperty mappingsProperty = serializedObject.FindProperty("sceneMusicMappings");
        
        // Check if mapping already exists
        for (int i = 0; i < mappingsProperty.arraySize; i++)
        {
            SerializedProperty mappingProperty = mappingsProperty.GetArrayElementAtIndex(i);
            SerializedProperty sceneNameProperty = mappingProperty.FindPropertyRelative("sceneName");
            
            if (sceneNameProperty.stringValue == currentSceneName)
            {
                EditorUtility.DisplayDialog("Scene Mapping Exists", 
                    $"A mapping for scene '{currentSceneName}' already exists.", "OK");
                return;
            }
        }
        
        // Add new mapping
        mappingsProperty.arraySize++;
        SerializedProperty newMapping = mappingsProperty.GetArrayElementAtIndex(mappingsProperty.arraySize - 1);
        
        SerializedProperty newSceneNameProperty = newMapping.FindPropertyRelative("sceneName");
        SerializedProperty newTrackNamesProperty = newMapping.FindPropertyRelative("trackNames");
        SerializedProperty shuffleProperty = newMapping.FindPropertyRelative("shuffleTrackOrder");
        SerializedProperty crossfadeProperty = newMapping.FindPropertyRelative("crossfadeToNext");
        SerializedProperty delayProperty = newMapping.FindPropertyRelative("delayBeforeStart");
        
        newSceneNameProperty.stringValue = currentSceneName;
        newTrackNamesProperty.arraySize = 0;
        shuffleProperty.boolValue = false;
        crossfadeProperty.boolValue = true;
        delayProperty.floatValue = 0f;
        
        serializedObject.ApplyModifiedProperties();
        EditorUtility.SetDirty(musicManager);
        
        Debug.Log($"Added scene mapping for '{currentSceneName}'");
    }
}

[CustomEditor(typeof(SceneMusicTrigger))]
public class SceneMusicTriggerEditor : Editor
{
    private SceneMusicTrigger trigger;
    
    private void OnEnable()
    {
        trigger = (SceneMusicTrigger)target;
    }
    
    public override void OnInspectorGUI()
    {
        // Draw default inspector
        DrawDefaultInspector();
        
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Scene Music Trigger Controls", EditorStyles.boldLabel);
        
        EditorGUILayout.BeginVertical("box");
        
        if (Application.isPlaying)
        {
            if (GUILayout.Button("Manual Trigger"))
            {
                trigger.ManualTrigger();
            }
            
            EditorGUILayout.Space();
            
            if (GUILayout.Button("Clear All Tracks"))
            {
                if (EditorUtility.DisplayDialog("Clear Tracks", 
                    "Are you sure you want to clear all tracks?", "Yes", "No"))
                {
                    trigger.ClearTracks();
                }
            }
        }
        else
        {
            EditorGUILayout.HelpBox("Controls available when playing", MessageType.Info);
        }
        
        EditorGUILayout.EndVertical();
        
        // Validation
        EditorGUILayout.Space();
        ValidateTrigger();
        
        if (GUI.changed)
        {
            EditorUtility.SetDirty(trigger);
        }
    }
    
    private void ValidateTrigger()
    {
        SerializedProperty trackNamesProperty = serializedObject.FindProperty("trackNames");
        
        if (trackNamesProperty.arraySize == 0)
        {
            EditorGUILayout.HelpBox("No tracks assigned to this trigger!", MessageType.Warning);
        }
        else
        {
            List<string> invalidTracks = new List<string>();
            
            for (int i = 0; i < trackNamesProperty.arraySize; i++)
            {
                SerializedProperty trackNameProperty = trackNamesProperty.GetArrayElementAtIndex(i);
                string trackName = trackNameProperty.stringValue;
                
                if (string.IsNullOrEmpty(trackName))
                {
                    invalidTracks.Add($"Empty track name at index {i}");
                }
            }
            
            if (invalidTracks.Count > 0)
            {
                EditorGUILayout.HelpBox("Track validation issues:", MessageType.Warning);
                foreach (string issue in invalidTracks)
                {
                    EditorGUILayout.LabelField("• " + issue, EditorStyles.miniLabel);
                }
            }
            else
            {
                EditorGUILayout.HelpBox($"Trigger configured with {trackNamesProperty.arraySize} track(s)", MessageType.Info);
            }
        }
    }
}
#endif