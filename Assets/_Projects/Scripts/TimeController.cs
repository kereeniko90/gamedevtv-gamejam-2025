using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class TimeController : MonoBehaviour
{
    public static TimeController Instance { get; private set; }
    [Header("Time Settings")]
    [SerializeField] private float dayDuration = 180f; // 3 minutes per day
    [SerializeField] private float warningTime = 30f; // When to start warning player
    private float timeRemaining;

    [Header("References")]
    [SerializeField] private GameObject hermitCrab;
    [SerializeField] private ScoreManager scoreManager;
    [SerializeField] private TextMeshProUGUI timeText; // UI Text for time display
    [SerializeField] private Image timeBarFill; // UI Image for time bar

    [Header("Audio")]
    [SerializeField] private AudioClip tickingSound; // For final countdown
    [SerializeField] private AudioClip bellSound; // For day start/end
    private AudioSource audioSource;

    // Game state
    private bool isHermitHome = false;
    private bool isPaused = false;
    private int currentDay = 1;
    [SerializeField] private int totalDays = 5; // Total days in the game
    private bool isGameOver = false;

    // Events
    public System.Action onDayEnd;
    public System.Action onDayStart;
    public System.Action onGameOver;

    private void Awake()
    {
        scoreManager = ScoreManager.Instance;
        // Find score manager if not assigned
        if (scoreManager == null)
        {
            scoreManager = FindFirstObjectByType<ScoreManager>();
        }

        // Get audio source component
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }
    }

    private void Start()
    {
        StartDay();
    }

    private void Update()
    {
        if (isGameOver || isPaused) return;

        if (timeRemaining > 0)
        {
            timeRemaining -= Time.deltaTime;
            UpdateTimeDisplay();

            // Play warning sound when time is running low
            if (timeRemaining <= warningTime && !audioSource.isPlaying && tickingSound != null)
            {
                audioSource.clip = tickingSound;
                audioSource.Play();
            }

            if (timeRemaining <= 0)
            {
                EndDay();
            }
        }
    }

    public void PauseTime()
    {
        isPaused = true;
    }

    public void ResumeTime()
    {
        isPaused = false;
    }


    public void StartDay()
    {
        timeRemaining = dayDuration;
        isHermitHome = false;

        // Play day start sound
        if (bellSound != null)
        {
            audioSource.PlayOneShot(bellSound);
        }

        // Animate hermit leaving
        StartCoroutine(AnimateHermitLeaving());

        // Notify listeners that day has started
        onDayStart?.Invoke();

        // Log day start
        Debug.Log($"Day {currentDay} started!");
    }

    public void EndDay()
    {
        timeRemaining = 0;
        isHermitHome = true;

        // Stop any warning sounds
        audioSource.Stop();

        // Play day end sound
        if (bellSound != null)
        {
            audioSource.PlayOneShot(bellSound);
        }

        // Animate hermit returning
        StartCoroutine(AnimateHermitReturning());

        // Calculate day score
        CalculateDayScore();

        // Notify listeners that day has ended
        onDayEnd?.Invoke();

        // Check if game is over
        if (currentDay >= totalDays)
        {
            isGameOver = true;
            onGameOver?.Invoke();
            Debug.Log("Game Over! Final score calculated.");
        }
        else
        {
            // Setup for next day
            currentDay++;
            StartCoroutine(PrepareNextDay());
        }
    }

    private void UpdateTimeDisplay()
    {
        // Update UI text
        if (timeText != null)
        {
            int minutes = Mathf.FloorToInt(timeRemaining / 60);
            int seconds = Mathf.FloorToInt(timeRemaining % 60);
            timeText.text = string.Format("{0:00}:{1:00}", minutes, seconds);
        }

        // Update progress bar
        if (timeBarFill != null)
        {
            timeBarFill.fillAmount = timeRemaining / dayDuration;

            // Change color based on time remaining
            if (timeRemaining <= warningTime)
            {
                // Flash red when time is low
                float flashValue = Mathf.PingPong(Time.time * 4, 1);
                timeBarFill.color = Color.Lerp(Color.red, Color.yellow, flashValue);
            }
            else
            {
                // Gradually change from green to yellow as time passes
                float normalizedTime = timeRemaining / dayDuration;
                timeBarFill.color = Color.Lerp(Color.yellow, Color.green, normalizedTime);
            }
        }
    }

    private void CalculateDayScore()
    {
        // Score is now calculated by the ScoreManager
        // This method remains as a hook for any additional day-end calculations
        Debug.Log("Day ended - ScoreManager calculating final day score...");
    }

    private IEnumerator AnimateHermitLeaving()
    {
        // Simple animation of hermit leaving
        if (hermitCrab != null)
        {
            // For placeholder, just disable the object with a delay
            yield return new WaitForSeconds(1.0f);
            hermitCrab.SetActive(false);
            Debug.Log("Hermit has left for work!");
        }
        else
        {
            yield return null;
        }
    }

    private IEnumerator AnimateHermitReturning()
    {
        // Simple animation of hermit returning
        if (hermitCrab != null)
        {
            yield return new WaitForSeconds(1.0f);
            hermitCrab.SetActive(true);
            Debug.Log("Hermit has returned home!");

            // Here you could add code for the hermit to react to the state of the home
            // For example, play different animations based on score
        }
        else
        {
            yield return null;
        }
    }

    private IEnumerator PrepareNextDay()
    {
        // Wait a few seconds to show day results
        yield return new WaitForSeconds(3.0f);

        // Start the next day
        StartDay();
    }

    // Public methods for UI buttons

    public void SkipToEndOfDay()
    {
        // For testing - immediately end the current day
        timeRemaining = 0.1f;
    }

    public float GetRemainingTimePercentage()
    {
        return timeRemaining / dayDuration;
    }

    public int GetCurrentDay()
    {
        return currentDay;
    }

    public int GetTotalDays()
    {
        return totalDays;
    }
}