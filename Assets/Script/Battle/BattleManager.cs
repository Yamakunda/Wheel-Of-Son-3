using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.EventSystems;

public enum BattleState
{
    BattleStart,
    PlayerTurn,
    PlayerSelectingTarget,
    EnemyTurn,             // Enemy AI is choosing action/target
    ActionExecution,
    CheckWinLoss,
    BattleWon,
    BattleLost
}

public class BattleManager : MonoBehaviour
{
    // GameDataManager is now a persistent singleton, access via Instance
    // [SerializeField] private GameDataManager gameDataManager; // REMOVED

    [Header("UI Elements")]
    [SerializeField] private BattleUIManager battleUIManager;
    [SerializeField] private Button returnButton; // Rename startButton to returnButton
    [SerializeField] private Button debugButton;
    [SerializeField] private Button UtilityButton;
    [SerializeField] private Button UtilityButton2;

    [Header("Prefabs & Spawning")]
    [SerializeField] private GameObject playerPrefab;
    [SerializeField] private GameObject enemyPrefab;
    [SerializeField] private GameObject HpBarPrefab;
    [SerializeField] private Transform playerStartPosition;
    [SerializeField] private float spacingBetweenPlayers = 3f;
    [SerializeField] private Transform enemyStartPosition;
    [SerializeField] private float spacingBetweenEnemies = 3f;
    // [SerializeField] private int numberOfEnemies = 3; // REMOVED - Enemies are determined by wave config

    // Battle State & Turn Order
    public BattleState currentState;
    private List<Character> turnOrder = new List<Character>();
    private int turnIndex = 0;
    private Character activeCharacter; // The character whose turn it currently is

    // Action Variables
    private Skill selectedSkill; // Used for both Player and Enemy's chosen skill
    private List<Character> finalTargets = new List<Character>(); // The actual targets chosen/calculated for action execution


    // Character Lists
    private List<Player> players = new List<Player>();
    private List<Enemy> enemies = new List<Enemy>();

     // Reference to the EventSystem in the scene
     private EventSystem eventSystem;


    private void Awake()
    {
        Debug.Log("BattleManager Awake called.");
        // Get singleton instances
        if (GameDataManager.Instance == null)
        {
             Debug.LogError("GameDataManager.Instance is null! Ensure GameDataManager is in the scene and set to DontDestroyOnLoad.");
             // Handle this critical error - maybe load the main menu scene?
             return; // Stop if critical dependency is missing
        }
        Debug.Log("GameDataManager found. Proceeding with BattleManager setup.");

        // Find scene-specific components
        if (battleUIManager == null) battleUIManager = FindFirstObjectByType<BattleUIManager>();
        eventSystem = FindFirstObjectByType<EventSystem>(); // Find EventSystem

        // Assign BattleManager reference in BattleUIManager if found
        if (battleUIManager != null)
        {
             battleUIManager.battleManager = this;
             Debug.Log("Assigned BattleManager to BattleUIManager.");
        } else {
             Debug.LogError("BattleUIManager not found in scene!");
        }

        if (eventSystem == null) Debug.LogError("EventSystem not found in scene! UI and character clicks may not work.");

        // Setup Button Listeners
        if (returnButton != null) returnButton.onClick.AddListener(OnReturnButtonClicked);
        if (debugButton != null) debugButton.onClick.AddListener(OnDebugButtonClicked);
        if (UtilityButton != null) UtilityButton.onClick.AddListener(OnUtilityButtonClicked);
        if (UtilityButton2 != null) UtilityButton2.onClick.AddListener(OnUtilityButton2Clicked);

         Debug.Log("BattleManager Awake complete.");
    }

    private void Start()
    {
        currentState = BattleState.BattleStart;
        StartCoroutine(SetupBattle());
    }

    private IEnumerator SetupBattle()
    {
        Debug.Log("Setting up battle...");

        InitializePlayers();
        InitializeEnemies(); // This now uses the wave config

        // Check if initialization was successful
         if (players.Count == 0 || enemies.Count == 0)
         {
             Debug.LogError("Battle cannot start: Players or Enemies list is empty.");
             SetState(BattleState.BattleLost); // Or return to GameScene directly?
             yield break; // Exit coroutine
         }


        yield return null; // Wait a frame for instantiation

        CreateTurnOrder();

        turnIndex = -1; // Start before the first index
        StartNextTurn();

        Debug.Log("Battle setup complete. Starting first turn.");
    }

    private void CreateTurnOrder()
    {
        turnOrder.Clear();
        turnOrder.AddRange(players.Where(p => p != null && p.IsAlive()));
        turnOrder.AddRange(enemies.Where(e => e != null && e.IsAlive()));
        turnOrder = turnOrder.OrderByDescending(c => c.Speed).ToList();

        Debug.Log("Turn Order Created:");
        foreach (var character in turnOrder.Where(c => c != null)) Debug.Log($"- {character.Name} (Speed: {character.Speed})"); // Filter nulls
    }

    private void StartNextTurn()
    {
        CleanUpDeadCharacters(); // Clean up dead characters before determining the next turn

        // Check for win/loss BEFORE starting the next turn's logic
         if (CheckWinLossConditions(false)) // Just check, don't change state yet
         {
             // CheckWinLossConditions will set the state if battle ends
              Debug.Log("StartNextTurn: Battle conditions met after cleanup. Not starting new turn.");
              return; // Exit if battle is over
         }

         // If battle is not over and turn order is somehow empty, something is wrong
        if (turnOrder.Count == 0)
        {
             Debug.LogWarning("Turn order is empty after cleanup. Battle likely over, but no win/loss detected?");
             SetState(BattleState.CheckWinLoss); // Force win/loss check
             return;
        }

        turnIndex = (turnIndex + 1) % turnOrder.Count;
        activeCharacter = turnOrder[turnIndex];

         // Loop to skip dead characters in the turn order
         while(activeCharacter == null || !activeCharacter.IsAlive())
         {
             Debug.Log($"StartNextTurn: Skipping turn for dead or null character at index {turnIndex}: {activeCharacter?.Name ?? "NULL"}");
             CleanUpDeadCharacters(); // Clean up again just in case someone died during the cleanup check

             // Check win/loss again after potentially removing more characters
             if (CheckWinLossConditions(false))
             {
                 Debug.Log("StartNextTurn: Battle conditions met after skipping dead character. Not starting new turn.");
                 return; // Exit if battle is over
             }

             // If battle is still ongoing but no characters left in turn order (highly unlikely but safe)
             if (turnOrder.Count == 0) {
                 Debug.LogWarning("Turn order empty after skipping dead characters. Forcing win/loss check.");
                 SetState(BattleState.CheckWinLoss);
                 return;
             }

             // Move to the next character in the (potentially smaller) turn order
             turnIndex = (turnIndex + 1) % turnOrder.Count;
             activeCharacter = turnOrder[turnIndex];
         }


        Debug.Log($"--- Starting Turn for: {activeCharacter.Name} ---");

        selectedSkill = null;
        finalTargets.Clear();


        if (activeCharacter is Player)
        {
            SetState(BattleState.PlayerTurn);
        }
        else if (activeCharacter is Enemy)
        {
            SetState(BattleState.EnemyTurn); // EnemyTurn state handles its own coroutine
        }
        else
        {
            Debug.LogError("Character in turn order is neither Player nor Enemy! Skipping turn.");
            StartNextTurn(); // Skip this invalid turn
        }
    }

