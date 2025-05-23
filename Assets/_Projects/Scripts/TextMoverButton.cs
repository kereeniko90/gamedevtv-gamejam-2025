using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class TextMoverButton : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
{
    [Header("References")]
    [SerializeField] private RectTransform textComponent;
    [SerializeField] private Sprite normalSprite;
    [SerializeField] private Sprite pressedSprite;
    
    [Header("Settings")]
    [SerializeField] private float posYOffset = 10f;
    
    private Image buttonImage;
    private Vector3 originalTextPosition;
    private bool isPressed = false;
    
    private void Awake()
    {
        buttonImage = GetComponent<Image>();
        
        if (textComponent == null)
        {
            Debug.LogWarning("TextMoverButton: No text component assigned!");
        }
        else
        {
            originalTextPosition = textComponent.localPosition;
        }
    }
    
    public void OnPointerDown(PointerEventData eventData)
    {
        isPressed = true;
        
        // Change to pressed sprite
        if (buttonImage != null && pressedSprite != null)
        {
            buttonImage.sprite = pressedSprite;
        }
        
        // Move text component
        if (textComponent != null)
        {
            Vector3 newPosition = textComponent.localPosition;
            newPosition.y += posYOffset;
            textComponent.localPosition = newPosition;
        }
    }
    
    public void OnPointerUp(PointerEventData eventData)
    {
        isPressed = false;
        
        // Change back to normal sprite
        if (buttonImage != null && normalSprite != null)
        {
            buttonImage.sprite = normalSprite;
        }
        
        // Return text to original position
        if (textComponent != null)
        {
            textComponent.localPosition = originalTextPosition;
        }
    }
}