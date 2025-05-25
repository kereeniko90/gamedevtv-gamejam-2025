#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;

[CustomEditor(typeof(DecorationItemData))]
public class DecorationItemDataEditor : Editor
{
    private DecorationItemData itemData;
    private bool showZonePicker = false;
    private PlaceableArea selectedArea;
    private int editingPreferenceIndex = -1;
    private Vector2 scrollPosition;

    // Zone picking state
    private bool pickingZones = false;
    private List<int> selectedZoneIndices = new List<int>();

    private void OnEnable()
    {
        itemData = (DecorationItemData)target;
    }

    public override void OnInspectorGUI()
    {
        // Draw default inspector first
        DrawDefaultInspector();

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Visual Zone Setup", EditorStyles.boldLabel);

        // Show placement preferences with enhanced controls
        if (itemData.placementPreferences.Count > 0)
        {
            EditorGUILayout.LabelField("Current Placement Preferences:", EditorStyles.boldLabel);

            for (int i = 0; i < itemData.placementPreferences.Count; i++)
            {
                DrawPlacementPreference(i);
            }
        }

        EditorGUILayout.Space();

        // Zone picker interface
        EditorGUILayout.BeginVertical("box");
        EditorGUILayout.LabelField("Add Zones from Scene", EditorStyles.boldLabel);

        // Area selection
        EditorGUI.BeginChangeCheck();
        selectedArea = (PlaceableArea)EditorGUILayout.ObjectField("Select PlaceableArea", selectedArea, typeof(PlaceableArea), true);
        if (EditorGUI.EndChangeCheck())
        {
            selectedZoneIndices.Clear();
            pickingZones = false;
        }

        if (selectedArea != null)
        {
            EditorGUILayout.LabelField($"Area: {selectedArea.AreaIdentifier}", EditorStyles.miniLabel);
            EditorGUILayout.LabelField($"Zones Available: {selectedArea.ScoringZones.Count}", EditorStyles.miniLabel);

            if (selectedArea.ScoringZones.Count > 0)
            {
                DrawZoneSelectionInterface();
            }
            else
            {
                EditorGUILayout.HelpBox("This PlaceableArea has no scoring zones. Add some zones in the PlaceableArea editor first.", MessageType.Info);
            }
        }
        else
        {
            EditorGUILayout.HelpBox("Select a PlaceableArea from your scene to pick zones from it.", MessageType.Info);
        }

        EditorGUILayout.EndVertical();

        // Zone templates
        EditorGUILayout.Space();
        DrawZoneTemplates();
        DrawRestrictedAreasSection();

        // Apply changes
        if (GUI.changed)
        {
            EditorUtility.SetDirty(itemData);
        }
    }

    private void DrawPlacementPreference(int index)
    {
        PlacementPreference preference = itemData.placementPreferences[index];

        EditorGUILayout.BeginVertical("box");

        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField($"Area: {preference.areaIdentifier}", EditorStyles.boldLabel);

        if (GUILayout.Button("Edit", GUILayout.Width(50)))
        {
            editingPreferenceIndex = (editingPreferenceIndex == index) ? -1 : index;
        }

        if (GUILayout.Button("X", GUILayout.Width(25)))
        {
            Undo.RecordObject(itemData, "Remove Placement Preference");
            itemData.placementPreferences.RemoveAt(index);
            EditorUtility.SetDirty(itemData);
            return;
        }

        EditorGUILayout.EndHorizontal();

        EditorGUILayout.LabelField($"Default Points: {preference.defaultAreaPoints}", EditorStyles.miniLabel);
        EditorGUILayout.LabelField($"Zones: {preference.zones.Count}", EditorStyles.miniLabel);

        // Show zone details when editing
        if (editingPreferenceIndex == index)
        {
            EditorGUI.indentLevel++;

            preference.defaultAreaPoints = EditorGUILayout.IntField("Default Area Points", preference.defaultAreaPoints);

            EditorGUILayout.LabelField("Zones:", EditorStyles.boldLabel);

            for (int zoneIndex = 0; zoneIndex < preference.zones.Count; zoneIndex++)
            {
                PlacementZone zone = preference.zones[zoneIndex];

                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField($"• {zone.zoneName} ({zone.pointValue} pts, {zone.zoneVertices.Count} vertices)", EditorStyles.miniLabel);

                if (GUILayout.Button("X", GUILayout.Width(25)))
                {
                    Undo.RecordObject(itemData, "Remove Zone");
                    preference.zones.RemoveAt(zoneIndex);
                    EditorUtility.SetDirty(itemData);
                    break;
                }

                EditorGUILayout.EndHorizontal();
            }

            EditorGUI.indentLevel--;
        }

        EditorGUILayout.EndVertical();
    }

    private void DrawZoneSelectionInterface()
    {
        EditorGUILayout.Space();

        // Zone selection list
        EditorGUILayout.LabelField("Available Zones:", EditorStyles.boldLabel);

        scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition, GUILayout.MaxHeight(150));

