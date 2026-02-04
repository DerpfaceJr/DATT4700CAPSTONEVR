using UnityEngine;
using UnityEngine.AI;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// Spawns enemies at appropriate times
/// Creates enemy GameObjects with appropriate components
/// </summary>
public class EnemySpawner : MonoBehaviour
{
    [Header("Enemy Prefab Settings")]
    [Tooltip("Basic enemy prefab for static enemies (Round 1) - simple cube/mesh")]
    public GameObject enemyPrefab;
    
    [Tooltip("Rigged character prefab for round 2+ enemies - must have IKTargetFollowVRRig")]
    public GameObject riggedEnemyPrefab;
    
    [Header("Projectile Settings")]
    [Tooltip("Enemy Ball prefab for enemy ranged attacks")]
    public GameObject enemyBallPrefab;
    
    [Tooltip("Enemy material (red for visibility) - only used if prefab not assigned")]
    public Material enemyMaterial;
    
    [Tooltip("Enemy scale - only used if prefab not assigned")]
    public Vector3 enemyScale = Vector3.one;
    
    [Header("Spawn Settings")]
    [Tooltip("Spawn points (if empty, uses random positions)")]
    public Transform[] spawnPoints;
    
    [Tooltip("Use random spawn positions within map bounds")]
    public bool useRandomSpawns = true;
    
    [Tooltip("Minimum distance between spawns")]
    public float minSpawnDistance = 2f;
    
    private PlayerRespawner playerRespawner;
    private List<GameObject> activeEnemies = new List<GameObject>();
    
    void Start()
    {
        playerRespawner = FindObjectOfType<PlayerRespawner>();
        
        // Create default enemy material if not assigned (only for procedural fallback)
        if (enemyPrefab == null && enemyMaterial == null)
        {
            enemyMaterial = CreateDefaultEnemyMaterial();
        }
        
        // Warn if no prefab assigned
        if (enemyPrefab == null)
        {
            Debug.LogWarning("EnemySpawner: No enemy prefab assigned! Using procedural creation as fallback. Consider creating an enemy prefab for better performance and customization.");
        }
    }
    
    /// <summary>
    /// Spawn a single enemy at the given world position (used by RoundManager).
    /// </summary>
    public GameObject SpawnSingleEnemyAt(int enemyIndex, Vector3 worldPosition, bool isFirstRound)
    {
        GameObject enemy = CreateEnemyAt(enemyIndex, worldPosition, isFirstRound);
        if (enemy != null)
        {
            activeEnemies.Add(enemy);
        }
        return enemy;
    }
    
    /// <summary>
    /// Get default spawn position for an index (used for round 1 spawn).
    /// </summary>
    public Vector3 GetSpawnPositionForIndex(int index, bool isRigged)
    {
        return GetSpawnPosition(index, isRigged);
    }
    
    /// <summary>
    /// Create a single enemy GameObject at the given position
    /// </summary>
    private GameObject CreateEnemyAt(int index, Vector3 worldPosition, bool isFirstRound)
    {
        GameObject enemy;
        bool useBasicPrefab = isFirstRound && index == 0;
        GameObject prefabToUse = useBasicPrefab ? enemyPrefab : (riggedEnemyPrefab != null ? riggedEnemyPrefab : enemyPrefab);
        
        if (prefabToUse != null)
        {
            enemy = Instantiate(prefabToUse);
            enemy.name = $"Enemy_Round{index + 1}";
        }
        else
        {
            enemy = CreateEnemyProcedurally();
            enemy.name = $"Enemy_Round{index + 1}";
        }
        
        EnsureEnemyComponents(enemy, index, isFirstRound);
        enemy.tag = "Enemy"; // Required for PlayerLoseOnEnemyTouch and other systems
        enemy.transform.position = worldPosition;
        SnapEnemyToNavMesh(enemy);
        return enemy;
    }

    /// <summary>
    /// Snap enemy to the nearest point on the NavMesh so they path around walls and can reach upper floors.
    /// </summary>
    private void SnapEnemyToNavMesh(GameObject enemy)
    {
        Vector3 pos = enemy.transform.position;
        NavMeshHit hit;
        if (NavMesh.SamplePosition(pos, out hit, 5f, NavMesh.AllAreas))
        {
            enemy.transform.position = hit.position;
        }
    }
    
    /// <summary>
    /// Create enemy procedurally (fallback if no prefab)
    /// </summary>
    private GameObject CreateEnemyProcedurally()
    {
        // Create enemy GameObject
        GameObject enemy = GameObject.CreatePrimitive(PrimitiveType.Cube);
        enemy.transform.localScale = enemyScale;
        
        // Set material to red
        Renderer renderer = enemy.GetComponent<Renderer>();
        if (renderer != null)
        {
            if (enemyMaterial != null)
            {
                renderer.material = enemyMaterial;
            }
            else
            {
                // Create red material if none assigned
                Material redMaterial = CreateDefaultEnemyMaterial();
                renderer.material = redMaterial;
            }
        }
        
        // Note: Rigidbody will be added and configured in EnsureEnemyComponents()
        // This avoids redundant setup and ensures consistent configuration
        
        return enemy;
    }
    
