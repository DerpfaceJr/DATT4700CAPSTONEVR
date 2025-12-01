using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Prevents balls from physically colliding with the player body
/// Can be toggled on/off in the inspector
/// </summary>
public class PlayerBallCollisionIgnore : MonoBehaviour
{
    [Header("Collision Settings")]
    [Tooltip("Enable to prevent balls from colliding with player")]
    public bool ignoreBallCollisions = true;
    
    [Header("Player References")]
    [Tooltip("Character Controller on the player (auto-detected if not set)")]
    public CharacterController playerCharacterController;
    
    [Tooltip("Camera Rig/XR Origin GameObject (auto-detected if not set)")]
    public GameObject cameraRig;
    
    [Header("Ball Detection")]
    [Tooltip("Search for objects with 'PhysicsBall' in name")]
    public bool searchPhysicsBalls = true;
    
    [Tooltip("Search for objects with 'BuildingBlock' in name")]
    public bool searchBuildingBlocks = true;
    
    [Tooltip("Search for objects with BuildingBlockProjectile component")]
    public bool searchProjectiles = true;
    
    [Tooltip("Automatically find new balls when they spawn")]
    public bool autoFindNewBalls = true;
    
    [Tooltip("How often to search for new balls (seconds)")]
    public float searchInterval = 1f;
    
    private List<Collider> ignoredBallColliders = new List<Collider>();
    private float lastSearchTime = 0f;
    private bool isInitialized = false;
    
    void Start()
    {
        Initialize();
    }
    
    void Update()
    {
        // Auto-find new balls if enabled
        if (autoFindNewBalls && ignoreBallCollisions)
        {
            if (Time.time - lastSearchTime >= searchInterval)
            {
                FindAndIgnoreBalls();
                lastSearchTime = Time.time;
            }
        }
    }
    
    void Initialize()
    {
        // Find Character Controller
        if (playerCharacterController == null)
        {
            playerCharacterController = GetComponent<CharacterController>();
            if (playerCharacterController == null)
            {
                // Try to find VRMovementController which has CharacterController
                VRMovementController vrmc = FindObjectOfType<VRMovementController>();
                if (vrmc != null)
                {
                    playerCharacterController = vrmc.GetComponent<CharacterController>();
                }
            }
        }
        
        // Find Camera Rig
        if (cameraRig == null)
        {
            GameObject xrOrigin = GameObject.Find("XR Origin");
            if (xrOrigin == null)
            {
                xrOrigin = GameObject.Find("[BuildingBlock] Camera Rig");
            }
            if (xrOrigin != null)
            {
                cameraRig = xrOrigin;
            }
        }
        
        if (playerCharacterController == null)
        {
            Debug.LogWarning("[PlayerBallCollisionIgnore] CharacterController not found. Collision ignoring may not work properly.");
        }
        
        // Find and ignore balls
        if (ignoreBallCollisions)
        {
            FindAndIgnoreBalls();
        }
        
        isInitialized = true;
    }
    
    /// <summary>
    /// Find all balls and ignore collisions with player
    /// </summary>
    public void FindAndIgnoreBalls()
    {
        if (playerCharacterController == null) return;
        
        List<Collider> ballColliders = new List<Collider>();
        
        // Find all GameObjects in scene
        GameObject[] allObjects = FindObjectsOfType<GameObject>();
        
        foreach (GameObject obj in allObjects)
        {
            bool isBall = false;
            
            // Check by name
            if (searchPhysicsBalls && obj.name.Contains("PhysicsBall"))
            {
                isBall = true;
            }
            else if (searchBuildingBlocks && obj.name.Contains("[BuildingBlock]"))
            {
                // Only treat BuildingBlock objects as balls if they have Rigidbody (grabbable)
                Rigidbody rb = obj.GetComponent<Rigidbody>();
                if (rb != null && !rb.isKinematic)
                {
                    isBall = true;
                }
            }
            
            // Check by component
            if (!isBall && searchProjectiles)
            {
                if (obj.GetComponent<BuildingBlockProjectile>() != null)
                {
                    isBall = true;
                }
            }
            
            if (isBall)
            {
                // Get all colliders on this ball
                Collider[] colliders = obj.GetComponentsInChildren<Collider>();
                foreach (Collider col in colliders)
                {
                    if (col != null && !col.isTrigger)
                    {
                        ballColliders.Add(col);
                    }
                }
            }
        }
        
        // Ignore collisions with player CharacterController
        foreach (Collider ballCollider in ballColliders)
        {
            if (ballCollider != null && !ignoredBallColliders.Contains(ballCollider))
            {
                Physics.IgnoreCollision(playerCharacterController, ballCollider, ignoreBallCollisions);
                ignoredBallColliders.Add(ballCollider);
                
                if (isInitialized)
                {
                    Debug.Log($"[PlayerBallCollisionIgnore] Ignoring collision between player and {ballCollider.gameObject.name}");
                }
            }
        }
    }
    
    /// <summary>
    /// Manually add a ball to ignore list
    /// </summary>
    public void AddBallToIgnore(GameObject ball)
    {
        if (playerCharacterController == null || ball == null) return;
        
        Collider[] colliders = ball.GetComponentsInChildren<Collider>();
        foreach (Collider col in colliders)
        {
            if (col != null && !col.isTrigger)
            {
                Physics.IgnoreCollision(playerCharacterController, col, ignoreBallCollisions);
                if (!ignoredBallColliders.Contains(col))
                {
                    ignoredBallColliders.Add(col);
                }
            }
        }
    }
    
    /// <summary>
    /// Remove a ball from ignore list (re-enable collisions)
    /// </summary>
    public void RemoveBallFromIgnore(GameObject ball)
    {
        if (playerCharacterController == null || ball == null) return;
        
        Collider[] colliders = ball.GetComponentsInChildren<Collider>();
        foreach (Collider col in colliders)
        {
            if (col != null && ignoredBallColliders.Contains(col))
            {
                Physics.IgnoreCollision(playerCharacterController, col, false);
                ignoredBallColliders.Remove(col);
            }
        }
    }
    
    /// <summary>
    /// Update collision ignoring based on toggle state
    /// </summary>
    void OnValidate()
    {
        if (Application.isPlaying && isInitialized && playerCharacterController != null)
        {
            // Update all ignored colliders
            foreach (Collider col in ignoredBallColliders)
            {
                if (col != null)
                {
                    Physics.IgnoreCollision(playerCharacterController, col, ignoreBallCollisions);
                }
            }
        }
    }
    
    /// <summary>
    /// Clear all ignored collisions
    /// </summary>
    public void ClearIgnoredCollisions()
    {
        if (playerCharacterController == null) return;
        
        foreach (Collider col in ignoredBallColliders)
        {
            if (col != null)
            {
                Physics.IgnoreCollision(playerCharacterController, col, false);
            }
        }
        
        ignoredBallColliders.Clear();
    }
}