        for (int i = 0; i < selectedArea.ScoringZones.Count; i++)
        {
            ScoringZone zone = selectedArea.ScoringZones[i];

            EditorGUILayout.BeginHorizontal();

            bool wasSelected = selectedZoneIndices.Contains(i);
            bool isSelected = EditorGUILayout.Toggle(wasSelected, GUILayout.Width(20));

            if (isSelected != wasSelected)
            {
                if (isSelected)
                {
                    selectedZoneIndices.Add(i);
                }
                else
                {
                    selectedZoneIndices.Remove(i);
                }
            }

            // Zone info
            EditorGUILayout.LabelField($"{zone.zoneName}", EditorStyles.miniLabel);
            EditorGUILayout.LabelField($"{zone.pointValue} pts", EditorStyles.miniLabel, GUILayout.Width(60));
            EditorGUILayout.LabelField($"{zone.zoneVertices.Count} vertices", EditorStyles.miniLabel, GUILayout.Width(80));

            EditorGUILayout.EndHorizontal();
        }

        EditorGUILayout.EndScrollView();

        EditorGUILayout.Space();

        // Action buttons
        EditorGUILayout.BeginHorizontal();

        if (GUILayout.Button("Select All"))
        {
            selectedZoneIndices.Clear();
            for (int i = 0; i < selectedArea.ScoringZones.Count; i++)
            {
                selectedZoneIndices.Add(i);
            }
        }

        if (GUILayout.Button("Clear Selection"))
        {
            selectedZoneIndices.Clear();
        }

        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Space();

        // Add zones section
        if (selectedZoneIndices.Count > 0)
        {
            EditorGUILayout.LabelField($"Selected {selectedZoneIndices.Count} zone(s)", EditorStyles.boldLabel);

            int defaultAreaPoints = EditorGUILayout.IntField("Default Area Points", 15);

            if (GUILayout.Button($"Add Zones to {selectedArea.AreaIdentifier}"))
            {
                AddSelectedZonesToItem(defaultAreaPoints);
            }
        }

