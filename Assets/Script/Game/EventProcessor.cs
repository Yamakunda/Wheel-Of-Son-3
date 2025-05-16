using UnityEngine;
using UnityEngine.SceneManagement; // Required for SceneManager
using System.Collections; // Required for IEnumerator

public class EventProcessor : MonoBehaviour
{
    // [Header("Components")] // gameDataManager is now a singleton, no need for direct ref
    // [SerializeField] private GameDataManager gameDataManager; // REMOVED
    [Header("Default Events")]
    [SerializeField] public EnemyWaveConfig defaultSmallWave; // Now references the SO
    [SerializeField] public EnemyWaveConfig defaultLargeWave; // Now references the SO
    [SerializeField] public RewardData defaultReward;
    [Header("UI Elements")]
    [SerializeField] private WheelUIManager wheelUIManager;
    [Header("Scene Management")]
    [SerializeField] private string battleSceneName = "BattleScene";

     // Reference to GameData is obtained via the singleton GameDataManager.Instance.gameData


    public void ProcessEvent(GameEventSegment currentEvent, System.Action onProcessComplete)
    {
        if (currentEvent == null) return;

        Debug.Log($"Processing event: {currentEvent.eventName} ({currentEvent.eventType})");

         // Ensure GameData is accessible
        if (GameDataManager.Instance == null || GameDataManager.Instance.gameData == null)
        {
            Debug.LogError("GameDataManager or GameData is null! Cannot process event.");
            if (onProcessComplete != null) onProcessComplete(); // Complete immediately on error
            return;
        }

        // Process based on event type
        switch (currentEvent.eventType)
        {
            case GameEventType.SmallEnemyWave:
                // onProcessComplete cannot be called *after* scene load from here
                ProcessSmallWaveEvent(currentEvent); // Removed onComplete parameter
                break;

            case GameEventType.LargeEnemyWave:
                // onProcessComplete cannot be called *after* scene load from here
                ProcessLargeWaveEvent(currentEvent); // Removed onComplete parameter
                break;

            case GameEventType.Reward:
                ProcessRewardEvent(currentEvent, onProcessComplete);
                break;

            case GameEventType.Mystery:
                ProcessMysteryEvent(onProcessComplete);
                break;
        }
    }

    // Removed onComplete parameter from battle events
    private void ProcessSmallWaveEvent(GameEventSegment currentEvent)
    {
        Debug.Log("Processing small enemy wave event");

        // Use the wave config from the event, or default if not specified
        EnemyWaveConfig waveToUse = currentEvent.waveConfig ?? defaultSmallWave;

        if (waveToUse == null)
        {
             Debug.LogError($"No wave config specified for SmallEnemyWave event '{currentEvent.eventName}' and defaultSmallWave is null!");
             // Call OnEventProcessingComplete (via GameManager) immediately if no battle occurs
             // This requires changing GameManager's ProcessCurrentEvent logic slightly or having EventProcessor call it back.
             // For now, let's assume a null wave is an error we log and skip. GameManager handles next event logic.
             GameManager gameManager = FindFirstObjectByType<GameManager>(); // Find GameManager to notify
             if (gameManager != null) gameManager.OnEventProcessingComplete();
             return;
        }

        // Start battle in battle scene
         StartCoroutine(TransitionToBattleScene(waveToUse)); // Removed onComplete parameter
    }

     // Removed onComplete parameter from battle events
    private void ProcessLargeWaveEvent(GameEventSegment currentEvent)
    {
        Debug.Log("Processing large enemy wave event");

        // Use the wave config from the event, or default if not specified
        EnemyWaveConfig waveToUse = currentEvent.waveConfig ?? defaultLargeWave;

         if (waveToUse == null)
        {
             Debug.LogError($"No wave config specified for LargeEnemyWave event '{currentEvent.eventName}' and defaultLargeWave is null!");
             // Call OnEventProcessingComplete (via GameManager) immediately if no battle occurs
             GameManager gameManager = FindFirstObjectByType<GameManager>(); // Find GameManager to notify
             if (gameManager != null) gameManager.OnEventProcessingComplete();
             return;
        }

        // Start battle in battle scene
        StartCoroutine(TransitionToBattleScene(waveToUse)); // Removed onComplete parameter
    }

