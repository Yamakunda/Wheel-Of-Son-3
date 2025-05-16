// --- START OF FILE CharacterCreator.cs ---

using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine.TextCore.Text;

public class WheelSegment
{
    public string attribute;
    public float weight;
    public Color backgroundColor;

    public WheelSegment(string attr, float wgt, Color col)
    {
        attribute = attr;
        weight = wgt;
        backgroundColor = col;
    }
}

public class CharacterCreator : MonoBehaviour
{
    // UI Elements
    public Image wheelImage;
    public Sprite wheelSprite;
    public Button spinButton;
    public Button nextButton;
    public TextMeshProUGUI raceText;
    public TextMeshProUGUI traitText;
    public TextMeshProUGUI strengthText;
    public TextMeshProUGUI magicText;
    public TextMeshProUGUI defendText;
    public TextMeshProUGUI durabilityText;
    public TextMeshProUGUI speedText;
    public TextMeshProUGUI currentAttributeText;
    public RectTransform needlePointer;
    private TextMeshProUGUI[] wheelSectionTexts;
    [SerializeField] private TMP_FontAsset pixelFont; // Assign in inspector or load from resources
    private int numberOfSections = 0;

    // Audio (for wheel sounds, not background music)
    public AudioSource audioSource;
    public AudioClip spinSound;
    public AudioClip resultSound;

    // Attribute collections using WheelSegment
    private List<WheelSegment> races = new List<WheelSegment>
    {
        new WheelSegment("Human", 5f, new Color(0.9f, 0.7f, 0.6f)),
        new WheelSegment("Elf", 4f, new Color(0.6f, 0.9f, 0.6f)),
        new WheelSegment("Dwarf", 3f, new Color(0.8f, 0.6f, 0.4f)),
        new WheelSegment("Orc", 2f, new Color(0.4f, 0.8f, 0.4f)),
        new WheelSegment("Gnome", 1f, new Color(0.9f, 0.9f, 0.5f))
    };

    private List<WheelSegment> traits = new List<WheelSegment>
    {
        new WheelSegment("Brave", 1f, new Color(0.9f, 0.3f, 0.3f)),
        new WheelSegment("Cunning", 1f, new Color(0.3f, 0.3f, 0.9f)),
        new WheelSegment("Wise", 1f, new Color(0.3f, 0.9f, 0.9f)),
        new WheelSegment("Swift", 1f, new Color(0.9f, 0.9f, 0.3f)),
        new WheelSegment("Strong", 1f, new Color(0.9f, 0.5f, 0.3f))
    };

    private List<WheelSegment> strengths = new List<WheelSegment>
    {
        new WheelSegment("1", 1f, new Color(0.9f, 0.3f, 0.3f, 0.7f)),
        new WheelSegment("2", 1f, new Color(0.9f, 0.4f, 0.3f, 0.7f)),
        new WheelSegment("3", 1f, new Color(0.9f, 0.5f, 0.3f, 0.7f)),
        new WheelSegment("4", 1f, new Color(0.9f, 0.6f, 0.3f, 0.7f)),
        new WheelSegment("5", 1f, new Color(0.9f, 0.7f, 0.3f, 0.7f))
    };

    private List<WheelSegment> magics = new List<WheelSegment>
    {
        new WheelSegment("1", 1f, new Color(0.3f, 0.3f, 0.9f, 0.7f)),
        new WheelSegment("2", 1f, new Color(0.4f, 0.3f, 0.9f, 0.7f)),
        new WheelSegment("3", 1f, new Color(0.5f, 0.3f, 0.9f, 0.7f)),
        new WheelSegment("4", 1f, new Color(0.6f, 0.3f, 0.9f, 0.7f)),
        new WheelSegment("5", 1f, new Color(0.7f, 0.3f, 0.9f, 0.7f))
    };

    private List<WheelSegment> defends = new List<WheelSegment>
    {
        new WheelSegment("1", 1f, new Color(0.3f, 0.9f, 0.3f, 0.7f)),
        new WheelSegment("2", 1f, new Color(0.3f, 0.9f, 0.4f, 0.7f)),
        new WheelSegment("3", 1f, new Color(0.3f, 0.9f, 0.5f, 0.7f)),
        new WheelSegment("4", 1f, new Color(0.3f, 0.9f, 0.6f, 0.7f)),
        new WheelSegment("5", 1f, new Color(0.3f, 0.9f, 0.7f, 0.7f))
    };

