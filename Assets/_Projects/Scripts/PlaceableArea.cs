using System.Collections.Generic;
using UnityEngine;

public class PlaceableArea : MonoBehaviour
{
    [Header("Area Identity")]
    [SerializeField] private string areaIdentifier = "Floor"; // Used to match with DecorationItemData

    [Header("Area Definition")]
    [SerializeField] private List<Vector2> areaVertices = new List<Vector2>();
    [SerializeField] private Color debugColor = new Color(0, 1, 0, 0.3f);
    [SerializeField] private bool showDebug = true;

    [Header("Scoring Zones")]
    [SerializeField] private List<ScoringZone> scoringZones = new List<ScoringZone>();
    [SerializeField] private bool showZoneDebug = true;

    // Properties
    public string AreaIdentifier => areaIdentifier;
    public List<Vector2> AreaVertices => areaVertices;
    public List<ScoringZone> ScoringZones => scoringZones;

    // Check if a point is inside this placeable area
    public bool ContainsPoint(Vector2 worldPoint)
    {
        Vector2 localPoint = transform.InverseTransformPoint(worldPoint);
        return IsPointInPolygon(localPoint, areaVertices);
    }

    // Check if a collider is fully contained in this area
    public bool ContainsCollider(Collider2D collider)
    {
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
            return ContainsBounds(collider.bounds);
        }
    }

    // Get the best scoring zone for a given world position
    public ScoringZone GetBestScoringZone(Vector2 worldPoint)
    {
        Vector2 localPoint = transform.InverseTransformPoint(worldPoint);

        ScoringZone bestZone = null;
        int highestPoints = int.MinValue;

        foreach (ScoringZone zone in scoringZones)
        {
            if (IsPointInPolygon(localPoint, zone.zoneVertices) && zone.pointValue > highestPoints)
            {
                highestPoints = zone.pointValue;
                bestZone = zone;
            }
        }

        return bestZone;
    }

    // Get scoring for an item placed at a specific position
    public PlacementScore CalculatePlacementScore(DecorationItemData itemData, Vector2 worldPosition)
    {
        PlacementScore score = new PlacementScore();
        score.worldPosition = worldPosition;
        score.placedInValidArea = ContainsPoint(worldPosition);

        if (!score.placedInValidArea)
        {
            score.pointsAwarded = itemData.wrongPlacementPenalty;
            score.scoringReason = "Placed outside valid area";
            return score;
        }

        // NEW: Check if this item is restricted from this area
        if (itemData.restrictedAreas.Contains(areaIdentifier))
        {
            score.pointsAwarded = itemData.wrongPlacementPenalty;
            score.scoringReason = $"Item cannot be placed in {areaIdentifier} area";
            score.placedInValidArea = false; // Mark as invalid placement
            return score;
        }

        // Find matching preference for this area
        PlacementPreference matchingPreference = null;
        foreach (var preference in itemData.placementPreferences)
        {
            if (preference.areaIdentifier == areaIdentifier)
            {
                matchingPreference = preference;
                break;
            }
        }

        if (matchingPreference == null)
        {
            // No specific preference for this area, use penalty instead of base points
            score.pointsAwarded = itemData.wrongPlacementPenalty;
            score.scoringReason = $"Item doesn't prefer this area ({areaIdentifier}) - penalty applied";
            return score;
        }

        // Rest of the method remains the same...
        // Check for zone-specific scoring
        Vector2 localPoint = transform.InverseTransformPoint(worldPosition);
        PlacementZone bestZone = null;
        int highestZonePoints = int.MinValue;

        foreach (var zone in matchingPreference.zones)
        {
            if (IsPointInPolygon(localPoint, zone.zoneVertices) && zone.pointValue > highestZonePoints)
            {
                highestZonePoints = zone.pointValue;
                bestZone = zone;
            }
        }

        if (bestZone != null)
        {
            score.pointsAwarded = bestZone.pointValue;
            score.scoringReason = $"Placed in {bestZone.zoneName} zone";
            score.zoneName = bestZone.zoneName;
        }
        else
        {
            // In preferred area but not in any specific zone
            score.pointsAwarded = matchingPreference.defaultAreaPoints;
            score.scoringReason = $"Placed in preferred area ({areaIdentifier})";
        }

        return score;
    }

    #region Collider Containment Methods
    private bool ContainsCircleCollider(CircleCollider2D circleCollider)
    {
        Vector2 center = circleCollider.transform.TransformPoint(circleCollider.offset);
        float worldRadius = circleCollider.radius;
        Vector3 scale = circleCollider.transform.lossyScale;
        worldRadius *= Mathf.Max(Mathf.Abs(scale.x), Mathf.Abs(scale.y));

        if (!ContainsPoint(center))
            return false;

        int numPoints = 8;
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

    private bool ContainsBoxCollider(BoxCollider2D boxCollider)
    {
        Vector2 halfSize = boxCollider.size / 2;
        Vector2 offset = boxCollider.offset;

        Vector2[] corners = new Vector2[4];
        corners[0] = offset + new Vector2(-halfSize.x, -halfSize.y);
        corners[1] = offset + new Vector2(halfSize.x, -halfSize.y);
        corners[2] = offset + new Vector2(halfSize.x, halfSize.y);
        corners[3] = offset + new Vector2(-halfSize.x, halfSize.y);

        for (int i = 0; i < 4; i++)
        {
            corners[i] = boxCollider.transform.TransformPoint(corners[i]);

            if (!ContainsPoint(corners[i]))
                return false;
        }

        return true;
    }

    private bool ContainsPolygonCollider(PolygonCollider2D polygonCollider)
    {
        for (int i = 0; i < polygonCollider.points.Length; i++)
        {
            Vector2 worldPoint = polygonCollider.transform.TransformPoint(polygonCollider.points[i]);

            if (!ContainsPoint(worldPoint))
                return false;
        }

        return true;
    }

    private bool ContainsBounds(Bounds bounds)
    {
        Vector2[] corners = new Vector2[4];
        corners[0] = new Vector2(bounds.min.x, bounds.min.y);
        corners[1] = new Vector2(bounds.max.x, bounds.min.y);
        corners[2] = new Vector2(bounds.max.x, bounds.max.y);
        corners[3] = new Vector2(bounds.min.x, bounds.max.y);

        foreach (Vector2 corner in corners)
        {
            if (!ContainsPoint(corner))
            {
                return false;
            }
        }

        return true;
    }
    #endregion

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

        // Draw the main placeable area
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

        // Draw scoring zones if enabled
        if (showZoneDebug)
        {
            foreach (ScoringZone zone in scoringZones)
            {
                Gizmos.color = zone.debugColor;

                if (zone.zoneVertices.Count > 1)
                {
                    for (int i = 0; i < zone.zoneVertices.Count; i++)
                    {
                        Vector2 current = transform.TransformPoint(zone.zoneVertices[i]);
                        Vector2 next = transform.TransformPoint(zone.zoneVertices[(i + 1) % zone.zoneVertices.Count]);
                        Gizmos.DrawLine(current, next);
                    }
                }
            }
        }
    }
}

[System.Serializable]
public class ScoringZone
{
    [Header("Zone Info")]
    public string zoneName;

    [Header("Zone Shape")]
    [Tooltip("Local coordinates relative to the PlaceableArea transform")]
    public List<Vector2> zoneVertices = new List<Vector2>();

    [Header("Scoring")]
    public int pointValue = 20;

    [Header("Visual")]
    public Color debugColor = Color.green;
}

[System.Serializable]
public class PlacementScore
{
    public Vector2 worldPosition;
    public bool placedInValidArea;
    public int pointsAwarded;
    public string scoringReason;
    public string zoneName;
}