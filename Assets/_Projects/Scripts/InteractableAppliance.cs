using UnityEngine;

public class InteractableAppliance : MonoBehaviour
{
    public enum ApplianceState { Working, Broken }
    
    [SerializeField] private ApplianceState currentState = ApplianceState.Working;
    [SerializeField] private string applianceName;
    [SerializeField] private string miniGamePrefabName; // Name of the mini-game prefab to instantiate
    
    public bool IsWorking => currentState == ApplianceState.Working;
    
    public void SetState(ApplianceState newState)
    {
        currentState = newState;
        UpdateVisuals();
    }
    
    public void Repair()
    {
        SetState(ApplianceState.Working);
    }
    
    private void UpdateVisuals()
    {
        // Update sprite or effects based on state
        // For broken: show smoke particles, different sprite, etc.
    }
    
    public void LaunchRepairMiniGame()
    {
        // Tell the mini-game manager to launch this appliance's mini-game
        MiniGameManager.Instance?.LaunchMiniGame(miniGamePrefabName, this);
    }
}