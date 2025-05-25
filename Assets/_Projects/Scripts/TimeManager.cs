using UnityEngine;
using System;

public class TimeManager : MonoBehaviour
{
    [Header("Time Settings")]
    [SerializeField] private float realTimePerGameHour = 60f; // 60 seconds = 1 game hour
    [SerializeField] private int startHour = 9; // 9 AM
    [SerializeField] private int endHour = 17; // 5 PM
    [SerializeField] private int totalDays = 3;

    [Header("Current Time State")]
    [SerializeField] private int currentDay = 1;
    [SerializeField] private int currentHour = 9;
    [SerializeField] private int currentMinute = 0;
    [SerializeField] private bool isTimeRunning = false;

    [Header("Debug")]
    [SerializeField] private bool showDebugInfo = true;

    // Events
    public static event Action<int, int, int> OnTimeUpdated; // day, hour, minute
    public static event Action<int> OnDayStarted; // day number
    public static event Action<int> OnDayEnded; // day number
    public static event Action OnWorkTimeStarted;
    public static event Action OnWorkTimeEnded;
    public static event Action OnAllDaysCompleted;

    // Properties
    public int CurrentDay => currentDay;
    public int CurrentHour => currentHour;
    public int CurrentMinute => currentMinute;
    public bool IsTimeRunning => isTimeRunning;
    public bool IsWorkTime => isTimeRunning && currentHour >= startHour && currentHour < endHour;
    public float TimeProgress => GetTimeProgress();
    public float DayProgress => GetDayProgress();

    // Singleton
    public static TimeManager Instance { get; private set; }

