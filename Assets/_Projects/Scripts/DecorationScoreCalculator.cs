using UnityEngine;
using System.Collections.Generic;

public class DecorationScoreCalculator : MonoBehaviour
{
    [System.Serializable]
    public class ThemeBonus
    {
        public string themeName;
        public int bonusPoints = 15;
        public int additionalPerItem = 5; // Additional points for each item beyond the first
    }
    
    [Header("Theme Bonuses")]
    [SerializeField] private List<ThemeBonus> themeBonuses = new List<ThemeBonus>();
    
    [Header("Placement Bonuses")]
    [SerializeField] private int preferredZoneBonus = 20;
    [SerializeField] private int adjacentItemBonus = 5; // Bonus for items placed adjacent to each other
    
    [Header("Hermit Preferences")]
    [SerializeField] private string[] dailyPreferredThemes; // Set per day
    [SerializeField] private int preferredThemeBonus = 25;
    
    // Reference to score manager
    private ScoreManager scoreManager;
    
    // Current day
    private int currentDay = 0;
    
    private void Start()
    {
        scoreManager = ScoreManager.Instance;
        if (scoreManager == null)
        {
            scoreManager = FindFirstObjectByType<ScoreManager>();
        }
        
        // Set daily preferred theme based on day
        UpdateDailyPreference();
    }
    
    public void SetDay(int day)
    {
        currentDay = day;
        UpdateDailyPreference();
    }
    
    // Update the hermit's daily theme preference
    private void UpdateDailyPreference()
    {
        if (dailyPreferredThemes != null && dailyPreferredThemes.Length > 0)
        {
            // Cycle through preferences based on day
            int index = currentDay % dailyPreferredThemes.Length;
            Debug.Log($"Today's preferred theme: {dailyPreferredThemes[index]}");
        }
    }
    
    // Calculate score for a decoration item placement
    public int CalculateScore(DecorationItem item, InteractionZone zone)
    {
        int score = item.pointValue; // Base value
        
        // Add preferred zone bonus
        if (IsInPreferredZone(item, zone))
        {
            score += preferredZoneBonus;
            Debug.Log($"{item.itemName} placed in preferred zone: +{preferredZoneBonus} points");
        }
        
        // Add theme bonus if this item matches today's preference
        if (MatchesPreferredTheme(item))
        {
            score += preferredThemeBonus;
            Debug.Log($"{item.itemName} matches today's preferred theme: +{preferredThemeBonus} points");
        }
        
        // Add adjacency bonus for items placed near other decorations
        int adjacencyBonus = CalculateAdjacencyBonus(item);
        if (adjacencyBonus > 0)
        {
            score += adjacencyBonus;
            Debug.Log($"{item.itemName} has adjacency bonus: +{adjacencyBonus} points");
        }
        
        return score;
    }
    
    // Calculate theme-based scores for all decorations at end of day
    public int CalculateThemeBonuses()
    {
        int totalBonus = 0;
        
        // Get all decoration items
        DecorationItem[] allDecorations = FindObjectsByType<DecorationItem>(FindObjectsSortMode.None);
        
        // Group items by theme
        Dictionary<string, List<DecorationItem>> themeGroups = new Dictionary<string, List<DecorationItem>>();
        
        foreach (DecorationItem item in allDecorations)
        {
            // Get the item's theme(s) - could be multiple
            string[] itemThemes = GetItemThemes(item);
            
            foreach (string theme in itemThemes)
            {
                if (!themeGroups.ContainsKey(theme))
                {
                    themeGroups.Add(theme, new List<DecorationItem>());
                }
                
                themeGroups[theme].Add(item);
            }
        }
        
        // Calculate bonus for each theme group
        foreach (var themeGroup in themeGroups)
        {
            string theme = themeGroup.Key;
            List<DecorationItem> items = themeGroup.Value;
            
            // Only apply bonus if there are at least 2 items of the same theme
            if (items.Count >= 2)
            {
                // Find the bonus for this theme
                ThemeBonus bonus = themeBonuses.Find(b => b.themeName == theme);
                
                if (bonus != null)
                {
                    // Base bonus for having the theme
                    int themeBonus = bonus.bonusPoints;
                    
                    // Additional bonus for each item beyond the first
                    themeBonus += (items.Count - 1) * bonus.additionalPerItem;
                    
                    totalBonus += themeBonus;
                    
                    Debug.Log($"Theme bonus for {theme}: +{themeBonus} points ({items.Count} items)");
                }
            }
        }
        
        return totalBonus;
    }
    
