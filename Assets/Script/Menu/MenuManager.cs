using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.EventSystems; // Make sure this is included for EventTrigger
using TMPro;
public class MenuManager : MonoBehaviour
{
    public Button newGameButton;
    public Button loadGameButton; // This will now be the "Continue Game" button
    public Button shopButton;
    public Button settingsButton;
    public Button exitButton;
    public AudioSource audioSource; // Assign in Inspector (for UI sounds)
    public AudioClip hoverSound; // Add this for hover sound
    public AudioClip clickSound; // Add this for click sound

    [Header("Music")] // Add header for music clips
    [SerializeField] private AudioClip menuMusicClip; // Assign your menu music here

    public TMP_FontAsset pixelFont; // Assign VT323-Regular SDF in inspector
    public TMP_FontAsset defaultFont;

    // Settings Popup
    public GameObject settingsCanvas; // The SettingsCanvas
    public Button overlayBackgroundButton; // The clickable overlay to close the popup
    public TMP_Dropdown languageDropdown;
    public TMP_Dropdown fontDropdown;
    public Slider musicSlider;
    void Start()
    {
        Debug.Log("MenuManager Start called.");

        // Play menu music when the scene starts
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayMenuMusic(); // Use the dedicated method
        } else {
             Debug.LogError("AudioManager.Instance is null! Cannot play menu music.");
        }


        // Set up button listeners
        newGameButton.onClick.AddListener(() =>
        {
            PlayClickSound();
            StartNewGame();
        });

        // --- MODIFIED: Load Game Button -> Continue Logic ---
        // Assign listener *before* checking visibility
        loadGameButton.onClick.AddListener(() =>
        {
            PlayClickSound();
            ContinueGame(); // Call the new ContinueGame method
        });

        // Check for saved game data and set visibility/interactability
        CheckForSavedGame();
        // --- END MODIFIED ---


        shopButton.onClick.AddListener(() =>
        {
            PlayClickSound();
            OpenShop();
        });

        settingsButton.onClick.AddListener(() =>
        {
            PlayClickSound();
            OpenSettings();
        });

        exitButton.onClick.AddListener(() =>
        {
            PlayClickSound();
            ExitGame();
        });

        // Add hover sound for each button
        AddHoverSound(newGameButton);
        if (loadGameButton != null) AddHoverSound(loadGameButton); // Add hover for continue button, null check
        AddHoverSound(shopButton);
        AddHoverSound(settingsButton);
        AddHoverSound(exitButton);

        // Settings popup setup
        if (overlayBackgroundButton != null) overlayBackgroundButton.onClick.AddListener(CloseSettings);
        if (languageDropdown != null) languageDropdown.onValueChanged.AddListener(delegate { OnLanguageChanged(); });
        if (fontDropdown != null) fontDropdown.onValueChanged.AddListener(delegate { OnFontChanged(); });
        if (musicSlider != null) musicSlider.onValueChanged.AddListener(delegate { OnMusicVolumeChanged(); });

