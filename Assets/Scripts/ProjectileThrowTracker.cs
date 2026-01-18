using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Tracks when player throws projectiles and records throw events
/// Works with PlayerMovementRecorder to add throw data to movement frames
/// </summary>
public class ProjectileThrowTracker : MonoBehaviour
{
    [Header("Tracking Settings")]
    [Tooltip("Minimum velocity to register as a throw")]
    public float minThrowVelocity = 2.0f;
    
    [Tooltip("How long to track an object after it's released")]
    public float trackingDuration = 0.5f;
    
    private PlayerMovementRecorder movementRecorder;
    private Dictionary<GameObject, ThrowTrackingData> trackedObjects = new Dictionary<GameObject, ThrowTrackingData>();
    private List<GameObject> objectsToRemove = new List<GameObject>();
    
    private class ThrowTrackingData
    {
        public float releaseTime;
        public Vector3 lastPosition;
        public bool throwRegistered;
        public Rigidbody rb;
    }
    
    void Start()
    {
        movementRecorder = FindObjectOfType<PlayerMovementRecorder>();
        if (movementRecorder == null)
        {
            Debug.LogError("ProjectileThrowTracker: No PlayerMovementRecorder found!");
        }
    }
    
    void Update()
    {
        // Clean up tracked objects
        objectsToRemove.Clear();
        
        foreach (var kvp in trackedObjects)
        {
            GameObject obj = kvp.Key;
            ThrowTrackingData data = kvp.Value;
            
            // Remove if object was destroyed or tracking expired
            if (obj == null || Time.time - data.releaseTime > trackingDuration)
            {
                objectsToRemove.Add(obj);
                continue;
            }
            
            // Check if object reached throw velocity
            if (!data.throwRegistered && data.rb != null)
            {
                float velocity = data.rb.linearVelocity.magnitude;
                
                if (velocity >= minThrowVelocity)
                {
                    // Register throw event
                    RegisterThrow(obj, data.rb);
                    data.throwRegistered = true;
                    Debug.Log($"ProjectileThrowTracker: Registered throw of {obj.name} at velocity {velocity:F2} m/s");
                }
            }
        }
        
        // Remove expired objects
        foreach (GameObject obj in objectsToRemove)
        {
            trackedObjects.Remove(obj);
        }
    }
    
    /// <summary>
    /// Start tracking a potentially thrown object
    /// Call this when player releases an object
    /// </summary>
    public void TrackObject(GameObject obj)
    {
        if (obj == null) return;
        
        Rigidbody rb = obj.GetComponent<Rigidbody>();
        if (rb == null)
        {
            Debug.LogWarning($"ProjectileThrowTracker: Object {obj.name} has no Rigidbody, cannot track throw");
            return;
        }
        
        // Add to tracking
        trackedObjects[obj] = new ThrowTrackingData
        {
            releaseTime = Time.time,
            lastPosition = obj.transform.position,
            throwRegistered = false,
            rb = rb
        };
        
        Debug.Log($"ProjectileThrowTracker: Started tracking {obj.name}");
    }
    
    /// <summary>
    /// Register a throw event with the movement recorder
    /// </summary>
    private void RegisterThrow(GameObject obj, Rigidbody rb)
    {
        if (movementRecorder == null) return;
        
        // Get the most recent movement frame and add throw data
        List<MovementFrame> movementData = movementRecorder.GetMovementData();
        if (movementData != null && movementData.Count > 0)
        {
            MovementFrame latestFrame = movementData[movementData.Count - 1];
            
            // Determine prefab name (for spawning during replay)
            string prefabName = obj.name.Replace("(Clone)", "").Trim();
            
            // Add throw event to the frame
            latestFrame.AddThrowEvent(
                prefabName,
                obj.transform.position,
                rb.linearVelocity,
                obj.transform.rotation
            );
            
            Debug.Log($"ProjectileThrowTracker: Added throw event to frame at time {latestFrame.timestamp:F2}s - {prefabName} with velocity {rb.linearVelocity}");
        }
    }
    
    /// <summary>
    /// Get all throwable objects in scene for initial setup
    /// </summary>
    public void RegisterAllThrowables()
    {
        BuildingBlockProjectile[] projectiles = FindObjectsOfType<BuildingBlockProjectile>();
        Debug.Log($"ProjectileThrowTracker: Found {projectiles.Length} projectile objects to potentially track");
    }
}