    // --- State Machine Logic ---

    private void SetState(BattleState newState)
    {
         if (currentState == newState) return; // Avoid re-entering the same state

         Debug.Log($"Battle State changing from {currentState} to: {newState}");
         // Exit logic for old state (clean up indicators, UI)
         switch (currentState)
         {
             case BattleState.PlayerTurn:
                 // Hide UI immediately when exiting PlayerTurn to prevent interaction during transition
                 if (battleUIManager != null) battleUIManager.HidePlayerUI();
                 break;
             case BattleState.PlayerSelectingTarget:
                 ClearTargetIndicators(); // Hide highlight when exiting this state
                 if (battleUIManager != null) battleUIManager.HidePlayerUI(); // Ensure UI is hidden
                 break;
             case BattleState.EnemyTurn:
                 if (battleUIManager != null) battleUIManager.HidePlayerUI(); // Ensure hidden after enemy turn ends
                 break;
             case BattleState.ActionExecution:
                 // ActionExecution coroutine handles its own cleanup (clearing selectedSkill, etc.)
                 break;
             case BattleState.BattleWon:
             case BattleState.BattleLost:
                 // EndBattleSequence handles cleanup and scene transition
                 break;
         }


        currentState = newState;


        // Execute entry logic for the new state
        switch (currentState)
        {
            case BattleState.BattleStart:
                 // SetupBattle() handles initial entry
                 break;
            case BattleState.PlayerTurn:
                EnterPlayerTurn();
                break;
            case BattleState.PlayerSelectingTarget:
                EnterPlayerSelectingTarget();
                break;
            case BattleState.EnemyTurn:
                StartCoroutine(EnterEnemyTurn()); // Start the coroutine for AI decision and action
                break;
            case BattleState.ActionExecution:
                // Entry logic is handled within the ProcessActionCoroutine which is started just before setting this state.
                 Debug.Log("Entered ActionExecution state.");
                break;
            case BattleState.CheckWinLoss:
                 Debug.Log("Entered CheckWinLoss state.");
                CheckWinLossConditions(true); // Pass true to allow state change here
                break;
            case BattleState.BattleWon:
                EnterBattleWon();
                break;
            case BattleState.BattleLost:
                EnterBattleLost();
                break;
        }
    }

    // --- State Entry Methods ---

    private void EnterPlayerTurn()
    {
        if (activeCharacter is Player player && player.IsAlive()) // Ensure character is alive
        {
            if (battleUIManager != null)
            {
                battleUIManager.InitializePlayerUI(player); // Rebuilds UI for the current player
                battleUIManager.UpdateSkillButtonsInteractability(player);
                battleUIManager.ShowPlayerUI(); // Make sure the skill UI is visible
            }
             Debug.Log($"Entering PlayerTurn for {player.Name}. Awaiting skill selection.");
        }
        else
        {
             Debug.LogWarning($"Player turn started, but active character ({activeCharacter?.Name ?? "NULL"}) is dead or not a Player. Skipping turn.");
             StartNextTurn(); // Skip to next turn if somehow a non-player or dead character ended up here
        }
    }

     private void EnterPlayerSelectingTarget()
    {
        Debug.Log($"Entering PlayerSelectingTarget state for {activeCharacter.Name} (Skill: {selectedSkill?.Name}).");
         // battleUIManager.HidePlayerUI(); // UI is already hidden when exiting previous state

        // Determine potential targets based on skill type
        List<Character> potentialTargetsList = new List<Character>(); // Local list for this state
        if (selectedSkill == null)
        {
             Debug.LogError("Entering PlayerSelectingTarget state with no skill selected!");
             SetState(BattleState.PlayerTurn); // Go back to skill selection
             return;
        }

        // Decide *which* group can be targeted based on skill type
        switch (selectedSkill.Targets.ToLower())
        {
            case "single":
            case "splash":
            case "all":
                 // Offensive skills typically target enemies
                 potentialTargetsList.AddRange(enemies.Where(e => e != null && e.IsAlive()));
                 break;
            case "self":
                 // Self-targeting skills
                 if (activeCharacter != null && activeCharacter.IsAlive())
                 {
                     potentialTargetsList.Add(activeCharacter);
                 }
                 break;
            // TODO: Add logic for other target types like "ally", "all_allies", "all_characters" targeting players
             case "ally": // Targets a single living player other than self
                 potentialTargetsList.AddRange(players.Where(p => p != null && p.IsAlive() && p != activeCharacter));
                 break;
             case "all_allies": // Targets all living players
                 potentialTargetsList.AddRange(players.Where(p => p != null && p.IsAlive()));
                 break;
             case "all_characters": // Targets all living players and enemies
                 potentialTargetsList.AddRange(players.Where(p => p != null && p.IsAlive()));
                 potentialTargetsList.AddRange(enemies.Where(e => e != null && e.IsAlive()));
                 break;
            default:
                 Debug.LogWarning($"Skill target type '{selectedSkill.Targets}' not handled in PlayerSelectingTarget. Assuming no valid targets.");
                 break;
        }


        // Show target indicators on potential targets
        ShowTargetIndicators(potentialTargetsList);

        // Wait for player to click a valid target (handled by CharacterTargetSelector).
        // The BattleManager will be notified by CharacterTargetSelector calling PlayerSelectTarget().

        // If no valid targets, go back to player turn or end turn? For now, return to player turn.
        if (potentialTargetsList.Count == 0)
        {
             Debug.LogWarning($"No valid targets found for skill {selectedSkill.Name} ({selectedSkill.Targets}). Returning to PlayerTurn.");
             selectedSkill = null; // Clear selected skill
             SetState(BattleState.PlayerTurn); // Go back to skill selection
        }
    }

