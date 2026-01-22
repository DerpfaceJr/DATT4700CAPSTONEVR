using UnityEngine;
using System.Collections.Generic;
using System;

/// <summary>
/// Manages round progression, enemy spawning, and game state
/// </summary>
public class RoundManager : MonoBehaviour
{
    [Header("Round Settings")]
    [Tooltip("Starting round number")]
    public int currentRound = 1;
    
    [Tooltip("Show round info in console")]
    public bool debugMode = true;
    
    [Header("References")]
    [Tooltip("Player movement recorder")]
    public PlayerMovementRecorder movementRecorder;
    
    [Tooltip("Enemy spawner")]
    public EnemySpawner enemySpawner;
    
    [Tooltip("Player respawner")]
    public PlayerRespawner playerRespawner;
    
    [Tooltip("Object position resetter (optional - will auto-find if not assigned)")]
    public ObjectPositionResetter objectResetter;
    
    [Header("Player Death Settings")]
    [Tooltip("GameObject to check for player death (XR Origin)")]
    public GameObject playerObject;
    
    [Tooltip("Y position threshold for player death (falling)")]
    public float deathYThreshold = -10f;
    
    private List<GameObject> currentEnemies = new List<GameObject>();
    private Dictionary<int, List<MovementFrame>> roundMovementData = new Dictionary<int, List<MovementFrame>>();
    private Dictionary<GameObject, int> enemyRoundAssignments = new Dictionary<GameObject, int>(); // Tracks which round data each enemy uses
    private bool isRoundActive = false;
    private bool gameOver = false;
    private int enemiesSpawnedThisRound = 0; // Track how many enemies were spawned in current round
    private bool waitingForCutscene = false; // Track if we're waiting for cutscene to finish
    
    // Events
    public event Action<int> OnRoundStart;
    public event Action<int> OnRoundComplete;
    public event Action OnGameOver;
    public event Action OnGameReset;
    
    void Start()
    {
        // Find components if not assigned
        if (movementRecorder == null)
        {
            movementRecorder = FindObjectOfType<PlayerMovementRecorder>();
        }
        
        if (enemySpawner == null)
        {
            enemySpawner = FindObjectOfType<EnemySpawner>();
        }
        
        if (playerRespawner == null)
        {
            playerRespawner = FindObjectOfType<PlayerRespawner>();
        }
        
        if (objectResetter == null)
        {
            objectResetter = FindObjectOfType<ObjectPositionResetter>();
            if (objectResetter != null)
            {
                Debug.Log("RoundManager: Found ObjectPositionResetter");
            }
        }
        
        if (playerObject == null)
        {
            GameObject xrOrigin = GameObject.Find("XR Origin");
            if (xrOrigin == null)
            {
                xrOrigin = GameObject.Find("[BuildingBlock] Camera Rig");
            }
            playerObject = xrOrigin;
        }
        
        // Start first round
        StartRound(1);
    }
    
    void Update()
    {
        // Check for player death (falling below threshold)
        if (playerObject != null && playerObject.transform.position.y < deathYThreshold)
        {
            HandlePlayerDeath();
        }
        
        // Check if all enemies are defeated
        if (isRoundActive)
        {
            CheckRoundCompletion();
        }
    }
    
