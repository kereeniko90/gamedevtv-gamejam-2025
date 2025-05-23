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
        EditorGUILayout.LabelField("Vertex Editing Tools", EditorStyles.boldLabel);
        
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
        else
        {
            EditorGUILayout.LabelField("No Vertex Selected", EditorStyles.boldLabel);
        }
        
        EditorGUILayout.Space();
        EditorGUILayout.HelpBox("• Click on a vertex to select it\n• Drag selected vertex to move it\n• Press Delete to remove a selected vertex", MessageType.Info);
        
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
    
    private void OnSceneGUI()
    {
        Event e = Event.current;
        
        // Draw the polygon shape and handle interactions
        DrawShapePolygon();
        
        // Handle keyboard shortcuts
        HandleKeyboardShortcuts(e);
        
        // CUSTOM VERTEX MOVEMENT SYSTEM
        // This replaces Unity's PositionHandle which can be finicky
        
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
        
        // SIMPLIFIED VERTEX SELECTION AND MOVEMENT
        // Use simple circular handles for all vertices
        for (int i = 0; i < worldVertices.Count; i++)
        {
            Vector3 worldPos = worldVertices[i];
            float actualHandleSize = HandleUtility.GetHandleSize(worldPos) * handleSize;
            
            // Determine color based on selection state
            Color handleColor = (i == selectedVertex) ? Color.red : Color.yellow;
            
            // Draw a solid disc for better visibility
            Handles.color = new Color(handleColor.r, handleColor.g, handleColor.b, 0.3f);
            Handles.DrawSolidDisc(worldPos, Vector3.forward, actualHandleSize);
            
            // Draw outline
            Handles.color = handleColor;
            Handles.DrawWireDisc(worldPos, Vector3.forward, actualHandleSize);
            
            // Draw vertex index
            Handles.Label(worldPos + Vector3.up * actualHandleSize * 1.5f, i.ToString());
            
            // Check for selection
            if (e.type == EventType.MouseDown && e.button == 0 && !isDraggingVertex && !addVertexMode)
            {
                // Convert handle position to screen space
                Vector2 screenPos = HandleUtility.WorldToGUIPoint(worldPos);
                float distToMouse = Vector2.Distance(screenPos, e.mousePosition);
                
                // If click is within handle area, select this vertex
                if (distToMouse < 30f) // Generous click radius in pixels
                {
                    selectedVertex = i;
                    isDraggingVertex = true;
                    dragStartPosition = worldPos;
                    e.Use();
                    Repaint();
                }
            }
        }
        
        // Handle dragging movement for selected vertex
        if (selectedVertex >= 0 && selectedVertex < area.AreaVertices.Count)
        {
            // Start dragging on mouse down
            if (e.type == EventType.MouseDown && e.button == 0 && !isDraggingVertex && !addVertexMode)
            {
                isDraggingVertex = true;
                dragStartPosition = worldVertices[selectedVertex];
                e.Use();
            }
            
            // Process dragging
            if (isDraggingVertex)
            {
                // Convert mouse position to world space
                Ray ray = HandleUtility.GUIPointToWorldRay(e.mousePosition);
                Vector3 worldPoint = ray.origin;
                worldPoint.z = area.transform.position.z; // Keep on same Z plane
                
                // Show a visual indicator during drag
                Handles.color = Color.white;
                Handles.DrawLine(dragStartPosition, worldPoint);
                Handles.DrawWireDisc(worldPoint, Vector3.forward, HandleUtility.GetHandleSize(worldPoint) * 0.1f);
                
                // Update vertex position on drag
                if (e.type == EventType.MouseDrag)
                {
                    Undo.RecordObject(area, "Move Vertex");
                    Vector2 localPos = area.transform.InverseTransformPoint(worldPoint);
                    area.AreaVertices[selectedVertex] = localPos;
                    EditorUtility.SetDirty(area);
                    e.Use();
                }
                
                // End dragging on mouse up
                if (e.type == EventType.MouseUp && e.button == 0)
                {
                    isDraggingVertex = false;
                    e.Use();
                }
            }
        }
        
        // Draw connecting lines between vertices
        if (worldVertices.Count > 1)
        {
            Handles.color = new Color(0f, 1f, 0f, 0.8f);
            for (int i = 0; i < worldVertices.Count; i++)
            {
                Vector3 start = worldVertices[i];
                Vector3 end = worldVertices[(i + 1) % worldVertices.Count];
                Handles.DrawLine(start, end);
            }
        }
        
        // Force repaint to ensure smooth dragging
        if (isDraggingVertex)
        {
            SceneView.RepaintAll();
        }
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
            if (selectedVertex >= 0 && selectedVertex < area.AreaVertices.Count)
            {
                Undo.RecordObject(area, "Remove Vertex");
                area.AreaVertices.RemoveAt(selectedVertex);
                selectedVertex = -1;
                e.Use();
                EditorUtility.SetDirty(area);
            }
        }
        
        if (addVertexMode && e.type == EventType.KeyDown && e.keyCode == KeyCode.Escape)
        {
            addVertexMode = false;
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