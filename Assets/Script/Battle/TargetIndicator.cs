// --- START OF FILE TargetIndicator.cs ---

using System;
using UnityEngine;

public class TargetIndicator : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private SpriteRenderer spriteRenderer; // Or MeshRenderer, or use an outline component

    [Header("Settings")]
    [SerializeField] private Color highlightColor = Color.yellow; // Color when targetable
    [SerializeField] private Color defaultColor = Color.white; // Original color

    private bool isHighlighted = false;

    void Awake()
    {
        if (spriteRenderer == null)
        {
            spriteRenderer = GetComponentInChildren<SpriteRenderer>(); // Try to find in children
            if (spriteRenderer == null)
            {
                Debug.LogWarning($"SpriteRenderer not found on {gameObject.name}. Targeting visuals may not work.");
            }
        }
        // Store original color if needed, or just use defaultColor
        if (spriteRenderer != null)
        {
             defaultColor = spriteRenderer.color;
        }
    }

    /// <summary>
    /// Sets the visual state of the indicator.
    /// </summary>
    /// <param name="highlight">True to show highlight, false to show default.</param>
    public void SetHighlight(bool highlight)
    {
        if (spriteRenderer == null || isHighlighted == highlight) return;
        isHighlighted = highlight;
        if (highlight)
        {
            spriteRenderer.color = highlightColor; // Apply highlight color
            // TODO: Add other visual effects like scaling, pulsating, outline
        }
        else
        {
            spriteRenderer.color = defaultColor; // Restore original color
            // TODO: Remove other visual effects
        }
    }

     /// <summary>
     /// Returns the current highlight state.
     /// </summary>
     public bool IsHighlighted()
     {
         return isHighlighted;
     }
}
// --- END OF FILE TargetIndicator.cs ---