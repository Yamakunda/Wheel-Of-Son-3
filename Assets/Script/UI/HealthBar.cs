using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class HealthBar : MonoBehaviour
{
    public Image healthFill;        // Reference to the foreground fill Image
    public TextMeshProUGUI playerNameText; // Reference to the TextMeshProUGUI for player name
    private float maxHealth = 100f;
    private float currentHealth = 100f;

    void Start()
    {
        UpdateHealthBar();
    }

    public void TakeDamage(float amount)
    {
        currentHealth -= amount;
        currentHealth = Mathf.Clamp(currentHealth, 0, maxHealth);
        UpdateHealthBar();
    }
    public void SetCurrentHealth(float amount)
    {
        currentHealth = amount;
        currentHealth = Mathf.Clamp(currentHealth, 0, maxHealth);
        UpdateHealthBar();
    }
    public void SetMaxHealth(float amount)
    {
        maxHealth = amount;
        maxHealth = Mathf.Clamp(maxHealth, 0, float.MaxValue);
        UpdateHealthBar();
    }
    public void SetNameText(string name)
    {
        if (playerNameText != null)
        {
            playerNameText.text = name; // Set the player name text
        }
    }

    public void UpdateHealthBar()
    {
        healthFill.fillAmount = currentHealth / maxHealth; // Update the fill amount based on current health
    }
}