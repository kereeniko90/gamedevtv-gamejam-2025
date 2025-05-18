using UnityEditor;
using UnityEngine;

public class AddHeaderUtility : MonoBehaviour
{
    [MenuItem("GameObject/Create Hierarchy Header", false, 0)]
    static void CreateHierarchyHeader(MenuCommand menuCommand)
    {
        GameObject headerObject = new GameObject("== HEADER ==");
        var header = headerObject.AddComponent<HierarchyHeader>();

        // Set default colors
        header.headerLabel = "== HEADER ==";
        header.backgroundColor = new Color(0.2f, 0.2f, 0.2f, 1f);
        header.textColor = Color.white;

        // Ensure it's parented correctly
        GameObjectUtility.SetParentAndAlign(headerObject, menuCommand.context as GameObject);
        Undo.RegisterCreatedObjectUndo(headerObject, "Create Hierarchy Header");
        Selection.activeObject = headerObject;
    }
}
