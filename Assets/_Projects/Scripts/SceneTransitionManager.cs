using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

public class SceneTransitionManager : MonoBehaviour
{
    [Header("Transition Settings")]
    [SerializeField] private float fadeInDuration = 1.0f;
    [SerializeField] private Ease fadeInEase = Ease.OutQuad;
    
    private CanvasGroup blackOverlay;
    private Canvas transitionCanvas;
    private Tween fadeTween;
    
    // Call this to set up the transition components if they don't exist yet
    public void SetupTransition()
    {
        // Create canvas if it doesn't exist
        if (transitionCanvas == null)
        {
            transitionCanvas = gameObject.GetComponent<Canvas>();
            if (transitionCanvas == null)
            {
                transitionCanvas = gameObject.AddComponent<Canvas>();
                transitionCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
                transitionCanvas.sortingOrder = 999; // Make sure it renders on top of everything
                
                // Add canvas scaler for proper UI scaling
                CanvasScaler scaler = gameObject.AddComponent<CanvasScaler>();
                scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                scaler.referenceResolution = new Vector2(1920, 1080);
                
                // Add raycaster to block input during transition
                gameObject.AddComponent<GraphicRaycaster>();
            }
        }
        
        // Create black overlay if it doesn't exist
        Transform overlayTransform = transform.Find("BlackOverlay");
        if (overlayTransform == null)
        {
            // Create the black image that covers the screen
            GameObject overlayObj = new GameObject("BlackOverlay");
            overlayObj.transform.SetParent(transform, false);
            
            // Add image component and set it to black
            Image blackImage = overlayObj.AddComponent<Image>();
            blackImage.color = Color.black;
            
            // Make it fill the entire screen
            RectTransform rectTransform = blackImage.rectTransform;
            rectTransform.anchorMin = Vector2.zero;
            rectTransform.anchorMax = Vector2.one;
            rectTransform.sizeDelta = Vector2.zero;
            rectTransform.anchoredPosition = Vector2.zero;
            
            // Add canvas group for fading
            blackOverlay = overlayObj.AddComponent<CanvasGroup>();
            blackOverlay.alpha = 1f; // Start fully opaque
            blackOverlay.blocksRaycasts = true; // Block input during transition
        }
        else
        {
            blackOverlay = overlayTransform.GetComponent<CanvasGroup>();
            if (blackOverlay == null)
                blackOverlay = overlayTransform.gameObject.AddComponent<CanvasGroup>();
                
            blackOverlay.alpha = 1f; // Ensure it starts fully opaque
            blackOverlay.blocksRaycasts = true;
        }
    }
    
    // Call this to start the fade-in transition of the scene
    public void StartFadeIn()
    {
        // Make sure the transition is set up
        if (blackOverlay == null)
            SetupTransition();
            
        // Kill any existing tween
        fadeTween?.Kill();
        
        // Start with fully black screen
        blackOverlay.alpha = 1f;
        blackOverlay.blocksRaycasts = true;
        
        // Fade to transparent
        fadeTween = blackOverlay.DOFade(0f, fadeInDuration)
            .SetEase(fadeInEase)
            .OnComplete(() => {
                // Disable the overlay once it's fully transparent
                blackOverlay.blocksRaycasts = false;
                
                // Optionally destroy this manager if no longer needed
                // Destroy(gameObject);
            });
    }
    
    private void OnDestroy()
    {
        // Clean up any ongoing tween
        fadeTween?.Kill();
    }
}