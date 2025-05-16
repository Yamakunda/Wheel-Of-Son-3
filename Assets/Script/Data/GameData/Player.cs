using UnityEngine;
using System.Collections.Generic;

public class Player : Character
{
    public string PlayerName { get; private set; }
    public int Level { get; set; }
    public int Experience { get; set; }
    

    public void Initialize(string name, float maxHp, float maxMana, float strength, float magic, float def,float speed)
    {
        base.Initialize(name, maxHp, maxMana, strength, magic, def, speed);
        PlayerName = name;
        Level = 1;
        Experience = 0;
        InitializeStarterSkills();
    }

    private void InitializeStarterSkills()
    {
        // AddSkill(new Skill("Fireball", 1.8f, "magic", 15, "all"));
    }

    // On develop
    public void GainExperience(int amount)
    {
        Experience += amount;
        if (Experience >= Level * 100)
        {
            LevelUp();
        }
    }

    private void LevelUp()
    {
        Level++;
        MaxHp += 10;
        CurrentHp = MaxHp;
        Strength += 2;
        Magic += 2;
        Speed += 1;
        MaxMana += 10;
        Mana = MaxMana;
        Debug.Log($"{PlayerName} leveled up to level {Level}!");
    }
}