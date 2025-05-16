using UnityEngine;

public class GameDataManager : MonoBehaviour
{
    // Make this a persistent singleton
    public static GameDataManager Instance { get; private set; }

    public GameData gameData; // Reference to the ScriptableObject asset

    private void Awake()
    {
        // Singleton pattern implementation
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject); // Keep this object alive across scenes
            Debug.Log("GameDataManager instance created and set to DontDestroyOnLoad.");
        }
        else if (Instance != this)
        {
            // If an instance already exists and it's not this one, destroy this one
            Debug.LogWarning("Another GameDataManager instance found, destroying this one.");
            Destroy(gameObject);
            return; // Stop further execution in this Awake call
        }

        // Ensure gameData SO is assigned, maybe try loading if null
        if (gameData == null)
        {
            gameData = Resources.Load<GameData>("GameData"); // Load the SO asset
            if (gameData == null)
            {
                Debug.LogError("GameData ScriptableObject asset not found in Resources folder!");
                // TODO: Handle this critical error - maybe create a default one?
            } else {
                 Debug.Log("GameData ScriptableObject loaded from Resources.");
            }
        }

        // Load game data from PlayerPrefs when the manager wakes up
        if (gameData != null)
        {
             gameData.LoadFromPlayerPrefs();
             Debug.Log("GameDataManager loaded game data from PlayerPrefs.");
        }

    }

    // No longer need explicit Save/Load methods here, they are handled by GameData.
    // Scripts should call GameDataManager.Instance.gameData.SaveToPlayerPrefs() etc. directly

    // Removed: SaveGameData, LoadGameData, ResetGameData, logStat methods from here.
    // These actions are now methods on the GameData ScriptableObject itself,
    // accessed via GameDataManager.Instance.gameData.MethodName().
}