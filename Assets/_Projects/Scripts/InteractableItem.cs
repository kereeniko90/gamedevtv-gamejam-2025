using UnityEngine;

public class InteractableItem : MonoBehaviour
{
    public enum ItemType { Chore, Decoration }
    public ItemType type;
    public int pointValue;
    public string itemName;
    
    // State tracking - could be expanded based on item type
    public virtual bool IsComplete { get; protected set; }
    
    // Virtual methods to be overridden by child classes
    public virtual void OnPlaced() { }
    public virtual void Reset() { }
}
