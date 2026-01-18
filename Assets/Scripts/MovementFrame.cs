using UnityEngine;

/// <summary>
/// Data structure for storing a single frame of player movement
/// Includes IK target data for rigged character animations
/// </summary>
[System.Serializable]
public class MovementFrame
{
    public float timestamp;
    public Vector3 position;
    public Quaternion rotation;
    
    // IK Target data for rigged animations
    public Vector3 headPosition;
    public Quaternion headRotation;
    public Vector3 leftHandPosition;
    public Quaternion leftHandRotation;
    public Vector3 rightHandPosition;
    public Quaternion rightHandRotation;
    
    // Projectile throw data
    public bool hasThrowEvent; // Whether a throw occurred at this frame
    public string thrownObjectPrefabName; // Name/path of thrown object for spawning
    public Vector3 throwPosition; // Position where throw originated
    public Vector3 throwVelocity; // Velocity vector of the throw
    public Quaternion throwRotation; // Rotation of thrown object
    
    public MovementFrame(float time, Vector3 pos, Quaternion rot)
    {
        timestamp = time;
        position = pos;
        rotation = rot;
        // Default IK data for backward compatibility
        headPosition = Vector3.zero;
        headRotation = Quaternion.identity;
        leftHandPosition = Vector3.zero;
        leftHandRotation = Quaternion.identity;
        rightHandPosition = Vector3.zero;
        rightHandRotation = Quaternion.identity;
        // Default throw data
        hasThrowEvent = false;
        thrownObjectPrefabName = "";
        throwPosition = Vector3.zero;
        throwVelocity = Vector3.zero;
        throwRotation = Quaternion.identity;
    }
    
    /// <summary>
    /// Constructor with IK target data for rigged character animations
    /// </summary>
    public MovementFrame(float time, Vector3 pos, Quaternion rot,
        Vector3 headPos, Quaternion headRot,
        Vector3 leftHandPos, Quaternion leftHandRot,
        Vector3 rightHandPos, Quaternion rightHandRot)
    {
        timestamp = time;
        position = pos;
        rotation = rot;
        headPosition = headPos;
        headRotation = headRot;
        leftHandPosition = leftHandPos;
        leftHandRotation = leftHandRot;
        rightHandPosition = rightHandPos;
        rightHandRotation = rightHandRot;
        // Default throw data
        hasThrowEvent = false;
        thrownObjectPrefabName = "";
        throwPosition = Vector3.zero;
        throwVelocity = Vector3.zero;
        throwRotation = Quaternion.identity;
    }
    
    /// <summary>
    /// Add throw event data to this frame
    /// </summary>
    public void AddThrowEvent(string prefabName, Vector3 throwPos, Vector3 velocity, Quaternion throwRot)
    {
        hasThrowEvent = true;
        thrownObjectPrefabName = prefabName;
        throwPosition = throwPos;
        throwVelocity = velocity;
        throwRotation = throwRot;
    }
}