    /// <summary>
    /// Start a new round
    /// </summary>
    public void StartRound(int roundNumber)
    {
        if (gameOver) return;
        
        currentRound = roundNumber;
        isRoundActive = true;
        
        if (debugMode)
        {
            Debug.Log($"=== Round {currentRound} Started ===");
        }
        
        // Respawn player at random location
        if (playerRespawner != null)
        {
            playerRespawner.RespawnPlayer();
        }
        
        // Reset throwable objects and weapons to starting positions
        if (objectResetter != null)
        {
            objectResetter.ResetObjects();
            Debug.Log($"Round {currentRound}: Reset all throwable objects and weapons");
        }
        
        // Remove null enemies (destroyed)
        currentEnemies.RemoveAll(e => e == null);
        
        // Determine if this is the first round
        bool isFirstRound = (currentRound == 1);
        
        if (isFirstRound)
        {
            // Round 1: Clear all enemies and spawn 1 static enemy
            if (enemySpawner != null)
            {
                enemySpawner.ClearEnemies();
            }
            currentEnemies.Clear();
            enemyRoundAssignments.Clear(); // Clear tracking dictionary for fresh start
            enemiesSpawnedThisRound = 0; // Reset counter for Round 1
        }
        // For Round 2+, keep all previous enemies alive (don't clear them)
        // enemiesSpawnedThisRound keeps track of how many were spawned this round
        else
        {
            // SYNC FIX: For Round 2+, sync our enemy list with EnemySpawner's actual list
            // This prevents spawning too many enemies if lists are out of sync
            if (enemySpawner != null)
            {
                List<GameObject> actualEnemies = enemySpawner.GetActiveEnemies();
                currentEnemies = actualEnemies;
                
                // Clean up dead enemies from tracking dictionary
                List<GameObject> deadEnemies = new List<GameObject>();
                foreach (var kvp in enemyRoundAssignments)
                {
                    if (kvp.Key == null || !currentEnemies.Contains(kvp.Key))
                    {
                        deadEnemies.Add(kvp.Key);
                    }
                }
                foreach (GameObject deadEnemy in deadEnemies)
                {
                    enemyRoundAssignments.Remove(deadEnemy);
                }
                
                if (debugMode)
                {
                    Debug.Log($"Round {currentRound}: Synced with EnemySpawner. Actual enemies in scene: {currentEnemies.Count}");
                }
            }
        }
        
        // Determine which enemy "slots" need to be filled
        // Each slot represents a round: slot 1 = Round 1 (static), slot 2 = Round 2 (replays R1), etc.
        int targetEnemyCount = currentRound;
        
        // Build a set of which rounds are already represented by living enemies
        HashSet<int> existingRoundSlots = new HashSet<int>();
        foreach (var kvp in enemyRoundAssignments)
        {
            if (kvp.Key != null && currentEnemies.Contains(kvp.Key))
            {
                existingRoundSlots.Add(kvp.Value);
            }
        }
        
        if (debugMode)
        {
            string existingSlotsStr = existingRoundSlots.Count > 0 ? string.Join(", ", existingRoundSlots) : "none";
            Debug.Log($"Round {currentRound}: Existing enemy slots: [{existingSlotsStr}], Target slots: 1-{targetEnemyCount}");
        }
        
        // Spawn enemies for missing slots
        if (enemySpawner != null)
        {
            for (int roundSlot = 1; roundSlot <= targetEnemyCount; roundSlot++)
            {
                // Skip if this slot is already filled
                if (existingRoundSlots.Contains(roundSlot))
                {
                    continue;
                }
                
                // Determine the movement data for this slot
                // Slot 1 (Round 1) = static, Slot 2 (Round 2) = Round 1 data, Slot N = Round N-1 data
                List<MovementFrame> movementDataForSlot = null;
                bool shouldBeStatic = (roundSlot == 1);
                
                if (!shouldBeStatic && roundMovementData.ContainsKey(roundSlot - 1))
                {
                    movementDataForSlot = roundMovementData[roundSlot - 1];
                }
                
                // Spawn the enemy
                int enemyIndex = currentEnemies.Count; // Index for spawn position calculation
                GameObject newEnemy = enemySpawner.SpawnSingleEnemy(
                    enemyIndex,
                    movementDataForSlot,
                    shouldBeStatic
                );
                
                if (newEnemy != null)
                {
                    currentEnemies.Add(newEnemy);
                    enemyRoundAssignments[newEnemy] = roundSlot; // Track which round slot this enemy represents
                    
                    // Subscribe to enemy death event
                    EnemyController controller = newEnemy.GetComponent<EnemyController>();
                    if (controller != null)
                    {
                        controller.OnEnemyDeath += OnEnemyDefeated;
                    }
                    
                    if (debugMode)
                    {
                        string dataInfo = shouldBeStatic ? "Static" : $"Round {roundSlot - 1} data ({(movementDataForSlot?.Count ?? 0)} frames)";
                        Debug.Log($"Spawned enemy for slot {roundSlot} with {dataInfo}. Total enemies: {currentEnemies.Count}");
                    }
                    
                    enemiesSpawnedThisRound++;
                }
            }
            
            if (debugMode)
            {
                Debug.Log($"Round {currentRound}: Finished spawning. Total enemies: {currentEnemies.Count} (target: {targetEnemyCount})");
            }
        }
        
        // Start recording player movements
        if (movementRecorder != null)
        {
            movementRecorder.StartRecording();
        }
        
        // Trigger round start event
        OnRoundStart?.Invoke(currentRound);
    }
    
    /// <summary>
    /// Handle enemy defeat
    /// </summary>
    private void OnEnemyDefeated(EnemyController enemy)
    {
        // Remove from list
        GameObject enemyObj = enemy.gameObject;
        if (currentEnemies.Contains(enemyObj))
        {
            currentEnemies.Remove(enemyObj);
        }
        
        // Remove from tracking dictionary
        if (enemyRoundAssignments.ContainsKey(enemyObj))
        {
            int defeatedSlot = enemyRoundAssignments[enemyObj];
            enemyRoundAssignments.Remove(enemyObj);
            
            if (debugMode)
            {
                Debug.Log($"Enemy defeated (was slot {defeatedSlot})! Remaining: {currentEnemies.Count}");
            }
        }
        else if (debugMode)
        {
            Debug.Log($"Enemy defeated! Remaining: {currentEnemies.Count}");
        }
    }
    
