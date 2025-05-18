using UnityEngine;

public class DecorationItem : InteractableItem
{
    // Always complete once placed
    public override bool IsComplete => true;
    
    // Preferred zones for scoring bonuses
    [Tooltip("Zones where this decoration receives bonus points")]
    public string[] preferredZones;
    
    // Last zone this decoration was placed in
    private InteractionZone currentZone;
    
    // Called when the decoration is placed
    public override void OnPlaced()
    {
        // Find the zone this decoration is in
        UpdateCurrentZone();
        
        // If we found a zone, register with score manager
        if (currentZone != null)
        {
            // Calculate placement score via the score manager
            ScoreManager.Instance?.RegisterDecorationPlacement(this, currentZone);
        }
    }
    
    // Update which zone this decoration is in
    public void UpdateCurrentZone()
    {
        // Get our collider
        Collider2D itemCollider = GetComponent<Collider2D>();
        if (itemCollider == null) return;
        
        // Find all interaction zones
        Collider2D[] zoneColliders = Physics2D.OverlapBoxAll(
            transform.position,
            itemCollider.bounds.size,
            0,
            LayerMask.GetMask("InteractionZone")
        );
        
        // Check each zone
        foreach (Collider2D zoneCollider in zoneColliders)
        {
            InteractionZone zone = zoneCollider.GetComponent<InteractionZone>();
            if (zone != null && zone.CanAcceptItem(this))
            {
                currentZone = zone;
                return;
            }
        }
        
        // If we got here, we're not in any valid zone
        currentZone = null;
    }
    
    // Check if this decoration is in a preferred zone
    public bool IsInPreferredZone()
    {
        if (currentZone == null || preferredZones == null || preferredZones.Length == 0)
            return false;
            
        foreach (string zoneName in preferredZones)
        {
            if (zoneName == currentZone.ZoneName)
                return true;
        }
        
        return false;
    }
    
    // Get the current score value
    public int GetCurrentScore()
    {
        if (currentZone == null)
            return 0;
            
        // Base value
        int score = pointValue;
        
        // Bonus for preferred placement
        if (IsInPreferredZone())
        {
            score += 20; // Special points value
        }
        
        return score;
    }
}
