using UnityEngine;

public class GameController : MonoBehaviour
{
    [Header("Game Flow Settings")]
    [SerializeField] private bool autoStartOnSceneLoad = true;
    [SerializeField] private float delayBeforeInstructions = 0.5f;

    [Header("Debug")]
    [SerializeField] private bool showDebugLogs = true;

    // Singleton
    public static GameController Instance { get; private set; }

    // Game state
    private bool gameHasStarted = false;

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
    }

    private void OnEnable()
    {
        // Subscribe to instruction panel events
        InstructionPanel.OnInstructionsCompleted += OnInstructionsCompleted;

        // Subscribe to time manager events
        TimeManager.OnWorkTimeStarted += OnWorkTimeStarted;
        TimeManager.OnWorkTimeEnded += OnWorkTimeEnded;
        TimeManager.OnDayEnded += OnDayEnded;
        TimeManager.OnAllDaysCompleted += OnAllDaysCompleted;

        HermitCrabAnimator.OnHermitFinishedLeaving += OnHermitFinishedLeaving;
        HermitCrabAnimator.OnHermitFinishedReturning += OnHermitFinishedReturning;

        // Subscribe to item spawner events
        ItemSpawner.OnItemsSpawned += OnItemsSpawned;
    }

    private void OnDisable()
    {
        // Unsubscribe from events
        InstructionPanel.OnInstructionsCompleted -= OnInstructionsCompleted;
        TimeManager.OnWorkTimeStarted -= OnWorkTimeStarted;
        TimeManager.OnWorkTimeEnded -= OnWorkTimeEnded;
        TimeManager.OnDayEnded -= OnDayEnded;
        TimeManager.OnAllDaysCompleted -= OnAllDaysCompleted;

        // Unsubscribe from hermit crab animation events
        HermitCrabAnimator.OnHermitFinishedLeaving -= OnHermitFinishedLeaving;
        HermitCrabAnimator.OnHermitFinishedReturning -= OnHermitFinishedReturning;

        // Unsubscribe from item spawner events
        ItemSpawner.OnItemsSpawned -= OnItemsSpawned;
    }

    private void OnHermitFinishedLeaving()
    {
        LogDebug("Hermit crab has finished leaving - Items can now be spawned!");

        // This is where the ItemSpawner will automatically spawn the box
        // No need to do anything here, just for logging/feedback
    }

    private void OnHermitFinishedReturning()
    {
        LogDebug("Hermit crab has returned home!");

        // Calculate day score


        if (ResultsPanel.Instance != null)
        {
            ResultsPanel.Instance.ShowResults();
        }
        else
        {
            Debug.LogWarning("GameController: ResultsPanel not found!");
        }
    }

    private void OnItemsSpawned(int day)
    {
        LogDebug($"Items spawned for day {day}");

        // Update UI or provide feedback that items are available
    }

    // Update the OnWorkTimeEnded method:
    private void OnWorkTimeEnded()
    {
        LogDebug("Work time ended - Hermit crab is returning!");

        // The hermit crab returning animation will be triggered automatically
        // by the HermitCrabAnimator listening to TimeManager.OnWorkTimeEnded
    }

    private void Start()
    {
        if (autoStartOnSceneLoad)
        {
            StartGameSequence();
        }
    }

    public void StartGameSequence()
    {
        if (gameHasStarted)
        {
            LogDebug("Game sequence already started");
            return;
        }

        LogDebug("Starting game sequence...");

        // Start the sequence with a small delay
        Invoke(nameof(ShowInstructionsIfNeeded), delayBeforeInstructions);
    }

    private void ShowInstructionsIfNeeded()
    {
        // Check if we should show instructions (Day 1 or manual trigger)
        if (InstructionPanel.Instance != null)
        {
            // Instructions will handle showing themselves based on day
            // But we can force show them here if needed
            if (TimeManager.Instance == null || TimeManager.Instance.CurrentDay == 1)
            {
                InstructionPanel.Instance.ShowInstructions();
            }
            else
            {
                // Skip instructions and start directly
                OnInstructionsCompleted();
            }
        }
        else
        {
            LogDebug("No instruction panel found, starting game directly");
            OnInstructionsCompleted();
        }
    }

    private void OnInstructionsCompleted()
    {
        LogDebug("Instructions completed - Starting game!");

        gameHasStarted = true;

        // This is where the hermit crab leaving animation would trigger
        StartHermitLeavingSequence();
    }

    private void StartHermitLeavingSequence()
    {
        LogDebug("Hermit crab is getting ready to leave for work...");

        if (TimeManager.Instance != null)
        {
            TimeManager.Instance.StartTime();
        }
    }

    private void OnHermitLeftForWork()
    {
        LogDebug("Hermit crab has left for work - Work time begins!");

        // Time should already be started by the TimeManager
        // This is where decoration boxes would spawn

        // TODO: Trigger decoration box spawning here
    }

    private void OnWorkTimeStarted()
    {
        LogDebug("Work time started - Player can now decorate!");

        // This is called when TimeManager starts the work day
        // Perfect place to spawn decoration boxes
    }

    private void OnDayEnded(int day)
    {
        LogDebug($"Day {day} has ended");

        // Handle day end logic
        // Score calculation, day summary, etc.
    }

    private void OnAllDaysCompleted()
    {
        LogDebug("All days completed - Game finished!");

        // Handle game completion
        // Final score, ending screen, etc.
    }

    // Public methods for manual control
    public void RestartGame()
    {
        LogDebug("Restarting game...");

        gameHasStarted = false;

        // Reset cumulative tracking
        if (ResultsPanel.Instance != null)
        {
            ResultsPanel.Instance.ResetCumulativeTracking();
        }

        if (TimeManager.Instance != null)
        {
            TimeManager.Instance.StartNewGame();
        }

        StartGameSequence();
    }

    public void SkipInstructions()
    {
        if (InstructionPanel.Instance != null)
        {
            InstructionPanel.Instance.ForceHideInstructions();
        }
    }

    // Debug helper
    private void LogDebug(string message)
    {
        if (showDebugLogs)
        {
            Debug.Log($"[GameController] {message}");
        }
    }

    // Debug controls
    private void Update()
    {
        if (showDebugLogs)
        {
            if (Input.GetKeyDown(KeyCode.R))
            {
                RestartGame();
            }

            if (Input.GetKeyDown(KeyCode.I))
            {
                if (InstructionPanel.Instance != null)
                {
                    InstructionPanel.Instance.ForceShowInstructions();
                }
            }
        }
    }
}