    private IEnumerator EnterEnemyTurn()
    {
         // battleUIManager.HidePlayerUI(); // UI is already hidden when exiting previous state

        Debug.Log($"{activeCharacter?.Name ?? "NULL"} deciding action...");
        // Optional: Add a visual indicator that the enemy is thinking or about to act
        yield return new WaitForSeconds(1.0f); // Simulate thinking time

        if (activeCharacter is Enemy enemy && enemy.IsAlive()) // Ensure character is alive
        {
            // 1. Choose Skill using Enemy AI
            selectedSkill = enemy.ChooseSkill();

            if (selectedSkill == null)
            {
                 Debug.LogWarning($"{enemy.Name} failed to choose a skill. Skipping turn.");
                 StartNextTurn(); // Skip this enemy's turn
                 yield break; // Exit coroutine
            }

            // 2. Choose Target(s) using Enemy AI
            finalTargets.Clear(); // Clear previous targets
            // Call the AI method in Enemy to get the intended targets
            finalTargets.AddRange(enemy.ChooseTargets(selectedSkill, players, enemies));

            // Ensure at least one valid target was chosen by AI
             if(finalTargets.Any(t => t != null && t.IsAlive()))
             {
                 Debug.Log($"{enemy.Name} chose skill '{selectedSkill.Name}' targeting {finalTargets.Count} character(s).");
                // 3. Execute Action - Start the coroutine
                StartCoroutine(ProcessActionCoroutine(enemy, selectedSkill, new List<Character>(finalTargets))); // Pass a copy
             } else {
                 Debug.LogWarning($"{enemy.Name} chose skill '{selectedSkill.Name}' but found no valid targets. Skipping turn.");
                 selectedSkill = null; // Clear selected skill
                 StartNextTurn(); // Skip this enemy's turn
             }
        }
        else
        {
            Debug.LogWarning("Enemy turn started, but active character is dead or not a valid enemy. Skipping turn.");
            StartNextTurn(); // Skip the turn
        }
    }

    // Check win/loss conditions. Returns true if battle is over.
    // Set allowStateChange to true to transition states here.
  private bool CheckWinLossConditions(bool allowStateChange)
    {
         Debug.Log($"Checking Win/Loss conditions (Allow State Change: {allowStateChange})...");
         CleanUpDeadCharacters(); // Ensure lists are up-to-date


        bool anyPlayersAlive = players.Any(p => p != null && p.IsAlive());
        bool anyEnemiesAlive = enemies.Any(e => e != null && e.IsAlive());

        // Win Condition
        if (!anyEnemiesAlive && enemies.Count > 0) // Ensure there were enemies to begin with
        {
             Debug.Log("Win Condition Met: All enemies defeated.");
             if (allowStateChange) SetState(BattleState.BattleWon);
             return true; // Battle over
        }
        // Loss Condition
        else if (!anyPlayersAlive && players.Count > 0) // Ensure there were players to begin with
        {
             Debug.Log("Loss Condition Met: All players defeated.");
             if (allowStateChange) SetState(BattleState.BattleLost);
             return true; // Battle over
        }
         // Handle initial empty lists / setup errors (less likely to be hit after first turn)
         else if (enemies.Count == 0 && players.Count > 0 && currentState != BattleState.BattleStart)
        {
             Debug.LogWarning("CheckWinLoss: No enemies were spawned. Declaring victory.");
              if (allowStateChange) SetState(BattleState.BattleWon);
              return true;
        }
         else if (players.Count == 0 && enemies.Count > 0 && currentState != BattleState.BattleStart)
        {
             Debug.LogWarning("CheckWinLoss: No players were spawned. Declaring loss.");
              if (allowStateChange) SetState(BattleState.BattleLost);
              return true;
        }
         else if (players.Count == 0 && enemies.Count == 0 && currentState == BattleState.BattleStart)
         {
              Debug.LogWarning("CheckWinLoss: No characters were spawned at all during BattleStart. Forcing loss.");
              if (allowStateChange) SetState(BattleState.BattleLost);
              return true;
         }
         else if (players.Count == 0 && enemies.Count == 0 && currentState != BattleState.BattleStart)
         {
             // Should not happen if CleanUpDeadCharacters is working, but as a safety...
             Debug.LogWarning("CheckWinLoss: Both player and enemy lists are empty after BattleStart. Forcing loss.");
             if (allowStateChange) SetState(BattleState.BattleLost);
              return true;
         }


        // Battle is still ongoing
        Debug.Log("CheckWinLoss: Battle is still ongoing.");
        if (allowStateChange) // If we are allowed to change state (called from SetState after action completion)
        {
             // Only start the next turn if we just finished an action and determined the battle isn't over
             // The state should be CheckWinLoss when this method is called with allowStateChange = true.
             if (currentState == BattleState.CheckWinLoss)
             {
                 Debug.Log("CheckWinLossConditions: Battle ongoing & allowStateChange true. Calling StartNextTurn.");
                 StartNextTurn();
             } else {
                  // This is a safety check - indicates SetState(CheckWinLoss) wasn't the call path
                  Debug.LogWarning($"CheckWinLossConditions called with allowStateChange true, but currentState is {currentState}. Not calling StartNextTurn.");
             }
        }
        return false; // Battle not over (return value primarily useful when allowStateChange is false)
    }


    private void EnterBattleWon()
    {
        Debug.Log("Battle Won! Processing rewards...");
        // TODO: Display victory message UI

        // *** NEW: Grant Rewards ***
        GrantBattleRewards();


        // Save persistent data (including updated player stats/XP)
        SaveAllPlayerData();

        // Transition back to previous scene (GameScene)
        StartCoroutine(EndBattleSequence(true));

    }

    private void EnterBattleLost()
    {
        Debug.Log("Battle Lost!");
        // TODO: Display defeat message UI
        // TODO: Handle game over logic (retry? load checkpoint? main menu?)
        // For now, just go back to the game scene (which should handle loss state)
        SaveAllPlayerData(); // Optional: Save player state even on loss? Or reset players?
        StartCoroutine(EndBattleSequence(false));
    }

     private IEnumerator EndBattleSequence(bool won)
    {
        // Hide Battle UI completely
        if (battleUIManager != null) battleUIManager.HidePlayerUI();
        ClearTargetIndicators(); // Ensure indicators are off

        // Wait a bit to see the result message/state
        yield return new WaitForSeconds(3.0f);

        // Load the GameScene
        SceneManager.LoadScene("GameScene");

        // TODO: Upon returning to GameScene, the GameManager needs to be notified that the battle is over
        // and it should continue its event loop (e.g., spin the wheel for the next event).
        // This could be done by:
        // 1. Having a static flag in GameDataManager checked by GameManager.Start() or Awake().
        // 2. Having GameManager be a persistent singleton that BattleManager calls back to.
        // 3. Using an event system that GameManager listens to upon scene load.
        // For now, the GameManager in GameScene's Awake/Start will load GameData and resume from the saved eventProgress.
        // We already increased eventProgress in GameManager.OnEventSelected, so the next event should be loaded automatically.
    }


    // --- Player Input Handling ---

    // Called by BattleUIManager when a skill button is clicked
    public void PlayerSelectSkill(Skill skill)
    {
        // Ensure we are in the correct state and the active character is a player
        if (currentState != BattleState.PlayerTurn || !(activeCharacter is Player player))
        {
            Debug.LogWarning("PlayerSelectSkill called outside of PlayerTurn or by non-player.");
            // You might want to reset state or give feedback here
            if (battleUIManager != null) battleUIManager.InitializePlayerUI(activeCharacter as Player); // Re-show UI for current player
            return;
        }

         // Ensure the player has enough mana
         if (player.Mana >= skill.ManaCost)
        {
            selectedSkill = skill;
            Debug.Log($"BattleManager received skill selection: {skill.Name}. Transitioning to targeting.");
            // Immediately transition to the target selection state
            SetState(BattleState.PlayerSelectingTarget);
        }
        else
        {
            Debug.Log($"Player {player.PlayerName} does not have enough mana ({player.Mana:F0}) for skill {skill.Name} ({skill.ManaCost})."); // Format mana
            // TODO: Provide UI feedback for insufficient mana (BattleUIManager could handle this)
        }
    }

