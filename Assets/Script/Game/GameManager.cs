using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Collections.Generic;
using System.Collections; // Required for IEnumerator
using TMPro; // Required for TextMeshProUGUI
public class GameManager : MonoBehaviour
{
    // GameDataManager is now a singleton, access via Instance
    // [Header("Components")]
    // [SerializeField] private GameDataManager gameDataManager; // REMOVED

    [Header("UI Elements")]
    [SerializeField] private WheelUIManager wheelUIManager;
    [Header("Event Processor")]
    [SerializeField] private EventProcessor eventProcessor;
    [Header("Event Configuration")]
    [SerializeField] private List<GameEventSegment> gameEvents = new List<GameEventSegment>();
    [SerializeField] private GameEventSegment currentEvent; // The event selected by the wheel, awaiting processing

    // *** NEW: Enemy Wave Configs to be assigned in Inspector ***
    [Header("Battle Wave Configurations")]
    [SerializeField] private EnemyWaveConfig smallGoblinWave;
    [SerializeField] private EnemyWaveConfig smallSkeletonWave;
    [SerializeField] private EnemyWaveConfig largeGoblinWave; // Or any other large wave
    [SerializeField] private EnemyWaveConfig mixedEnemyWave;
    [SerializeField] private EnemyWaveConfig bossWave;
    [SerializeField] private TMP_FontAsset pixelFont;
    private void Awake()
    {
        Debug.Log("GameManager Awake called.");
        // Access GameData via the singleton GameDataManager instance
        if (GameDataManager.Instance != null && GameDataManager.Instance.gameData != null)
        {
             // GameData should already be loaded by GameDataManager.Awake()
             Debug.Log("GameData accessed via singleton.");
        } else {
            Debug.LogError("GameDataManager.Instance or GameData is null!");
            // Handle case where GameDataManager is not found or GameData asset is missing
             // Consider disabling game functionality or loading a critical error scene
             if (wheelUIManager != null) wheelUIManager.EnableSpinButton(false);
             // ... handle other dependencies ...
        }

         // Ensure AudioManager is accessible
         if (AudioManager.Instance != null)
         {
             Debug.Log("AudioManager accessed via singleton.");
         } else {
             Debug.LogError("AudioManager.Instance is null! Cannot control music.");
         }
    }
    private void Start()
    {
        Debug.Log("GameManager Start called.");

        // Play game music when the scene starts
        if (AudioManager.Instance != null)
        {
             AudioManager.Instance.PlayGameMusic(); // Use the dedicated method
        } else {
            Debug.LogError("AudioManager.Instance is null! Cannot play game music.");
        }


        // If we just returned from a battle, the BattleManager should have handled the return logic.
        // The GameDataManager.Instance.gameData should reflect the saved state.
        // The eventProgress is already updated in OnEventSelected when the wheel stops.
        // If the state was BattleWon/BattleLost, BattleManager loads GameScene.
        // The GameManager in GameScene will just start up normally here.
        // The next event should be processed based on the saved eventProgress.

        // Initialize events and UI if starting fresh or returning to the main loop
        InitializeDefaultEvents(); // Define available events
        InitializeWheelUI();       // Setup the wheel and UI

        // Check if event progress is complete upon returning (or starting new game/load)
        if (GameDataManager.Instance != null && GameDataManager.Instance.gameData != null)
        {
             if (GameDataManager.Instance.gameData.eventProgress >= GameDataManager.Instance.gameData.eventProgressMax)
             {
                 Debug.Log("Game progression complete!");
                 // Handle end game state
                 if (wheelUIManager != null)
                 {
                     wheelUIManager.EnableSpinButton(false);
                     wheelUIManager.SetDescriptionText("All events completed! Game finished.");
                     wheelUIManager.EnableNextButton(false); // No more events to process
                 }
                // TODO: Add logic to transition to an end-game scene after a delay
                StartCoroutine(TransitionToEndGameSceneAfterDelay(1f));
             } else {
                 // If game is not complete, make the wheel ready for the next spin
                 if (wheelUIManager != null)
                 {
                      wheelUIManager.EnableSpinButton(true); // Make sure spin button is enabled
                      wheelUIManager.EnableNextButton(false); // Hide next button until spin
                      // Check if the description text was perhaps set by a Mystery event callback
                      // If so, leave it. Otherwise, reset it.
                      // A more robust way would be to pass the description text from the event processor
                      // explicitly to the UI after processing is complete for non-battle events.
                      // For now, let's assume if we just returned from battle, description should reset.
                      // If wheelUIManager.SetDescriptionText is called by EventProcessor.ProcessMysteryEvent, it will override this later.
                      wheelUIManager.SetDescriptionText("Spin the wheel to determine the next encounter!");
                 }
             }
        } else {
             Debug.LogError("GameDataManager.Instance or GameData is null in GameManager.Start! Cannot check game progress.");
             if (wheelUIManager != null) wheelUIManager.EnableSpinButton(false); // Disable interaction on critical error
        }
    }
    private void InitializeWheelUI()
    {
        // Extract wheel UI initialization from Start() to this method
        if (wheelUIManager != null)
        {
            wheelUIManager.Initialize(gameEvents);
            // Initial description is set in Start after checking game complete state

            // Access GameData via singleton
            if (GameDataManager.Instance != null && GameDataManager.Instance.gameData != null)
            {
                 wheelUIManager.UpdateEventCounter(GameDataManager.Instance.gameData.eventProgress, GameDataManager.Instance.gameData.eventProgressMax);
            } else {
                 Debug.LogError("GameDataManager.Instance or GameData is null in InitializeWheelUI!");
            }


            // Set up event handlers
            wheelUIManager.onEventSelected.RemoveAllListeners(); // Prevent duplicates
            wheelUIManager.onEventSelected.AddListener(OnEventSelected);

            // Configure next button to process the selected event
            if (wheelUIManager.nextButton != null)
            {
                wheelUIManager.nextButton.onClick.RemoveAllListeners();
                wheelUIManager.nextButton.onClick.AddListener(ProcessCurrentEvent);
                wheelUIManager.nextButton.gameObject.SetActive(false); // Ensure hidden initially
            }
             // Ensure spin button is active initially based on game progress
             // This logic is now handled in Start()
        } else {
            Debug.LogError("WheelUIManager is not assigned!");
        }

    }

