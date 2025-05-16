using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "PlayerData", menuName = "Game/EnemyData")]
public class EnemyData : ScriptableObject
{
    // Character information
    public string enemyName;
    public string race ;
    public string trait ;
    public string enemyType;
    public int RewardExp;
    
    // Core stats
    public float maxHp ;
    public float currentHp ;
    public float maxMana ;
    public float currentMana ;
    public float strength ;
    public float magic ;
    public float defense ;
    public float speed ;

    
    public List<SavedSkill> skills = new List<SavedSkill>();
    
    // Helper class for JSON serialization of skills list
    [System.Serializable]
    private class SkillsList
    {
        public List<SavedSkill> skills;
    }
    
    // Initialize a Enemy object with this data
    public void ApplyToPlayer(Enemy enemy)
    {
        if (enemy == null) return;
        
        // Initialize base stats
        enemy.Initialize(enemyName, enemyType, maxHp, strength, magic, defense, speed);
        enemy.Race = race;
        enemy.Trait = trait;
        enemy.CurrentHp = currentHp;
        enemy.Mana = currentMana;
        
        // Additional player-specific properties
        enemy.RewardExp = RewardExp;
        enemy.EnemyType = enemyType;
    }
    
    // Save data from existing Player object
    public void SaveFromEnemy(Enemy enemy)
    {
        if (enemy == null) return;
        
        // Save basic info
        race = enemy.Race;
        trait = enemy.Trait;
        enemyType = enemy.EnemyType;
        RewardExp = enemy.RewardExp;
        
        // Save stats
        maxHp = enemy.MaxHp;
        currentHp = enemy.CurrentHp;
        maxMana = enemy.MaxMana;
        currentMana = enemy.Mana;
        strength = enemy.Strength;
        magic = enemy.Magic;
        defense = enemy.Defense;
        speed = enemy.Speed;
        
        // Save skills - requires modifying the Skills property in Character class
        skills.Clear();
        foreach (var skill in enemy.Skills)
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
        
    }
    public void LogStat()
    {
        Debug.Log($"Enemy name: {enemyName}, HP: {currentHp}/{maxHp}, Mana: {currentMana}/{maxMana}, " +
                  $"Strength: {strength}, Magic: {magic}, Defense: {defense}, Speed: {speed}");
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
