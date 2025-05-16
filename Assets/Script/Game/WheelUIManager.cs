using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine.Events;

public class WheelUIManager : MonoBehaviour
{
    [Header("Wheel UI")]
    public Image wheelImage;
    public Sprite wheelSprite;
    public Button spinButton;
    public Button nextButton;
    public RectTransform needlePointer;
    public TextMeshProUGUI eventDescriptionText;
    public TextMeshProUGUI currentEventText;
    private TextMeshProUGUI[] wheelSectionTexts;
    private GameObject[] wheelBackgrounds;
    private int numberOfSections = 0;
    [SerializeField] private TMP_FontAsset fontAsset;
    [Header("Wheel Configuration")]
    [SerializeField] private float minSpinTime = 2.5f;
    [SerializeField] private float maxSpinTime = 4.0f;

    [Header("Audio")]
    public AudioSource audioSource;
    public AudioClip spinSound;
    public AudioClip resultSound;

    // Events
    public UnityEvent onSpinStart;
    public UnityEvent onSpinEnd;
    public UnityEvent<GameEventSegment> onEventSelected;

    // State
    private bool isSpinning = false;
    private List<GameEventSegment> gameEvents;

    public void Initialize(List<GameEventSegment> events)
    {
        gameEvents = events;
        numberOfSections = events.Count;
        Debug.Log($"Initializing Wheel UI with {numberOfSections} sections.");
        Debug.Assert(numberOfSections > 0, "No game events provided for the wheel.");
        // Set up button listeners
        if (spinButton != null)
            spinButton.onClick.AddListener(SpinWheel);

        if (nextButton != null)
            nextButton.gameObject.SetActive(false);

        // Position the needle
        if (needlePointer != null)
        {
            float wheelRadius = wheelImage.rectTransform.rect.width / 2f;
            needlePointer.anchoredPosition = new Vector2(0, wheelRadius);
            needlePointer.localRotation = Quaternion.Euler(0, 0, 0);
        }

        // Create and position wheel section texts
        PositionWheelTexts();
        UpdateWheelSections();
    }

    public void UpdateEventCounter(int current, int max)
    {
        if (currentEventText != null)
            currentEventText.text = $"Event {current}/{max}";
    }

    public void SetDescriptionText(string description)
    {
        if (eventDescriptionText != null)
            eventDescriptionText.text = description;
    }

    public void EnableNextButton(bool enable)
    {
        if (nextButton != null)
            nextButton.gameObject.SetActive(enable);
    }

    public void EnableSpinButton(bool enable)
    {
        if (spinButton != null)
            spinButton.interactable = enable;
    }

    public void SpinWheel()
    {
        if (!isSpinning)
        {
            onSpinStart?.Invoke();
            StartCoroutine(SpinCoroutine());
        }
    }

    private IEnumerator SpinCoroutine()
    {
        isSpinning = true;
        EnableSpinButton(false);
        
        // Play spin sound
        if (audioSource && spinSound)
            audioSource.PlayOneShot(spinSound);

        // Generate random parameters for the spin
        float spinTime = Random.Range(minSpinTime, maxSpinTime);
        float maxSpinSpeed = Random.Range(1080f, 1440f);
        float spinDeceleration = maxSpinSpeed / spinTime;

        float currentSpeed = maxSpinSpeed;
        float totalRotation = 0f;
        float elapsed = 0f;
        float startRotationZ = wheelImage.transform.localRotation.eulerAngles.z;

        // Spin the wheel with decreasing speed
        while (elapsed < spinTime)
        {
            currentSpeed = maxSpinSpeed - (spinDeceleration * elapsed);
            float rotationThisFrame = currentSpeed * Time.deltaTime;
            totalRotation += rotationThisFrame;

            wheelImage.transform.localRotation = Quaternion.Euler(0, 0, startRotationZ - totalRotation);

            elapsed += Time.deltaTime;
            yield return null;
        }
        
        // Play result sound
        if (audioSource && resultSound)
            audioSource.PlayOneShot(resultSound);

        // Calculate result and notify
        int resultIndex = CalculateResultSection();
        GameEventSegment result = gameEvents[resultIndex];
        
        // Display result
        if (eventDescriptionText != null)
            eventDescriptionText.text = result.description;
            
        Debug.Log($"Event wheel landed on: {result.eventName} ({result.eventType})");

        onSpinEnd?.Invoke();
        onEventSelected?.Invoke(result);
        
        EnableNextButton(true);
        EnableSpinButton(true);
        isSpinning = false;
    }

    private int CalculateResultSection()
    {
        float currentRotationAngle = wheelImage.transform.localRotation.eulerAngles.z;
        float normalizedAngle = currentRotationAngle;

        // Calculate total weight
        float totalWeight = 0f;
        foreach (var evt in gameEvents)
            totalWeight += evt.weight;

        // Calculate the angle of each section based on weights
        float[] sectionAngles = new float[numberOfSections];
        for (int i = 0; i < numberOfSections; i++)
            sectionAngles[i] = (gameEvents[i].weight / totalWeight) * 360f;

        // Find which section contains the current angle
        float adjustedAngle = (normalizedAngle + 360) % 360;
        float cumAngle = 0f;
        
        for (int i = 0; i < numberOfSections; i++)
        {
            cumAngle += sectionAngles[i];
            if (adjustedAngle < cumAngle)
                return i;
        }

        return 0;
    }