    // Check if the decoration is in one of its preferred zones
    private bool IsInPreferredZone(DecorationItem item, InteractionZone zone)
    {
        if (item.preferredZones == null || item.preferredZones.Length == 0)
            return false;
            
        foreach (string preferredZone in item.preferredZones)
        {
            if (preferredZone == zone.ZoneName)
                return true;
        }
        
        return false;
    }
    
    // Check if this item matches today's preferred theme
    private bool MatchesPreferredTheme(DecorationItem item)
    {
        if (dailyPreferredThemes == null || dailyPreferredThemes.Length == 0)
            return false;
            
        // Get current preferred theme
        string currentPreference = dailyPreferredThemes[currentDay % dailyPreferredThemes.Length];
        
        // Check if this item has the preferred theme
        string[] itemThemes = GetItemThemes(item);
        
        foreach (string theme in itemThemes)
        {
            if (theme == currentPreference)
                return true;
        }
        
        return false;
    }
    
    // Get the theme(s) of an item
    // This is a placeholder - in a real implementation, you'd have theme properties on the items
    private string[] GetItemThemes(DecorationItem item)
    {
        // For now, this is a simple implementation based on the item name
        // In a real game, you'd probably have a theme property on the decoration
        
        // Just parse item name for themes (e.g. "Plant_Tropical" -> "Tropical" theme)
        if (string.IsNullOrEmpty(item.itemName))
            return new string[0];
        
        // Check for known themes in the name
        List<string> themes = new List<string>();
        
        // List of potential themes to check for
        string[] knownThemes = new string[] {
            "Tropical", "Modern", "Vintage", "Cozy", "Beach", "Nautical", 
            "Rustic", "Minimalist", "Plant", "Light", "Dark", "Bright"
        };
        
        foreach (string theme in knownThemes)
        {
            if (item.itemName.Contains(theme))
            {
                themes.Add(theme);
            }
        }
        
        return themes.ToArray();
    }
    
    // Calculate bonus for items placed adjacent to each other
    private int CalculateAdjacencyBonus(DecorationItem item)
    {
        // Get all nearby decoration items
        Collider2D itemCollider = item.GetComponent<Collider2D>();
        if (itemCollider == null) return 0;
        
        // Look for items close to this one
        Collider2D[] nearbyCols = Physics2D.OverlapCircleAll(
            item.transform.position,
            1.0f, // Detection radius - adjust as needed
            LayerMask.GetMask("Decoration")
        );
        
        int nearbyCount = 0;
        
        // Count valid nearby decorations
        foreach (Collider2D col in nearbyCols)
        {
            // Skip self
            if (col.gameObject == item.gameObject)
                continue;
                
            // Check if it's a decoration
            DecorationItem otherItem = col.GetComponent<DecorationItem>();
            if (otherItem != null)
            {
                nearbyCount++;
            }
        }
        
        // Calculate adjacency bonus
        if (nearbyCount > 0)
        {
            return nearbyCount * adjacentItemBonus;
        }
        
        return 0;
    }
    
    // Get the hermit's current preferred theme
    public string GetCurrentPreferredTheme()
    {
        if (dailyPreferredThemes == null || dailyPreferredThemes.Length == 0)
            return string.Empty;
            
        return dailyPreferredThemes[currentDay % dailyPreferredThemes.Length];
    }
    
    // Provide hint about preferred theme - for UI or thought bubble
    public string GetPreferredThemeHint()
    {
        string theme = GetCurrentPreferredTheme();
        
        if (string.IsNullOrEmpty(theme))
            return "I wonder what would look nice today...";
            
        // Different hint formats based on theme
        switch (theme)
        {
            case "Tropical":
                return "I'm feeling like some tropical vibes today...";
            case "Modern":
                return "Something sleek and modern would be perfect...";
            case "Vintage":
                return "I'm nostalgic for some old-fashioned charm...";
            case "Cozy":
                return "I want to feel warm and cozy when I get home...";
            case "Beach":
                return "I miss the beach today...";
            case "Nautical":
                return "Something ocean-themed would remind me of home...";
            default:
                return $"I'm really into {theme.ToLower()} style today...";
        }
    }
}