using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Tracks and resets positions of throwable objects and weapons between rounds
/// Attach to RoundManager or GameManager
/// </summary>
public class ObjectPositionResetter : MonoBehaviour
{
    [Header("Object Tracking")]
    [Tooltip("Automatically find and track all throwable objects on Start")]
    public bool autoFindObjects = true;
    
    [Tooltip("Tags to search for when finding objects")]
    public string[] trackableTags = new string[] { "Throwable", "Sword", "Weapon", "Melee" };
    
    [Tooltip("Manually assigned objects to track")]
    public GameObject[] manualObjects;
    
    [Header("Reset Settings")]
    [Tooltip("Reset objects when round starts")]
    public bool resetOnRoundStart = true;
    
    [Tooltip("Reset objects when round ends (in addition to round start)")]
    public bool resetOnRoundEnd = true;
    
    [Tooltip("Reset physics velocities")]
    public bool resetVelocities = true;
    
    [Tooltip("Reset rotations to original")]
    public bool resetRotations = true;
    
    [Tooltip("Re-find objects each reset (in case new ones were added)")]
    public bool refindObjectsOnReset = true;
    
    // Tracked objects and their initial states
    private Dictionary<GameObject, ObjectState> trackedObjects = new Dictionary<GameObject, ObjectState>();
    
    private class ObjectState
    {
        public Vector3 position;
        public Quaternion rotation;
        public bool wasKinematic;
        public Vector3 scale;
    }
    
    void Start()
    {
        if (autoFindObjects)
        {
            FindAndTrackObjects();
        }
        
        // Track manually assigned objects
        if (manualObjects != null)
        {
            foreach (GameObject obj in manualObjects)
            {
                if (obj != null)
                {
                    TrackObject(obj);
                }
            }
        }
        
        // Subscribe to round events
        RoundManager roundManager = FindObjectOfType<RoundManager>();
        if (roundManager != null)
        {
            // Subscribe to round end event if enabled
            if (resetOnRoundEnd)
            {
                roundManager.OnRoundComplete += OnRoundComplete;
            }
            Debug.Log($"ObjectPositionResetter: Found RoundManager, ready to reset {trackedObjects.Count} objects");
        }
        else
        {
            Debug.LogWarning("ObjectPositionResetter: No RoundManager found!");
        }
    }
    
    /// <summary>
    /// Find and track all throwable objects in the scene
    /// </summary>
    public void FindAndTrackObjects()
    {
        trackedObjects.Clear();
        
        // Find by tags
        foreach (string tag in trackableTags)
        {
            try
            {
                GameObject[] objects = GameObject.FindGameObjectsWithTag(tag);
                foreach (GameObject obj in objects)
                {
                    TrackObject(obj);
                }
                Debug.Log($"ObjectPositionResetter: Found {objects.Length} objects with tag '{tag}'");
            }
            catch (UnityException)
            {
                // Tag doesn't exist, skip
                Debug.LogWarning($"ObjectPositionResetter: Tag '{tag}' not defined");
            }
        }
        
        // Find objects with specific components
        BuildingBlockProjectile[] projectiles = FindObjectsOfType<BuildingBlockProjectile>();
        foreach (var proj in projectiles)
        {
            TrackObject(proj.gameObject);
        }
        
        // Find balls with ConfigureBallLikeCube component
        ConfigureBallLikeCube[] balls = FindObjectsOfType<ConfigureBallLikeCube>();
        foreach (var ball in balls)
        {
            TrackObject(ball.gameObject);
        }
        
        // Find objects with "Ball" in name (catch any balls we might have missed)
        GameObject[] allObjects = FindObjectsOfType<GameObject>();
        foreach (GameObject obj in allObjects)
        {
            if (obj.name.ToLower().Contains("ball") && obj.GetComponent<Rigidbody>() != null)
            {
                // Only track if it has a Rigidbody (is throwable)
                if (!trackedObjects.ContainsKey(obj))
                {
                    TrackObject(obj);
                }
            }
        }
        
        SwordPhysicsFix[] swords = FindObjectsOfType<SwordPhysicsFix>();
        foreach (var sword in swords)
        {
            TrackObject(sword.gameObject);
        }
        
        Debug.Log($"ObjectPositionResetter: Now tracking {trackedObjects.Count} total objects");
    }
    
