using System.Collections.Generic;
using UnityEngine;

public class PlaceableArea : MonoBehaviour
{
    [SerializeField] private List<Vector2> areaVertices = new List<Vector2>();
    [SerializeField] private Color debugColor = new Color(0, 1, 0, 0.3f);
    [SerializeField] private bool showDebug = true;
    
    // For editor functions
    public List<Vector2> AreaVertices => areaVertices;
    
    // Check if a point is inside this placeable area
    public bool ContainsPoint(Vector2 worldPoint)
    {
        // Convert world point to local space
        Vector2 localPoint = transform.InverseTransformPoint(worldPoint);
        return IsPointInPolygon(localPoint, areaVertices);
    }
    
    // Check if a collider is fully contained in this area
    public bool ContainsCollider(Collider2D collider)
    {
        // Handle different collider types
        if (collider is CircleCollider2D circleCollider)
        {
            return ContainsCircleCollider(circleCollider);
        }
        else if (collider is BoxCollider2D boxCollider)
        {
            return ContainsBoxCollider(boxCollider);
        }
        else if (collider is PolygonCollider2D polygonCollider)
        {
            return ContainsPolygonCollider(polygonCollider);
        }
        else
        {
            // For any other collider type, fall back to bounds check
            return ContainsBounds(collider.bounds);
        }
    }
    
    // Handle Circle Collider specifically
    private bool ContainsCircleCollider(CircleCollider2D circleCollider)
    {
        // Get the center and radius in world space
        Vector2 center = circleCollider.transform.TransformPoint(circleCollider.offset);
        
        // Get the world space radius by accounting for scaling
        float worldRadius = circleCollider.radius;
        Vector3 scale = circleCollider.transform.lossyScale;
        worldRadius *= Mathf.Max(Mathf.Abs(scale.x), Mathf.Abs(scale.y));
        
        // First check if the center is inside
        if (!ContainsPoint(center))
            return false;
        
        // Then check points around the perimeter of the circle
        int numPoints = 8; // Check 8 points around the circle
        for (int i = 0; i < numPoints; i++)
        {
            float angle = (2 * Mathf.PI * i) / numPoints;
            Vector2 pointOnCircle = center + new Vector2(
                Mathf.Cos(angle) * worldRadius,
                Mathf.Sin(angle) * worldRadius
            );
            
            if (!ContainsPoint(pointOnCircle))
                return false;
        }
        
        return true;
    }
    
    // Handle Box Collider specifically
    private bool ContainsBoxCollider(BoxCollider2D boxCollider)
    {
        // Get local corners of the box
        Vector2 halfSize = boxCollider.size / 2;
        Vector2 offset = boxCollider.offset;
        
        Vector2[] corners = new Vector2[4];
        corners[0] = offset + new Vector2(-halfSize.x, -halfSize.y); // Bottom-left
        corners[1] = offset + new Vector2(halfSize.x, -halfSize.y);  // Bottom-right
        corners[2] = offset + new Vector2(halfSize.x, halfSize.y);   // Top-right
        corners[3] = offset + new Vector2(-halfSize.x, halfSize.y);  // Top-left
        
        // Transform corners to world space
        for (int i = 0; i < 4; i++)
        {
            corners[i] = boxCollider.transform.TransformPoint(corners[i]);
            
            if (!ContainsPoint(corners[i]))
                return false;
        }
        
        return true;
    }
    
    // Handle Polygon Collider specifically
    private bool ContainsPolygonCollider(PolygonCollider2D polygonCollider)
    {
        // Check all vertices of the polygon
        for (int i = 0; i < polygonCollider.points.Length; i++)
        {
            Vector2 worldPoint = polygonCollider.transform.TransformPoint(polygonCollider.points[i]);
            
            if (!ContainsPoint(worldPoint))
                return false;
        }
        
        return true;
    }
    
    // Fallback method for other collider types
    private bool ContainsBounds(Bounds bounds)
    {
        // Get the four corners of the bounding box in world space
        Vector2[] corners = new Vector2[4];
        corners[0] = new Vector2(bounds.min.x, bounds.min.y); // Bottom-left
        corners[1] = new Vector2(bounds.max.x, bounds.min.y); // Bottom-right
        corners[2] = new Vector2(bounds.max.x, bounds.max.y); // Top-right
        corners[3] = new Vector2(bounds.min.x, bounds.max.y); // Top-left
        
        // Check if all corners are inside the polygon
        foreach (Vector2 corner in corners)
        {
            if (!ContainsPoint(corner))
            {
                return false;
            }
        }
        
        return true;
    }
    
    // Point-in-polygon algorithm (ray casting)
    private bool IsPointInPolygon(Vector2 point, List<Vector2> polygon)
    {
        if (polygon.Count < 3) return false;
        
        bool inside = false;
        for (int i = 0, j = polygon.Count - 1; i < polygon.Count; j = i++)
        {
            bool intersect = ((polygon[i].y > point.y) != (polygon[j].y > point.y)) &&
                (point.x < (polygon[j].x - polygon[i].x) * (point.y - polygon[i].y) / 
                (polygon[j].y - polygon[i].y) + polygon[i].x);
                
            if (intersect) inside = !inside;
        }
        
        return inside;
    }
    
    // Convert local vertices to world space points
    public List<Vector2> GetWorldVertices()
    {
        List<Vector2> worldPoints = new List<Vector2>();
        foreach (Vector2 point in areaVertices)
        {
            worldPoints.Add((Vector2)transform.TransformPoint(point));
        }
        return worldPoints;
    }
    
    // For debugging
    private void OnDrawGizmos()
    {
        if (!showDebug) return;
        
        // Draw the placeable area
        Gizmos.color = debugColor;
        List<Vector2> worldVertices = GetWorldVertices();
        
        if (worldVertices.Count > 1)
        {
            for (int i = 0; i < worldVertices.Count; i++)
            {
                Vector2 current = worldVertices[i];
                Vector2 next = worldVertices[(i + 1) % worldVertices.Count];
                Gizmos.DrawLine(current, next);
            }
        }
    }
}