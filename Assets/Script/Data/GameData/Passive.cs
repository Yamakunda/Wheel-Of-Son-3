using UnityEngine;
using System.Collections.Generic;

// Define battle stages where passive effects might apply
public enum PassiveEffectType
{
    DamageDealt,          // Modify damage dealt by character
    DamageTaken,          // Modify damage received by character
    HealingReceived,      // Modify healing received by character
    HealingDealt,         // Modify healing done by character
    ManaCost,             // Modify skill mana costs
    ManaRegen,            // Modify mana regeneration rate
    StatBoost,            // Modify base stats (strength, magic, etc.)
    SkillDamage,          // Modify damage of specific skills
    SkillHealing,         // Modify healing of specific skills
    LifeSteal,            // Convert portion of damage to healing
    ManaSteal,            // Convert portion of damage to mana
    DamageReflection,     // Reflect damage back to attacker
    OnTurnStart,          // Trigger at start of character's turn
    OnTurnEnd,            // Trigger at end of character's turn
    OnBattleStart,        // Trigger at start of battle
    OnBattleEnd,          // Trigger at end of battle
    OnHealthThreshold,    // Trigger when health reaches certain percentage
    OnSkillUsed,          // Trigger after using any skill
    OnAttacked,           // Trigger when character is attacked
    FieldCondition        // Trigger based on field conditions
}

// Define teams for battlefield context
public enum Team
{
    Player,
    Enemy,
    Ally,   // NPCs fighting with player
    Neutral // Non-combatants or environmental objects
}

public abstract class Passive
{
    public string Name { get; protected set; }
    public string Description { get; protected set; }
    public bool Active { get; set; } = true;
    public List<PassiveEffectType> EffectTypes { get; protected set; } = new List<PassiveEffectType>();
    
    // Properties to control passive behavior
    public int Priority { get; protected set; } = 0;  // Higher values get processed first
    public float ChanceToTrigger { get; protected set; } = 100f;  // Percentage (0-100)
    public float CooldownTime { get; protected set; } = 0f;  // Seconds until usable again
    public float CurrentCooldown { get; protected set; } = 0f;
    public int MaxTriggerCount { get; protected set; } = -1;  // -1 means unlimited
    public int TriggerCount { get; protected set; } = 0;
    
    // Constructor
    protected Passive(string name, string description)
    {
        Name = name;
        Description = description;
    }
    
    // Check if this passive can be triggered
    public bool CanTrigger(PassiveEffectType effectType)
    {
        // Check if passive is active, not on cooldown, and supports this effect type
        if (!Active || CurrentCooldown > 0 || !EffectTypes.Contains(effectType))
            return false;
        
        // Check trigger count if limited
        if (MaxTriggerCount > 0 && TriggerCount >= MaxTriggerCount)
            return false;
            
        // Check random chance
        if (ChanceToTrigger < 100f && Random.Range(0f, 100f) > ChanceToTrigger)
            return false;
            
        return true;
    }
    
    // Update cooldown - call this every frame or fixed interval
    public void UpdateCooldown(float deltaTime)
    {
        if (CurrentCooldown > 0)
            CurrentCooldown = Mathf.Max(0, CurrentCooldown - deltaTime);
    }
    
    // Reset trigger counter
    public void ResetTriggerCount()
    {
        TriggerCount = 0;
    }

    public override string ToString()
    {
        return $"{Name}: {Description}";
    }
}