    // Called by CharacterTargetSelector when the player clicks on a character
    public void PlayerSelectTarget(Character target)
    {
        // Ensure we are in the correct state and a skill is selected
        if (currentState != BattleState.PlayerSelectingTarget || selectedSkill == null)
        {
             Debug.LogWarning("PlayerSelectTarget called outside of PlayerSelectingTarget state or without a skill.");
             // This could happen if the player clicks very quickly. Ignore the click.
            return;
        }

         // Ensure the clicked target is valid for the selected skill type
         // Re-evaluate valid targets here based on the skill type and *living* characters.
         bool isValidTarget = false;
         List<Character> currentValidTargets = new List<Character>(); // Characters that are valid to *click*

         switch (selectedSkill.Targets.ToLower())
         {
             case "single":
             case "splash":
             case "all": // For 'all', clicking any enemy confirms the target group
                  currentValidTargets.AddRange(enemies.Where(e => e != null && e.IsAlive()));
                  break;
             case "self":
                  if (activeCharacter != null && activeCharacter.IsAlive()) currentValidTargets.Add(activeCharacter);
                  break;
             case "ally": // Clicking any living player other than self
                  currentValidTargets.AddRange(players.Where(p => p != null && p.IsAlive() && p != activeCharacter));
                  break;
             case "all_allies": // Clicking any living player confirms targeting all allies
                  currentValidTargets.AddRange(players.Where(p => p != null && p.IsAlive()));
                  break;
             case "all_characters": // Clicking any living character confirms targeting all living characters
                  currentValidTargets.AddRange(players.Where(p => p != null && p.IsAlive()));
                  currentValidTargets.AddRange(enemies.Where(e => e != null && e.IsAlive()));
                  break;
             default:
                 Debug.LogWarning($"Skill target type '{selectedSkill.Targets}' not handled for click validation in PlayerSelectTarget.");
                 break;
         }

         isValidTarget = currentValidTargets.Contains(target);


         if (!isValidTarget)
         {
              Debug.LogWarning($"Clicked target {target?.Name ?? "NULL"} is not a valid target for skill {selectedSkill.Name} ({selectedSkill.Targets}) in the current state. Ignoring click.");
              // Optionally give feedback to the player (e.g., a sound or message)
              return; // Ignore the invalid click
         }

        // Determine the *final* list of targets based on the *clicked* target and the skill's target type
        finalTargets.Clear(); // Clear targets from previous actions

         switch (selectedSkill.Targets.ToLower())
         {
             case "single":
                 finalTargets.Add(target); // Just the clicked target
                 Debug.Log($"Player chose final targets (Single): {target?.Name ?? "NULL"}");
                 break;
             case "splash":
                  // Add the clicked target + adjacent characters
                  finalTargets.Add(target);
                  // TODO: Implement logic to find characters adjacent to 'target' and add them to finalTargets
                  // This depends heavily on your scene setup and positioning.
                  // For a simple implementation, you might iterate through enemies and add those within a certain distance of 'target'.
                   float splashRadius = 2f; // Adjust radius based on your game scale
                   // Find adjacent enemies (excluding the primary target itself)
                   foreach(var enemy in enemies.Where(e => e != null && e.IsAlive() && e != target))
                   {
                       if (Vector3.Distance(target.transform.position, enemy.transform.position) <= splashRadius)
                       {
                           finalTargets.Add(enemy);
                       }
                   }
                    // Also check adjacent players if skill could target players with splash? (Less common)
                   // foreach(var player in players.Where(p => p != null && p.IsAlive() && p != target))
                   // {
                   //     if (Vector3.Distance(target.transform.position, player.transform.position) <= splashRadius)
                   //     {
                   //         finalTargets.Add(player);
                   //     }
                   // }
                  Debug.Log($"Player chose final targets (Splash): Primary: {target?.Name ?? "NULL"} + {finalTargets.Count -1} Adjacents");
                  break;
             case "all":
                  // For "all", the clicked target is just a confirmation click on any valid target.
                  // Target all living characters of the appropriate type (enemies for offensive 'all').
                  // Assuming 'all' on an enemy means 'all enemies'.
                  finalTargets.AddRange(enemies.Where(e => e != null && e.IsAlive()));
                   Debug.Log($"Player chose final targets (All Enemies): {finalTargets.Count} characters");
                  break;
             case "self":
                 if (activeCharacter != null && activeCharacter.IsAlive()) finalTargets.Add(activeCharacter); // Target the active player
                  Debug.Log($"Player chose final targets (Self): {activeCharacter?.Name ?? "NULL"}");
                 break;
              case "ally":
                 if (target != null && target.IsAlive() && target != activeCharacter) finalTargets.Add(target); // Clicked living ally (player)
                  Debug.Log($"Player chose final targets (Ally): {target?.Name ?? "NULL"}");
                 break;
              case "all_allies":
                   finalTargets.AddRange(players.Where(p => p != null && p.IsAlive())); // All living players
                   Debug.Log($"Player chose final targets (All Allies): {finalTargets.Count}");
                   break;
              case "all_characters":
                   finalTargets.AddRange(players.Where(p => p != null && p.IsAlive())); // All living players
                   finalTargets.AddRange(enemies.Where(e => e != null && e.IsAlive())); // All living enemies
                   Debug.Log($"Player chose final targets (All Characters): {finalTargets.Count}");
                   break;

             default:
                 Debug.LogError($"Skill target type '{selectedSkill.Targets}' not handled in PlayerSelectTarget final target determination.");
                 finalTargets.Clear(); // Clear targets if logic is missing
                 break;
         }

        // Ensure final targets list only contains living characters (should be implicitly handled by .Where(c => c.IsAlive()) but double check)
         finalTargets = finalTargets.Where(t => t != null && t.IsAlive()).ToList();


        // Hide targeting indicators
        ClearTargetIndicators();

        // Execute the action if we have targets
         if(finalTargets.Count > 0) // Ensure at least one valid target exists in the final list
         {
             Debug.Log($"BattleManager executing skill {selectedSkill.Name} with {finalTargets.Count} final targets.");
            // Transition to ActionExecution state and start the coroutine
            SetState(BattleState.ActionExecution); // Set state *before* starting coroutine
            StartCoroutine(ProcessActionCoroutine(activeCharacter, selectedSkill, new List<Character>(finalTargets))); // Pass a copy to prevent modification during coroutine
         } else {
              Debug.LogWarning("No valid targets found for action execution after player selection. Returning to Player Turn.");
              selectedSkill = null; // Clear selected skill
              SetState(BattleState.PlayerTurn); // Go back to allow picking a different skill
         }
    }

    // --- Targeting Indicator Management ---

