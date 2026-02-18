using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections.Generic;
using System;
using TMPro;

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
    [Tooltip("Enemy spawner")]
    public EnemySpawner enemySpawner;
    
    [Tooltip("Player respawner")]
    public PlayerRespawner playerRespawner;
    
    [Tooltip("Object position resetter (optional - will auto-find if not assigned)")]
    public ObjectPositionResetter objectResetter;
    
    [Header("Player Death Settings")]
    [Tooltip("GameObject to check for player death (XR Origin)")]
    public GameObject playerObject;
    
    [Tooltip("Panel shown when player loses (e.g. YouLostPanel). Auto-found by name if not assigned.")]
    public GameObject youLostPanel;
    
    [Tooltip("Y position threshold for player death (falling)")]
    public float deathYThreshold = -10f;
    
    [Tooltip("Offset from player position when spawning new enemy (round 2+); avoids spawning inside player")]
    public float spawnOffsetFromPlayer = 1.5f;
    
    [Header("Audio")]
    [Tooltip("AudioSource for SFX (e.g. on Audio Manager). Auto-finds GameObject named 'AudioManager' if not set.")]
    public AudioSource sfxAudioSource;
    [Tooltip("Sound played when an enemy is killed. Assign Death clip (e.g. Assets/Audio/Death.mp3).")]
    public AudioClip enemyDeathClip;
    [Tooltip("Sound played when you finish a round (round 2+). Assign Win clip (e.g. Assets/Audio/Win.mp3).")]
    public AudioClip winClip;
    
    private List<GameObject> currentEnemies = new List<GameObject>();
    private Dictionary<GameObject, int> enemyRoundAssignments = new Dictionary<GameObject, int>();
    private Vector3 lastRoundEndPlayerPosition;
    private bool isRoundActive = false;
    private bool gameOver = false;
    private PlayerHealth cachedPlayerHealth;
    private int enemiesSpawnedThisRound = 0; // Track how many enemies were spawned in current round
    private bool waitingForCutscene = false; // Track if we're waiting for cutscene to finish
    
    // Events
    public event Action<int> OnRoundStart;
    public event Action<int> OnRoundComplete;
    public event Action OnGameOver;
    public event Action OnGameReset;

    // Round Text Control
    public TextMeshProUGUI roundNumText;

    void Start()
    {
        // Find components if not assigned
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
        
        if (youLostPanel == null)
        {
            // GameObject.Find only finds ACTIVE objects; YouLostPanel is inactive at start
            youLostPanel = FindInSceneByName("YouLostPanel");
            if (youLostPanel == null && debugMode)
            {
                Debug.LogWarning("RoundManager: YouLostPanel not found. Assign in Inspector or create a UI Panel named 'YouLostPanel'.");
            }
            else if (youLostPanel != null && debugMode)
            {
                Debug.Log("RoundManager: Found YouLostPanel (was inactive).");
            }
        }
        
        cachedPlayerHealth = playerObject != null ? playerObject.GetComponent<PlayerHealth>() : null;
        if (cachedPlayerHealth == null)
        {
            GameObject playerTagged = GameObject.FindGameObjectWithTag("Player");
            if (playerTagged != null)
                cachedPlayerHealth = playerTagged.GetComponent<PlayerHealth>() ?? playerTagged.GetComponentInChildren<PlayerHealth>();
        }
        if (cachedPlayerHealth == null)
        {
            cachedPlayerHealth = FindObjectOfType<PlayerHealth>();
        }
        if (cachedPlayerHealth != null)
        {
            cachedPlayerHealth.onDeath.AddListener(HandlePlayerDeath);
        }
        else if (debugMode)
        {
            Debug.LogWarning("RoundManager: PlayerHealth not found. HP-based game over (touch/projectile) will not trigger.");
        }
        
        if (sfxAudioSource == null)
        {
            GameObject audioManagerObj = GameObject.Find("AudioManager");
            if (audioManagerObj != null)
            {
                sfxAudioSource = audioManagerObj.GetComponent<AudioSource>();
                if (sfxAudioSource != null && debugMode)
                    Debug.Log("RoundManager: Using AudioSource from AudioManager for SFX.");
            }
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
        roundNumText.text = currentRound.ToString();
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
        
        // Determine which enemy "slots" need to be filled (1 more enemy per round: round 1 = 1, round 2 = 2, etc.)
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
                
                bool isFirstRoundSlot = (roundSlot == 1);
                Vector3 spawnPosition;
                if (isFirstRoundSlot)
                {
                    // Round 1: use spawner's default position (e.g. random within bounds)
                    spawnPosition = enemySpawner.GetSpawnPositionForIndex(currentEnemies.Count, isRigged: !isFirstRoundSlot && enemySpawner.riggedEnemyPrefab != null);
                }
                else
                {
                    // Round 2+: spawn at player's last round-end position (with offset), clamped to map
                    float pad = (playerRespawner != null) ? playerRespawner.wallPadding : 1f;
                    spawnPosition = lastRoundEndPlayerPosition + new Vector3(spawnOffsetFromPlayer, 0f, spawnOffsetFromPlayer);
                    spawnPosition.y = lastRoundEndPlayerPosition.y;
                    spawnPosition = ClampSpawnToMapBounds(spawnPosition, pad);
                }
                
                int enemyIndex = currentEnemies.Count;
                GameObject newEnemy = enemySpawner.SpawnSingleEnemyAt(
                    enemyIndex,
                    spawnPosition,
                    isFirstRoundSlot
                );
                
                if (newEnemy != null)
                {
                    currentEnemies.Add(newEnemy);
                    enemyRoundAssignments[newEnemy] = roundSlot;
                    
                    EnemyController controller = newEnemy.GetComponent<EnemyController>();
                    if (controller != null)
                    {
                        controller.OnEnemyDeath += OnEnemyDefeated;
                    }
                    
                    if (debugMode)
                    {
                        Debug.Log($"Spawned enemy for slot {roundSlot} at {spawnPosition}. Total enemies: {currentEnemies.Count}");
                    }
                    
                    enemiesSpawnedThisRound++;
                }
                else
                {
                    Debug.LogWarning($"[RoundManager] SpawnSingleEnemyAt returned null for slot {roundSlot}. Assign enemy prefab on EnemySpawner.");
                }
            }
            
            if (debugMode)
            {
                Debug.Log($"Round {currentRound}: Finished spawning. Total enemies: {currentEnemies.Count} (target: {targetEnemyCount})");
            }
        }
        
        // Trigger round start event
        OnRoundStart?.Invoke(currentRound);
    }
    
    /// <summary>
    /// Handle enemy defeat
    /// </summary>
    private void OnEnemyDefeated(EnemyController enemy)
    {
        // Play death sound effect
        if (sfxAudioSource != null && enemyDeathClip != null)
            sfxAudioSource.PlayOneShot(enemyDeathClip);
        
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
        
        // Play win SFX when round is cleared (not for round 1 / first enemy)
        if (currentRound > 1 && sfxAudioSource != null && winClip != null)
            sfxAudioSource.PlayOneShot(winClip);
        
        // Store player position for next round's new enemy spawn
        if (playerObject != null)
        {
            lastRoundEndPlayerPosition = playerObject.transform.position;
            if (debugMode)
            {
                Debug.Log($"Stored last round end player position: {lastRoundEndPlayerPosition}");
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
    /// Public entry point to trigger game over (e.g. when enemy touch kills player).
    /// </summary>
    public void TriggerGameOver()
    {
        HandlePlayerDeath();
    }

    /// <summary>
    /// Handle player death - reset game
    /// </summary>
    private void HandlePlayerDeath()
    {
        if (gameOver) return;
        
        gameOver = true;
        isRoundActive = false;
        
        if (youLostPanel != null)
        {
            youLostPanel.SetActive(true);
            Transform p = youLostPanel.transform.parent;
            while (p != null)
            {
                if (!p.gameObject.activeSelf)
                    p.gameObject.SetActive(true);
                p = p.parent;
            }
        }
        
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
        
        if (youLostPanel != null)
        {
            youLostPanel.SetActive(false);
        }
        
        // Reset player health and health bar so the bar is full again after game over
        if (cachedPlayerHealth != null)
        {
            cachedPlayerHealth.ResetHealth();
            if (debugMode)
                Debug.Log("RoundManager: Reset player health and health bar for new game.");
        }
        
        // Clear enemies
        if (enemySpawner != null)
        {
            enemySpawner.ClearEnemies();
        }
        currentEnemies.Clear();
        enemyRoundAssignments.Clear();
        
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
    /// Clamp a spawn position to map bounds (WestWall, EastWall, NorthWall, SouthWall).
    /// </summary>
    private Vector3 ClampSpawnToMapBounds(Vector3 pos, float wallPadding)
    {
        GameObject w = GameObject.Find("WestWall");
        GameObject e = GameObject.Find("EastWall");
        GameObject s = GameObject.Find("SouthWall");
        GameObject n = GameObject.Find("NorthWall");
        float minX = -10f, maxX = 10f, minZ = -10f, maxZ = 10f;
        if (w != null) minX = w.transform.position.x + wallPadding;
        if (e != null) maxX = e.transform.position.x - wallPadding;
        if (s != null) minZ = s.transform.position.z + wallPadding;
        if (n != null) maxZ = n.transform.position.z - wallPadding;
        pos.x = Mathf.Clamp(pos.x, minX, maxX);
        pos.z = Mathf.Clamp(pos.z, minZ, maxZ);
        return pos;
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

    /// <summary>
    /// Find a GameObject by name in the active scene, including inactive objects.
    /// GameObject.Find() only finds active objects, so panels that start inactive are missed.
    /// </summary>
    private static GameObject FindInSceneByName(string name)
    {
        if (string.IsNullOrEmpty(name)) return null;
        GameObject[] roots = SceneManager.GetActiveScene().GetRootGameObjects();
        foreach (GameObject root in roots)
        {
            Transform found = FindChildByName(root.transform, name);
            if (found != null) return found.gameObject;
        }
        return null;
    }

    private static Transform FindChildByName(Transform parent, string name)
    {
        if (parent.name == name) return parent;
        for (int i = 0; i < parent.childCount; i++)
        {
            Transform child = parent.GetChild(i);
            Transform found = FindChildByName(child, name);
            if (found != null) return found;
        }
        return null;
    }
}