    /// <summary>
    /// Check if round is complete (all enemies defeated)
    /// With persistent enemies: When all enemies are killed, round completes and next round starts
    /// New rounds will keep any surviving enemies and add new ones
    /// </summary>
    private void CheckRoundCompletion()
    {
        // Remove null enemies (destroyed)
        currentEnemies.RemoveAll(e => e == null);
        
        // Round completes when all enemies are defeated
        // When next round starts, it will keep any surviving enemies (if any)
        // and spawn additional enemies to reach the round number
        if (currentEnemies.Count == 0)
        {
            CompleteRound();
        }
    }
    
    /// <summary>
    /// Complete the current round and advance to next
    /// </summary>
    private void CompleteRound()
    {
        if (!isRoundActive) return;
        
        isRoundActive = false;
        
        if (debugMode)
        {
            Debug.Log($"=== Round {currentRound} Complete! ===");
        }
        
        // Stop recording and save movement data for this round
        if (movementRecorder != null)
        {
            List<MovementFrame> thisRoundMovements = movementRecorder.StopRecording();
            
            // Store movement data for this round (will be used by next round's new enemy)
            if (thisRoundMovements != null && thisRoundMovements.Count > 0)
            {
                roundMovementData[currentRound] = thisRoundMovements;
                
                if (debugMode)
                {
                    Debug.Log($"Recorded {thisRoundMovements.Count} movement frames for round {currentRound}");
                    Debug.Log($"First frame: pos={thisRoundMovements[0].position}, time={thisRoundMovements[0].timestamp}");
                    Debug.Log($"Last frame: pos={thisRoundMovements[thisRoundMovements.Count - 1].position}, time={thisRoundMovements[thisRoundMovements.Count - 1].timestamp}");
                }
            }
        }
        
        // Reset throwable objects and weapons to starting positions when round ends
        if (objectResetter != null)
        {
            objectResetter.ResetObjects();
            if (debugMode)
            {
                Debug.Log($"Round {currentRound} Complete: Reset all throwable objects and weapons to original positions");
            }
        }
        
        // Trigger round complete event
        OnRoundComplete?.Invoke(currentRound);
        
        // Check if there's a cutscene system that will handle the delay
        RoundCompleteCutscene cutscene = FindObjectOfType<RoundCompleteCutscene>();
        if (cutscene != null)
        {
            // Cutscene will call ContinueToNextRound() when done
            waitingForCutscene = true;
            if (debugMode)
            {
                Debug.Log("Waiting for cutscene to finish before starting next round...");
            }
        }
        else
        {
            // No cutscene, advance immediately
            StartRound(currentRound + 1);
        }
    }
    
    /// <summary>
    /// Handle player death - reset game
    /// </summary>
    private void HandlePlayerDeath()
    {
        if (gameOver) return;
        
        gameOver = true;
        isRoundActive = false;
        
        if (debugMode)
        {
            Debug.Log("=== GAME OVER - Player Died ===");
        }
        
        // Trigger game over event
        OnGameOver?.Invoke();
        
        // Reset game after a delay
        Invoke(nameof(ResetGame), 2f);
    }
    
    /// <summary>
    /// Reset game to round 1
    /// </summary>
    public void ResetGame()
    {
        gameOver = false;
        currentRound = 1;
        roundMovementData.Clear();
        
        // Clear enemies
        if (enemySpawner != null)
        {
            enemySpawner.ClearEnemies();
        }
        currentEnemies.Clear();
        
        // Clear movement data
        if (movementRecorder != null)
        {
            movementRecorder.ClearData();
        }
        
        if (debugMode)
        {
            Debug.Log("=== Game Reset - Starting Round 1 ===");
        }
        
        // Trigger reset event
        OnGameReset?.Invoke();
        
        // Start round 1
        StartRound(1);
    }
    
    /// <summary>
    /// Get current round number
    /// </summary>
    public int GetCurrentRound()
    {
        return currentRound;
    }
    
    /// <summary>
    /// Get number of active enemies
    /// </summary>
    public int GetActiveEnemyCount()
    {
        currentEnemies.RemoveAll(e => e == null);
        return currentEnemies.Count;
    }
    
    /// <summary>
    /// Continue to the next round (called by cutscene when finished)
    /// </summary>
    public void ContinueToNextRound()
    {
        if (waitingForCutscene)
        {
            waitingForCutscene = false;
            StartRound(currentRound + 1);
        }
    }
}