     private void ShowTargetIndicators(List<Character> targetsToHighlight)
     {
         // Iterate through all characters (players and enemies)
         // Find the CharacterTargetSelector on each and set its indicator state

         // Combine players and enemies into a single list (ensure no nulls)
         List<Character> allCombatants = players.Where(p => p != null).Cast<Character>().ToList();
         allCombatants.AddRange(enemies.Where(e => e != null));


         foreach (var character in allCombatants.Where(c => c.IsAlive())) // Only show indicators on LIVING characters
         {
             CharacterTargetSelector selector = character.GetComponent<CharacterTargetSelector>();
             if (selector != null)
             {
                 bool isPotentialTarget = targetsToHighlight.Contains(character);
                 selector.SetTargetable(isPotentialTarget);
             } else {
                  // Debug.LogWarning($"Character {character.Name} is missing CharacterTargetSelector component.");
             }
         }
     }

     private void ClearTargetIndicators()
     {
         // Iterate through all characters and turn off their indicators
          List<Character> allCombatants = players.Where(p => p != null).Cast<Character>().ToList();
         allCombatants.AddRange(enemies.Where(e => e != null));

         foreach (var character in allCombatants) // Check for null, no need to check IsAlive to clear
         {
              CharacterTargetSelector selector = character.GetComponent<CharacterTargetSelector>();
              if (selector != null)
              {
                  selector.SetTargetable(false); // Turn off highlighting
              }
         }
     }


    // --- Action Execution ---

    private IEnumerator ProcessActionCoroutine(Character attacker, Skill skill, List<Character> targetsForExecution)
    {
        // State is already set to ActionExecution by PlayerSelectTarget or EnterEnemyTurn

        Debug.Log($"{attacker.Name} executing skill '{skill.Name}' on {targetsForExecution.Count} targets...");

        // 1. Deduct Mana Cost
        if (!attacker.UseMana(skill.ManaCost))
        {
            Debug.LogError($"{attacker.Name} couldn't use {skill.Name} during execution, not enough mana. (This should ideally be prevented by UI/AI checks).");
            // This is a fallback. If this happens, something went wrong with mana checks.
            selectedSkill = null; // Clear selected skill
            SetState(BattleState.CheckWinLoss); // Go to next phase
            yield break;
        }
        // TODO: Update Attacker's Mana UI if visible (e.g., a ManaBar next to HPBar)
         // For now, log mana change
         Debug.Log($"{attacker.Name} used {skill.ManaCost} mana. Remaining: {attacker.Mana:F0}");


        // 2. Attacker Animation
        if (attacker.characterAnimator != null) // Use the public property
        {
            Debug.Log($"{attacker.Name} is playing attack animation for skill '{skill.Name}'.");
            yield return StartCoroutine(attacker.characterAnimator.PlayAttackAnimation());
        } else {
             yield return new WaitForSeconds(0.5f); // Default delay if no animator
        }

        // Wait briefly after the attack animation starts before effects apply
        // yield return new WaitForSeconds(0.2f); // Adjust this delay as needed for animation sync

        // 3. Apply Effects to Targets
        // Filter targets again just before applying effects, in case someone died from splash damage
        List<Character> livingTargets = targetsForExecution.Where(t => t != null && t.IsAlive()).ToList();

        foreach (Character target in livingTargets)
        {
             // Calculate Damage/Healing/Effect based on skill properties
             // This logic needs expansion for different skill types (damage, heal, buff, debuff)
            float calculatedValue = 0;
             string effectType = "damage"; // Default assumption

             // Basic Damage Calculation based on StatType
             if (skill.StatType.ToLower() == "strength" || skill.StatType.ToLower() == "magic" || skill.StatType.ToLower() == "speed")
             {
                 // Determine if this target is the primary target (clicked by player / chosen first by AI)
                 // For 'single' and 'splash', the first target in targetsForExecution is the primary.
                 // For 'all', 'self', 'ally', 'all_allies', 'all_characters', there isn't really a 'primary' target in the damage calculation sense,
                 // but GetDamage uses the isPrimaryTarget flag primarily for splash damage calculation.
                 // Let's simplify: for Splash, pass `targetsForExecution[0] == target`. For others, pass `true`.
                 bool isPrimary = skill.Targets.ToLower() != "splash" || (targetsForExecution.Count > 0 && targetsForExecution[0] == target);
                 calculatedValue = skill.GetDamage(attacker, isPrimary);
                 effectType = "damage";
             }
             // TODO: Add cases for other effects, e.g.:
             // else if (skill.StatType.ToLower() == "healing")
             // {
             //     // Example healing calculation based on Magic/Strength
             //     calculatedValue = attacker.Magic * skill.DamageMultiplier; // Or Strength, or a dedicated HealingStat
             //     effectType = "healing";
             // }
             // TODO: Add logic for Buffs/Debuffs (might not use calculatedValue, but apply StatusEffects)


             // Apply the effect
             if (effectType == "damage" && calculatedValue > 0)
             {
                 // Apply damage (using Defense for reduction)
                 float finalDamage = Mathf.Max(0, calculatedValue - target.Defense); // Simple defense reduction
                 target.TakeTrueDamage(finalDamage); // Use TakeTrueDamage or TakeDamageWithDef depending on desired flow
                 Debug.Log($"{target.Name} took {finalDamage:F0} damage from {attacker.Name}'s {skill.Name}. HP: {target.CurrentHp:F0}/{target.MaxHp:F0}"); // Format HP

                 // Target Animation (Damage) - Play immediately
                 if (target.characterAnimator != null) // Use the public property
                 {
                     StartCoroutine(target.characterAnimator.PlayDamageAnimation());
                 }
                 // TODO: Add damage numbers UI feedback (Instantiate a damage number prefab)

             } else if (effectType == "healing" && calculatedValue > 0)
             {
                  target.Heal(calculatedValue);
                  Debug.Log($"{target.Name} healed {calculatedValue:F0} from {attacker.Name}'s {skill.Name}. HP: {target.CurrentHp:F0}/{target.MaxHp:F0}"); // Format HP
                 // TODO: Add healing numbers UI feedback
                 // TODO: Add healing animation
             }
             // TODO: Add logic for applying buffs, debuffs (might involve adding/removing Passive components or managing status effects)


            // Update Target Health Bar immediately after applying effect
            HealthBar targetHpBar = target.GetComponentInChildren<HealthBar>();
            if(targetHpBar != null)
            {
                 targetHpBar.SetCurrentHealth(target.CurrentHp);
            }

             // Check if target died immediately after taking damage/effect
            if (!target.IsAlive())
            {
                Debug.Log($"{target.Name} has been defeated!");
                 if (target.characterAnimator != null) // Use public property
                 {
                     target.characterAnimator.PlayDeathAnimation();
                     // Death animation might need a delay before the GameObject is removed
                     // CleanUpDeadCharacters handles the actual removal/destruction later
                 }
            }
        }

         // Ensure all damage/death animations have a moment to play if they have a delay
         // You might yield for a longer duration if death animations are long or have trailing effects
         // yield return new WaitForSeconds(Mathf.Max(0, defaultHitAnimationDuration - previousYield)); // Example: if hit anim is 0.3s and previous yield was 0.2s, wait 0.1s

        // 4. Transition to check win/loss
        selectedSkill = null; // Clear the skill after use
        finalTargets.Clear(); // Clear targets after action
        SetState(BattleState.CheckWinLoss);
    }

