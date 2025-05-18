using UnityEngine;

public class ZoneClickHandler : MonoBehaviour
{
    private InteractionZone zone;
    
    private void Start()
    {
        zone = GetComponent<InteractionZone>();
    }
    
    private void OnMouseDown()
    {
        if (!DragAndDropHandler.Instance.IsDragging)
        {
            zone?.InteractWithZone();
        }
    }
}