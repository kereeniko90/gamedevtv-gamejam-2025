using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

public class UIWiggler : MonoBehaviour
{
    [Header("Wiggle Settings")]
    [SerializeField] private bool wiggleOnStart = true;
    [SerializeField] private bool continuousWiggle = true;
    
    [Header("Position Wiggle")]
    [SerializeField] private Vector2 wiggleAmount = new Vector2(10f, 10f);
    [SerializeField] private float wiggleDuration = 0.5f;
    [SerializeField] private Ease wiggleEase = Ease.InOutSine;
    
    [Header("Timing")]
    [SerializeField] private float delayBetweenWiggles = 1f;
    
    private RectTransform rectTransform;
    private Vector2 originalPosition;
    private Tween currentTween;
    private bool isWiggling = false;
    
    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        originalPosition = rectTransform.anchoredPosition;
    }
    
    private void Start()
    {
        if (wiggleOnStart)
        {
            StartWiggling();
        }
    }
    
    public void StartWiggling()
    {
        if (isWiggling) return;
        
        isWiggling = true;
        
        if (continuousWiggle)
        {
            WiggleLoop();
        }
        else
        {
            WiggleOnce();
        }
    }
    
    public void StopWiggling()
    {
        isWiggling = false;
        currentTween?.Kill();
        
        // Return to original position
        rectTransform.DOAnchorPos(originalPosition, 0.2f);
    }
    
    public void WiggleOnce()
    {
        currentTween?.Kill();
        
        // Create a simple back-and-forth wiggle
        Sequence wiggleSeq = DOTween.Sequence();
        
        // Move right
        wiggleSeq.Append(rectTransform.DOAnchorPos(originalPosition + new Vector2(wiggleAmount.x, 0), wiggleDuration * 0.25f).SetEase(wiggleEase));
        
        // Move left
        wiggleSeq.Append(rectTransform.DOAnchorPos(originalPosition + new Vector2(-wiggleAmount.x, 0), wiggleDuration * 0.5f).SetEase(wiggleEase));
        
        // Return to center
        wiggleSeq.Append(rectTransform.DOAnchorPos(originalPosition, wiggleDuration * 0.25f).SetEase(wiggleEase));
        
        currentTween = wiggleSeq;
        wiggleSeq.Play();
    }
    
    public void WiggleRandom()
    {
        currentTween?.Kill();
        
        // Random wiggle in different directions
        Sequence wiggleSeq = DOTween.Sequence();
        
        for (int i = 0; i < 4; i++)
        {
            Vector2 randomOffset = new Vector2(
                Random.Range(-wiggleAmount.x, wiggleAmount.x),
                Random.Range(-wiggleAmount.y, wiggleAmount.y)
            );
            
            wiggleSeq.Append(rectTransform.DOAnchorPos(originalPosition + randomOffset, wiggleDuration * 0.2f).SetEase(wiggleEase));
        }
        
        // Return to original
        wiggleSeq.Append(rectTransform.DOAnchorPos(originalPosition, wiggleDuration * 0.2f).SetEase(wiggleEase));
        
        currentTween = wiggleSeq;
        wiggleSeq.Play();
    }
    
    public void WiggleScale()
    {
        currentTween?.Kill();
        
        Vector3 originalScale = rectTransform.localScale;
        Vector3 biggerScale = originalScale * 1.2f;
        
        Sequence scaleSeq = DOTween.Sequence();
        scaleSeq.Append(rectTransform.DOScale(biggerScale, wiggleDuration * 0.5f).SetEase(Ease.OutQuad));
        scaleSeq.Append(rectTransform.DOScale(originalScale, wiggleDuration * 0.5f).SetEase(Ease.InQuad));
        
        currentTween = scaleSeq;
        scaleSeq.Play();
    }
    
    public void WiggleRotation()
    {
        currentTween?.Kill();
        
        Vector3 originalRotation = rectTransform.eulerAngles;
        
        Sequence rotSeq = DOTween.Sequence();
        rotSeq.Append(rectTransform.DORotate(originalRotation + new Vector3(0, 0, 15f), wiggleDuration * 0.25f).SetEase(wiggleEase));
        rotSeq.Append(rectTransform.DORotate(originalRotation + new Vector3(0, 0, -15f), wiggleDuration * 0.5f).SetEase(wiggleEase));
        rotSeq.Append(rectTransform.DORotate(originalRotation, wiggleDuration * 0.25f).SetEase(wiggleEase));
        
        currentTween = rotSeq;
        rotSeq.Play();
    }
    
    private void WiggleLoop()
    {
        if (!isWiggling) return;
        
        WiggleRandom();
        
        // Schedule next wiggle
        DOVirtual.DelayedCall(wiggleDuration + delayBetweenWiggles, () => {
            if (isWiggling)
            {
                WiggleLoop();
            }
        });
    }
    
    // Test methods you can call from inspector or other scripts
    [ContextMenu("Test Wiggle Once")]
    public void TestWiggleOnce()
    {
        WiggleOnce();
    }
    
    [ContextMenu("Test Wiggle Random")]
    public void TestWiggleRandom()
    {
        WiggleRandom();
    }
    
    [ContextMenu("Test Wiggle Scale")]
    public void TestWiggleScale()
    {
        WiggleScale();
    }
    
    [ContextMenu("Test Wiggle Rotation")]
    public void TestWiggleRotation()
    {
        WiggleRotation();
    }
    
    private void OnDestroy()
    {
        currentTween?.Kill();
    }
}