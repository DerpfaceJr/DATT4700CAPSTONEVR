using UnityEngine;

/// <summary>
/// Handles damage from enemy-thrown projectiles to the player
/// Attached to projectiles spawned/thrown by enemies during replay
/// </summary>
[RequireComponent(typeof(Rigidbody))]
public class EnemyProjectileDamage : MonoBehaviour
{
    [Header("Damage Settings")]
    [Tooltip("Damage dealt to player on hit")]
    public float damageAmount = 25f;
    
    [Tooltip("Minimum velocity to deal damage")]
    public float minDamageVelocity = 2.0f;
    
    [Tooltip("Instant kill velocity threshold")]
    public float instakillVelocity = 8.0f;
    
    [Tooltip("Damage multiplier based on velocity")]
    public bool useVelocityDamageScaling = true;
    
    [Tooltip("Layer mask for player detection")]
    public LayerMask playerLayerMask = -1; // All layers by default
    
    private Rigidbody rb;
    private bool hasHit = false;
    
    void Start()
    {
        rb = GetComponent<Rigidbody>();
        if (rb == null)
        {
            Debug.LogError($"EnemyProjectileDamage: No Rigidbody on {gameObject.name}");
        }
        
        // Ensure projectile has proper physics setup
        if (rb != null)
        {
            rb.collisionDetectionMode = CollisionDetectionMode.Continuous;
        }
    }
    
    void OnCollisionEnter(Collision collision)
    {
        // Prevent multiple damage from same throw
        if (hasHit) return;
        
        // Check if we hit the player
        if (IsPlayer(collision.gameObject))
        {
            float velocity = rb != null ? rb.linearVelocity.magnitude : 0f;
            
            // Check if velocity is high enough to deal damage
            if (velocity >= minDamageVelocity)
            {
                DealDamageToPlayer(collision.gameObject, velocity);
                hasHit = true;
            }
            else
            {
                Debug.Log($"EnemyProjectileDamage: Hit player but velocity too low ({velocity:F2} < {minDamageVelocity})");
            }
        }
    }
    
    /// <summary>
    /// Check if the collided object is the player
    /// </summary>
    private bool IsPlayer(GameObject obj)
    {
        // Check tag
        if (obj.CompareTag("Player"))
            return true;
        
        // Check for XR Origin or Camera Rig
        if (obj.name.Contains("XR Origin") || obj.name.Contains("Camera Rig"))
            return true;
        
        // Check parent
        Transform parent = obj.transform.parent;
        if (parent != null && (parent.CompareTag("Player") || 
            parent.name.Contains("XR Origin") || 
            parent.name.Contains("Camera Rig")))
        {
            return true;
        }
        
        // Check for player rigged character
        IKTargetFollowVRRig ikRig = obj.GetComponent<IKTargetFollowVRRig>();
        if (ikRig != null && obj.CompareTag("Player"))
            return true;
        
        return false;
    }
    
    /// <summary>
    /// Deal damage to the player
    /// </summary>
    private void DealDamageToPlayer(GameObject playerObj, float velocity)
    {
        float finalDamage = damageAmount;
        
        // Check for instakill
        bool isInstakill = velocity >= instakillVelocity;
        
        // Scale damage by velocity if enabled
        if (useVelocityDamageScaling && !isInstakill)
        {
            float velocityMultiplier = Mathf.Clamp(velocity / minDamageVelocity, 1f, 3f);
            finalDamage *= velocityMultiplier;
        }
        
        // Try to find player health component
        PlayerHealth playerHealth = FindPlayerHealth(playerObj);
        
        if (playerHealth != null)
        {
            if (isInstakill)
            {
                playerHealth.TakeDamage(9999f); // Instant kill
                Debug.Log($"EnemyProjectileDamage: INSTAKILL! Enemy projectile hit player at {velocity:F2} m/s");
            }
            else
            {
                playerHealth.TakeDamage(finalDamage);
                Debug.Log($"EnemyProjectileDamage: Enemy projectile hit player for {finalDamage:F1} damage at {velocity:F2} m/s");
            }
        }
        else
        {
            // No health component found, just log
            Debug.LogWarning($"EnemyProjectileDamage: Hit player but no PlayerHealth component found! Would deal {finalDamage:F1} damage (velocity: {velocity:F2} m/s, instakill: {isInstakill})");
            
            // If you want to force a respawn even without health component:
            // PlayerRespawner respawner = FindObjectOfType<PlayerRespawner>();
            // if (respawner != null) respawner.RespawnPlayer();
        }
    }
    
    /// <summary>
    /// Find the PlayerHealth component on the player or nearby
    /// </summary>
    private PlayerHealth FindPlayerHealth(GameObject startObj)
    {
        // Check object itself
        PlayerHealth health = startObj.GetComponent<PlayerHealth>();
        if (health != null) return health;
        
        // Check parent hierarchy
        Transform current = startObj.transform;
        while (current != null)
        {
            health = current.GetComponent<PlayerHealth>();
            if (health != null) return health;
            current = current.parent;
        }
        
        // Check children
        health = startObj.GetComponentInChildren<PlayerHealth>();
        if (health != null) return health;
        
        // Last resort: find in scene
        return FindObjectOfType<PlayerHealth>();
    }
}
