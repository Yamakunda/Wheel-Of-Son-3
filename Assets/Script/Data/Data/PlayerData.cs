using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "PlayerData", menuName = "Game/PlayerData")]
public class PlayerData : ScriptableObject
{
    // Character information
    public string playerName;
    public string race;
    public string trait;
    public int level = 1;
    public int experience = 0;

    // Core stats
    public float maxHp;
    public float currentHp;
    public float maxMana;
    public float currentMana;
    public float strength;
    public float magic;
    public float defense;
    public float speed;

    public List<SavedSkill> skills = new List<SavedSkill>();

    // Methods to save/load from PlayerPrefs
    public void SaveToPlayerPrefs()
    {
        // Convert entire object to JSON
        string jsonData = JsonUtility.ToJson(this);
        PlayerPrefs.SetString("PlayerData", jsonData);

        // Also save individual fields for backward compatibility
        PlayerPrefs.SetString("PlayerName", playerName);
        PlayerPrefs.SetString("Race", race);
        PlayerPrefs.SetString("Trait", trait);
        PlayerPrefs.SetInt("PlayerLevel", level);
        PlayerPrefs.SetInt("PlayerExperience", experience);
        PlayerPrefs.SetFloat("PlayerMaxHP", maxHp);
        PlayerPrefs.SetFloat("PlayerCurrentHP", currentHp);
        PlayerPrefs.SetFloat("PlayerMaxMana", maxMana);
        PlayerPrefs.SetFloat("PlayerCurrentMana", currentMana);
        PlayerPrefs.SetFloat("PlayerStrength", strength);
        PlayerPrefs.SetFloat("PlayerMagic", magic);
        PlayerPrefs.SetFloat("PlayerDefense", defense);
        PlayerPrefs.SetFloat("PlayerSpeed", speed);

        // Save skills as JSON (if we update the skills system)
        if (skills.Count > 0)
        {   
            string skillsJson = JsonUtility.ToJson(new SkillsList { skills = skills });
            PlayerPrefs.SetString("PlayerSkills", skillsJson);
            Debug.Log($"Saved skills JSON: {skillsJson}");
        }
        Debug.Log("Skill count: " + skills.Count);

        PlayerPrefs.Save();
        Debug.Log($"Player data saved as JSON: {jsonData}");
    }

    public void LoadFromPlayerPrefs()
    {
        // Check if player data exists in PlayerPrefs
        if (!PlayerPrefs.HasKey("PlayerData"))
        {
            Debug.Log("No PlayerData found in PlayerPrefs. Using default values.");
            return;
        }
        try
        {
            // Get the JSON string from PlayerPrefs
            string jsonData = PlayerPrefs.GetString("PlayerData");
            Debug.Log($"Loading PlayerData from JSON: {jsonData}");

            // Attempt to parse the JSON data into this object
            JsonUtility.FromJsonOverwrite(jsonData, this);
            
            if (PlayerPrefs.HasKey("PlayerSkills"))
            {
                string skillsJson = PlayerPrefs.GetString("PlayerSkills");
                SkillsList loadedSkills = JsonUtility.FromJson<SkillsList>(skillsJson);
                skills = loadedSkills.skills;
                Debug.Log($"Loaded {skills.Count} skills from PlayerPrefs.");
            }
            else
            {
                Debug.LogWarning("No skills data found in PlayerPrefs. Using default values.");
            }
  

            Debug.Log($"Player data successfully loaded: {playerName}, Race: {race}, Trait: {trait}");
            Debug.Log($"Stats - HP: {currentHp}/{maxHp}, Mana: {currentMana}/{maxMana}, " +
                     $"STR: {strength}, MAG: {magic}, DEF: {defense}, SPD: {speed}, Skills: {skills.Count}");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Error loading player data: {e.Message}");

        }
    }

    // Helper class for JSON serialization of skills list
    [System.Serializable]
    private class SkillsList
    {
        public List<SavedSkill> skills;
    }

    // Initialize a Player object with this data
    public void ApplyToPlayer(Player player)
    {
        if (player == null) return;

        // Initialize base stats
        player.Initialize(playerName, maxHp, maxMana, strength, magic, defense, speed);
        player.Race = race;
        player.Trait = trait;
        player.CurrentHp = currentHp;
        player.Mana = currentMana;

        // Additional player-specific properties
        player.Level = level;
        player.Experience = experience;

        foreach (var skill in skills)
        {
            Skill newSkill = new Skill(skill.name, skill.multiplier, skill.statBase, (int)skill.manaCost, skill.targetType);
            newSkill.SplashMultiplier = skill.splashMultiplier;
            player.AddSkill(newSkill);
        }

    }

    // Save data from existing Player object
    public void SaveFromPlayer(Player player)
    {
        if (player == null) return;

        // Save basic info
        playerName = player.PlayerName;
        race = player.Race;
        trait = player.Trait;
        level = player.Level;
        experience = player.Experience;

        // Save stats
        maxHp = player.MaxHp;
        currentHp = player.CurrentHp;
        maxMana = player.MaxMana;
        currentMana = player.Mana;
        strength = player.Strength;
        magic = player.Magic;
        defense = player.Defense;
        speed = player.Speed;

        // Save skills - requires modifying the Skills property in Character class
        skills.Clear();
        foreach (var skill in player.Skills)
        {
            SavedSkill savedSkill = new SavedSkill
            {
                name = skill.Name,
                multiplier = skill.DamageMultiplier,
                statBase = skill.StatType,
                manaCost = skill.ManaCost,
                targetType = skill.Targets,
                splashMultiplier = skill.SplashMultiplier
            };
            skills.Add(savedSkill);
        }

        Debug.Log($"Saved player data from {playerName}");
    }
    public void ResetPlayerData()
    {

        PlayerPrefs.SetInt("PlayerLevel", 1);
        PlayerPrefs.SetInt("PlayerExperience", 0);
        // Save stats
        PlayerPrefs.SetFloat("PlayerCurrentHP", maxHp);
        PlayerPrefs.SetFloat("PlayerCurrentMana", maxMana);
        PlayerPrefs.Save();

    }
    public void LogStat()
    {
        Debug.Log($"Player Name: {playerName}, Level: {level}, HP: {currentHp}/{maxHp}, Mana: {currentMana}/{maxMana}, " +
                  $"Strength: {strength}, Magic: {magic}, Defense: {defense}, Speed: {speed}, Skills: {skills.Count}");
    }
    public void ApplyStatBoost(string statName, float amount)
    {
        switch (statName.ToLower())
        {
            case "hp":
                maxHp += amount;
                currentHp = Mathf.Min(currentHp, maxHp); // Ensure current HP doesn't exceed max HP
                break;
            case "mana":
                maxMana += amount;
                currentMana = Mathf.Min(currentMana, maxMana); // Ensure current Mana doesn't exceed max Mana
                break;
            case "strength":
                strength += amount;
                break;
            case "magic":
                magic += amount;
                break;
            case "defense":
                defense += amount;
                break;
            case "speed":
                speed += amount;
                break;
            default:
                Debug.LogWarning($"Unknown stat: {statName}");
                break;
        }
    }
    public void ApplyTrueDamage(float damage)
    {
        currentHp -= damage;
        if (currentHp < 0) currentHp = 0;
    }
}
[System.Serializable]
public class SavedSkill
{
    public string name;
    public float multiplier;
    public string statBase;
    public float manaCost;
    public string targetType;
    public float splashMultiplier;
}