    private List<WheelSegment> durabilities = new List<WheelSegment>
    {
        new WheelSegment("1", 1f, new Color(0.9f, 0.9f, 0.3f, 0.7f)),
        new WheelSegment("2", 1f, new Color(0.9f, 0.9f, 0.4f, 0.7f)),
        new WheelSegment("3", 1f, new Color(0.9f, 0.9f, 0.5f, 0.7f)),
        new WheelSegment("4", 1f, new Color(0.9f, 0.9f, 0.6f, 0.7f)),
        new WheelSegment("5", 1f, new Color(0.9f, 0.9f, 0.7f, 0.7f))
    };

    private List<WheelSegment> speeds = new List<WheelSegment>
    {
        new WheelSegment("1", 1f, new Color(0.3f, 0.9f, 0.9f, 0.7f)),
        new WheelSegment("2", 1f, new Color(0.4f, 0.9f, 0.9f, 0.7f)),
        new WheelSegment("3", 1f, new Color(0.5f, 0.9f, 0.9f, 0.7f)),
        new WheelSegment("4", 1f, new Color(0.6f, 0.9f, 0.9f, 0.7f)),
        new WheelSegment("5", 1f, new Color(0.7f, 0.9f, 0.9f, 0.7f))
    };

    // State tracking
    private int currentAttribute = 0;
    private string[] attributeNames = { "Race", "Trait", "Strength", "Magic", "Durability", "Defend", "Speed" };
    private bool isSpinning = false;
    private WheelSegment[] currentWheelSegments;
    private GameObject[] wheelBackgrounds;

    public string nextSceneName = "GameScene"; // Set this in the inspector

    void Start()
    {
        // Initialize UI
        if(spinButton != null) spinButton.onClick.AddListener(SpinWheel);
        if(nextButton != null) nextButton.onClick.AddListener(MoveToNextAttribute);

        // Position the needle at the top of the wheel
        if (needlePointer != null)
        {
            // Place the needle at the top edge of the wheel
            float wheelRadius = wheelImage.rectTransform.rect.width / 2f;
            needlePointer.anchoredPosition = new Vector2(0, wheelRadius);
            needlePointer.localRotation = Quaternion.Euler(0, 0, 0); // Point downward
        }

        // Create and position wheel section texts
        PositionWheelTexts();

        // Update UI with initial attribute
        UpdateAttributeDisplay();
    }

    void UpdateWheelSections()
    {
        // Get the current attribute segments
        List<WheelSegment> currentSegments = GetCurrentAttributeSegments();

        // Update arrays for reference
        currentWheelSegments = currentSegments.ToArray();
        numberOfSections = currentWheelSegments.Length;

        // Ensure wheel section texts are created and positioned
        if (wheelSectionTexts == null || wheelSectionTexts.Length != numberOfSections)
        {
            PositionWheelTexts();
        }
        else
        {
            // Update existing wheel section texts
            for (int i = 0; i < numberOfSections; i++)
            {
                if (wheelSectionTexts[i] != null)
                {
                    wheelSectionTexts[i].text = currentWheelSegments[i].attribute;
                }
                // Adjust font size based on text length
                if (currentWheelSegments[i].attribute.Length > 6)
                {
                    if(wheelSectionTexts[i] != null) wheelSectionTexts[i].fontSize = 16; // Smaller font for longer text
                }
                else
                {
                     if(wheelSectionTexts[i] != null) wheelSectionTexts[i].fontSize = 20; // Normal size for shorter text
                }
            }
        }

        // Update segment backgrounds
        UpdateSegmentBackgrounds();
    }

