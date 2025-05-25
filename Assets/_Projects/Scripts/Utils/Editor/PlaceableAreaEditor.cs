#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

[CustomEditor(typeof(PlaceableArea))]
public class PlaceableAreaEditor : Editor
{
    private PlaceableArea area;
    private int selectedVertex = -1;
    private float handleSize = 0.15f;
    private bool addVertexMode = false;
    
    // Zone editing variables
    private int selectedZone = -1;
    private int selectedZoneVertex = -1;
    private bool editingZones = false;
    private bool addZoneVertexMode = false;
    
    // For vertex dragging
    private bool isDraggingVertex = false;
    private Vector2 dragStartPosition;
    
    private void OnEnable()
    {
        area = (PlaceableArea)target;
        Tools.hidden = false;
    }
    
    private void OnDisable()
    {
        Tools.hidden = false;
    }
    
    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();
        
        EditorGUILayout.Space();
        
        // Mode selection
        EditorGUILayout.LabelField("Editing Mode", EditorStyles.boldLabel);
        EditorGUILayout.BeginHorizontal();
        
        GUI.backgroundColor = !editingZones ? Color.green : Color.white;
        if (GUILayout.Button("Edit Area"))
        {
            editingZones = false;
            addVertexMode = false;
            addZoneVertexMode = false;
            selectedVertex = -1;
            selectedZone = -1;
            selectedZoneVertex = -1;
            Tools.hidden = false;
        }
        
        GUI.backgroundColor = editingZones ? Color.green : Color.white;
        if (GUILayout.Button("Edit Zones"))
        {
            editingZones = true;
            addVertexMode = false;
            addZoneVertexMode = false;
            selectedVertex = -1;
            Tools.hidden = false;
        }
        GUI.backgroundColor = Color.white;
        
        EditorGUILayout.EndHorizontal();
        EditorGUILayout.Space();
        
