using UnityEngine;

public class DraggableItem : MonoBehaviour
{
    public bool isDraggable = true;
    public bool isCurrentlyPlaced = false;
    public Vector3 lastValidPosition;
    
    [Header("Visual Feedback")]
    public bool useColorFeedback = true;
    public Color validPlacementColor = new Color(0, 1, 0, 0.7f);
    public Color invalidPlacementColor = new Color(1, 0, 0, 0.7f);
    
    private Color originalColor;
    private SpriteRenderer spriteRenderer;
    
    void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer != null)
        {
            originalColor = spriteRenderer.color;
        }
        else
        {
            Debug.LogWarning($"DraggableItem on {gameObject.name} has no SpriteRenderer for visual feedback");
        }
        
        // Make sure we have a collider for raycasting
        Collider2D collider = GetComponent<Collider2D>();
        if (collider == null)
        {
            Debug.LogError($"DraggableItem on {gameObject.name} needs a Collider2D to be clickable");
        }
        
        lastValidPosition = transform.position;
    }
    
    public void SetColorFeedback(bool isValid)
    {
        if (useColorFeedback && spriteRenderer != null)
        {
            spriteRenderer.color = isValid ? validPlacementColor : invalidPlacementColor;
        }
    }
    
    public void ResetColor()
    {
        if (spriteRenderer != null)
        {
            spriteRenderer.color = originalColor;
        }
    }
}