using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Spawns enemies at appropriate times
/// Creates enemy GameObjects with appropriate components
/// </summary>
public class EnemySpawner : MonoBehaviour
{
    [Header("Enemy Prefab Settings")]
    [Tooltip("Enemy prefab to spawn (must have EnemyController component)")]
    public GameObject enemyPrefab;
    
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
        
        // Use prefab if assigned, otherwise create procedurally
        if (enemyPrefab != null)
        {
            // Instantiate from prefab
            enemy = Instantiate(enemyPrefab);
            enemy.name = $"Enemy_Round{index + 1}";
        }
        else
        {
            // Fallback: Create procedurally (for backwards compatibility)
            enemy = CreateEnemyProcedurally();
            enemy.name = $"Enemy_Round{index + 1}";
        }
        
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
            rb.mass = 1f;
            rb.useGravity = true; // Ensure gravity is enabled
            rb.isKinematic = false; // Must be non-kinematic for gravity to work
            rb.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
        }
        else
        {
            // Ensure gravity is enabled and not kinematic on existing Rigidbody
            rb.useGravity = true;
            rb.isKinematic = false; // Must be non-kinematic for gravity to work
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
            combatSystem.instakillMinVelocity = 0.5f; // Low threshold for easy instakills
        }
        else
        {
            // Ensure instakill is enabled on existing CombatSystem
            combatSystem.instakillEnabled = true;
            combatSystem.instakillMinVelocity = 0.5f;
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
        
        // Use random spawn position
        if (playerRespawner != null)
        {
            // Use PlayerRespawner's bounds calculation
            Bounds bounds = new Bounds(Vector3.zero, new Vector3(5f, 0.1f, 5f));
            
            // Generate random position within bounds
            Vector3 randomPos = new Vector3(
                Random.Range(-2f, 2f),
                0.5f, // Enemy height
                Random.Range(-2f, 2f)
            );
            
            return randomPos;
        }
        
        // Fallback
        return new Vector3(
            Random.Range(-2f, 2f),
            0.5f,
            Random.Range(-2f, 2f)
        );
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

