using UnityEngine;
using UnityEditor;

[InitializeOnLoad]
public static class HierarchyHeaderEditor
{
    static HierarchyHeaderEditor()
    {
        EditorApplication.hierarchyWindowItemOnGUI += OnHierarchyGUI;
    }

    static void OnHierarchyGUI(int instanceID, Rect selectionRect)
    {
        GameObject obj = EditorUtility.InstanceIDToObject(instanceID) as GameObject;
        if (obj == null) return;

        HierarchyHeader header = obj.GetComponent<HierarchyHeader>();
        if (header == null) return;

        EditorGUI.DrawRect(selectionRect, header.backgroundColor);

        var style = new GUIStyle(EditorStyles.boldLabel)
        {
            normal = { textColor = header.textColor },
            alignment = TextAnchor.MiddleCenter,
            fontStyle = FontStyle.Bold
        };

        EditorGUI.LabelField(selectionRect, header.headerLabel, style);
    }
}