    private void OnEventSelected(GameEventSegment eventSegment)
    {
        currentEvent = eventSegment;
        // Increase event progress when the wheel *selects* the event
        if (GameDataManager.Instance != null && GameDataManager.Instance.gameData != null)
        {
            GameDataManager.Instance.gameData.IncreaseEventProgress(1); // This also saves data
            wheelUIManager.UpdateEventCounter(GameDataManager.Instance.gameData.eventProgress, GameDataManager.Instance.gameData.eventProgressMax);
            Debug.Log($"Event selected: {eventSegment.eventName}. Game Progress: {GameDataManager.Instance.gameData.eventProgress}/{GameDataManager.Instance.gameData.eventProgressMax}");
        } else {
             Debug.LogError("GameDataManager.Instance or GameData is null in OnEventSelected!");
        }

        // The WheelUIManager's SpinCoroutine enables the next button automatically
        // We just need to make sure ProcessCurrentEvent is correctly linked to it.
        // This was done in InitializeWheelUI.
    }

    public void ProcessCurrentEvent()
    {
        if (currentEvent == null)
        {
             Debug.LogWarning("ProcessCurrentEvent called but no event is selected.");
             // Maybe re-enable spin button and clear UI state?
             if(wheelUIManager != null)
             {
                 wheelUIManager.SetDescriptionText("An error occurred. Spin again.");
                 wheelUIManager.EnableNextButton(false);
                 wheelUIManager.EnableSpinButton(true);
             }
            return;
        }
        if (eventProcessor == null)
        {
             Debug.LogError("EventProcessor is not assigned! Cannot process event.");
             if(wheelUIManager != null)
             {
                 wheelUIManager.SetDescriptionText("System error. Cannot process event.");
                 wheelUIManager.EnableNextButton(false);
                 wheelUIManager.EnableSpinButton(false); // Disable interaction
             }
            return;
        }


        // Hide the next button during processing
        if (wheelUIManager != null) wheelUIManager.EnableNextButton(false);

        // Process the event through our specialized processor
        // Pass a callback action that will be called when the event processing is *finished*
        // (This callback is important for non-battle events like Reward or Mystery)
        eventProcessor.ProcessEvent(currentEvent, OnEventProcessingComplete);

        // If the event was a battle, ProcessEvent will load the BattleScene.
        // This method will stop executing here because of the scene load.
        // OnEventProcessingComplete will *not* be called immediately in that case.
        // When returning from battle, the GameManager.Start() method will run again
        // in the GameScene and should pick up where the game left off based on saved GameData.

        // Optionally, stop game music if transitioning to a scene with different music (like BattleScene)
        // If BattleScene has its own music, uncomment the following:
        // if (currentEvent.eventType == GameEventType.SmallEnemyWave || currentEvent.eventType == GameEventType.LargeEnemyWave)
        // {
        //     if (AudioManager.Instance != null) AudioManager.Instance.StopMusic(); // Or PlayBattleMusic() from AudioManager
        // }
    }

