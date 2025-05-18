using System.Collections.Generic;
using UnityEngine;

public class ScoreManager : MonoBehaviour
{
    // Singleton instance
    public static ScoreManager Instance { get; private set; }

    // Point values from your table
    [Header("Point Values")]
    [SerializeField] private int generalPointsPerTask = 10;
    [SerializeField] private int completionBonus = 20;
    [SerializeField] private int dailyBonus = 5;
    [SerializeField] private int specialPoints = 20;

    // References
    [SerializeField] private TimeController timeController;
    [SerializeField] private ScoreUIManager uiManager;
    [SerializeField] private DecorationScoreCalculator decorationCalculator;
    
    // Score tracking
    private int dailyScore = 0;
    private int totalScore = 0;
    
    // Score breakdown for day end summary
    private int chorePointsToday = 0;
    private int decorationPointsToday = 0;
    private int themeBonusToday = 0;
    private int dailyBonusToday = 0;
    
    // Dictionary to track decoration points and their positions
    private Dictionary<int, DecorationScore> decorationScores = new Dictionary<int, DecorationScore>();
    
    // Track completed chores
    private List<ChoreItem> completedChores = new List<ChoreItem>();
    
    // Event for when points are added
    public System.Action<int, Vector3> onPointsAdded;
    
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
    }

    private void Start()
    {
        // Find references if not set
        if (timeController == null)
            timeController = TimeController.Instance;

        if (uiManager == null)
            uiManager = ScoreUIManager.Instance;
            
        if (decorationCalculator == null)
            decorationCalculator = GetComponent<DecorationScoreCalculator>();
            
        // Subscribe to day end event
        if (timeController != null)
        {
            timeController.onDayEnd += CalculateEndOfDayScore;
            timeController.onDayStart += ResetDailyScores;
        }
        
        // Subscribe to points added event
        onPointsAdded += ShowPointsPopup;
    }

    // Called when a chore is completed
    public void RegisterCompletedChore(ChoreItem chore)
    {
        if (!completedChores.Contains(chore))
        {
            completedChores.Add(chore);
            
            // Add points for completing the chore
            AddPoints(generalPointsPerTask, chore.transform.position);
            
            // Update chore points for today
            chorePointsToday += generalPointsPerTask;
            
            Debug.Log($"Chore completed: {chore.itemName}. +{generalPointsPerTask} points!");
        }
    }

    // Register a decoration placement
    public void RegisterDecorationPlacement(DecorationItem decoration, InteractionZone zone)
    {
        int itemID = decoration.GetInstanceID();
        
        // Calculate score based on placement
        int scoreValue = CalculateDecorationScore(decoration, zone);
        
        // Store or update the decoration's score
        if (decorationScores.ContainsKey(itemID))
        {
            // Get previous score
            int previousScore = decorationScores[itemID].score;
            
            // Update existing score record
            decorationScores[itemID] = new DecorationScore(decoration, zone, scoreValue);
            
            // Add the difference to daily score
            int difference = scoreValue - previousScore;
            if (difference != 0)
            {
                AddPoints(difference, decoration.transform.position);
                decorationPointsToday += difference;
            }
            
            Debug.Log($"Updated decoration: {decoration.itemName} at {zone.ZoneName}. Now worth {scoreValue} points!");
        }
        else
        {
            // Create new score record
            decorationScores.Add(itemID, new DecorationScore(decoration, zone, scoreValue));
            
            // Add to daily score
            AddPoints(scoreValue, decoration.transform.position);
            decorationPointsToday += scoreValue;
            
            Debug.Log($"New decoration: {decoration.itemName} at {zone.ZoneName}. Worth {scoreValue} points!");
        }
    }

    // Calculate a decoration's score based on placement
    private int CalculateDecorationScore(DecorationItem decoration, InteractionZone zone)
    {
        // If we have a decoration calculator, use it for more advanced scoring
        if (decorationCalculator != null)
        {
            return decorationCalculator.CalculateScore(decoration, zone);
        }
        
        // Basic calculation
        int score = decoration.pointValue; // Base value
        
        // Check if this zone is a preferred zone for the decoration
        if (decoration.preferredZones != null)
        {
            foreach (string preferredZone in decoration.preferredZones)
            {
                if (preferredZone == zone.ZoneName)
                {
                    // Bonus for preferred placement
                    score += specialPoints;
                    break;
                }
            }
        }
        
        return score;
    }

    // Add points to the daily score
    public void AddPoints(int points, Vector3 position)
    {
        dailyScore += points;
        
        // Trigger event for UI
        onPointsAdded?.Invoke(points, position);
        
        // Update UI
        UpdateScoreDisplay();
    }
    
    // Add points without position (used for bonuses)
    public void AddPoints(int points)
    {
        dailyScore += points;
        UpdateScoreDisplay();
    }
    
    // Show points popup
    private void ShowPointsPopup(int points, Vector3 position)
    {
        if (uiManager != null)
        {
            // Set color based on points (green for positive, red for negative)
            Color color = points >= 0 ? Color.green : Color.red;
            
            uiManager.CreateScorePopup(position, points, color);
        }
    }
    
    // Reset daily score tracking
    public void ResetDailyScores()
    {
        chorePointsToday = 0;
        decorationPointsToday = 0;
        themeBonusToday = 0;
        dailyBonusToday = 0;
    }

    // Calculate the total score at the end of the day
    private void CalculateEndOfDayScore()
    {
        // Recalculate all decoration placement scores
        // This ensures we have the final positions
        RecalculateAllDecorationScores();
        
        // Reset daily score to start fresh
        dailyScore = 0;
        
        // Add chore points
        dailyScore += chorePointsToday;
        
        // Add decoration points
        dailyScore += decorationPointsToday;
        
        // Calculate theme bonuses using the decoration calculator
        if (decorationCalculator != null)
        {
            themeBonusToday = decorationCalculator.CalculateThemeBonuses();
            dailyScore += themeBonusToday;
        }
        
        // Add bonus for completing all chores
        if (AllChoresCompleted())
        {
            dailyScore += completionBonus;
            Debug.Log($"All chores completed! Bonus: +{completionBonus} points!");
        }
        
        // Add daily bonus
        dailyBonusToday = dailyBonus;
        dailyScore += dailyBonusToday;
        Debug.Log($"Daily bonus: +{dailyBonus} points!");
        
        // Add to total score
        totalScore += dailyScore;
        
        // Display end of day summary
        DisplayEndOfDayResults();
    }
    
    // Check if all assigned chores are completed
    private bool AllChoresCompleted()
    {
        // Find all chores in the scene
        ChoreItem[] allChores = FindObjectsByType<ChoreItem>(FindObjectsSortMode.None);
        
        // Check if we've completed all of them
        return completedChores.Count == allChores.Length;
    }
    
    // Recalculate all decoration scores based on final positions
    private void RecalculateAllDecorationScores()
    {
        // Find all decorations in the scene
        DecorationItem[] allDecorations = FindObjectsByType<DecorationItem>(FindObjectsSortMode.None);
        
        foreach (DecorationItem decoration in allDecorations)
        {
            // Find the zone this decoration is currently in
            InteractionZone currentZone = FindZoneForDecoration(decoration);
            
            if (currentZone != null)
            {
                // Recalculate the score
                int newScore = CalculateDecorationScore(decoration, currentZone);
                
                // Update in dictionary
                int itemID = decoration.GetInstanceID();
                if (decorationScores.ContainsKey(itemID))
                {
                    decorationScores[itemID] = new DecorationScore(decoration, currentZone, newScore);
                }
                else
                {
                    decorationScores.Add(itemID, new DecorationScore(decoration, currentZone, newScore));
                }
                
                Debug.Log($"Final position for {decoration.itemName}: {newScore} points");
            }
        }
    }
    
    // Find which zone a decoration is currently in
    private InteractionZone FindZoneForDecoration(DecorationItem decoration)
    {
        // Get the collider of the decoration
        Collider2D decorationCollider = decoration.GetComponent<Collider2D>();
        if (decorationCollider == null) return null;
        
        // Find all interaction zones
        InteractionZone[] allZones = FindObjectsByType<InteractionZone>(FindObjectsSortMode.None);
        
        foreach (InteractionZone zone in allZones)
        {
            // Get zone collider
            Collider2D zoneCollider = zone.GetComponent<Collider2D>();
            if (zoneCollider == null) continue;
            
            // Check if decoration is inside this zone
            if (IsFullyContained(decorationCollider.bounds, zoneCollider.bounds))
            {
                return zone;
            }
        }
        
        return null;
    }
    
    // Check if the first bounds is fully contained within the second bounds
    private bool IsFullyContained(Bounds objectBounds, Bounds containerBounds)
    {
        // Check if all corners of the object bounds are inside the container bounds
        return containerBounds.Contains(new Vector3(objectBounds.min.x, objectBounds.min.y, objectBounds.min.z)) &&
               containerBounds.Contains(new Vector3(objectBounds.max.x, objectBounds.min.y, objectBounds.min.z)) &&
               containerBounds.Contains(new Vector3(objectBounds.min.x, objectBounds.max.y, objectBounds.min.z)) &&
               containerBounds.Contains(new Vector3(objectBounds.max.x, objectBounds.max.y, objectBounds.min.z));
    }
    
    // Display the end of day score results
    private void DisplayEndOfDayResults()
    {
        // For now, just display in console
        Debug.Log($"=== END OF DAY RESULTS ===");
        Debug.Log($"Chores completed: {completedChores.Count} points: {chorePointsToday}");
        Debug.Log($"Decoration points: {decorationPointsToday}");
        Debug.Log($"Theme bonuses: {themeBonusToday}");
        Debug.Log($"Daily bonus: {dailyBonusToday}");
        Debug.Log($"Daily score: {dailyScore}");
        Debug.Log($"Total score so far: {totalScore}");
        Debug.Log($"=========================");
        
        // Update UI
        if (uiManager != null)
        {
            uiManager.UpdateScoreText(totalScore);
        }
        
        // Prepare for next day
        PrepareForNextDay();
    }
    
    // Calculate the total points from decorations
    private int CalculateTotalDecorationPoints()
    {
        int total = 0;
        foreach (var score in decorationScores.Values)
        {
            total += score.score;
        }
        return total;
    }
    
    // Prepare for the next day
    private void PrepareForNextDay()
    {
        // Clear completed chores
        completedChores.Clear();
        
        // Reset chore objects
        ChoreItem[] allChores = FindObjectsByType<ChoreItem>(FindObjectsSortMode.None);
        foreach (ChoreItem chore in allChores)
        {
            chore.Reset();
        }
        
        // Update the score display
        UpdateScoreDisplay();
    }
    
    // Update the UI to show the current score
    private void UpdateScoreDisplay()
    {
        // For now, just log to console
        Debug.Log($"Current Score: {dailyScore} (Total: {totalScore})");
        
        // Update UI
        if (uiManager != null)
        {
            uiManager.UpdateScoreText(dailyScore);
        }
    }
    
    // Get score data for UI
    public int GetDailyScore() => dailyScore;
    public int GetTotalScore() => totalScore;
    public int GetChorePoints() => chorePointsToday;
    public int GetDecorationPoints() => decorationPointsToday;
    public int GetThemeBonus() => themeBonusToday;
    public int GetDailyBonus() => dailyBonusToday;
    public int GetCompletedChoresCount() => completedChores.Count;
    
    // Structure to track decoration scores
    private class DecorationScore
    {
        public DecorationItem decoration;
        public InteractionZone zone;
        public int score;
        
        public DecorationScore(DecorationItem decoration, InteractionZone zone, int score)
        {
            this.decoration = decoration;
            this.zone = zone;
            this.score = score;
        }
    }
}