     // Removed onComplete parameter from coroutine signature
    private IEnumerator TransitionToBattleScene(EnemyWaveConfig waveConfig)
    {
        Debug.Log($"Transitioning to battle scene with wave: {waveConfig.waveName}");
        // Store the current wave config in GameDataManager's GameData
        GameDataManager.Instance.gameData.currentBattleWave = waveConfig;

        // Wait a moment for visual clarity or transition effects
        yield return new WaitForSeconds(1f);

        // Load battle scene asynchronously
        AsyncOperation asyncLoad = SceneManager.LoadSceneAsync(battleSceneName);

        // Wait until the asynchronous scene fully loads
        while (!asyncLoad.isDone)
        {
            yield return null;
        }

        // Scene is loaded. The BattleManager in the new scene will pick up the wave config.
        Debug.Log("Battle scene loaded.");

        // The onComplete action for the event processing should happen AFTER the battle,
        // when the BattleManager loads *back* to the GameScene and notifies the GameManager.
        // So, we don't call onComplete here.
    }

    private void ProcessRewardEvent(GameEventSegment currentEvent, System.Action onComplete)
    {
        Debug.Log("Processing reward event");

        // Use the reward data from the event, or default if not specified
        RewardData rewardToGive = currentEvent.reward ?? defaultReward;

        if (rewardToGive != null)
        {
            // Apply gold and experience to player data via the singleton
            GameDataManager.Instance.gameData.AddGold(rewardToGive.gold);
            GameDataManager.Instance.gameData.AddExperience(rewardToGive.experience);
            Debug.Log($"Added {rewardToGive.gold} gold and {rewardToGive.experience} XP to player data");
        } else {
             Debug.LogWarning($"No reward data specified for Reward event '{currentEvent.eventName}' and defaultReward is null!");
        }


        // Complete processing for non-battle events
        if (onComplete != null)
            onComplete();
    }

    private void ProcessMysteryEvent(System.Action onComplete)
    {
        Debug.Log("Processing mystery event");

         // Ensure GameData is accessible
        if (GameDataManager.Instance == null || GameDataManager.Instance.gameData == null)
        {
            Debug.LogError("GameDataManager or GameData is null! Cannot process mystery event.");
            if (onComplete != null) onComplete(); // Complete immediately on error
            return;
        }


        // Generate a random mystery event
        int randomEvent = Random.Range(0, 5);
        string eventDescription = "A mysterious event occurs...";

        switch (randomEvent)
        {
            case 0:
                eventDescription = "A traveler gives you a healing potion!";
                // TODO: Add healing potion to inventory (Need inventory system)
                break;

            case 1:
                eventDescription = "You find a shortcut through the dungeon!";
                // Skip the next event by increasing progress by 1 extra
                GameDataManager.Instance.gameData.IncreaseEventProgress(1); // ProcessEvent already increases by 1
                break;

            case 2:
                eventDescription = "You stumble into a trap! Everyone takes 10 damage.";
                // Deal damage to all player characters via the singleton
                GameDataManager.Instance.gameData.ApplyDamageToAllPlayers(10);
                Debug.Log("Applied trap damage to all players");
                break;

            case 3:
                eventDescription = "You discover an ancient shrine. All characters gain +5 strength!";
                // Buff all player characters via the singleton
                GameDataManager.Instance.gameData.ApplyStatBoostToAllPlayers("Strength", 5);
                Debug.Log("Applied strength boost to all players");
                break;

            case 4:
                eventDescription = "You encounter a friendly merchant. He offers you goods at a discount.";
                // TODO: Open shop UI with discounted items (Need shop UI)
                break;
        }
        // Display description on the wheel UI
        if(wheelUIManager != null)
        {
             wheelUIManager.SetDescriptionText(eventDescription);
        }


        // Complete processing for non-battle events
        if (onComplete != null)
            onComplete();
    }
}