    private void InitializeDefaultEvents()
    {
        gameEvents.Clear(); // Clear the list before adding defaults

         // Ensure default wave configs are assigned in the inspector
         if (smallGoblinWave == null || smallSkeletonWave == null || largeGoblinWave == null || mixedEnemyWave == null || bossWave == null)
         {
             Debug.LogError("Some default EnemyWaveConfig assets are not assigned in the GameManager inspector!");
             // Continue, but battle events might use null configs if not explicitly set per event
         }
         if (eventProcessor == null)
         {
              Debug.LogError("EventProcessor is not assigned! Cannot initialize default events with rewards/mystery defaults.");
              // Cannot proceed with creating events that rely on EventProcessor defaults
              return;
         }
         // Ensure EventProcessor's defaults are assigned
          if (eventProcessor.defaultReward == null)
         {
              Debug.LogWarning("EventProcessor's defaultReward is not assigned. Reward events might fail or use hardcoded values.");
         }


        // Small enemy waves
        GameEventSegment goblinWave = new GameEventSegment
        {
            eventName = "Goblins",
            description = "A pack of goblins ambushes you!",
            weight = 2.5f,
            segmentColor = new Color(0.3f, 0.7f, 0.3f, 0.8f),
            eventType = GameEventType.SmallEnemyWave,
            waveConfig = smallGoblinWave // *** LINK THE WAVE CONFIG ***
        };

        GameEventSegment skeletonWave = new GameEventSegment
        {
            eventName = "Skeletons",
            description = "Skeletons rise from the ground to attack!",
            weight = 2f,
            segmentColor = new Color(0.5f, 0.7f, 0.3f, 0.8f),
            eventType = GameEventType.SmallEnemyWave,
            waveConfig = smallSkeletonWave // Corrected: should reference smallSkeletonWave
        };
         goblinWave.waveConfig = smallGoblinWave; // Ensure these links are correctly made

        // Large enemy wave
        GameEventSegment largeWave = new GameEventSegment
        {
            eventName = "Horde",
            description = "A large horde of enemies surrounds you!",
            weight = 1.5f,
            segmentColor = new Color(0.8f, 0.3f, 0.3f, 0.8f),
            eventType = GameEventType.LargeEnemyWave,
            waveConfig = largeGoblinWave // *** LINK THE WAVE CONFIG ***
        };

        // Mixed enemy wave
        GameEventSegment mixedWave = new GameEventSegment
        {
            eventName = "Mixed",
            description = "Different enemy types attack together!",
            weight = 1.2f,
            segmentColor = new Color(0.6f, 0.4f, 0.3f, 0.8f),
            eventType = GameEventType.LargeEnemyWave,
            waveConfig = mixedEnemyWave // *** LINK THE WAVE CONFIG ***
        };

        // Boss encounter
        GameEventSegment bossEvent = new GameEventSegment
        {
            eventName = "Boss",
            description = "A powerful foe blocks your path!",
            weight = 0.8f,
            segmentColor = new Color(0.8f, 0.1f, 0.1f, 0.9f),
            eventType = GameEventType.LargeEnemyWave,
            waveConfig = bossWave // *** LINK THE WAVE CONFIG ***
        };

        // Reward (common)
        GameEventSegment reward = new GameEventSegment
        {
            eventName = "Treasure",
            description = "You found a treasure chest!",
            weight = 2f,
            segmentColor = new Color(0.9f, 0.8f, 0.2f, 0.8f),
            eventType = GameEventType.Reward,
            // Use default from EventProcessor if available, otherwise use hardcoded fallback
            reward = eventProcessor.defaultReward != null ? eventProcessor.defaultReward : new RewardData { gold = 50, experience = 100 }
        };

        // Mystery event (uncommon)
        GameEventSegment mystery = new GameEventSegment
        {
            eventName = "Mystery",
            description = "Something unusual happens...",
            weight = 1.5f,
            segmentColor = new Color(0.4f, 0.4f, 0.9f, 0.8f),
            eventType = GameEventType.Mystery
            // Mystery event doesn't have waveConfig or direct reward data in the segment itself;
            // its effects are handled within EventProcessor.ProcessMysteryEvent
        };

        // Add all events to the list
        gameEvents.Add(goblinWave);
        gameEvents.Add(skeletonWave); // Add the corrected skeleton wave
        gameEvents.Add(largeWave);
        gameEvents.Add(mixedWave);
        gameEvents.Add(bossEvent);
        gameEvents.Add(reward);
        gameEvents.Add(mystery);

         Debug.Log($"Initialized {gameEvents.Count} default game events.");
    }

