using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

public class UIBobber : MonoBehaviour
{
    [Header("Bob Settings")]
    [SerializeField] private bool bobOnStart = true;
    [SerializeField] private bool continuousBob = true;
    
    [Header("Movement Settings")]
    [SerializeField] private float bobHeight = 10f; // How far up and down
    [SerializeField] private float bobDuration = 1f; // Time for one complete bob cycle
    [SerializeField] private Ease bobEase = Ease.InOutSine;
    
    [Header("Timing")]
    [SerializeField] private float delayBeforeStart = 0f;
    
    private RectTransform rectTransform;
    private Vector2 originalPosition;
    private Tween currentTween;
    private bool isBobbing = false;
    
    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        originalPosition = rectTransform.anchoredPosition;
    }
    
    private void Start()
    {
        if (bobOnStart)
        {
            if (delayBeforeStart > 0)
            {
                DOVirtual.DelayedCall(delayBeforeStart, StartBobbing);
            }
            else
            {
                StartBobbing();
            }
        }
    }
    
    public void StartBobbing()
    {
        if (isBobbing) return;
        
        isBobbing = true;
        
        if (continuousBob)
        {
            StartContinuousBob();
        }
        else
        {
            BobOnce();
        }
    }
    
    public void StopBobbing()
    {
        isBobbing = false;
        currentTween?.Kill();
        
        // Return to original position smoothly
        rectTransform.DOAnchorPos(originalPosition, 0.3f).SetEase(Ease.OutQuad);
    }
    
    public void BobOnce()
    {
        currentTween?.Kill();
        
        // Create a simple up-down bob motion
        Sequence bobSeq = DOTween.Sequence();
        
        // Move up
        bobSeq.Append(rectTransform.DOAnchorPos(originalPosition + new Vector2(0, bobHeight), bobDuration * 0.5f).SetEase(bobEase));
        
        // Move down to original position
        bobSeq.Append(rectTransform.DOAnchorPos(originalPosition, bobDuration * 0.5f).SetEase(bobEase));
        
        currentTween = bobSeq;
        bobSeq.Play();
    }
    
    public void BobUpDown()
    {
        currentTween?.Kill();
        
        // More pronounced up-down motion
        Sequence bobSeq = DOTween.Sequence();
        
        // Move up
        bobSeq.Append(rectTransform.DOAnchorPos(originalPosition + new Vector2(0, bobHeight), bobDuration * 0.25f).SetEase(bobEase));
        
        // Move down below original
        bobSeq.Append(rectTransform.DOAnchorPos(originalPosition + new Vector2(0, -bobHeight * 0.5f), bobDuration * 0.5f).SetEase(bobEase));
        
        // Return to original
        bobSeq.Append(rectTransform.DOAnchorPos(originalPosition, bobDuration * 0.25f).SetEase(bobEase));
        
        currentTween = bobSeq;
        bobSeq.Play();
    }
    
    public void FloatGently()
    {
        currentTween?.Kill();
        
        // Gentle floating motion - smaller movement, slower
        float gentleHeight = bobHeight * 0.3f;
        float gentleDuration = bobDuration * 1.5f;
        
        Sequence floatSeq = DOTween.Sequence();
        floatSeq.Append(rectTransform.DOAnchorPos(originalPosition + new Vector2(0, gentleHeight), gentleDuration * 0.5f).SetEase(Ease.InOutSine));
        floatSeq.Append(rectTransform.DOAnchorPos(originalPosition, gentleDuration * 0.5f).SetEase(Ease.InOutSine));
        
        currentTween = floatSeq;
        floatSeq.Play();
    }
    
    private void StartContinuousBob()
    {
        if (!isBobbing) return;
        
        currentTween?.Kill();
        
        // Infinite up-down motion
        Sequence bobSeq = DOTween.Sequence();
        
        // Move up
        bobSeq.Append(rectTransform.DOAnchorPos(originalPosition + new Vector2(0, bobHeight), bobDuration * 0.5f).SetEase(bobEase));
        
        // Move down
        bobSeq.Append(rectTransform.DOAnchorPos(originalPosition, bobDuration * 0.5f).SetEase(bobEase));
        
        // Loop infinitely
        bobSeq.SetLoops(-1, LoopType.Restart);
        
        currentTween = bobSeq;
        bobSeq.Play();
    }
    
    // Preset bob styles
    public void BobGentle()
    {
        bobHeight = 5f;
        bobDuration = 2f;
        bobEase = Ease.InOutSine;
        BobOnce();
    }
    
    public void BobNormal()
    {
        bobHeight = 10f;
        bobDuration = 1f;
        bobEase = Ease.InOutQuad;
        BobOnce();
    }
    
    public void BobEnergetic()
    {
        bobHeight = 15f;
        bobDuration = 0.6f;
        bobEase = Ease.OutBounce;
        BobOnce();
    }
    
    // Test methods for Inspector
    [ContextMenu("Test Bob Once")]
    public void TestBobOnce()
    {
        BobOnce();
    }
    
    [ContextMenu("Test Bob Up Down")]
    public void TestBobUpDown()
    {
        BobUpDown();
    }
    
    [ContextMenu("Test Float Gently")]
    public void TestFloatGently()
    {
        FloatGently();
    }
    
    [ContextMenu("Test Gentle Bob")]
    public void TestGentleBob()
    {
        BobGentle();
    }
    
    [ContextMenu("Test Energetic Bob")]
    public void TestEnergeticBob()
    {
        BobEnergetic();
    }
    
    // Public method to change bob settings at runtime
    public void SetBobSettings(float height, float duration, Ease ease)
    {
        bobHeight = height;
        bobDuration = duration;
        bobEase = ease;
    }
    
    private void OnDestroy()
    {
        currentTween?.Kill();
    }
}