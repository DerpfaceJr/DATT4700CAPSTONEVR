using UnityEngine;

/// <summary>
/// Detects collisions between thrown objects and enemies
/// Calculates damage based on object velocity and mass
/// </summary>
public class CombatSystem : MonoBehaviour
{
    [Header("Damage Settings")]
    [Tooltip("Damage multiplier for thrown objects")]
    public float damageMultiplier = 1f;
    
    [Tooltip("Minimum velocity to cause damage")]
    public float minDamageVelocity = 2f;
    
    [Tooltip("Maximum damage from a single hit")]
    public float maxDamage = 50f;
    
    [Header("Instakill Settings")]
    [Tooltip("Enable instakill for balls/projectiles")]
    public bool instakillEnabled = true;
    
    [Tooltip("Minimum velocity for instakill (lower than minDamageVelocity for easier kills)")]
    public float instakillMinVelocity = 0.5f;
    
    [Tooltip("Objects that have already damaged enemies (prevents multiple hits)")]
    private System.Collections.Generic.HashSet<GameObject> usedObjects = new System.Collections.Generic.HashSet<GameObject>();
    
    void OnCollisionEnter(Collision collision)
    {
        HandleCollision(collision.gameObject, collision.relativeVelocity);
    }
    
    void OnTriggerEnter(Collider other)
    {
        // For trigger-based detection
        Rigidbody rb = other.attachedRigidbody;
        if (rb != null)
        {
            #if UNITY_2023_1_OR_NEWER
            HandleCollision(other.gameObject, rb.linearVelocity);
            #else
            HandleCollision(other.gameObject, rb.velocity);
            #endif
        }
    }
    
    private void HandleCollision(GameObject collidingObject, Vector3 velocity)
    {
        // Check if this is a thrown object (has Rigidbody)
        Rigidbody rb = collidingObject.GetComponent<Rigidbody>();
        if (rb == null) return;

        //Check if rb is player
        if (collidingObject.CompareTag("Player")) return;
        
        // Check if object has already been used for damage
        if (usedObjects.Contains(collidingObject)) return;
        
        // Check if it's a building block projectile
        BuildingBlockProjectile projectile = collidingObject.GetComponent<BuildingBlockProjectile>();
        float velocityMagnitude = velocity.magnitude;
        
        // If it's a building block projectile, use its velocity check
        if (projectile != null)
        {
            // Use projectile's velocity magnitude for more accurate detection
            velocityMagnitude = projectile.GetVelocityMagnitude();
            
            // For instakill mode, use lower threshold
            if (instakillEnabled)
            {
                if (velocityMagnitude < instakillMinVelocity)
                {
                    // Too slow for instakill
                    return;
                }
            }
            else if (!projectile.IsThrown())
            {
                // Projectile not thrown fast enough (normal mode)
                return;
            }
        }
        else
        {
            // For non-projectile objects, check minimum velocity
            float requiredVelocity = instakillEnabled ? instakillMinVelocity : minDamageVelocity;
            if (velocityMagnitude < requiredVelocity) return;
        }
        
        // Find enemy controller on this GameObject (the enemy that was hit)
        EnemyController enemy = GetComponent<EnemyController>();
        if (enemy == null) return;
        
        // Check if instakill is enabled
        if (instakillEnabled)
        {
            // Instakill: Deal massive damage (more than max health)
            float instakillDamage = enemy.maxHealth * 100f; // Way more than needed to ensure instant death
            enemy.TakeDamage(instakillDamage);
            
            Debug.Log($"[CombatSystem] INSTAKILL! {collidingObject.name} hit {gameObject.name} (velocity: {velocityMagnitude:F2} m/s)");
        }
        else
        {
            // Normal damage calculation: velocity * mass * multiplier
            float projectileMultiplier = (projectile != null) ? projectile.damageMultiplier : 1f;
            float damage = velocityMagnitude * rb.mass * damageMultiplier * projectileMultiplier;
            damage = Mathf.Clamp(damage, 0f, maxDamage);
            
            // Apply damage
            enemy.TakeDamage(damage);
            
            Debug.Log($"[CombatSystem] {collidingObject.name} hit {gameObject.name} for {damage:F1} damage (velocity: {velocityMagnitude:F2} m/s)");
        }
        
        // Mark object as used
        usedObjects.Add(collidingObject);
        
        // Clean up used objects after a delay (in case object is reused)
        StartCoroutine(ClearUsedObject(collidingObject, 2f));
    }
    
    private System.Collections.IEnumerator ClearUsedObject(GameObject obj, float delay)
    {
        yield return new WaitForSeconds(delay);
        usedObjects.Remove(obj);
    }
}

