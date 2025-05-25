using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "New Decoration Item", menuName = "Hermit Home/Decoration Item")]
public class DecorationItemData : ScriptableObject
{
    [Header("Basic Info")]
    public string itemName;
    public Sprite itemSprite;
    public string description;

    [Header("Scoring Preferences")]
    public List<PlacementPreference> placementPreferences = new List<PlacementPreference>();

    [Header("General Scoring")]
    [Tooltip("Points awarded when placed in any valid placeable area")]
    public int basePoints = 10;

    [Tooltip("Points deducted when placed in wrong area")]
    public int wrongPlacementPenalty = -5;

    [Header("Placement Restrictions")]
    [Tooltip("Areas where this item cannot be placed")]
    public List<string> restrictedAreas = new List<string>();
}

[System.Serializable]
public class PlacementPreference
{
    [Header("Area Identification")]
    [Tooltip("Name or tag to identify the placeable area")]
    public string areaIdentifier;

    [Header("Zone-based Scoring")]
    [Tooltip("Different zones within the area with different point values")]
    public List<PlacementZone> zones = new List<PlacementZone>();

    [Header("Fallback Scoring")]
    [Tooltip("Points if placed in this area but not in any specific zone")]
    public int defaultAreaPoints = 15;
}

[System.Serializable]
public class PlacementZone
{
    [Header("Zone Definition")]
    public string zoneName;

    [Tooltip("Local coordinates defining this zone (relative to the placeable area)")]
    public List<Vector2> zoneVertices = new List<Vector2>();

    [Header("Scoring")]
    public int pointValue = 20;

    [Header("Visual Debug")]
    public Color debugColor = Color.green;
}