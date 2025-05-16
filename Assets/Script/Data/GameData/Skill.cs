using UnityEngine;

public class Skill
{
    public string Name { get; private set; }
    public float DamageMultiplier { get; private set; }
    public string StatType { get; private set; } // "strength" or "magic"
    public int ManaCost { get; private set; }
    public string Targets { get; private set; } // "single", "all", or "splash"
    public float SplashMultiplier { get; set; } = 0.5f; // Damage multiplier for secondary targets if splash

    public Skill(string name, float damageMultiplier, string statType, int manaCost = 0, string targets = "single")
    {
        Name = name;
        DamageMultiplier = damageMultiplier;
        StatType = statType;
        ManaCost = manaCost;
        Targets = targets;
    }

    public int GetDamage(Character character, bool isPrimaryTarget = true)
    {
        float baseStat = StatType == "strength" ? character.Strength : character.Magic;

        if (isPrimaryTarget || Targets == "all")
        {
            return (int)(baseStat * DamageMultiplier);
        }
        else
        {
            return (int)(baseStat * DamageMultiplier * SplashMultiplier);
        }
    }

    public override string ToString()
    {
        string targetType = "";
        if (Targets == "all")
            targetType = " (AOE)";
        else if (Targets == "splash")
            targetType = $" (Splash {(int)(SplashMultiplier * 100)}%)";

        return $"{Name} ({DamageMultiplier}x {StatType}{targetType} - {ManaCost} MP)";
    }
}