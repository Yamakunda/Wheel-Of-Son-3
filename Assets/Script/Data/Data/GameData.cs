using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "GameData", menuName = "Game/GameData")]
public class GameData : ScriptableObject
{
    public int coinCount = 0;
    public int eventProgress = 0;
    public int eventProgressMax = 15;
    public List<PlayerData> playerDataList = new List<PlayerData>();
    // Add enemy data list (loaded from Resources)
    public List<EnemyData> enemyDataList = new List<EnemyData>();

    // *** NEW: Field to hold the wave config for the next battle ***
    [System.NonSerialized] // Don't serialize this into PlayerPrefs JSON
    public EnemyWaveConfig currentBattleWave = null; // Set by EventProcessor, read by BattleManager

    // Save game data to PlayerPrefs
    public void SaveToPlayerPrefs()
    {
        // Save primitive values directly
        PlayerPrefs.SetInt("CoinCount", coinCount);
        PlayerPrefs.SetInt("EventProgress", eventProgress);
        PlayerPrefs.SetInt("EventProgressMax", eventProgressMax);

        // Save player count
        PlayerPrefs.SetInt("PlayerCount", playerDataList.Count);

        // For each player in the list, save their data
        for (int i = 0; i < playerDataList.Count; i++)
        {
            if (playerDataList[i] != null)
            {
                // Store a reference to the player data asset name
                PlayerPrefs.SetString($"PlayerData_{i}_Name", playerDataList[i].name);

                // Have each player data save itself
                playerDataList[i].SaveToPlayerPrefs();
            }
        }

        // We don't save enemy data to PlayerPrefs as they are loaded from static files
        // We also don't save currentBattleWave as it's transient

        PlayerPrefs.Save();
        Debug.Log($"Game data saved: Coins: {coinCount}, Event Progress: {eventProgress}/{eventProgressMax}");
    }

    // Load game data from PlayerPrefs
    public void LoadFromPlayerPrefs()
    {
        // *** IMPORTANT: Load static enemy data first or here, as it's needed regardless of save state ***
        LoadEnemyData(); // Ensure static enemy data is loaded

        if (!PlayerPrefs.HasKey("CoinCount"))
        {
            Debug.Log("No game data found in PlayerPrefs. Using default values.");
             // Default values are already set in the ScriptableObject or remain at 0/null
            return; // Exit load if no save data
        }

        try
        {
            // Load primitive values
            coinCount = PlayerPrefs.GetInt("CoinCount", 0);
            eventProgress = PlayerPrefs.GetInt("EventProgress", 0);
            eventProgressMax = PlayerPrefs.GetInt("EventProgressMax", 5);

            // Load player data list
            int playerCount = PlayerPrefs.GetInt("PlayerCount", 0);
            playerDataList.Clear();

            for (int i = 0; i < playerCount; i++)
            {
                string playerAssetName = PlayerPrefs.GetString($"PlayerData_{i}_Name", "");

                if (!string.IsNullOrEmpty(playerAssetName))
                {
                    // Try to find the player data asset by name
                    // Note: This assumes the asset exists in Resources/PlayerData
                    // and the PlayerData scriptable object's 'name' field matches the filename
                    PlayerData playerData = Resources.Load<PlayerData>($"PlayerData/{playerAssetName}");

                    if (playerData != null)
                    {
                        // Load the player's saved data (HP, Mana, XP, etc.)
                        playerData.LoadFromPlayerPrefs();
                        playerDataList.Add(playerData);
                    }
                    else
                    {
                        Debug.LogWarning($"Could not find PlayerData asset named '{playerAssetName}' in Resources/PlayerData");
                    }
                }
            }


            Debug.Log($"Game data loaded: Coins: {coinCount}, Event Progress: {eventProgress}/{eventProgressMax}, Players: {playerDataList.Count}, Enemies: {enemyDataList.Count}");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Error loading game data: {e.Message}");
        }
    }

    // Load enemy data from static files in Resources
    private void LoadEnemyData()
    {
        // Clear existing enemy data
        enemyDataList.Clear();

        // Try to load all enemy data assets from the Resources/EnemyData folder
        EnemyData[] loadedEnemies = Resources.LoadAll<EnemyData>("EnemyData");

        if (loadedEnemies != null && loadedEnemies.Length > 0)
        {
            // Add all loaded enemy data to our list
            enemyDataList.AddRange(loadedEnemies);
            Debug.Log($"Loaded {enemyDataList.Count} enemies from Resources/EnemyData");
        }
        else
        {
            Debug.LogWarning("No enemy data found in Resources/EnemyData folder");
        }
    }

