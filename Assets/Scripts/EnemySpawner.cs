using UnityEngine;
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
    
    [Tooltip("Rigged character prefab for replay enemies (Round 2+) - must have IKTargetFollowVRRig")]
    public GameObject riggedEnemyPrefab;
    
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
    /// Spawn a single enemy for a round
    /// </summary>
    public GameObject SpawnSingleEnemy(int enemyIndex, List<MovementFrame> movementData, bool isFirstRound)
    {
        GameObject enemy = CreateEnemy(enemyIndex, movementData, isFirstRound);
        if (enemy != null)
        {
            activeEnemies.Add(enemy);
        }
        return enemy;
    }
    
    /// <summary>
    /// Spawn enemies for a round (legacy method - kept for compatibility)
    /// </summary>
    public List<GameObject> SpawnEnemies(int enemyCount, List<MovementFrame> movementData, bool isFirstRound)
    {
        // Clear previous enemies
        ClearEnemies();
        
        activeEnemies.Clear();
        
        for (int i = 0; i < enemyCount; i++)
        {
            GameObject enemy = CreateEnemy(i, movementData, isFirstRound);
            if (enemy != null)
            {
                activeEnemies.Add(enemy);
            }
        }
        
        return activeEnemies;
    }
    
    /// <summary>
    /// Create a single enemy GameObject
    /// </summary>
    private GameObject CreateEnemy(int index, List<MovementFrame> movementData, bool isFirstRound)
    {
        GameObject enemy;
        
        // Determine which prefab to use based on enemy type
        bool shouldBeStatic = isFirstRound && index == 0;
        GameObject prefabToUse = null;
        
        if (shouldBeStatic)
        {
            // Static enemy (Round 1 first enemy) - use basic enemy prefab
            prefabToUse = enemyPrefab;
        }
        else
        {
            // Replay enemy (Round 2+) - use rigged character prefab
            prefabToUse = riggedEnemyPrefab != null ? riggedEnemyPrefab : enemyPrefab;
        }
        
        // Use prefab if assigned, otherwise create procedurally
        if (prefabToUse != null)
        {
            // Instantiate from prefab
            enemy = Instantiate(prefabToUse);
            enemy.name = $"Enemy_Round{index + 1}";
        }
        else
        {
            // Fallback: Create procedurally (for backwards compatibility)
            enemy = CreateEnemyProcedurally();
            enemy.name = $"Enemy_Round{index + 1}";
        }
        
        // Set enemy tag (important: rigged enemies from Player prefab need this!)
        enemy.tag = "Enemy";
        
        // Ensure required components exist
        EnsureEnemyComponents(enemy, index, movementData, isFirstRound);
        
        // Position enemy
        Vector3 spawnPosition = GetSpawnPosition(index);
        enemy.transform.position = spawnPosition;
        
        return enemy;
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
        
        // Add Rigidbody with gravity enabled
        Rigidbody rb = enemy.AddComponent<Rigidbody>();
        rb.mass = 1f;
        rb.useGravity = true; // Ensure gravity is enabled
        rb.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
        
        return enemy;
    }
    
    /// <summary>
    /// Ensure enemy has all required components
    /// </summary>
    private void EnsureEnemyComponents(GameObject enemy, int index, List<MovementFrame> movementData, bool isFirstRound)
    {
        // Ensure Rigidbody exists with gravity enabled
        Rigidbody rb = enemy.GetComponent<Rigidbody>();
        if (rb == null)
        {
            rb = enemy.AddComponent<Rigidbody>();
            rb.mass = 70f; // Typical human weight
            rb.linearDamping = 2f; // Add drag to prevent sliding
            rb.angularDamping = 5f; // Prevent rotation drift
            rb.useGravity = true; // Ensure gravity is enabled
            rb.isKinematic = false; // Must be non-kinematic for gravity to work
            rb.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
            rb.collisionDetectionMode = CollisionDetectionMode.Continuous; // Better collision detection
            rb.interpolation = RigidbodyInterpolation.Interpolate; // Smooth movement
        }
        else
        {
            // Ensure gravity is enabled and not kinematic on existing Rigidbody
            rb.linearDamping = 2f;
            rb.angularDamping = 5f;
            rb.useGravity = true;
            rb.isKinematic = false; // Must be non-kinematic for gravity to work
            rb.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
            rb.collisionDetectionMode = CollisionDetectionMode.Continuous;
            rb.interpolation = RigidbodyInterpolation.Interpolate;
        }
        
        // Ensure enemy has a collider for physics and combat
        Collider collider = enemy.GetComponent<Collider>();
        if (collider == null)
        {
            // Add CapsuleCollider for rigged characters (encompasses body)
            CapsuleCollider capsule = enemy.AddComponent<CapsuleCollider>();
            capsule.center = new Vector3(0f, 1.05f, 0f); // Center slightly higher to prevent ground penetration
            capsule.radius = 0.35f; // Slightly narrower for more precise hits
            capsule.height = 1.9f; // Slightly taller
            capsule.direction = 1; // Y-axis (vertical)
            
            Debug.Log($"Added CapsuleCollider to enemy {enemy.name}: center={capsule.center}, radius={capsule.radius}, height={capsule.height}");
        }
        
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
        
        // Check if enemy has rigged character model (has IKTargetFollowVRRig component)
        IKTargetFollowVRRig enemyIKRig = enemy.GetComponent<IKTargetFollowVRRig>();
        if (enemyIKRig == null)
        {
            enemyIKRig = enemy.GetComponentInChildren<IKTargetFollowVRRig>();
        }
        
        bool hasRiggedCharacter = enemyIKRig != null;
        
        // Add appropriate AI component (remove existing ones first)
        StaticEnemy existingStatic = enemy.GetComponent<StaticEnemy>();
        EnemyAI existingAI = enemy.GetComponent<EnemyAI>();
        EnemyIKReplay existingIKReplay = enemy.GetComponent<EnemyIKReplay>();
        
        if (isFirstRound && index == 0)
        {
            // First enemy is static
            if (existingAI != null)
            {
                Destroy(existingAI);
            }
            if (existingIKReplay != null)
            {
                Destroy(existingIKReplay);
            }
            if (existingStatic == null)
            {
                StaticEnemy staticEnemy = enemy.AddComponent<StaticEnemy>();
                staticEnemy.rotationSpeed = 30f;
                staticEnemy.pattern = StaticEnemy.RotationPattern.Continuous;
            }
            
            // Disable IKFootSolver components on static enemy (not needed for stationary enemy)
            IKFootSolver[] footSolvers = enemy.GetComponentsInChildren<IKFootSolver>();
            foreach (IKFootSolver footSolver in footSolvers)
            {
                footSolver.enabled = false;
            }
        }
        else
        {
            // Other enemies replay movements
            if (existingStatic != null)
            {
                Destroy(existingStatic);
            }
            
            if (hasRiggedCharacter)
            {
                // Use EnemyIKReplay for rigged characters
                if (existingAI != null)
                {
                    Destroy(existingAI);
                }
                
                EnemyIKReplay enemyIKReplay = existingIKReplay;
                if (enemyIKReplay == null)
                {
                    enemyIKReplay = enemy.AddComponent<EnemyIKReplay>();
                }
                
                // Set IK rig reference
                enemyIKReplay.ikRig = enemyIKRig;
                enemyIKReplay.useInterpolation = true;
                
                // Verify IK targets are assigned
                if (enemyIKRig != null)
                {
                    bool hasHeadIK = enemyIKRig.head.ikTarget != null;
                    bool hasLeftHandIK = enemyIKRig.leftHand.ikTarget != null;
                    bool hasRightHandIK = enemyIKRig.rightHand.ikTarget != null;
                    Debug.Log($"Enemy {index + 1} (Rigged): IK Targets - Head: {hasHeadIK}, LeftHand: {hasLeftHandIK}, RightHand: {hasRightHandIK}");
                }
                
                // Set movement data - use previous round's movements
                if (enemyIKReplay != null)
                {
                    if (movementData != null && movementData.Count > 0)
                    {
                        // Check if movement data has IK data
                        bool hasIKData = movementData.Count > 0 && (
                            movementData[0].headPosition != Vector3.zero ||
                            movementData[0].leftHandPosition != Vector3.zero ||
                            movementData[0].rightHandPosition != Vector3.zero
                        );
                        
                        // Create a copy of the movement data to avoid reference issues
                        List<MovementFrame> dataCopy = new List<MovementFrame>(movementData);
                        enemyIKReplay.SetMovementData(dataCopy);
                        
                        // Add time offset for multiple enemies replaying same path
                        enemyIKReplay.timeOffset = index * 0.5f; // Stagger enemies slightly
                        
                        Debug.Log($"Enemy {index + 1} (Rigged): Assigned {movementData.Count} movement frames. Has IK Data: {hasIKData} (first frame time: {movementData[0].timestamp}, last: {movementData[movementData.Count - 1].timestamp})");
                    }
                    else
                    {
                        Debug.LogWarning($"Enemy {index + 1} (Rigged): No movement data available for replay! (isFirstRound={isFirstRound}, index={index}, movementData is {(movementData == null ? "null" : $"empty (count={movementData.Count})")})");
                    }
                }
            }
            else
            {
                // Fallback to EnemyAI for non-rigged enemies (backward compatible)
                if (existingIKReplay != null)
                {
                    Destroy(existingIKReplay);
                }
                
                EnemyAI enemyAI = existingAI;
                if (enemyAI == null)
                {
                    enemyAI = enemy.AddComponent<EnemyAI>();
                    enemyAI.useInterpolation = true;
                }
                
                // Set movement data - use previous round's movements
                if (enemyAI != null)
                {
                    if (movementData != null && movementData.Count > 0)
                    {
                        // Create a copy of the movement data to avoid reference issues
                        List<MovementFrame> dataCopy = new List<MovementFrame>(movementData);
                        enemyAI.SetMovementData(dataCopy);
                        
                        // Add time offset for multiple enemies replaying same path
                        enemyAI.timeOffset = index * 0.5f; // Stagger enemies slightly
                        
                        Debug.Log($"Enemy {index + 1} (Basic): Assigned {movementData.Count} movement frames (first frame time: {movementData[0].timestamp}, last: {movementData[movementData.Count - 1].timestamp})");
                    }
                    else
                    {
                        Debug.LogWarning($"Enemy {index + 1} (Basic): No movement data available for replay! (isFirstRound={isFirstRound}, index={index}, movementData is {(movementData == null ? "null" : $"empty (count={movementData.Count})")})");
                    }
                }
            }
        }
    }
    
    /// <summary>
    /// Get spawn position for an enemy
    /// </summary>
    private Vector3 GetSpawnPosition(int index)
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
            y = hit.point.y + enemyHeightOffset;
        }
        else
        {
            // Raycast failed - use groundLevel as fallback
            y = groundLevel + enemyHeightOffset;
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

