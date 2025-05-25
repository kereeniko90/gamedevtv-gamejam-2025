using UnityEngine;
using UnityEngine.EventSystems;

public class TooltipTrigger : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    private DecorationItem decorationItem;
    private bool isHovering = false;
    
    [Header("Debug")]
    [SerializeField] private bool showDebugLogs = true;
    
    private void Awake()
    {
        // Get the decoration item component from this object or parent
        decorationItem = GetComponent<DecorationItem>();
        if (decorationItem == null)
        {
            decorationItem = GetComponentInParent<DecorationItem>();
        }
        
        if (decorationItem == null)
        {
            Debug.LogWarning($"TooltipTrigger on {gameObject.name} couldn't find a DecorationItem component!");
        }
        
        // Make sure we have a collider
        Collider2D collider = GetComponent<Collider2D>();
        if (collider == null)
        {
            Debug.LogWarning($"TooltipTrigger on {gameObject.name} has no Collider2D! Hover detection won't work.");
        }
        else if (!collider.isTrigger)
        {
            Debug.LogWarning($"TooltipTrigger on {gameObject.name} - Collider should be a trigger for best hover detection.");
        }
    }
    
    public void OnPointerEnter(PointerEventData eventData)
    {
        if (showDebugLogs)
            Debug.Log($"Hover ENTER on {gameObject.name}");
            
        if (decorationItem != null && decorationItem.ItemData != null && HintUI.Instance != null)
        {
            isHovering = true;
            HintUI.Instance.ShowHint(decorationItem.ItemData.description);
            
            if (showDebugLogs)
                Debug.Log($"Showing hint: {decorationItem.ItemData.description}");
        }
        else
        {
            if (showDebugLogs)
                Debug.LogWarning($"Cannot show hint - DecorationItem: {decorationItem != null}, ItemData: {decorationItem?.ItemData != null}, HintUI: {HintUI.Instance != null}");
        }
    }
    
    public void OnPointerExit(PointerEventData eventData)
    {
        if (showDebugLogs)
            Debug.Log($"Hover EXIT on {gameObject.name}");
            
        if (HintUI.Instance != null)
        {
            isHovering = false;
            HintUI.Instance.HideHint();
        }
    }
    
    // Public method to hide tooltip (useful when dragging starts)
    public void ForceHideTooltip()
    {
        if (HintUI.Instance != null)
        {
            isHovering = false;
            HintUI.Instance.HideHint();
        }
    }
    
    // Public method to show tooltip (useful when dragging)
    public void ForceShowTooltip()
    {
        if (decorationItem != null && decorationItem.ItemData != null && HintUI.Instance != null)
        {
            HintUI.Instance.ShowHint(decorationItem.ItemData.description);
        }
    }
    
    public bool IsHovering => isHovering;
}