    void UpdateSegmentBackgrounds()
    {
        if (wheelImage == null || wheelSprite == null) return; // Safety check

        float wheelRadius = wheelImage.rectTransform.rect.width / 2f;

        // Create new background array if needed
        if (wheelBackgrounds == null)
        {
            wheelBackgrounds = new GameObject[numberOfSections];
        }

        // Calculate total weight for angle calculation
        float totalWeight = 0f;
        for (int i = 0; i < numberOfSections; i++)
        {
             if(currentWheelSegments[i] != null)
                totalWeight += currentWheelSegments[i].weight;
        }
         if (totalWeight <= 0) totalWeight = 1; // Prevent division by zero

        // Track current angle position
        float currentAngle = 0;

        // Create/update each background segment
        for (int i = 0; i < numberOfSections; i++)
        {
             if(currentWheelSegments[i] == null) continue; // Skip null segments

            float sectionAngle = (currentWheelSegments[i].weight / totalWeight) * 360f;

            // Create or get the background GameObject
            if (wheelBackgrounds[i] == null)
            {
                wheelBackgrounds[i] = new GameObject("WheelBackground_" + i);
                 if (wheelImage.transform != null) wheelBackgrounds[i].transform.SetParent(wheelImage.transform, false);
                wheelBackgrounds[i].transform.SetAsFirstSibling(); // Ensure backgrounds are behind everything
            }

            // Create or update the pie-shaped segment
            Image image = wheelBackgrounds[i].GetComponent<Image>();
            if (image == null)
            {
                // Add UI Image component to render the background
                image = wheelBackgrounds[i].AddComponent<Image>();
                image.raycastTarget = false;
            }
            image.sprite = wheelSprite;
            // Set the color for this segment
            image.color = currentWheelSegments[i].backgroundColor;

            // Use a pie-shaped mask for this segment
            image.type = Image.Type.Filled;
            image.fillMethod = Image.FillMethod.Radial360;
            image.fillOrigin = 2; // Top position (needle position)
            image.fillClockwise = true;
            image.fillAmount = sectionAngle / 360f;

            // Get the background's RectTransform
            RectTransform bgRect = wheelBackgrounds[i].GetComponent<RectTransform>();
            if (bgRect != null)
            {
                // Position and size the background to cover the whole wheel
                bgRect.anchoredPosition = Vector2.zero; // Center of wheel
                bgRect.sizeDelta = new Vector2(wheelRadius * 2f, wheelRadius * 2f); // Cover whole wheel

                // Rotate the segment to the correct position based on accumulated angles
                bgRect.localRotation = Quaternion.Euler(0, 0, -currentAngle);
            }


            // Update text position and content at the same time
            if (wheelSectionTexts != null && i < wheelSectionTexts.Length && wheelSectionTexts[i] != null)
            {
                // Calculate angle for text placement (middle of the section)
                float textAngle = currentAngle + (sectionAngle / 2);
                float radians = textAngle * Mathf.Deg2Rad;

                // Calculate position (using a smaller radius to position text within segment)
                float textRadius = wheelRadius * 0.7f;
                float x = Mathf.Sin(radians) * textRadius;
                float y = Mathf.Cos(radians) * textRadius;

                // Position and rotate text
                RectTransform rectTransform = wheelSectionTexts[i].rectTransform;
                 if (rectTransform != null)
                 {
                     rectTransform.anchoredPosition = new Vector2(x, y);
                     rectTransform.localRotation = Quaternion.Euler(0, 0, -textAngle + 90);
                     rectTransform.sizeDelta = new Vector2(100, 40); // Ensure text box size is reasonable
                 }


                // Set text content
                wheelSectionTexts[i].text = currentWheelSegments[i].attribute;

                // Adjust font size based on text length
                if (currentWheelSegments[i].attribute.Length > 6)
                {
                    wheelSectionTexts[i].fontSize = 16; // Smaller font for longer text
                }
                else
                {
                    wheelSectionTexts[i].fontSize = 20; // Normal size for shorter text
                }
            }

            // Update current angle for next segment
            currentAngle += sectionAngle;
        }
    }

    List<WheelSegment> GetCurrentAttributeSegments()
    {
        switch (currentAttribute)
        {
            case 0: return races;
            case 1: return traits;
            case 2: return strengths;
            case 3: return magics;
            case 4: return durabilities;
            case 5: return defends;
            case 6: return speeds;
            default: return new List<WheelSegment> { new WheelSegment("Error", 1f, Color.red) };
        }
    }