    /// <summary>
    /// Ensure enemy has all required components
    /// </summary>
    private void EnsureEnemyComponents(GameObject enemy, int index, bool isFirstRound)
    {
        IKTargetFollowVRRig enemyIKRig = enemy.GetComponent<IKTargetFollowVRRig>();
        if (enemyIKRig == null)
        {
            enemyIKRig = enemy.GetComponentInChildren<IKTargetFollowVRRig>();
        }

        bool hasRiggedCharacter = enemyIKRig != null;

        Rigidbody rb = enemy.GetComponent<Rigidbody>();
        if (rb == null)
        {
            rb = enemy.AddComponent<Rigidbody>();
        }

        // Shared settings (safe for both)
        rb.collisionDetectionMode = CollisionDetectionMode.Continuous;
        rb.interpolation = RigidbodyInterpolation.Interpolate;

        // IMPORTANT: Branch by rig type
        if (hasRiggedCharacter)
        {
            // Rigged humanoid → animation driven
            rb.useGravity = false;
            rb.isKinematic = true;
            rb.constraints = RigidbodyConstraints.None;
            // Note: mass/damping are ignored for kinematic bodies, so we skip them
        }
        else
        {
            // Simple / physics enemy
            rb.mass = 70f; // Only set for non-kinematic bodies
            rb.linearDamping = 2f; // Only set for non-kinematic bodies
            rb.angularDamping = 5f; // Only set for non-kinematic bodies
            rb.useGravity = true;
            rb.isKinematic = false;
            rb.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
        }

        // Ensure enemy root has a non-trigger body collider so when they walk into the player,
        // the player's trigger detects them (PlayerLoseOnEnemyTouch). Prefabs may have no root collider or only triggers.
        EnsureEnemyBodyCollider(enemy, hasRiggedCharacter);

        // Ensure enemy is red (if using prefab, check material)
        Renderer renderer = enemy.GetComponent<Renderer>();
        if (renderer != null)
        {
            Material currentMat = renderer.material;
            // Check if material is not red (rough check)
            if (currentMat != null && currentMat.color != Color.red)
            {
                // Create or use red material
                if (enemyMaterial != null && enemyMaterial.color == Color.red)
                {
                    renderer.material = enemyMaterial;
                }
                else
                {
                    // Create red material
                    Material redMaterial = CreateDefaultEnemyMaterial();
                    renderer.material = redMaterial;
                }
            }
        }
        
        // Ensure EnemyController exists (HP is set on prefab, but we can verify)
        EnemyController enemyController = enemy.GetComponent<EnemyController>();
        if (enemyController == null)
        {
            enemyController = enemy.AddComponent<EnemyController>();
            enemyController.maxHealth = 100f; // Default if not set on prefab
        }
        // Note: HP is editable on the prefab, so we don't override it here
        
        // Ensure CombatSystem exists
        CombatSystem combatSystem = enemy.GetComponent<CombatSystem>();
        if (combatSystem == null)
        {
            combatSystem = enemy.AddComponent<CombatSystem>();
            combatSystem.damageMultiplier = 1f;
            combatSystem.minDamageVelocity = 2f;
            combatSystem.instakillEnabled = true; // Enable instakill by default
            combatSystem.instakillMinVelocity = 1.5f; // Prevent death from touching static objects
        }
        else
        {
            // Ensure instakill is enabled on existing CombatSystem
            combatSystem.instakillEnabled = true;
            combatSystem.instakillMinVelocity = 1.5f; // Prevent death from touching static objects
        }
        
        // Add seek-and-attack behaviour for all enemies
        EnemySeekAndAttack seekAttack = enemy.GetComponent<EnemySeekAndAttack>();
        if (seekAttack == null)
        {
            seekAttack = enemy.AddComponent<EnemySeekAndAttack>();
        }
        seekAttack.moveSpeed = 3f;
        
        // Rigged: keep LegBonePositionFixer for proper leg positioning
        if (hasRiggedCharacter)
        {
            LegBonePositionFixer legFixer = enemy.GetComponent<LegBonePositionFixer>();
            if (legFixer == null)
            {
                legFixer = enemy.AddComponent<LegBonePositionFixer>();
            }
            legFixer.hipWidth = 0.2f;
            legFixer.legForwardOffset = 0.05f;
            legFixer.autoFixOnStart = true;
            legFixer.showDebugInfo = false;
        }
    }

