using System.Collections.Generic;
using UnityEngine;

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
        // Handle starting drag
        if (Input.GetMouseButtonDown(0))
        {
            RaycastHit2D hit = Physics2D.Raycast(GetMouseWorldPosition(), Vector2.zero);
            if (hit.collider != null)
            {
                DraggableItem draggable = hit.collider.GetComponent<DraggableItem>();
                if (draggable != null && draggable.isDraggable)
                {
                    StartDragging(draggable, hit.point);
                }
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

        // Calculate offset to maintain relative position of cursor to object
        dragOffset = currentDraggable.transform.position - new Vector3(hitPoint.x, hitPoint.y, currentDraggable.transform.position.z);
        dragOffset.z = 0; // Ensure offset doesn't affect z
    }

    private void CheckPlacement()
    {
        // Get the collider for bounds checking
        Collider2D collider = currentDraggable.GetComponent<Collider2D>();
        if (collider == null)
        {
            Debug.LogWarning("Draggable item has no collider for bounds checking");
            return;
        }

        // Check if the object is fully contained in any placeable area
        bool insideArea = false;
        foreach (PlaceableArea area in placeableAreas)
        {
            if (area.ContainsCollider(collider))
            {
                insideArea = true;
                break;
            }
        }

        // Check if overlapping with any other draggable items
        bool isOverlapping = CheckForOverlap(collider);

        // Only allow placement if inside area AND not overlapping other items
        canPlace = insideArea && !isOverlapping;

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
        }

        // No overlap with other draggable items
        return false;
    }

    private void DropItem()
    {
        if (canPlace)
        {
            // Place at current position, but reset Z to 0
            Vector3 finalPosition = currentDraggable.transform.position;
            finalPosition.z = 0;
            currentDraggable.transform.position = finalPosition;
            currentDraggable.lastValidPosition = finalPosition;
            currentDraggable.isCurrentlyPlaced = true;
        }
        else
        {
            // Return to last valid position
            currentDraggable.transform.position = currentDraggable.lastValidPosition;
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