using UnityEngine;

[AddComponentMenu("")] // hides from Add Component
public class HierarchyHeader : MonoBehaviour
{
    public string headerLabel = "== HEADER ==";
    public Color backgroundColor = Color.gray;
    public Color textColor = Color.white;

    private void Reset()
    {
        gameObject.name = headerLabel;
    }

    private void Awake()
    {
        #if UNITY_EDITOR
        gameObject.hideFlags = HideFlags.NotEditable;
        #endif
    }
}
