using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;
using UnityEngine.SceneManagement;

public class FinalResultsPanel : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private GameObject finalResultsPanel;
    [SerializeField] private TextMeshProUGUI thankYouText;
    [SerializeField] private TextMeshProUGUI finalScoreText;
    [SerializeField] private TextMeshProUGUI totalPossibleText;
    [SerializeField] private TextMeshProUGUI percentageText;
    [SerializeField] private TextMeshProUGUI gradeText;
    [SerializeField] private Image gradeIcon;
    // [SerializeField] private Button playAgainButton;
    [SerializeField] private Button quitButton;

    [Header("Grade Settings")]
    [SerializeField] private Color perfectGradeColor = Color.green;
    [SerializeField] private Color goodGradeColor = Color.blue;
    [SerializeField] private Color okayGradeColor = Color.yellow;
    [SerializeField] private Color poorGradeColor = Color.red;

    [Header("Grade Sprites")]
    [SerializeField] private Sprite starSprite; // For perfect scores
    [SerializeField] private Sprite heartSprite; // For good scores
    [SerializeField] private Sprite thumbsUpSprite; // For okay scores
    [SerializeField] private Sprite neutralSprite; // For poor scores

    [Header("Grade Sprites - Hermit Emotions")]
    [SerializeField] private Sprite happyHermitSprite; // For good scores (80%+)
    [SerializeField] private Sprite neutralHermitSprite; // For okay scores (60-79%)
    [SerializeField] private Sprite sadHermitSprite; // For poor scores (<60%)

    [Header("Speech Bubble")]
    [SerializeField] private GameObject speechBubble; // Speech bubble image
    [SerializeField] private float hermitAppearDuration = 0.8f;
    [SerializeField] private float speechBubbleDelay = 0.5f;
    [SerializeField] private float speechBubbleAppearDuration = 0.6f;
    [SerializeField] private float textStartDelay = 0.8f;

    [Header("Animation Settings")]
    [SerializeField] private float panelFadeInDuration = 1f;
    [SerializeField] private float scoreCountDuration = 2f;
    [SerializeField] private float textAppearDelay = 0.5f;
    [SerializeField] private Ease fadeEase = Ease.OutQuad;

    [Header("Messages")]
    [SerializeField] private string perfectMessage = "Perfect! The hermit crab loves their new home!";
    [SerializeField] private string goodMessage = "Great job! The hermit crab is very happy!";
    [SerializeField] private string okayMessage = "Not bad! The hermit crab likes their home.";
    [SerializeField] private string poorMessage = "The hermit crab appreciates your effort!";

    [Header("Scene Management")]
    [SerializeField] private string titleSceneName = "TitleScreen"; // Or whatever your title scene is called
    [SerializeField] private bool useLoadingScreen = true;

    // Singleton
    public static FinalResultsPanel Instance { get; private set; }

    private int finalScore = 0;
    private int totalPossibleScore = 0;

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
        if (finalResultsPanel != null)
        {
            finalResultsPanel.SetActive(false);
        }

        // Setup buttons
        // if (playAgainButton != null)
        // {
        //     playAgainButton.onClick.AddListener(OnPlayAgainClicked);
        // }

        if (quitButton != null)
        {
            quitButton.onClick.AddListener(OnQuitClicked);
        }
    }

    public void ShowFinalResults(int totalScore, int maxPossibleScore)
    {
        finalScore = totalScore;
        totalPossibleScore = maxPossibleScore;

        Debug.Log($"Final Results: {finalScore} / {totalPossibleScore}");

        SetupUI();
        AnimatePanel();
    }

    private void SetupUI()
    {
        // Calculate percentage
        float percentage = totalPossibleScore > 0 ? (float)finalScore / totalPossibleScore : 0f;

        // Determine grade and message based on hermit emotions
        string grade;
        string message;
        Color gradeColor;
        Sprite hermitSprite;

        if (percentage >= 0.80f) // 80% or higher - Happy hermit
        {
            grade = percentage >= 0.95f ? "PERFECT!" : "EXCELLENT!";
            message = percentage >= 0.95f ? perfectMessage : goodMessage;
            gradeColor = percentage >= 0.95f ? perfectGradeColor : goodGradeColor;
            hermitSprite = happyHermitSprite;
        }
        else if (percentage >= 0.60f) // 60-79% - Neutral hermit
        {
            grade = "GOOD!";
            message = okayMessage;
            gradeColor = okayGradeColor;
            hermitSprite = neutralHermitSprite;
        }
        else // Below 60% - Sad hermit
        {
            grade = "KEEP TRYING!";
            message = poorMessage;
            gradeColor = poorGradeColor;
            hermitSprite = sadHermitSprite;
        }

        // Set thank you message
        if (thankYouText != null)
        {
            thankYouText.text = $"Thanks for playing!\n{message}";
        }

        // Set score texts (will be animated)
        if (finalScoreText != null)
        {
            finalScoreText.text = "0";
        }

        if (totalPossibleText != null)
        {
            totalPossibleText.text = $"/ {totalPossibleScore}";
        }

        if (percentageText != null)
        {
            percentageText.text = "0%";
        }

        if (gradeText != null)
        {
            gradeText.text = grade;
            gradeText.color = gradeColor;
        }

        // Set hermit sprite
        if (gradeIcon != null && hermitSprite != null)
        {
            gradeIcon.sprite = hermitSprite;
            gradeIcon.color = Color.white; // Keep hermit natural color
        }

        // Initially hide ALL animated elements
        if (gradeIcon != null) gradeIcon.gameObject.SetActive(false);
        if (speechBubble != null) speechBubble.SetActive(false);
        if (thankYouText != null) thankYouText.gameObject.SetActive(false);
        if (finalScoreText != null) finalScoreText.gameObject.SetActive(false);
        if (totalPossibleText != null) totalPossibleText.gameObject.SetActive(false);
        if (percentageText != null) percentageText.gameObject.SetActive(false);
        if (gradeText != null) gradeText.gameObject.SetActive(false);
        // if (playAgainButton != null) playAgainButton.gameObject.SetActive(false);
        if (quitButton != null) quitButton.gameObject.SetActive(false);
    }

    private void AnimatePanel()
    {
        if (finalResultsPanel == null) return;

        // Show panel
        finalResultsPanel.SetActive(true);

        // Get canvas group for fading
        CanvasGroup canvasGroup = finalResultsPanel.GetComponent<CanvasGroup>();
        if (canvasGroup == null)
        {
            canvasGroup = finalResultsPanel.AddComponent<CanvasGroup>();
        }

        // Start with transparent
        canvasGroup.alpha = 0f;

        // Fade in panel
        canvasGroup.DOFade(1f, panelFadeInDuration)
            .SetEase(fadeEase)
            .OnComplete(() =>
            {
                AnimateScoreReveal();
            });
    }

    private void AnimateScoreReveal()
    {
        Sequence revealSequence = DOTween.Sequence();

        // 1. Show hermit with scale animation
        revealSequence.AppendCallback(() =>
        {
            if (gradeIcon != null)
            {
                gradeIcon.gameObject.SetActive(true);
                gradeIcon.transform.localScale = Vector3.zero;
            }
        });

        if (gradeIcon != null)
        {
            revealSequence.Append(
                gradeIcon.transform.DOScale(Vector3.one, hermitAppearDuration)
                .SetEase(Ease.OutBounce)
            );
        }

        // 2. Wait a bit, then show speech bubble
        revealSequence.AppendInterval(speechBubbleDelay);

        revealSequence.AppendCallback(() =>
        {
            if (speechBubble != null)
            {
                speechBubble.SetActive(true);
                speechBubble.transform.localScale = Vector3.zero;
            }
        });

        if (speechBubble != null)
        {
            revealSequence.Append(
                speechBubble.transform.DOScale(Vector3.one, speechBubbleAppearDuration)
                .SetEase(Ease.OutBack)
            );
        }

        // 3. Wait a bit, then start showing text elements
        revealSequence.AppendInterval(textStartDelay);

        // 4. Show thank you message first
        revealSequence.AppendCallback(() =>
        {
            if (thankYouText != null)
            {
                thankYouText.gameObject.SetActive(true);
                thankYouText.transform.localScale = Vector3.zero;
                thankYouText.transform.DOScale(Vector3.one, 0.4f).SetEase(Ease.OutQuad);
            }
        });

        revealSequence.AppendInterval(textAppearDelay);

        // 5. Show and animate final score
        revealSequence.AppendCallback(() =>
        {
            if (finalScoreText != null) finalScoreText.gameObject.SetActive(true);
        });

        // Count up the score
        if (finalScoreText != null)
        {
            int startScore = 0;
            revealSequence.Append(
                DOTween.To(() => startScore, x =>
                {
                    startScore = x;
                    finalScoreText.text = startScore.ToString();
                }, finalScore, scoreCountDuration)
                .SetEase(Ease.OutQuad)
            );
        }

        // 6. Show total possible score
        revealSequence.AppendCallback(() =>
        {
            if (totalPossibleText != null) totalPossibleText.gameObject.SetActive(true);
        });

        revealSequence.AppendInterval(textAppearDelay);

        // 7. Show and animate percentage
        revealSequence.AppendCallback(() =>
        {
            if (percentageText != null) percentageText.gameObject.SetActive(true);
        });

        if (percentageText != null)
        {
            float targetPercentage = totalPossibleScore > 0 ? (float)finalScore / totalPossibleScore * 100f : 0f;
            float currentPercentage = 0f;

            revealSequence.Append(
                DOTween.To(() => currentPercentage, x =>
                {
                    currentPercentage = x;
                    percentageText.text = $"{currentPercentage:F0}%";
                }, targetPercentage, 1f)
                .SetEase(Ease.OutQuad)
            );
        }

        revealSequence.AppendInterval(textAppearDelay);

        // 8. Show grade text
        revealSequence.AppendCallback(() =>
        {
            if (gradeText != null)
            {
                gradeText.gameObject.SetActive(true);
                gradeText.transform.localScale = Vector3.zero;
                gradeText.transform.DOScale(Vector3.one, 0.5f).SetEase(Ease.OutBack);
            }
        });

        revealSequence.AppendInterval(1f);

        // 9. Finally show buttons
        revealSequence.AppendCallback(() =>
        {
            // if (playAgainButton != null)
            // {
            //     playAgainButton.gameObject.SetActive(true);
            //     playAgainButton.transform.localScale = Vector3.zero;
            //     playAgainButton.transform.DOScale(Vector3.one, 0.3f).SetEase(Ease.OutBack);
            // }

            if (quitButton != null)
            {
                quitButton.gameObject.SetActive(true);
                quitButton.transform.localScale = Vector3.zero;
                quitButton.transform.DOScale(Vector3.one, 0.3f).SetEase(Ease.OutBack).SetDelay(0.1f);
            }
        });

        revealSequence.Play();
    }

    // private void OnPlayAgainClicked()
    // {
    //     Debug.Log("Play Again clicked - Restarting scene");

    //     // Fade out and restart scene
    //     CanvasGroup canvasGroup = finalResultsPanel.GetComponent<CanvasGroup>();
    //     if (canvasGroup != null)
    //     {
    //         canvasGroup.DOFade(0f, 0.5f).OnComplete(() =>
    //         {
    //             RestartCurrentScene();
    //         });
    //     }
    //     else
    //     {
    //         RestartCurrentScene();
    //     }
    // }

    private void OnQuitClicked()
    {
        Debug.Log("Back to Title clicked");

        // Fade out and go to title screen
        CanvasGroup canvasGroup = finalResultsPanel.GetComponent<CanvasGroup>();
        if (canvasGroup != null)
        {
            canvasGroup.DOFade(0f, 0.5f).OnComplete(() =>
            {
                GoToTitleScreen();
            });
        }
        else
        {
            GoToTitleScreen();
        }
    }

    // Update the RestartGame method to restart scene
    // private void RestartCurrentScene()
    // {
    //     string currentSceneName = SceneManager.GetActiveScene().name;

    //     if (useLoadingScreen && LoadingManager.Instance != null)
    //     {
    //         // Use loading screen for smoother transition
    //         LoadingManager.Instance.StartLoading(currentSceneName, 2f, currentSceneName);
    //     }
    //     else
    //     {
    //         // Direct scene reload
    //         SceneManager.LoadScene(currentSceneName);
    //     }
    // }

    // Add new method to go to title screen
    private void GoToTitleScreen()
    {
        
            // Direct scene load
            SceneManager.LoadScene(titleSceneName);
        
    }

    private void RestartGame()
    {
        // Reset managers
        if (TimeManager.Instance != null)
        {
            TimeManager.Instance.StartNewGame();
        }

        if (ScoreManager.Instance != null)
        {
            // You might need to add a reset method to ScoreManager
            // ScoreManager.Instance.ResetGame();
        }

        // Hide this panel
        if (finalResultsPanel != null)
        {
            finalResultsPanel.SetActive(false);
        }

        // Optionally reload the scene
        // SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);

        Debug.Log("Game restarted!");
    }

    // Public method to hide the panel
    public void HideFinalResults()
    {
        if (finalResultsPanel != null)
        {
            finalResultsPanel.SetActive(false);
        }
    }

    // Public static method for easy access
    public static void ShowResults(int totalScore, int maxPossibleScore)
    {
        if (Instance != null)
        {
            Instance.ShowFinalResults(totalScore, maxPossibleScore);
        }
        else
        {
            Debug.LogWarning("FinalResultsPanel: No instance found!");
        }
    }
}