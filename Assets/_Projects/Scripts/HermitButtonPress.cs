using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using DG.Tweening;

public class HermitButtonPress : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
{
    [Header("References")]
    [SerializeField] private Transform hermitRightHand;
    [SerializeField] private Transform hermitLeftClaw;
    [SerializeField] private Transform buttonTransform;
    [SerializeField] private Image buttonImage;
    [SerializeField] private Sprite normalButtonSprite;
    [SerializeField] private Sprite pressedButtonSprite;
    [SerializeField] private RectTransform buttonText;
    
    [Header("Right Hand Animation")]
    [SerializeField] private float handMoveDuration = 0.5f;
    [SerializeField] private Ease handMoveEase = Ease.OutQuad;
    
    [Header("Left Claw Animation")]
    [SerializeField] private float leftClawIdleAmount = 3f;
    [SerializeField] private float leftClawIdleDuration = 1.5f;
    [SerializeField] private float leftClawReactAmount = 10f;
    [SerializeField] private float leftClawReactDuration = 0.3f;
    [SerializeField] private Ease leftClawReactEase = Ease.OutElastic;
    
    [Header("Text Animation")]
    [SerializeField] private float textYOffset = -5f;
    [SerializeField] private float textMoveDuration = 0.1f;
    
    [Header("Loading Settings")]
    [SerializeField] private string nextSceneName = "GameScene";
    [SerializeField] private float minLoadTime = 3f;
    [SerializeField] private float maxLoadTime = 5f;
    
    private Vector3 handOriginalPosition;
    private Vector3 textOriginalPosition;
    private Quaternion leftClawOriginalRotation;
    private bool isPressed = false;
    private bool loadingStarted = false;
    
    private Tween handTween;
    private Tween textTween;
    private Tween leftClawIdleTween;
    private Tween leftClawReactTween;
    
    private void Awake()
    {
        if (buttonImage == null)
            buttonImage = GetComponent<Image>();
            
        if (hermitRightHand == null)
        {
            Debug.LogWarning("HermitButtonPress: No hermit right hand assigned!");
        }
        else
        {
            handOriginalPosition = hermitRightHand.position;
        }
        
        if (hermitLeftClaw == null)
        {
            Debug.LogWarning("HermitButtonPress: No hermit left claw assigned!");
        }
        else
        {
            leftClawOriginalRotation = hermitLeftClaw.localRotation;
        }
        
        if (buttonText == null)
        {
            Debug.LogWarning("HermitButtonPress: No button text assigned!");
        }
        else
        {
            textOriginalPosition = buttonText.localPosition;
        }
        
        // Make sure we start with the normal sprite
        if (buttonImage != null && normalButtonSprite != null)
        {
            buttonImage.sprite = normalButtonSprite;
        }
    }
    
    private void Start()
    {
        // Start the idle animation for the left claw
        StartLeftClawIdleAnimation();
    }
    
    private void StartLeftClawIdleAnimation()
    {
        if (hermitLeftClaw == null) return;
        
        // Kill any existing idle animation
        leftClawIdleTween?.Kill();
        
        // Create a sequence that rotates the claw back and forth gently
        Sequence idleSequence = DOTween.Sequence();
        
        // First half of the idle movement
        idleSequence.Append(
            hermitLeftClaw.DOLocalRotate(
                new Vector3(0, 0, leftClawOriginalRotation.eulerAngles.z + leftClawIdleAmount), 
                leftClawIdleDuration/2)
            .SetEase(Ease.InOutSine)
        );
        
        // Second half of the idle movement
        idleSequence.Append(
            hermitLeftClaw.DOLocalRotate(
                new Vector3(0, 0, leftClawOriginalRotation.eulerAngles.z - leftClawIdleAmount), 
                leftClawIdleDuration/2)
            .SetEase(Ease.InOutSine)
        );
        
        // Loop the sequence infinitely
        idleSequence.SetLoops(-1, LoopType.Yoyo);
        
        leftClawIdleTween = idleSequence;
    }
    
    private void Update()
    {
        // Check for space key press
        if (Input.GetKeyDown(KeyCode.Space) && !isPressed && !loadingStarted)
        {
            InitiateButtonPress();
        }
        else if (Input.GetKeyUp(KeyCode.Space) && isPressed && !loadingStarted)
        {
            InitiateButtonRelease();
        }
    }
    
    public void OnPointerDown(PointerEventData eventData)
    {
        if (!isPressed && !loadingStarted)
        {
            InitiateButtonPress();
            SoundManager.Instance.PlaySFX(SoundEffect.PlaceDeco);
        }
    }
    
    public void OnPointerUp(PointerEventData eventData)
    {
        if (isPressed && !loadingStarted)
        {
            InitiateButtonRelease();
        }
    }
    
    private void InitiateButtonPress()
    {
        isPressed = true;
        
        // Kill any ongoing hand tween
        handTween?.Kill();
        textTween?.Kill();
        
        // React with left claw
        AnimateLeftClawReaction();
        
        // Move hand to button position
        if (hermitRightHand != null)
        {
            handTween = hermitRightHand.DOMove(buttonTransform.position, handMoveDuration)
                .SetEase(handMoveEase)
                .OnComplete(() => {
                    // Change to pressed sprite ONLY when hand reaches button
                    if (buttonImage != null && pressedButtonSprite != null)
                    {
                        buttonImage.sprite = pressedButtonSprite;
                    }
                    
                    // Move text down when button is pressed
                    if (buttonText != null)
                    {
                        Vector3 pressedTextPosition = textOriginalPosition + new Vector3(0, textYOffset, 0);
                        textTween = buttonText.DOLocalMove(pressedTextPosition, textMoveDuration)
                            .SetEase(Ease.OutQuad);
                    }
                });
        }
    }
    
    private void AnimateLeftClawReaction()
    {
        if (hermitLeftClaw == null) return;
        
        // Pause the idle animation
        leftClawIdleTween?.Pause();
        
        // Kill any existing reaction animation
        leftClawReactTween?.Kill();
        
        // Create a quick, excited movement for the left claw
        leftClawReactTween = hermitLeftClaw.DOLocalRotate(
            new Vector3(0, 0, leftClawOriginalRotation.eulerAngles.z + leftClawReactAmount), 
            leftClawReactDuration)
            .SetEase(leftClawReactEase)
            .OnComplete(() => {
                // Return to idle animation when reaction is done
                leftClawIdleTween?.Play();
            });
    }
    
    private void InitiateButtonRelease()
    {
        isPressed = false;
        
        // Start loading when button is released
        loadingStarted = true;
        
        // Keep the button in pressed state during loading
        if (buttonImage != null && pressedButtonSprite != null)
        {
            buttonImage.sprite = pressedButtonSprite;
        }
        
        // Show the loading screen and start loading the next scene
        LoadingManager.Instance.StartLoading(nextSceneName, 
            Random.Range(minLoadTime, maxLoadTime), 
            UnityEngine.SceneManagement.SceneManager.GetActiveScene().name);
    }
    
    private void OnDestroy()
    {
        // Clean up any ongoing tweens when object is destroyed
        handTween?.Kill();
        textTween?.Kill();
        leftClawIdleTween?.Kill();
        leftClawReactTween?.Kill();
    }
}