    // --- Reward Logic ---
     private void GrantBattleRewards()
     {
         int totalExp = 0;
         int totalGold = 0;

         // Sum up rewards from all defeated enemies
         // We need the list of enemies as they were *at the start* of battle
         // Or, sum up rewards from enemies removed by CleanUpDeadCharacters?
         // A simpler approach: have the Enemy class store its base reward,
         // and sum rewards from the *initial* wave config if needed, or just killed enemies.
         // For now, let's sum RewardExp from all Enemy objects *initially spawned* that are now !IsAlive().
         // This requires storing the initial enemy list separately or iterating the 'enemies' list *after* cleanup.
         // Let's iterate the 'enemies' list after cleanup, filtering by !IsAlive().

         // Note: CleanUpDeadCharacters removes dead characters *from the list*.
         // A better approach for rewards might be to iterate over the list of *all originally spawned* enemies,
         // which requires storing that list. Or, iterate over the GameObjects in scene tagged "Enemy" that are !IsAlive().

         // Let's revise CleanUpDeadCharacters slightly to return defeated enemies, or add a method to get them.
         // Or, iterate the turnOrder list before cleaning it? No, turnOrder also gets pruned.

         // Simple approach for now: Iterate through the *initial* enemies list saved in BattleManager
         // and find which ones are no longer alive (assuming CleanUpDeadCharacters hasn't destroyed them yet).
         // Or, perhaps the Enemy class itself gives its reward when it "dies"?

         // Let's add a way to track killed enemies and their rewards.
         // Or, even simpler, just grant a fixed reward per enemy type/count in the *defeated wave config*.
         // Let's add reward properties to EnemyWaveConfig.

         // *** Assuming EnemyWaveConfig has totalGoldReward and totalExperienceReward fields ***
         // If the battle was initiated by a wave config:
         EnemyWaveConfig defeatedWave = GameDataManager.Instance.gameData.currentBattleWave; // Get the wave that was fought
         if (defeatedWave != null)
         {
             // Grant rewards defined in the wave config
             // Need to add reward fields to EnemyWaveConfig and read them here.
             // Let's add them temporarily to the EnemyWaveConfig script now for this logic.
             // (Add public int totalGoldReward; public int totalExperienceReward; to EnemyWaveConfig.cs)

             // For now, let's just sum RewardExp from the *initial list* of enemies spawned, if they are now dead.
             // This requires storing the list of enemies *before* CleanUpDeadCharacters potentially removes them.
             // Let's modify `InitializeEnemies` to store the initial full list.

             // OR, sum RewardExp from the `enemyDataList` field in the *defeatedWave* config.
             // This is simpler as it doesn't require tracking individual instances.
             // But it means the reward isn't tied to *which* enemies you killed, just which wave you fought.
             // Let's go with summing RewardExp from the *EnemyData* in the wave config.

             if (defeatedWave.enemiesInWave != null)
             {
                  foreach (EnemyData enemyData in defeatedWave.enemiesInWave)
                  {
                      if (enemyData != null) // Ensure the reference isn't null
                      {
                          totalExp += enemyData.RewardExp;
                          // Maybe add gold to EnemyData too? Or just the wave config.
                          // Let's add gold to EnemyWaveConfig.
                      }
                  }
             }
              // Assuming EnemyWaveConfig now has a totalGoldReward field
              // totalGold = defeatedWave.totalGoldReward; // Need to add this field

             // For now, let's stick to just XP from EnemyData in the wave, and add Gold to the wave config itself.
             // Add `public int goldReward; public int experienceReward;` to EnemyWaveConfig.cs

             totalExp = defeatedWave.experienceReward; // Use the value from wave config
             totalGold = defeatedWave.goldReward; // Use the value from wave config

             Debug.Log($"Granting Rewards from wave '{defeatedWave.waveName}': {totalGold} Gold, {totalExp} XP.");

             // Apply rewards to player data
             GameDataManager.Instance.gameData.AddGold(totalGold);
             GameDataManager.Instance.gameData.AddExperience(totalExp);

             // Clear the wave config from GameData after rewarding
             GameDataManager.Instance.gameData.currentBattleWave = null;
         } else {
              Debug.LogWarning("Battle ended, but no currentBattleWave was set in GameData. No specific rewards granted.");
              // Maybe grant a small default reward?
         }

         // TODO: Display reward summary UI
     }


    // --- Helper Methods ---

    private void CleanUpDeadCharacters()
    {
         Debug.Log("Cleaning up dead characters...");
        // Find all characters (living or dead) currently managed by this BattleManager
        List<Character> allManagedCharacters = new List<Character>();
        allManagedCharacters.AddRange(players);
        allManagedCharacters.AddRange(enemies);
        allManagedCharacters = allManagedCharacters.Where(c => c != null).ToList(); // Filter out any null entries

        // Lists to remove from
        List<Player> playersToRemove = players.Where(p => p != null && !p.IsAlive()).ToList();
        List<Enemy> enemiesToRemove = enemies.Where(e => e != null && !e.IsAlive()).ToList();

        int playerRemovedCount = players.RemoveAll(p => p == null || !p.IsAlive());
        int enemyRemovedCount = enemies.RemoveAll(e => e == null || !e.IsAlive());

        if (playerRemovedCount > 0) Debug.Log($"Removed {playerRemovedCount} dead player(s) from list.");
        if (enemyRemovedCount > 0) Debug.Log($"Removed {enemyRemovedCount} dead enemy(s) from list.");


        // Remove dead characters from the turn order list
        int turnOrderRemovedCount = turnOrder.RemoveAll(c => c == null || !c.IsAlive());
        if (turnOrderRemovedCount > 0) {
             Debug.Log($"Removed {turnOrderRemovedCount} dead character(s) from turn order list.");
        }

         // Queue GameObjects of defeated characters for destruction after a delay
         // to allow death animations to finish.
         foreach (var player in playersToRemove)
         {
              Debug.Log($"Queueing destruction for defeated player: {player.Name}");
               // Disable colliders immediately to prevent interaction
              if (player.TryGetComponent<Collider>(out var col)) col.enabled = false; // For 3D
              if (player.TryGetComponent<Collider2D>(out var col2D)) col2D.enabled = false; // For 2D
              if (player.characterAnimator != null) player.characterAnimator.PlayDeathAnimation(); // Ensure death anim is playing
              Destroy(player.gameObject, player.characterAnimator != null ? player.characterAnimator.hitAnimDuration * 2 : 2f); // Destroy after death anim or 2s
         }
         foreach (var enemy in enemiesToRemove)
         {
              Debug.Log($"Queueing destruction for defeated enemy: {enemy.Name}");
               // Disable colliders immediately
              if (enemy.TryGetComponent<Collider>(out var col)) col.enabled = false; // For 3D
              if (enemy.TryGetComponent<Collider2D>(out var col2D)) col2D.enabled = false; // For 2D
              if (enemy.characterAnimator != null) enemy.characterAnimator.PlayDeathAnimation(); // Ensure death anim is playing
              Destroy(enemy.gameObject, enemy.characterAnimator != null ? enemy.characterAnimator.hitAnimDuration * 2 : 2f); // Destroy after death anim or 2s
         }
    }