    void UpdateAttributeDisplay()
    {
        if(spinButton != null) spinButton.interactable = !isSpinning;
        // Update the current attribute text
        if (currentAttributeText != null)
        {
            // Display the current attribute name
             if (currentAttribute < attributeNames.Length)
             {
                currentAttributeText.text = $"{attributeNames[currentAttribute]}";
             } else {
                 currentAttributeText.text = "Complete!";
             }
        }
        UpdateWheelSections();
    }

    void SpinWheel()
    {
        if (!isSpinning)
        {
            StartCoroutine(SpinCoroutine());
        }
    }

    IEnumerator SpinCoroutine()
    {
        isSpinning = true;
        if(spinButton != null) spinButton.interactable = false;

        // Play spin sound
        if (audioSource != null && spinSound != null)
        {
            audioSource.PlayOneShot(spinSound);
        }

        // Generate random parameters for the spin
        float spinTime = Random.Range(2.5f, 4.0f);  // Random spin duration
        float maxSpinSpeed = Random.Range(1080f, 1440f);  // 3-4 full rotations per second at max
        float spinDeceleration = maxSpinSpeed / spinTime;  // Deceleration rate

        // Track spin state
        float currentSpeed = maxSpinSpeed;  // Starting at max speed
        float totalRotation = 0f;
        float elapsed = 0f;

        // Store initial rotation as Z value
         float startRotationZ = wheelImage != null ? wheelImage.transform.localRotation.eulerAngles.z : 0f;


        // Spin the wheel with decreasing speed
        while (elapsed < spinTime)
        {
            // Calculate current speed (linear deceleration)
            currentSpeed = maxSpinSpeed - (spinDeceleration * elapsed);

            // Calculate rotation this frame
            float rotationThisFrame = currentSpeed * Time.deltaTime;
            totalRotation += rotationThisFrame;

            // Apply rotation
            if (wheelImage != null) wheelImage.transform.localRotation = Quaternion.Euler(0, 0, startRotationZ - totalRotation);

            elapsed += Time.deltaTime;
            yield return null;
        }

        // Make sure the wheel stops completely
        currentSpeed = 0f;

        // Play result sound
        if (audioSource != null && resultSound != null)
        {
            audioSource.PlayOneShot(resultSound);
        }

        // Calculate which section is under the needle (at top/0 degrees)
        int resultIndex = CalculateResultSection();

        // Get the result from the wheel section
         string result = "Unknown";
         if (currentWheelSegments != null && resultIndex >= 0 && resultIndex < currentWheelSegments.Length && currentWheelSegments[resultIndex] != null)
         {
             result = currentWheelSegments[resultIndex].attribute;
         } else {
              Debug.LogError($"CharacterCreator: Failed to get result segment at index {resultIndex}. numberOfSections: {numberOfSections}");
         }

        DisplayResult(result);

        if(spinButton != null) spinButton.interactable = true;
        isSpinning = false;
    }

