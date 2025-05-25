using UnityEngine;
using UnityEngine.SceneManagement;

public class CameraController : MonoBehaviour
{
    [Header("Scene Settings")]
    [SerializeField] private string gameSceneName = "GameScene";
    [SerializeField] private bool enableOnlyInGameScene = true;
    
    [Header("Camera Bounds")]
    [SerializeField] private Vector2 minBounds = new Vector2(-10f, -10f);
    [SerializeField] private Vector2 maxBounds = new Vector2(10f, 10f);
    [SerializeField] private bool showBoundsGizmo = true;
    [SerializeField] private Color boundsGizmoColor = Color.yellow;
    
    [Header("Middle Click Drag Settings")]
    [SerializeField] private float dragSensitivity = 1f;
    [SerializeField] private bool invertDrag = false;
    
    [Header("Edge Scrolling Settings")]
    [SerializeField] private bool enableEdgeScrolling = true;
    [SerializeField] private float edgeScrollSpeed = 5f;
    [SerializeField] private float edgeScrollBorderSize = 50f; // pixels from screen edge
    [SerializeField] private bool edgeScrollRequiresFocus = true;
    
    private Camera cam;
    private bool isDragging = false;
    private Vector3 lastMousePosition;
    private Vector3 dragStartPosition;
    private bool isControllerEnabled = false;
    
    private void Awake()
    {
        cam = GetComponent<Camera>();
        if (cam == null)
        {
            Debug.LogError("CameraController: No Camera component found!");
        }
        
        // Subscribe to scene loaded events
        SceneManager.sceneLoaded += OnSceneLoaded;
        
        // Check current scene state
        CheckSceneState();
    }
    