    /// <summary>
    /// Ensures the enemy root has at least one non-trigger collider so when the enemy walks
    /// into the player, the player's body trigger (PlayerLoseOnEnemyTouch) detects contact.
    /// </summary>
    private void EnsureEnemyBodyCollider(GameObject enemy, bool isRigged)
    {
        Collider[] colliders = enemy.GetComponents<Collider>();
        foreach (Collider c in colliders)
        {
            if (c != null && !c.isTrigger)
                return; // Already has a solid body collider
        }
        // No non-trigger collider on root: add a capsule so walking into the player triggers lose
        CapsuleCollider body = enemy.AddComponent<CapsuleCollider>();
        body.isTrigger = false;
        body.direction = 1; // Y-axis
        if (isRigged)
        {
            body.radius = 0.35f;
            body.height = 2f;
            body.center = new Vector3(0f, 1f, 0f); // standing character, center at waist
        }
        else
        {
            body.radius = 0.5f;
            body.height = 1f;
            body.center = Vector3.zero;
        }
    }
    
    /// <summary>
    /// Get spawn position for an enemy
    /// </summary>
    private Vector3 GetSpawnPosition(int index, bool isRigged = false)
    {
        if (spawnPoints != null && spawnPoints.Length > 0 && !useRandomSpawns)
        {
            // Use predefined spawn points
            int spawnIndex = index % spawnPoints.Length;
            if (spawnPoints[spawnIndex] != null)
            {
                return spawnPoints[spawnIndex].position;
            }
        }
        
        // Calculate random X, Z position within map bounds
        float x = 0f;
        float z = 0f;
        float groundLevel = 0f;
        float wallPadding = 1f;
        
        // Get map bounds (similar to PlayerRespawner logic)
        if (playerRespawner != null)
        {
            // Try to get groundLevel from PlayerRespawner
            groundLevel = playerRespawner.groundLevel;
            wallPadding = playerRespawner.wallPadding;
        }
        
        // Find walls to determine map boundaries (same logic as PlayerRespawner)
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
        
        // Generate random X, Z within bounds
        x = Random.Range(minX, maxX);
        z = Random.Range(minZ, maxZ);
        
        // Calculate enemy height offset based on pivot point
        float enemyHeightOffset = 0f;
        
        if (enemyPrefab != null)
        {
            // Get enemy's renderer to calculate bounds
            Renderer renderer = enemyPrefab.GetComponent<Renderer>();
            if (renderer == null)
            {
                renderer = enemyPrefab.GetComponentInChildren<Renderer>();
            }
            
            if (renderer != null)
            {
                // Get the bounds (this gives us the size)
                Bounds bounds = renderer.bounds;
                // bounds.extents.y is half the height
                // Most Unity objects have pivot at center, so add extents.y
                enemyHeightOffset = bounds.extents.y;
            }
            else
            {
                // No renderer found, use default
                enemyHeightOffset = 0.5f;
            }
        }
        else
        {
            // Procedural enemy - use enemyScale
            enemyHeightOffset = enemyScale.y * 0.5f;
        }
        
        // Use raycast to find actual ground surface
        float y = groundLevel + enemyHeightOffset; // Default fallback
        RaycastHit hit;
        Vector3 rayStart = new Vector3(x, 10f, z); // Start raycast from above
        
        if (Physics.Raycast(rayStart, Vector3.down, out hit, 20f))
        {
            // Found ground - spawn enemy on the surface
            // For rigged characters, use a small fixed offset to prevent visual pop
            // (rigged characters have their pivot at hips, not feet)
            if (isRigged)
            {
                y = hit.point.y + 0.05f; // Small offset for rigged characters
            }
            else
            {
                y = hit.point.y + enemyHeightOffset;
            }
        }
        else
        {
            // Raycast failed - use groundLevel as fallback
            if (isRigged)
            {
                y = groundLevel + 0.05f;
            }
            else
            {
                y = groundLevel + enemyHeightOffset;
            }
        }
        
        return new Vector3(x, y, z);
    }
    
    /// <summary>
    /// Clear all active enemies
    /// </summary>
    public void ClearEnemies()
    {
        foreach (GameObject enemy in activeEnemies)
        {
            if (enemy != null)
            {
                Destroy(enemy);
            }
        }
        activeEnemies.Clear();
    }
    
    /// <summary>
    /// Get list of active enemies
    /// </summary>
    public List<GameObject> GetActiveEnemies()
    {
        // Remove null enemies before returning
        activeEnemies.RemoveAll(e => e == null);
        return new List<GameObject>(activeEnemies);
    }
    
    /// <summary>
    /// Remove an enemy from the active list (called when enemy is destroyed)
    /// </summary>
    public void RemoveEnemy(GameObject enemy)
    {
        if (activeEnemies.Contains(enemy))
        {
            activeEnemies.Remove(enemy);
        }
    }
    
    /// <summary>
    /// Create default red material for enemies
    /// </summary>
    private Material CreateDefaultEnemyMaterial()
    {
        Material mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
        mat.color = Color.red;
        return mat;
    }
}