    // Calculate which section is under the needle
    int CalculateResultSection()
    {
        if (wheelImage == null || currentWheelSegments == null || numberOfSections == 0) return 0; // Safety fallback

        // Get the current rotation of the wheel
        float currentRotationAngle = wheelImage.transform.localRotation.eulerAngles.z;

        // Normalize the angle to 0-360 range
        // The needle is at 0 degrees (top). Unity's Z rotation is clockwise from positive Y axis.
        // Let's adjust the wheel's rotation so that 0 degrees aligns with the *bottom* of the wheel,
        // and the needle at the top corresponds to 180 degrees relative to that 0 bottom point.
        // Or, simplify: The needle points UP (0 deg). We need to find which segment is currently
        // oriented such that its center is aligned with the upward vector.
        // The angle the wheel stopped at, relative to the needle's upward orientation (0 deg).
        // Let's assume the needle is fixed pointing UP (0 degrees localRotation).
        // The wheel image is rotated. If the wheel's Z rotation is `rot`, the needle points
        // at `rot` on the wheel. We need to find the segment at that `rot` position.
        // Unity's rotation is CCW from the positive X axis, for Z rotation.
        // Let's re-verify needle/wheel setup. The needlePointer is anchored at top (0, Radius), rotated 0,0,0.
        // The wheel image is at center, rotated.
        // If the needle points "up" (0 degrees), and the segments are laid out starting from "up" and going CCW,
        // then the angle we care about is the negative of the wheel's Z rotation.
        // Let wheelAngle = wheelImage.transform.localRotation.eulerAngles.z
        // The angle under the needle is (360 - wheelAngle) % 360 (if segments ordered CCW from top)
        // Let's re-use the angle logic from background/text positioning, which starts from the top (0) and goes CCW.

        float wheelAngle = wheelImage.transform.localRotation.eulerAngles.z;
         // Adjust for the fact that the needle is at the top (0 degrees), and our segment angles
         // are calculated starting from the top and accumulating CCW.
         // A section is under the needle if the needle's angle (0) falls within its slice.
         // Since the wheel rotates, the angle under the needle changes. If the wheel rotates -90 deg,
         // the segment that was at +90 deg is now under the needle.
         // The angle on the wheel that the needle points to is -wheelAngle.
         // Normalize this angle to be positive, within [0, 360). Add 360 and take modulo.
         float needleRelativeAngle = ( -wheelAngle % 360 + 360 ) % 360;


        // Calculate section angles based on weights
        float totalWeight = 0f;
        foreach (WheelSegment segment in currentWheelSegments)
        {
            if(segment != null) totalWeight += segment.weight;
        }
        if (totalWeight <= 0) totalWeight = 1; // Prevent division by zero


        // Find which section contains the needle's angle
        float cumAngle = 0f;
        for (int i = 0; i < numberOfSections; i++)
        {
            if(currentWheelSegments[i] == null) continue; // Skip null segment

            float sectionAngle = (currentWheelSegments[i].weight / totalWeight) * 360f;
             // Check if the needle's angle falls within the current segment's slice [cumAngle, cumAngle + sectionAngle)
             if (needleRelativeAngle >= cumAngle && needleRelativeAngle < cumAngle + sectionAngle)
             {
                 return i; // Found the segment
             }
            cumAngle += sectionAngle;
        }

        // Fallback: If somehow the angle falls exactly on a boundary or loop fails,
        // return the last segment or a default.
        Debug.LogWarning($"CharacterCreator: Needle angle {needleRelativeAngle:F2} did not fall into any segment. Returning last segment index.");
        return numberOfSections - 1; // Or 0, or handle as error
    }


    // Replace the MoveToNextAttribute method with this updated version
    void MoveToNextAttribute()
    {
        currentAttribute++;
        if (currentAttribute >= attributeNames.Length)
        {
            // Character creation is complete
            currentAttribute = attributeNames.Length; // Don't reset to 0, stay at completion state
            if(spinButton != null) spinButton.gameObject.SetActive(false);
            if(nextButton != null) nextButton.gameObject.SetActive(false);

            // Show completion message if we have the text component
            if (currentAttributeText != null)
            {
                currentAttributeText.text = "Complete!";
            }

            // Save character data and proceed to next scene
            SaveCharacterData();
            // StartCoroutine(LoadNextSceneAfterDelay(2.0f)); // Load is now called inside SaveCharacterData
        }
        else
        {
            // Reset wheel rotation for next attribute? Optional.
            if(wheelImage != null) wheelImage.transform.localRotation = Quaternion.identity;

            PositionWheelTexts();
            UpdateAttributeDisplay();
        }
    }

