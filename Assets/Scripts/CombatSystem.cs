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
        
        // Check if object has already been used for damage
        if (usedObjects.Contains(collidingObject)) return;
        
        // Check if it's a building block projectile
        BuildingBlockProjectile projectile = collidingObject.GetComponent<BuildingBlockProjectile>();
        float velocityMagnitude = velocity.magnitude;
        
        // If it's a building block projectile, use its velocity check
        if (projectile != null)
        {
            if (!projectile.IsThrown())
            {
                // Projectile not thrown fast enough
                return;
            }
            // Use projectile's velocity magnitude for more accurate detection
            velocityMagnitude = projectile.GetVelocityMagnitude();
        }
        else
        {
            // For non-projectile objects, check minimum velocity
            if (velocityMagnitude < minDamageVelocity) return;
        }
        
        // Find enemy controller on this GameObject (the enemy that was hit)
        EnemyController enemy = GetComponent<EnemyController>();
        if (enemy == null) return;
        
        // Calculate damage: velocity * mass * multiplier
        float projectileMultiplier = (projectile != null) ? projectile.damageMultiplier : 1f;
        float damage = velocityMagnitude * rb.mass * damageMultiplier * projectileMultiplier;
        damage = Mathf.Clamp(damage, 0f, maxDamage);
        
        // Apply damage
        enemy.TakeDamage(damage);
        
        Debug.Log($"[CombatSystem] {collidingObject.name} hit {gameObject.name} for {damage:F1} damage (velocity: {velocityMagnitude:F2} m/s)");
        
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

