using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using TMPro;

public class ScoreUIManager : MonoBehaviour
{   
    public static ScoreUIManager Instance { get; private set;}
    [Header("UI References")]
    [SerializeField] private TextMeshProUGUI currentScoreText;
    [SerializeField] private GameObject scorePopupPrefab;
    [SerializeField] private Transform scorePopupParent;
    
    [Header("Day End UI")]
    [SerializeField] private GameObject dayEndPanel;
    [SerializeField] private TextMeshProUGUI dayEndScoreText;
    [SerializeField] private TextMeshProUGUI choresCompletedText;
    [SerializeField] private TextMeshProUGUI decorationPointsText;
    [SerializeField] private TextMeshProUGUI themeBonusText;
    [SerializeField] private TextMeshProUGUI totalScoreText;
    
    [Header("Game Over UI")]
    [SerializeField] private GameObject gameOverPanel;
    [SerializeField] private TextMeshProUGUI finalScoreText;
    [SerializeField] private TextMeshProUGUI finalRankText;
    [SerializeField] private Image hermitReactionImage;
    [SerializeField] private Sprite[] hermitReactionSprites; // From worst to best
    
    // Reference to score manager
    private ScoreManager scoreManager;
    
    private void Start()
    {
        // Find score manager
        scoreManager = ScoreManager.Instance;
        
        // Hide panels initially
        if (dayEndPanel != null) dayEndPanel.SetActive(false);
        if (gameOverPanel != null) gameOverPanel.SetActive(false);

        // Subscribe to events
        TimeController timeController = TimeController.Instance;
        if (timeController != null)
        {
            timeController.onDayEnd += ShowDayEndScreen;
            timeController.onGameOver += ShowGameOverScreen;
        }
        
        // Update initial score text
        UpdateScoreText(0);
    }
    
    // Update the current score display
    public void UpdateScoreText(int score)
    {
        if (currentScoreText != null)
        {
            currentScoreText.text = $"Score: {score}";
        }
    }
    
    // Create a score popup at a world position
    public void CreateScorePopup(Vector3 worldPosition, int points, Color color)
    {
        if (scorePopupPrefab == null || scorePopupParent == null) return;
        
        // Create popup
        GameObject popup = Instantiate(scorePopupPrefab, scorePopupParent);
        
        // Position it in world space
        RectTransform rectTransform = popup.GetComponent<RectTransform>();
        if (rectTransform != null)
        {
            // Convert world position to screen position
            Vector3 screenPos = Camera.main.WorldToScreenPoint(worldPosition);
            rectTransform.position = screenPos;
        }
        
        // Set the text
        TextMeshProUGUI popupText = popup.GetComponent<TextMeshProUGUI>();
        if (popupText != null)
        {
            string prefix = points >= 0 ? "+" : "";
            popupText.text = $"{prefix}{points}";
            popupText.color = color;
        }
        
        // Destroy after animation
        Destroy(popup, 2f);
    }
    
    // Show the day end screen with score breakdown
    private void ShowDayEndScreen()
    {
        if (dayEndPanel == null) return;
        
        // Get score data from score manager
        int choresScore = 0;
        int decorationsScore = 0;
        int themeBonus = 0;
        int totalDayScore = 0;
        
        // In a real implementation, you'd get these values from the score manager
        if (scoreManager != null)
        {
            choresScore = Random.Range(10, 50) * 10; // Placeholder
            decorationsScore = Random.Range(5, 20) * 10; // Placeholder
            themeBonus = Random.Range(0, 5) * 10; // Placeholder
            totalDayScore = choresScore + decorationsScore + themeBonus;
        }
        
        // Update UI
        if (choresCompletedText != null)
            choresCompletedText.text = $"Chores Completed: {choresScore}";
            
        if (decorationPointsText != null)
            decorationPointsText.text = $"Decoration Points: {decorationsScore}";
            
        if (themeBonusText != null)
            themeBonusText.text = $"Theme Bonus: {themeBonus}";
            
        if (totalScoreText != null)
            totalScoreText.text = $"Today's Total: {totalDayScore}";
            
        // Show the panel
        dayEndPanel.SetActive(true);
    }
    
    // Show the game over screen with final score
    private void ShowGameOverScreen()
    {
        if (gameOverPanel == null) return;
        
        // Get final score from score manager
        int finalScore = 0;
        string rank = "Tidy Hermit";
        
        // In a real implementation, you'd get these values from the score manager
        if (scoreManager != null)
        {
            finalScore = Random.Range(500, 1000); // Placeholder
            
            // Determine rank based on score
            if (finalScore < 300)
                rank = "Messy Hermit";
            else if (finalScore < 600)
                rank = "Tidy Hermit";
            else if (finalScore < 900)
                rank = "Clean Hermit";
            else
                rank = "Perfect Hermit";
        }
        
        // Update UI
        if (finalScoreText != null)
            finalScoreText.text = $"Final Score: {finalScore}";
            
        if (finalRankText != null)
            finalRankText.text = $"Rank: {rank}";
            
        // Set hermit reaction image based on score
        if (hermitReactionImage != null && hermitReactionSprites != null && hermitReactionSprites.Length > 0)
        {
            // Calculate which reaction to show (0 = worst, length-1 = best)
            int reactionIndex = 0;
            
            if (finalScore >= 900)
                reactionIndex = hermitReactionSprites.Length - 1; // Best
            else if (finalScore >= 600)
                reactionIndex = hermitReactionSprites.Length - 2; // Second best
            else if (finalScore >= 300)
                reactionIndex = 1; // Second worst
            else
                reactionIndex = 0; // Worst
                
            // Make sure index is valid
            reactionIndex = Mathf.Clamp(reactionIndex, 0, hermitReactionSprites.Length - 1);
            
            // Set the sprite
            hermitReactionImage.sprite = hermitReactionSprites[reactionIndex];
        }
        
        // Show the panel
        gameOverPanel.SetActive(true);
    }
    
    // UI Button handlers
    
    public void ContinueToNextDay()
    {
        // Hide day end panel
        if (dayEndPanel != null) dayEndPanel.SetActive(false);
    }
    
    public void RestartGame()
    {
        // Hide game over panel
        if (gameOverPanel != null) gameOverPanel.SetActive(false);
        
        // Reload the scene
        UnityEngine.SceneManagement.SceneManager.LoadScene(
            UnityEngine.SceneManagement.SceneManager.GetActiveScene().name
        );
    }
}