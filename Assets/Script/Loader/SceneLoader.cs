// --- START OF FILE SceneLoader.cs ---

using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections; // Required for IEnumerator
using UnityEngine.UI; // Required for Slider
using TMPro; // Required for TextMeshProUGUI

public class SceneLoader : MonoBehaviour
{
    [Header("Next Scene")]
    [SerializeField] private string nextSceneName = "LobbyScene"; // Name of the scene to load after initialization

    // Optional: Add a small delay before starting the load (e.g., to show a logo)
    [SerializeField] private float initialDelay = 0.5f; // Seconds

    [Header("Loading UI")]
    [SerializeField] private Slider loadingSlider; // Assign your UI Slider here
    [SerializeField] private TextMeshProUGUI loadingText; // Assign your UI TextMeshProUGUI here

    void Start()
    {
        Debug.Log("LoaderScene started. Initializing managers...");

        // Ensure GameDataManager instance is initialized (its Awake should handle this)
        // Explicitly access managers to ensure their singletons are initialized.
         if (GameDataManager.Instance == null)
         {
             Debug.LogError("GameDataManager.Instance is null! Ensure GameDataManager is in the scene and set to DontDestroyOnLoad.");
             // Handle this critical error - perhaps load a specific error scene
             // For now, we'll continue, but dependent systems might fail.
         } else {
             Debug.Log("GameDataManager instance confirmed.");
         }

         if (AudioManager.Instance == null)
         {
              Debug.LogError("AudioManager.Instance is null! Ensure AudioManager is in the scene and set to DontDestroyOnLoad.");
              // Handle this critical error
         } else {
             Debug.Log("AudioManager instance confirmed.");
         }
        // Add other manager checks here if needed...


        // Ensure UI elements are assigned and potentially set initial state
        if (loadingSlider != null) loadingSlider.value = 0; // Start at 0%
        if (loadingText != null) loadingText.text = "Loading..."; // Initial text


        // Start the coroutine to handle the loading process
        StartCoroutine(LoadNextSceneProcess());
    }

    private IEnumerator LoadNextSceneProcess()
    {
        // Initial delay before starting the scene load
        yield return new WaitForSeconds(initialDelay);

        Debug.Log($"Starting to load next scene: {nextSceneName}");

        // Start loading the next scene asynchronously
        AsyncOperation asyncLoad = SceneManager.LoadSceneAsync(nextSceneName);

        // Prevent the scene from activating immediately upon loading completion
        // This allows you to control when the new scene actually becomes active.
        asyncLoad.allowSceneActivation = false;

        // Wait until the scene is fully loaded (progress reaches 0.9)
        // asyncLoad.progress goes from 0 to 0.9 while loading, then jumps to 1
        // when allowSceneActivation is true. If allowSceneActivation is false,
        // it stays at 0.9 until you set allowSceneActivation to true.
        while (!asyncLoad.isDone)
        {
            // Get the loading progress (value is typically 0 to 0.9)
            float progress = Mathf.Clamp01(asyncLoad.progress / 0.9f); // Normalize 0-0.9 range to 0-1

            // Update the UI elements
            if (loadingSlider != null) loadingSlider.value = progress;
            if (loadingText != null) loadingText.text = $"Loading {progress * 100f:F0}%"; // Display percentage (no decimals)

            Debug.Log($"Loading progress: {progress * 100f:F0}%");

            // Loading is complete when progress reaches 0.9
            if (asyncLoad.progress >= 0.9f)
            {
                // Loading is done, we can update UI to 100% and wait for activation input/delay

                // Ensure UI shows 100%
                if (loadingSlider != null) loadingSlider.value = 1;
                if (loadingText != null) loadingText.text = "Loading Complete!"; // Final text
                Debug.Log("Scene loading reached 100%. Ready for activation.");


                // *** Activation Logic ***
                // You can add a brief final delay before activating the scene for visual polish,
                // or wait for a button press, etc.
                yield return new WaitForSeconds(0.5f); // Example: wait 0.5 seconds at 100%


                // Activate the new scene
                asyncLoad.allowSceneActivation = true;

                // The loop condition `!asyncLoad.isDone` will now become false, and the coroutine will exit.
            }

            yield return null; // Wait for the next frame
        }

        // The scene is now fully active.
         Debug.Log($"{nextSceneName} activated.");
         // This GameObject (SceneLoader) can be destroyed now, as GameDataManager is persistent
         Destroy(gameObject);

    }

     // Optional: Add an Awake method if you need to ensure specific manager initialization order
    void Awake()
    {
        // Explicitly access managers here if their instantiation is crucial
        // and might not happen automatically before Start (less likely for singletons
        // placed in the initial scene and set to DontDestroyOnLoad, but can add here
        // for extra safety or if manually instantiating prefabs).
         // Accessing them like this ensures their Awake methods run:
         var gm = GameDataManager.Instance;
         var am = AudioManager.Instance;
         // Add others if needed...
    }
}

// --- END OF FILE SceneLoader.cs ---