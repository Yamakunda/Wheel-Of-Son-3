using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "EnemyWaveConfig", menuName = "Game/Enemy Wave Configuration")]
public class EnemyWaveConfig : ScriptableObject
{
    [Header("Wave Configuration")]
    public string waveName = "New Enemy Wave";
    public List<EnemyData> enemiesInWave = new List<EnemyData>();

    // *** NEW: Reward for defeating this wave ***
    [Header("Rewards")]
    public int goldReward = 0;
    public int experienceReward = 0;
}