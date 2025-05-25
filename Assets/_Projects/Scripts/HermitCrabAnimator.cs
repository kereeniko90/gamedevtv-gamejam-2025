using UnityEngine;
using DG.Tweening;

public class HermitCrabAnimator : MonoBehaviour
{
    [Header("Animation Settings")]
    [SerializeField] private float leavingDuration = 2f;
    [SerializeField] private float returningDuration = 1.5f;
    [SerializeField] private float wiggleAmount = 0.2f;
    [SerializeField] private int wiggleLoops = 3;

    [Header("Movement Settings")]
    [SerializeField] private Vector3 offScreenPosition = new Vector3(10f, 0f, 0f);
    [SerializeField] private bool moveRelativeToStart = true;

    [Header("Wiggle Settings")]
    [SerializeField] private float wiggleSpeed = 0.3f;
    [SerializeField] private Ease wiggleEase = Ease.InOutSine;

    // Events
    public static System.Action OnHermitStartedLeaving;
    public static System.Action OnHermitFinishedLeaving;
    public static System.Action OnHermitStartedReturning;
    public static System.Action OnHermitFinishedReturning;

    // Singleton
    public static HermitCrabAnimator Instance { get; private set; }

    private Vector3 originalPosition;
    private Vector3 targetOffScreenPosition;
    private bool isAnimating = false;
    private Sequence currentAnimation;

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

        // Store original position
        originalPosition = transform.position;

        // Calculate off-screen position
        if (moveRelativeToStart)
        {
            targetOffScreenPosition = originalPosition + offScreenPosition;
        }
        else
        {
            targetOffScreenPosition = offScreenPosition;
        }
    }

    private void OnEnable()
    {
        // Subscribe to time events
        TimeManager.OnWorkTimeStarted += StartLeavingAnimation;
        TimeManager.OnWorkTimeEnded += StartReturningAnimation;
    }

    private void OnDisable()
    {
        // Unsubscribe from events
        TimeManager.OnWorkTimeStarted -= StartLeavingAnimation;
        TimeManager.OnWorkTimeEnded -= StartReturningAnimation;

        // Kill any ongoing animations
        currentAnimation?.Kill();
    }

    public void StartLeavingAnimation()
    {
        if (isAnimating)
        {
            Debug.LogWarning("Hermit crab is already animating!");
            return;
        }

        Debug.Log("Hermit crab starting to leave for work...");

        isAnimating = true;
        OnHermitStartedLeaving?.Invoke();

        // Kill any existing animation
        currentAnimation?.Kill();

        // Create leaving animation sequence
        currentAnimation = DOTween.Sequence();

        // First: Wiggle animation (getting ready)
        Tween wiggleTween = transform.DOShakePosition(
            wiggleSpeed * wiggleLoops,
            wiggleAmount,
            10,
            90f,
            false,
            true
        ).SetLoops(wiggleLoops);

        currentAnimation.Append(wiggleTween);

        // Then: Move off-screen
        Tween moveTween = transform.DOMove(targetOffScreenPosition, leavingDuration - (wiggleSpeed * wiggleLoops))
            .SetEase(Ease.InQuad);

        currentAnimation.Append(moveTween);

        // On completion
        currentAnimation.OnComplete(() =>
        {
            isAnimating = false;
            OnHermitFinishedLeaving?.Invoke();

            Debug.Log("Hermit crab has left for work!");
        });

        currentAnimation.Play();
        SoundManager.Instance.PlaySFX(SoundEffect.DoorClose);
    }

    public void StartReturningAnimation()
    {
        if (isAnimating)
        {
            Debug.LogWarning("Hermit crab is already animating!");
            return;
        }

        Debug.Log("Hermit crab is returning from work...");

        isAnimating = true;
        OnHermitStartedReturning?.Invoke();

        // Kill any existing animation
        currentAnimation?.Kill();

        // Create returning animation sequence
        currentAnimation = DOTween.Sequence();

        // Move back to original position
        Tween moveTween = transform.DOMove(originalPosition, returningDuration)
            .SetEase(Ease.OutQuad);

        currentAnimation.Append(moveTween);

        // Final wiggle (happy to be home)
        Tween happyWiggle = transform.DOShakePosition(
            wiggleSpeed,
            wiggleAmount * 0.5f,
            5,
            90f,
            false,
            true
        );

        currentAnimation.Append(happyWiggle);

        // On completion
        currentAnimation.OnComplete(() =>
        {
            isAnimating = false;
            OnHermitFinishedReturning?.Invoke();

            Debug.Log("Hermit crab is home!");
        });

        currentAnimation.Play();
        SoundManager.Instance.PlaySFX(SoundEffect.DoorClose);
    }

    // Manual trigger methods for testing
    public void TriggerLeaving()
    {
        if (!isAnimating)
        {
            StartLeavingAnimation();
        }
    }

    public void TriggerReturning()
    {
        if (!isAnimating)
        {
            StartReturningAnimation();
        }
    }

    // Reset hermit to original position (useful for testing/restarting)
    public void ResetToOriginalPosition()
    {
        currentAnimation?.Kill();
        isAnimating = false;
        transform.position = originalPosition;
    }

    // Check if hermit is currently animating
    public bool IsAnimating()
    {
        return isAnimating;
    }

    private void OnDestroy()
    {
        currentAnimation?.Kill();
    }

    // Debug controls
    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.L))
        {
            TriggerLeaving();
        }

        if (Input.GetKeyDown(KeyCode.R))
        {
            TriggerReturning();
        }

        if (Input.GetKeyDown(KeyCode.H))
        {
            ResetToOriginalPosition();
        }
    }
}