using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Handles player respawn at random locations within map boundaries
/// </summary>
public class PlayerRespawner : MonoBehaviour
{
    [Header("Spawn Settings")]
    [Tooltip("Minimum distance from walls")]
    public float wallPadding = 1f;
    
    [Tooltip("Ground level Y position (floor level for VR rig - headset adds head height automatically)")]
    public float groundLevel = 0f;
    
    [Tooltip("Legacy: was used for spawn Y. VR rig should be at floor level; headset provides head height. Kept for raycast fallback only.")]
    [HideInInspector]
    public float playerHeightOffset = 1.6f;
    
    [Tooltip("Maximum attempts to find valid spawn location")]
    public int maxSpawnAttempts = 50;
    
    [Header("Spawn Location Tracking")]
    [Tooltip("Minimum distance from previous spawn locations")]
    public float minDistanceFromPreviousSpawns = 3f;
    
    [Tooltip("Maximum number of previous spawn locations to track")]
    public int maxTrackedSpawnLocations = 20;
    
    [Header("Enemy Avoidance")]
    [Tooltip("Minimum distance from enemies when spawning (prevents instant death)")]
    public float minDistanceFromEnemies = 2.5f;
    
    private Transform xrOrigin;
    private Bounds spawnBounds;
    private List<Vector3> previousSpawnLocations = new List<Vector3>();
    
    void Start()
    {
        // Find XR Origin
        GameObject xrOriginObj = GameObject.Find("XR Origin");
        if (xrOriginObj == null)
        {
            xrOriginObj = GameObject.Find("[BuildingBlock] Camera Rig");
        }
        
        if (xrOriginObj != null)
        {
            xrOrigin = xrOriginObj.transform;
        }
        
        // Calculate spawn bounds from map walls
        CalculateSpawnBounds();
    }
    
    /// <summary>
    /// Calculate valid spawn area from map boundaries
    /// </summary>
    private void CalculateSpawnBounds()
    {
        // Find walls to determine map boundaries (active walls first)
        GameObject westWall = GameObject.Find("WestWall");
        GameObject eastWall = GameObject.Find("EastWall");
        GameObject northWall = GameObject.Find("NorthWall");
        GameObject southWall = GameObject.Find("SouthWall");
        
        // Fallback: search inactive walls if not found
        if (westWall == null || eastWall == null || northWall == null || southWall == null)
        {
            Transform[] allTransforms = FindObjectsOfType<Transform>(true);
            foreach (Transform t in allTransforms)
            {
                if (westWall == null && t.name == "WestWall") westWall = t.gameObject;
                if (eastWall == null && t.name == "EastWall") eastWall = t.gameObject;
                if (northWall == null && t.name == "NorthWall") northWall = t.gameObject;
                if (southWall == null && t.name == "SouthWall") southWall = t.gameObject;
            }
        }
        
        float minX = -10f; // Default values if walls not found (20x20 map)
        float maxX = 10f;
        float minZ = -10f;
        float maxZ = 10f;
        
        if (westWall != null)
        {
            minX = westWall.transform.position.x + wallPadding;
        }
        if (eastWall != null)
        {
            maxX = eastWall.transform.position.x - wallPadding;
        }
        if (southWall != null)
        {
            minZ = southWall.transform.position.z + wallPadding;
        }
        if (northWall != null)
        {
            maxZ = northWall.transform.position.z - wallPadding;
        }
        
        // Create bounds
        Vector3 center = new Vector3((minX + maxX) / 2f, groundLevel, (minZ + maxZ) / 2f);
        Vector3 size = new Vector3(maxX - minX, 0.1f, maxZ - minZ);
        spawnBounds = new Bounds(center, size);
    }
    
    /// <summary>
    /// Respawn player at a random valid location
    /// </summary>
    public void RespawnPlayer()
    {
        if (xrOrigin == null)
        {
            Debug.LogWarning("PlayerRespawner: XR Origin not found!");
            return;
        }
        
        // Recalculate spawn bounds in case map was resized
        CalculateSpawnBounds();
        
        Vector3 spawnPosition = GetRandomSpawnPosition();
        
        if (spawnPosition != Vector3.zero)
        {
            // Teleport player to spawn position
            xrOrigin.position = spawnPosition;
            
            // Track this spawn location
            RecordSpawnLocation(spawnPosition);
            
            // Reset rotation to face forward (optional)
            // xrOrigin.rotation = Quaternion.identity;
            
            Debug.Log($"Player respawned at: {spawnPosition}");
        }
        else
        {
            Debug.LogWarning("PlayerRespawner: Could not find valid spawn position!");
            // Fallback to center (floor level - VR headset adds head height)
            Vector3 fallbackPos = new Vector3(0f, groundLevel, 0f);
            xrOrigin.position = fallbackPos;
            RecordSpawnLocation(fallbackPos);
        }
    }
    
