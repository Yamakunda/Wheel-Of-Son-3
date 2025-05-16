// --- START OF FILE CharacterTargetSelector.cs ---

using UnityEngine;
using UnityEngine.EventSystems; // Required for IPointerClickHandler

// This script should be attached to your Player and Enemy prefabs
public class CharacterTargetSelector : MonoBehaviour, IPointerClickHandler
{
    private Character character;
    private BattleManager battleManager;
    private TargetIndicator targetIndicator; // Reference to the indicator script

    void Awake()
    {
        character = GetComponent<Character>();
        battleManager = FindFirstObjectByType<BattleManager>(); // Find the BattleManager in the scene
        targetIndicator = GetComponent<TargetIndicator>(); // Get the indicator script

        if (character == null) Debug.LogError($"Character component not found on {gameObject.name} for CharacterTargetSelector.");
        if (battleManager == null) Debug.LogError("BattleManager not found in scene for CharacterTargetSelector.");
         // TargetIndicator warning is in TargetIndicator script
    }

    // Called by the EventSystem when the pointer clicks on this object's collider
    public void OnPointerClick(PointerEventData eventData)
    {
        // Only process clicks if we are in the target selection state AND this character is currently highlighted
        if (battleManager != null && character != null &&
            battleManager.currentState == BattleState.PlayerSelectingTarget &&
            targetIndicator != null && targetIndicator.IsHighlighted()) // Check if highlighted
        {
            Debug.Log($"Clicked on {character.Name} (Targetable) during PlayerSelectingTarget.");
            battleManager.PlayerSelectTarget(character); // Notify BattleManager of the selected target
        }
        // Ignore clicks if not in the correct state or not a targetable character
    }

    // Add methods to control the indicator from BattleManager
    public void SetTargetable(bool isTargetable)
    {
        Debug.Log($"Setting targetable state to {isTargetable} for {gameObject.name}.");
        Debug.Log($"Current state: {targetIndicator}.");
        if (targetIndicator != null)
        {
            targetIndicator.SetHighlight(isTargetable);
        }
        // You might also enable/disable the collider here if needed
        // GetComponent<Collider>().enabled = isTargetable; // Example for 3D
        // GetComponent<Collider2D>().enabled = isTargetable; // Example for 2D
    }

    // Optional: Add hover effects using IPointerEnterHandler and IPointerExitHandler
    // public void OnPointerEnter(PointerEventData eventData) { ... }
    // public void OnPointerExit(PointerEventData eventData) { ... }
}
// --- END OF FILE CharacterTargetSelector.cs ---