using UnityEngine;
using System.Collections;

public class DragAndDropHandler : MonoBehaviour
{   
    public static DragAndDropHandler Instance { get; private set;}
    private bool isDragging = false;
    public bool IsDragging => isDragging;
    private Vector3 offset;
    private Camera mainCamera;

    // The object being currently dragged
    private GameObject draggedObject;
    private Vector3 previousPos;
    private Color originalColor; // Store original sprite color

    // Layer mask for the table surface
    [SerializeField] private LayerMask placeableSurfaceLayer;

    // Color for invalid placement
    [SerializeField] private Color invalidPlacementColor = Color.red;

    [SerializeField] private TimeController timeController;
    
    // Reference to score manager
    [SerializeField] private ScoreManager scoreManager;

    private void Start()
    {
        mainCamera = Camera.main;
        
        // Find score manager if not assigned
        if (scoreManager == null)
        {
            scoreManager = FindFirstObjectByType<ScoreManager>();
        }
    }

    private void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
            // Cast a ray to see if we hit a draggable object
            RaycastHit2D hit = Physics2D.Raycast(mainCamera.ScreenToWorldPoint(Input.mousePosition), Vector2.zero);

            if (hit.collider != null && hit.collider.CompareTag("Draggable"))
            {
                isDragging = true;
                draggedObject = hit.collider.gameObject;
                previousPos = draggedObject.transform.position; // Store the position before dragging
                offset = draggedObject.transform.position - mainCamera.ScreenToWorldPoint(Input.mousePosition);

                // Store the original color of the sprite
                SpriteRenderer renderer = draggedObject.GetComponent<SpriteRenderer>();
                if (renderer != null)
                {
                    originalColor = renderer.color;
                }
                
                // De-highlight all zones first
                HighlightValidZones(false);
            }
        }

        if (Input.GetMouseButtonUp(0) && isDragging)
        {
            // Check if the object is over a valid placement surface
            if (IsFullyOverValidSurface())
            {
                // Valid placement - leave it where it is
                // Reset color to original
                ResetSpriteColor();
                
                // Check if we're over an interaction zone
                TryInteractWithZone(draggedObject);
                
                // For decoration items, update placement score
                DecorationItem decoItem = draggedObject.GetComponent<DecorationItem>();
                if (decoItem != null)
                {
                    decoItem.UpdateCurrentZone();
                    decoItem.OnPlaced();
                }
            }
            else
            {
                // Invalid placement, return to original position
                ReturnObjectToOrigin();
                ResetSpriteColor();
            }

            // De-highlight all zones
            HighlightValidZones(false);
            
            isDragging = false;
            draggedObject = null;
        }

        if (isDragging)
        {
            // Move the object with the mouse while dragging
            Vector3 newPosition = mainCamera.ScreenToWorldPoint(Input.mousePosition) + offset;
            draggedObject.transform.position = new Vector3(newPosition.x, newPosition.y, draggedObject.transform.position.z);

            // Update color based on current position validity
            UpdateSpriteColor(IsFullyOverValidSurface());
            
            // Highlight valid zones
            HighlightValidZones(true);
        }
    }

    private bool IsFullyOverValidSurface()
    {
        // Get the collider of the draggable object
        Collider2D objCollider = draggedObject.GetComponent<Collider2D>();
        if (objCollider == null) return false;

        // Get the bounds of the draggable object's collider
        Bounds objBounds = objCollider.bounds;

        // Find all potential placeable surfaces in the vicinity
        Collider2D[] surfaceColliders = Physics2D.OverlapAreaAll(
            objBounds.min,
            objBounds.max,
            placeableSurfaceLayer
        );

        // If we found no potential surfaces, the object is not over a valid surface
        if (surfaceColliders.Length == 0) return false;

        // Check each surface to see if it fully contains the object
        foreach (Collider2D surfaceCollider in surfaceColliders)
        {
            // Skip nulls
            if (surfaceCollider == null) continue;

            // Get the bounds of this surface collider
            Bounds surfaceBounds = surfaceCollider.bounds;

            // Check if surface fully contains the object
            if (IsFullyContained(objBounds, surfaceBounds))
            {
                return true;
            }
        }

        // No valid surface fully contains the object
        return false;
    }

    // Checks if the first bounds is fully contained within the second bounds
    private bool IsFullyContained(Bounds objectBounds, Bounds containerBounds)
    {
        // Check if all corners of the object bounds are inside the container bounds
        return containerBounds.Contains(new Vector3(objectBounds.min.x, objectBounds.min.y, objectBounds.min.z)) &&
               containerBounds.Contains(new Vector3(objectBounds.max.x, objectBounds.min.y, objectBounds.min.z)) &&
               containerBounds.Contains(new Vector3(objectBounds.min.x, objectBounds.max.y, objectBounds.min.z)) &&
               containerBounds.Contains(new Vector3(objectBounds.max.x, objectBounds.max.y, objectBounds.min.z));
    }

    private void ReturnObjectToOrigin()
    {
        draggedObject.transform.position = previousPos;
    }

    private bool TryInteractWithZone(GameObject draggedItem)
    {
        // Get the item component
        InteractableItem item = draggedItem.GetComponent<InteractableItem>();
        if (item == null) return false;

        // Find nearby interaction zones
        Collider2D[] nearbyZones = Physics2D.OverlapCircleAll(
            draggedItem.transform.position,
            1.0f, // Detection radius
            LayerMask.GetMask("InteractionZone")
        );

        // Try to interact with each zone
        foreach (Collider2D zoneCollider in nearbyZones)
        {
            InteractionZone zone = zoneCollider.GetComponent<InteractionZone>();
            if (zone == null) continue;

            // For ChoreItems, check if this zone is appropriate for the current state
            ChoreItem choreItem = item as ChoreItem;
            if (choreItem != null && choreItem.TryInteractWithZone(zone.ZoneName))
            {
                // Chore item has progressed its state
                
                // Check if the chore is now complete
                if (choreItem.IsComplete)
                {
                    // Register completion with score manager
                    scoreManager?.RegisterCompletedChore(choreItem);
                }
                
                return true;
            }

            // For decoration items
            DecorationItem decorItem = item as DecorationItem;
            if (decorItem != null && zone.CanAcceptItem(decorItem))
            {
                // Register with score manager
                scoreManager?.RegisterDecorationPlacement(decorItem, zone);
                return true;
            }

            // For other items, use generic zone handling
            if (zone.CanAcceptItem(item))
            {
                zone.ProcessItem(item);
                return true;
            }
        }

        return false;
    }
    
    // Highlight all valid zones for the current dragged item
    private void HighlightValidZones(bool highlight)
    {
        // Only process if we have a dragged object
        if (draggedObject == null) return;
        
        // Get the item component
        InteractableItem item = draggedObject.GetComponent<InteractableItem>();
        if (item == null) return;
        
        // Find all interaction zones
        InteractionZone[] allZones = FindObjectsByType<InteractionZone>(FindObjectsSortMode.None);
        
        foreach (InteractionZone zone in allZones)
        {
            if (highlight)
            {
                // Only highlight zones that can accept this item
                bool canAccept = zone.CanAcceptItem(item);
                
                // For chore items, also check if this zone is appropriate for the current state
                ChoreItem choreItem = item as ChoreItem;
                if (choreItem != null)
                {
                    string requiredZone = choreItem.RequiredZoneForCurrentState;
                    if (requiredZone != null && requiredZone != zone.ZoneName)
                    {
                        canAccept = false;
                    }
                }
                
                zone.Highlight(canAccept);
            }
            else
            {
                // Turn off highlighting
                zone.Highlight(false);
            }
        }
    }

    private void UpdateSpriteColor(bool isValid)
    {
        SpriteRenderer renderer = draggedObject.GetComponent<SpriteRenderer>();
        if (renderer != null)
        {
            // If position is invalid, change to red; otherwise keep original color
            renderer.color = isValid ? originalColor : invalidPlacementColor;
        }
    }

    private void ResetSpriteColor()
    {
        SpriteRenderer renderer = draggedObject.GetComponent<SpriteRenderer>();
        if (renderer != null)
        {
            renderer.color = originalColor;
        }
    }
}