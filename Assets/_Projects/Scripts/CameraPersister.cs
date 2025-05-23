using UnityEngine;

public class CameraPersister : MonoBehaviour
{
    // Singleton instance
    public static CameraPersister Instance { get; private set; }

    private void Awake()
    {
        // If an instance already exists and it's not this one
        if (Instance != null && Instance != this)
        {
            // Destroy this instance
            Destroy(gameObject);
            return;
        }

        // Set the instance and make it persist
        Instance = this;
        DontDestroyOnLoad(gameObject);

        // Ensure this camera has the MainCamera tag
        gameObject.tag = "MainCamera";
    }

    // Check and remove any duplicate cameras that might exist in new scenes
    private void OnEnable()
    {
        // Subscribe to the scene loaded event
        UnityEngine.SceneManagement.SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDisable()
    {
        // Unsubscribe from the scene loaded event
        UnityEngine.SceneManagement.SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnSceneLoaded(UnityEngine.SceneManagement.Scene scene, UnityEngine.SceneManagement.LoadSceneMode mode)
    {
        // Find all cameras in the scene
        Camera[] cameras = FindObjectsByType<Camera>(FindObjectsSortMode.None);

        foreach (Camera camera in cameras)
        {
            // Skip if it's this camera
            if (camera.gameObject == gameObject)
                continue;

            // If the camera has the MainCamera tag, remove it or disable it
            if (camera.CompareTag("MainCamera"))
            {
                Debug.Log($"Found duplicate MainCamera in scene {scene.name}. Disabling it.");
                camera.gameObject.SetActive(false);
            }
        }
    }
}