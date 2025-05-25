using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public class ScoreManager : MonoBehaviour
{
    [Header("Current Day Scoring")]
    [SerializeField] private int currentDayScore = 0;
    [SerializeField] private int totalGameScore = 0;

    [Header("Bonus Settings")]
    [SerializeField] private int dailyCompletionBonus = 20;
    [SerializeField] private int perfectPlacementBonus = 5; // Extra points for placing in optimal zones

    [Header("Debug")]
    [SerializeField] private bool showDetailedScoring = true;

    // Events for UI updates
    public static System.Action<int> OnScoreUpdated;
    public static System.Action<int> OnDayScoreCalculated;

    // Singleton pattern
    public static ScoreManager Instance { get; private set; }

    private List<DecorationItem> trackedItems = new List<DecorationItem>();
    private Dictionary<DecorationItem, PlacementScore> dailyScores = new Dictionary<DecorationItem, PlacementScore>();

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

    private void Start()
    {
        // Find all decoration items in the scene
        RefreshTrackedItems();
    }

    // Refresh the list of tracked decoration items
    public void RefreshTrackedItems()
    {
        trackedItems.Clear();
        trackedItems.AddRange(FindObjectsByType<DecorationItem>(FindObjectsSortMode.None));

        Debug.Log($"ScoreManager: Now tracking {trackedItems.Count} decoration items");
    }

    // Add a new item to tracking (useful for items spawned during gameplay)
    public void AddItemToTracking(DecorationItem item)
    {
        if (!trackedItems.Contains(item))
        {
            trackedItems.Add(item);
            Debug.Log($"ScoreManager: Added '{item.ItemData?.itemName}' to tracking");
        }
    }

    // Remove an item from tracking
    public void RemoveItemFromTracking(DecorationItem item)
    {
        if (trackedItems.Contains(item))
        {
            trackedItems.Remove(item);
            dailyScores.Remove(item);
            Debug.Log($"ScoreManager: Removed '{item.ItemData?.itemName}' from tracking");
        }
    }

    // Calculate score for a single item
    public void ScoreItem(DecorationItem item)
    {
        if (item == null || item.ItemData == null) return;

        // Force recalculation using current collider position
        item.CalculateCurrentScore();

        if (item.IsScored)
        {
            dailyScores[item] = item.CurrentScore;

            if (showDetailedScoring)
            {
                Vector2 colliderPos = item.GetColliderWorldCenter();
                Vector2 transformPos = item.transform.position;
                Vector2 offset = colliderPos - transformPos;

                string positionInfo = $"Transform: {transformPos}, Collider: {colliderPos}";
                if (offset.magnitude > 0.01f)
                {
                    positionInfo += $", Offset: {offset}";
                }

                Debug.Log($"Scored '{item.ItemData.itemName}': {item.CurrentScore.pointsAwarded} points - {item.CurrentScore.scoringReason} ({positionInfo})");
            }
        }
    }

    // Score all tracked items
    public void ScoreAllItems()
    {
        dailyScores.Clear();

        // Remove any null items from tracking
        trackedItems.RemoveAll(item => item == null || item.ItemData == null);

        foreach (DecorationItem item in trackedItems)
        {
            ScoreItem(item);
        }

        CalculateDayScore();
    }

    // Calculate the total score for the current day
    public void CalculateDayScore()
    {
        currentDayScore = 0;
        int perfectPlacements = 0;

        foreach (var kvp in dailyScores)
        {
            DecorationItem item = kvp.Key;
            PlacementScore score = kvp.Value;

            currentDayScore += score.pointsAwarded;

            // Check if this was a perfect placement (highest possible score for this item)
            if (IsOptimalPlacement(item, score))
            {
                currentDayScore += perfectPlacementBonus;
                perfectPlacements++;
            }
        }

        // Add completion bonus if all items are placed optimally
        if (perfectPlacements == trackedItems.Count && trackedItems.Count > 0)
        {
            currentDayScore += dailyCompletionBonus;

            if (showDetailedScoring)
            {
                Debug.Log($"Perfect day! Added {dailyCompletionBonus} completion bonus!");
            }
        }

        if (showDetailedScoring)
        {
            Debug.Log($"Day Score: {currentDayScore} (Perfect placements: {perfectPlacements}/{trackedItems.Count})");
        }

        OnDayScoreCalculated?.Invoke(currentDayScore);
        OnScoreUpdated?.Invoke(totalGameScore + currentDayScore);
    }

    // Check if an item placement is optimal (highest possible score)
    private bool IsOptimalPlacement(DecorationItem item, PlacementScore score)
    {
        if (item.ItemData == null) return false;

        // Find the highest possible score for this item
        int maxPossibleScore = item.ItemData.basePoints; // Start with base points

        foreach (var preference in item.ItemData.placementPreferences)
        {
            // Check default area points
            if (preference.defaultAreaPoints > maxPossibleScore)
            {
                maxPossibleScore = preference.defaultAreaPoints;
            }

            // Check zone-specific points
            foreach (var zone in preference.zones)
            {
                if (zone.pointValue > maxPossibleScore)
                {
                    maxPossibleScore = zone.pointValue;
                }
            }
        }

        return score.pointsAwarded >= maxPossibleScore;
    }

    // Finalize the day (add current day score to total)
    public void FinalizeDayScore()
    {
        totalGameScore += currentDayScore;

        if (showDetailedScoring)
        {
            Debug.Log($"Day finalized! Day score: {currentDayScore}, Total score: {totalGameScore}");
        }

        OnScoreUpdated?.Invoke(totalGameScore);
    }

    // Start a new day (reset daily tracking)
    public void StartNewDay()
    {
        currentDayScore = 0;
        dailyScores.Clear();

        // Reset all tracked items' scoring
        foreach (DecorationItem item in trackedItems)
        {
            item.ResetScore();
        }

        Debug.Log("New day started - scores reset");
    }

    // Get detailed score breakdown
    public ScoreBreakdown GetScoreBreakdown()
    {
        ScoreBreakdown breakdown = new ScoreBreakdown();
        breakdown.currentDayScore = currentDayScore;
        breakdown.totalGameScore = totalGameScore;
        breakdown.itemScores = new List<ItemScore>();

        foreach (var kvp in dailyScores)
        {
            ItemScore itemScore = new ItemScore();
            itemScore.itemName = kvp.Key.ItemData?.itemName ?? "Unknown Item";
            itemScore.pointsAwarded = kvp.Value.pointsAwarded;
            itemScore.scoringReason = kvp.Value.scoringReason;
            itemScore.isOptimal = IsOptimalPlacement(kvp.Key, kvp.Value);

            breakdown.itemScores.Add(itemScore);
        }

        return breakdown;
    }

    public void ValidateItemPositions()
    {
        if (!showDetailedScoring) return;

        Debug.Log("=== ITEM POSITION VALIDATION ===");

        foreach (DecorationItem item in trackedItems)
        {
            if (item == null || item.ItemData == null) continue;

            Vector2 transformPos = item.transform.position;
            Vector2 colliderPos = item.GetColliderWorldCenter();
            Vector2 offset = colliderPos - transformPos;

            string validation = $"Item '{item.ItemData.itemName}':";
            validation += $"\n  Transform: {transformPos}";
            validation += $"\n  Collider: {colliderPos}";

            if (offset.magnitude > 0.01f)
            {
                validation += $"\n  Offset: {offset} (magnitude: {offset.magnitude:F3})";
            }
            else
            {
                validation += "\n  Positions match";
            }

            // Check which areas contain each position
            PlaceableArea[] areas = FindObjectsByType<PlaceableArea>(FindObjectsSortMode.None);
            bool transformInArea = false;
            bool colliderInArea = false;

            foreach (PlaceableArea area in areas)
            {
                if (area.ContainsPoint(transformPos))
                {
                    transformInArea = true;
                    validation += $"\n  Transform in area: {area.AreaIdentifier}";
                }

                if (area.ContainsPoint(colliderPos))
                {
                    colliderInArea = true;
                    validation += $"\n  Collider in area: {area.AreaIdentifier}";
                }
            }

            if (!transformInArea && !colliderInArea)
            {
                validation += "\n  Neither position in any area";
            }
            else if (transformInArea != colliderInArea)
            {
                validation += "\n  WARNING: Position mismatch between transform and collider!";
            }

            Debug.Log(validation);
        }

        Debug.Log("=== END VALIDATION ===");
    }

    public void ResetGame()
    {
        currentDayScore = 0;
        totalGameScore = 0;
        dailyScores.Clear();
        trackedItems.Clear();

        Debug.Log("ScoreManager: Game reset");
    }

    // Public getters
    public int GetCurrentDayScore() => currentDayScore;
    public int GetTotalGameScore() => totalGameScore;
    public Dictionary<DecorationItem, PlacementScore> GetDailyScores() => new Dictionary<DecorationItem, PlacementScore>(dailyScores);
}

[System.Serializable]
public class ScoreBreakdown
{
    public int currentDayScore;
    public int totalGameScore;
    public List<ItemScore> itemScores;
}

[System.Serializable]
public class ItemScore
{
    public string itemName;
    public int pointsAwarded;
    public string scoringReason;
    public bool isOptimal;
}