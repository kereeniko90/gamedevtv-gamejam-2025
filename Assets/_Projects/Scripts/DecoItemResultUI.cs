using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class DecoItemResultUI : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private Image itemImage;
    [SerializeField] private TextMeshProUGUI pointsText;

    [Header("Text Colors")]
    [SerializeField] private Color greyColor = new Color(0.5f, 0.5f, 0.5f, 1f);
    [SerializeField] private Color orangeColor = new Color(1f, 0.6f, 0f, 1f);
    [SerializeField] private Color greenColor = new Color(0f, 0.8f, 0f, 1f);

    private void Awake()
    {
        // Auto-find components if not assigned
        if (itemImage == null)
        {
            itemImage = GetComponentInChildren<Image>();
        }

        if (pointsText == null)
        {
            pointsText = GetComponentInChildren<TextMeshProUGUI>();
        }
    }

    public void SetupResult(DecoItemResult result)
    {
        // Set item sprite
        if (itemImage != null && result.itemSprite != null)
        {
            itemImage.sprite = result.itemSprite;
            itemImage.preserveAspect = true;
        }

        // Set points text and color
        if (pointsText != null)
        {
            pointsText.text = result.pointsEarned.ToString();

            // Determine color based on percentage
            float percentage = result.GetPercentage();
            Color textColor;

            if (percentage >= 1f)
            {
                textColor = greenColor;
            }
            else if (percentage >= 0.5f)
            {
                textColor = orangeColor;
            }
            else
            {
                textColor = greyColor;
            }

            pointsText.color = textColor;
        }

        Debug.Log($"DecoItemResultUI: Setup {result.itemName} - {result.pointsEarned}/{result.maxPossiblePoints} points ({result.GetPercentage() * 100:F1}%)");
    }

    // Optional: Add hover effect or animation
    public void OnPointerEnter()
    {
        // Could add a slight scale up effect
        transform.localScale = Vector3.one * 1.05f;
    }

    public void OnPointerExit()
    {
        // Return to normal scale
        transform.localScale = Vector3.one;
    }
}