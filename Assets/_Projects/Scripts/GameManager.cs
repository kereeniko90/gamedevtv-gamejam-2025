using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class GameManager : MonoBehaviour
{
    // Singleton instance
    public static GameManager Instance { get; private set; }
    
    [Header("References")]
    [SerializeField] private TimeController timeController;
    [SerializeField] private ScoreManager scoreManager;
    
    [Header("UI Elements")]
    [SerializeField] private GameObject mainGameUI;
    [SerializeField] private GameObject dayEndScreen;
    [SerializeField] private GameObject gameOverScreen;
    [SerializeField] private Text dayText;
    [SerializeField] private Text scoreText;
    
    [Header("Package Delivery System")]
    [SerializeField] private GameObject packagePrefab;
    [SerializeField] private Transform packageSpawnPoint;
    [SerializeField] private List<DecorationItem> availableDecorations = new List<DecorationItem>();
    [SerializeField] private int decorationsPerDay = 2;
    
    [Header("Game State")]
    private GameState currentState = GameState.MainMenu;
    
    // List of decorations delivered today
    private List<DecorationItem> todaysDecorations = new List<DecorationItem>();
    
    // Game states enum
    private enum GameState
    {
        MainMenu,
        Playing,
        DayEnd,
        GameOver
    }
    
    private void Awake()
    {
        // Singleton pattern
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }

        timeController = TimeController.Instance;
        scoreManager = ScoreManager.Instance;
        
        // Find references if not set
        if (timeController == null)
            timeController = FindFirstObjectByType<TimeController>();
            
        if (scoreManager == null)
            scoreManager = FindFirstObjectByType<ScoreManager>();
    }
    
    private void Start()
    {
        // Subscribe to events
        if (timeController != null)
        {
            timeController.onDayStart += OnDayStart;
            timeController.onDayEnd += OnDayEnd;
            timeController.onGameOver += OnGameOver;
        }
        
        // Start in main menu (or go directly to playing for testing)
        // For now, just start playing
        SetGameState(GameState.Playing);
    }
    
    private void OnDayStart()
    {
        // Update UI
        if (dayText != null && timeController != null)
        {
            dayText.text = $"Day {timeController.GetCurrentDay()} of {timeController.GetTotalDays()}";
        }
        
        // Deliver today's package
        DeliverDailyPackage();
        
        // Create daily chores
        SpawnDailyChores();
        
        // Show main game UI
        SetGameState(GameState.Playing);
    }
    
    private void OnDayEnd()
    {
        // Show day end screen
        SetGameState(GameState.DayEnd);
        
        // Update UI
        if (scoreText != null && scoreManager != null)
        {
            // Display day's score (this would be shown on the day end screen)
            // The actual value would come from ScoreManager
            scoreText.text = $"Today's Score: {Random.Range(50, 150)}"; // Placeholder
        }
    }
    
    private void OnGameOver()
    {
        // Show game over screen
        SetGameState(GameState.GameOver);
        
        // Update UI
        if (scoreText != null && scoreManager != null)
        {
            // Display total score (this would be shown on the game over screen)
            // The actual value would come from ScoreManager
            scoreText.text = $"Final Score: {Random.Range(200, 600)}"; // Placeholder
        }
    }
    
    private void SetGameState(GameState newState)
    {
        currentState = newState;
        
        // Update UI based on state
        if (mainGameUI != null) mainGameUI.SetActive(newState == GameState.Playing);
        if (dayEndScreen != null) dayEndScreen.SetActive(newState == GameState.DayEnd);
        if (gameOverScreen != null) gameOverScreen.SetActive(newState == GameState.GameOver);
        
        Debug.Log($"Game State changed to: {newState}");
    }
    
    // Deliver the daily decoration package
    private void DeliverDailyPackage()
    {
        // Clear previous day's decorations
        todaysDecorations.Clear();
        
        // Check if we have available decorations
        if (availableDecorations.Count == 0)
        {
            Debug.LogWarning("No available decorations to deliver!");
            return;
        }
        
        // Determine how many decorations to deliver today (based on day number, etc.)
        int numDecorations = Mathf.Min(decorationsPerDay, availableDecorations.Count);
        
        // Spawn package
        if (packagePrefab != null && packageSpawnPoint != null)
        {
            GameObject package = Instantiate(packagePrefab, packageSpawnPoint.position, Quaternion.identity);
            
            // In a real implementation, this package would be interactive
            // When opened, it would spawn the decorations
            
            Debug.Log($"Package delivered with {numDecorations} new decorations!");
        }
        
        // For now, just spawn the decorations directly
        for (int i = 0; i < numDecorations; i++)
        {
            if (i < availableDecorations.Count)
            {
                // Get random decoration from available pool
                int randomIndex = Random.Range(0, availableDecorations.Count);
                DecorationItem decoration = availableDecorations[randomIndex];
                
                // Remove from available pool
                availableDecorations.RemoveAt(randomIndex);
                
                // Add to today's decorations
                todaysDecorations.Add(decoration);
                
                // In a real implementation, these would be spawned when the package is opened
                // For now, just log them
                Debug.Log($"New decoration available: {decoration.itemName}");
            }
        }
    }
    
    // Create the daily chores
    private void SpawnDailyChores()
    {
        // In a real implementation, this would spawn/activate the chore items for today
        // This could vary by day, difficulty, etc.
        
        // For now, just log what we'd do
        Debug.Log("Daily chores spawned!");
    }
    
    // UI Button handlers
    
    public void ContinueToNextDay()
    {
        // Called from the day end screen's continue button
        timeController.StartDay();
    }
    
    public void RestartGame()
    {
        // Called from the game over screen's restart button
        // In a real implementation, this would reload the scene or reset the game state
        // For now, just reset to day 1
        UnityEngine.SceneManagement.SceneManager.LoadScene(
            UnityEngine.SceneManagement.SceneManager.GetActiveScene().name
        );
    }
}