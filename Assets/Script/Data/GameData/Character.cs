using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;
public class Character : MonoBehaviour
{
    // Character base stats
    public string Name { get; set; }
    public string Race { get; set; }
    public string Trait { get; set; }
    public float MaxHp { get; set; }
    public float CurrentHp { get; set; }
    public float MaxMana { get; set; }
    public float Mana { get; set; }
    public float Strength { get; set; }
    public float Magic { get; set; }
    public float Defense { get; set; }
    public float Speed { get; set; }
    public List<Skill> Skills { get; private set; } = new List<Skill>();
    public List<Passive> Passives { get; private set; } = new List<Passive>();
    public Dictionary<string, float> BaseStats { get; private set; }
    public CharacterAnimator characterAnimator;
    private HealthBar hpBar;

    public virtual void Awake()
    {
        characterAnimator = GetComponent<CharacterAnimator>();
        BaseStats = new Dictionary<string, float>();
    }

    public void Initialize(string name, float maxHp, float maxMana, float strength, float magic, float defense, float speed, string race = "", string trait = "")
    {
        // fix future
        Name = name;
        Race = race;
        Trait = trait;
        MaxHp = maxHp;
        CurrentHp = maxHp;
        MaxMana = maxMana;
        Mana = maxMana;
        Strength = strength;
        Magic = magic;
        Defense = defense;
        Speed = speed;
        BaseStats = new Dictionary<string, float>
        {
            { "max_hp", maxHp }, { "max_mana", maxMana }, { "strength", strength },
            { "magic", magic }, {"defence", defense}, { "speed", speed }
        };
    }
    public void SetHpBar(HealthBar hpBar)
    {
        this.hpBar = hpBar;
        if (hpBar != null)
        {
            hpBar.SetMaxHealth(MaxHp);
            hpBar.SetCurrentHealth(CurrentHp);
            hpBar.UpdateHealthBar(); // Update the health bar to reflect the current health
            hpBar.SetNameText(Name) ; // Set the player name text
            hpBar.transform.SetParent(transform); // Set the health bar as a child of the character
            hpBar.transform.localPosition = new Vector3(0, 1f, 0); // Adjust position as needed
        }
    }

    public float TakeTrueDamage(float damage)
    {
        CurrentHp = Mathf.Max(0, CurrentHp - damage);
        hpBar?.SetCurrentHealth(CurrentHp);
        return CurrentHp;
    }
    public float TakeDamageWithDef(float damage)
    {
        damage = Mathf.Max(damage / 10, damage - Defense);
        CurrentHp = Mathf.Max(0, CurrentHp - damage);
        return CurrentHp;
    }
    public float Heal(float amount)
    {
        CurrentHp = Mathf.Min(MaxHp, CurrentHp + amount);
        return CurrentHp;
    }
    public bool UseMana(float amount)
    {
        if (Mana >= amount)
        {
            Mana -= amount;
            return true;
        }
        return false;
    }

    public void RestoreMana(float amount)
    {
        Mana = Mathf.Min(MaxMana, Mana + amount);
    }
    public bool IsAlive() => CurrentHp > 0;

    public void AddSkill(Skill skill) => Skills.Add(skill);

    public void AddPassive(Passive passive) => Passives.Add(passive);

    public IEnumerator PlayAttackAnimation()
    {
        if (characterAnimator != null)
        {
            yield return StartCoroutine(characterAnimator.PlayAttackAnimation());
        }
        else
        {
            yield return null;
        }
    }
    public bool IsInitialized()
    {
        // A character is considered initialized if it has base stats and a name
        return BaseStats != null && BaseStats.Count > 0 && !string.IsNullOrEmpty(Name);
    }
    public void modifyStats(string stat, float value)
    {
        BaseStats[stat] += value;
    }
    public void addSkill(Skill skill)
    {
        if (skill != null && !Skills.Contains(skill))
        {
            Skills.Add(skill);
        }
    }
    public void logStat()
    {
        Debug.Log($"Name: {Name}, Current HP: {CurrentHp}/{MaxHp}, Skill Count: {Skills.Count}, Passive Count: {Passives.Count}");
    }
}