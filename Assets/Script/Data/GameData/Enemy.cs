// --- START OF FILE Enemy.cs (MODIFIED) ---

using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public class Enemy : Character
{
    public string EnemyType { get; set; }
    public int RewardExp { get; set; }

    public void Initialize(string name, string enemyType, float maxHp, float strength, float magic, float def, float speed)
    {
        base.Initialize(name, maxHp, 0, strength, magic, def, speed);
        EnemyType = enemyType;
        RewardExp = System.Convert.ToInt32(maxHp) / 2; // Basic calculation for reward
        // Initialize skills based on EnemyData or enemyType later
        InitializeSkills(); // For now, add a default attack
    }

    // This method will be called during initialization or loaded from data
    private void InitializeSkills()
    {
        // For now, enemies just get a basic attack.
        // In a full game, you would load these from EnemyData or define them based on enemyType
        if (Skills.Count == 0) // Avoid adding duplicates if loading from data
        {
             AddSkill(new Skill("Basic Attack", 1.0f, "strength", 0)); // Simple attack
        }

        // Example: Add a magic skill if it's a specific enemy type (e.g., "Goblin Mage")
        // if (EnemyType == "Goblin Mage")
        // {
        //      AddSkill(new Skill("Firebolt", 1.5f, "magic", 5, "single"));
        // }
         // Example: Add a multi-target skill
        // if (EnemyType == "Goblin Shaman")
        // {
        //      AddSkill(new Skill("Weakening Totem", 0.5f, "magic", 8, "all")); // Example: applies debuff (need to implement effects)
        // }
    }

    public float GetAttackDamage()
    {
         // This was a simple damage calculation, now skills handle this.
         // Consider if enemies need a base auto-attack damage separate from skills.
         // For now, skills determine damage.
         Debug.LogWarning("GetAttackDamage is deprecated. Use Skill.GetDamage instead.");
         return 0; // Or return a default value
    }

    public int GetReward() => RewardExp;


    // --- AI Logic for Skill Selection ---
    public Skill ChooseSkill()
    {
        // Filter skills based on current mana
        var availableSkills = Skills.Where(s => Mana >= s.ManaCost).ToList();

        // If no skills can be used (e.g., not enough mana), use the first skill if possible (assuming it's a basic attack with 0 mana cost)
        if (availableSkills.Count == 0)
        {
            Debug.Log($"{Name} has no available skills based on mana.");
             // Try to find a 0-cost skill as a fallback
             Skill fallbackSkill = Skills.Find(s => s.ManaCost == 0);
             if (fallbackSkill != null)
             {
                 Debug.Log($"{Name} using fallback 0-cost skill: {fallbackSkill.Name}");
                 return fallbackSkill;
             }

            Debug.LogWarning($"{Name} has no available skills and no 0-cost fallback. Skipping turn?");
            return null; // Indicate no skill can be chosen
        }

        // --- Basic AI Decision Making ---

        // Prioritize skills based on different factors:

        // 1. High Damage Skills (Single Target) if a strong target is available?
        // 2. AOE Skills if multiple targets are available?
        // 3. Healing/Supportive Skills (if implemented)?
        // 4. Low Mana Cost Skills for efficiency?
        // 5. Random selection otherwise?

        // --- Example Basic AI (can be expanded): ---

        // Option A: Prioritize high-mana, high-damage skills if enough mana
        // availableSkills = availableSkills.OrderByDescending(s => s.ManaCost).ToList(); // Prioritize skills that cost more (often stronger)
        // availableSkills = availableSkills.OrderByDescending(s => s.GetDamage(this)).ToList(); // Prioritize skills that deal more damage

        // Option B: Simple Weighted Random Selection (similar to character creator)
        float totalWeight = 0;
        foreach (var skill in availableSkills)
        {
            // Assign weights - could be based on damage, utility, mana cost ratio, etc.
            // For a basic example, let's just give all available skills a weight of 1
            totalWeight += 1;
            // More complex: totalWeight += skill.GetDamage(this) / skill.ManaCost; // Value per mana
            // More complex: totalWeight += (skill.Targets == "all" || skill.Targets == "splash") ? 2 : 1; // Prioritize AOE
        }

        float randomWeight = Random.Range(0f, totalWeight);
        float cumulativeWeight = 0f;
        foreach (var skill in availableSkills)
        {
            cumulativeWeight += 1; // Use the same simple weight
            if (randomWeight < cumulativeWeight)
            {
                 Debug.Log($"{Name} chose skill (Random): {skill.Name}");
                return skill;
            }
        }

        // Fallback in case the random selection loop somehow fails
        return availableSkills[0];

        // Option C: Simple Random Selection (if weights are not needed)
        // int randomIndex = Random.Range(0, availableSkills.Count);
        // Debug.Log($"{Name} chose skill (Simple Random): {availableSkills[randomIndex].Name}");
        // return availableSkills[randomIndex];
    }

    // --- AI Logic for Target Selection ---
    // This method helps the BattleManager determine valid targets for the chosen skill.
    // It doesn't return the *final* list directly, but rather potential targets based on AI intent.
    public List<Character> ChooseTargets(Skill skill, List<Player> allPlayers, List<Enemy> allEnemies)
    {
        List<Character> chosenTargets = new List<Character>();

        if (skill == null) return chosenTargets;

        // Filter living characters
        List<Player> alivePlayers = allPlayers.Where(p => p != null && p.IsAlive()).ToList();
        List<Enemy> aliveEnemies = allEnemies.Where(e => e != null && e.IsAlive()).ToList();


        switch (skill.Targets.ToLower())
        {
            case "single":
                 // AI for single target: Target a random living player
                 if (alivePlayers.Count > 0)
                 {
                     Player targetPlayer = alivePlayers[Random.Range(0, alivePlayers.Count)];
                     chosenTargets.Add(targetPlayer);
                 } else {
                      Debug.LogWarning($"{Name} trying to target single player, but none are alive.");
                 }
                 break;

            case "splash":
                 // AI for splash target: Target a random living player (the primary target)
                 if (alivePlayers.Count > 0)
                 {
                     Player targetPlayer = alivePlayers[Random.Range(0, alivePlayers.Count)];
                     chosenTargets.Add(targetPlayer);
                     // BattleManager's ProcessActionCoroutine will handle finding adjacent targets for splash
                 } else {
                      Debug.LogWarning($"{Name} trying to target splash on player, but none are alive.");
                 }
                 break;

            case "all":
                 // AI for AOE: Target all living players
                 chosenTargets.AddRange(alivePlayers);
                 if (chosenTargets.Count == 0)
                 {
                     Debug.LogWarning($"{Name} trying to target all players, but none are alive.");
                 }
                 break;

            case "self":
                 // AI for self-targeting: Target itself
                 chosenTargets.Add(this);
                 break;

            // TODO: Add AI logic for other target types like "ally", "all_allies", "all_characters"

            default:
                 Debug.LogWarning($"AI does not have logic for skill target type: {skill.Targets}");
                 break;
        }

         // Ensure chosen targets are valid (alive and not null) before returning
         return chosenTargets.Where(t => t != null && t.IsAlive()).ToList();
    }
}
// --- END OF FILE Enemy.cs (MODIFIED) ---