    // --- Initialization ---

    private void InitializePlayers()
    {
        Debug.Log("Initializing Players...");
        // Destroy existing player objects before creating new ones (Important if reusing scene)
        foreach (var player in players) { if (player != null && player.gameObject != null) Destroy(player.gameObject); }
        players.Clear();

        // Get player data from the persistent GameDataManager
         List<PlayerData> playerDataSources = GameDataManager.Instance.gameData.playerDataList;

         if (playerDataSources == null || playerDataSources.Count == 0)
         {
             Debug.LogError("No player data found in GameData! Cannot initialize players.");
             return; // Exit if no players
         }

        Vector3 spawnPosition = playerStartPosition != null ? playerStartPosition.position : new Vector3(-4f, 0f, 0f);
        // Calculate initial Y offset to center the group
        float totalHeight = (playerDataSources.Count - 1) * spacingBetweenPlayers;
        spawnPosition.y -= totalHeight / 2f;

        for(int i = 0; i < playerDataSources.Count; i++)
        {
            var playerData = playerDataSources[i];
            if (playerData != null)
            {
                GameObject playerObject = Instantiate(playerPrefab, spawnPosition, Quaternion.identity);
                Player player = playerObject.GetComponent<Player>();
                if (player != null)
                {
                    playerData.ApplyToPlayer(player); // Apply data from persistent PlayerData
                    players.Add(player);
                    playerObject.name = $"Player_{player.Name}_{i}"; // Use Player.Name now that it's set

                    // Add CharacterHoverHandler and CharacterTargetSelector if not present
                    if (playerObject.GetComponent<CharacterHoverHandler>() == null) playerObject.AddComponent<CharacterHoverHandler>();
                    if (playerObject.GetComponent<CharacterTargetSelector>() == null) playerObject.AddComponent<CharacterTargetSelector>();
                    // Add TargetIndicator if not present
                    if (playerObject.GetComponent<TargetIndicator>() == null) playerObject.AddComponent<TargetIndicator>();


                    // Setup Health Bar
                    GameObject hpBarObject = Instantiate(HpBarPrefab, playerObject.transform);
                    HealthBar hpBar = hpBarObject.GetComponent<HealthBar>();
                    if (hpBar != null)
                    {
                        player.SetHpBar(hpBar); // Link the HP bar to the character
                        hpBar.transform.localPosition = new Vector3(0, 1.5f, 0); // Adjust position relative to player
                         // Player HP bars face forward, no rotation needed here
                    } else {
                         Debug.LogError("HP Bar Prefab missing HealthBar component!");
                    }

                }
                else Debug.LogError("Player Prefab missing Player component!");

                spawnPosition.y += spacingBetweenPlayers; // Move up for the next player
            } else {
                 Debug.LogWarning($"PlayerData at index {i} from GameData is null. Skipping player initialization.");
            }
        }
         Debug.Log($"Initialized {players.Count} players.");
         // Initialize BattleUIManager with the first player if available
         if (players.Count > 0 && battleUIManager != null)
         {
             battleUIManager.InitializePlayerUI(players[0]); // Initialize with the first player for now
             battleUIManager.HidePlayerUI(); // Keep hidden until PlayerTurn state
         } else if (battleUIManager != null) {
             battleUIManager.HidePlayerUI(); // Hide UI if no players
             Debug.LogWarning("No players initialized, BattleUIManager UI will be hidden.");
         } else {
             Debug.LogWarning("No players initialized or BattleUIManager missing.");
         }
    }

    // This method now acts as the "Enemy Factory" for a battle wave
    private void InitializeEnemies()
    {
        Debug.Log("Initializing Enemies from wave config...");
         // Destroy existing enemy objects before creating new ones (Important if reusing scene)
        foreach (var enemy in enemies) { if (enemy != null && enemy.gameObject != null) Destroy(enemy.gameObject); }
        enemies.Clear();

        // Get the wave configuration from the persistent GameDataManager
         EnemyWaveConfig waveConfig = GameDataManager.Instance.gameData.currentBattleWave;

         if (waveConfig == null || waveConfig.enemiesInWave == null || waveConfig.enemiesInWave.Count == 0)
         {
             Debug.LogError("No enemy wave config found in GameData, or the wave is empty! Cannot initialize enemies.");
              // Optionally spawn a default enemy or go to a loss state
              // For now, we'll rely on the CheckWinLossConditions to detect 0 enemies.
             return; // Exit if no wave data
         }

        Vector3 spawnPosition = enemyStartPosition != null ? enemyStartPosition.position : new Vector3(4f, 0f, 0f);
         // Calculate initial Y offset to center the group based on the number of enemies in the wave
        float totalHeight = (waveConfig.enemiesInWave.Count - 1) * spacingBetweenEnemies;
        spawnPosition.y -= totalHeight / 2f;

        for (int i = 0; i < waveConfig.enemiesInWave.Count; i++)
        {
            // Get the EnemyData from the wave config
            EnemyData enemyDataSource = waveConfig.enemiesInWave[i];
            if (enemyDataSource != null)
            {
                 // Instantiate with a rotation to face the players (assuming players are on the left)
                GameObject enemyObject = Instantiate(enemyPrefab, spawnPosition, Quaternion.Euler(0, 180, 0));
                Enemy enemy = enemyObject.GetComponent<Enemy>();
                 if(enemy != null) {
                    // Create a *copy* of the EnemyData ScriptableObject to avoid modifying the asset directly during battle
                    // This ensures stat changes (like damage taken) in battle don't persist to the SO asset
                    EnemyData runtimeEnemyData = ScriptableObject.Instantiate(enemyDataSource);

                    // Apply data from the runtime copy
                    runtimeEnemyData.ApplyToPlayer(enemy); // Apply data to the Enemy instance

                    enemies.Add(enemy); // Add the Enemy instance to the battle list
                    enemyObject.name = $"Enemy_{enemy.Name}_{i}"; // Use Enemy.Name now that it's set

                     // Add CharacterHoverHandler and CharacterTargetSelector if not present
                    if (enemyObject.GetComponent<CharacterHoverHandler>() == null) enemyObject.AddComponent<CharacterHoverHandler>();
                    if (enemyObject.GetComponent<CharacterTargetSelector>() == null) enemyObject.AddComponent<CharacterTargetSelector>();
                     // Add TargetIndicator if not present
                    if (enemyObject.GetComponent<TargetIndicator>() == null) enemyObject.AddComponent<TargetIndicator>();


                    // Setup Health Bar
                    GameObject hpBarObject = Instantiate(HpBarPrefab, enemyObject.transform);
                    HealthBar hpBar = hpBarObject.GetComponent<HealthBar>();
                     if (hpBar != null)
                    {
                        enemy.SetHpBar(hpBar); // Link the HP bar to the character
                        hpBar.transform.localPosition = new Vector3(0, 1.5f, 0); // Adjust position relative to enemy
                        // Counter-rotate the HP bar so its text and fill are correctly oriented
                        hpBarObject.transform.localRotation = Quaternion.Euler(0, 180, 0);

                    } else {
                         Debug.LogError("HP Bar Prefab missing HealthBar component!");
                    }
                    // Clean up the runtime EnemyData SO when the enemy GameObject is destroyed
                    Destroy(runtimeEnemyData); // Mark the SO instance for destruction with the GameObject

                }
                 else Debug.LogError("Enemy Prefab missing Enemy component!");

                spawnPosition.y += spacingBetweenEnemies; // Move up for the next enemy
            } else {
                 Debug.LogWarning($"EnemyData reference at index {i} in wave '{waveConfig.waveName}' is null. Skipping spawning this enemy.");
            }
        }
         Debug.Log($"Initialized {enemies.Count} enemies from wave '{waveConfig.waveName}'.");

         // The wave config is used, clear it from GameData so it's not reused
         // GameDataManager.Instance.gameData.currentBattleWave = null; // Moved this to GrantBattleRewards
    }