    private void OnDestroy()
    {
        // Unsubscribe from scene events
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }
    
    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        CheckSceneState();
    }
    
    private void CheckSceneState()
    {
        if (!enableOnlyInGameScene)
        {
            isControllerEnabled = true;
            return;
        }
        
        // Check if the game scene is loaded
        Scene gameScene = SceneManager.GetSceneByName(gameSceneName);
        if (gameScene.IsValid() && gameScene.isLoaded)
        {
            isControllerEnabled = true;
            Debug.Log($"CameraController: Enabled for scene '{gameSceneName}'");
        }
        else
        {
            isControllerEnabled = false;
            
            // Stop any ongoing drag operation
            if (isDragging)
            {
                isDragging = false;
            }
        }
    }
    
    private void Update()
    {
        // Only handle input if controller is enabled
        if (!isControllerEnabled)
            return;
            
        HandleMiddleClickDrag();
        
        if (enableEdgeScrolling)
        {
            HandleEdgeScrolling();
        }
    }
    
    private void HandleMiddleClickDrag()
    {
        // Start dragging on middle mouse button down
        if (Input.GetMouseButtonDown(2)) // Middle mouse button
        {
            isDragging = true;
            lastMousePosition = GetMouseWorldPosition();
            dragStartPosition = transform.position;
        }
        
        // Continue dragging while middle mouse button is held
        if (isDragging && Input.GetMouseButton(2))
        {
            Vector3 currentMousePosition = GetMouseWorldPosition();
            Vector3 deltaMovement = lastMousePosition - currentMousePosition;
            
            if (invertDrag)
            {
                deltaMovement = -deltaMovement;
            }
            
            // Apply drag sensitivity
            deltaMovement *= dragSensitivity;
            
            // Calculate new position
            Vector3 newPosition = transform.position + deltaMovement;
            
            // Apply bounds
            newPosition = ClampToBounds(newPosition);
            
            // Update camera position
            transform.position = newPosition;
            
            // Update last mouse position for next frame
            lastMousePosition = GetMouseWorldPosition();
        }
        
        // Stop dragging on middle mouse button up
        if (Input.GetMouseButtonUp(2))
        {
            isDragging = false;
        }
    }
    
    private void HandleEdgeScrolling()
    {
        // Skip edge scrolling if we require focus and don't have it
        if (edgeScrollRequiresFocus && !Application.isFocused)
            return;
            
        // Skip edge scrolling while dragging
        if (isDragging)
            return;
        
        Vector3 mousePosition = Input.mousePosition;
        Vector3 moveDirection = Vector3.zero;
        
        // Check left edge
        if (mousePosition.x <= edgeScrollBorderSize)
        {
            moveDirection.x -= 1f;
        }
        // Check right edge
        else if (mousePosition.x >= Screen.width - edgeScrollBorderSize)
        {
            moveDirection.x += 1f;
        }
        
        // Check bottom edge
        if (mousePosition.y <= edgeScrollBorderSize)
        {
            moveDirection.y -= 1f;
        }
        // Check top edge
        else if (mousePosition.y >= Screen.height - edgeScrollBorderSize)
        {
            moveDirection.y += 1f;
        }
        
        // Apply edge scrolling movement
        if (moveDirection != Vector3.zero)
        {
            // Normalize diagonal movement to prevent faster diagonal scrolling
            moveDirection.Normalize();
            
            Vector3 newPosition = transform.position + moveDirection * edgeScrollSpeed * Time.deltaTime;
            newPosition = ClampToBounds(newPosition);
            transform.position = newPosition;
        }
    }
    
    private Vector3 GetMouseWorldPosition()
    {
        Vector3 mousePos = Input.mousePosition;
        mousePos.z = -cam.transform.position.z; // Distance from camera
        return cam.ScreenToWorldPoint(mousePos);
    }
    
    private Vector3 ClampToBounds(Vector3 position)
    {
        // Clamp X and Y to the defined bounds
        position.x = Mathf.Clamp(position.x, minBounds.x, maxBounds.x);
        position.y = Mathf.Clamp(position.y, minBounds.y, maxBounds.y);
        
        // Keep the original Z position
        return position;
    }
    
    // Public method to manually enable/disable the controller
    public void SetControllerEnabled(bool enabled)
    {
        isControllerEnabled = enabled;
        
        // Stop any ongoing drag operation if disabling
        if (!enabled && isDragging)
        {
            isDragging = false;
        }
        
        Debug.Log($"CameraController: Manually set to {(enabled ? "enabled" : "disabled")}");
    }
    
    // Public method to check if controller is currently enabled
    public bool IsControllerEnabled()
    {
        return isControllerEnabled;
    }
    
    // Public method to set bounds programmatically
    public void SetBounds(Vector2 min, Vector2 max)
    {
        minBounds = min;
        maxBounds = max;
        
        // Immediately clamp current position to new bounds
        transform.position = ClampToBounds(transform.position);
    }
    
    // Public method to get current bounds
    public void GetBounds(out Vector2 min, out Vector2 max)
    {
        min = minBounds;
        max = maxBounds;
    }
    
    // Public method to check if a world position is within bounds
    public bool IsWithinBounds(Vector3 worldPosition)
    {
        return worldPosition.x >= minBounds.x && worldPosition.x <= maxBounds.x &&
               worldPosition.y >= minBounds.y && worldPosition.y <= maxBounds.y;
    }
    
    // For debugging - draw the camera bounds
    private void OnDrawGizmos()
    {
        if (!showBoundsGizmo) return;
        
        Gizmos.color = boundsGizmoColor;
        
        Vector3 center = new Vector3(
            (minBounds.x + maxBounds.x) / 2f,
            (minBounds.y + maxBounds.y) / 2f,
            0f
        );
        
        Vector3 size = new Vector3(
            maxBounds.x - minBounds.x,
            maxBounds.y - minBounds.y,
            0f
        );
        
        Gizmos.DrawWireCube(center, size);
        
        // Draw corner markers
        Gizmos.color = Color.red;
        float markerSize = 0.5f;
        
        // Bottom-left
        Gizmos.DrawWireCube(new Vector3(minBounds.x, minBounds.y, 0), Vector3.one * markerSize);
        // Top-right
        Gizmos.DrawWireCube(new Vector3(maxBounds.x, maxBounds.y, 0), Vector3.one * markerSize);
        // Top-left
        Gizmos.DrawWireCube(new Vector3(minBounds.x, maxBounds.y, 0), Vector3.one * markerSize);
        // Bottom-right
        Gizmos.DrawWireCube(new Vector3(maxBounds.x, minBounds.y, 0), Vector3.one * markerSize);
    }
}