using UnityEngine;

/// <summary>
/// Marks building block objects as projectiles that can damage enemies
/// Ensures proper physics setup for combat detection
/// </summary>
[RequireComponent(typeof(Rigidbody))]
public class BuildingBlockProjectile : MonoBehaviour
{
    [Header("Combat Settings")]
    [Tooltip("Minimum velocity to register as a thrown projectile")]
    public float minThrowVelocity = 1.0f;
    
    [Tooltip("Damage multiplier for this projectile")]
    public float damageMultiplier = 1.0f;
    
    private Rigidbody rb;
    private bool wasThrown = false;
    private float throwTime = 0f;
    private const float THROW_TIMEOUT = 5f; // Stop tracking after 5 seconds
    
    void Start()
    {
        rb = GetComponent<Rigidbody>();
        if (rb == null)
        {
            Debug.LogError($"[BuildingBlockProjectile] No Rigidbody found on {gameObject.name}");
            return;
        }
        
        // Ensure proper physics setup for combat
        rb.useGravity = true;
        rb.isKinematic = false;
        rb.collisionDetectionMode = CollisionDetectionMode.Continuous; // Better collision detection
        
        // Ensure collider exists and is not a trigger
        Collider col = GetComponent<Collider>();
        if (col == null)
        {
            // Add appropriate collider based on object type
            if (GetComponent<MeshFilter>() != null)
            {
                MeshFilter mf = GetComponent<MeshFilter>();
                if (mf.sharedMesh != null)
                {
                    // Try to determine shape
                    if (gameObject.name.ToLower().Contains("sphere") || gameObject.name.ToLower().Contains("ball"))
                    {
                        SphereCollider sphereCol = gameObject.AddComponent<SphereCollider>();
                        sphereCol.radius = 0.5f;
                    }
                    else
                    {
                        BoxCollider boxCol = gameObject.AddComponent<BoxCollider>();
                        boxCol.size = Vector3.one;
                    }
                }
            }
        }
        else
        {
            // Ensure collider is not a trigger (needed for collision detection)
            col.isTrigger = false;
        }
        
        // Register with PlayerBallCollisionIgnore if it exists
        RegisterWithCollisionIgnore();
    }
    
    void RegisterWithCollisionIgnore()
    {
        PlayerBallCollisionIgnore collisionIgnore = FindObjectOfType<PlayerBallCollisionIgnore>();
        if (collisionIgnore != null && collisionIgnore.ignoreBallCollisions)
        {
            collisionIgnore.AddBallToIgnore(gameObject);
        }
    }
    
    void Update()
    {
        if (rb == null) return;
        
        // Track if object is being thrown
        float velocity = rb.linearVelocity.magnitude;
        
        if (velocity > minThrowVelocity)
        {
            wasThrown = true;
            throwTime = Time.time;
        }
        
        // Reset throw status after timeout
        if (wasThrown && Time.time - throwTime > THROW_TIMEOUT)
        {
            wasThrown = false;
        }
    }
    
    /// <summary>
    /// Check if this object was thrown (has sufficient velocity)
    /// </summary>
    public bool IsThrown()
    {
        if (rb == null) return false;
        return rb.linearVelocity.magnitude > minThrowVelocity;
    }
    
    /// <summary>
    /// Get the current velocity magnitude
    /// </summary>
    public float GetVelocityMagnitude()
    {
        if (rb == null) return 0f;
        return rb.linearVelocity.magnitude;
    }
    
    void OnCollisionEnter(Collision collision)
    {
        // Debug log for combat detection
        if (IsThrown())
        {
            Debug.Log($"[BuildingBlockProjectile] {gameObject.name} hit {collision.gameObject.name} at velocity {GetVelocityMagnitude():F2} m/s");
        }
    }
}