        // Load saved settings (including volume)
        LoadSettings();
    }

    // --- NEW METHOD: Check for Saved Game Data ---
    private void CheckForSavedGame()
    {
        // Ensure GameDataManager and GameData are available
         if (GameDataManager.Instance == null || GameDataManager.Instance.gameData == null)
         {
             Debug.LogError("GameDataManager or GameData is null in MenuManager.CheckForSavedGame!");
             if (loadGameButton != null) loadGameButton.gameObject.SetActive(false); // Hide if cannot check
             return;
         }

        // We can check if the CoinCount key exists AND if there's at least one player saved.
        // PlayerCount is saved when GameData.SaveToPlayerPrefs is called.
        // GameData.LoadFromPlayerPrefs is called in GameDataManager.Awake, so playerDataList should be populated if a save exists.
        bool saveExists = GameDataManager.Instance.gameData.playerDataList.Count > 0; // More reliable check after load

        // Enable or disable the continue button GameObject
        if (loadGameButton != null)
        {
            loadGameButton.gameObject.SetActive(saveExists);
            Debug.Log($"Continue Game button set to active: {saveExists}");
        }
        else
        {
            Debug.LogWarning("Load/Continue Game Button is not assigned in the MenuManager inspector!");
        }
    }
    // --- END NEW METHOD ---

    // --- NEW METHOD: Continue Game Logic ---
    private void ContinueGame()
    {
        Debug.Log("Continuing Game...");

        // The GameDataManager.Awake method automatically loads data from PlayerPrefs
        // when the scene starts. We just need to transition to the game scene.
        // Add a safety check here just in case, though CheckForSavedGame should prevent this.
         if (GameDataManager.Instance != null && GameDataManager.Instance.gameData != null && GameDataManager.Instance.gameData.playerDataList.Count > 0)
         {
              // Optionally stop menu music before loading the next scene
              if (AudioManager.Instance != null) AudioManager.Instance.StopMusic();

              SceneManager.LoadScene("GameScene"); // Load the main game scene
         } else {
              Debug.LogError("Attempted to continue game, but no valid player data was loaded. Check save logic.");
              // Optionally hide the button again or show an error message
              CheckForSavedGame(); // Re-run check in case state changed unexpectedly
         }
    }
    // --- END NEW METHOD ---

    void AddHoverSound(Button button)
    {
         if (button == null) return; // Safety check
        EventTrigger trigger = button.gameObject.GetComponent<EventTrigger>(); // Get existing or add
        if (trigger == null) trigger = button.gameObject.AddComponent<EventTrigger>();

        // Check if PointerEnter entry already exists to avoid adding duplicates
        bool entryExists = false;
        foreach(var entry in trigger.triggers)
        {
            if(entry.eventID == EventTriggerType.PointerEnter)
            {
                entryExists = true;
                break;
            }
        }

        if (!entryExists)
        {
            var entry = new EventTrigger.Entry { eventID = EventTriggerType.PointerEnter };
             // Use a local function or lambda to capture hoverSound
            entry.callback.AddListener((data) =>
            {
                if (audioSource != null && hoverSound != null)
                {
                    audioSource.PlayOneShot(hoverSound);
                }
            });
            trigger.triggers.Add(entry);
        }
    }


    void PlayClickSound()
    {
        if (audioSource != null && clickSound != null)
            audioSource.PlayOneShot(clickSound);
    }

    void StartNewGame()
    {
        Debug.Log("Starting New Game...");
        // Optionally stop menu music before loading the next scene
        if (AudioManager.Instance != null) AudioManager.Instance.StopMusic();
        // New Game flow begins with Character Creation
        SceneManager.LoadScene("CharacterCreateScene");
    }

    void OpenShop()
    {
        Debug.Log("Opening Shop...");
        // TODO: Implement shop scene/UI loading
    }

    void OpenSettings()
    {
        Debug.Log("Opening Settings...");
        if (settingsCanvas != null) settingsCanvas.SetActive(true);
        // Reload volume slider value when opening settings
        if (musicSlider != null)
        {
            // Ensure AudioManager exists before trying to load from it
            if (AudioManager.Instance != null)
            {
                 // Volume is loaded and applied in AudioManager.Awake
                 // Just update the slider position based on the AudioManager's current volume
                 musicSlider.value = AudioManager.Instance.GetMusicSource().volume;
                 // Or, if AudioManager had a public GetMusicVolume() method
                 // musicSlider.value = AudioManager.Instance.GetMusicVolume();
            } else {
                // Fallback: load directly from PlayerPrefs if AudioManager isn't ready (shouldn't happen)
                 musicSlider.value = PlayerPrefs.GetFloat("MusicVolume", 1f);
            }
        }

    }

    void CloseSettings()
    {
        Debug.Log("Closing Settings...");
        if (settingsCanvas != null) settingsCanvas.SetActive(false);
    }

    void ExitGame()
    {
        Debug.Log("Exiting Game...");
        #if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
        #else
        Application.Quit();
        #endif
    }

    // Settings functionality
    void OnLanguageChanged()
    {
         if (languageDropdown == null) return; // Safety check
        string selectedLanguage = languageDropdown.options[languageDropdown.value].text;
        Debug.Log("Language changed to: " + selectedLanguage);
        PlayerPrefs.SetString("Language", selectedLanguage);
        PlayerPrefs.Save(); // Save change immediately
        // Add logic to update game language (e.g., reload text assets)
    }

    void OnFontChanged()
    {
         if (fontDropdown == null) return; // Safety check
        string selectedFont = fontDropdown.options[fontDropdown.value].text;
        Debug.Log("Font changed to: " + selectedFont);
        PlayerPrefs.SetString("Font", selectedFont);
        PlayerPrefs.Save(); // Save change immediately

        // Apply the font change
        TMP_FontAsset fontToApply = null;
        if (selectedFont == "Pixel Font") // Make sure this matches the option text in the Dropdown
        {
            fontToApply = pixelFont;
        }
        else if (selectedFont == "Default Font") // Match your default option text
        {
             fontToApply = defaultFont;
        }

        // Apply to all text elements (Be cautious with this approach, might affect unintended UI)
        if (fontToApply != null)
        {
             // Using FindObjectsByType is the modern way in recent Unity versions
            TextMeshProUGUI[] allTexts = FindObjectsByType<TextMeshProUGUI>(FindObjectsSortMode.None);
            foreach (TextMeshProUGUI text in allTexts)
            {
                 // Consider adding a tag or a specific component to UI elements
                 // you *want* the font to apply to, to avoid changing everything.
                text.font = fontToApply;
            }
        }
    }

    void OnMusicVolumeChanged()
    {
        if (musicSlider == null) return; // Safety check
        float volume = musicSlider.value;
        Debug.Log("Music volume slider changed to: " + volume);

        // Delegate setting and saving volume to the AudioManager
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.SetMusicVolume(volume);
        } else {
            Debug.LogError("AudioManager.Instance is null! Cannot set music volume.");
             // Manual save as fallback (less ideal)
             PlayerPrefs.SetFloat("MusicVolume", volume);
             PlayerPrefs.Save();
             // Set AudioListener volume as a final fallback (affects all audio)
             // AudioListener.volume = volume; // Removed this from original plan as AudioManager handles it
        }
    }

    void LoadSettings()
    {
        Debug.Log("MenuManager: Loading Settings...");
        // Load saved settings
        if (languageDropdown != null)
        {
             string savedLanguage = PlayerPrefs.GetString("Language", "English");
             int langIndex = languageDropdown.options.FindIndex(option => option.text == savedLanguage);
             languageDropdown.value = Mathf.Clamp(langIndex, 0, languageDropdown.options.Count - 1);
        }


        if (fontDropdown != null)
        {
             string savedFont = PlayerPrefs.GetString("Font", "Pixel Font"); // Default must match an option text
             int fontIndex = fontDropdown.options.FindIndex(option => option.text == savedFont);
             fontDropdown.value = Mathf.Clamp(fontIndex, 0, fontIndex != -1 ? fontDropdown.options.Count - 1 : 0); // Clamp correctly
             // Trigger the font change immediately after loading to apply the saved font
             OnFontChanged();
        }


        if (musicSlider != null)
        {
             // Volume is loaded and applied in AudioManager.Awake.
             // We just need to set the slider's initial position based on the loaded volume.
             // Ensure AudioManager exists and has a musicSource before getting its volume.
             if (AudioManager.Instance != null && AudioManager.Instance.GetMusicSource() != null)
             {
                 musicSlider.value = AudioManager.Instance.GetMusicSource().volume;
             } else {
                  // Fallback: load directly from PlayerPrefs if AudioManager isn't ready
                  musicSlider.value = PlayerPrefs.GetFloat("MusicVolume", 1f);
             }
             // No need to call OnMusicVolumeChanged or SetMusicVolume here,
             // AudioManager.Awake already applied the loaded volume.
        }
         Debug.Log("MenuManager: Settings loaded.");
    }
}