    // Add these methods to the CharacterCreator class
    void SaveCharacterData()
    {
        Debug.Log("Saving character data...");
        // Create a new player data instance
        PlayerData playerData = ScriptableObject.CreateInstance<PlayerData>();

        // Get base stats from wheel selections
        int strengthBase = ParseTextValue(strengthText.text);
        int magicBase = ParseTextValue(magicText.text);
        int defendBase = ParseTextValue(defendText.text);
        int durabilityBase = ParseTextValue(durabilityText.text);
        int speedBase = ParseTextValue(speedText.text);

        // Apply the scaling formula
        playerData.playerName = "Son"; // Default name, consider adding naming later
        playerData.race = raceText.text;
        playerData.trait = traitText.text;
        playerData.maxHp = 100 * (1 + 0.2f * durabilityBase); // Store raw durability value
        playerData.currentHp = playerData.maxHp; // Set current HP to max HP
        playerData.maxMana = 100 * (1 + 0.2f * magicBase); // Mana formula
        playerData.currentMana = playerData.maxMana; // Set current mana to max mana
        playerData.strength = 10 * strengthBase;
        playerData.magic = 10 * magicBase;
        playerData.defense = defendBase; // No scaling for defense? Or maybe 5 * defendBase? Adjust formula as needed.
        playerData.speed = 10 * speedBase;

        // Create initial skills based on race and trait
        AddInitialSkills(playerData);

        // --- NEW GAME LOGIC ---
        // Get the persistent GameData instance via the singleton manager
        GameData gameData = GameDataManager.Instance.gameData;

        if (gameData == null)
        {
             Debug.LogError("GameData is null in CharacterCreator! Cannot save new character.");
             // Handle this error appropriately (e.g., show error message to player, return to menu)
             return;
        }

        // *** IMPORTANT: Reset game progress for a New Game ***
        // This clears old players, resets coin count, event progress, etc.
        gameData.ResetGameData(); // Resetting also calls SaveToPlayerPrefs internally

        // Now add the newly created player to the clean game data
        gameData.AddPlayer(playerData);
        // Save the game data again after adding the player
        gameData.SaveToPlayerPrefs();


        // Save the player data asset to a file (Editor only)
#if UNITY_EDITOR
        string playerDataPath = "Assets/Resources/PlayerData";
        // Make sure the directory exists
        if (!System.IO.Directory.Exists(playerDataPath))
        {
            System.IO.Directory.CreateDirectory(playerDataPath);
        }
        // Create a unique file name for this player
        // Use the player name and add a timestamp for uniqueness if multiple saves per name are possible
        string playerFileName = $"Player_{playerData.playerName}_{System.DateTime.Now.ToString("yyyyMMdd_HHmmss")}";
        // Use the asset name property for the SO asset itself
        playerData.name = playerFileName;
         // Check if an asset with this name already exists before creating? Or rely on timestamp?
         // For a simple single player game, destroying the old PlayerData SO asset before creating the new one might be an option in ResetGameData.
         // For now, we create a new asset each time, which will clutter the Resources folder over time unless manually cleaned.
        UnityEditor.AssetDatabase.CreateAsset(playerData, $"{playerDataPath}/{playerFileName}.asset");
        UnityEditor.AssetDatabase.SaveAssets();
        Debug.Log($"Created new PlayerData asset: {playerFileName}.asset");
#endif

        Debug.Log($"Character data saved to GameData with {gameData.playerDataList.Count} players");
        Debug.Log($"Player stats - HP: {playerData.maxHp}, STR: {playerData.strength}, MAG: {playerData.magic}, " +
                  $"DEF: {playerData.defense}, SPD: {playerData.speed}");

        // Load next scene after saving
        StartCoroutine(LoadNextSceneAfterDelay(2.0f));
    }

    int ParseTextValue(string text)
    {
        int result = 1;
        if (int.TryParse(text, out result))
        {
             return result;
        }
         Debug.LogWarning($"Failed to parse int from text: '{text}'. Returning 1.");
        return 1; // Default to 1 on parse failure
    }