    private float timeAccumulator = 0f;
    private bool hasWorkStartedToday = false;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        ResetToStartOfDay();
    }

    private void Update()
    {
        if (isTimeRunning)
        {
            UpdateTime();
        }

        if (showDebugInfo && Input.GetKeyDown(KeyCode.T))
        {
            ToggleTimeRunning();
        }
    }

    private void UpdateTime()
    {
        timeAccumulator += Time.deltaTime;

        // Convert real time to game minutes
        float minutesPerSecond = 60f / realTimePerGameHour;

        while (timeAccumulator >= (1f / minutesPerSecond))
        {
            timeAccumulator -= (1f / minutesPerSecond);
            AdvanceMinute();
        }
    }

    private void AdvanceMinute()
    {
        currentMinute++;

        if (currentMinute >= 60)
        {
            currentMinute = 0;
            currentHour++;

            // Check for work time start
            if (currentHour == startHour && !hasWorkStartedToday)
            {
                hasWorkStartedToday = true;
                OnWorkTimeStarted?.Invoke();

                if (showDebugInfo)
                    Debug.Log($"Work time started! Day {currentDay}, {FormatTime(currentHour, currentMinute)}");
            }

            // Check for work time end
            if (currentHour >= endHour)
            {
                EndCurrentDay();
                return;
            }
        }

        // Fire time update event
        OnTimeUpdated?.Invoke(currentDay, currentHour, currentMinute);

        if (showDebugInfo)
        {
            //Debug.Log($"Time: Day {currentDay}, {FormatTime(currentHour, currentMinute)} - Progress: {(DayProgress * 100f):F1}%");
        }
    }

    public void PrepareNextDay()
    {
        if (currentDay >= totalDays)
        {
            OnAllDaysCompleted?.Invoke();
            return;
        }

        // Move to next day
        currentDay++;
        ResetToStartOfDay();

        // Fire day started event but DON'T start time
        OnDayStarted?.Invoke(currentDay);

        if (showDebugInfo)
            Debug.Log($"Day {currentDay} prepared - waiting for instructions to complete before starting time");
    }

    private void EndCurrentDay()
    {
        isTimeRunning = false;
        OnWorkTimeEnded?.Invoke();
        OnDayEnded?.Invoke(currentDay);

        if (showDebugInfo)
            Debug.Log($"Day {currentDay} ended at {FormatTime(currentHour, currentMinute)}");

        // Check if all days are completed
        if (currentDay >= totalDays)
        {
            OnAllDaysCompleted?.Invoke();

            if (showDebugInfo)
                Debug.Log("All days completed!");
        }
       
    }

    private void StartNextDay()
    {
        OnDayStarted?.Invoke(currentDay);

        if (showDebugInfo)
            Debug.Log($"Day {currentDay} started!");

        StartTime();
    }

    private void ResetToStartOfDay()
    {
        currentHour = startHour;
        currentMinute = 0;
        hasWorkStartedToday = false;
        timeAccumulator = 0f;

        OnTimeUpdated?.Invoke(currentDay, currentHour, currentMinute);
    }

    // Public Methods
    public void StartTime()
    {
        isTimeRunning = true;

        // Only trigger work started event if we haven't already done so today
        if (!hasWorkStartedToday)
        {
            hasWorkStartedToday = true;
            OnWorkTimeStarted?.Invoke();

            if (showDebugInfo)
                Debug.Log($"Work time started for Day {currentDay} - Hermit crab should start leaving!");
        }

        if (showDebugInfo)
            Debug.Log($"Time started for Day {currentDay}");
    }

    public void StopTime()
    {
        isTimeRunning = false;

        if (showDebugInfo)
            Debug.Log("Time stopped");
    }

    public void ToggleTimeRunning()
    {
        if (isTimeRunning)
            StopTime();
        else
            StartTime();
    }

    public void StartNewGame()
    {
        currentDay = 1;
        ResetToStartOfDay();
        OnDayStarted?.Invoke(currentDay);

        if (showDebugInfo)
            Debug.Log("New game started!");
    }

    public void ForceEndDay()
    {
        if (showDebugInfo)
            Debug.Log("Force ending current day");

        EndCurrentDay();
    }

    public void SkipToNextDay()
    {
        if (currentDay < totalDays)
        {
            EndCurrentDay();
        }
    }

    // Utility Methods
    private float GetTimeProgress()
    {
        int totalGameMinutes = (endHour - startHour) * 60;
        int currentGameMinutes = (currentHour - startHour) * 60 + currentMinute;
        return Mathf.Clamp01((float)currentGameMinutes / totalGameMinutes);
    }

    private float GetDayProgress()
    {
        return GetTimeProgress();
    }

    public string FormatTime(int hour, int minute)
    {
        string period = hour >= 12 ? "PM" : "AM";
        int displayHour = hour;

        if (hour == 0)
            displayHour = 12;
        else if (hour > 12)
            displayHour = hour - 12;

        return $"{displayHour}:{minute:D2} {period}";
    }

    public string GetCurrentTimeString()
    {
        return FormatTime(currentHour, currentMinute);
    }

    public string GetTimeRemainingString()
    {
        int remainingMinutes = (endHour - currentHour) * 60 - currentMinute;
        int hours = remainingMinutes / 60;
        int minutes = remainingMinutes % 60;

        if (hours > 0)
            return $"{hours}h {minutes}m";
        else
            return $"{minutes}m";
    }

    // Debug
    private void OnGUI()
    {
        if (!showDebugInfo) return;

        GUILayout.BeginArea(new Rect(10, 10, 300, 200));
        GUILayout.BeginVertical("box");

        GUILayout.Label("TIME MANAGER DEBUG");
        GUILayout.Label($"Day: {currentDay}/{totalDays}");
        GUILayout.Label($"Time: {GetCurrentTimeString()}");
        GUILayout.Label($"Time Remaining: {GetTimeRemainingString()}");
        GUILayout.Label($"Progress: {(DayProgress * 100f):F1}%");
        GUILayout.Label($"Running: {isTimeRunning}");
        GUILayout.Label($"Work Time: {IsWorkTime}");

        GUILayout.Space(10);

        if (GUILayout.Button(isTimeRunning ? "Stop Time" : "Start Time"))
        {
            ToggleTimeRunning();
        }

        if (GUILayout.Button("Skip Day"))
        {
            SkipToNextDay();
        }

        if (GUILayout.Button("Restart Game"))
        {
            StartNewGame();
        }

        GUILayout.Label("Press 'T' to toggle time");

        GUILayout.EndVertical();
        GUILayout.EndArea();
    }
}