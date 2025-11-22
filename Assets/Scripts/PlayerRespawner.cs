using UnityEngine;

/// <summary>
/// Handles player respawn at random locations within map boundaries
/// </summary>
public class PlayerRespawner : MonoBehaviour
{
    [Header("Spawn Settings")]
    [Tooltip("Minimum distance from walls")]
    public float wallPadding = 1f;
    
    [Tooltip("Ground level Y position")]
    public float groundLevel = 0f;
    
    [Tooltip("Player height offset from ground")]
    public float playerHeightOffset = 1.6f;
    
    [Tooltip("Maximum attempts to find valid spawn location")]
    public int maxSpawnAttempts = 50;
    
    private Transform xrOrigin;
    private Bounds spawnBounds;
    
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
        // Find walls to determine map boundaries
        GameObject westWall = GameObject.Find("WestWall");
        GameObject eastWall = GameObject.Find("EastWall");
        GameObject northWall = GameObject.Find("NorthWall");
        GameObject southWall = GameObject.Find("SouthWall");
        
        float minX = -2.5f; // Default values if walls not found
        float maxX = 2.5f;
        float minZ = -2.5f;
        float maxZ = 2.5f;
        
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
        
        Vector3 spawnPosition = GetRandomSpawnPosition();
        
        if (spawnPosition != Vector3.zero)
        {
            // Teleport player to spawn position
            xrOrigin.position = spawnPosition;
            
            // Reset rotation to face forward (optional)
            // xrOrigin.rotation = Quaternion.identity;
            
            Debug.Log($"Player respawned at: {spawnPosition}");
        }
        else
        {
            Debug.LogWarning("PlayerRespawner: Could not find valid spawn position!");
            // Fallback to center
            xrOrigin.position = new Vector3(0f, groundLevel + playerHeightOffset, 0f);
        }
    }
    
    /// <summary>
    /// Get a random valid spawn position within bounds
    /// </summary>
    private Vector3 GetRandomSpawnPosition()
    {
        for (int i = 0; i < maxSpawnAttempts; i++)
        {
            // Generate random position within bounds
            Vector3 randomPos = new Vector3(
                Random.Range(spawnBounds.min.x, spawnBounds.max.x),
                groundLevel + playerHeightOffset,
                Random.Range(spawnBounds.min.z, spawnBounds.max.z)
            );
            
            // Check if position is valid (not inside walls, has ground)
            if (IsValidSpawnPosition(randomPos))
            {
                return randomPos;
            }
        }
        
        // If no valid position found, return center
        return new Vector3(0f, groundLevel + playerHeightOffset, 0f);
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
            
            return true;
        }
        
        return false;
    }
    
    void OnDrawGizmosSelected()
    {
        // Draw spawn bounds in editor
        Gizmos.color = Color.green;
        Gizmos.DrawWireCube(spawnBounds.center, spawnBounds.size);
    }
}