    IEnumerator LoadNextSceneAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);

        // Stop character creation music/sounds if any, and stop any lingering menu music
        if (AudioManager.Instance != null)
        {
             AudioManager.Instance.StopMusic(); // Stop whatever music is currently playing
             // The GameManager in GameScene will start the game music in its Start()
        } else {
             Debug.LogError("AudioManager.Instance is null! Cannot stop music before scene load.");
        }


        UnityEngine.SceneManagement.SceneManager.LoadScene(nextSceneName);
    }

    void DisplayResult(string result)
    {
        // Update the correct text element based on currentAttribute
        switch (currentAttribute) // Subtract 1 because we increment BEFORE displaying
        {
            case 0: if(raceText != null) raceText.text = result; break;
            case 1: if(traitText != null) traitText.text = result; break;
            case 2: if(strengthText != null) strengthText.text = result; break;
            case 3: if(magicText != null) magicText.text = result; break;
            case 4: if(durabilityText != null) durabilityText.text = result; break;
            case 5: if(defendText != null) defendText.text = result; break;
            case 6: if(speedText != null) speedText.text = result; break;
             default: Debug.LogWarning($"CharacterCreator: Attempted to display result for unknown attribute index: {currentAttribute - 1}"); break;
        }
    }

    void PositionWheelTexts()
    {
        // Make sure we have current wheel segments
        if (currentWheelSegments == null || currentWheelSegments.Length == 0)
        {
            UpdateWheelSections();
            if (currentWheelSegments == null || currentWheelSegments.Length == 0) // Check again after updating
            {
                 Debug.LogError("CharacterCreator: Failed to get current wheel segments.");
                 return;
            }
        }

        // Set number of sections based on current segments
        numberOfSections = currentWheelSegments.Length;

        // If no wheel section texts are assigned, create them
        if (wheelSectionTexts == null || wheelSectionTexts.Length != numberOfSections)
        {
            // Clean up old texts if count changed
            if (wheelSectionTexts != null)
            {
                foreach (var textObj in wheelSectionTexts)
                {
                    if (textObj != null) Destroy(textObj.gameObject);
                }
            }
            wheelSectionTexts = new TextMeshProUGUI[numberOfSections];

            // Create text objects for each section
            for (int i = 0; i < numberOfSections; i++)
            {
                GameObject textObj = new GameObject("WheelSection_" + i);
                if (wheelImage.transform != null) textObj.transform.SetParent(wheelImage.transform, false);

                // Add TextMeshProUGUI component
                TextMeshProUGUI tmpText = textObj.AddComponent<TextMeshProUGUI>();
                tmpText.alignment = TextAlignmentOptions.Center;
                tmpText.fontSize = 20;
                 // Load font if not assigned in inspector (optional fallback)
                 if (tmpText.font == null) tmpText.font = Resources.Load<TMP_FontAsset>("Fonts & Materials/VT323-Regular SDF 1");
                tmpText.color = Color.black;
                tmpText.raycastTarget = false; // Prevent UI interactions with text
                tmpText.font = pixelFont;

                wheelSectionTexts[i] = tmpText;
            }
        }


        // Calculate total weight for angle calculation
        float totalWeight = 0f;
        for (int i = 0; i < numberOfSections; i++)
        {
             if(currentWheelSegments[i] != null) totalWeight += currentWheelSegments[i].weight;
        }
         if (totalWeight <= 0) totalWeight = 1; // Prevent division by zero

        // Position the text elements on the wheel based on weights
        float radius = wheelImage != null ? wheelImage.rectTransform.rect.width * 0.4f : 100f; // Default radius if image is null
        float currentAngle = 0f;

        // Position text elements
        for (int i = 0; i < numberOfSections; i++)
        {
             if (currentWheelSegments[i] == null || wheelSectionTexts[i] == null) continue; // Skip null entries


            // Calculate section angle based on weight
            float sectionAngle = currentWheelSegments[i].weight / totalWeight * 360f;

            // Calculate angle for text placement (middle of the section)
            float textAngle = currentAngle + (sectionAngle / 2);
            float radians = textAngle * Mathf.Deg2Rad;

            // Calculate position
            float x = Mathf.Sin(radians) * radius;
            float y = Mathf.Cos(radians) * radius;

            // Position and rotate text
            RectTransform rectTransform = wheelSectionTexts[i].rectTransform;
             if (rectTransform != null)
             {
                 rectTransform.anchoredPosition = new Vector2(x, y);
                 rectTransform.localRotation = Quaternion.Euler(0, 0, -textAngle + 90);
                 rectTransform.sizeDelta = new Vector2(100, 40); // Ensure text box size is reasonable
             }


            // Set text content
            wheelSectionTexts[i].text = currentWheelSegments[i].attribute;

            // Adjust font size based on text length
            if (currentWheelSegments[i].attribute.Length > 6)
            {
                wheelSectionTexts[i].fontSize = 16; // Smaller font for longer text
            }
            else
            {
                wheelSectionTexts[i].fontSize = 20; // Normal size for shorter text
            }

            // Update current angle for next section
            currentAngle += sectionAngle;
        }
    }
    void AddInitialSkills(PlayerData playerData)
    {
         if (playerData == null) return; // Safety check

        // Always add a basic attack (ensure it's not added multiple times if this is called more than once)
        //  if (!playerData.skills.Exists(s => s.name == "Power Strike"))
        //  {
        //     SavedSkill basicAttack = new SavedSkill
        //     {
        //         name = "Power Strike",
        //         multiplier = 1.5f,
        //         statBase = "strength",
        //         manaCost = 5,
        //         targetType = "single",
        //         splashMultiplier = 0
        //     };
        //     playerData.skills.Add(basicAttack);
        //  }


        // Add a race-specific skill
        switch (playerData.race.ToLower())
        {
            case "human":
                if (!playerData.skills.Exists(s => s.name == "Versatile Strike"))
                 playerData.skills.Add(new SavedSkill
                 {
                     name = "Versatile Strike",
                     multiplier = 1.2f,
                     statBase = "strength",
                     manaCost = 8,
                     targetType = "single",
                     splashMultiplier = 0
                 });
                break;

            case "elf":
                 if (!playerData.skills.Exists(s => s.name == "Nature's Wrath"))
                 playerData.skills.Add(new SavedSkill
                 {
                     name = "Nature's Wrath",
                     multiplier = 1.6f,
                     statBase = "magic",
                     manaCost = 12,
                     targetType = "single",
                     splashMultiplier = 0
                 });
                break;

            case "dwarf":
                 if (!playerData.skills.Exists(s => s.name == "Stone Armor"))
                 playerData.skills.Add(new SavedSkill
                 {
                     name = "Stone Armor",
                     multiplier = 1.4f,
                     statBase = "defense",
                     manaCost = 10,
                     targetType = "self",
                     splashMultiplier = 0
                 });
                break;

            case "orc":
                 if (!playerData.skills.Exists(s => s.name == "Crushing Blow"))
                 playerData.skills.Add(new SavedSkill
                 {
                     name = "Crushing Blow",
                     multiplier = 1.8f,
                     statBase = "strength",
                     manaCost = 15,
                     targetType = "single",
                     splashMultiplier = 0
                 });
                break;

            case "gnome":
                 if (!playerData.skills.Exists(s => s.name == "Trickster's Gambit"))
                 playerData.skills.Add(new SavedSkill
                 {
                     name = "Trickster's Gambit",
                     multiplier = 1.3f,
                     statBase = "magic",
                     manaCost = 7,
                     targetType = "splash",
                     splashMultiplier = 0.6f
                 });
                break;
        }

        // Add a trait-specific skill
        switch (playerData.trait.ToLower())
        {
            case "brave":
                 if (!playerData.skills.Exists(s => s.name == "Heroic Charge"))
                 playerData.skills.Add(new SavedSkill
                 {
                     name = "Heroic Charge",
                     multiplier = 1.7f,
                     statBase = "strength",
                     manaCost = 14,
                     targetType = "single",
                     splashMultiplier = 0
                 });
                break;

            case "cunning":
                 if (!playerData.skills.Exists(s => s.name == "Strategic Strike"))
                 playerData.skills.Add(new SavedSkill
                 {
                     name = "Strategic Strike",
                     multiplier = 1.5f,
                     statBase = "strength",
                     manaCost = 10,
                     targetType = "splash",
                     splashMultiplier = 0.5f
                 });
                break;

            case "wise":
                 if (!playerData.skills.Exists(s => s.name == "Arcane Wisdom"))
                 playerData.skills.Add(new SavedSkill
                 {
                     name = "Arcane Wisdom",
                     multiplier = 1.8f,
                     statBase = "magic",
                     manaCost = 18,
                     targetType = "single",
                     splashMultiplier = 0
                 });
                break;

            case "swift":
                 if (!playerData.skills.Exists(s => s.name == "Rapid Strikes"))
                 playerData.skills.Add(new SavedSkill
                 {
                     name = "Rapid Strikes",
                     multiplier = 0.8f,
                     statBase = "speed", // Use speed stat for damage calculation
                     manaCost = 8,
                     targetType = "splash",
                     splashMultiplier = 0.7f
                 });
                break;

            case "strong":
                 if (!playerData.skills.Exists(s => s.name == "Mighty Slam"))
                 playerData.skills.Add(new SavedSkill
                 {
                     name = "Mighty Slam",
                     multiplier = 2.0f,
                     statBase = "strength",
                     manaCost = 20,
                     targetType = "single",
                     splashMultiplier = 0
                 });
                break;
        }
         Debug.Log($"Added {playerData.skills.Count} skills to new player.");
    }
}