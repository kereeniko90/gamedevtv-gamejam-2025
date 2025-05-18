using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class HermitPreferenceHint : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private GameObject thoughtBubble;
    [SerializeField] private Text thoughtText;
    
    [Header("Timing")]
    [SerializeField] private float showDelay = 30f; // Time after day starts before showing hint
    [SerializeField] private float showDuration = 10f; // How long to show the hint
    
    // Reference to the decoration calculator
    private DecorationScoreCalculator decorationCalculator;
    
    // Track if hint shown today
    private bool hintShownToday = false;
    
    private void Start()
    {
        // Find calculator
        decorationCalculator = FindFirstObjectByType<DecorationScoreCalculator>();
        
        // Hide thought bubble initially
        if (thoughtBubble != null)
        {
            thoughtBubble.SetActive(false);
        }
        
        // Subscribe to day start event
        TimeController timeController = TimeController.Instance;
        if (timeController != null)
        {
            timeController.onDayStart += OnDayStart;
        }
    }
    
    private void OnDayStart()
    {
        // Reset hint status
        hintShownToday = false;
        
        // Schedule hint to appear later in the day
        StartCoroutine(ShowHintAfterDelay());
    }
    
    private IEnumerator ShowHintAfterDelay()
    {
        // Wait for delay
        yield return new WaitForSeconds(showDelay);
        
        // Only show hint if not already shown today
        if (!hintShownToday)
        {
            ShowHint();
        }
    }
    
    public void ShowHint()
    {
        if (thoughtBubble == null || thoughtText == null) return;
        
        // Mark as shown
        hintShownToday = true;
        
        // Get hint text
        string hint = GetHintText();
        
        // Set text
        thoughtText.text = hint;
        
        // Show thought bubble
        thoughtBubble.SetActive(true);
        
        // Schedule hiding
        StartCoroutine(HideAfterDuration());
    }
    
    private IEnumerator HideAfterDuration()
    {
        yield return new WaitForSeconds(showDuration);
        
        // Hide thought bubble
        if (thoughtBubble != null)
        {
            thoughtBubble.SetActive(false);
        }
    }
    
    private string GetHintText()
    {
        // If we have a decoration calculator, get hint from there
        if (decorationCalculator != null)
        {
            return decorationCalculator.GetPreferredThemeHint();
        }
        
        // Fallback hints
        string[] fallbackHints = new string[]
        {
            "I wonder how my home is doing today...",
            "Hope everything is tidy when I get back!",
            "I've been thinking about redecorating...",
            "A nice plant would brighten up the place...",
            "Maybe I should rearrange the furniture?",
            "I'm really tired of clutter..."
        };
        
        // Return random hint
        return fallbackHints[Random.Range(0, fallbackHints.Length)];
    }
}