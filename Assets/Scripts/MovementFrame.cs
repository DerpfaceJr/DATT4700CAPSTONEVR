using UnityEngine;

/// <summary>
/// Data structure for storing a single frame of player movement
/// </summary>
[System.Serializable]
public class MovementFrame
{
    public float timestamp;
    public Vector3 position;
    public Quaternion rotation;
    
    public MovementFrame(float time, Vector3 pos, Quaternion rot)
    {
        timestamp = time;
        position = pos;
        rotation = rot;
    }
}