    /// <summary>
    /// Callback function called by EventProcessor after a non-battle event finishes.
    /// </summary>
    public void OnEventProcessingComplete()
    {
        Debug.Log("GameManager: Event processing completed.");

        // Check if this was the final event
        if (GameDataManager.Instance != null && GameDataManager.Instance.gameData != null)
        {
            if (GameDataManager.Instance.gameData.eventProgress >= GameDataManager.Instance.gameData.eventProgressMax)
            {
                 Debug.Log("Game progression complete!");
                // Disable the spin button and show final message
                if (wheelUIManager != null)
                {
                    wheelUIManager.EnableSpinButton(false);
                    wheelUIManager.SetDescriptionText("All events completed! Game finished.");
                    wheelUIManager.EnableNextButton(false);
                }
                // TODO: Add logic to transition to an End Game scene after a delay
                StartCoroutine(TransitionToEndGameSceneAfterDelay(1f));
            }
            else
            {
                Debug.Log("Event processing complete. Preparing for next event...");
                // Re-enable spin button for the next event after a delay
                if (wheelUIManager != null)
                {
                     StartCoroutine(EnableSpinAfterDelay(1f)); // Wait 1 second
                } else {
                     Debug.LogWarning("WheelUIManager is null, cannot re-enable spin button.");
                }
            }
             // Clear the current event reference after processing
             currentEvent = null;
        } else {
             Debug.LogError("GameDataManager.Instance or GameData is null in OnEventProcessingComplete!");
             // Unable to check game progress, leave spin button disabled?
             if (wheelUIManager != null) wheelUIManager.EnableSpinButton(false);
        }
    }

     // Helper coroutine to wait before enabling the spin button
     private IEnumerator EnableSpinAfterDelay(float delay)
     {
         yield return new WaitForSeconds(delay);
         if (wheelUIManager != null)
         {
             wheelUIManager.EnableSpinButton(true);
             // Note: If a Mystery event just finished, its description might still be showing.
             // Consider adding a parameter to OnEventProcessingComplete to get the final description.
             // For now, we'll just reset the text.
             wheelUIManager.SetDescriptionText("Spin the wheel to determine the next encounter!"); // Reset description
         }
     }

    // Placeholder for eventual end game scene transition
    private IEnumerator TransitionToEndGameSceneAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        Debug.Log("Transitioning to end game scene...");
         // Optionally stop current music
         if (AudioManager.Instance != null) AudioManager.Instance.StopMusic();
        SceneManager.LoadScene("LobbyScene"); // Replace with your end game scene name
    }
}

[System.Serializable] // <-- Here is the definition of RewardData
public class RewardData
{
    public int gold;
    public int experience;
}

public enum GameEventType
{
    SmallEnemyWave,
    LargeEnemyWave,
    Reward,
    Mystery
}
