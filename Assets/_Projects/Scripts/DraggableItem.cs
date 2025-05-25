using UnityEngine;
using UnityEngine.EventSystems;

public class DraggableItem : MonoBehaviour
{
    public bool isDraggable = true;
    public bool isCurrentlyPlaced = false;
    public Vector3 lastValidPosition;

    [Header("Visual Feedback")]
    public bool useColorFeedback = true;
    public Color validPlacementColor = new Color(0, 1, 0, 0.7f);
    public Color invalidPlacementColor = new Color(1, 0, 0, 0.7f);

    [Header("Interaction Settings")]
    [Tooltip("Optional larger collider for easier clicking/dragging. If null, uses the main collider.")]
    public Collider2D interactionCollider;

    [Tooltip("If true, automatically creates an interaction collider based on sprite bounds")]
    public bool autoCreateInteractionCollider = true;

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

    void Start()
    {
        SetupInteractionCollider();
    }

    private void SetupInteractionCollider()
    {
        // If no interaction collider is specified and auto-create is enabled
        if (interactionCollider == null && autoCreateInteractionCollider)
        {
            // Try to find an existing interaction collider
            Collider2D[] colliders = GetComponents<Collider2D>();

            // Look for a collider marked as trigger (we'll use this as interaction)
            foreach (Collider2D col in colliders)
            {
                if (col.isTrigger)
                {
                    interactionCollider = col;
                    break;
                }
            }

            // If no trigger collider found, create one based on sprite bounds
            if (interactionCollider == null)
            {
                SpriteRenderer spriteRenderer = GetComponent<SpriteRenderer>();
                if (spriteRenderer != null && spriteRenderer.sprite != null)
                {
                    // Create a new game object for the interaction collider
                    GameObject interactionObj = new GameObject("InteractionCollider");
                    interactionObj.transform.SetParent(transform);
                    interactionObj.transform.localPosition = Vector3.zero;
                    interactionObj.transform.localRotation = Quaternion.identity;
                    interactionObj.transform.localScale = Vector3.one;

                    // Add box collider that matches sprite bounds
                    BoxCollider2D interactionBox = interactionObj.AddComponent<BoxCollider2D>();
                    interactionBox.isTrigger = true; // Make it a trigger so it doesn't interfere with physics

                    // Size it to match the sprite bounds
                    Bounds spriteBounds = spriteRenderer.sprite.bounds;
                    interactionBox.size = spriteBounds.size;
                    interactionBox.offset = spriteBounds.center;

                    interactionCollider = interactionBox;

                    Debug.Log($"Auto-created interaction collider for {gameObject.name} with size {interactionBox.size}");
                }
            }
        }

        // Fallback to main collider if no interaction collider is set
        if (interactionCollider == null)
        {
            interactionCollider = GetComponent<Collider2D>();
        }

        // Add tooltip trigger to the interaction collider if it doesn't have one
        if (interactionCollider != null)
        {
            TooltipTrigger tooltipTrigger = interactionCollider.GetComponent<TooltipTrigger>();
            if (tooltipTrigger == null)
            {
                tooltipTrigger = interactionCollider.gameObject.AddComponent<TooltipTrigger>();
                Debug.Log($"Added TooltipTrigger to {interactionCollider.gameObject.name}");
            }

            // Ensure the interaction collider can receive pointer events
            // For 2D UI events to work with world space colliders, we need a Physics2DRaycaster on the camera
            EnsurePhysics2DRaycaster();
        }
    }

    private void EnsurePhysics2DRaycaster()
    {
        Camera mainCam = Camera.main;
        if (mainCam != null)
        {
            Physics2DRaycaster raycaster = mainCam.GetComponent<Physics2DRaycaster>();
            if (raycaster == null)
            {
                raycaster = mainCam.gameObject.AddComponent<Physics2DRaycaster>();
                Debug.Log("Added Physics2DRaycaster to main camera for tooltip events");
            }
        }
    }

    public Collider2D GetInteractionCollider()
    {
        return interactionCollider != null ? interactionCollider : GetComponent<Collider2D>();
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