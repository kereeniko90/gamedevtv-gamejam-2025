using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class TimeDisplayUI : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private TextMeshProUGUI dayText;
    [SerializeField] private TextMeshProUGUI timeText;
    [SerializeField] private TextMeshProUGUI timeRemainingText;

    // Replace slider with circular fill components
    [SerializeField] private Image circularFillBackground; // Background circle
    [SerializeField] private Image circularFillForeground; // Fill circle
    [SerializeField] private Transform clockHand; // Optional clock hand
    [SerializeField] private Button endDayButton;

    [Header("Circular Fill Settings")]
    [SerializeField] private bool animateFillSmooth = true;
    [SerializeField] private float fillAnimationSpeed = 2f;
    [SerializeField] private float startAngle = 0f;

    [Header("Visual Settings")]
    [SerializeField] private Color normalTimeColor = Color.black;
    [SerializeField] private Color warningTimeColor = Color.yellow;
    [SerializeField] private Color urgentTimeColor = Color.red;
    [SerializeField] private float warningThreshold = 0.7f; // 70% of day passed
    [SerializeField] private float urgentThreshold = 0.9f;  // 90% of day passed

    [Header("Animation")]
    [SerializeField] private bool animateUrgentTime = true;
    [SerializeField] private float urgentBlinkSpeed = 2f;
    [Header("End Day Settings")]
    [SerializeField] private bool showEndDayButton = true;
    [SerializeField] private float endDayConfirmationDelay = 1f;
    [SerializeField] private string endDayButtonText = "End Day";
    

    // For smooth animation
    private float targetFillAmount = 1f;
    private float currentFillAmount = 1f;
    private bool playTicking = false;

    private void OnEnable()
    {
        // Subscribe to time events
        TimeManager.OnTimeUpdated += UpdateTimeDisplay;
        TimeManager.OnDayStarted += OnDayStarted;
        TimeManager.OnDayEnded += OnDayEnded;
        TimeManager.OnWorkTimeStarted += OnWorkTimeStarted;
        TimeManager.OnWorkTimeEnded += OnWorkTimeEnded;
    }

    private void OnDisable()
    {
        // Unsubscribe from events
        TimeManager.OnTimeUpdated -= UpdateTimeDisplay;
        TimeManager.OnDayStarted -= OnDayStarted;
        TimeManager.OnDayEnded -= OnDayEnded;
        TimeManager.OnWorkTimeStarted -= OnWorkTimeStarted;
        TimeManager.OnWorkTimeEnded -= OnWorkTimeEnded;
    }

    private void Start()
    {
        // Setup circular fill if components exist
        SetupCircularFill();

        // Initialize display if TimeManager already exists
        if (TimeManager.Instance != null)
        {
            UpdateTimeDisplay(
                TimeManager.Instance.CurrentDay,
                TimeManager.Instance.CurrentHour,
                TimeManager.Instance.CurrentMinute
            );
        }

        SetupEndDayButton();
    }

    private void SetupEndDayButton()
    {
        if (endDayButton != null)
        {
            endDayButton.onClick.AddListener(OnEndDayButtonClicked);

            // Set initial button text
            TextMeshProUGUI buttonText = endDayButton.GetComponentInChildren<TextMeshProUGUI>();
            if (buttonText != null)
            {
                buttonText.text = endDayButtonText;
            }

            // Show/hide based on setting
            endDayButton.gameObject.SetActive(showEndDayButton);
        }
    }

    private void OnEndDayButtonClicked()
    {
        if (TimeManager.Instance == null) return;

        // Only allow ending day during work time
        if (!TimeManager.Instance.IsWorkTime || !TimeManager.Instance.IsTimeRunning)
        {
            Debug.Log("Can only end day during active work time");
            return;
        }

        // End the day immediately
        Debug.Log("Player ended day early - Hermit crab coming home!");
        TimeManager.Instance.ForceEndDay();

        // Hide the button
        if (endDayButton != null)
        {
            endDayButton.gameObject.SetActive(false);
        }
    }



    private void Update()
    {
        if (animateFillSmooth && circularFillForeground != null)
        {
            if (Mathf.Abs(currentFillAmount - targetFillAmount) > 0.001f)
            {
                currentFillAmount = Mathf.Lerp(currentFillAmount, targetFillAmount,
                    Time.deltaTime * fillAnimationSpeed);
                circularFillForeground.fillAmount = currentFillAmount;
            }
        }



    }

    private void SetupCircularFill()
    {
        if (circularFillForeground != null)
        {
            // Set the fill to radial and configure direction

            circularFillForeground.fillOrigin = GetFillOrigin();
            currentFillAmount = 1f;
            targetFillAmount = 1f;
        }
    }


    private void UpdateTimeDisplay(int day, int hour, int minute)
    {
        // Update day text
        if (dayText != null)
        {
            dayText.text = $"Day {day}";
        }

        // Update time text
        if (timeText != null)
        {
            timeText.text = TimeManager.Instance.GetCurrentTimeString();
        }

        // Update time remaining
        if (timeRemainingText != null)
        {
            timeRemainingText.text = $"{TimeManager.Instance.GetTimeRemainingString()} left";
        }

        // Update circular progress
        float progress = TimeManager.Instance.DayProgress;
        UpdateCircularFill(progress);

        // Update colors based on progress
        UpdateTimeColors(progress);
    }

    private void UpdateCircularFill(float progress)
    {
        targetFillAmount = Mathf.Clamp01(1f - progress);

        if (!animateFillSmooth && circularFillForeground != null)
        {
            circularFillForeground.fillAmount = targetFillAmount;
            currentFillAmount = targetFillAmount;
        }
    }

    private int GetFillOrigin()
    {
        // Convert start angle to Unity's fill origin
        // Unity's origins: 0=Bottom, 1=Right, 2=Top, 3=Left
        float normalizedAngle = (startAngle + 360f) % 360f;

        if (normalizedAngle >= 315f || normalizedAngle < 45f)
            return 2; // Top
        else if (normalizedAngle >= 45f && normalizedAngle < 135f)
            return 1; // Right
        else if (normalizedAngle >= 135f && normalizedAngle < 225f)
            return 0; // Bottom
        else
            return 3; // Left
    }

    private void UpdateTimeColors(float progress)
    {
        Color currentColor = normalTimeColor;

        if (progress >= urgentThreshold)
        {
            currentColor = urgentTimeColor;

            // Add blinking effect for urgent time
            if (animateUrgentTime)
            {
                float alpha = Mathf.Lerp(0.5f, 1f, (Mathf.Sin(Time.time * urgentBlinkSpeed) + 1f) / 2f);
                currentColor.a = alpha;
            }

            if (!playTicking)
            {   
                playTicking = true;
                SoundManager.Instance.PlaySFX(SoundEffect.Ticking, 0.6f);
                
            }

            
        }
        else if (progress >= warningThreshold)
        {
            currentColor = warningTimeColor;
        }

        // Apply color to UI elements
        if (timeText != null)
        {
            timeText.color = currentColor;
        }

        if (timeRemainingText != null)
        {
            timeRemainingText.color = currentColor;
        }


    }

    // Public methods for manual control
    public void SetCircularFillProgress(float progress)
    {
        UpdateCircularFill(progress);
    }

    public void ResetCircularFill()
    {
        targetFillAmount = 1f;
        currentFillAmount = 1f;
        if (circularFillForeground != null)
        {
            circularFillForeground.fillAmount = 1f;
        }
    }

    private void OnDayStarted(int day)
    {
        Debug.Log($"UI: Day {day} started!");
        playTicking = false;
        ResetCircularFill();

        // You can add day start animations here
        // For example, a fade-in effect or day start notification
    }

    private void OnDayEnded(int day)
    {
        Debug.Log($"UI: Day {day} ended!");

        // You can add day end animations here
        // For example, a fade-out effect or day end summary
    }

    private void OnWorkTimeStarted()
    {
        Debug.Log("UI: Work time started - Hermit crab left for work!");

        // Show a notification or play an animation
        // This is where you'd trigger the hermit crab leaving animation
        if (endDayButton != null && showEndDayButton)
        {
            endDayButton.gameObject.SetActive(true);
        }
    }

    private void OnWorkTimeEnded()
    {
        Debug.Log("UI: Work time ended - Hermit crab is coming home!");

        // Show a notification that the hermit crab is returning
        // This is where you'd trigger the hermit crab returning animation
        if (endDayButton != null)
        {
            endDayButton.gameObject.SetActive(false);
        }
    }

}