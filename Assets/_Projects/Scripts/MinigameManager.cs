using UnityEngine;

public class MiniGameManager : MonoBehaviour
{
    public static MiniGameManager Instance { get; private set; }
    
    [SerializeField] private Transform miniGameCanvas;
    [SerializeField] private GameObject[] miniGamePrefabs;
    
    private InteractableAppliance currentAppliance;
    
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
        }
    }
    
    public void LaunchMiniGame(string prefabName, InteractableAppliance appliance)
    {
        // Store reference to the appliance being repaired
        currentAppliance = appliance;
        
        // Find the mini-game prefab
        GameObject prefab = System.Array.Find(miniGamePrefabs, p => p.name == prefabName);
        if (prefab != null)
        {
            // Instantiate mini-game on canvas
            GameObject miniGame = Instantiate(prefab, miniGameCanvas);
            
            // Pause main game time if needed
            TimeController.Instance?.PauseTime();
        }
    }
    
    public void CompleteMiniGame(bool success)
    {
        if (success && currentAppliance != null)
        {
            // Repair the appliance
            currentAppliance.Repair();
            
            // Award points
            ScoreManager.Instance?.AddPoints(25, currentAppliance.transform.position);
        }
        
        // Resume main game time
        TimeController.Instance?.ResumeTime();
        
        // Clear reference
        currentAppliance = null;
    }
}