        // Highlight selected zones in scene
        if (selectedZoneIndices.Count > 0 && Event.current.type == EventType.Repaint)
        {
            SceneView.RepaintAll();
        }
    }

    private void AddSelectedZonesToItem(int defaultAreaPoints)
    {
        if (selectedArea == null || selectedZoneIndices.Count == 0) return;

        Undo.RecordObject(itemData, "Add Zones from Scene");

        // Find or create placement preference for this area
        PlacementPreference preference = itemData.placementPreferences
            .FirstOrDefault(p => p.areaIdentifier == selectedArea.AreaIdentifier);

        if (preference == null)
        {
            preference = new PlacementPreference();
            preference.areaIdentifier = selectedArea.AreaIdentifier;
            preference.defaultAreaPoints = defaultAreaPoints;
            preference.zones = new List<PlacementZone>();
            itemData.placementPreferences.Add(preference);
        }
        else
        {
            preference.defaultAreaPoints = defaultAreaPoints;
        }

        // Add selected zones
        foreach (int zoneIndex in selectedZoneIndices)
        {
            if (zoneIndex >= 0 && zoneIndex < selectedArea.ScoringZones.Count)
            {
                ScoringZone sourceZone = selectedArea.ScoringZones[zoneIndex];

                // Check if zone already exists
                bool zoneExists = preference.zones.Any(z => z.zoneName == sourceZone.zoneName);

                if (!zoneExists)
                {
                    PlacementZone newZone = new PlacementZone();
                    newZone.zoneName = sourceZone.zoneName;
                    newZone.pointValue = sourceZone.pointValue;
                    newZone.debugColor = sourceZone.debugColor;
                    newZone.zoneVertices = new List<Vector2>(sourceZone.zoneVertices);

                    preference.zones.Add(newZone);
                }
            }
        }

        // Clear selection
        selectedZoneIndices.Clear();

        EditorUtility.SetDirty(itemData);

        Debug.Log($"Added {selectedZoneIndices.Count} zones from {selectedArea.AreaIdentifier} to {itemData.itemName}");
    }

    private void DrawZoneTemplates()
    {
        EditorGUILayout.BeginVertical("box");
        EditorGUILayout.LabelField("Quick Zone Templates", EditorStyles.boldLabel);

        EditorGUILayout.BeginHorizontal();

        if (GUILayout.Button("Center Zone"))
        {
            CreateTemplateZone("Center", 25, CreateCenterZone());
        }

        if (GUILayout.Button("Corner Zone"))
        {
            CreateTemplateZone("Corner", 20, CreateCornerZone());
        }

        if (GUILayout.Button("Edge Zone"))
        {
            CreateTemplateZone("Edge", 15, CreateEdgeZone());
        }

        EditorGUILayout.EndHorizontal();

        EditorGUILayout.HelpBox("These create standard zone shapes that you can modify afterwards.", MessageType.Info);

        EditorGUILayout.EndVertical();
    }

    private void CreateTemplateZone(string zoneName, int pointValue, List<Vector2> vertices)
    {
        Undo.RecordObject(itemData, $"Add {zoneName} Template");

        // Create or find a generic preference
        PlacementPreference preference = itemData.placementPreferences
            .FirstOrDefault(p => p.areaIdentifier == "Generic");

        if (preference == null)
        {
            preference = new PlacementPreference();
            preference.areaIdentifier = "Generic";
            preference.defaultAreaPoints = 10;
            preference.zones = new List<PlacementZone>();
            itemData.placementPreferences.Add(preference);
        }

        PlacementZone newZone = new PlacementZone();
        newZone.zoneName = zoneName;
        newZone.pointValue = pointValue;
        newZone.debugColor = Color.cyan;
        newZone.zoneVertices = vertices;

        preference.zones.Add(newZone);

        EditorUtility.SetDirty(itemData);
    }

    private void DrawRestrictedAreasSection()
    {
        EditorGUILayout.Space();
        EditorGUILayout.BeginVertical("box");
        EditorGUILayout.LabelField("Placement Restrictions", EditorStyles.boldLabel);

        // Show current restricted areas
        if (itemData.restrictedAreas.Count > 0)
        {
            EditorGUILayout.LabelField("Restricted Areas:", EditorStyles.boldLabel);

            for (int i = itemData.restrictedAreas.Count - 1; i >= 0; i--)
            {
                EditorGUILayout.BeginHorizontal();

                EditorGUI.BeginChangeCheck();
                string areaName = EditorGUILayout.TextField(itemData.restrictedAreas[i]);
                if (EditorGUI.EndChangeCheck())
                {
                    Undo.RecordObject(itemData, "Edit Restricted Area");
                    itemData.restrictedAreas[i] = areaName;
                    EditorUtility.SetDirty(itemData);
                }

                if (GUILayout.Button("X", GUILayout.Width(25)))
                {
                    Undo.RecordObject(itemData, "Remove Restricted Area");
                    itemData.restrictedAreas.RemoveAt(i);
                    EditorUtility.SetDirty(itemData);
                }

                EditorGUILayout.EndHorizontal();
            }
        }
        else
        {
            EditorGUILayout.LabelField("No area restrictions", EditorStyles.miniLabel);
        }

        EditorGUILayout.Space();       

        // Add custom area
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("Custom:", GUILayout.Width(60));

        string customAreaName = EditorGUILayout.TextField("");

        if (GUILayout.Button("Add", GUILayout.Width(50)) && !string.IsNullOrEmpty(customAreaName))
        {
            AddRestrictedArea(customAreaName);
        }

        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Space();
        EditorGUILayout.HelpBox("Items cannot be placed in restricted areas. Use area identifiers that match your PlaceableArea components.", MessageType.Info);

        EditorGUILayout.EndVertical();
    }

    private void AddRestrictedArea(string areaName)
    {
        if (!itemData.restrictedAreas.Contains(areaName))
        {
            Undo.RecordObject(itemData, "Add Restricted Area");
            itemData.restrictedAreas.Add(areaName);
            EditorUtility.SetDirty(itemData);
        }
    }

    private List<Vector2> CreateCenterZone()
    {
        return new List<Vector2>
        {
            new Vector2(-0.5f, -0.5f),
            new Vector2(0.5f, -0.5f),
            new Vector2(0.5f, 0.5f),
            new Vector2(-0.5f, 0.5f)
        };
    }

    private List<Vector2> CreateCornerZone()
    {
        return new List<Vector2>
        {
            new Vector2(-1f, -1f),
            new Vector2(-0.5f, -1f),
            new Vector2(-1f, -0.5f)
        };
    }

    private List<Vector2> CreateEdgeZone()
    {
        return new List<Vector2>
        {
            new Vector2(-1f, 0.8f),
            new Vector2(1f, 0.8f),
            new Vector2(1f, 1f),
            new Vector2(-1f, 1f)
        };
    }

    // Highlight selected zones in scene view
    private void OnSceneGUI()
    {
        if (selectedArea == null || selectedZoneIndices.Count == 0) return;

        foreach (int zoneIndex in selectedZoneIndices)
        {
            if (zoneIndex >= 0 && zoneIndex < selectedArea.ScoringZones.Count)
            {
                ScoringZone zone = selectedArea.ScoringZones[zoneIndex];

                if (zone.zoneVertices.Count >= 3)
                {
                    // Convert to world space
                    Vector3[] worldVertices = new Vector3[zone.zoneVertices.Count];
                    for (int i = 0; i < zone.zoneVertices.Count; i++)
                    {
                        worldVertices[i] = selectedArea.transform.TransformPoint(zone.zoneVertices[i]);
                    }

                    // Draw highlighted zone
                    Handles.color = new Color(1f, 1f, 0f, 0.6f); // Bright yellow highlight
                    Handles.DrawAAConvexPolygon(worldVertices);

                    // Draw zone name
                    Vector3 center = Vector3.zero;
                    foreach (Vector3 vertex in worldVertices)
                    {
                        center += vertex;
                    }
                    center /= worldVertices.Length;

                    Handles.color = Color.white;
                    Handles.Label(center, $"{zone.zoneName}\n{zone.pointValue} pts");
                }
            }
        }
    }
}
#endif