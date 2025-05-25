using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;

public class GameFeedbackUI : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private GameObject notificationPanel;
    [SerializeField] private TextMeshProUGUI notificationText;
    [SerializeField] private CanvasGroup notificationCanvasGroup;
    
    [Header("Animation Settings")]
    [SerializeField] private float fadeInDuration = 0.5f;
    [SerializeField] private float displayDuration = 3f;
    [SerializeField] private float fadeOutDuration = 0.5f;
    [SerializeField] private Ease fadeEase = Ease.OutQuad;
    
    [Header("Notification Messages")]
    [SerializeField] private string hermitLeavingMessage = "Hermit crab is leaving for work...";
    [SerializeField] private string hermitLeftMessage = "Click the box to unpack today's items!";
    [SerializeField] private string hermitReturningMessage = "Hermit crab is coming home!";
    [SerializeField] private string hermitReturnedMessage = "Welcome home! Let's see how you decorated...";
    [SerializeField] private string itemsSpawnedMessage = "Items unpacked! Start decorating!";
    
    // Singleton
    public static GameFeedbackUI Instance { get; private set; }
    
    private Sequence currentNotification;
    
    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            SetupUI();
        }
        else
        {
            Destroy(gameObject);
        }
    }
    
    private void OnEnable()
    {
        // Subscribe to events
        HermitCrabAnimator.OnHermitStartedLeaving += OnHermitStartedLeaving;
        HermitCrabAnimator.OnHermitFinishedLeaving += OnHermitFinishedLeaving;
        HermitCrabAnimator.OnHermitStartedReturning += OnHermitStartedReturning;
        HermitCrabAnimator.OnHermitFinishedReturning += OnHermitFinishedReturning;
        ItemSpawner.OnBoxSpawned += OnBoxSpawned;
        ItemSpawner.OnItemsSpawned += OnItemsSpawned;
    }
    
    private void OnDisable()
    {
        // Unsubscribe from events
        HermitCrabAnimator.OnHermitStartedLeaving -= OnHermitStartedLeaving;
        HermitCrabAnimator.OnHermitFinishedLeaving -= OnHermitFinishedLeaving;
        HermitCrabAnimator.OnHermitStartedReturning -= OnHermitStartedReturning;
        HermitCrabAnimator.OnHermitFinishedReturning -= OnHermitFinishedReturning;
        ItemSpawner.OnBoxSpawned -= OnBoxSpawned;
        ItemSpawner.OnItemsSpawned -= OnItemsSpawned;
    }
    
    private void SetupUI()
    {
        // Ensure we have required components
        if (notificationCanvasGroup == null && notificationPanel != null)
        {
            notificationCanvasGroup = notificationPanel.GetComponent<CanvasGroup>();
            if (notificationCanvasGroup == null)
            {
                notificationCanvasGroup = notificationPanel.AddComponent<CanvasGroup>();
            }
        }
        
        // Hide notification initially
        if (notificationPanel != null)
        {
            notificationPanel.SetActive(false);
        }
        if (notificationCanvasGroup != null)
        {
            notificationCanvasGroup.alpha = 0f;
        }
    }
    
    private void OnHermitStartedLeaving()
    {
        ShowNotification(hermitLeavingMessage, displayDuration * 0.8f);
    }
    
    private void OnHermitFinishedLeaving()
    {
        // Don't show notification immediately - wait for box to spawn
    }
    
    private void OnBoxSpawned()
    {
        ShowNotification(hermitLeftMessage, displayDuration * 1.5f);
    }
    
    private void OnHermitStartedReturning()
    {
        ShowNotification(hermitReturningMessage, displayDuration * 0.8f);
    }
    
    private void OnHermitFinishedReturning()
    {
        ShowNotification(hermitReturnedMessage, displayDuration);
    }
    
    private void OnItemsSpawned(int day)
    {
        ShowNotification(itemsSpawnedMessage, displayDuration);
    }
    
    public void ShowNotification(string message, float duration = -1f)
    {
        if (notificationPanel == null || notificationText == null || notificationCanvasGroup == null)
        {
            Debug.LogWarning("GameFeedbackUI: Missing UI components for notification");
            return;
        }
        
        if (duration < 0)
            duration = displayDuration;
        
        // Kill any existing notification
        currentNotification?.Kill();
        
        // Set message
        notificationText.text = message;
        
        // Show panel
        notificationPanel.SetActive(true);
        
        // Create notification sequence
        currentNotification = DOTween.Sequence();
        
        // Fade in
        currentNotification.Append(
            notificationCanvasGroup.DOFade(1f, fadeInDuration).SetEase(fadeEase)
        );
        
        // Wait
        currentNotification.AppendInterval(duration);
        
        // Fade out
        currentNotification.Append(
            notificationCanvasGroup.DOFade(0f, fadeOutDuration).SetEase(fadeEase)
        );
        
        // Hide panel when done
        currentNotification.OnComplete(() => {
            if (notificationPanel != null)
            {
                notificationPanel.SetActive(false);
            }
        });
        
        currentNotification.Play();
        
        Debug.Log($"GameFeedbackUI: Showing notification - {message}");
    }
    
    public void HideNotification()
    {
        currentNotification?.Kill();
        
        if (notificationCanvasGroup != null)
        {
            notificationCanvasGroup.DOFade(0f, fadeOutDuration)
                .OnComplete(() => {
                    if (notificationPanel != null)
                    {
                        notificationPanel.SetActive(false);
                    }
                });
        }
    }
    
    // Public method for custom notifications
    public void ShowCustomNotification(string message, float duration = 3f)
    {
        ShowNotification(message, duration);
    }
    
    private void OnDestroy()
    {
        currentNotification?.Kill();
    }
}