    /// <summary>
    /// Get a random valid spawn position within bounds
    /// </summary>
    private Vector3 GetRandomSpawnPosition()
    {
        for (int i = 0; i < maxSpawnAttempts; i++)
        {
            // Generate random position within bounds (Y = floor level; VR headset adds head height)
            Vector3 randomPos = new Vector3(
                Random.Range(spawnBounds.min.x, spawnBounds.max.x),
                groundLevel,
                Random.Range(spawnBounds.min.z, spawnBounds.max.z)
            );
            
            // Check if position is valid (not inside walls, has ground, not near previous spawns)
            if (IsValidSpawnPosition(randomPos) && IsFarEnoughFromPreviousSpawns(randomPos))
            {
                return randomPos;
            }
        }
        
        // If no valid position found after max attempts, try without spawn distance check
        // (fallback if map is too small or too many previous spawns)
        for (int i = 0; i < maxSpawnAttempts; i++)
        {
            Vector3 randomPos = new Vector3(
                Random.Range(spawnBounds.min.x, spawnBounds.max.x),
                groundLevel,
                Random.Range(spawnBounds.min.z, spawnBounds.max.z)
            );
            
            if (IsValidSpawnPosition(randomPos))
            {
                Debug.LogWarning("PlayerRespawner: Could not find spawn location far enough from previous spawns. Using closest valid position.");
                return randomPos;
            }
        }
        
        // Final fallback to center (floor level)
        return new Vector3(0f, groundLevel, 0f);
    }
    
    /// <summary>
    /// Check if a spawn position is valid
    /// </summary>
    private bool IsValidSpawnPosition(Vector3 position)
    {
        // Raycast down to check for ground
        RaycastHit hit;
        if (Physics.Raycast(position + Vector3.up, Vector3.down, out hit, 5f))
        {
            // Check if we're not too close to walls
            Collider[] nearbyColliders = Physics.OverlapSphere(position, 0.5f);
            foreach (Collider col in nearbyColliders)
            {
                if (col.gameObject.name.Contains("Wall"))
                {
                    return false;
                }
            }
            
            if (!IsFarEnoughFromEnemies(position))
                return false;
            
            return true;
        }
        
        return false;
    }
    
    private bool IsFarEnoughFromEnemies(Vector3 position)
    {
        GameObject[] enemies = GameObject.FindGameObjectsWithTag("Enemy");
        Vector2 pos2D = new Vector2(position.x, position.z);
        foreach (GameObject enemy in enemies)
        {
            if (enemy == null) continue;
            Vector2 enemy2D = new Vector2(enemy.transform.position.x, enemy.transform.position.z);
            if (Vector2.Distance(pos2D, enemy2D) < minDistanceFromEnemies)
                return false;
        }
        return true;
    }
    
    /// <summary>
    /// Check if a position is far enough from all previous spawn locations
    /// </summary>
    private bool IsFarEnoughFromPreviousSpawns(Vector3 position)
    {
        // Only check X and Z coordinates (ignore Y height difference)
        Vector2 pos2D = new Vector2(position.x, position.z);
        
        foreach (Vector3 previousSpawn in previousSpawnLocations)
        {
            Vector2 prev2D = new Vector2(previousSpawn.x, previousSpawn.z);
            float distance = Vector2.Distance(pos2D, prev2D);
            
            if (distance < minDistanceFromPreviousSpawns)
            {
                return false;
            }
        }
        
        return true;
    }
    
    /// <summary>
    /// Record a spawn location for future avoidance
    /// </summary>
    private void RecordSpawnLocation(Vector3 position)
    {
        previousSpawnLocations.Add(position);
        
        // Limit the number of tracked locations to prevent memory growth
        if (previousSpawnLocations.Count > maxTrackedSpawnLocations)
        {
            previousSpawnLocations.RemoveAt(0); // Remove oldest
        }
    }
    
    /// <summary>
    /// Clear all tracked spawn locations (useful for game reset)
    /// </summary>
    public void ClearSpawnHistory()
    {
        previousSpawnLocations.Clear();
        Debug.Log("PlayerRespawner: Cleared spawn location history");
    }
    
    void OnDrawGizmosSelected()
    {
        // Draw spawn bounds in editor
        Gizmos.color = Color.green;
        Gizmos.DrawWireCube(spawnBounds.center, spawnBounds.size);
    }
}