    // --- Button Listeners ---

    private void OnReturnButtonClicked() // This button is used to return to GameScene
    {
        Debug.Log("Return button clicked! Saving data and loading game scene...");
        SaveAllPlayerData(); // Save current state before leaving battle
        // Ensure BattleUIManager is hidden before leaving
        if (battleUIManager != null) battleUIManager.HidePlayerUI();
        ClearTargetIndicators(); // Ensure indicators are off
        SceneManager.LoadScene("GameScene");
    }

    private void SaveAllPlayerData()
    {
         Debug.Log("Saving all player data from battle instances back to GameData ScriptableObjects...");
        // Save the current state (HP, Mana, XP, etc.) of the active player instances back to their ScriptableObject data
        // Iterate through the players list managed by BattleManager
        foreach (var player in players.Where(p => p != null))
        {
             // Find the corresponding PlayerData ScriptableObject in GameDataManager
             PlayerData playerData = GameDataManager.Instance.gameData.playerDataList.Find(pd => pd != null && pd.playerName == player.PlayerName);

             if (playerData != null)
            {
                playerData.SaveFromPlayer(player); // Save player instance state to PlayerData SO
                 Debug.Log($"Saved battle state for player: {player.PlayerName}");
            } else {
                Debug.LogWarning($"Could not find matching PlayerData SO for player instance: {player.PlayerName}. Skipping save.");
            }
        }
        // Save the entire GameData (which now contains updated PlayerData) to PlayerPrefs
        GameDataManager.Instance.gameData.SaveToPlayerPrefs();
        Debug.Log("Game data (including player stats) saved to PlayerPrefs.");
    }

    private void OnDebugButtonClicked()
    {
        Debug.Log("--- DEBUG BUTTON ---");
        Debug.Log($"Current State: {currentState}");
        Debug.Log($"Active Character: {activeCharacter?.Name ?? "None"} (Type: {activeCharacter?.GetType().Name ?? "None"})");
        Debug.Log($"Turn Index: {turnIndex} / {turnOrder.Count} (Current Turn Order Size)");
        Debug.Log("Players (managed by BattleManager):");
        if (players.Count > 0) foreach (var p in players.Where(p => p != null)) p.logStat(); else Debug.Log("- None");
        Debug.Log("Enemies (managed by BattleManager):");
        if (enemies.Count > 0) foreach (var e in enemies.Where(e => e != null)) e.logStat(); else Debug.Log("- None");
        Debug.Log("Turn Order (active combatants):");
         if (turnOrder.Count > 0) foreach (var c in turnOrder.Where(c => c != null)) Debug.Log($"- {c.Name} (HP: {c.CurrentHp:F0}, SPD: {c.Speed:F0})"); else Debug.Log("- Empty");

        // Log GameData state via the singleton
        if (GameDataManager.Instance != null && GameDataManager.Instance.gameData != null)
        {
             GameDataManager.Instance.gameData.LogGameState();
        } else {
             Debug.LogWarning("GameDataManager.Instance or GameData is null. Cannot log GameData state.");
        }

        Debug.Log("--- END DEBUG ---");
    }

     private void OnUtilityButtonClicked() // Example: Damage players
    {
        Debug.Log("Utility button clicked! Damaging players by 10.");
         // Damage living players
        foreach (var player in players.Where(p => p != null && p.IsAlive()).ToList()) // Use ToList() to avoid modifying during iteration
        {
             int damageAmount = 10; // Example damage
             player.TakeTrueDamage(damageAmount); // Use true damage for debug
              Debug.Log($"{player.PlayerName} took {damageAmount} damage. HP: {player.CurrentHp:F0}/{player.MaxHp:F0}");

              // Update UI
              HealthBar hpBar = player.GetComponentInChildren<HealthBar>();
              if(hpBar) hpBar.SetCurrentHealth(player.CurrentHp);

             // Play damage animation (as coroutine if CharacterAnimator exists)
             if (player.characterAnimator != null)
             {
                 StartCoroutine(player.characterAnimator.PlayDamageAnimation());
             }

              // Check if player died
              if(!player.IsAlive()) {
                   Debug.Log($"{player.PlayerName} has been defeated!");
                   // Death animation will be played by CleanUpDeadCharacters or in the damage application if that was the killing blow.
              }
        }
         // After applying effects, clean up dead and check win/loss
         CleanUpDeadCharacters();
         CheckWinLossConditions(true); // Check condition and potentially change state
    }

    private void OnUtilityButton2Clicked() // Example: Damage enemies
    {
        Debug.Log("Utility button 2 clicked! Damaging enemies by 30.");
        // Damage living enemies
        foreach (var enemy in enemies.Where(e => e != null && e.IsAlive()).ToList()) // Use ToList() to avoid modifying during iteration
        {
             int damageAmount = 30; // Example damage
             enemy.TakeTrueDamage(damageAmount); // Use true damage for debug
             Debug.Log($"{enemy.Name} took {damageAmount} damage. HP: {enemy.CurrentHp:F0}/{enemy.MaxHp:F0}"); // Format HP

             // Update UI
              HealthBar hpBar = enemy.GetComponentInChildren<HealthBar>();
              if(hpBar) hpBar.SetCurrentHealth(enemy.CurrentHp);

             // Play damage animation
             if (enemy.characterAnimator != null)
             {
                 StartCoroutine(enemy.characterAnimator.PlayDamageAnimation());
             }

              // Check if enemy died
             if(!enemy.IsAlive()) {
                 Debug.Log($"{enemy.Name} has been defeated!");
                 // Death animation will be played by CleanUpDeadCharacters or in the damage application if that was the killing blow.
             }
        }
         // After applying effects, clean up dead and check win/loss
         CleanUpDeadCharacters();
         CheckWinLossConditions(true); // Check condition and potentially change state
    }
}