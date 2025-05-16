using UnityEngine;
[System.Serializable]
public class GameEventSegment
{
    public string eventName;
    public string description;
    public float weight = 1f;
    public Color segmentColor = Color.white;
    public GameEventType eventType; // Additional properties based on event type
    // *** NEW: Reference to Enemy Wave Config for battle events ***
    public EnemyWaveConfig waveConfig;
    public RewardData reward;
}