    /// <summary>
    /// Track a specific object's initial state
    /// </summary>
    public void TrackObject(GameObject obj)
    {
        if (obj == null || trackedObjects.ContainsKey(obj))
            return;
        
        ObjectState state = new ObjectState
        {
            position = obj.transform.position,
            rotation = obj.transform.rotation,
            scale = obj.transform.localScale
        };
        
        Rigidbody rb = obj.GetComponent<Rigidbody>();
        if (rb != null)
        {
            state.wasKinematic = rb.isKinematic;
        }
        
        trackedObjects[obj] = state;
        Debug.Log($"ObjectPositionResetter: Tracking {obj.name} at position {state.position}");
    }
    
    private void OnDestroy()
    {
        // Unsubscribe from events
        RoundManager roundManager = FindObjectOfType<RoundManager>();
        if (roundManager != null)
        {
            roundManager.OnRoundComplete -= OnRoundComplete;
        }
    }
    
    /// <summary>
    /// Handle round complete event
    /// </summary>
    private void OnRoundComplete(int roundNumber)
    {
        if (resetOnRoundEnd)
        {
            ResetObjects();
        }
    }
    
    /// <summary>
    /// Reset all tracked objects to their initial positions
    /// Call this at the start of each round or when round ends
    /// </summary>
    public void ResetObjects()
    {
        if (!resetOnRoundStart && !resetOnRoundEnd)
        {
            Debug.Log("ObjectPositionResetter: Reset disabled");
            return;
        }
        
        // Re-find objects if enabled (in case new ones were added or scene changed)
        if (refindObjectsOnReset)
        {
            FindAndTrackObjects();
        }
        
        int resetCount = 0;
        List<GameObject> objectsToRemove = new List<GameObject>();
        
        foreach (var kvp in trackedObjects)
        {
            GameObject obj = kvp.Key;
            ObjectState state = kvp.Value;
            
            // Skip if object was destroyed
            if (obj == null)
            {
                objectsToRemove.Add(obj);
                continue;
            }
            
            // Reset position
            obj.transform.position = state.position;
            
            // Reset rotation if enabled
            if (resetRotations)
            {
                obj.transform.rotation = state.rotation;
            }
            
            // Reset scale (in case it was modified)
            obj.transform.localScale = state.scale;
            
            // Reset physics
            Rigidbody rb = obj.GetComponent<Rigidbody>();
            if (rb != null)
            {
                if (resetVelocities)
                {
                    rb.linearVelocity = Vector3.zero;
                    rb.angularVelocity = Vector3.zero;
                }
                
                // Restore kinematic state
                rb.isKinematic = state.wasKinematic;
                
                // Force physics sync
                Physics.SyncTransforms();
            }
            
            resetCount++;
        }
        
        // Clean up destroyed objects
        foreach (GameObject obj in objectsToRemove)
        {
            trackedObjects.Remove(obj);
        }
        
        Debug.Log($"ObjectPositionResetter: Reset {resetCount} objects to starting positions");
    }
    
    /// <summary>
    /// Update stored positions for all tracked objects
    /// Useful if you want to set a new "home" position mid-game
    /// </summary>
    public void UpdateStoredPositions()
    {
        foreach (var kvp in trackedObjects)
        {
            GameObject obj = kvp.Key;
            if (obj != null)
            {
                ObjectState state = kvp.Value;
                state.position = obj.transform.position;
                state.rotation = obj.transform.rotation;
                state.scale = obj.transform.localScale;
                
                Rigidbody rb = obj.GetComponent<Rigidbody>();
                if (rb != null)
                {
                    state.wasKinematic = rb.isKinematic;
                }
            }
        }
        
        Debug.Log($"ObjectPositionResetter: Updated stored positions for {trackedObjects.Count} objects");
    }
    
    /// <summary>
    /// Get count of tracked objects
    /// </summary>
    public int GetTrackedObjectCount()
    {
        return trackedObjects.Count;
    }
}
