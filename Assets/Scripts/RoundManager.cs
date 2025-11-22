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
    private List<MovementFrame> previousRoundMovements = new List<MovementFrame>();
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
        
        // Clear previous round data
        currentEnemies.Clear();
        
        // Determine enemy count (round number = enemy count)
        // Round 1 = 1 enemy, Round 2 = 2 enemies, Round 5 = 5 enemies, etc.
        int enemyCount = currentRound;
        
        if (debugMode)
        {
            Debug.Log($"Spawning {enemyCount} enemies for round {currentRound}");
            Debug.Log($"Movement data available: {previousRoundMovements != null && previousRoundMovements.Count > 0} ({previousRoundMovements?.Count ?? 0} frames)");
        }
        
        // Determine if this is the first round
        bool isFirstRound = (currentRound == 1);
        
        // Spawn enemies
        if (enemySpawner != null)
        {
            currentEnemies = enemySpawner.SpawnEnemies(
                enemyCount,
                previousRoundMovements,
                isFirstRound
            );
            
            if (debugMode)
            {
                Debug.Log($"Successfully spawned {currentEnemies.Count} enemies");
            }
            
            // Subscribe to enemy death events
            foreach (GameObject enemy in currentEnemies)
            {
                EnemyController controller = enemy.GetComponent<EnemyController>();
                if (controller != null)
                {
                    controller.OnEnemyDeath += OnEnemyDefeated;
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
        
        // Stop recording and save movement data for next round
        if (movementRecorder != null)
        {
            previousRoundMovements = movementRecorder.StopRecording();
            
            if (debugMode)
            {
                Debug.Log($"Recorded {previousRoundMovements.Count} movement frames for next round");
                if (previousRoundMovements.Count > 0)
                {
                    Debug.Log($"First frame: pos={previousRoundMovements[0].position}, time={previousRoundMovements[0].timestamp}");
                    Debug.Log($"Last frame: pos={previousRoundMovements[previousRoundMovements.Count - 1].position}, time={previousRoundMovements[previousRoundMovements.Count - 1].timestamp}");
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
        previousRoundMovements.Clear();
        
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

