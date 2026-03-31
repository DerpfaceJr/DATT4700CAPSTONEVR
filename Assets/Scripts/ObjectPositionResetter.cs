using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Tracks and resets positions of throwable objects and weapons between rounds
/// Attach to RoundManager or GameManager
/// </summary>
[DefaultExecutionOrder(100)]
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
        // Do not clear: merge new finds so stored spawn positions survive refind after rounds.
        // (Destroyed objects are removed during ResetObjects when references are null.)
        
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
        BuildingBlockProjectile[] projectiles = FindObjectsByType<BuildingBlockProjectile>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (var proj in projectiles)
        {
            TrackObject(proj.gameObject);
        }

        // Find healing projectiles (e.g. apples) — include inactive (used apples are hidden, not destroyed)
        HealingProjectile[] healingProjectiles = FindObjectsByType<HealingProjectile>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (var hp in healingProjectiles)
        {
            TrackObject(hp.gameObject);
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
            // BuildingBlockProjectile / HealingProjectile force isKinematic=false in Start; if we track
            // before that runs, we'd store true and reset would break throws (notably on Mac / variable script order).
            if (UsesDynamicThrowablePhysics(obj))
                state.wasKinematic = false;
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
            
            // Skip if player is holding this object (prevents player from floating/being yanked)
            SwordPhysicsFix sword = obj.GetComponent<SwordPhysicsFix>();
            if (sword != null && sword.isGrabbed)
                continue;
            
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

                // Match BuildingBlockProjectile / HealingProjectile: must stay dynamic for throws and enemy hits
                if (UsesDynamicThrowablePhysics(obj))
                    ApplyDynamicThrowablePhysics(rb, obj);
                
                // Force physics sync
                Physics.SyncTransforms();
            }

            // Restore healing apples (hidden after use until next round reset)
            HealingProjectile healProj = obj.GetComponent<HealingProjectile>();
            if (healProj != null)
                healProj.ResetForNewRound();
            
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
                    if (UsesDynamicThrowablePhysics(obj))
                        state.wasKinematic = false;
                }
            }
        }
        
        Debug.Log($"ObjectPositionResetter: Updated stored positions for {trackedObjects.Count} objects");
    }

    /// <summary>Throwables that must stay non-kinematic for physics throws / CombatSystem hits.</summary>
    private static bool UsesDynamicThrowablePhysics(GameObject obj)
    {
        if (obj == null) return false;
        return obj.GetComponent<BuildingBlockProjectile>() != null
            || obj.GetComponent<HealingProjectile>() != null
            || obj.GetComponent<ConfigureBallLikeCube>() != null;
    }

    private static void ApplyDynamicThrowablePhysics(Rigidbody rb, GameObject obj)
    {
        if (rb == null || obj == null) return;
        rb.isKinematic = false;
        rb.useGravity = true;
        if (obj.GetComponent<HealingProjectile>() != null)
            rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
        else
            rb.collisionDetectionMode = CollisionDetectionMode.Continuous;
    }
    
    /// <summary>
    /// Get count of tracked objects
    /// </summary>
    public int GetTrackedObjectCount()
    {
        return trackedObjects.Count;
    }
}
