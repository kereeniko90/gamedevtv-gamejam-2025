using UnityEngine;

public class InteractionZone : MonoBehaviour
{
    [SerializeField] private string zoneName;
    [SerializeField] private InteractableAppliance linkedAppliance;
    public string ZoneName => zoneName;

    // Which item types can this zone accept?
    [SerializeField] private InteractableItem.ItemType[] acceptedItemTypes;

    // Visual feedback for valid hover
    [SerializeField] private Color highlightColor = new Color(0.5f, 1f, 0.5f, 0.3f);
    private Color originalColor;
    private SpriteRenderer spriteRenderer;

    // For decorations, is this a themed zone?
    [Tooltip("If set, decorations that match this theme get bonus points")]
    [SerializeField] private string[] zoneThemes;

    private void Start()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer != null)
        {
            originalColor = spriteRenderer.color;
        }
    }

    // Can this zone accept this item?
    public bool CanAcceptItem(InteractableItem item)
    {
        // Check item type
        bool validType = false;
        foreach (var type in acceptedItemTypes)
        {
            if (item.type == type)
            {
                validType = true;
                break;
            }
        }

        if (!validType) return false;

        // If this zone has a linked appliance that's broken
        if (linkedAppliance != null && !linkedAppliance.IsWorking)
        {
            // For chore items that need this appliance, prevent placement
            ChoreItem choreItem = item as ChoreItem;
            if (choreItem != null)
            {
                string requiredZone = choreItem.RequiredZoneForCurrentState;
                if (requiredZone == zoneName)
                {
                    // Can't use this zone until appliance is fixed
                    return false;
                }
            }
        }

        // Special handling for chores
        ChoreItem choreItem2 = item as ChoreItem;
        if (choreItem2 != null)
        {
            string requiredZone = choreItem2.RequiredZoneForCurrentState;

            // If chore requires a specific zone, check if this is the right one
            if (requiredZone != null && requiredZone != zoneName)
            {
                return false;
            }
        }

        return true;
    }

    public void InteractWithZone()
    {
        // If zone has a broken appliance, launch repair mini-game
        if (linkedAppliance != null && !linkedAppliance.IsWorking)
        {
            linkedAppliance.LaunchRepairMiniGame();
        }
    }

    // Process the item when it's placed here
    public void ProcessItem(InteractableItem item)
    {
        // Handle chore items
        ChoreItem choreItem = item as ChoreItem;
        if (choreItem != null)
        {
            // Advance the chore state when placed in the right zone
            choreItem.TryInteractWithZone(zoneName);
        }

        // Handle decoration items
        DecorationItem decorItem = item as DecorationItem;
        if (decorItem != null)
        {
            decorItem.OnPlaced();

            // Register with score manager
            ScoreManager.Instance?.RegisterDecorationPlacement(decorItem, this);
        }
    }

    // Check if this zone matches a theme for decoration scoring
    public bool MatchesTheme(string theme)
    {
        if (zoneThemes == null || zoneThemes.Length == 0) return false;

        foreach (string zoneTheme in zoneThemes)
        {
            if (zoneTheme == theme) return true;
        }

        return false;
    }

    // Visual feedback when valid items hover over this zone
    public void Highlight(bool isActive)
    {
        if (spriteRenderer != null)
        {
            spriteRenderer.color = isActive ? highlightColor : originalColor;
        }
    }
}