    // Reset game data to defaults
    public void ResetGameData()
    {
        coinCount = 0;
        eventProgress = 0;
        eventProgressMax = 5;
        playerDataList.Clear(); // Reset player list

        // Load static enemy data again to ensure it's present after reset
        LoadEnemyData();

        // Save the reset data to persist the state (empty player list)
        SaveToPlayerPrefs();

        Debug.Log("Game data has been reset to defaults");
    }

    // Add a new player to the game
    public void AddPlayer(PlayerData playerData)
    {
        if (playerData != null && !playerDataList.Contains(playerData))
        {
            // Check if a player with the same name already exists (optional, but good practice)
            if (playerDataList.Exists(p => p.playerName == playerData.playerName))
            {
                 Debug.LogWarning($"Player with name '{playerData.playerName}' already exists in GameData.");
                 return;
            }
            playerDataList.Add(playerData);
            Debug.Log($"Added player '{playerData.playerName}' to game data. Total players: {playerDataList.Count}");
        } else if (playerData == null)
        {
             Debug.LogWarning("Attempted to add a null PlayerData object to game data.");
        } else {
             Debug.LogWarning($"Attempted to add player '{playerData.playerName}' which is already in GameData.");
        }
    }

    // Remove a player from the game
    public void RemovePlayer(PlayerData playerData)
    {
        if (playerData != null && playerDataList.Contains(playerData))
        {
            playerDataList.Remove(playerData);
            Debug.Log($"Removed player '{playerData.playerName}' from game data. Total players: {playerDataList.Count}");
        } else if (playerData == null)
        {
             Debug.LogWarning("Attempted to remove a null PlayerData object from game data.");
        } else {
             Debug.LogWarning($"Attempted to remove player '{playerData.playerName}' which is not in GameData.");
        }
    }

    // Get enemy data by type (might not be needed anymore if using Wave Configs)
    public EnemyData GetEnemyDataByType(string enemyType)
    {
        return enemyDataList.Find(e => e.enemyType == enemyType);
    }

    // Get random enemy data (might still be useful for Mystery events or random encounters)
    public EnemyData GetRandomEnemyData()
    {
        if (enemyDataList.Count == 0)
        {
            Debug.LogWarning("No enemy data available to select from");
            return null;
        }

        return enemyDataList[Random.Range(0, enemyDataList.Count)];
    }

    // Log current game state
    public void LogGameState()
    {
        Debug.Log($"--- Current Game State ---");
        Debug.Log($"Coins: {coinCount}, Event Progress: {eventProgress}/{eventProgressMax}");
        Debug.Log($"Players: ({playerDataList.Count})");

        foreach (PlayerData player in playerDataList)
        {
            if (player != null)
            {
                player.LogStat();
            } else {
                Debug.Log("- NULL PlayerData entry");
            }
        }

        Debug.Log($"Enemies Loaded (from Resources): ({enemyDataList.Count})");
         // Don't log stats of static EnemyData assets, only instances in battle

        Debug.Log($"Next Battle Wave: {currentBattleWave?.waveName ?? "None"}");
        Debug.Log($"--- End Game State ---");
    }

    public void IncreaseEventProgress(int amount)
    {
        eventProgress += amount;
        if (eventProgress > eventProgressMax)
        {
            eventProgress = eventProgressMax;
        }
         // Automatically save after event progress changes
         SaveToPlayerPrefs();
    }

    public void AddGold(int amount)
    {
        coinCount += amount;
         // Automatically save after currency changes
         SaveToPlayerPrefs();
    }

    public void AddExperience(int amount)
    {
        foreach (PlayerData player in playerDataList)
        {
            if (player != null)
            {
                player.experience += amount; // LevelUp logic is in Player script/PlayerData
            }
        }
         // Automatically save after XP changes
         SaveToPlayerPrefs();
    }

    public void ApplyStatBoostToAllPlayers(string statName, int amount)
    {
        foreach (PlayerData player in playerDataList)
        {
            if (player != null)
            {
                player.ApplyStatBoost(statName, amount); // Apply to persistent data
            }
        }
         // Automatically save after stat changes
         SaveToPlayerPrefs();
    }

    public void ApplyDamageToAllPlayers(int damage)
    {
        foreach (PlayerData player in playerDataList)
        {
            if (player != null)
            {
                player.ApplyTrueDamage(damage); // Apply to persistent data
            }
        }
         // Automatically save after damage changes
         SaveToPlayerPrefs();
    }
}