        if (!editingZones)
        {
            DrawAreaEditingGUI();
        }
        else
        {
            DrawZoneEditingGUI();
        }
    }
    
    private void DrawAreaEditingGUI()
    {
        EditorGUILayout.LabelField("Area Vertex Editing", EditorStyles.boldLabel);
        
        handleSize = EditorGUILayout.Slider("Handle Size", handleSize, 0.05f, 0.5f);
        
        EditorGUILayout.Space();
        
        GUI.backgroundColor = addVertexMode ? Color.green : Color.white;
        if (GUILayout.Button(addVertexMode ? "✓ Click in Scene to Add Vertices" : "Enter Add Vertex Mode"))
        {
            addVertexMode = !addVertexMode;
            Tools.hidden = addVertexMode;
        }
        GUI.backgroundColor = Color.white;
        
        if (!addVertexMode && GUILayout.Button("Add Vertex at Center"))
        {
            Undo.RecordObject(area, "Add Vertex");
            AddVertexAtCenter();
            EditorUtility.SetDirty(area);
        }
        
        EditorGUILayout.Space();
        
        if (selectedVertex >= 0 && selectedVertex < area.AreaVertices.Count)
        {
            EditorGUILayout.LabelField($"Selected Vertex: {selectedVertex}", EditorStyles.boldLabel);
            
            EditorGUI.BeginChangeCheck();
            Vector2 newPos = EditorGUILayout.Vector2Field("Position", area.AreaVertices[selectedVertex]);
            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(area, "Move Vertex");
                area.AreaVertices[selectedVertex] = newPos;
                EditorUtility.SetDirty(area);
            }
            
            if (GUILayout.Button("Delete Selected Vertex"))
            {
                Undo.RecordObject(area, "Remove Vertex");
                area.AreaVertices.RemoveAt(selectedVertex);
                selectedVertex = -1;
                EditorUtility.SetDirty(area);
            }
        }
        
        // Preset shapes
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Preset Shapes", EditorStyles.boldLabel);
        EditorGUILayout.BeginHorizontal();
        
        if (GUILayout.Button("Square"))
        {
            Undo.RecordObject(area, "Create Square Shape");
            CreateSquareShape();
            EditorUtility.SetDirty(area);
        }
        
        if (GUILayout.Button("Diamond"))
        {
            Undo.RecordObject(area, "Create Diamond Shape");
            CreateDiamondShape();
            EditorUtility.SetDirty(area);
        }
        
        if (GUILayout.Button("Circle (8)"))
        {
            Undo.RecordObject(area, "Create Circle Shape");
            CreateCircleShape(8);
            EditorUtility.SetDirty(area);
        }
        
        EditorGUILayout.EndHorizontal();
    }
    
    private void DrawZoneEditingGUI()
    {
        EditorGUILayout.LabelField("Scoring Zone Editing", EditorStyles.boldLabel);
        
        handleSize = EditorGUILayout.Slider("Handle Size", handleSize, 0.05f, 0.5f);
        
        EditorGUILayout.Space();
        
        // Zone selection
        EditorGUILayout.LabelField("Zones", EditorStyles.boldLabel);
        
        if (GUILayout.Button("Add New Zone"))
        {
            Undo.RecordObject(area, "Add Scoring Zone");
            ScoringZone newZone = new ScoringZone();
            newZone.zoneName = $"Zone {area.ScoringZones.Count}";
            newZone.pointValue = 20;
            newZone.debugColor = Color.blue;
            area.ScoringZones.Add(newZone);
            selectedZone = area.ScoringZones.Count - 1;
            selectedZoneVertex = -1;
            EditorUtility.SetDirty(area);
        }
        
        // Display zones
        for (int i = 0; i < area.ScoringZones.Count; i++)
        {
            EditorGUILayout.BeginHorizontal();
            
            GUI.backgroundColor = (i == selectedZone) ? Color.green : Color.white;
            if (GUILayout.Button($"{area.ScoringZones[i].zoneName} ({area.ScoringZones[i].zoneVertices.Count} vertices)"))
            {
                selectedZone = i;
                selectedZoneVertex = -1;
                addZoneVertexMode = false;
            }
            GUI.backgroundColor = Color.white;
            
            if (GUILayout.Button("X", GUILayout.Width(25)))
            {
                Undo.RecordObject(area, "Remove Scoring Zone");
                area.ScoringZones.RemoveAt(i);
                if (selectedZone >= i) selectedZone = -1;
                selectedZoneVertex = -1;
                EditorUtility.SetDirty(area);
                break;
            }
            
            EditorGUILayout.EndHorizontal();
        }
        
        EditorGUILayout.Space();
        
        // Selected zone editing
        if (selectedZone >= 0 && selectedZone < area.ScoringZones.Count)
        {
            ScoringZone zone = area.ScoringZones[selectedZone];
            
            EditorGUILayout.LabelField($"Editing: {zone.zoneName}", EditorStyles.boldLabel);
            
            EditorGUI.BeginChangeCheck();
            
            zone.zoneName = EditorGUILayout.TextField("Zone Name", zone.zoneName);
            zone.pointValue = EditorGUILayout.IntField("Point Value", zone.pointValue);
            zone.debugColor = EditorGUILayout.ColorField("Debug Color", zone.debugColor);
            
            if (EditorGUI.EndChangeCheck())
            {
                EditorUtility.SetDirty(area);
            }
            
            EditorGUILayout.Space();
            
            // Zone vertex editing
            GUI.backgroundColor = addZoneVertexMode ? Color.green : Color.white;
            if (GUILayout.Button(addZoneVertexMode ? "✓ Click in Scene to Add Zone Vertices" : "Enter Add Zone Vertex Mode"))
            {
                addZoneVertexMode = !addZoneVertexMode;
                Tools.hidden = addZoneVertexMode;
            }
            GUI.backgroundColor = Color.white;
            
            if (!addZoneVertexMode && GUILayout.Button("Add Zone Vertex at Center"))
            {
                Undo.RecordObject(area, "Add Zone Vertex");
                AddZoneVertexAtCenter(selectedZone);
                EditorUtility.SetDirty(area);
            }
            
            EditorGUILayout.Space();
            
            // Selected zone vertex info
            if (selectedZoneVertex >= 0 && selectedZoneVertex < zone.zoneVertices.Count)
            {
                EditorGUILayout.LabelField($"Selected Zone Vertex: {selectedZoneVertex}", EditorStyles.boldLabel);
                
                EditorGUI.BeginChangeCheck();
                Vector2 newPos = EditorGUILayout.Vector2Field("Position", zone.zoneVertices[selectedZoneVertex]);
                if (EditorGUI.EndChangeCheck())
                {
                    Undo.RecordObject(area, "Move Zone Vertex");
                    zone.zoneVertices[selectedZoneVertex] = newPos;
                    EditorUtility.SetDirty(area);
                }
                
                if (GUILayout.Button("Delete Selected Zone Vertex"))
                {
                    Undo.RecordObject(area, "Remove Zone Vertex");
                    zone.zoneVertices.RemoveAt(selectedZoneVertex);
                    selectedZoneVertex = -1;
                    EditorUtility.SetDirty(area);
                }
            }
        }
        
        EditorGUILayout.Space();
        EditorGUILayout.HelpBox("• Switch to 'Edit Zones' mode to see and edit scoring zones\n• Click on zone vertices to select them\n• Drag selected vertices to move them", MessageType.Info);
    }
    
    private void OnSceneGUI()
    {
        Event e = Event.current;
        
        if (!editingZones)
        {
            HandleAreaEditing(e);
        }
        else
        {
            HandleZoneEditing(e);
        }
        
        // Handle keyboard shortcuts
        HandleKeyboardShortcuts(e);
        
        // Force repaint during dragging
        if (isDraggingVertex)
        {
            SceneView.RepaintAll();
        }
    }
    
    private void HandleAreaEditing(Event e)
    {
        // Draw the polygon shape
        DrawShapePolygon();
        
        List<Vector2> worldVertices = area.GetWorldVertices();
        
        // Add vertices in Add Vertex Mode
        if (addVertexMode && e.type == EventType.MouseDown && e.button == 0)
        {
            Ray ray = HandleUtility.GUIPointToWorldRay(e.mousePosition);
            Vector3 worldPoint = ray.origin;
            worldPoint.z = area.transform.position.z;
            
            Undo.RecordObject(area, "Add Vertex");
            Vector2 localPos = area.transform.InverseTransformPoint(worldPoint);
            area.AreaVertices.Add(localPos);
            
            e.Use();
            EditorUtility.SetDirty(area);
        }
        
        // Handle vertex editing
        HandleVertexEditing(worldVertices, e, area.AreaVertices, ref selectedVertex);
        
        // Draw connecting lines
        DrawVertexConnections(worldVertices);
    }
    
    private void HandleZoneEditing(Event e)
    {
        // Draw area outline
        DrawShapePolygon();
        
        // Draw all zones
        for (int zoneIndex = 0; zoneIndex < area.ScoringZones.Count; zoneIndex++)
        {
            ScoringZone zone = area.ScoringZones[zoneIndex];
            List<Vector2> worldZoneVertices = GetWorldZoneVertices(zone);
            
            // Draw zone polygon
            if (worldZoneVertices.Count >= 3)
            {
                Vector3[] poly = new Vector3[worldZoneVertices.Count];
                for (int i = 0; i < worldZoneVertices.Count; i++)
                {
                    poly[i] = worldZoneVertices[i];
                }
                
                Color zoneColor = zone.debugColor;
                zoneColor.a = (zoneIndex == selectedZone) ? 0.4f : 0.2f;
                Handles.color = zoneColor;
                Handles.DrawAAConvexPolygon(poly);
            }
            
            // Draw zone connections
            DrawVertexConnections(worldZoneVertices, zone.debugColor);
            
            // Handle zone vertex editing only for selected zone
            if (zoneIndex == selectedZone)
            {
                // Add zone vertices in Add Zone Vertex Mode
                if (addZoneVertexMode && e.type == EventType.MouseDown && e.button == 0)
                {
                    Ray ray = HandleUtility.GUIPointToWorldRay(e.mousePosition);
                    Vector3 worldPoint = ray.origin;
                    worldPoint.z = area.transform.position.z;
                    
                    Undo.RecordObject(area, "Add Zone Vertex");
                    Vector2 localPos = area.transform.InverseTransformPoint(worldPoint);
                    zone.zoneVertices.Add(localPos);
                    
                    e.Use();
                    EditorUtility.SetDirty(area);
                }
                
                HandleVertexEditing(worldZoneVertices, e, zone.zoneVertices, ref selectedZoneVertex);
            }
        }
    }
    
    private void HandleVertexEditing(List<Vector2> worldVertices, Event e, List<Vector2> localVertices, ref int selectedVertexRef)
    {
        // Draw vertex handles and handle selection/dragging
        for (int i = 0; i < worldVertices.Count; i++)
        {
            Vector3 worldPos = worldVertices[i];
            float actualHandleSize = HandleUtility.GetHandleSize(worldPos) * handleSize;
            
            // Determine color based on selection state
            Color handleColor = (i == selectedVertexRef) ? Color.red : Color.yellow;
            
            // Draw handle
            Handles.color = new Color(handleColor.r, handleColor.g, handleColor.b, 0.3f);
            Handles.DrawSolidDisc(worldPos, Vector3.forward, actualHandleSize);
            
            Handles.color = handleColor;
            Handles.DrawWireDisc(worldPos, Vector3.forward, actualHandleSize);
            
            // Draw vertex index
            Handles.Label(worldPos + Vector3.up * actualHandleSize * 1.5f, i.ToString());
            
            // Check for selection
            if (e.type == EventType.MouseDown && e.button == 0 && !isDraggingVertex && !addVertexMode && !addZoneVertexMode)
            {
                Vector2 screenPos = HandleUtility.WorldToGUIPoint(worldPos);
                float distToMouse = Vector2.Distance(screenPos, e.mousePosition);
                
                if (distToMouse < 30f)
                {
                    selectedVertexRef = i;
                    isDraggingVertex = true;
                    dragStartPosition = worldPos;
                    e.Use();
                    Repaint();
                }
            }
        }
        
        // Handle dragging for selected vertex
        if (selectedVertexRef >= 0 && selectedVertexRef < localVertices.Count && isDraggingVertex)
        {
            Ray ray = HandleUtility.GUIPointToWorldRay(e.mousePosition);
            Vector3 worldPoint = ray.origin;
            worldPoint.z = area.transform.position.z;
            
            // Show drag indicator
            Handles.color = Color.white;
            Handles.DrawLine(dragStartPosition, worldPoint);
            Handles.DrawWireDisc(worldPoint, Vector3.forward, HandleUtility.GetHandleSize(worldPoint) * 0.1f);
            
            if (e.type == EventType.MouseDrag)
            {
                Undo.RecordObject(area, "Move Vertex");
                Vector2 localPos = area.transform.InverseTransformPoint(worldPoint);
                localVertices[selectedVertexRef] = localPos;
                EditorUtility.SetDirty(area);
                e.Use();
            }
            
            if (e.type == EventType.MouseUp && e.button == 0)
            {
                isDraggingVertex = false;
                e.Use();
            }
        }
    }
    
    private void DrawVertexConnections(List<Vector2> worldVertices, Color? color = null)
    {
        if (worldVertices.Count > 1)
        {
            Handles.color = color ?? new Color(0f, 1f, 0f, 0.8f);
            for (int i = 0; i < worldVertices.Count; i++)
            {
                Vector3 start = worldVertices[i];
                Vector3 end = worldVertices[(i + 1) % worldVertices.Count];
                Handles.DrawLine(start, end);
            }
        }
    }
    
    private List<Vector2> GetWorldZoneVertices(ScoringZone zone)
    {
        List<Vector2> worldPoints = new List<Vector2>();
        foreach (Vector2 point in zone.zoneVertices)
        {
            worldPoints.Add((Vector2)area.transform.TransformPoint(point));
        }
        return worldPoints;
    }
    
    private void DrawShapePolygon()
    {
        List<Vector2> worldVertices = area.GetWorldVertices();
        if (worldVertices.Count < 3) return;
        
        Vector3[] poly = new Vector3[worldVertices.Count];
        for (int i = 0; i < worldVertices.Count; i++)
        {
            poly[i] = worldVertices[i];
        }
        
        Handles.color = new Color(0f, 1f, 0f, 0.2f);
        Handles.DrawAAConvexPolygon(poly);
    }
    
    private void HandleKeyboardShortcuts(Event e)
    {
        if (e.type == EventType.KeyDown && (e.keyCode == KeyCode.Delete || e.keyCode == KeyCode.Backspace))
        {
            if (!editingZones)
            {
                if (selectedVertex >= 0 && selectedVertex < area.AreaVertices.Count)
                {
                    Undo.RecordObject(area, "Remove Vertex");
                    area.AreaVertices.RemoveAt(selectedVertex);
                    selectedVertex = -1;
                    e.Use();
                    EditorUtility.SetDirty(area);
                }
            }
            else
            {
                if (selectedZoneVertex >= 0 && selectedZone >= 0 && selectedZone < area.ScoringZones.Count)
                {
                    Undo.RecordObject(area, "Remove Zone Vertex");
                    area.ScoringZones[selectedZone].zoneVertices.RemoveAt(selectedZoneVertex);
                    selectedZoneVertex = -1;
                    e.Use();
                    EditorUtility.SetDirty(area);
                }
            }
        }
        
        if ((addVertexMode || addZoneVertexMode) && e.type == EventType.KeyDown && e.keyCode == KeyCode.Escape)
        {
            addVertexMode = false;
            addZoneVertexMode = false;
            Tools.hidden = false;
            e.Use();
            Repaint();
        }
    }
    
    private void AddVertexAtCenter()
    {
        Vector2 newPos = Vector2.zero;
        if (area.AreaVertices.Count > 0)
        {
            foreach (Vector2 v in area.AreaVertices)
            {
                newPos += v;
            }
            newPos /= area.AreaVertices.Count;
            newPos += new Vector2(0.5f, 0.5f);
        }
        
        area.AreaVertices.Add(newPos);
    }
    
    private void AddZoneVertexAtCenter(int zoneIndex)
    {
        if (zoneIndex < 0 || zoneIndex >= area.ScoringZones.Count) return;
        
        ScoringZone zone = area.ScoringZones[zoneIndex];
        Vector2 newPos = Vector2.zero;
        
        if (zone.zoneVertices.Count > 0)
        {
            foreach (Vector2 v in zone.zoneVertices)
            {
                newPos += v;
            }
            newPos /= zone.zoneVertices.Count;
            newPos += new Vector2(0.2f, 0.2f);
        }
        
        zone.zoneVertices.Add(newPos);
    }
    
    private void CreateSquareShape()
    {
        area.AreaVertices.Clear();
        float size = 1.0f;
        area.AreaVertices.Add(new Vector2(-size, -size));
        area.AreaVertices.Add(new Vector2(size, -size));
        area.AreaVertices.Add(new Vector2(size, size));
        area.AreaVertices.Add(new Vector2(-size, size));
    }
    
    private void CreateDiamondShape()
    {
        area.AreaVertices.Clear();
        float size = 1.0f;
        area.AreaVertices.Add(new Vector2(0, -size));
        area.AreaVertices.Add(new Vector2(size, 0));
        area.AreaVertices.Add(new Vector2(0, size));
        area.AreaVertices.Add(new Vector2(-size, 0));
    }
    
    private void CreateCircleShape(int segments)
    {
        area.AreaVertices.Clear();
        float radius = 1.0f;
        float angleStep = 2f * Mathf.PI / segments;
        
        for (int i = 0; i < segments; i++)
        {
            float angle = i * angleStep;
            Vector2 point = new Vector2(Mathf.Cos(angle) * radius, Mathf.Sin(angle) * radius);
            area.AreaVertices.Add(point);
        }
    }
}
#endif