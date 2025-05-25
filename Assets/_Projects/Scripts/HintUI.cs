using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;
using System.Collections;

public class HintUI : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private GameObject hintPanel;
    [SerializeField] private TextMeshProUGUI descriptionText;
    [SerializeField] private CanvasGroup hintCanvasGroup;
    
    [Header("Animation Settings")]
    [SerializeField] private float fadeInDuration = 0.3f;
    [SerializeField] private float fadeOutDuration = 0.2f;
    [SerializeField] private Ease fadeEase = Ease.OutQuad;
    
    [Header("Typewriter Settings")]
    [SerializeField] private float typewriterSpeed = 30f; // Characters per second
    [SerializeField] private bool skipTypewriterOnRepeatedText = true;
    
    // Singleton for easy access
    public static HintUI Instance { get; private set; }
    
    private Tween fadeTween;
    private Coroutine typewriterCoroutine;
    private string currentDisplayedText = "";
    private string lastShownDescription = "";
    private bool isVisible = false;
    
    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            SetupHintUI();
        }
        else
        {
            Destroy(gameObject);
        }
    }
    
    private void Start()
    {
        // Ensure hint is hidden at start
        HideHint();
    }
    
    private void SetupHintUI()
    {
        // Get components
        if (hintCanvasGroup == null && hintPanel != null)
        {
            hintCanvasGroup = hintPanel.GetComponent<CanvasGroup>();
            if (hintCanvasGroup == null)
            {
                hintCanvasGroup = hintPanel.AddComponent<CanvasGroup>();
            }
        }
        
        // Initial setup
        if (hintCanvasGroup != null)
        {
            hintCanvasGroup.alpha = 0f;
            hintCanvasGroup.blocksRaycasts = false;
        }
        
        if (hintPanel != null)
        {
            hintPanel.SetActive(false);
        }
    }
    
    public void ShowHint(string description)
    {
        if (string.IsNullOrEmpty(description))
        {
            HideHint();
            return;
        }
        
        // If already showing the same description, don't restart
        if (isVisible && description == lastShownDescription && skipTypewriterOnRepeatedText)
        {
            return;
        }
        
        lastShownDescription = description;
        
        // Stop any ongoing animations
        fadeTween?.Kill();
        if (typewriterCoroutine != null)
        {
            StopCoroutine(typewriterCoroutine);
        }
        
        // Show panel
        if (hintPanel != null)
        {
            hintPanel.SetActive(true);
        }
        
        // Fade in
        if (hintCanvasGroup != null)
        {
            fadeTween = hintCanvasGroup.DOFade(1f, fadeInDuration)
                .SetEase(fadeEase)
                .OnComplete(() => {
                    isVisible = true;
                    // Start typewriter effect
                    typewriterCoroutine = StartCoroutine(TypewriterEffect(description));
                });
        }
        else
        {
            isVisible = true;
            typewriterCoroutine = StartCoroutine(TypewriterEffect(description));
        }
    }
    
    public void HideHint()
    {
        if (!isVisible && (hintPanel == null || !hintPanel.activeInHierarchy))
        {
            return;
        }
        
        lastShownDescription = "";
        
        // Stop any ongoing animations
        fadeTween?.Kill();
        if (typewriterCoroutine != null)
        {
            StopCoroutine(typewriterCoroutine);
            typewriterCoroutine = null;
        }
        
        if (hintCanvasGroup != null)
        {
            fadeTween = hintCanvasGroup.DOFade(0f, fadeOutDuration)
                .SetEase(fadeEase)
                .OnComplete(() => {
                    isVisible = false;
                    if (hintPanel != null)
                    {
                        hintPanel.SetActive(false);
                    }
                    currentDisplayedText = "";
                    if (descriptionText != null)
                    {
                        descriptionText.text = "";
                    }
                });
        }
        else
        {
            isVisible = false;
            if (hintPanel != null)
            {
                hintPanel.SetActive(false);
            }
            currentDisplayedText = "";
            if (descriptionText != null)
            {
                descriptionText.text = "";
            }
        }
    }
    
    private IEnumerator TypewriterEffect(string fullText)
    {
        if (descriptionText == null)
        {
            yield break;
        }
        
        currentDisplayedText = "";
        descriptionText.text = "";
        
        float characterDelay = 1f / typewriterSpeed;
        
        for (int i = 0; i < fullText.Length; i++)
        {
            // Check if we should stop (hint was hidden or new text started)
            if (!isVisible || lastShownDescription != fullText)
            {
                yield break;
            }
            
            currentDisplayedText += fullText[i];
            descriptionText.text = currentDisplayedText;
            
            // Wait for next character
            yield return new WaitForSeconds(characterDelay);
        }
        
        typewriterCoroutine = null;
    }
    
    // Public method to instantly show full text (useful for repeated shows)
    public void ShowHintInstant(string description)
    {
        bool wasSkipping = skipTypewriterOnRepeatedText;
        skipTypewriterOnRepeatedText = false;
        ShowHint(description);
        skipTypewriterOnRepeatedText = wasSkipping;
        
        // Force instant completion
        if (typewriterCoroutine != null)
        {
            StopCoroutine(typewriterCoroutine);
            typewriterCoroutine = null;
        }
        
        if (descriptionText != null)
        {
            descriptionText.text = description;
            currentDisplayedText = description;
        }
    }
    
    // Public getters
    public bool IsVisible => isVisible;
    public string CurrentDescription => lastShownDescription;
    
    private void OnDestroy()
    {
        fadeTween?.Kill();
        if (typewriterCoroutine != null)
        {
            StopCoroutine(typewriterCoroutine);
        }
    }
}