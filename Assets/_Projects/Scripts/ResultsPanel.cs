using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using DG.Tweening;

public class ResultsPanel : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private GameObject resultsPanel;
    [SerializeField] private TextMeshProUGUI dayText;
    [SerializeField] private Slider progressBar;
    [SerializeField] private Image progressBarFill;
    [SerializeField] private GameObject heartsEmpty;
    [SerializeField] private GameObject heartsFull;
    [SerializeField] private GameObject starsEmpty;
    [SerializeField] private GameObject starsFull;
    [SerializeField] private TextMeshProUGUI totalPointsText;
    [SerializeField] private Transform bottomContainer; // Grid layout container
    [SerializeField] private GameObject decoItemPrefab;
    [SerializeField] private Button continueButton;

    [Header("Progress Bar Colors")]
    [SerializeField] private Color greyColor = new Color(0.5f, 0.5f, 0.5f, 1f);
    [SerializeField] private Color orangeColor = new Color(1f, 0.6f, 0f, 1f);
    [SerializeField] private Color greenColor = new Color(0f, 0.8f, 0f, 1f);

    [Header("Animation Settings")]
    [SerializeField] private float panelFadeInDuration = 0.5f;
    [SerializeField] private float progressBarAnimationDuration = 1.5f;
    [SerializeField] private float itemSpawnDelay = 0.1f;
    [SerializeField] private Ease progressBarEase = Ease.OutQuad;

    // Singleton for easy access
    public static ResultsPanel Instance { get; private set; }
    private static int cumulativeMaxPoints = 0;

    // State tracking
    private int currentDayMaxPoints = 0;
    private int totalMaxPointsSoFar = 0;
    private List<DecoItemResult> currentDayResults = new List<DecoItemResult>();

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        // Initially hide the panel
        if (resultsPanel != null)
        {
            resultsPanel.SetActive(false);
        }

        // Setup continue button
        if (continueButton != null)
        {
            continueButton.onClick.AddListener(OnContinueButtonClicked);
        }
    }

    private void OnEnable()
    {
        // Subscribe to score events
        HermitCrabAnimator.OnHermitFinishedReturning += OnHermitReturned;

        // Also subscribe to day ended event as backup
        TimeManager.OnDayEnded += OnDayEnded;
    }

    private void OnDisable()
    {
        // Unsubscribe from events
        HermitCrabAnimator.OnHermitFinishedReturning -= OnHermitReturned;
        TimeManager.OnDayEnded -= OnDayEnded;
    }

    private void OnHermitReturned()
    {
        Debug.Log("ResultsPanel: Hermit returned, showing results...");
        // Wait a moment then show results
        DOVirtual.DelayedCall(1f, ShowResults);
    }

    private void OnDayEnded(int day)
    {
        Debug.Log($"ResultsPanel: Day {day} ended, showing results...");
        // Alternative trigger if hermit event doesn't fire
        DOVirtual.DelayedCall(2f, ShowResults);
    }

    public void ShowResults()
    {
        Debug.Log("ResultsPanel: ShowResults called");

        if (TimeManager.Instance == null || ScoreManager.Instance == null)
        {
            Debug.LogWarning("ResultsPanel: TimeManager or ScoreManager not found!");
            return;
        }

        // IMPORTANT: Force score calculation at the END of day based on current positions
        // This ensures we score based on final positions, not intermediate placements
        if (ScoreManager.Instance != null)
        {
            Debug.Log("ResultsPanel: Forcing final score calculation...");
            ScoreManager.Instance.StartNewDay(); // Reset day scores first
            ScoreManager.Instance.ScoreAllItems(); // Calculate based on current positions
        }

        CollectResultsData();
        UpdateUI();
        AnimatePanel();
    }

    private void CollectResultsData()
    {
        currentDayResults.Clear();

        // Get current day info
        int currentDay = TimeManager.Instance.CurrentDay;

        // Get the current day score BEFORE finalizing it
        int currentDayScore = ScoreManager.Instance.GetCurrentDayScore();

        // Calculate max points for ONLY items spawned TODAY
        currentDayMaxPoints = CalculateCurrentDayMaxPointsOnly();

        // Set cumulative max points = current day max points (since it's the total for all items spawned so far)
        cumulativeMaxPoints = currentDayMaxPoints;

        Debug.Log($"Day {currentDay}: Current day max = {currentDayMaxPoints}, Cumulative max = {cumulativeMaxPoints}");

        // Get score breakdown from ScoreManager for current day only
        ScoreBreakdown breakdown = ScoreManager.Instance.GetScoreBreakdown();

        // Convert to our result format - only items from current day
        foreach (ItemScore itemScore in breakdown.itemScores)
        {
            DecoItemResult result = new DecoItemResult();
            result.itemName = itemScore.itemName;
            result.pointsEarned = itemScore.pointsAwarded;
            result.maxPossiblePoints = GetItemMaxPoints(itemScore.itemName);
            result.itemSprite = GetItemSprite(itemScore.itemName);

            currentDayResults.Add(result);
        }

        Debug.Log($"Results: Day {currentDay}, Current Day Score: {currentDayScore}, Current Day Max: {currentDayMaxPoints}, Cumulative Max: {cumulativeMaxPoints}");
    }

    private int CalculateCurrentDayMaxPointsOnly()
    {
        // Get all decoration items currently in the scene (this includes all items spawned so far)
        DecorationItem[] allItems = FindObjectsByType<DecorationItem>(FindObjectsSortMode.None);

        int maxPoints = 0;
        foreach (DecorationItem item in allItems)
        {
            if (item.ItemData != null)
            {
                maxPoints += CalculateMaxPointsForItem(item.ItemData);
            }
        }

        return maxPoints;
    }

    private int CalculateCurrentDayMaxPoints()
    {
        // Get all decoration items that were spawned today
        DecorationItem[] allItems = FindObjectsByType<DecorationItem>(FindObjectsSortMode.None);

        int maxPoints = 0;
        foreach (DecorationItem item in allItems)
        {
            if (item.ItemData != null)
            {
                maxPoints += GetItemMaxPoints(item.ItemData.itemName);
            }
        }

        return maxPoints;
    }

    private int CalculateTotalMaxPoints(int currentDay)
    {
        // This is a simplified calculation - in a real game you'd track this more precisely
        // Assuming each day has roughly the same max points potential
        return currentDayMaxPoints * currentDay;
    }

    private int GetItemMaxPoints(string itemName)
    {
        // Find the decoration item data and calculate its maximum possible points
        DecorationItem[] allItems = FindObjectsByType<DecorationItem>(FindObjectsSortMode.None);

        foreach (DecorationItem item in allItems)
        {
            if (item.ItemData != null && item.ItemData.itemName == itemName)
            {
                return CalculateMaxPointsForItem(item.ItemData);
            }
        }

        return 10; // Default fallback
    }

    private int CalculateMaxPointsForItem(DecorationItemData itemData)
    {
        int maxPoints = itemData.basePoints;

        // Check all placement preferences for the highest possible score
        foreach (var preference in itemData.placementPreferences)
        {
            if (preference.defaultAreaPoints > maxPoints)
            {
                maxPoints = preference.defaultAreaPoints;
            }

            foreach (var zone in preference.zones)
            {
                if (zone.pointValue > maxPoints)
                {
                    maxPoints = zone.pointValue;
                }
            }
        }

        return maxPoints;
    }

    private Sprite GetItemSprite(string itemName)
    {
        DecorationItem[] allItems = FindObjectsByType<DecorationItem>(FindObjectsSortMode.None);

        foreach (DecorationItem item in allItems)
        {
            if (item.ItemData != null && item.ItemData.itemName == itemName)
            {
                return item.ItemData.itemSprite;
            }
        }

        return null;
    }

    private void UpdateUI()
    {
        // Update day text
        if (dayText != null)
        {
            int currentDay = TimeManager.Instance.CurrentDay;
            dayText.text = $"Day {currentDay} of 3";
        }

        // Update total points text - use ONLY current day score
        if (totalPointsText != null)
        {
            int currentDayScore = ScoreManager.Instance.GetCurrentDayScore();
            totalPointsText.text = currentDayScore.ToString(); // Show only today's score
        }

        // Setup progress bar using cumulative max points
        if (progressBar != null)
        {
            progressBar.maxValue = cumulativeMaxPoints; // Use cumulative instead of calculated total
            progressBar.value = 0; // Start at 0 for animation
        }

        // Setup hearts and stars (initially hidden, will be shown during animation)
        SetHeartsState(false);
        SetStarsState(false);

        // Populate bottom container with item results
        PopulateItemResults();
    }

    private void PopulateItemResults()
    {
        if (bottomContainer == null || decoItemPrefab == null) return;

        // Clear existing items
        foreach (Transform child in bottomContainer)
        {
            Destroy(child.gameObject);
        }

        // Create item result entries
        for (int i = 0; i < currentDayResults.Count; i++)
        {
            DecoItemResult result = currentDayResults[i];

            GameObject itemObj = Instantiate(decoItemPrefab, bottomContainer);
            DecoItemResultUI itemUI = itemObj.GetComponent<DecoItemResultUI>();

            if (itemUI == null)
            {
                itemUI = itemObj.AddComponent<DecoItemResultUI>();
            }

            itemUI.SetupResult(result);

            // Initially hide for animation
            itemObj.SetActive(false);
        }
    }

    private void AnimatePanel()
    {
        if (resultsPanel == null) return;

        // Show panel
        resultsPanel.SetActive(true);

        // Get canvas group for fading
        CanvasGroup canvasGroup = resultsPanel.GetComponent<CanvasGroup>();
        if (canvasGroup == null)
        {
            canvasGroup = resultsPanel.AddComponent<CanvasGroup>();
        }

        // Start with transparent
        canvasGroup.alpha = 0f;

        // Fade in panel
        canvasGroup.DOFade(1f, panelFadeInDuration).OnComplete(() =>
        {
            AnimateProgressBar();
        });
    }

    private void AnimateProgressBar()
    {
        if (progressBar == null) return;

        // Use the cumulative total score (all days so far) for the progress bar animation
        int cumulativeTotalScore = ScoreManager.Instance.GetTotalGameScore() + ScoreManager.Instance.GetCurrentDayScore();
        int rightScore = ScoreManager.Instance.GetCurrentDayScore();

        Debug.Log($"Animating progress bar to {cumulativeTotalScore} out of {progressBar.maxValue} (cumulative)");
        Debug.Log($"<color=cyan>Animating progress bar to {rightScore} out of {progressBar.maxValue} (cumulative)</color>");

        // Animate progress bar to cumulative total score
        progressBar.DOValue(rightScore, progressBarAnimationDuration)
            .SetEase(progressBarEase)
            .OnUpdate(() =>
            {
                UpdateProgressBarColor();
                UpdateHeartsAndStars();
            })
            .OnComplete(() =>
            {
                AnimateItemResults();
            });
    }

    private void UpdateProgressBarColor()
    {
        if (progressBar == null || progressBarFill == null) return;

        float percentage = progressBar.value / progressBar.maxValue;

        Color targetColor;
        if (percentage >= 1f)
        {
            targetColor = greenColor;
        }
        else if (percentage >= 0.5f)
        {
            targetColor = orangeColor;
        }
        else
        {
            targetColor = greyColor;
        }

        progressBarFill.color = targetColor;
    }

    private void UpdateHeartsAndStars()
    {
        if (progressBar == null) return;

        float percentage = progressBar.value / progressBar.maxValue;

        // Update hearts at 50%
        if (percentage >= 0.5f)
        {
            SetHeartsState(true);
            
        }

        // Update stars at 100%
        if (percentage >= 1f)
        {
            SetStarsState(true);
            
        }
    }

    private void SetHeartsState(bool full)
    {
        if (heartsEmpty != null) heartsEmpty.SetActive(!full);
        if (heartsFull != null) heartsFull.SetActive(full);
        SoundManager.Instance.PlaySFX(SoundEffect.Hearts);
    }

    private void SetStarsState(bool full)
    {
        if (starsEmpty != null) starsEmpty.SetActive(!full);
        if (starsFull != null) starsFull.SetActive(full);
        SoundManager.Instance.PlaySFX(SoundEffect.Stars);
    }

    private void AnimateItemResults()
    {
        if (bottomContainer == null) return;

        // Animate each item appearing with a delay
        for (int i = 0; i < bottomContainer.childCount; i++)
        {
            GameObject itemObj = bottomContainer.GetChild(i).gameObject;

            DOVirtual.DelayedCall(i * itemSpawnDelay, () =>
            {
                if (itemObj != null)
                {
                    itemObj.SetActive(true);

                    // Scale animation
                    itemObj.transform.localScale = Vector3.zero;
                    itemObj.transform.DOScale(Vector3.one, 0.3f).SetEase(Ease.OutBack);
                    SoundManager.Instance.PlaySFX(SoundEffect.ResultSpawn);
                }
            });
        }
    }

    private void OnContinueButtonClicked()
    {
        HideResults();
    }

    public void HideResults()
    {
        if (resultsPanel == null) return;

        CanvasGroup canvasGroup = resultsPanel.GetComponent<CanvasGroup>();
        if (canvasGroup != null)
        {
            canvasGroup.DOFade(0f, 0.3f).OnComplete(() =>
            {
                resultsPanel.SetActive(false);

                // Continue to next day or end game
                HandleContinueLogic();
            });
        }
        else
        {
            resultsPanel.SetActive(false);
            HandleContinueLogic();
        }
    }

    private void HandleContinueLogic()
    {
        // Finalize the day score
        if (ScoreManager.Instance != null)
        {
            ScoreManager.Instance.FinalizeDayScore();
        }

        // Check if game is complete or continue to next day
        if (TimeManager.Instance != null)
        {
            int currentDay = TimeManager.Instance.CurrentDay;

            if (currentDay >= 3) // Assuming 3 days total
            {
                // Game complete - show final results screen
                Debug.Log("Game Complete! Showing final results...");
                ShowFinalResultsScreen();
            }
            else
            {
                // Start next day but don't start time yet - just trigger hermit leaving
                Debug.Log("Continuing to next day...");
                StartNextDay();
            }
        }
    }

    private void ShowFinalResultsScreen()
    {
        // Use the cumulative max points we've been tracking
        int totalScore = ScoreManager.Instance.GetCurrentDayScore();
        int totalPossibleScore = cumulativeMaxPoints;

        // Show final results panel
        if (FinalResultsPanel.Instance != null)
        {
            FinalResultsPanel.Instance.ShowFinalResults(totalScore, totalPossibleScore);
        }
        else
        {
            Debug.LogWarning("FinalResultsPanel not found! Add it to the scene.");
            // Fallback - just log the results
            Debug.Log($"GAME COMPLETE! Final Score: {totalScore} / {totalPossibleScore} ({(float)totalScore / totalPossibleScore * 100:F1}%)");
        }
    }

    public void ResetCumulativeTracking()
    {
        cumulativeMaxPoints = 0;
        Debug.Log("ResultsPanel: Reset cumulative max points tracking");
    }

    private int CalculateTotalGameMaxPoints()
    {
        // This should calculate the maximum possible points across all 3 days
        // For now, using a simple calculation - you might want to make this more sophisticated

        // Assuming each day has roughly the same potential
        // In a real implementation, you'd track this more precisely
        int estimatedMaxPerDay = 60; // Based on your game design
        return estimatedMaxPerDay * 3; // 3 days total
    }

    private void StartNextDay()
    {
        /// Tell TimeManager to prepare next day but don't start time
        if (TimeManager.Instance != null)
        {
            TimeManager.Instance.PrepareNextDay();
        }

        // Only show instructions on Day 1, for other days start directly
        int currentDay = TimeManager.Instance != null ? TimeManager.Instance.CurrentDay : 1;

        // Reset cumulative tracking if this is day 1 (new game)
        if (currentDay == 1)
        {
            ResetCumulativeTracking();
        }

        if (currentDay == 1 && InstructionPanel.Instance != null)
        {
            // Day 1 - show instructions
            InstructionPanel.Instance.ShowInstructions();
        }
        else
        {
            // Day 2+ - skip instructions and start hermit leaving directly
            StartHermitLeavingForNewDay();
        }
    }

    private void StartHermitLeavingForNewDay()
    {
        // Start time which will trigger hermit leaving sequence
        if (TimeManager.Instance != null)
        {
            TimeManager.Instance.StartTime();
        }
    }

    // Public method for manual testing
    [ContextMenu("Test Show Results")]
    public void TestShowResults()
    {
        Debug.Log("ResultsPanel: Manual test triggered");
        ShowResults();
    }

    // Public method to force show results (can be called from other scripts)
    public static void ForceShowResults()
    {
        if (Instance != null)
        {
            Debug.Log("ResultsPanel: Force show results called");
            Instance.ShowResults();
        }
        else
        {
            Debug.LogWarning("ResultsPanel: No instance found to show results");
        }
    }

    private void Update()
    {
        // Debug key to manually trigger results panel
        if (Input.GetKeyDown(KeyCode.P))
        {
            Debug.Log("ResultsPanel: Manual trigger via P key");
            ShowResults();
        }
    }
}

[System.Serializable]
public class DecoItemResult
{
    public string itemName;
    public int pointsEarned;
    public int maxPossiblePoints;
    public Sprite itemSprite;

    public float GetPercentage()
    {
        if (maxPossiblePoints == 0) return 0f;
        return (float)pointsEarned / maxPossiblePoints;
    }
}