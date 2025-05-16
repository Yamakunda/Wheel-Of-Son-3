using UnityEngine;
using UnityEngine.Audio; // Optional, for using Audio Mixers if needed

public class AudioManager : MonoBehaviour
{
    // Singleton instance
    public static AudioManager Instance { get; private set; }

    [Header("Audio Sources")]
    [SerializeField] private AudioSource musicSource; // Assigned in Inspector or added dynamically

    [Header("Music Clips")]
    [SerializeField] private AudioClip defaultMusicClip; // Default music to play if none specified
    [SerializeField] private AudioClip menuMusicClip;
    [SerializeField] private AudioClip gameMusicClip;
    // Add more clips here for different scenes/situations as needed

    // Save key for volume settings
    private const string MusicVolumeKey = "MusicVolume";

    private void Awake()
    {
        // Singleton pattern
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject); // Keep this object across scenes
            Debug.Log("AudioManager instance created and set to DontDestroyOnLoad.");

            // Get or add AudioSource component
            if (musicSource == null)
            {
                musicSource = GetComponent<AudioSource>();
                if (musicSource == null)
                {
                    musicSource = gameObject.AddComponent<AudioSource>();
                    Debug.Log("AudioManager: Added AudioSource component.");
                }
            }

            // Configure AudioSource for 2D music
            musicSource.playOnAwake = false;
            musicSource.spatialBlend = 0; // 0 for 2D sound

            // Load and apply saved volume setting
            LoadVolume();

        }
        else if (Instance != this)
        {
            // If an instance already exists and it's not this one, destroy this one
            Debug.LogWarning("Another AudioManager instance found, destroying this one.");
            Destroy(gameObject);
            return; // Stop further execution in this Awake call
        }
    }

    private void Start()
    {
        // You could start playing default music here, or let other managers call PlayMusic
        // Decided to let scene managers (MenuManager, GameManager) trigger music playback.
        // This Start() is mostly for singleton setup completion confirmation.
        Debug.Log("AudioManager Start called.");
    }

    /// <summary>
    /// Plays the specified music clip. Stops current music first.
    /// </summary>
    /// <param name="clip">The AudioClip to play.</param>
    /// <param name="loop">Whether the clip should loop.</param>
    public void PlayMusic(AudioClip clip, bool loop = true)
    {
        if (clip == null)
        {
            Debug.LogWarning("AudioManager: Attempted to play a null music clip.");
            // Optionally stop current music if a null clip is explicitly requested
            // musicSource.Stop();
            return;
        }

        if (musicSource.clip == clip && musicSource.isPlaying)
        {
            // Already playing this clip
            // Debug.Log($"AudioManager: Music clip '{clip.name}' is already playing.");
            return;
        }

        Debug.Log($"AudioManager: Playing music clip: {clip.name}");
        musicSource.Stop(); // Stop current music
        musicSource.clip = clip;
        musicSource.loop = loop;
        musicSource.Play();
    }

    /// <summary>
    /// Stops the current music playback.
    /// </summary>
    public void StopMusic()
    {
        Debug.Log("AudioManager: Stopping music.");
        musicSource.Stop();
    }

    /// <summary>
    /// Sets the music volume and saves it to PlayerPrefs.
    /// </summary>
    /// <param name="volume">Volume level (0.0 to 1.0).</param>
    public void SetMusicVolume(float volume)
    {
        volume = Mathf.Clamp01(volume); // Ensure volume is between 0 and 1
        if (musicSource != null)
        {
            musicSource.volume = volume;
            Debug.Log($"AudioManager: Music volume set to {volume:F2}");

            // Save the volume setting
            PlayerPrefs.SetFloat(MusicVolumeKey, volume);
            PlayerPrefs.Save(); // Immediately save PlayerPrefs
        } else {
             Debug.LogError("AudioManager: Cannot set volume, musicSource is null.");
        }
    }

    /// <summary>
    /// Loads the music volume from PlayerPrefs.
    /// </summary>
    private void LoadVolume()
    {
        // Default volume is 1.0 if no key is found
        float savedVolume = PlayerPrefs.GetFloat(MusicVolumeKey, 1.0f);
        Debug.Log($"AudioManager: Loaded music volume: {savedVolume:F2}");

        // Apply the loaded volume
         if (musicSource != null)
         {
            musicSource.volume = savedVolume;
         } else {
             Debug.LogError("AudioManager: Cannot apply loaded volume, musicSource is null.");
         }
    }

     // Public methods to play specific common clips
     public void PlayMenuMusic()
     {
         PlayMusic(menuMusicClip);
     }

     public void PlayGameMusic()
     {
         PlayMusic(gameMusicClip);
     }
    
    public AudioSource GetMusicSource()
    {
        return musicSource;
    }
     // Add other specific play methods if needed, e.g., PlayBattleMusic()
     // public void PlayBattleMusic()
     // {
     //     PlayMusic(battleMusicClip); // Assuming battleMusicClip is a field
     // }
     // public void PlayDefaultMusic()
     // {
     //     PlayMusic(defaultMusicClip);
     // }

}
// --- END OF FILE AudioManager.cs ---