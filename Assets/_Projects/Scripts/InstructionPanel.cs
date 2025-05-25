using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;

public class InstructionPanel : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private GameObject instructionCanvas;
    [SerializeField] private CanvasGroup instructionCanvasGroup;
    [SerializeField] private Button readyButton;
    [SerializeField] private TextMeshProUGUI readyButtonText;
    [SerializeField] private TextMeshProUGUI pageOneInstruction;

    [Header("Page System")]
    [SerializeField] private GameObject[] instructionPages; // Array of page GameObjects
    [SerializeField] private Button nextButton;
    [SerializeField] private Button previousButton;
    [SerializeField] private TextMeshProUGUI pageIndicatorText; // Optional: "Page 1 of 2"

    [Header("Animation Settings")]
    [SerializeField] private float fadeInDuration = 1f;
    [SerializeField] private float fadeOutDuration = 0.5f;
    [SerializeField] private Ease fadeEase = Ease.OutQuad;

    [Header("Page Animation Settings")]
    [SerializeField] private float pageTransitionDuration = 0.3f;
    [SerializeField] private Ease pageTransitionEase = Ease.OutQuad;

    [Header("Content Settings")]
    [SerializeField] private bool showOnlyOnDay1 = true;
    [SerializeField] private string readyButtonLabel = "Ready!";

    // Events
    public static System.Action OnInstructionsCompleted;

    // Singleton for easy access
    public static InstructionPanel Instance { get; private set; }

    private const string INSTRUCTION_STRING = "Help Hermit unpack items from boxes and <color=cyan>place them at his preferred areas</color>. He comes home at <color=cyan>5 o'clock</color> to rest, so you'll have to stop decorating by then. You'll have <color=cyan>3 days</color> to decorate.";

    private bool hasShownInstructions = false;
    private int currentPageIndex = 0;
    private bool isTransitioning = false;

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

        // Ensure we have a canvas group
        if (instructionCanvasGroup == null && instructionCanvas != null)
        {
            instructionCanvasGroup = instructionCanvas.GetComponent<CanvasGroup>();
            if (instructionCanvasGroup == null)
            {
                instructionCanvasGroup = instructionCanvas.AddComponent<CanvasGroup>();
            }
        }

        // Setup ready button
        if (readyButton != null)
        {
            readyButton.onClick.AddListener(OnReadyButtonClicked);
        }

        // Setup page system
        SetupPageSystem();

        // Set initial state - hidden
        if (instructionCanvas != null)
        {
            instructionCanvas.SetActive(false);
        }
        if (instructionCanvasGroup != null)
        {
            instructionCanvasGroup.alpha = 0f;
            instructionCanvasGroup.blocksRaycasts = false;
        }
    }

    private void OnEnable()
    {
        // Subscribe to day started events
        TimeManager.OnDayStarted += OnDayStarted;
    }

    private void OnDisable()
    {
        // Unsubscribe from events
        TimeManager.OnDayStarted -= OnDayStarted;
    }

    private void Start()
    {
        // Check if we should show instructions immediately
        if (ShouldShowInstructions())
        {
            ShowInstructions();
        }

    }

    private void SetupPageSystem()
    {
        // Setup navigation buttons
        if (nextButton != null)
        {
            nextButton.onClick.AddListener(NextPage);
        }

        if (previousButton != null)
        {
            previousButton.onClick.AddListener(PreviousPage);
        }

        // Initialize pages - hide all except first
        for (int i = 0; i < instructionPages.Length; i++)
        {
            if (instructionPages[i] != null)
            {
                instructionPages[i].SetActive(i == 0);

                // Ensure each page has a CanvasGroup for animations
                CanvasGroup pageCanvasGroup = instructionPages[i].GetComponent<CanvasGroup>();
                if (pageCanvasGroup == null)
                {
                    pageCanvasGroup = instructionPages[i].AddComponent<CanvasGroup>();
                }

                // Set initial alpha
                pageCanvasGroup.alpha = (i == 0) ? 1f : 0f;
            }
        }

        UpdatePageIndicator();
        UpdateNavigationButtons();
    }

    private void OnDayStarted(int day)
    {
        if (ShouldShowInstructions(day))
        {
            ShowInstructions();
        }
    }

    private bool ShouldShowInstructions(int? day = null)
    {
        int currentDay = day ?? TimeManager.Instance?.CurrentDay ?? 1;

        // Only show on day 1 if the setting is enabled
        if (showOnlyOnDay1 && currentDay > 1)
        {
            return false;
        }

        // Don't show again if already shown
        if (hasShownInstructions && showOnlyOnDay1)
        {
            return false;
        }

        return true;
    }

    public void ShowInstructions()
    {
        int currentDay = TimeManager.Instance?.CurrentDay ?? 1;
        if (showOnlyOnDay1 && currentDay > 1)
        {
            Debug.Log($"InstructionPanel: Skipping instructions for Day {currentDay}");
            OnInstructionsCompleted?.Invoke(); // Skip directly to game start
            return;
        }
        if (instructionCanvas == null) return;

        hasShownInstructions = true;

        // Reset to first page
        currentPageIndex = 0;

        // Setup pages if not already done
        if (instructionPages != null && instructionPages.Length > 0)
        {
            SetupPageSystem();
        }

        // Setup ready button text
        if (readyButtonText != null)
        {
            readyButtonText.text = readyButtonLabel;
        }

        // Make sure time is stopped while showing instructions
        if (TimeManager.Instance != null)
        {
            TimeManager.Instance.StopTime();
        }

        // Show and fade in
        instructionCanvas.SetActive(true);

        if (instructionCanvasGroup != null)
        {
            instructionCanvasGroup.alpha = 0f;
            instructionCanvasGroup.blocksRaycasts = true;

            instructionCanvasGroup.DOFade(1f, fadeInDuration)
                .SetEase(fadeEase)
                .OnComplete(() =>
                {
                    Debug.Log("Instructions shown with page system");
                });
        }

        // Focus the appropriate button
        if (instructionPages.Length > 1 && nextButton != null)
        {
            nextButton.Select();
        }
        else if (readyButton != null)
        {
            readyButton.Select();
        }
    }

    public void NextPage()
    {
        if (isTransitioning || currentPageIndex >= instructionPages.Length - 1) return;

        TransitionToPage(currentPageIndex + 1);
        SoundManager.Instance.PlaySFX(SoundEffect.PressButton);
    }

    public void PreviousPage()
    {
        if (isTransitioning || currentPageIndex <= 0) return;

        TransitionToPage(currentPageIndex - 1);
        SoundManager.Instance.PlaySFX(SoundEffect.PressButton);
    }

    private void TransitionToPage(int targetPageIndex)
    {
        if (targetPageIndex < 0 || targetPageIndex >= instructionPages.Length) return;
        if (isTransitioning) return;

        isTransitioning = true;

        GameObject currentPage = instructionPages[currentPageIndex];
        GameObject targetPage = instructionPages[targetPageIndex];

        // Get canvas groups
        CanvasGroup currentCanvasGroup = currentPage.GetComponent<CanvasGroup>();
        CanvasGroup targetCanvasGroup = targetPage.GetComponent<CanvasGroup>();

        // Prepare target page
        targetPage.SetActive(true);
        targetCanvasGroup.alpha = 0f;

        // Create animation sequence
        Sequence pageTransition = DOTween.Sequence();

        // Fade out current page
        pageTransition.Append(currentCanvasGroup.DOFade(0f, pageTransitionDuration * 0.5f).SetEase(pageTransitionEase));

        // Fade in target page
        pageTransition.Append(targetCanvasGroup.DOFade(1f, pageTransitionDuration * 0.5f).SetEase(pageTransitionEase));

        // On completion
        pageTransition.OnComplete(() =>
        {
            // Hide the old page
            currentPage.SetActive(false);
            currentCanvasGroup.alpha = 1f; // Reset alpha for future use

            // Update current page index
            currentPageIndex = targetPageIndex;

            // Update UI
            UpdatePageIndicator();
            UpdateNavigationButtons();

            isTransitioning = false;
        });

        pageTransition.Play();
    }

    private void UpdatePageIndicator()
    {
        if (pageIndicatorText != null && instructionPages.Length > 1)
        {
            pageIndicatorText.text = $"Page {currentPageIndex + 1} of {instructionPages.Length}";
        }
    }

    private void UpdateNavigationButtons()
    {
        if (previousButton != null)
        {
            previousButton.interactable = currentPageIndex == 1;

        }

        if (nextButton != null)
        {
            nextButton.interactable = currentPageIndex == 0;
        }

        // Update ready button visibility - only show on last page
        if (readyButton != null)
        {
            readyButton.gameObject.SetActive(currentPageIndex == instructionPages.Length - 1);
        }
        Debug.Log($"current page index is {currentPageIndex}");
    }

    private void OnReadyButtonClicked()
    {
        HideInstructions();
        SoundManager.Instance.PlaySFX(SoundEffect.PressButton);
    }

    public void HideInstructions()
    {
        if (instructionCanvasGroup == null) return;

        instructionCanvasGroup.DOFade(0f, fadeOutDuration)
            .SetEase(fadeEase)
            .OnComplete(() =>
            {
                if (instructionCanvas != null)
                {
                    instructionCanvas.SetActive(false);
                }

                instructionCanvasGroup.blocksRaycasts = false;

                // Notify that instructions are completed
                OnInstructionsCompleted?.Invoke();

                // Start the time/game
                if (TimeManager.Instance != null)
                {
                    TimeManager.Instance.StartTime();
                }

                Debug.Log("Instructions hidden - Game started!");
            });
    }

    // Public methods for manual control
    public void ForceShowInstructions()
    {
        hasShownInstructions = false;
        ShowInstructions();
    }

    public void ForceHideInstructions()
    {
        HideInstructions();
    }

    // Handle keyboard shortcuts
    private void Update()
    {
        // Allow Enter or Space to close instructions (only on last page)
        if (instructionCanvas != null && instructionCanvas.activeInHierarchy)
        {
            if (currentPageIndex == instructionPages.Length - 1)
            {
                if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.Space))
                {
                    OnReadyButtonClicked();
                }
            }

            // Arrow key navigation
            if (Input.GetKeyDown(KeyCode.RightArrow) || Input.GetKeyDown(KeyCode.D))
            {
                NextPage();
            }

            if (Input.GetKeyDown(KeyCode.LeftArrow) || Input.GetKeyDown(KeyCode.A))
            {
                PreviousPage();
            }

            // Allow Escape to close (from any page)
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                OnReadyButtonClicked();
            }
        }
    }
}