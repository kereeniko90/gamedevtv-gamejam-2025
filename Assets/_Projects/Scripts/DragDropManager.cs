using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;

public class DragDropManager : MonoBehaviour
{
    [Header("Placement Settings")]
    [SerializeField] private float dragHoverHeight = 0.1f;
    [SerializeField] private bool showDebugBounds = true;

    private Camera mainCamera;
    private DraggableItem currentDraggable;
    private Vector3 dragOffset;
    private bool canPlace = false;

    // Cache all placeable areas
    private List<PlaceableArea> placeableAreas = new List<PlaceableArea>();

    void Start()
    {
        mainCamera = Camera.main;

        // Find all placeable areas in the scene
        placeableAreas.AddRange(FindObjectsByType<PlaceableArea>(FindObjectsSortMode.None));
        Debug.Log($"Found {placeableAreas.Count} placeable areas in the scene");
    }

    void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
            Vector2 mouseWorldPos = GetMouseWorldPosition();
            Debug.Log($"Mouse clicked at world position: {mouseWorldPos}");
            SoundManager.Instance.PlaySFX(SoundEffect.PickupDeco);
            RaycastHit2D hit = Physics2D.Raycast(mouseWorldPos, Vector2.zero);
            if (hit.collider != null)
            {
                Debug.Log($"Raycast hit collider: {hit.collider.name} on object: {hit.collider.gameObject.name}");

                DraggableItem draggable = hit.collider.GetComponent<DraggableItem>();

                // If the hit collider doesn't have DraggableItem, check the parent
                if (draggable == null)
                {
                    Debug.Log("No DraggableItem on hit collider, checking parent...");
                    draggable = hit.collider.GetComponentInParent<DraggableItem>();
                }

                if (draggable != null)
                {
                    Debug.Log($"Found DraggableItem: {draggable.name}, isDraggable: {draggable.isDraggable}");

                    if (draggable.isDraggable)
                    {
                        draggable.transform.DOKill();
                        // Use the interaction collider bounds for better hit detection
                        Collider2D interactionCollider = draggable.GetInteractionCollider();
                        Debug.Log($"Interaction collider: {interactionCollider.name}, bounds: {interactionCollider.bounds}");

                        // Check if the mouse position is actually within the interaction bounds
                        if (interactionCollider.bounds.Contains(mouseWorldPos))
                        {
                            Debug.Log("Mouse is within interaction bounds - starting drag");
                            StartDragging(draggable, hit.point);
                        }
                        else
                        {
                            Debug.Log("Mouse is NOT within interaction bounds");
                        }
                    }
                }
                else
                {
                    Debug.Log("No DraggableItem found on hit object or its parent");
                }
            }
            else
            {
                Debug.Log("Raycast hit nothing");
            }
        }

        // Handle active dragging
        if (currentDraggable != null)
        {
            Vector3 targetPosition = GetMouseWorldPosition() + dragOffset;
            targetPosition.z = -dragHoverHeight; // Hover effect
            currentDraggable.transform.position = targetPosition;

            // Check placement validity
            CheckPlacement();

            // Handle drop
            if (Input.GetMouseButtonUp(0))
            {
                DropItem();
                
            }
        }
    }

    private void StartDragging(DraggableItem draggable, Vector2 hitPoint)
    {
        currentDraggable = draggable;

        // Hide any tooltips when starting to drag and show description during drag
        TooltipTrigger tooltipTrigger = currentDraggable.GetInteractionCollider().GetComponent<TooltipTrigger>();
        if (tooltipTrigger != null)
        {
            tooltipTrigger.ForceHideTooltip();
        }

        // Show hint during dragging if the item has description
        DecorationItem decorationItem = currentDraggable.GetComponent<DecorationItem>();
        if (decorationItem != null && decorationItem.ItemData != null && HintUI.Instance != null)
        {
            HintUI.Instance.ShowHint(decorationItem.ItemData.description);
        }

        // Calculate offset to maintain relative position of cursor to object
        dragOffset = currentDraggable.transform.position - new Vector3(hitPoint.x, hitPoint.y, currentDraggable.transform.position.z);
        dragOffset.z = 0;
    }

    private void CheckPlacement()
    {
        Collider2D collider = currentDraggable.GetComponent<Collider2D>();
        if (collider == null)
        {
            Debug.LogWarning("Draggable item has no collider for bounds checking");
            return;
        }

        // Check if the object is fully contained in any placeable area
        bool insideValidArea = false;
        bool inRestrictedArea = false;

        // Get decoration item data for restriction checking
        DecorationItem decorationItem = currentDraggable.GetComponent<DecorationItem>();

        foreach (PlaceableArea area in placeableAreas)
        {
            if (area.ContainsCollider(collider))
            {
                insideValidArea = true;

                // Check if this item is restricted from this area
                if (decorationItem != null && decorationItem.ItemData != null)
                {
                    if (decorationItem.ItemData.restrictedAreas.Contains(area.AreaIdentifier))
                    {
                        inRestrictedArea = true;
                        break; // Exit early if we find a restriction
                    }
                }
            }
        }

        // Check if overlapping with any other draggable items
        bool isOverlapping = CheckForOverlap(collider);

        // Only allow placement if inside area AND not in restricted area AND not overlapping other items
        canPlace = insideValidArea && !inRestrictedArea && !isOverlapping;

        // Update visual feedback
        currentDraggable.SetColorFeedback(canPlace);
    }

    private bool CheckForOverlap(Collider2D collider)
    {
        // Get all colliders that overlap with this one
        ContactFilter2D filter = new ContactFilter2D();
        filter.useTriggers = false; // Only consider non-trigger colliders

        List<Collider2D> results = new List<Collider2D>();
        int count = Physics2D.OverlapCollider(collider, filter, results);

        // Check if any of the overlapping colliders belong to another draggable item
        foreach (Collider2D other in results)
        {
            // Skip checking against its own collider
            if (other.gameObject == collider.gameObject)
                continue;

            // Check if the other object is a draggable item
            DraggableItem otherDraggable = other.GetComponent<DraggableItem>();
            if (otherDraggable != null && otherDraggable.isCurrentlyPlaced)
            {
                // Found overlap with another placed draggable item
                return true;
            }

            if (other.gameObject.CompareTag("Blocker"))
            {
                return true;
            }
        }

        // No overlap with other draggable items
        return false;
    }

    private void DropItem()
    {
        // Hide hint when dropping
        if (HintUI.Instance != null)
        {
            HintUI.Instance.HideHint();
        }

        if (canPlace)
        {
            // Place at current position, but reset Z to 0
            Vector3 finalPosition = currentDraggable.transform.position;
            finalPosition.z = 0;
            currentDraggable.transform.position = finalPosition;
            currentDraggable.lastValidPosition = finalPosition;
            currentDraggable.isCurrentlyPlaced = true;
            SoundManager.Instance.PlaySFX(SoundEffect.PlaceDeco);

            // Handle scoring if this is a decoration item
            DecorationItem decorationItem = currentDraggable.GetComponent<DecorationItem>();
            if (decorationItem != null)
            {
                // DON'T calculate score during placement - only at end of day
                // Just reset the score so it's clean for end-of-day calculation
                decorationItem.ResetScore();

                // Add to score manager tracking if not already tracked
                if (ScoreManager.Instance != null)
                {
                    ScoreManager.Instance.AddItemToTracking(decorationItem);
                }

                // Show placement debug but don't calculate final score yet
                if (PlacementDebugUI.Instance != null)
                {
                    // Preview the score but don't make it final
                    PlacementScore previewScore = decorationItem.PreviewScoreAtPosition(decorationItem.GetColliderWorldCenter());
                    PlacementDebugUI.Instance.ShowPlacementScore(decorationItem, previewScore);
                }
            }
        }
        else
        {
            // Return to last valid position
            currentDraggable.transform.position = currentDraggable.lastValidPosition;
            SoundManager.Instance.PlaySFX(SoundEffect.InvalidPlace);
            // Reset scoring for decoration items when they return to invalid position
            DecorationItem decorationItem = currentDraggable.GetComponent<DecorationItem>();
            if (decorationItem != null)
            {
                decorationItem.ResetScore();
            }

            if (PlacementDebugUI.Instance != null)
            {
                PlacementDebugUI.Instance.ClearDebugInfo();
            }
        }

        // Reset color and clear reference
        currentDraggable.ResetColor();
        currentDraggable = null;
    }    

    private Vector3 GetMouseWorldPosition()
    {
        Vector3 mousePos = Input.mousePosition;
        mousePos.z = -mainCamera.transform.position.z;
        return mainCamera.ScreenToWorldPoint(mousePos);
    }

    // For debugging - show the correct collider shape
    private void OnDrawGizmos()
    {
        if (!showDebugBounds || currentDraggable == null) return;

        Collider2D collider = currentDraggable.GetComponent<Collider2D>();
        if (collider == null) return;

        Gizmos.color = canPlace ? Color.green : Color.red;

        // Draw appropriate debug shape based on collider type
        if (collider is CircleCollider2D circleCollider)
        {
            Vector3 center = collider.bounds.center;
            // Estimate radius from bounds
            float radius = Mathf.Max(collider.bounds.extents.x, collider.bounds.extents.y);
            DrawWireCircle(center, radius, 36);
        }
        else if (collider is BoxCollider2D)
        {
            Bounds bounds = collider.bounds;
            Gizmos.DrawWireCube(bounds.center, bounds.size);
        }
        else if (collider is PolygonCollider2D polyCollider)
        {
            DrawWirePolygon(polyCollider);
        }
        else
        {
            // Fallback for any other collider type
            Bounds bounds = collider.bounds;
            Gizmos.DrawWireCube(bounds.center, bounds.size);
        }
    }

    // Helper methods for drawing debug shapes
    private void DrawWireCircle(Vector3 center, float radius, int segments)
    {
        float angleStep = 360f / segments;
        Vector3 prevPoint = center + new Vector3(radius, 0, 0);

        for (int i = 1; i <= segments; i++)
        {
            float angle = i * angleStep * Mathf.Deg2Rad;
            Vector3 point = center + new Vector3(Mathf.Cos(angle) * radius, Mathf.Sin(angle) * radius, 0);
            Gizmos.DrawLine(prevPoint, point);
            prevPoint = point;
        }
    }

    private void DrawWirePolygon(PolygonCollider2D polygon)
    {
        for (int i = 0; i < polygon.points.Length; i++)
        {
            Vector2 currentPoint = polygon.transform.TransformPoint(polygon.points[i]);
            Vector2 nextPoint = polygon.transform.TransformPoint(
                polygon.points[(i + 1) % polygon.points.Length]);

            Gizmos.DrawLine(currentPoint, nextPoint);
        }
    }
}