    private void PositionWheelTexts()
    {
        // Implementation remains similar to the original PositionWheelTexts method
        // Create and position text elements for each wheel section
        
        // Cleanup old text objects
        if (wheelSectionTexts != null)
        {
            foreach (var text in wheelSectionTexts)
            {
                if (text != null)
                    Destroy(text.gameObject);
            }
        }

        wheelSectionTexts = new TextMeshProUGUI[numberOfSections];

        // Create text objects for each section
        for (int i = 0; i < numberOfSections; i++)
        {
            GameObject textObj = new GameObject("WheelSection_" + i);
            textObj.transform.SetParent(wheelImage.transform, false);

            // Add TextMeshProUGUI component
            TextMeshProUGUI tmpText = textObj.AddComponent<TextMeshProUGUI>();
            tmpText.alignment = TextAlignmentOptions.Center;
            tmpText.fontSize = 18;
            tmpText.color = Color.black;
            tmpText.raycastTarget = false;
            tmpText.font = fontAsset;

            wheelSectionTexts[i] = tmpText;
        }

        // Position text elements - will be fully positioned in UpdateSegmentBackgrounds
    }

    private void UpdateWheelSections()
    {
        if (wheelSectionTexts == null || wheelSectionTexts.Length != numberOfSections)
            PositionWheelTexts();
        else
            UpdateSegmentBackgrounds();
    }

    private void UpdateSegmentBackgrounds()
    {
        // Implementation remains similar to original UpdateSegmentBackgrounds
        // Creates colored segments and positions text elements
        
        float wheelRadius = wheelImage.rectTransform.rect.width / 2f;

        // Clean up old backgrounds if needed
        if (wheelBackgrounds != null && wheelBackgrounds.Length != numberOfSections)
        {
            foreach (var bgObj in wheelBackgrounds)
            {
                if (bgObj != null)
                    Destroy(bgObj);
            }
            wheelBackgrounds = null;
        }

        // Create new background array if needed
        if (wheelBackgrounds == null)
            wheelBackgrounds = new GameObject[numberOfSections];

        // Calculate total weight
        float totalWeight = 0f;
        for (int i = 0; i < numberOfSections; i++)
            totalWeight += gameEvents[i].weight;

        float currentAngle = 0;

        // Create/update each background segment
        for (int i = 0; i < numberOfSections; i++)
        {
            float sectionAngle = (gameEvents[i].weight / totalWeight) * 360f;

            // Create or get the background GameObject
            if (wheelBackgrounds[i] == null)
            {
                wheelBackgrounds[i] = new GameObject("WheelBackground_" + i);
                wheelBackgrounds[i].transform.SetParent(wheelImage.transform, false);
                wheelBackgrounds[i].transform.SetAsFirstSibling();
            }

            // Create or update the pie-shaped segment
            Image image = wheelBackgrounds[i].GetComponent<Image>();
            if (image == null)
            {
                image = wheelBackgrounds[i].AddComponent<Image>();
                image.raycastTarget = false;
            }

            image.sprite = wheelSprite;
            image.color = gameEvents[i].segmentColor;
            image.type = Image.Type.Filled;
            image.fillMethod = Image.FillMethod.Radial360;
            image.fillOrigin = 2;
            image.fillClockwise = true;
            image.fillAmount = sectionAngle / 360f;

            // Position and size the background
            RectTransform bgRect = wheelBackgrounds[i].GetComponent<RectTransform>();
            bgRect.anchoredPosition = Vector2.zero;
            bgRect.sizeDelta = new Vector2(wheelRadius * 2f, wheelRadius * 2f);
            bgRect.localRotation = Quaternion.Euler(0, 0, -currentAngle);

            // Update text position and content
            if (wheelSectionTexts != null && i < wheelSectionTexts.Length && wheelSectionTexts[i] != null)
            {
                float textAngle = currentAngle + (sectionAngle / 2);
                float radians = textAngle * Mathf.Deg2Rad;
                float textRadius = wheelRadius * 0.7f;
                float x = Mathf.Sin(radians) * textRadius;
                float y = Mathf.Cos(radians) * textRadius;

                RectTransform rectTransform = wheelSectionTexts[i].rectTransform;
                rectTransform.anchoredPosition = new Vector2(x, y);
                rectTransform.localRotation = Quaternion.Euler(0, 0, -textAngle + 90);
                wheelSectionTexts[i].text = gameEvents[i].eventName;
                
                // Adjust font size based on text length
                wheelSectionTexts[i].fontSize = gameEvents[i].eventName.Length > 6 ? 16 : 18;
            }

            // Update current angle for next segment
            currentAngle += sectionAngle;
        }
    }

    public void ShowWheel(bool show)
    {
        if (wheelImage != null)
            wheelImage.gameObject.SetActive(show);
    }
  public void OnDestroy()
  {
    Debug.Log("Wheel UI Manager destroyed + " + gameObject.GetInstanceID());
  }
}