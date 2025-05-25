using UnityEngine;

[RequireComponent(typeof(DraggableItem))]
public class DecorationItem : MonoBehaviour
{
    [Header("Item Data")]
    [SerializeField] private DecorationItemData itemData;

    [Header("Current Placement")]
    [SerializeField] private bool isScored = false;
    [SerializeField] private PlacementScore currentScore;

    // Properties
    public DecorationItemData ItemData => itemData;
    public bool IsScored => isScored;
    public PlacementScore CurrentScore => currentScore;

    private DraggableItem draggableComponent;

    private void Awake()
    {
        draggableComponent = GetComponent<DraggableItem>();
    }

    private void Start()
    {
        // Initialize sprite if item data is assigned
        if (itemData != null && itemData.itemSprite != null)
        {
            SpriteRenderer spriteRenderer = GetComponent<SpriteRenderer>();
            if (spriteRenderer != null)
            {
                spriteRenderer.sprite = itemData.itemSprite;
            }
        }
    }

    // Called when the item is placed (you can call this from DragDropManager)
    public void OnItemPlaced()
    {
        if (itemData == null)
        {
            Debug.LogWarning($"DecorationItem '{gameObject.name}' has no ItemData assigned!");
            return;
        }

        CalculateCurrentScore();
    }

    // Calculate and store the current placement score
    public void CalculateCurrentScore()
    {
        if (itemData == null) return;

        // Get collider position instead of transform position
        Vector2 currentPosition = GetColliderCenter();
        PlaceableArea[] allAreas = FindObjectsByType<PlaceableArea>(FindObjectsSortMode.None);

        PlacementScore bestScore = new PlacementScore();
        bestScore.pointsAwarded = itemData.wrongPlacementPenalty;
        bestScore.scoringReason = "Not placed in any valid area";
        bestScore.worldPosition = currentPosition;
        bestScore.placedInValidArea = false;

        // Check all placeable areas to find the best score
        foreach (PlaceableArea area in allAreas)
        {
            if (area.ContainsPoint(currentPosition))
            {
                PlacementScore areaScore = area.CalculatePlacementScore(itemData, currentPosition);

                // Take the best score (highest points)
                if (areaScore.pointsAwarded > bestScore.pointsAwarded)
                {
                    bestScore = areaScore;
                }
            }
        }

        currentScore = bestScore;
        isScored = true;

        Debug.Log($"Item '{itemData.itemName}' scored: {currentScore.pointsAwarded} points - {currentScore.scoringReason}");
    }

    // Reset scoring (useful when item is picked up again)
    public void ResetScore()
    {
        isScored = false;
        currentScore = new PlacementScore();
    }

    // Public method to set item data (useful for runtime instantiation)
    public void SetItemData(DecorationItemData data)
    {
        itemData = data;

        // Update sprite if available
        if (itemData != null && itemData.itemSprite != null)
        {
            SpriteRenderer spriteRenderer = GetComponent<SpriteRenderer>();
            if (spriteRenderer != null)
            {
                spriteRenderer.sprite = itemData.itemSprite;
            }
        }
    }

    // Get a preview of what the score would be at a specific position
    public PlacementScore PreviewScoreAtPosition(Vector2 worldPosition)
    {
        if (itemData == null)
        {
            return new PlacementScore
            {
                pointsAwarded = 0,
                scoringReason = "No item data",
                worldPosition = worldPosition,
                placedInValidArea = false
            };
        }

        PlaceableArea[] allAreas = FindObjectsByType<PlaceableArea>(FindObjectsSortMode.None);

        PlacementScore bestScore = new PlacementScore();
        bestScore.pointsAwarded = itemData.wrongPlacementPenalty;
        bestScore.scoringReason = "Not in any valid area";
        bestScore.worldPosition = worldPosition;
        bestScore.placedInValidArea = false;

        foreach (PlaceableArea area in allAreas)
        {
            if (area.ContainsPoint(worldPosition))
            {
                PlacementScore areaScore = area.CalculatePlacementScore(itemData, worldPosition);

                if (areaScore.pointsAwarded > bestScore.pointsAwarded)
                {
                    bestScore = areaScore;
                }
            }
        }

        return bestScore;
    }

    private Vector2 GetColliderCenter()
    {
        Collider2D collider = GetComponent<Collider2D>();
        if (collider != null)
        {
            return collider.bounds.center;
        }

        // Fallback to transform position if no collider
        return transform.position;
    }

    // Public method to get collider center (for use by other systems)
    public Vector2 GetColliderWorldCenter()
    {
        return GetColliderCenter();
    }
}