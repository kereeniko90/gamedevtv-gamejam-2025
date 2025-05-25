using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

public class PlacementDebugUI : MonoBehaviour
{
    [Header("Debug UI References")]
    [SerializeField] private GameObject debugPanel;
    [SerializeField] private TextMeshProUGUI debugText;
    [SerializeField] private Button toggleButton;

    [Header("Debug Settings")]
    [SerializeField] private bool showDebugOnStart = true;
    [SerializeField] private float fadeOutTime = 3f;
    [SerializeField] private Color positiveScoreColor = Color.green;
    [SerializeField] private Color negativeScoreColor = Color.red;
    [SerializeField] private Color neutralScoreColor = Color.yellow;

    // Singleton for easy access
    public static PlacementDebugUI Instance { get; private set; }

    private void Awake()
    {
        // Singleton setup
        if (Instance == null)
        {
            Instance = this;
            SetupDebugUI();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        if (debugPanel != null)
        {
            debugPanel.SetActive(showDebugOnStart);
        }

        // Setup toggle button
        if (toggleButton != null)
        {
            toggleButton.onClick.AddListener(ToggleDebugPanel);
        }
    }

    private void SetupDebugUI()
    {
        // If no UI references are set, create them dynamically
        if (debugPanel == null)
        {
            CreateDebugUI();
        }
    }

    private void CreateDebugUI()
    {
        // Create Canvas if needed
        Canvas canvas = FindFirstObjectByType<Canvas>();
        if (canvas == null)
        {
            GameObject canvasObj = new GameObject("Debug Canvas");
            canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 1000; // Make sure it's on top
            canvasObj.AddComponent<CanvasScaler>();
            canvasObj.AddComponent<GraphicRaycaster>();
        }

        // Create debug panel
        GameObject panelObj = new GameObject("PlacementDebugPanel");
        panelObj.transform.SetParent(canvas.transform, false);

        debugPanel = panelObj;

        // Add panel background
        Image panelImage = panelObj.AddComponent<Image>();
        panelImage.color = new Color(0, 0, 0, 0.7f);

        // Position panel
        RectTransform panelRect = panelObj.GetComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(0, 1);
        panelRect.anchorMax = new Vector2(0, 1);
        panelRect.pivot = new Vector2(0, 1);
        panelRect.anchoredPosition = new Vector2(10, -10);
        panelRect.sizeDelta = new Vector2(400, 200);

        // Create debug text
        GameObject textObj = new GameObject("DebugText");
        textObj.transform.SetParent(panelObj.transform, false);

        debugText = textObj.AddComponent<TextMeshProUGUI>();
        debugText.text = "Placement Debug Ready\nPlace items to see scoring...";
        debugText.fontSize = 30;
        debugText.color = Color.white;
        debugText.alignment = TextAlignmentOptions.TopLeft;

        // Position text
        RectTransform textRect = textObj.GetComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = new Vector2(10, 10);
        textRect.offsetMax = new Vector2(-10, -10);

        // Create toggle button
        GameObject buttonObj = new GameObject("ToggleButton");
        buttonObj.transform.SetParent(canvas.transform, false);

        toggleButton = buttonObj.AddComponent<Button>();
        Image buttonImage = buttonObj.AddComponent<Image>();
        buttonImage.color = new Color(0.2f, 0.2f, 0.2f, 0.8f);

        // Position button
        RectTransform buttonRect = buttonObj.GetComponent<RectTransform>();
        buttonRect.anchorMin = new Vector2(1, 1);
        buttonRect.anchorMax = new Vector2(1, 1);
        buttonRect.pivot = new Vector2(1, 1);
        buttonRect.anchoredPosition = new Vector2(-10, -10);
        buttonRect.sizeDelta = new Vector2(100, 30);

        // Button text
        GameObject buttonTextObj = new GameObject("ButtonText");
        buttonTextObj.transform.SetParent(buttonObj.transform, false);

        TextMeshProUGUI buttonText = buttonTextObj.AddComponent<TextMeshProUGUI>();
        buttonText.text = "Debug";
        buttonText.fontSize = 12;
        buttonText.color = Color.white;
        buttonText.alignment = TextAlignmentOptions.Center;

        RectTransform buttonTextRect = buttonTextObj.GetComponent<RectTransform>();
        buttonTextRect.anchorMin = Vector2.zero;
        buttonTextRect.anchorMax = Vector2.one;
        buttonTextRect.offsetMin = Vector2.zero;
        buttonTextRect.offsetMax = Vector2.zero;
    }

    // Call this method when an item is placed
    public void ShowPlacementScore(DecorationItem item, PlacementScore score)
    {
        if (debugPanel == null || !debugPanel.activeInHierarchy) return;

        string debugInfo = FormatScoreDebugInfo(item, score);

        if (debugText != null)
        {
            debugText.text = debugInfo;

            // Color coding based on score
            if (score.pointsAwarded > 0)
                debugText.color = positiveScoreColor;
            else if (score.pointsAwarded < 0)
                debugText.color = negativeScoreColor;
            else
                debugText.color = neutralScoreColor;
        }

        // Log to console as well - include collider position info
        Vector2 colliderPos = item.GetColliderWorldCenter();
        Debug.Log($"[PLACEMENT DEBUG] {item.ItemData.itemName}: {score.pointsAwarded} points - {score.scoringReason} (Collider Center: {colliderPos})");
    }

    private string FormatScoreDebugInfo(DecorationItem item, PlacementScore score)
    {
        string info = "=== PLACEMENT DEBUG ===\n";
        info += $"Item: {item.ItemData.itemName}\n";
        info += $"Score Position: {score.worldPosition}\n";

        // Show both transform and collider positions for debugging
        Vector2 transformPos = item.transform.position;
        Vector2 colliderPos = item.GetColliderWorldCenter();
        info += $"Transform Pos: {transformPos}\n";
        info += $"Collider Center: {colliderPos}\n";

        // Show offset if there's a difference
        Vector2 offset = colliderPos - transformPos;
        if (offset.magnitude > 0.01f)
        {
            info += $"Collider Offset: {offset}\n";
        }

        info += $"Valid Area: {(score.placedInValidArea ? "YES" : "NO")}\n";
        info += $"Points Awarded: {score.pointsAwarded}\n";
        info += $"Reason: {score.scoringReason}\n";

        if (!string.IsNullOrEmpty(score.zoneName))
        {
            info += $"Zone: {score.zoneName}\n";
        }

        info += "\n--- Item Preferences ---\n";
        if (item.ItemData.placementPreferences.Count == 0)
        {
            info += "No placement preferences set\n";
        }
        else
        {
            foreach (var pref in item.ItemData.placementPreferences)
            {
                info += $"• {pref.areaIdentifier}: {pref.defaultAreaPoints} pts\n";
                foreach (var zone in pref.zones)
                {
                    info += $"  - {zone.zoneName}: {zone.pointValue} pts\n";
                }
            }
        }

        info += $"\nBase Points: {item.ItemData.basePoints}\n";
        info += $"Wrong Placement Penalty: {item.ItemData.wrongPlacementPenalty}\n";

        return info;
    }

    // Call this to show area information when hovering/dragging
    public void ShowAreaInfo(Vector2 worldPosition)
    {
        if (debugPanel == null || !debugPanel.activeInHierarchy) return;

        PlaceableArea[] allAreas = FindObjectsByType<PlaceableArea>(FindObjectsSortMode.None);

        string info = "=== AREA DEBUG ===\n";
        info += $"Position: {worldPosition}\n\n";

        bool foundArea = false;
        foreach (PlaceableArea area in allAreas)
        {
            if (area.ContainsPoint(worldPosition))
            {
                foundArea = true;
                info += $"IN AREA: {area.AreaIdentifier}\n";

                // Check zones
                foreach (var zone in area.ScoringZones)
                {
                    Vector2 localPoint = area.transform.InverseTransformPoint(worldPosition);
                    if (IsPointInPolygon(localPoint, zone.zoneVertices))
                    {
                        info += $"IN ZONE: {zone.zoneName} ({zone.pointValue} pts)\n";
                    }
                }
                info += "\n";
            }
        }

        if (!foundArea)
        {
            info += "NOT IN ANY PLACEABLE AREA\n";
        }

        if (debugText != null)
        {
            debugText.text = info;
            debugText.color = Color.white;
        }
    }

    // Point-in-polygon check (copied from PlaceableArea for consistency)
    private bool IsPointInPolygon(Vector2 point, List<Vector2> polygon)
    {
        if (polygon.Count < 3) return false;

        bool inside = false;
        for (int i = 0, j = polygon.Count - 1; i < polygon.Count; j = i++)
        {
            bool intersect = ((polygon[i].y > point.y) != (polygon[j].y > point.y)) &&
                (point.x < (polygon[j].x - polygon[i].x) * (point.y - polygon[i].y) /
                (polygon[j].y - polygon[i].y) + polygon[i].x);

            if (intersect) inside = !inside;
        }

        return inside;
    }

    public void ToggleDebugPanel()
    {
        if (debugPanel != null)
        {
            debugPanel.SetActive(!debugPanel.activeInHierarchy);
        }
    }

    public void ClearDebugInfo()
    {
        if (debugText != null)
        {
            debugText.text = "Placement Debug Ready\nPlace items to see scoring...";
            debugText.color = Color.white;
        }
    }

    private void Update()
    {
        // Keyboard shortcut to toggle debug
        if (Input.GetKeyDown(KeyCode.F1))
        {
            ToggleDebugPanel();
        }

        // Show area info when dragging (optional feature)
        if (Input.GetMouseButton(0))
        {
            Vector3 mouseWorldPos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            mouseWorldPos.z = 0;

            // Only show if we're potentially dragging something
            DraggableItem dragItem = FindMouseOverDraggable();
            if (dragItem != null)
            {
                ShowAreaInfo(mouseWorldPos);
            }
        }
    }

    private DraggableItem FindMouseOverDraggable()
    {
        Vector3 mouseWorldPos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        RaycastHit2D hit = Physics2D.Raycast(mouseWorldPos, Vector2.zero);

        if (hit.collider != null)
        {
            return hit.collider.GetComponent<DraggableItem>();
        }

        return null;
    }
}