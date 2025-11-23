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
    
    [Header("Player Death Settings")]
    [Tooltip("GameObject to check for player death (XR Origin)")]
    public GameObject playerObject;
    
    [Tooltip("Y position threshold for player death (falling)")]
    public float deathYThreshold = -10f;
    
    private List<GameObject> currentEnemies = new List<GameObject>();
    private Dictionary<int, List<MovementFrame>> roundMovementData = new Dictionary<int, List<MovementFrame>>();
    private bool isRoundActive = false;
    private bool gameOver = false;
    
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
        }
        
        // Get movement data for the previous round (to assign to new enemy)
        List<MovementFrame> previousRoundMovements = null;
        if (currentRound > 1 && roundMovementData.ContainsKey(currentRound - 1))
        {
            previousRoundMovements = roundMovementData[currentRound - 1];
        }
        
        if (debugMode)
        {
            Debug.Log($"Round {currentRound}: Current enemies: {currentEnemies.Count}, Previous round movements: {(previousRoundMovements != null ? previousRoundMovements.Count : 0)} frames");
        }
        
        // Spawn 1 new enemy for this round
        if (enemySpawner != null)
        {
            GameObject newEnemy = enemySpawner.SpawnSingleEnemy(
                currentRound - 1, // Index for this enemy (0-based)
                previousRoundMovements,
                isFirstRound
            );
            
            if (newEnemy != null)
            {
                currentEnemies.Add(newEnemy);
                
                // Subscribe to enemy death event
                EnemyController controller = newEnemy.GetComponent<EnemyController>();
                if (controller != null)
                {
                    controller.OnEnemyDeath += OnEnemyDefeated;
                }
                
                if (debugMode)
                {
                    Debug.Log($"Spawned new enemy for round {currentRound}. Total enemies: {currentEnemies.Count}");
                }
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
        
        if (debugMode)
        {
            Debug.Log($"Enemy defeated! Remaining: {currentEnemies.Count}");
        }
    }
    
    /// <summary>
    /// Check if round is complete (all enemies defeated)
    /// </summary>
    private void CheckRoundCompletion()
    {
        // Remove null enemies (destroyed)
        currentEnemies.RemoveAll(e => e == null);
        
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
        
        // Trigger round complete event
        OnRoundComplete?.Invoke(currentRound);
        
        // Advance to next round
        StartRound(currentRound + 1);
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
}

