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
    
    [Header("Melee Weapon Settings")]
    [Tooltip("Enable sword/melee weapon kills")]
    public bool meleeWeaponsEnabled = true;
    
    [Tooltip("Minimum velocity for sword kill")]
    public float swordMinVelocity = 1.0f;
    
    [Tooltip("Tags that identify melee weapons")]
    public string[] meleeWeaponTags = new string[] { "Sword", "Melee", "Weapon" };
    
    [Tooltip("Objects that have already damaged enemies (prevents multiple hits)")]
    private System.Collections.Generic.HashSet<GameObject> usedObjects = new System.Collections.Generic.HashSet<GameObject>();
    
    void OnCollisionEnter(Collision collision)
    {
        GameObject collidingObject = collision.gameObject;
        
        // Enhanced debug logging
        Debug.Log($"[CombatSystem] Collision detected with {collidingObject.name}, tag: {collidingObject.tag}, layer: {LayerMask.LayerToName(collidingObject.layer)}");
        Debug.Log($"[CombatSystem] This enemy: {gameObject.name}, tag: {gameObject.tag}, layer: {LayerMask.LayerToName(gameObject.layer)}");
        Debug.Log($"[CombatSystem] Collision velocity: {collision.relativeVelocity.magnitude:F2} m/s");
        
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
        if (rb == null)
        {
            Debug.Log($"[CombatSystem] {collidingObject.name} has no Rigidbody - ignoring");
            return;
        }

        //Check if rb is player
        if (collidingObject.CompareTag("Player"))
        {
            Debug.Log($"[CombatSystem] {collidingObject.name} is tagged as Player - ignoring");
            return;
        }
        
        // Check if object has already been used for damage
        if (usedObjects.Contains(collidingObject))
        {
            Debug.Log($"[CombatSystem] {collidingObject.name} already used for damage - ignoring");
            return;
        }
        
        // Check if it's a melee weapon (sword)
        bool isMeleeWeapon = IsMeleeWeapon(collidingObject);
        Debug.Log($"[CombatSystem] Is melee weapon? {isMeleeWeapon}, meleeWeaponsEnabled: {meleeWeaponsEnabled}");
        
        // Check if it's a building block projectile
        BuildingBlockProjectile projectile = collidingObject.GetComponent<BuildingBlockProjectile>();
        float velocityMagnitude = velocity.magnitude;
        
        // Special handling for melee weapons
        if (isMeleeWeapon && meleeWeaponsEnabled)
        {
            //// For kinematic swords (held), use collision relative velocity
            //// For non-kinematic swords (thrown), use rigidbody velocity
            //if (rb.isKinematic)
            //{
            //    // Sword is held (kinematic) - use collision velocity
            //    velocityMagnitude = velocity.magnitude;
            //    Debug.Log($"[CombatSystem] Sword (kinematic) collision velocity: {velocityMagnitude:F2} m/s");
            //}
            //else
            //{
            //    // Sword is released (non-kinematic) - use rigidbody velocity
            //    velocityMagnitude = rb.linearVelocity.magnitude;
            //    Debug.Log($"[CombatSystem] Sword (non-kinematic) rigidbody velocity: {velocityMagnitude:F2} m/s");
            //}

            //if (velocityMagnitude >= swordMinVelocity)
            //{
            //    // Sword hit with sufficient velocity - instakill
            //    InstakillEnemy(collidingObject, velocityMagnitude, "SWORD KILL");
            //    return;
            //}
            //else
            //{
            //    Debug.Log($"[CombatSystem] Sword hit but velocity too low ({velocityMagnitude:F2} < {swordMinVelocity})");
            //    return;
            //}

            InstakillEnemy(collidingObject, velocityMagnitude, "SWORD KILL");
            usedObjects.Add(collidingObject);
            StartCoroutine(ClearUsedObject(collidingObject, 2f));
            return;
        }
        
        // If it's a building block projectile, only count when the player threw it (not when enemy walks into it)
        if (projectile != null)
        {
            if (!projectile.WasThrownByPlayer())
            {
                return; // Object was not recently thrown by player (e.g. enemy walked into it)
            }
            // Use projectile's velocity magnitude for more accurate detection
            velocityMagnitude = projectile.GetVelocityMagnitude();
            if (velocityMagnitude < instakillMinVelocity)
            {
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
            InstakillEnemy(collidingObject, velocityMagnitude, "INSTAKILL");
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
    
    /// <summary>
    /// Check if object is a melee weapon (sword)
    /// </summary>
    private bool IsMeleeWeapon(GameObject obj)
    {
        // Check tag
        foreach (string tag in meleeWeaponTags)
        {
            if (obj.CompareTag(tag))
                return true;
        }
        
        // Check name
        string objName = obj.name.ToLower();
        if (objName.Contains("sword") || objName.Contains("blade") || objName.Contains("katana"))
            return true;
        
        // Check for SwordPhysicsFix component
        if (obj.GetComponent<SwordPhysicsFix>() != null)
            return true;
        
        return false;
    }
    
    /// <summary>
    /// Instantly kill enemy
    /// </summary>
    private void InstakillEnemy(GameObject weapon, float velocity, string killType)
    {
        EnemyController enemy = GetComponent<EnemyController>();
        if (enemy == null) return;
        
        float instakillDamage = enemy.maxHealth * 100f; // Way more than needed to ensure instant death
        enemy.TakeDamage(instakillDamage);
        
        Debug.Log($"[CombatSystem] {killType}! {weapon.name} hit {gameObject.name} (velocity: {velocity:F2} m/s)");
    }
    
    private System.Collections.IEnumerator ClearUsedObject(GameObject obj, float delay)
    {
        yield return new WaitForSeconds(delay);
        usedObjects.Remove(obj);
    }
}

