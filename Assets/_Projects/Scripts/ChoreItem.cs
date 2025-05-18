using System.Collections.Generic;
using UnityEngine;

public class ChoreItem : InteractableItem
{
    // Define chore types
    public enum ChoreType
    {
        Laundry,
        Plants,
        Dishes,
        Dusting,
        Trash,
        // Add more as needed
    }

    [SerializeField] private ChoreType choreType;

    // Current progression index in the state sequence
    private int currentStateIndex = 0;

    // Each chore type has its own sequence of states
    private Dictionary<ChoreType, string[]> choreStates = new Dictionary<ChoreType, string[]>()
    {
        { ChoreType.Laundry, new string[] { "Dirty", "Washing", "Drying", "Folded" } },
        { ChoreType.Plants, new string[] { "Wilting", "Watering", "Watered" } },
        { ChoreType.Dishes, new string[] { "Dirty", "Soaking", "Washing", "Drying", "Clean" } },
        { ChoreType.Dusting, new string[] { "Dusty", "Dusting", "Clean" } },
        { ChoreType.Trash, new string[] { "Full", "Emptying", "Empty" } },
        // Add more as needed
    };

    // Track if this chore has been registered as complete with the score manager
    private bool scoreRegistered = false;

    // Get the current state name
    public string CurrentStateName
    {
        get
        {
            string[] states = GetStatesForChoreType();
            if (states != null && currentStateIndex < states.Length)
                return states[currentStateIndex];
            return "Unknown";
        }
    }

    // Is this chore at its final state?
    public override bool IsComplete => currentStateIndex >= GetStatesForChoreType().Length - 1;

    // Required interaction zone for current state (could be null if any zone works)
    public string RequiredZoneForCurrentState
    {
        get
        {
            // Define which zones are needed for each state
            // This could be moved to a more robust configuration system
            if (choreType == ChoreType.Laundry)
            {
                switch (currentStateIndex)
                {
                    case 0: return null; // Dirty - can be picked up from anywhere
                    case 1: return "WashingMachine"; // Needs washing machine
                    case 2: return "DryingRack"; // Needs drying rack
                    case 3: return "Drawer"; // Needs to be put away
                    default: return null;
                }
            }
            else if (choreType == ChoreType.Plants)
            {
                switch (currentStateIndex)
                {
                    case 0: return null; // Wilting - can be identified anywhere
                    case 1: return "WaterSource"; // Needs water source
                    case 2: return "PlantLocation"; // Needs to be placed back
                    default: return null;
                }
            }
            // Add more mappings for other chore types

            return null; // Default - no specific zone required
        }
    }

    // Get the array of states for this chore type
    private string[] GetStatesForChoreType()
    {
        if (choreStates.TryGetValue(choreType, out string[] states))
            return states;

        // Fallback if the chore type isn't defined
        Debug.LogWarning($"No states defined for chore type: {choreType}");
        return new string[] { "Incomplete", "Complete" };
    }

    // Get total number of states for this chore
    public int TotalStates => GetStatesForChoreType().Length;

    // Advance to the next state
    public void AdvanceState()
    {
        string[] states = GetStatesForChoreType();
        if (currentStateIndex < states.Length - 1)
        {
            currentStateIndex++;
            OnStateChanged();
        }
    }

    // Set to a specific state by name
    public void SetState(string stateName)
    {
        string[] states = GetStatesForChoreType();
        for (int i = 0; i < states.Length; i++)
        {
            if (states[i] == stateName)
            {
                currentStateIndex = i;
                OnStateChanged();
                return;
            }
        }

        Debug.LogWarning($"State '{stateName}' not found for chore type {choreType}");
    }

    // Reset to initial state
    public override void Reset()
    {
        currentStateIndex = 0;
        scoreRegistered = false;
        OnStateChanged();
    }

    // Called when state changes
    private void OnStateChanged()
    {
        // Update visual representation
        UpdateVisual();

        // Notify score manager if this is now complete
        if (IsComplete && !scoreRegistered)
        {
            ScoreManager.Instance?.RegisterCompletedChore(this);
            scoreRegistered = true;
        }
    }

    // Change visual based on state
    private void UpdateVisual()
    {
        // For placeholder graphics, change color based on progress
        SpriteRenderer renderer = GetComponent<SpriteRenderer>();
        if (renderer != null)
        {
            // Gradually shift from red (0%) to green (100%)
            float progress = (float)currentStateIndex / (GetStatesForChoreType().Length - 1);
            renderer.color = Color.Lerp(Color.red, Color.green, progress);
        }

        // When you have proper sprites, you'd change the sprite instead:
        // GetComponent<SpriteRenderer>().sprite = stateSprites[currentStateIndex];
    }

    // Additional method to interact with a specific zone
    public bool TryInteractWithZone(string zoneName)
    {
        // Check if this zone is valid for the current state
        string requiredZone = RequiredZoneForCurrentState;

        // If no specific zone is required, or the provided zone matches
        if (requiredZone == null || requiredZone == zoneName)
        {
            AdvanceState();
            return true;
        }

        return false;
    }
}