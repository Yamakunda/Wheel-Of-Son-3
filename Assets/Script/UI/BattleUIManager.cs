// --- START OF FILE BattleUIManager.cs ---

using System.Collections.Generic;
using UnityEngine.EventSystems;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class BattleUIManager : MonoBehaviour
{
    [Header("References")]
    [SerializeField] public BattleManager battleManager; // Add this reference!
    [SerializeField] private Transform skillButtonContainer;
    [SerializeField] private GameObject skillButtonPrefab;

    [Header("Button Layout")]
    [SerializeField] private float buttonSpacing = 120f; // Space between buttons on X-axis
    [SerializeField] private float initialXOffset = 0f;  // Starting X position

    [Header("Tooltip")]
    [SerializeField] private GameObject skillTooltip;
    [SerializeField] private TextMeshProUGUI tooltipTitleText;
    [SerializeField] private TextMeshProUGUI tooltipDescriptionText;
    [SerializeField] private TextMeshProUGUI tooltipStatText;

    [Header("Character Tooltip")]
    [SerializeField] private GameObject characterTooltip;
    [SerializeField] private TextMeshProUGUI characterNameText;
    [SerializeField] private TextMeshProUGUI characterStatsText;
    [SerializeField] private TextMeshProUGUI characterHealthText;

    // Dictionary to keep track of skill buttons and their associated skills
    private Dictionary<Button, Skill> skillButtonMap = new Dictionary<Button, Skill>();

    // Currently selected player
    private Player currentPlayer;

    private void Awake()
    {
        // Hide tooltips by default
        if (skillTooltip != null)
        {
            skillTooltip.SetActive(false);
        }

        if (characterTooltip != null)
        {
            characterTooltip.SetActive(false);
        }

        // Find the BattleManager if not assigned in the inspector
        if (battleManager == null)
        {
            battleManager = FindFirstObjectByType<BattleManager>();
            if (battleManager == null)
            {
                Debug.LogError("BattleManager not found! BattleUIManager cannot function without it.");
            }
        }
    }

    /// <summary>
    /// Initialize the UI for the given player
    /// </summary>
    /// <param name="player">The player whose skills should be displayed</param>
    public void InitializePlayerUI(Player player)
    {
        currentPlayer = player;
        ClearSkillButtons();

        if (player == null || player.Skills.Count == 0)
        {
            Debug.LogWarning("Player or player skills are null");
            return;
        }

        // Create a button for each skill
        for (int i = 0; i < player.Skills.Count; i++)
        {
            CreateSkillButton(player.Skills[i], i);
        }
    }

    /// <summary>
    /// Creates a button for a skill and sets up its event handlers
    /// </summary>
    /// <param name="skill">The skill to create a button for</param>
    /// <param name="index">Index of the skill for positioning</param>
    private void CreateSkillButton(Skill skill, int index)
    {
        if (skillButtonPrefab == null || skillButtonContainer == null)
        {
            Debug.LogError("Skill button prefab or container not assigned");
            return;
        }

        // Create button
        GameObject buttonObj = Instantiate(skillButtonPrefab, skillButtonContainer);
        Button button = buttonObj.GetComponent<Button>();

        // Set up the button layout to start from the left side
        RectTransform rectTransform = buttonObj.GetComponent<RectTransform>();
        if (rectTransform != null)
        {
            // Set anchors to left-center of parent
            rectTransform.anchorMin = new Vector2(0, 0.5f);
            rectTransform.anchorMax = new Vector2(0, 0.5f);

            // Set pivot to left-center of button
            rectTransform.pivot = new Vector2(0, 0.5f);

            // Position button with proper spacing from left edge
            // Since pivot is at left edge, this properly aligns buttons sequentially
            rectTransform.anchoredPosition = new Vector2(initialXOffset + (index * buttonSpacing), 0);
        }

        // Set button text
        TextMeshProUGUI buttonText = buttonObj.GetComponentInChildren<TextMeshProUGUI>();
        if (buttonText != null)
        {
            buttonText.text = skill.Name;
        }

        // Set up click event - NOW CALLS BATTLEMANAGER
        button.onClick.AddListener(() => OnSkillButtonClicked(skill));

        // Set up hover events
        EventTrigger trigger = buttonObj.GetComponent<EventTrigger>();
        if (trigger == null)
        {
            trigger = buttonObj.AddComponent<EventTrigger>();
        }

        // Add pointer enter event
        EventTrigger.Entry enterEntry = new EventTrigger.Entry();
        enterEntry.eventID = EventTriggerType.PointerEnter;
        enterEntry.callback.AddListener((data) => OnSkillButtonHover(skill));
        trigger.triggers.Add(enterEntry);

        // Add pointer exit event
        EventTrigger.Entry exitEntry = new EventTrigger.Entry();
        exitEntry.eventID = EventTriggerType.PointerExit;
        exitEntry.callback.AddListener((data) => OnSkillButtonExit());
        trigger.triggers.Add(exitEntry);

        // Store button-skill mapping
        skillButtonMap.Add(button, skill);
    }

    /// <summary>
    /// Called when a skill button is clicked - NOW NOTIFIES BATTLEMANAGER
    /// </summary>
    /// <param name="skill">The skill that was clicked</param>
    private void OnSkillButtonClicked(Skill skill)
    {
        if (currentPlayer != null && battleManager != null)
        {
            battleManager.PlayerSelectSkill(skill); // Notify BattleManager
            Debug.Log($"UI notified BattleManager: Skill {skill.Name} selected.");
        }
        else
        {
             Debug.LogWarning("Skill button clicked, but currentPlayer or battleManager is null.");
        }
    }

    /// <summary>
    /// Called when the pointer enters a skill button
    /// </summary>
    /// <param name="skill">The skill being hovered</param>
    private void OnSkillButtonHover(Skill skill)
    {
        if (skillTooltip == null)
        {
            return;
        }

        // Update tooltip content
        if (tooltipTitleText != null)
        {
            tooltipTitleText.text = skill.Name;
        }

        if (tooltipDescriptionText != null)
        {
            string targetType = "Single Target";
            if (skill.Targets == "splash")
            {
                targetType = "Area Effect (Splash)";
            }
             else if (skill.Targets == "all")
            {
                targetType = "Area Effect (All)";
            }
            else if (skill.Targets == "self")
            {
                targetType = "Self Buff"; // Or Healing, etc.
            }

            tooltipDescriptionText.text = $"Targets: {targetType}\nMana Cost: {skill.ManaCost}";
        }

        if (tooltipStatText != null)
        {
            float baseStat = 0;
            string statType = skill.StatType.ToLower();

            if (currentPlayer != null)
            {
                // Get the appropriate base stat value
                if (statType == "strength")
                {
                    baseStat = currentPlayer.Strength;
                }
                else if (statType == "magic")
                {
                    baseStat = currentPlayer.Magic;
                }
                else if (statType == "defense")
                {
                     // Defense based skills might work differently (e.g., give a shield)
                     baseStat = currentPlayer.Defense;
                }
                 else if (statType == "speed")
                {
                    // Speed based skills might affect turn order or multiple hits
                    baseStat = currentPlayer.Speed;
                }


                // Calculate estimated damage or effect
                float effectValue = baseStat * skill.DamageMultiplier;

                 tooltipStatText.text = $"Base Stat: {statType.ToUpper()}\n" +
                                       $"Power: {effectValue:F0}\n" +
                                       $"Multiplier: {skill.DamageMultiplier:F1}x";

                if (skill.Targets == "splash" && skill.SplashMultiplier > 0)
                {
                    tooltipStatText.text += $"\nSplash Dmg: {(effectValue * skill.SplashMultiplier):F0}";
                }
            } else {
                 tooltipStatText.text = $"Base Stat: {statType.ToUpper()}\n" +
                                       $"Multiplier: {skill.DamageMultiplier:F1}x\n" +
                                       "Character stat unavailable.";
            }
        }

        // Position and show tooltip (near the cursor)
        skillTooltip.transform.position = Input.mousePosition + new Vector3(0, 120, 0); // Adjust offset as needed
        skillTooltip.SetActive(true);
    }

    /// <summary>
    /// Called when the pointer exits a skill button
    /// </summary>
    private void OnSkillButtonExit()
    {
        if (skillTooltip != null)
        {
            skillTooltip.SetActive(false);
        }
    }

    /// <summary>
    /// Display character or enemy stats when hovering over them
    /// </summary>
    /// <param name="character">The character to display stats for</param>
    public void OnCharacterHover(Character character)
    {
        if (characterTooltip == null || character == null) // Add null check for character
        {
            return;
        }

        // Hide skill tooltip if it's visible
        if (skillTooltip != null && skillTooltip.activeSelf)
        {
            skillTooltip.SetActive(false);
        }

        // Update character tooltip content
        if (characterNameText != null)
        {
            characterNameText.text = character.Name;
        }

        if (characterStatsText != null)
        {
            characterStatsText.text = $"STR: {character.Strength:F0}\n" + // Format to remove decimals
                                     $"MAG: {character.Magic:F0}\n" +
                                     $"DEF: {character.Defense:F0}\n" +
                                     $"SPD: {character.Speed:F0}";
        }

        if (characterHealthText != null)
        {
            characterHealthText.text = $"HP: {character.CurrentHp:F0}/{character.MaxHp:F0}\n" +
                                       $"MP: {character.Mana:F0}/{character.MaxMana:F0}";
        }

        // Position tooltip near the cursor
        // Adjust position to appear to the right or left based on screen side
        Vector3 tooltipPosition = Input.mousePosition;
        if(tooltipPosition.x > Screen.width / 2)
        {
             // If on the right side, position tooltip to the left of the cursor
            tooltipPosition.x -= 200; // Adjust offset as needed
        }
        else
        {
             // If on the left side, position tooltip to the right of the cursor
            tooltipPosition.x += 200; // Adjust offset as needed
        }
         characterTooltip.transform.position = tooltipPosition;

        characterTooltip.SetActive(true);
    }

    /// <summary>
    /// Hide character tooltip when the pointer leaves a character
    /// </summary>
    public void OnCharacterExit()
    {
        if (characterTooltip != null)
        {
            characterTooltip.SetActive(false);
        }
    }

    /// <summary>
    /// Clears all skill buttons from the container
    /// </summary>
    private void ClearSkillButtons()
    {
        skillButtonMap.Clear();

        if (skillButtonContainer != null)
        {
            foreach (Transform child in skillButtonContainer)
            {
                Destroy(child.gameObject);
            }
        }
    }

    /// <summary>
    /// Updates the UI to reflect player's current mana, enabling/disabling skill buttons
    /// </summary>
    /// <param name="player">The player whose mana to check</param>
    public void UpdateSkillButtonsInteractability(Player player)
    {
        if (player == null)
        {
            return;
        }

        foreach (var pair in skillButtonMap)
        {
            Button button = pair.Key;
            Skill skill = pair.Value;

            // Disable button if player doesn't have enough mana
            if (button != null) // Add null check for button
            {
                 button.interactable = player.Mana >= skill.ManaCost;
            }
        }
    }

     // Call this from BattleManager to hide the skill UI
     public void HidePlayerUI()
     {
         if (skillButtonContainer != null)
         {
             skillButtonContainer.gameObject.SetActive(false);
         }
         // Also hide tooltips
         OnSkillButtonExit();
         OnCharacterExit();
     }

    // Call this from BattleManager to show the skill UI
     public void ShowPlayerUI()
     {
         if (skillButtonContainer != null)
         {
              // Re-initialize if needed, or just set active if already initialized
              if(currentPlayer != null) InitializePlayerUI(currentPlayer); // Rebuilds if player changes or first time
              skillButtonContainer.gameObject.SetActive(true);
         }
     }

}
// --- END OF FILE BattleUIManager.cs ---